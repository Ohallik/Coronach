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
    /// The Glass Choir (dungeon, then the Last Warden's engine hall; rule: a stay held taut pins the glass, and a stay eased lets it go). Map brief, coordinates and the boss arena contract:
    /// docs/maps/GlassChoir.md. Everything of the zone's own is in <see cref="Choir"/>, so nothing here can collide with the other Oriel files' members.
    /// <c>-choir-greybox</c> leaves out dressing and the ground kit (the blockout); <c>-choir-eased</c> builds it with every stay eased, for stills (never saved as the zone).
    /// </summary>
    public static partial class OrielBuilder
    {
        static void BuildGlassChoir()=>Choir.Build();
        /// <summary>The Choir's own fields on its definition: a cooler, brighter grade than the Skyfields' for the glass, and no sky (it is under a roof, and the fixed camera never sees one).</summary>
        static void GlassChoirDefinition(ZoneDef zone)
        {
            // The glass's highlights and the fibres are what blooms; the grade is cooler than the Skyfields' gold so the body reads as glass and not as amber.
            zone.hd2dProfile=ArenaBuilder.Asset<Hd2dProfile>("Choir",p=>{p.tint=new Color(.97f,.99f,1);p.tiltStart=.2f;p.tiltStrength=0;p.nativeResolution=true;p.bloom=.7f;});
            zone.skybox=null;
            // The manifest's first silence: nobody says anything on coming in (Sela's arrival line would be "Nice place. Who broke it?").
            zone.quietArrival=true;
        }
        /// <summary>The Choir's light, registered beside its builder (ZoneLook finds it by its attribute). The body is the light in the room: one low cool light comes from the east through
        /// the glass (its direction is set by the builder, <see cref="Choir.SunYaw"/>; ZoneLook keeps a scene's yaw), so everything in the gallery has a lit side toward the body and a long
        /// shadow away from it, and the body's ribs lay bands of shade across the floor. The fill is low and cool, so the court's stone is dim beside the glass. Fog stays off.</summary>
        [ZoneLook.Source] static void GlassChoirLooks()
        {
            ZoneLook.Register(ChoirZone,new ZoneLook.Spec{sun=new Color(.88f,.93f,1f),sunIntensity=1.3f,sunPitch=Choir.SunPitch,shadowStrength=.9f,
                sky=new Color(.25f,.3f,.42f),equator=new Color(.18f,.2f,.27f),ground=new Color(.09f,.09f,.11f),
                exposure=.08f,contrast=16,saturation=4,splitShadows=new Color(.36f,.44f,.6f),splitHighlights=new Color(.55f,.55f,.54f),
                bloomThreshold=.9f,bloomIntensity=.7f,vignette=.32f});
        }

        /// <summary>The flag each clamp sets as it opens is named here (BridleClamp.openedFlag, set at run time), and the lights and fibres that wait on it.</summary>
        [SetsFlag("oriel.clamp",Prefix=true)] // oriel.clamp1 to oriel.clamp4
        internal static class Choir
        {
            // ---------------------------------------------------------------- the plan
            // x runs east and z north, so north is the back of every view. The body lies east of the route the whole way down (every room's east side is the balustrade at x 4, with the
            // body beyond it); the route goes north and down: the narthex, the nave (two levels), the first clamp, the rib walk, the sentinel bay, the fibre gallery, the heart landing,
            // the stair, and the engine hall, whose north and east rims look onto the body's chest with the heart in it.
            internal const float Door=0,NaveLow=-1.2f,ClampFloor=-2.4f,RibFloor=-4.4f,GalleryFloor=-6.6f,HallFloor=-10,EastEdge=4,RailHigh=1.05f,TrenchDrop=7;
            internal const float HallRadius=18,ClampSeat=11,ClampBody=14,FibreIn=2.6f,FibreOut=17.5f;
            /// <summary>The key light: from the east (it travels west), low, through the body.</summary>
            internal const float SunYaw=270,SunPitch=24;
            /// <summary>The engine hall's centre, C in the boss arena contract.</summary>
            internal static readonly Vector2 C=new(0,292);
            /// <summary>Plan points a player stands at, named once: the keep-offs, the navigation's must-reach list, LookDev's views, the map brief and a route all want them.</summary>
            internal static readonly Vector2 Arrival=new(0,3),DoorWay=new(0,-4.4f),Cradle=new(-6.5f,6),NaveTalk=new(-1.5f,96),ClerksCache=new(-22,51),ClampTalk=new(-4.2f,137.5f),
                FirstClamp=new(1.4f,137),BreachCache=new(11,192),Niche=new(7.4f,230),DoorSpawn=new(-3,257.5f),DoorCradle=new(-8.2f,255.8f),
                WardenTalk=new(0,273),RecordWall=new(-13.9f,300),IshenStair=new(-2.6f,268);
            /// <summary>The four stays: where the rib's root is in the body (its pivot, on the plan), which way the held rib lies from it, how long it is, the seat a hero stands at, the flag.
            /// Rib 4 holds a niche's mouth and lies along the gallery instead of across it.</summary>
            internal static readonly (Vector2 pivot,Vector2 lies,float length,Vector2 seat,string flag)[] Stays=
            {
                (new(5.8f,166),new(-1,0),12.8f,new(-5.4f,162.4f),"oriel.stay1"),
                (new(5.8f,205.6f),new(-1,0),12.9f,new(-8.2f,201.8f),"oriel.stay2"),
                (new(5.8f,249.2f),new(-1,0),12.6f,new(-7.4f,245.4f),"oriel.stay3"),
                (new(4.9f,235.4f),new(0,-1),10.4f,new(2.2f,222.6f),"oriel.stay4"),
            };
            /// <summary>The sentinels held in glass on the bay's west side until stay 2 lets the body move.</summary>
            internal static readonly Vector2[] Casings={new(-11.2f,183),new(-11.2f,197)};
            internal static readonly Vector2[] Tenders={new(-10.2f,240.5f),new(1.6f,239)};
            /// <summary>The fibres strung across the gallery, west post to the body (each a stay of the fourth clamp's works; Fibre Wisps ride them).</summary>
            internal static readonly float[] GalleryFibres={220.5f,229.5f,238.5f};
            internal static readonly float[] ClampBearings={45,315,225,135};
            internal static Vector2 Polar(float bearing,float r)=>C+new Vector2(Mathf.Sin(bearing*Mathf.Deg2Rad),Mathf.Cos(bearing*Mathf.Deg2Rad))*r;

            static float RoundBox(Vector2 p,float x0,float x1,float z0,float z1,float round=.5f)
            {
                var d=new Vector2(Mathf.Abs(p.x-(x0+x1)*.5f)-(x1-x0)*.5f+round,Mathf.Abs(p.y-(z0+z1)*.5f)-(z1-z0)*.5f+round);
                return Mathf.Min(Mathf.Max(d.x,d.y),0)+new Vector2(Mathf.Max(d.x,0),Mathf.Max(d.y,0)).magnitude-round;
            }
            internal enum Room{None,Narthex,Porch,NaveA,FlightA,NaveB,Passage,Chapel,FlightB,ClampRoom,StairA,RibWalk,Bay,Breach,StairB,Gallery,Niche,RibPass,Landing,StairC,Hall,Overlook}
            /// <summary>Where Sela speaks (z 96) the nave steps out over the trench toward the body, a railed bay whose far rail is a stride from the glass: the place to stop.</summary>
            internal const float OverlookEdge=6.6f,OverlookFrom=91.4f,OverlookTo=100.6f;
            /// <summary>Metres outside the nearest floor (negative on one), and which. Rooms that meet overlap a metre, and meet at one height there (a stair's level is clamped at its ends),
            /// so there is no step where two overlap. Every room's east side is the balustrade line, x 4, and is sharp, so the drop to the body's trench is one straight wall.</summary>
            internal static float Nearest(Vector2 p,out Room room)
            {
                float best=float.PositiveInfinity;var found=Room.None;
                void Try(float clear,Room which){if(clear<best){best=clear;found=which;}}
                Try(RoundBox(p,-10,EastEdge,-6,12.6f,.05f),Room.Narthex);
                Try(RoundBox(p,-5,5,-16,-5.6f,.4f),Room.Porch);
                Try(RoundBox(p,-7,EastEdge,11.4f,59,.05f),Room.NaveA);
                Try(RoundBox(p,-7,EastEdge,57.4f,62.6f,.05f),Room.FlightA);
                Try(RoundBox(p,-7,EastEdge,61.4f,114.6f,.05f),Room.NaveB);
                Try(RoundBox(p,-13,-6.4f,45.6f,52.4f,.3f),Room.Passage);
                Try(RoundBox(p,-27,-12.4f,39.5f,58.5f,.6f),Room.Chapel);
                Try(RoundBox(p,-7,EastEdge,113.4f,118.6f,.05f),Room.FlightB);
                Try(RoundBox(p,-12.5f,EastEdge,117.4f,150.6f,.05f),Room.ClampRoom);
                Try(RoundBox(p,-6,EastEdge,149.4f,156.6f,.05f),Room.StairA);
                Try(RoundBox(p,-7.5f,EastEdge,155.4f,172.6f,.05f),Room.RibWalk);
                Try(RoundBox(p,-15,EastEdge,171.4f,206.4f,.05f),Room.Bay);
                Try(RoundBox(p,EastEdge-.6f,14.5f,182.5f,196.5f,1.2f),Room.Breach);
                Try(RoundBox(p,-6,EastEdge,205.4f,214.6f,.05f),Room.StairB);
                Try(RoundBox(p,-12.5f,EastEdge,213.4f,247.6f,.05f),Room.Gallery);
                // The way out of the gallery's north end is as wide as rib 3 is long, so the rib closes it.
                Try(RoundBox(p,-6.2f,EastEdge,246.8f,251.2f,.05f),Room.RibPass);
                Try(RoundBox(p,EastEdge-.6f,10.6f,225.6f,234.8f,.8f),Room.Niche);
                Try(RoundBox(p,-10.5f,EastEdge,250.4f,264.6f,.05f),Room.Landing);
                Try(RoundBox(p,-4.6f,4.6f,263.4f,275.6f,.3f),Room.StairC);
                Try(Vector2.Distance(p,C)-HallRadius,Room.Hall);
                Try(RoundBox(p,EastEdge-.8f,OverlookEdge,OverlookFrom,OverlookTo,1.4f),Room.Overlook);
                room=found;return best;
            }
            static float Ramp(float z,float z0,float z1,float a,float b)=>Mathf.Lerp(a,b,Mathf.Clamp01((z-z0)/(z1-z0)));
            internal static float Level(Vector2 p,Room room)=>room switch
            {
                Room.Narthex or Room.Porch or Room.NaveA or Room.Passage or Room.Chapel=>Door,
                Room.FlightA=>Ramp(p.y,58.6f,61.4f,Door,NaveLow),
                Room.NaveB=>NaveLow,
                Room.FlightB=>Ramp(p.y,114.6f,117.4f,NaveLow,ClampFloor),
                Room.Overlook=>NaveLow,
                Room.ClampRoom=>ClampFloor,
                Room.StairA=>Ramp(p.y,150.6f,155.4f,ClampFloor,RibFloor),
                Room.RibWalk or Room.Bay or Room.Breach=>RibFloor,
                Room.StairB=>Ramp(p.y,206.4f,213.4f,RibFloor,GalleryFloor),
                Room.Gallery or Room.Niche or Room.Landing or Room.RibPass=>GalleryFloor,
                Room.StairC=>Ramp(p.y,264.6f,274.6f,GalleryFloor,HallFloor),
                Room.Hall=>HallAt(p),
                _=>Door,
            };
            /// <summary>The engine hall: level within a hand (a worn bowl of three centimetres), so the contract's "no step over .3 m inside r 17" holds by construction.</summary>
            internal static float HallAt(Vector2 p)=>HallFloor-.03f*(1-Mathf.Clamp01(Vector2.Distance(p,C)/HallRadius))+.012f*(Mathf.PerlinNoise(p.x*.4f+2,p.y*.4f+5)-.5f);
            /// <summary>The level of the walked route at a distance north: what the body's trench and the body itself follow down.</summary>
            internal static float RouteLevel(float z)=>z<58.6f?Door:z<61.4f?Ramp(z,58.6f,61.4f,Door,NaveLow):z<114.6f?NaveLow:z<117.4f?Ramp(z,114.6f,117.4f,NaveLow,ClampFloor):
                z<150.6f?ClampFloor:z<155.4f?Ramp(z,150.6f,155.4f,ClampFloor,RibFloor):z<206.4f?RibFloor:z<213.4f?Ramp(z,206.4f,213.4f,RibFloor,GalleryFloor):
                z<264.6f?GalleryFloor:z<274.6f?Ramp(z,264.6f,274.6f,GalleryFloor,HallFloor):HallFloor;
            /// <summary>The ground under the body: east of the balustrade along the gallery, and round the engine hall's north and east rims where the chest comes down. Not walked: the
            /// balustrade stops a body, and the hall's rim is a wall round the south and west.</summary>
            internal static bool InTrench(Vector2 p)
            {
                if(p.x>EastEdge&&p.y>-5.6f&&p.y<266)return true;
                if(p.y<266)return false;
                var off=p-C;float r=off.magnitude;if(r<=HallRadius||r>40)return false;
                float bearing=Mathf.Atan2(off.x,off.y)*Mathf.Rad2Deg;
                return bearing>-62&&bearing<128;
            }
            /// <summary>The trench's floor: seven metres under the walk beside it, shallower toward the great door, where the body thins to its tail and lies near the top (the tail is
            /// the first thing the party sees of it, and lies on the floor it rests on, not over a pit).</summary>
            internal static float TrenchLevel(Vector2 p)=>p.y>=266&&Vector2.Distance(p,C)>HallRadius?HallFloor-9:
                RouteLevel(Mathf.Clamp(p.y,-5,262))-TrenchDrop*Mathf.Lerp(.2f,1,Mathf.SmoothStep(0,1,Mathf.Clamp01((p.y-3)/28)));
            internal static float FloorAt(Vector2 p,out float level){float clear=Nearest(p,out var room);level=Level(p,room);return clear;}
            static readonly Vector2 Away=new(Mathf.Sin(SightlineAudit.Yaw*Mathf.Deg2Rad),Mathf.Cos(SightlineAudit.Yaw*Mathf.Deg2Rad));
            /// <summary>The ground. A floor is its room's level; east of the gallery and round the hall's north and east, the body's trench; elsewhere the cathedral's masonry, which, with a
            /// floor beyond it along the camera's line, stays under the sightline to a hero at that floor's edge (the Drowned Waystation's rule, D119). Built as a wall is, not as a slope:
            /// where the sightline lets it stand tall it stands its full height with a level top, and where it doesn't it is a parapet at hip height with a level aisle behind it, the step
            /// between the two a short face. (Walls the rule lowered by the metre read as stepped brick pyramids in the Waystation and in this zone's first blockout.)</summary>
            internal static float Height(Vector2 p)
            {
                float clear=FloorAt(p,out float level);if(clear<=0)return level;
                if(InTrench(p))return TrenchLevel(p);
                float allowed=float.PositiveInfinity;
                for(float d=.5f;d<=14;d+=.5f){if(FloorAt(p+Away*d,out float there)<0){allowed=Mathf.Min(level,there)+.6f+.82f*d;break;}}
                float rough=Mathf.PerlinNoise((p.x+70)*.11f,(p.y+20)*.09f);
                float tall=level+5.4f+.8f*rough,parapet=level+Parapet;
                float top=Mathf.Lerp(Mathf.Min(parapet,allowed),tall,Mathf.SmoothStep(0,1,Mathf.Clamp01((allowed-tall+.7f)/.7f)));
                return level+(top-level)*Mathf.SmoothStep(0,1,Mathf.Clamp01(clear/.7f));
            }
            internal const float Parapet=1.15f;
            /// <summary>Whether a body could stand here: a floor.</summary>
            internal static bool Walkable(Vector2 p)=>FloorAt(p,out _)<=0;
            static Vector3 On(ZoneKit kit,Vector2 p,float up=0)=>kit.On(p.x,p.y,up);

            /// <summary>Pale gold paving in the cathedral, worn paler along the walked middle; darker where the old engineers' works stood on it; the hall's floor bronze-dark round the engine.</summary>
            static Color Tone(Vector2 p)
            {
                float mottle=.92f+.16f*Mathf.PerlinNoise(p.x*.17f+3,p.y*.17f+7);
                // Under the body: the court's floor the body lies on, lit cool by the glass over it (it read as a dark grey field through the breach).
                if(InTrench(p)&&FloorAt(p,out _)>0)return new Color(.5f*mottle,.58f*mottle,.66f*mottle,1);
                var tone=new Color(.86f,.8f,.68f);
                // Worn smooth and paler down the middle, where a thousand years of feet went.
                float walked=1-Mathf.Clamp01(Mathf.Abs(p.x+1.5f)/5);if(p.y<266){tone=Color.Lerp(tone,new Color(.94f,.9f,.8f),.5f*walked);mottle=Mathf.Lerp(mottle,1,.6f*walked);}
                float r=Vector2.Distance(p,C);if(r<HallRadius+1)tone=Color.Lerp(new Color(.62f,.56f,.48f),new Color(.8f,.74f,.64f),Mathf.Clamp01((r-3)/12));
                // The glass's light on the floor beside it: cooler and a little brighter toward the balustrade (and round the hall's open rim), as if it lit the room.
                float lit=p.y<266?Mathf.SmoothStep(0,1,Mathf.Clamp01((p.x+1.5f)/(EastEdge+1.5f))):InTrench(C+(p-C).normalized*(HallRadius+2))?Mathf.SmoothStep(0,1,Mathf.Clamp01((r-9)/9)):0;
                tone=Color.Lerp(tone,new Color(tone.r*.98f,tone.g*1.06f,tone.b*1.18f)*1.12f,.4f*lit);
                return new Color(tone.r*mottle,tone.g*mottle,tone.b*mottle,1);
            }

            // ---------------------------------------------------------------- what it is made of
            internal static Material Paving,Masonry,Bronze,Glass,Core,Rib,Fibre,Bone,Lamp,Shaft,Tablet,Heart,HeartWarm;
            static readonly Color StoneWarm=new(1,.95f,.86f),StoneDark=new(.72f,.66f,.58f),BronzeLight=new(1,.96f,.9f),BronzeDark=new(.62f,.6f,.56f);
            static void Materials()
            {
                // Paving: big flags of pale gold stone, each its own tone, fine joints, a few cracked. The ground kit's layers lie over it.
                var flags=ZoneKit.Texture("Oriel/choir-paving",512,(u,v)=>
                {
                    float row=v*4;int course=Mathf.FloorToInt(row);float up=row-course;float across=u*4+(course%2)*.5f;int block=Mathf.FloorToInt(across);float along=across-block;
                    float own=ZoneKit.Tile(block*2.1f+course*5.3f,course*1.9f,37,5),grain=ZoneKit.Cloud(u,v,20,11),fleck=ZoneKit.Tile(u*70,v*70,70,9);
                    float tone=.72f+.12f*(own-.5f)+.1f*(grain-.5f)-(fleck>.92f?.06f:0);
                    if(up<.035f||along<.02f)tone=.42f;
                    float crack=Mathf.Abs(ZoneKit.Cloud(u*2,v*2,8,31)-.5f);if(crack<.006f&&own>.6f)tone-=.18f;
                    return new Color(tone*1.04f,tone,tone*.9f,1);
                });
                // The court's masonry: courses of carved blocks with a running band of the ring-and-bar every third course, so a wall reads as built by the same hands as the relays.
                var courses=ZoneKit.Texture("Oriel/choir-masonry",512,(u,v)=>
                {
                    float row=v*8;int course=Mathf.FloorToInt(row);float up=row-course;float across=u*4+(course%2)*.5f;int block=Mathf.FloorToInt(across);float along=across-block;
                    float own=ZoneKit.Tile(block*2.3f+course*7.1f,course*1.3f,45,11),grain=ZoneKit.Cloud(u,v,18,7);
                    float tone=.7f+.14f*(own-.5f)+.1f*(grain-.5f);
                    if(course%3==1){var c=new Vector2(Mathf.Repeat(u*12,1)-.5f,(up-.5f)*.5f);float ring=Mathf.Abs(c.magnitude-.18f);if(ring<.03f||Mathf.Abs(c.y)<.02f&&Mathf.Abs(c.x)<.34f)tone-=.16f;}
                    if(up<.06f||along<.02f)tone=.4f+.05f*own;
                    return new Color(tone*1.05f,tone,tone*.88f,1);
                });
                var bronze=ZoneKit.Texture("Oriel/choir-bronze",256,(u,v)=>
                {
                    float streak=ZoneKit.Cloud(u*4,v*.6f,4,9),pit=ZoneKit.Tile(u*48,v*48,48,3),blot=ZoneKit.Cloud(u,v,6,2);
                    float green=ZoneKit.Smooth(.5f,.74f,streak*.7f+blot*.3f);float tone=.62f+.2f*(blot-.5f)-(pit>.88f?.12f:0);
                    return Color.Lerp(new Color(tone*1.12f,tone*.86f,tone*.56f),new Color(tone*.66f,tone*.98f,tone*.86f),green*.8f);
                });
                // The old hand cut in stone (the record wall): three lines cut deep, and under them a fourth, smaller. Nobody remarks on the fourth.
                var hand=ZoneKit.Texture("Oriel/choir-tablet",512,(u,v)=>
                {
                    float tone=.6f+.08f*(ZoneKit.Cloud(u,v,9,3)-.5f);
                    foreach(var (y,size,count) in new[]{(.78f,.07f,11),(.6f,.07f,12),(.42f,.07f,10),(.24f,.04f,8)})
                    {
                        if(Mathf.Abs(v-y)>size)continue;float x0=.12f,x1=.12f+count*.065f;if(u<x0||u>x1)continue;
                        int glyph=Mathf.FloorToInt((u-x0)/.065f);float gu=(u-x0)/.065f-glyph,gv=(v-y)/size*.5f+.5f;
                        float seed=ZoneKit.Tile(glyph*3.1f+y*17,y*5,40,7);
                        bool stroke=Mathf.Abs(gu-.5f)<.07f&&seed>.3f||Mathf.Abs(gv-.5f)<.08f&&gu>.15f&&gu<.85f||Mathf.Abs(new Vector2(gu-.5f,gv-.5f).magnitude-.3f)<.06f&&seed>.55f||Mathf.Abs(gu-gv)<.07f&&seed<.4f;
                        if(stroke)tone=.24f;
                    }
                    float edge=Mathf.Min(Mathf.Min(u,1-u),Mathf.Min(v,1-v));if(edge<.04f)tone=.44f;
                    return new Color(tone*1.08f,tone*.92f,tone*.7f,1);
                });
                // The court's stone and bronze take the low light from the body and keep a deep cool shade away from it (D119's sightlines are the walls'; this is the light's):
                // the room is dimmer than the glass beside it, and a shadow across the floor is a band you see, not a tint.
                Paving=ZoneKit.Toon("choir-floor",flags,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_BaseColor",new Color(1,.94f,.84f));m.SetFloat("_BaseMapStrength",.85f);m.SetColor("_ShadowTint",new Color(.3f,.34f,.47f));m.SetFloat("_LightFloor",.07f);});
                Masonry=ZoneKit.Toon("choir-masonry",courses,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.9f,.82f,.68f));m.SetColor("_ShadowTint",new Color(.3f,.33f,.46f));m.SetFloat("_LightFloor",.08f);});
                Bronze=ZoneKit.Toon("choir-bronze",bronze,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.92f,.84f,.66f));m.SetColor("_ShadowTint",new Color(.34f,.36f,.47f));m.SetColor("_RimColor",new Color(.66f,.56f,.4f));m.SetFloat("_LightFloor",.12f);});
                Tablet=ZoneKit.Toon("choir-tablet",hand,m=>{m.SetColor("_BaseColor",new Color(.98f,.84f,.6f));m.SetFloat("_LightFloor",.32f);});
                // The body's glass. See-through at the heart of a facet and dense at its rim (the gel's optics), its own slow light drifting through it, a cold sheen and sharp glints
                // on the facets (the ice's), and drawn after everything solid and before every danger marking, writing no depth, so a marking or a body behind it is never lost.
                Glass=ZoneKit.Toon("choir-glass",null,m=>
                {
                    ZoneKit.Tinted(m);ZoneKit.With(m,"_GEL_ON","_Gel");ZoneKit.With(m,"_ICE_ON","_Ice");
                    // Lit through: the low light comes from beyond the body, so the face the gallery sees is the lit one too (no dark side to translucent glass), and its own
                    // light reads at the middle of every facet. It is the brightest broad thing in any frame of the gallery.
                    m.SetColor("_BaseColor",new Color(.74f,.88f,.96f,1));m.SetColor("_RimColor",new Color(.84f,.97f,1.08f));m.SetFloat("_RimPower",2.4f);m.SetFloat("_RimUnlit",.7f);
                    m.SetColor("_ShadowTint",new Color(.96f,1,1.06f));
                    m.SetVector("_GelParams",new Vector4(2.4f,.24f,.78f,.06f));m.SetColor("_GelDeepColor",new Color(.6f,.78f,.95f,.45f));m.SetColor("_GelCoreColor",new Color(.2f,.34f,.46f));
                    m.SetVector("_IceParams",new Vector4(2.2f,.34f,.25f,.55f));m.SetColor("_IceDeepColor",new Color(.36f,.56f,.72f));m.SetColor("_IceRimColor",new Color(.7f,.9f,1.1f));
                    m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.SetOverrideTag("RenderType","Transparent");
                    m.renderQueue=(int)RenderQueue.Transparent-40;m.SetFloat("_ReceiveShadows",0);
                });
                // A rib and a casing: the same glass, thicker (more of it is rim), so a rib pressed across the way reads as a solid thing in the way.
                Rib=ZoneKit.Toon("choir-rib",null,m=>
                {
                    ZoneKit.Tinted(m);ZoneKit.With(m,"_GEL_ON","_Gel");ZoneKit.With(m,"_ICE_ON","_Ice");
                    m.SetColor("_BaseColor",new Color(.8f,.92f,1f,1));m.SetColor("_RimColor",new Color(.88f,.98f,1.08f));m.SetFloat("_RimPower",2.2f);m.SetFloat("_RimUnlit",.7f);
                    m.SetColor("_ShadowTint",new Color(.96f,1,1.06f));
                    m.SetVector("_GelParams",new Vector4(1.8f,.42f,.9f,.3f));m.SetColor("_GelDeepColor",new Color(.62f,.8f,.96f,.5f));m.SetColor("_GelCoreColor",new Color(.18f,.32f,.44f));
                    m.SetVector("_IceParams",new Vector4(2f,.4f,.3f,.6f));m.SetColor("_IceDeepColor",new Color(.4f,.6f,.76f));m.SetColor("_IceRimColor",new Color(.76f,.94f,1.1f));
                    m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.SetOverrideTag("RenderType","Transparent");
                    m.renderQueue=(int)RenderQueue.Transparent-36;m.SetFloat("_ReceiveShadows",0);
                });
                // A fibre: unlit, its colour the line's own (a stay's pulse, a slack fibre's amber), bright enough to bloom white while it holds.
                Fibre=ZoneKit.Toon("choir-fibre",null,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetVector("_GlowParams",new Vector4(1.35f,0,0,0));m.SetFloat("_ReceiveShadows",0);});
                // The bones inside the glass, and the heart: pale and faintly lit from within, so they read through the body.
                Bone=ZoneKit.Toon("choir-bone",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.9f,.94f,.96f));m.SetColor("_EmissionColor",new Color(.14f,.2f,.24f));m.SetFloat("_LightFloor",.35f);});
                Heart=ZoneKit.Toon("choir-heart",null,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetVector("_GlowParams",new Vector4(1.1f,0,0,0));m.SetFloat("_ReceiveShadows",0);});
                // The heart by the landing: the one warm light in the building, bright enough to glow through the milky core round it.
                HeartWarm=ZoneKit.Toon("choir-heart-warm",null,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetVector("_GlowParams",new Vector4(1.25f,0,0,0));m.SetFloat("_ReceiveShadows",0);});
                Lamp=ZoneKit.Toon("choir-lamp",null,m=>{m.SetColor("_BaseColor",new Color(.9f,.96f,1));m.SetColor("_EmissionColor",new Color(1.4f,1.6f,1.8f));});
                // A shaft of light through the vault: a soft band, brightest a third of the way down and gone at both ends and both edges; additive, so it lightens what it crosses
                // and never hides it. Drawn by a line facing the camera.
                var beam=ZoneKit.Texture("Oriel/choir-beam",128,(u,v)=>
                {
                    float along=ZoneKit.Smooth(0,.3f,u)*(1-ZoneKit.Smooth(.62f,1,u)),across=Mathf.Exp(-Mathf.Pow((v-.5f)*3.4f,2));float tone=along*across;
                    return new Color(tone,tone,tone,1);
                },false);
                Shaft=ZoneKit.Toon("choir-shaft",beam,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");ZoneKit.TwoSided(m);m.SetVector("_GlowParams",new Vector4(1,0,0,0));
                    m.SetFloat("_SrcBlend",(float)BlendMode.One);m.SetFloat("_DstBlend",(float)BlendMode.One);m.SetFloat("_ZWrite",0);m.SetOverrideTag("RenderType","Transparent");
                    m.renderQueue=(int)RenderQueue.Transparent-30;m.SetFloat("_ReceiveShadows",0);});
                // The body's depth: a milky glass well inside the skin, faintly lit, so the body reads as a mass of glass with light in it and not as a pane over a pit. Drawn before
                // the skin, and like it writing no depth.
                // Denser than it was, and drawn from both sides: the body is a mass of milky glass with light in it, and what lies under it (the trench's floor) is lost in it.
                Core=ZoneKit.Toon("choir-core",null,m=>
                {
                    ZoneKit.Tinted(m);ZoneKit.With(m,"_GEL_ON","_Gel");ZoneKit.TwoSided(m);
                    m.SetColor("_BaseColor",new Color(.6f,.8f,.92f,1));m.SetColor("_RimColor",new Color(.6f,.8f,.94f));m.SetFloat("_RimPower",3f);m.SetColor("_ShadowTint",new Color(.96f,1,1.06f));
                    m.SetVector("_GelParams",new Vector4(1.6f,.46f,.74f,.05f));m.SetColor("_GelDeepColor",new Color(.5f,.72f,.92f,.6f));m.SetColor("_GelCoreColor",new Color(.14f,.27f,.38f));
                    m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.SetOverrideTag("RenderType","Transparent");
                    m.renderQueue=(int)RenderQueue.Transparent-44;m.SetFloat("_ReceiveShadows",0);
                });
                AssetDatabase.SaveAssets();
            }

            // ---------------------------------------------------------------- pieces
            /// <summary>The enemy if the combat engineer has defined it, else a stand-in of the same family (logged), so the zone is walkable and fought before the roster lands.</summary>
            static string Enemy(string id)
            {
                if(ZoneKit.Find<EnemyDef>(id)!=null)return id;
                string stand=id switch{"GlassSentinel"=>"Rootwarden","FibreWisp"=>"FrostMotes","ClampTender"=>"Scrapmite","LastWarden"=>"HostMarshal",_=>id};
                Debug.Log("CHOIR_STAND_IN "+stand+" for "+id);return stand;
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
            /// <summary>A cache: a bronze-bound coffer of the court's.</summary>
            static LootCache Coffer(ZoneKit kit,string id,string label,Vector2 at,float yaw,int scrip,params LootEntry[] contents)
            {
                var shape=new ZoneKit.Shape();
                shape.Slot(0).Box(new Vector3(0,.36f,0),new Vector3(1.3f,.72f,.8f),Quaternion.identity,StoneDark,StoneWarm).Box(new Vector3(0,.8f,0),new Vector3(1.36f,.16f,.86f),Quaternion.identity,StoneWarm);
                foreach(float x in new[]{-.42f,.42f})shape.Slot(1).Box(new Vector3(x,.45f,0),new Vector3(.1f,.92f,.9f),Quaternion.identity,BronzeLight);
                shape.Slot(1).Box(new Vector3(0,.5f,-.42f),new Vector3(.22f,.26f,.06f),Quaternion.identity,BronzeLight);
                var body=kit.Built("Coffer "+Mathf.RoundToInt(at.x)+" "+Mathf.RoundToInt(at.y),shape,Masonry,Bronze);
                body.transform.SetPositionAndRotation(kit.On(at.x,at.y),Quaternion.Euler(0,yaw,0));
                var box=body.AddComponent<BoxCollider>();box.center=Vector3.up*.45f;box.size=new Vector3(1.4f,.9f,.9f);body.isStatic=true;
                var cache=LootCache.On(body,id,label,scrip,contents);cache.range=2.6f;return cache;
            }
            /// <summary>A cradle in the court's make: the house's console and its mark (the spanner the whole game reads as a free repair) on a plinth of the court's pale stone banded
            /// in bronze, in place of the dark deck pad and the tech bollards the standard bay lays (ChapterOneStaging.CradleBay), which read as a garage in a cathedral.</summary>
            static void CourtPlinth(ZoneKit kit,string name,GameObject console,Vector2 at)
            {
                const float Rise=.2f,Side=3.2f;FloorAt(at,out float floor);var foot=new Vector3(at.x,floor,at.y);
                var bay=GameObject.Find(name+" bay");
                if(bay!=null)foreach(var part in bay.transform.Cast<Transform>().ToArray()){if(part.name=="Bay pad"||part.name=="Bay bollard")UnityEngine.Object.DestroyImmediate(part.gameObject);else part.position+=Vector3.up*Rise;}
                console.transform.position+=Vector3.up*Rise;
                var shape=new ZoneKit.Shape();
                shape.Slot(0).Box(foot+Vector3.up*(Rise*.5f-.02f),new Vector3(Side,Rise+.04f,Side),Quaternion.identity,StoneDark,StoneWarm);
                shape.Slot(0).Box(foot+Vector3.up*(Rise*.35f),new Vector3(Side+.3f,Rise*.7f,Side+.3f),Quaternion.identity,StoneDark,StoneDark);
                foreach(var (dx,dz,wx,wz) in new[]{(0f,Side*.5f-.1f,Side,.14f),(0f,-Side*.5f+.1f,Side,.14f),(Side*.5f-.1f,0f,.14f,Side),(-Side*.5f+.1f,0f,.14f,Side)})
                    shape.Slot(1).Box(foot+new Vector3(dx,Rise+.012f,dz),new Vector3(wx,.04f,wz),Quaternion.identity,BronzeDark,BronzeLight);
                var plinth=kit.Built(name+" plinth",shape,Paving,Bronze);ZoneKit.Block(plinth,foot+Vector3.up*(Rise*.5f),new Vector3(Side,Rise,Side),0,"Plinth");
            }
            /// <summary>Singing Glass growing where the body broke: a cluster of thin glass blades, gone once taken.</summary>
            static void Harvest(ZoneKit kit,string id,Vector2 at,int seed)
            {
                var shape=new ZoneKit.Shape();var rng=new System.Random(seed);
                for(int k=0;k<7;k++)
                {
                    var c=new Vector3(((float)rng.NextDouble()-.5f)*.9f,0,((float)rng.NextDouble()-.5f)*.9f);float tall=.4f+.6f*(float)rng.NextDouble();
                    var lean=new Vector3(((float)rng.NextDouble()-.5f)*.4f,1,((float)rng.NextDouble()-.5f)*.4f).normalized;
                    shape.Slot(0).Tube(new[]{c,c+lean*tall*.6f,c+lean*tall},new[]{.07f,.05f,.005f},5,new Color(.8f,.95f,1.05f),new Color(1.05f,1.1f,1.15f));
                }
                var body=kit.Built("Singing glass "+id,shape,Rib);body.transform.position=kit.On(at.x,at.y);
                var solid=body.AddComponent<BoxCollider>();solid.center=Vector3.up*.3f;solid.size=new Vector3(1.1f,.6f,1.1f);body.isStatic=true;
                var node=body.AddComponent<MineNode>();node.nodeId=id;node.material="SingingGlass";node.amount=2;node.prompt="Gather Singing Glass";node.range=2.6f;
                var glow=kit.Lamp(new Vector3(at.x,1,at.y),3.2f,.7f,new Color(.7f,.9f,1));glow.name="Singing glass light";glow.transform.SetParent(body.transform,true);
            }
            /// <summary>A kit piece fitted by how long it lies, where Prop fits by how tall it stands; drawn only.</summary>
            static GameObject Lying(ZoneKit kit,string key,Vector3 at,float length,float yaw)
            {
                var probe=WorldBuilder.Piece(key,Vector3.zero,Vector3.one,"Rock",false);var box=ModelGeometry.BoundsOf(probe);UnityEngine.Object.DestroyImmediate(probe);
                return kit.Prop(key,at,length*box.size.y/Mathf.Max(box.size.x,box.size.z),yaw,false,false);
            }
            /// <summary>The rotation that turns a rib lying along <paramref name="lies"/> up by <paramref name="degrees"/> about the level axis across it, whichever sense lifts its tip.</summary>
            static Quaternion Lift(Vector3 lies,float degrees)
            {
                var axis=Vector3.Cross(Vector3.up,lies).normalized;var a=Quaternion.AngleAxis(degrees,axis);var b=Quaternion.AngleAxis(-degrees,axis);
                return (a*lies).y>(b*lies).y?a:b;
            }

            // ---------------------------------------------------------------- the zone
            internal static void Build()
            {
                Materials();
                var kit=ZoneKit.Ground(ChoirZone,Height,1.1f);bool greybox=DevArgs.Has("-choir-greybox");
                var scene=WorldBuilder.Begin(ChoirZone);
                // The key light's direction (ZoneLook sets its colour, strength and pitch, and keeps this yaw): from the east, through the body.
                foreach(var sun in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional))
                {sun.transform.rotation=Quaternion.Euler(SunPitch,SunYaw,0);sun.shadows=LightShadows.Soft;Debug.Log("CHOIR_SUN yaw="+SunYaw+" pitch="+SunPitch);}
                kit.Terrain("Glass Choir",-40,48,-24,334,.5f,Paving,Masonry,Tone,new Color(1,.97f,.92f),new Color(.66f,.66f,.7f));
                Body(kit);Balustrade(kit);

                // ---- ways in and out, services
                WorldBuilder.Spawn("Arrival",On(kit,Arrival));WorldBuilder.Spawn("Door",On(kit,DoorSpawn));
                GreatDoor(kit);
                var cradle=kit.Prop("DeckConsole",new Vector3(Cradle.x,0,Cradle.y),1.4f,70).AddComponent<RepairBay>();cradle.free=true;cradle.saveSpawn="Arrival";cradle.prompt="Choir cradle — repair and save";
                ChapterOneStaging.CradleBay("Choir cradle",cradle.gameObject,On(kit,Cradle),kit.Y(Cradle.x,Cradle.y)+.02f,false,true);CourtPlinth(kit,"Choir cradle",cradle.gameObject,Cradle);

                // ---- the nave: the silence. One sign, the body on the right, and Sela's line well in.
                kit.Sign("THE NAVE",-4.6f,16,1.8f);
                // Across the nave and the overlook both, so nobody walks round Sela's line by the overlook's far rail.
                var nave=kit.Crossing("The body, in view",new Vector3(-.2f,0,NaveTalk.y),new Vector3(14.4f,5,3),"oriel.choirSeen","SelaGlassChoir","Sela");nave.report="Nave";
                Coffer(kit,"oriel.choir.clerks","Clerks' coffer",ClerksCache,90,180,ProgressionBuilder.AnyPart(4,GearSlot.Module,1),ProgressionBuilder.Drop("FibreThread",2,1),ProgressionBuilder.Drop("RepairSalve",1,1));

                // ---- the first clamp: the release read off its housing
                kit.Sign("FIRST CLAMP",-8.8f,124,1.8f);
                var read=kit.Crossing("The first clamp",new Vector3(ClampTalk.x,0,ClampTalk.y),new Vector3(17,5,3),"oriel.releaseLearned","IshenFirstClamp","Taren");read.report="FirstClamp";
                FirstClampWorks(kit);

                // ---- the stays: the zone's rule
                var stays=new BridleStay[Stays.Length];
                for(int i=0;i<Stays.Length;i++)stays[i]=Stay(kit,i);
                // Stay 3 has keepers: the Clamp Tenders its slack wakes walk to its seat and draw it taut again (Tended, the combat engineer's).
                var keeps=stays[2].gameObject.AddComponent<Tended>();keeps.seat=stays[2].point.transform;stays[2].keeper=keeps;
                // Stay 2's strain cracks the glass the two sentinels stand held in.
                var casings=Casings.Select((c,i)=>Casing(kit,"Sentinel casing "+(i+1),c)).ToArray();stays[1].frees=casings;
                var sentinels=Fight(kit,"Choir_Sentinels",new Vector2(-5.5f,189.5f),new Vector2(19,32),"oriel.stay2",("GlassSentinel",1,Casings[0],90),("GlassSentinel",1,Casings[1],90));
                var held=new GameObject("Sentinels in the glass");held.transform.position=On(kit,new Vector2(-6,190));sentinels.previewPoint=held.transform;sentinels.previewRadius=16;
                var fibres=Fight(kit,"Choir_Fibres",new Vector2(-4.2f,229.5f),new Vector2(16.5f,30),null,("FibreWisp",1,new Vector2(-6,224),0),("FibreWisp",1,new Vector2(0,234),0),("GlassSentinel",1,new Vector2(-4,236.5f),180));
                var tenders=Fight(kit,"Choir_Tenders",new Vector2(-4.2f,242.5f),new Vector2(16.5f,15),"oriel.stay3",("ClampTender",1,Tenders[0],90),("ClampTender",1,Tenders[1],270));
                Coffer(kit,"oriel.choir.niche","The court's coffer",Niche,-90,220,ProgressionBuilder.AnyPart(4,1),ProgressionBuilder.Drop("SingingGlass",2,1),ProgressionBuilder.Drop("SyncTonic",1,.5f));
                // The breach: where the body broke open off the bay, Singing Glass and a glass-cutter's chest.
                Coffer(kit,"oriel.choir.breach","Glass-cutter's chest",BreachCache,-60,160,ProgressionBuilder.AnyPart(4,GearSlot.Edge,1),ProgressionBuilder.Drop("SingingGlass",2,1),ProgressionBuilder.Drop("ChargeCell",2,1));
                Harvest(kit,"oriel_glass_0",new Vector2(8.6f,186),701);Harvest(kit,"oriel_glass_1",new Vector2(12.4f,195),702);

                // ---- the heart landing: the checkpoint, and the stair down
                kit.Sign("ENGINE HALL",-6.8f,262,1.8f);
                // On the landing's west side, facing the way across it (at x 5 it stood down in the body's trench, past the balustrade: the shared reach test found it).
                var doorBay=kit.Prop("DeckConsole",new Vector3(DoorCradle.x,0,DoorCradle.y),1.4f,80).AddComponent<RepairBay>();doorBay.free=true;doorBay.saveSpawn="Door";doorBay.prompt="Heart cradle — repair and save";
                ChapterOneStaging.CradleBay("Heart cradle",doorBay.gameObject,On(kit,DoorCradle),kit.Y(DoorCradle.x,DoorCradle.y)+.02f,false,false);CourtPlinth(kit,"Heart cradle",doorBay.gameObject,DoorCradle);

                // ---- the engine hall (the boss arena contract)
                var hall=EngineHall(kit,out var seal);
                Ishen(kit);

                // ---- the Bridle's hold, seen: the fibres it still pulls on, and the light that waits to move
                var hold=new GameObject("The Bridle's hold",typeof(BridleHold)).GetComponent<BridleHold>();hold.flag="oriel.wardenStoodDown";
                Fibres(kit,hold);

                if(!greybox)Dress(kit,hold);
                foreach(var line in hold.fibres)BridleHold.Pulse(line,0,hold.held,hold.band,0);foreach(var stay in stays)BridleHold.Pulse(stay.cord,0,stay.taut,stay.pulse,0);

                // For stills of the hall's clamps open (never saved as the zone: rebuild without it): the first two on their catch, the other two let go.
                if(DevArgs.Has("-choir-open"))foreach(var clamp in UnityEngine.Object.FindObjectsByType<BridleClamp>(FindObjectsSortMode.None))
                {if(clamp.whole!=null)clamp.whole.SetActive(false);var shown=clamp.index<=2?clamp.onCatch:clamp.released;if(shown!=null)shown.SetActive(true);Debug.Log("CHOIR_STILL_OPEN "+clamp.index);}
                // For stills of the gallery with every stay eased (never saved as the zone: rebuild without it).
                if(DevArgs.Has("-choir-eased"))foreach(var stay in stays){stay.rib.rotation=stay.eased;if(stay.blocks!=null)stay.blocks.SetActive(false);foreach(var g in stay.frees)g.SetActive(false);Debug.Log("CHOIR_STILL_EASED "+stay.flag);}

                // ---- limits: the cathedral's walls and the body's trench are the stop; a box on each side keeps a body from finding a way up a wall.
                WorldBuilder.Boundary("South limit",new Vector3(4,4,-23),new Vector3(88,24,2));WorldBuilder.Boundary("North limit",new Vector3(4,4,333),new Vector3(88,24,2));
                WorldBuilder.Boundary("West limit",new Vector3(-39,4,155),new Vector3(2,24,360));WorldBuilder.Boundary("East limit",new Vector3(47,4,155),new Vector3(2,24,360));

                // ---- navigation, with every stay eased and the seal open: what is held at first is play's business.
                var shut=stays.Select(s=>s.blocks).Where(b=>b!=null).ToList();var were=shut.Select(g=>g.activeSelf).ToArray();foreach(var g in shut)g.SetActive(false);
                var reach=new List<Vector2>{Arrival,Cradle+new Vector2(1.6f,0),new(0,30),new(-3,80),NaveTalk,new(5.2f,96),new(-9.5f,49),new(-20,49),ClerksCache+new Vector2(1.6f,0),ClampTalk,new(-6,140),
                    new(-2,160),new(-2,170),new(-6,190),new(9,190),BreachCache+new Vector2(0,-1.6f),new(8.6f,184.4f),new(12.4f,193.4f),new(-2,210),new(-4,230),new(-4,246),Niche+new Vector2(-1.8f,0),
                    new(-2,256),DoorSpawn,DoorCradle+new Vector2(1.6f,0),new(0,268),C,new(0,306),new(-14,292),new(14,292),RecordWall+new Vector2(1.6f,-.6f)};
                foreach(var (_,_,_,seat,_) in Stays)reach.Add(seat);
                foreach(float bearing in ClampBearings)reach.Add(Polar(bearing,ClampSeat));
                kit.BakeNavigation(new Bounds(new Vector3(4,-4,155),new Vector3(88,30,360)),Arrival,reach);
                for(int i=0;i<shut.Count;i++)shut[i].SetActive(were[i]);
                if(!greybox)Ground(kit);
                WorldBuilder.Save(scene,ChoirZone);Debug.Log("GLASS_CHOIR_OK");
            }

            // ---------------------------------------------------------------- the body
            /// <summary>The spine of the Choir's body: plan point, and its half-width and half-height there. Its centre lies three metres under the route beside it (the trench's floor is seven
            /// under), so its crest stands six over the gallery and its near flank comes up out of the trench a stride past the balustrade. Its chest curls west round the engine hall's north rim.</summary>
            static readonly (float x,float z,float wide,float tall)[] Spine=
            {
                // Its tail first: thin as a man beside the great door, lying on the trench's floor there, and thickening north to the body (the party comes in at the tail).
                (6.4f,-4.4f,.7f,.6f),(8.6f,3,2.2f,1.8f),(11.6f,14,5,4.2f),(14.4f,30,8,6.4f),(16.5f,50,10.5f,8),(17.5f,100,12,9),(17.5f,160,12.5f,9.5f),(17.5f,220,12.5f,9.5f),(18.2f,252,12.5f,9.5f),(31,280,13,10),(33,300,13,10),
                (23,318,13,10),(2,324.5f,12,9.5f),(-22,326,10,8),(-44,322,8,6),
            };
            static Vector3 SpinePoint(float t,out float wide,out float tall)
            {
                int n=Spine.Length-1;float f=Mathf.Clamp(t,0,n-.0001f);int i=Mathf.FloorToInt(f);float u=f-i;
                (float x,float z,float wide,float tall) P(int k)=>Spine[Mathf.Clamp(k,0,n)];
                var p0=P(i-1);var p1=P(i);var p2=P(i+1);var p3=P(i+2);
                float Cr(float a,float b,float c,float d)=>.5f*(2*b+(-a+c)*u+(2*a-5*b+4*c-d)*u*u+(-a+3*b-3*c+d)*u*u*u);
                float x=Cr(p0.x,p1.x,p2.x,p3.x),z=Cr(p0.z,p1.z,p2.z,p3.z);wide=Mathf.Lerp(p1.wide,p2.wide,u);tall=Mathf.Lerp(p1.tall,p2.tall,u);
                float y=z>268?HallFloor-3:RouteLevel(Mathf.Clamp(z,-5,262))-3;
                if(z>258&&z<=268)y=Mathf.Lerp(RouteLevel(262)-3,HallFloor-3,(z-258)/10);
                // The tail rests on the trench's floor where the trench is shallow, its underside on the ground; the body further north lies three under the walk as before.
                if(z<40)y=Mathf.Lerp(TrenchLevel(new Vector2(x,z))+tall*.86f,y,Mathf.SmoothStep(0,1,Mathf.Clamp01((z-8)/30)));
                return new Vector3(x,y,z);
            }
            /// <summary>Where the body's skin is broken open (the breach off the bay, the niche behind rib 4): no facet is drawn over them, so nothing see-through lies over a walked floor.</summary>
            /// <summary>The room a hero stands in inside the body (the breach, the niche): no bone or plate stands in it, or the hollow is a thicket of white.</summary>
            static bool Hollow(Vector3 p)=>p.y>RibFloor-4&&p.x>1.5f&&p.x<19&&p.z>178&&p.z<201||p.y>GalleryFloor-4&&p.x>1.5f&&p.x<14&&p.z>222&&p.z<239||HidesHollow(p);
            /// <summary>Whether a piece of the body here would stand between the fixed camera and the floor of the breach or the niche (the sightline rule the walls keep, D119): glass
            /// on the camera's side of a hollow is cut away, or it lies over the hero standing in it.</summary>
            static bool HidesHollow(Vector3 p)
            {
                if(p.z<170||p.z>242||p.x<2)return false;var plan=new Vector2(p.x,p.z);
                for(float d=0;d<=18;d+=.5f)
                {
                    var q=plan+Away*d;if(Nearest(q,out var room)>0||room!=Room.Breach&&room!=Room.Niche)continue;
                    // With a margin under the line, so what is left of the flank round the hollow lies well under the view and not grazing it.
                    if(p.y>Level(q,room)+.6f+.82f*d-2.8f)return true;
                }
                return false;
            }
            /// <summary>The skin pressed back where the overlook steps out toward it, so its rail stands a stride from the glass and never in it.</summary>
            static Vector3 KeepsOffTheOverlook(Vector3 v)
            {
                if(v.y<NaveLow-2||v.y>NaveLow+6)return v;
                float outside=Mathf.Max(OverlookFrom-1-v.z,v.z-OverlookTo-1,0);float limit=OverlookEdge+1-outside*2.5f;
                if(v.x<limit)v.x=limit+(v.x-limit)*.2f+.1f*Mathf.Sin(v.z*1.7f+v.y);
                return v;
            }
            static bool Broken(Vector3 p)=>p.y>RibFloor-1.6f&&p.x>2.6f&&p.x<16&&p.z>180.5f&&p.z<198.5f||p.y>GalleryFloor-1.4f&&p.x>2.6f&&p.x<12&&p.z>224&&p.z<237;
            /// <summary>The Choir: a faceted glass skin over a spine of bone, ribs that curve up out of the trench toward the gallery, and the old road's fibres still lit inside it, the
            /// light frozen where it was when the body was held. Each piece its own mesh, so no one box stands over the gallery.</summary>
            static void Body(ZoneKit kit)
            {
                int n=Spine.Length-1;var rng=new System.Random(907);float R()=>(float)rng.NextDouble();
                // Sample the spine finely, then lay rings every 2.6 m of its length.
                var samples=new List<(Vector3 at,float wide,float tall)>();for(float t=0;t<=n;t+=.02f){var at=SpinePoint(t,out float w,out float h);samples.Add((at,w,h));}
                var rings=new List<(Vector3 at,Vector3 along,float wide,float tall)>();float run=0,next=0;
                for(int i=1;i<samples.Count;i++)
                {
                    run+=Vector3.Distance(samples[i].at,samples[i-1].at);if(run<next)continue;next+=2.6f;
                    var along=samples[i].at-samples[i-1].at;along.y=0;rings.Add((samples[i].at,along.normalized,samples[i].wide,samples[i].tall));
                }
                const int Around=22;
                Vector3 Ring(int r,int k)
                {
                    var (at,along,wide,tall)=rings[r];var side=Vector3.Cross(Vector3.up,along).normalized;float a=k*Mathf.PI*2/Around;
                    // Facets: each ring point pushed in or out, the same for its neighbours along, so the skin is cut glass and not a tube; the body's own swell and pinch every few
                    // metres (where the ribs come to the skin) so it reads as a body and not as a pipe.
                    float facet=1+.13f*(Mathf.PerlinNoise(r*.37f+k*1.3f,k*.71f)-.5f)*2+.05f*Mathf.Sin(r*.9f+k*.4f);
                    float swell=1+.06f*Mathf.Sin(r*.55f)+.04f*Mathf.Sin(r*1.7f+1);
                    return at+side*Mathf.Cos(a)*wide*facet*swell+Vector3.up*Mathf.Sin(a)*tall*facet*swell;
                }
                // The skin, and inside it a milky core at three quarters of its size: the depth the light sits in.
                void Skin(float scale,Material glass,string name)
                {
                    var skins=new Dictionary<int,ZoneKit.Shape>();
                    Vector3 In(int r,int k)=>scale<1?rings[r].at+(Ring(r,k)-rings[r].at)*scale:KeepsOffTheOverlook(Ring(r,k));
                    for(int r=1;r<rings.Count;r++)for(int k=0;k<Around;k++)
                    {
                        Vector3 a=In(r-1,k),b=In(r-1,(k+1)%Around),c=In(r,(k+1)%Around),d=In(r,k);
                        var mid=(a+b+c+d)*.25f;if(mid.y<TrenchLevel(new Vector2(mid.x,mid.z))-1.2f||HidesHollow(mid))continue;
                        // Where the breach and the niche open the flank, the skin goes; the milky core stays on their far side, so a hollow has a back of dense glass and
                        // the trench's floor seven metres down never shows through it.
                        if(Broken(mid)&&(scale>=1||mid.x<(mid.z<210?13.5f:10)))continue;
                        var outward=mid-Vector3.Lerp(rings[r-1].at,rings[r].at,.5f);
                        int region=Mathf.FloorToInt(mid.z/40);if(!skins.TryGetValue(region,out var skin))skins[region]=skin=new ZoneKit.Shape();
                        float shade=.86f+.2f*R();var tone=new Color(.86f*shade,.95f*shade,1.05f*shade,1);
                        skin.Slot(0).Triangle(a,b,c,outward,tone,.2f).Triangle(a,c,d,outward,ZoneKit.Shape.Dim(tone,.94f),.2f);
                    }
                    foreach(var (region,skin) in skins){var drawn=kit.Built(name+" "+region,skin,glass);drawn.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;}
                }
                Skin(1,Glass,"Choir skin");Skin(.72f,Core,"Choir core");
                // The skeleton: a long spine of knuckled vertebrae, and ribs at uneven spacing that leave it, rise a little and sweep down over the flank toward the gallery, as a creature
                // lying on its side has them. Some are broken short. Dull bone in glass, not white: the glass is what catches the light.
                var bones=new Dictionary<int,ZoneKit.Shape>();int nextRib=0;
                for(int r=0;r<rings.Count;r++)
                {
                    var (at,along,wide,tall)=rings[r];var side=Vector3.Cross(Vector3.up,along).normalized;int region=Mathf.FloorToInt(at.z/40);
                    if(!bones.TryGetValue(region,out var bone))bones[region]=bone=new ZoneKit.Shape();
                    if(r%2==0&&!Hollow(at))bone.Slot(0).Lump(at,new Vector3(.9f,.7f,1.4f)*(wide/12)*(.8f+.4f*R()),r*7+3,.32f,new Color(.74f,.8f,.84f),new Color(.4f,.48f,.56f),7,5,.25f,Quaternion.LookRotation(along));
                    if(r<nextRib)continue;nextRib=r+2+rng.Next(2);
                    var toward=side;if(Vector3.Dot(toward,new Vector3(-1,0,0))<0&&at.z<268)toward=-side;
                    var path=new Vector3[7];var radii=new float[7];float reach=R()<.18f?.55f:1;float lean=(R()-.5f)*.5f;
                    for(int i=0;i<7;i++)
                    {
                        float t=i/6f*reach,ang=Mathf.Lerp(.75f,-1.25f,t);
                        path[i]=at+toward*Mathf.Cos(ang)*wide*(.5f+.38f*Mathf.Sin(t*Mathf.PI*.5f))+Vector3.up*Mathf.Sin(ang)*tall*.8f+along*lean*t*3;radii[i]=Mathf.Lerp(.5f,.1f,t)*(wide/12);
                    }
                    if(path.Any(Hollow))continue;
                    bone.Tube(path,radii,7,new Color(.5f,.58f,.66f),new Color(.8f,.86f,.9f),1,.3f);
                }
                foreach(var (region,bone) in bones)kit.Built("Choir bones "+region,bone,Bone);
                // Plates along the crest: the glass grown thick and clear, catching the light from above.
                var plates=new Dictionary<int,ZoneKit.Shape>();
                for(int r=0;r<rings.Count;r+=2)
                {
                    var (at,along,wide,tall)=rings[r];var top=at+Vector3.up*tall*.96f;int region=Mathf.FloorToInt(at.z/40);
                    if(!plates.TryGetValue(region,out var plate))plates[region]=plate=new ZoneKit.Shape();
                    var side=Vector3.Cross(Vector3.up,along).normalized;
                    for(int k=-1;k<=1;k++)
                    {
                        float size=1.1f+R()*1.4f;var at2=top+side*k*wide*.32f+Vector3.down*Mathf.Abs(k)*tall*.08f;if(Hollow(at2))continue;
                        plate.Slot(0).Lump(at2,new Vector3(size*.5f,size,size*.9f),r*11+k,.35f,new Color(.9f,.97f,1.05f),new Color(.66f,.8f,.92f),6,4,.25f,Quaternion.LookRotation(along)*Quaternion.Euler(0,0,k*-28+R()*14-7));
                    }
                }
                foreach(var (region,plate) in plates){var drawn=kit.Built("Choir plates "+region,plate,Rib);drawn.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;}
                // The road's fibres inside the body, running its length, lit; still while the Bridle holds.
                var lit=new ZoneKit.Shape();
                foreach(var (off,up,width) in new[]{(-.45f,.25f,.08f),(.2f,.55f,.06f),(-.7f,-.2f,.05f),(.5f,.1f,.07f)})
                {
                    var path=new List<Vector3>();var radii=new List<float>();
                    for(int r=0;r<rings.Count;r+=2){var (at,along,wide,tall)=rings[r];var side=Vector3.Cross(Vector3.up,along).normalized;var p=at+side*wide*off+Vector3.up*tall*up;if(Broken(p)||HidesHollow(p))continue;path.Add(p);radii.Add(width*(wide/10));}
                    for(int s=0;s+1<path.Count;s+=24){int e=Mathf.Min(path.Count,s+26);lit.Slot(0).Tube(path.GetRange(s,e-s),radii.GetRange(s,e-s),5,new Color(.62f,.8f,1),new Color(.8f,.92f,1));}
                }
                var inner=kit.Built("Choir fibres",lit,Heart);inner.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            }

            // ---------------------------------------------------------------- the balustrade
            /// <summary>Where the gallery's balustrade breaks: the breach into the body off the bay, the niche's mouth behind rib 4.</summary>
            static readonly (float z0,float z1)[] RailGaps={(184.4f,194.8f),(227.2f,233.2f),(OverlookFrom-.2f,OverlookTo+.2f)};
            /// <summary>The balustrade along the gallery's east side over the body's trench, and round the engine hall's north and east rim: a stone plinth, bronze posts and a rail at hip
            /// height. Solid (a body stops at it and the paths end at it), in pieces short enough that each stands on one floor.</summary>
            static void Balustrade(ZoneKit kit)
            {
                var runs=new List<List<Vector3>>();var run=new List<Vector3>();
                for(float z=-5.2f;z<=265.6f;z+=2)
                {
                    bool gap=RailGaps.Any(g=>z>g.z0&&z<g.z1);
                    if(gap){if(run.Count>1)runs.Add(run);run=new List<Vector3>();continue;}
                    run.Add(kit.On(EastEdge-.25f,z));if(run.Count==4){runs.Add(run);run=new List<Vector3>{run[^1]};}
                }
                if(run.Count>1)runs.Add(run);
                // Round the overlook: out from the gallery's rail, along its far edge a stride from the glass, and back.
                run=new List<Vector3>();
                foreach(var (x,z) in new[]{(EastEdge-.25f,OverlookFrom-.2f),(5.3f,OverlookFrom+.1f),(OverlookEdge-.3f,OverlookFrom+1.3f),(OverlookEdge-.25f,96f),
                    (OverlookEdge-.3f,OverlookTo-1.3f),(5.3f,OverlookTo-.1f),(EastEdge-.25f,OverlookTo+.2f)})
                {run.Add(kit.On(x,z));if(run.Count==4){runs.Add(run);run=new List<Vector3>{run[^1]};}}
                if(run.Count>1)runs.Add(run);
                run=new List<Vector3>();
                for(float bearing=-62;bearing<=128;bearing+=6)
                {
                    var p=Polar(bearing,HallRadius-.25f);run.Add(kit.On(p.x,p.y));if(run.Count==4){runs.Add(run);run=new List<Vector3>{run[^1]};}
                }
                if(run.Count>1)runs.Add(run);
                int n=0;
                foreach(var points in runs)
                {
                    var shape=new ZoneKit.Shape();
                    for(int i=1;i<points.Count;i++)
                    {
                        Vector3 a=points[i-1],b=points[i];
                        shape.Slot(0).Beam(a+Vector3.up*.16f,b+Vector3.up*.16f,.5f,.42f,StoneDark,StoneWarm);
                        shape.Slot(1).Beam(a+Vector3.up*RailHigh,b+Vector3.up*RailHigh,.12f,.1f,BronzeLight);
                    }
                    foreach(var p in points)shape.Slot(1).Post(p+Vector3.up*.3f,RailHigh-.28f,.07f,.06f,6,BronzeDark,BronzeLight);
                    var rail=kit.Built("Balustrade "+n++,shape,Masonry,Bronze);
                    for(int i=1;i<points.Count;i++)ZoneKit.BlockAlong(rail,points[i-1]+Vector3.up*.6f,points[i]+Vector3.up*.6f,.5f,1.2f,"Balustrade");
                }
            }

            // ---------------------------------------------------------------- the stays
            /// <summary>A stay: the rib it holds (a glass beam pressed across the gallery, turning on its root in the body), its fibre from a bronze seat to a collar on the rib, the seat's
            /// hand, its light, and the box it stops a body with (a seal, so every world test that opens seals opens it, and carved out of the paths while it stands).</summary>
            static BridleStay Stay(ZoneKit kit,int index)
            {
                var (pivot,lies,length,seat,flag)=Stays[index];float floor=RouteLevel(pivot.y);if(index==3)floor=GalleryFloor;
                var root=new Vector3(pivot.x,floor+1.35f,pivot.y);var dir=new Vector3(lies.x,0,lies.y);var across=Vector3.Cross(Vector3.up,dir).normalized;
                // The rib in its own space: from its root it dips to the floor, lies along it hip-high and ends on the far side; faceted by its sides.
                var shape=new ZoneKit.Shape();var path=new List<Vector3>();var radii=new List<float>();
                // Heavy: as thick as a man at its root and a forearm at its end, its middle pressed into the paving and cracking it; knuckled every couple of metres, as the body's ribs are.
                for(int i=0;i<=12;i++){float t=i/12f;path.Add(dir*length*t+Vector3.down*Mathf.Lerp(0,1.1f,Mathf.SmoothStep(0,1,Mathf.Clamp01(t*2.4f)))+across*Mathf.Sin(t*Mathf.PI)*.4f);radii.Add(Mathf.Lerp(.86f,.34f,t)*(1+.12f*Mathf.Cos(t*31)));}
                shape.Slot(0).Tube(path,radii,6,new Color(.86f,.95f,1.02f),new Color(.96f,1,1.04f),1,.35f);
                // Its marrow: a thread of the road's light down its middle, so the glass round it reads as round and deep and not as a ribbon.
                shape.Slot(2).Tube(path,radii.Select(r=>r*.2f).ToList(),5,new Color(.5f,.72f,.9f),new Color(.7f,.86f,1),1,.5f);
                // A bronze collar near its end, where the stay grips it.
                var gripAt=path[11];shape.Slot(1).Tube(new[]{gripAt-dir*.3f,gripAt+dir*.3f},new[]{radii[11]+.1f,radii[11]+.1f},12,BronzeDark,BronzeLight,1,.5f);
                var rib=kit.Built("Rib "+(index+1),shape,Rib,Bronze,Heart);rib.transform.position=root;
                // Where it presses the paving, the paving is broken and heaped: what says the rib lies on the floor and is heavy.
                var rubble=new ZoneKit.Shape();var crush=new System.Random(index*31+5);
                for(int i=5;i<=12;i++)foreach(int side in new[]{-1,1})
                {
                    if(crush.NextDouble()<.35)continue;var p=root+path[i]+across*side*(radii[i]+.15f+(float)crush.NextDouble()*.35f);p.y=floor;float bit=.16f+(float)crush.NextDouble()*.24f;
                    rubble.Slot(0).Lump(p+Vector3.up*bit*.3f,new Vector3(bit,bit*.55f,bit*.8f),i*7+side,.35f,StoneWarm,StoneDark,6,4,.25f,Quaternion.Euler(0,(float)crush.NextDouble()*360,(float)crush.NextDouble()*20));
                }
                kit.Built("Rib "+(index+1)+" rubble",rubble,Masonry);
                var grip=new GameObject("Stay grip").transform;grip.SetParent(rib.transform,false);grip.localPosition=gripAt;
                // The seat: a bronze ratchet post a stride from the rib's end on the near side: its stone foot, its drum the stay winds on, two hand seats on its face.
                var seatPost=new Vector3(seat.x,0,seat.y)+(new Vector3(pivot.x,0,pivot.y)+dir*length*.9f-new Vector3(seat.x,0,seat.y)).normalized*1.2f;
                var post=new ZoneKit.Shape();var foot=kit.On(seatPost.x,seatPost.z);var face=(new Vector3(seat.x,0,seat.y)-seatPost).normalized;var beside=Vector3.Cross(Vector3.up,face);
                post.Slot(0).Post(foot+Vector3.down*.1f,.42f,.52f,.44f,12,StoneDark,StoneWarm);
                post.Slot(1).Post(foot+Vector3.up*.3f,1.45f,.2f,.17f,10,BronzeDark,BronzeLight)
                    .Tube(new[]{foot+Vector3.up*1.32f-beside*.38f,foot+Vector3.up*1.32f+beside*.38f},new[]{.34f,.34f},14,BronzeLight,BronzeDark,1,.5f)
                    .Post(foot+Vector3.up*1.72f,.16f,.24f,.12f,10,BronzeLight,BronzeLight);
                foreach(int side in new[]{-1,1})post.Slot(1).Beam(foot+Vector3.up*.95f+beside*side*.24f,foot+Vector3.up*.95f+beside*side*.24f+face*.34f,.12f,.1f,BronzeLight).Box(foot+Vector3.up*.95f+beside*side*.24f+face*.38f,new Vector3(.2f,.06f,.16f),Quaternion.LookRotation(face),BronzeLight);
                // The stay's own object stands at its seat (a sound, a test and a clamp tender find it there); what is drawn is a child, its mesh in the world's space.
                var holder=new GameObject("Stay "+(index+1));holder.transform.position=foot;
                var seatBody=kit.Built("Stay seat "+(index+1),post,Masonry,Bronze);seatBody.transform.SetParent(holder.transform,true);ZoneKit.Block(seatBody,foot+Vector3.up*.9f,new Vector3(.9f,1.8f,.9f),0,"Stay seat");
                var anchor=new GameObject("Stay anchor").transform;anchor.SetParent(holder.transform,true);anchor.position=foot+Vector3.up*1.32f;
                var rest=new GameObject("Stay rests").transform;rest.SetParent(holder.transform,true);rest.position=kit.On(seatPost.x+across.x*1.6f-dir.x*.6f,seatPost.z+across.z*1.6f-dir.z*.6f,.05f);
                var cord=new GameObject("Stay "+(index+1)+" fibre",typeof(LineRenderer)).GetComponent<LineRenderer>();cord.transform.SetParent(holder.transform,true);
                cord.sharedMaterial=Fibre;cord.widthMultiplier=.12f;cord.numCapVertices=2;cord.shadowCastingMode=ShadowCastingMode.Off;cord.receiveShadows=false;cord.useWorldSpace=true;
                cord.positionCount=2;cord.SetPositions(new[]{anchor.position,root+gripAt});
                var lit=Vector3.Lerp(anchor.position,root+gripAt,.5f)+Vector3.up*.5f;
                var light=kit.Lamp(new Vector3(lit.x,lit.y-kit.Y(lit.x,lit.z),lit.z),5,2.2f,new Color(.86f,.94f,1));light.name="Stay light";light.transform.SetParent(holder.transform,true);
                // What it stops: a box under the held rib along its whole length, a seal (shut) with a carving obstacle, not in the bake.
                var blocks=new GameObject("Rib "+(index+1)+" held",typeof(Membrane));
                var solid=new GameObject("Held rib",typeof(BoxCollider),typeof(NavMeshObstacle));solid.transform.SetParent(blocks.transform,false);
                var middle=root+dir*length*.52f;middle.y=floor+.9f;solid.transform.SetPositionAndRotation(middle,Quaternion.LookRotation(dir));
                var size=new Vector3(2f,1.8f,length*.96f);solid.GetComponent<BoxCollider>().size=size;
                var carve=solid.GetComponent<NavMeshObstacle>();carve.shape=NavMeshObstacleShape.Box;carve.size=size;carve.carving=true;carve.carveOnlyStationary=false;
                // The hand.
                var hand=kit.Discovery("Stay "+(index+1)+" seat",new Vector3(seat.x,0,seat.y),"Take the stay's weight",flag,"Stay",null,"Taren",null,0,2.6f);
                var stay=holder.AddComponent<BridleStay>();stay.flag=flag;stay.point=hand;stay.rib=rib.transform;stay.held=Quaternion.identity;stay.eased=Lift(dir,index==3?96:108);
                stay.cord=cord;stay.anchor=anchor;stay.grip=grip;stay.rest=rest;stay.glow=light;stay.blocks=blocks;
                var shows=new Bounds(root+dir*length*.5f,new Vector3(4,3,4));shows.Encapsulate(root+Vector3.up*6);shows.Encapsulate(root+dir*length+Vector3.down);stay.shows=shows;
                return stay;
            }
            /// <summary>A Glass Sentinel's casing: a block of the body's glass grown round where it stands, cracked away by stay 2's strain.</summary>
            static GameObject Casing(ZoneKit kit,string name,Vector2 at)
            {
                var shape=new ZoneKit.Shape();var foot=kit.On(at.x,at.y);var rng=new System.Random((int)(at.y*13));
                for(int k=0;k<7;k++)
                {
                    float a=k*Mathf.PI*2/7+(float)rng.NextDouble()*.4f,tall=2.6f+(float)rng.NextDouble()*.9f;var c=foot+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*1.05f;
                    var lean=new Vector3(-Mathf.Cos(a)*.18f,1,-Mathf.Sin(a)*.18f).normalized;
                    shape.Slot(0).Tube(new[]{c+Vector3.down*.1f,c+lean*tall*.55f,c+lean*tall},new[]{.42f,.34f,.06f},5,new Color(.84f,.96f,1.04f),new Color(1,1.06f,1.1f));
                }
                var casing=kit.Built(name,shape,Rib);casing.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;return casing;
            }

            // ---------------------------------------------------------------- the first clamp
            /// <summary>The first clamp: the kit's Bridle clamp, larger than the hall's four, standing on the gallery's east side with a rib of the body drawn out over the balustrade
            /// into its jaw, the release cut in its own housing and nothing in front of it.</summary>
            static void FirstClampWorks(ZoneKit kit)
            {
                var clamp=kit.Prop(ZoneKit.Kit("BridleClamp","AncelanePump"),new Vector3(FirstClamp.x,0,FirstClamp.y),5.6f,180+ClampYaw);clamp.name="The first clamp";
                // The rib it holds, out of the body and through the jaw: a thick glass limb, clamped.
                var shape=new ZoneKit.Shape();var from=new Vector3(11,ClampFloor+3.6f,139.6f);var to=new Vector3(FirstClamp.x+1.5f,ClampFloor+2.7f,FirstClamp.y+.1f);
                shape.Slot(0).Tube(new[]{from,Vector3.Lerp(from,to,.5f)+Vector3.up*.5f,to},new[]{1.05f,.9f,.62f},10,new Color(.84f,.94f,1.02f),new Color(.94f,1,1.04f),1,.3f);
                var held=kit.Built("The first clamp's rib",shape,Rib);held.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                // The release is read off the clamp itself (world engineer A's note in the map brief): the kit's clamp carries the maker's ring-and-bar lines cut in gold along its
                // arch and two hand seats at its foot, both on the face turned to the gallery, so nothing stands in front of it; the talk's crossing (ClampTalk) is where the play
                // camera has the whole clamp in the frame.
            }
            /// <summary>Lines cut in a face: a quad whose texture is laid once across it, its front toward <paramref name="facing"/>'s forward (a built quad's texture runs from the
            /// world's origin, and would land a tablet's four lines anywhere on it).</summary>
            static GameObject Inscription(string name,Vector3 centre,Quaternion facing,Vector2 size)
            {
                var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name=name;UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.transform.SetPositionAndRotation(centre,facing);quad.transform.localScale=new Vector3(size.x,size.y,1);
                var drawn=quad.GetComponent<MeshRenderer>();drawn.sharedMaterial=Tablet;drawn.shadowCastingMode=ShadowCastingMode.Off;return quad;
            }

            // ---------------------------------------------------------------- the engine hall
            /// <summary>The Last Warden's hall, to the contract: the boss at C facing the door, shown inert from the stair; the door and its seal at the stair's foot; the four clamps (their
            /// seats r 11, their bodies r 14) and the twelve fibres on the floor; the Warden standing down where it stands once it has, and Ishen's record wall on the west rim.</summary>
            static EncounterVolume EngineHall(ZoneKit kit,out Membrane seal)
            {
                var talk=kit.Crossing("The engine hall",new Vector3(WardenTalk.x,0,WardenTalk.y),new Vector3(9,5,2.2f),"oriel.wardenMet","WardenFirst","Warden","oriel.releaseLearned");talk.saveSpawn="Door";
                seal=WorldBuilder.Membrane("Engine hall seal",kit.On(0,275.2f,1.8f),9.2f,true,3.8f);
                foreach(var filter in seal.GetComponentsInChildren<MeshFilter>())UnityEngine.Object.DestroyImmediate(filter);
                foreach(var drawnSeal in seal.GetComponentsInChildren<Renderer>())UnityEngine.Object.DestroyImmediate(drawnSeal);
                var sealBox=seal.GetComponent<BoxCollider>();var sealCarve=seal.gameObject.AddComponent<NavMeshObstacle>();sealCarve.shape=NavMeshObstacleShape.Box;sealCarve.center=sealBox.center;sealCarve.size=sealBox.size;sealCarve.carving=true;sealCarve.carveOnlyStationary=false;sealCarve.enabled=false;
                // The seal is the hall's own fibres, drawn taut across the stair's foot as the fight starts and gone when it ends.
                var bars=new ZoneKit.Shape();for(int i=0;i<=8;i++){float x=-4.4f+i*1.1f;bars.Slot(0).Tube(new[]{kit.On(x,275.2f,-.1f),kit.On(x,275.2f,3.6f)},new[]{.06f,.06f},5,new Color(.9f,.96f,1),Color.white);}
                var drawnBars=kit.Built("Engine hall seal fibres",bars,Heart);drawnBars.transform.SetParent(seal.transform,true);drawnBars.GetComponent<Renderer>().enabled=false;
                var duel=WorldBuilder.Encounter("LastWarden",kit.On(C.x,C.y,2),new Vector3(38,8,38),Enemy("LastWarden"),1,new[]{seal});
                var post=duel.spawners[0];post.transform.SetPositionAndRotation(kit.On(C.x,C.y),Quaternion.Euler(0,180,0));post.faceAsPlaced=true;post.radius=0;
                duel.requiredFlag="oriel.wardenMet";duel.checkpointSpawn="Door";
                var view=new GameObject("Last Warden preview (the stair)");view.transform.position=kit.On(0,270);duel.previewPoint=view.transform;duel.previewRadius=24;talk.previewEncounter=duel;
                // The four clamps (BridleClamp, the combat engineer's): each on an empty object at its seat (r 11, facing C), its body 3 m behind it at r 14 in three states. Opened
                // onto its catch, the clamp's jaw stands swung up on its hinge at the pillar's head (the kit clamp cut in two, tools/blender/split_clamp_jaw.py); let go, it is
                // thrown right back. Without the cut pieces the clamp is swung back whole on its base, as it was first built.
                var clamps=new GameObject("The four clamps");
                for(int i=0;i<ClampBearings.Length;i++)
                {
                    float bearing=ClampBearings[i];var seat=Polar(bearing,ClampSeat);var bodyAt=Polar(bearing,ClampBody);var toC=(C-bodyAt).normalized;float facing=Mathf.Atan2(toC.x,toC.y)*Mathf.Rad2Deg;
                    var seatObject=new GameObject("Clamp "+(i+1)+" seat");seatObject.transform.SetPositionAndRotation(kit.On(seat.x,seat.y),Quaternion.Euler(0,facing,0));seatObject.transform.SetParent(clamps.transform,true);
                    GameObject Body(string state,float swung)
                    {
                        var body=kit.Prop(ZoneKit.Kit("BridleClamp","AncelanePump"),new Vector3(bodyAt.x,0,bodyAt.y),4.2f,facing+ClampYaw,false);body.name="Clamp "+(i+1)+" "+state;
                        if(swung!=0&&!OpenJaw(body,swung))Swing(body,kit.On(bodyAt.x,bodyAt.y)-new Vector3(toC.x,0,toC.y)*.9f,new Vector3(-toC.x,0,-toC.y),swung*.27f);
                        ZoneKit.Block(body,kit.On(bodyAt.x,bodyAt.y,2),new Vector3(2.6f,4,1.8f),facing,"Clamp body");
                        // All four fade, not only the camera's two: a hero on the floor behind the north clamps (r 15 to 18) stands behind 4 m of bronze from the camera too.
                        ZoneKit.Fadeable(body);
                        body.transform.SetParent(seatObject.transform,true);return body;
                    }
                    var clamp=seatObject.AddComponent<BridleClamp>();clamp.index=i+1;clamp.settledBy="LastWarden";clamp.openedFlag="oriel.clamp"+(i+1);
                    clamp.whole=Body("shut",0);clamp.onCatch=Body("on its catch",OnCatch);clamp.released=Body("let go",LetGo);
                }
                // The twelve fibres the force runs down (ForceLine): on the floor, spokes from r 2.6 to r 17.5, every 30 degrees from 15, each held by the clamp whose sector it lies in.
                var lines=new GameObject("The hall's fibres");
                for(int k=0;k<12;k++)
                {
                    float bearing=15+30*k;var inner=Polar(bearing,FibreIn);var outer=Polar(bearing,FibreOut);
                    var line=new GameObject("Fibre "+bearing);line.transform.SetPositionAndRotation(kit.On(inner.x,inner.y,.02f),Quaternion.Euler(0,bearing,0));line.transform.SetParent(lines.transform,true);
                    var end=new GameObject("Fibre end");end.transform.SetParent(line.transform,true);end.transform.position=kit.On(outer.x,outer.y,.02f);
                    var force=line.AddComponent<ForceLine>();force.end=end.transform.position;force.clamp=ClampOf(bearing);
                }
                // After the fall: the Warden's last words once the four have let go, and its body standing where it stood, staff lowered, on every later load.
                var asks=new GameObject("The Warden stands down",typeof(AfterTalk)).GetComponent<AfterTalk>();asks.flag="bossdown.LastWarden";asks.node="WardenAsks";asks.speaker="Warden";asks.heardFlag="oriel.wardenStoodDown";asks.delay=1.2f;
                var stood=ZoneKit.Prefab("Enemies","LastWarden");
                if(stood!=null)
                {
                    var holder=new GameObject("The Warden, stood down");holder.transform.SetPositionAndRotation(kit.On(C.x,C.y),Quaternion.Euler(0,180,0));holder.isStatic=true;
                    var body=(GameObject)PrefabUtility.InstantiatePrefab(stood);body.transform.SetParent(holder.transform,false);body.name="The Warden's body";
                    foreach(var b in body.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;
                    // Staff lowered and head bowed, at once (Combat/StandsDown: a pose on its own rig, as the fight's Warden takes it at the fourth clamp); hidden while that one stands.
                    var lowered=body.AddComponent<StandsDown>();lowered.id="LastWarden";lowered.seconds=0;
                    var standing=holder.AddComponent<BoxCollider>();standing.center=Vector3.up*1.6f;standing.size=new Vector3(1.4f,3.2f,1.4f);
                    var after=new GameObject("The Warden, stood down (once the four have let go)",typeof(FlagGate)).GetComponent<FlagGate>();after.flag="bossdown.LastWarden";after.showWhenSet=true;after.targets=new[]{holder};
                }
                // The record wall: the court's own record cut where the Warden could read it, on the west rim beside the heart.
                RecordWallStone(kit);
                var reads=kit.Discovery("The record wall",new Vector3(RecordWall.x,0,RecordWall.y),"Look at the record wall","oriel.coronachHeard","RecordWall","IshenReads","Ishen",null,0,3);
                reads.requiredFlag="oriel.wardenStoodDown";
                return duel;
            }
            /// <summary>The kit's clamp faces its two hand seats toward its own back (yaw 0 shows them to the south): this turns them to face a bearing.</summary>
            internal const float ClampYaw=180;
            /// <summary>How far a hall clamp's jaw stands open on its catch, and thrown back once all four have let go (degrees about its hinge).</summary>
            internal const float OnCatch=58,LetGo=96;
            internal const string ClampParts="Assets/_Project/Art/WorldMeshes/ChoirClamp/ChoirClamp_";
            static Mesh ClampPart(string part)=>AssetDatabase.LoadAllAssetsAtPath(ClampParts+part+".fbx").OfType<Mesh>().FirstOrDefault();
            static Vector3 Middle(IReadOnlyCollection<Vector3> points){var sum=Vector3.zero;foreach(var p in points)sum+=p;return sum/Mathf.Max(1,points.Count);}
            /// <summary>Opens a placed kit clamp: its one mesh becomes the cut base, and the cut jaw, laid on the same transform, swings up about its hinge where the arm meets the
            /// pillar's head (found from the pieces themselves: the jaw's end nearest the pillar), by <paramref name="degrees"/>, its free end rising. False, and the clamp left as
            /// it was, if the pieces are missing or don't make the whole clamp again.</summary>
            static bool OpenJaw(GameObject body,float degrees)
            {
                var whole=body.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh!=null).OrderByDescending(f=>f.sharedMesh.vertexCount).FirstOrDefault();
                Mesh baseMesh=ClampPart("base"),jawMesh=ClampPart("jaw");
                if(whole==null||baseMesh==null||jawMesh==null){Debug.Log("CHOIR_CLAMP_PARTS_MISSING");return false;}
                var drawn=whole.GetComponent<MeshRenderer>();var was=drawn.bounds;var from=whole.transform;
                // The pieces come in at their file's own unit (Blender writes metres where the intake's clean file came in at centimetres): measured against the whole mesh, not assumed.
                var cutBox=baseMesh.bounds;cutBox.Encapsulate(jawMesh.bounds);float unit=whole.sharedMesh.bounds.size.magnitude/Mathf.Max(1e-6f,cutBox.size.magnitude);
                GameObject Piece(string name,Mesh mesh)
                {
                    var piece=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));piece.transform.SetParent(from.parent,false);
                    piece.transform.localPosition=from.localPosition;piece.transform.localRotation=from.localRotation;piece.transform.localScale=from.localScale*unit;
                    piece.GetComponent<MeshFilter>().sharedMesh=mesh;var r=piece.GetComponent<MeshRenderer>();r.sharedMaterials=drawn.sharedMaterials;r.shadowCastingMode=drawn.shadowCastingMode;return piece;
                }
                var stand=Piece("Clamp base",baseMesh);var jaw=Piece("Clamp jaw",jawMesh);
                var now=stand.GetComponent<MeshRenderer>().bounds;now.Encapsulate(jaw.GetComponent<MeshRenderer>().bounds);
                if(Vector3.Distance(now.min,was.min)>.15f||Vector3.Distance(now.max,was.max)>.15f)
                {Debug.Log("CHOIR_CLAMP_MISFIT was="+was+" cut="+now+" unit="+unit);UnityEngine.Object.DestroyImmediate(stand);UnityEngine.Object.DestroyImmediate(jaw);return false;}
                drawn.enabled=false;
                var jawPoints=jawMesh.vertices.Select(v=>jaw.transform.TransformPoint(v)).ToArray();
                var basePoints=baseMesh.vertices.Select(v=>stand.transform.TransformPoint(v)).ToArray();
                float cut=jawPoints.Min(v=>v.y);var pillar=Middle(basePoints.Where(v=>v.y>cut+.4f).ToArray());var free=Middle(jawPoints);
                var across=new Vector3(pillar.x-free.x,0,pillar.z-free.z).normalized;float reach=jawPoints.Max(v=>Vector3.Dot(v,across));
                var hinge=Middle(jawPoints.Where(v=>Vector3.Dot(v,across)>reach-.12f).ToArray());var axis=Vector3.Cross(Vector3.up,across).normalized;
                if((Quaternion.AngleAxis(10,axis)*(free-hinge)).y<free.y-hinge.y)degrees=-degrees;
                jaw.transform.RotateAround(hinge,axis,degrees);
                Debug.Log("CHOIR_CLAMP_OPEN "+body.name+" degrees="+Mathf.Abs(degrees)+" hinge="+hinge);return true;
            }
            /// <summary>Turns a piece about a level axis through <paramref name="pivot"/> so its top goes toward <paramref name="away"/>: a clamp swung back on its base.</summary>
            static void Swing(GameObject body,Vector3 pivot,Vector3 away,float degrees)
            {
                var axis=Vector3.Cross(Vector3.up,away).normalized;var top=ZoneKit.Drawn(body).center+Vector3.up*2-pivot;
                if(Vector3.Dot(Quaternion.AngleAxis(degrees,axis)*top-top,away)<0)axis=-axis;
                body.transform.RotateAround(pivot,axis,degrees);
            }
            /// <summary>The record wall: a tall slab of the court's stone set into the hall's north-west rim, its face cut with rows of the clerks' hand, a bronze rail round its foot.</summary>
            static void RecordWallStone(ZoneKit kit)
            {
                var shape=new ZoneKit.Shape();var at=Polar(299,HallRadius+.5f);var foot=new Vector3(at.x,HallFloor,at.y);var face=Quaternion.Euler(0,140,0);var n=face*Vector3.forward;
                shape.Slot(0).Box(foot+Vector3.up*1.9f-n*.25f,new Vector3(5.2f,3.8f,.5f),face,StoneDark,StoneWarm);
                var wall=kit.Built("The record wall",shape,Masonry);ZoneKit.Block(wall,foot+Vector3.up*1.9f-n*.25f,new Vector3(5.2f,3.8f,.5f),140,"Record wall");
                Inscription("The court's record",foot+Vector3.up*1.9f+n*.01f,face*Quaternion.Euler(0,180,0),new Vector2(4.6f,3)).transform.SetParent(wall.transform,true);
            }

            // ---------------------------------------------------------------- the door
            /// <summary>The great door at the body's tail: its bronze leaves standing open against the stone, a sill across the way out a hero steps up to and no further, the porch beyond
            /// in daylight. The way out is the sill.</summary>
            static void GreatDoor(ZoneKit kit)
            {
                var shape=new ZoneKit.Shape();
                shape.Slot(0).Box(kit.On(0,-5.6f,.28f),new Vector3(7.4f,.56f,.9f),Quaternion.identity,StoneDark,StoneWarm);
                foreach(int side in new[]{-1,1})
                {
                    shape.Slot(0).Box(kit.On(side*4.3f,-5.8f,2.6f),new Vector3(1.2f,5.2f,1.3f),Quaternion.identity,StoneDark,StoneWarm);
                    var hinge=kit.On(side*3.7f,-5.4f);var leaf=Quaternion.Euler(0,side*-72,0);
                    shape.Slot(1).Box(hinge+leaf*new Vector3(side*-1.7f,2.3f,.1f),new Vector3(3.4f,4.6f,.2f),leaf,BronzeDark,BronzeLight);
                }
                // The way's own object stands on the sill, where the prompt measures from; the drawn door is its child.
                var holder=new GameObject("Choir door");holder.transform.position=kit.On(DoorWay.x,DoorWay.y,.3f);
                var door=kit.Built("The great door",shape,Masonry,Bronze);door.transform.SetParent(holder.transform,true);
                ZoneKit.BlockAlong(door,kit.On(-3.7f,-5.6f,.3f),kit.On(3.7f,-5.6f,.3f),.9f,.6f,"Sill");
                foreach(int side in new[]{-1,1})ZoneKit.Block(door,kit.On(side*4.3f,-5.8f,2.6f),new Vector3(1.2f,5.2f,1.3f),0,"Door post");
                kit.Way(holder,"Choir door — The Skyfields",SkyfieldsZone,"ChoirDoor",null,null,3.4f,new Vector2(-2.6f,-2.6f));
            }

            // ---------------------------------------------------------------- Ishen
            /// <summary>Ishen with the party, as he walks the Skyfields (KeepsPace): at their heels from the door down the body, holding back while anything is awake (never in a fight);
            /// onto the stair as the Warden speaks, where he stays through the fight (manifest 8.4); and to the record wall once it has stood down, facing it.</summary>
            internal static readonly Vector2 IshenReads=new(-12.2f,299.4f);
            static void Ishen(ZoneKit kit)
            {
                var prefab=ZoneKit.Prefab("Characters","IshenBody");if(prefab==null)return;
                var root=new GameObject("Ishen, with the party");root.transform.SetPositionAndRotation(On(kit,new Vector2(-3.5f,-1.5f)),Quaternion.identity);
                var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);body.transform.SetParent(root.transform,false);body.name="Ishen's body";
                foreach(var rig in body.GetComponentsInChildren<Animator>()){rig.cullingMode=AnimatorCullingMode.CullUpdateTransforms;PrefabUtility.RecordPrefabInstancePropertyModifications(rig);}
                var keeps=root.AddComponent<KeepsPace>();keeps.gap=6;keeps.walk=3;keeps.hurry=6.6f;keeps.calm=14;
                keeps.stages=new[]
                {
                    new KeepsPace.Stage{when="",follows=true},
                    new KeepsPace.Stage{when="oriel.wardenMet",point=On(kit,IshenStair),faces=On(kit,C)},
                    new KeepsPace.Stage{when="oriel.wardenStoodDown",point=On(kit,IshenReads),faces=On(kit,new Vector2(-17,302))},
                };
            }

            // ---------------------------------------------------------------- the Bridle's fibres
            /// <summary>The fibres the Bridle still pulls on, drawn and pulsing (BridleHold): the three strung across the fibre gallery, and the hall's twelve laid on its floor, each of whose
            /// three goes slack as its clamp opens; and the light through the body that waits to move.</summary>
            static void Fibres(ZoneKit kit,BridleHold hold)
            {
                var lines=new List<LineRenderer>();
                LineRenderer Line(string name,Vector3 from,Vector3 to,float width,float sag)
                {
                    var line=new GameObject(name,typeof(LineRenderer)).GetComponent<LineRenderer>();line.sharedMaterial=Fibre;line.widthMultiplier=width;line.numCapVertices=2;
                    line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;line.useWorldSpace=true;var points=new Vector3[12];
                    for(int i=0;i<12;i++){float t=i/11f;points[i]=Vector3.Lerp(from,to,t)+Vector3.down*sag*4*t*(1-t);}
                    line.positionCount=12;line.SetPositions(points);lines.Add(line);return line;
                }
                // The gallery's three: from a bronze post on the west wall over the walk into the body, high enough to pass over a head.
                foreach(float z in GalleryFibres)
                {
                    var west=kit.On(-12.4f,z,3.4f);var east=new Vector3(9,GalleryFloor+3,z+1.2f);Line("Gallery fibre "+z,east,west,.06f,.25f);
                    // Its line on the floor beneath it, from the post's foot to the balustrade, for a Fibre Wisp to ride (ForceLine).
                    var under=new GameObject("Gallery fibre line "+z);under.transform.position=kit.On(-11.8f,z,.02f);
                    var force=under.AddComponent<ForceLine>();force.end=kit.On(EastEdge-.4f,z+.25f,.02f);force.clamp=0;
                    var pole=new ZoneKit.Shape();var foot=kit.On(-12.3f,z);pole.Slot(0).Post(foot+Vector3.down*.1f,3.6f,.16f,.12f,8,BronzeDark,BronzeLight).Post(foot+Vector3.up*3.3f,.3f,.22f,.22f,10,BronzeLight,BronzeLight);
                    var drawn=kit.Built("Gallery fibre post "+z,pole,Bronze);ZoneKit.Block(drawn,foot+Vector3.up*1.6f,new Vector3(.4f,3.2f,.4f),0,"Fibre post");
                }
                var slackOn=lines.Select(_=>"").ToList();
                // The hall's twelve, a hand above the floor, from the engine (the first point: the hold's pulse runs in to it) to the rim; each three go slack as their clamp opens.
                for(int k=0;k<12;k++)
                {
                    float bearing=15+30*k;var inner=Polar(bearing,FibreIn);var outer=Polar(bearing,FibreOut);
                    Line("Hall fibre "+bearing,kit.On(inner.x,inner.y,.06f),kit.On(outer.x,outer.y,.06f),.08f,0);slackOn.Add("oriel.clamp"+ClampOf(bearing));
                }
                hold.fibres=lines.ToArray();hold.slackFlags=slackOn.ToArray();
            }
            /// <summary>The clamp whose sector a hall fibre lies in (the contract's numbering: 1 NE, 2 NW, 3 SW, 4 SE).</summary>
            internal static int ClampOf(float bearing){bearing=Mathf.Repeat(bearing,360);return bearing<90?1:bearing<180?4:bearing<270?3:2;}

            // ---------------------------------------------------------------- dressing
            /// <summary>The walked lines, room by room: the ground kit keeps them clear.</summary>
            internal static readonly Vector2[][] Ways=
            {
                new Vector2[]{new(0,-3),new(-1,14),new(-1.5f,58),new(-1.5f,118),new(-3,129),new(-2,152),new(-2,170),new(-4,190),new(-2,208),new(-3,232),new(-2,252),new(-2,264),new(0,276),C},
                new Vector2[]{new(-6,49),new(-20,49)},new Vector2[]{new(-4,190),new(9,190)},new Vector2[]{new(-3,230),new(7,230)},new Vector2[]{new(-2,288),new(-12,301)},
            };
            /// <summary>What makes the Choir a cathedral round a body and not a corridor beside a sheet of glass: the court's columns and lamps down the nave's west side, the bronze
            /// inlay that runs with the hold toward the engine, the treads of every flight, shafts of light through the vault onto the glass (still until the Bridle opens; the hall's
            /// four come as their clamps open), lights in the body, the clerks' desks and the engine's ring and works round the heart.</summary>
            static void Dress(ZoneKit kit,BridleHold hold)
            {
                var rng=new System.Random(77);float R()=>(float)rng.NextDouble();
                // ---- treads on every flight, so each reads as built and not as a slope
                var treads=new ZoneKit.Shape();
                void Flight(float x0,float x1,float z0,float z1,float step){for(float z=z0+step*.5f;z<z1;z+=step){float y=RouteLevel(z);treads.Slot(0).Box(new Vector3((x0+x1)*.5f,y-.05f,z),new Vector3(x1-x0-.3f,.12f,step*.94f),Quaternion.identity,StoneDark,StoneWarm,.55f);}}
                Flight(-6.8f,EastEdge,58.6f,61.4f,.4f);Flight(-6.8f,EastEdge,114.6f,117.4f,.4f);Flight(-5.8f,EastEdge,150.6f,155.4f,.4f);Flight(-5.8f,EastEdge,206.4f,213.4f,.44f);Flight(-4.4f,4.4f,264.6f,274.6f,.42f);
                kit.Built("Stair treads",treads,Masonry);
                // ---- the nave's beat: the colonnade's stumps every ten metres down the west side, set into the wall, some whole to their capitals and some broken off, and between
                // each two what stood there: a lamp, a clerk's stall, a fallen capital. Nothing between the last two before the overlook: that stretch is the body's.
                foreach(var (z,tall) in new[]{(18f,5.4f),(28f,3.2f),(38f,5.2f),(58f,2.9f),(68f,5.4f),(78f,3.5f),(88f,5f),(98f,3f),(108f,5.4f)})Column(kit,new Vector2(-8.3f,z),tall);
                var lights=new List<Light>();
                foreach(var (x,z) in new[]{(-6.2f,23f),(-6.2f,53f),(-6.2f,83f),(-11.6f,121.5f),(-11.6f,147f),(-14.2f,176f),(-14.2f,203f),(-11.8f,216f),(-9.8f,262f),(-15.6f,281f)})
                    CourtLamp(kit,new Vector2(x,z));
                foreach(float z in new[]{33f,64f})ClerkStall(kit,new Vector2(-5.9f,z));
                foreach(var (z,yaw) in new[]{(43f,28f),(73f,-34f),(103f,12f)})FallenCapital(kit,new Vector2(-5.5f,z),yaw);
                // ---- the bronze inlay down the nave's middle, and the hold's pulse running along it toward the engine
                var inlay=new ZoneKit.Shape();var running=new List<LineRenderer>();
                foreach(var (z0,z1) in new[]{(13f,58f),(62f,114f),(118.5f,126f)})
                {
                    var a=kit.On(-1.5f,z0,.012f);var b=kit.On(-1.5f,z1,.012f);inlay.Slot(0).Beam(a,b,.34f,.03f,BronzeDark,BronzeLight);
                    var line=new GameObject("Nave inlay fibre "+z0,typeof(LineRenderer)).GetComponent<LineRenderer>();line.sharedMaterial=Fibre;line.widthMultiplier=.07f;line.useWorldSpace=true;
                    line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;line.positionCount=8;var points=new Vector3[8];for(int i=0;i<8;i++)points[i]=Vector3.Lerp(b,a,i/7f)+Vector3.up*.035f;line.SetPositions(points);
                    running.Add(line);
                }
                // The court's line carried the whole way down: on each level floor below the nave, along the walk; a border inlaid along the balustrade; a ring in the overlook's
                // floor; and in the engine hall a ring at its rim, a ring round each clamp's seat and the twelve spokes under the fibres.
                void Line(Vector2 from,Vector2 to,float wide){var a=kit.On(from.x,from.y,.012f);var b=kit.On(to.x,to.y,.012f);inlay.Slot(0).Beam(a,b,wide,.03f,BronzeDark,BronzeLight);}
                // A ring laid as short straight runs, each on the floor where it lies (one flat disc z-fought the hall's shallow bowl at its rim and drew as a dashed line).
                void Ring(Vector2 at,float radius,float wide){int sides=Mathf.Clamp(Mathf.RoundToInt(radius*7),16,128);for(int k=0;k<sides;k++){float a=k*Mathf.PI*2/sides,b=(k+1.04f)*Mathf.PI*2/sides;Line(at+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*radius,at+new Vector2(Mathf.Sin(b),Mathf.Cos(b))*radius,wide);}}
                foreach(var (from,to) in new[]{(new Vector2(-2,128f),new Vector2(-2,149.6f)),(new Vector2(-2,156.4f),new Vector2(-2,171f)),(new Vector2(-3.5f,173f),new Vector2(-3.5f,205f)),
                    (new Vector2(-3,215f),new Vector2(-3,246.6f)),(new Vector2(-1.5f,251f),new Vector2(-1.5f,263.8f))})Line(from,to,.34f);
                foreach(var (z0,z1) in new[]{(-4.4f,57.6f),(62.4f,OverlookFrom-.4f),(OverlookTo+.4f,113.6f),(118.4f,149.6f),(156.4f,184f),(195.2f,205.4f),(214.4f,226.8f),(233.6f,264f)})
                    for(float z=z0;z<z1-.01f;z+=2){float to=Mathf.Min(z1,z+2);Line(new Vector2(EastEdge-1.1f,z),new Vector2(EastEdge-1.1f,to),.16f);}
                Ring(new Vector2(5,96),1.05f,.14f);Ring(new Vector2(5,96),.4f,.1f);
                Ring(C,HallRadius-1.2f,.22f);Ring(C,HallRadius-1.6f,.08f);
                foreach(float bearing in ClampBearings)Ring(Polar(bearing,ClampSeat),2.1f,.14f);
                for(int k=0;k<12;k++){float bearing=15+30*k;Line(Polar(bearing,FibreIn+.2f),Polar(bearing,FibreOut-.4f),.2f);}
                kit.Built("Court inlay",inlay,Bronze);
                hold.fibres=hold.fibres.Concat(running).ToArray();hold.slackFlags=hold.slackFlags.Concat(running.Select(_=>"")).ToArray();
                // ---- the body's ribs in the low light: bands of shade laid across the floor from the body's side, a beat down the nave between the colonnade's stumps and on down the
                // gallery (drawn only as shadow: the ribs a band falls from are the body's own, inside the glass).
                ShadowRibs(kit);
                // ---- the tail's end beside the great door, and the heart beside the landing
                TailEnd(kit);
                lights.Add(WarmHeart(kit));
                HollowBacks(kit);
                // ---- shafts of light through the vault: onto the body down the nave, across the first clamp, into the breach and the niche; in the hall, one onto each clamp as it opens
                var shafts=new List<Transform>();
                Transform LightShaft(string name,Vector3 top,Vector3 foot,float wide,float strength)
                {
                    // A soft band drawn by a line turned to the camera, in its own space with its object at the top, so the Bridle's letting go can swing it from there.
                    var line=new GameObject(name,typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.position=top;
                    line.useWorldSpace=false;line.alignment=LineAlignment.View;line.textureMode=LineTextureMode.Stretch;line.sharedMaterial=Shaft;line.widthMultiplier=wide;
                    line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;line.positionCount=2;line.SetPositions(new[]{Vector3.zero,foot-top});
                    var tone=new Color(1,.95f,.84f)*strength;line.startColor=line.endColor=new Color(tone.r,tone.g,tone.b,1);
                    shafts.Add(line.transform);return line.transform;
                }
                foreach(float z in new[]{30f,64f,90f,108f})LightShaft("Shaft over the body "+z,new Vector3(17,RouteLevel(z)+17,z+4),new Vector3(8.5f,RouteLevel(z)+.5f,z-1.5f),4.2f,.42f);
                LightShaft("Shaft on the first clamp",new Vector3(14,ClampFloor+17,143),new Vector3(2.5f,ClampFloor+2.6f,137.5f),3.6f,.46f);
                LightShaft("Shaft into the breach",new Vector3(16,RibFloor+16,194),new Vector3(9.5f,RibFloor+.3f,190),3.8f,.4f);
                LightShaft("Shaft on the niche",new Vector3(14,GalleryFloor+15,233),new Vector3(7.5f,GalleryFloor+.3f,230),3.2f,.4f);
                for(int i=0;i<ClampBearings.Length;i++)
                {
                    var bodyAt=Polar(ClampBearings[i],ClampBody);var from=Polar(ClampBearings[i],HallRadius+9);
                    var shaft=LightShaft("Shaft on clamp "+(i+1),new Vector3(from.x,HallFloor+18,from.y),new Vector3(bodyAt.x,HallFloor+2.4f,bodyAt.y),3.6f,.46f);
                    var opens=new GameObject("Light on clamp "+(i+1)+" (once it opens)",typeof(FlagGate)).GetComponent<FlagGate>();opens.flag="oriel.clamp"+(i+1);opens.showWhenSet=true;opens.targets=new[]{shaft.gameObject};
                }
                hold.shafts=shafts.ToArray();
                // ---- the light still inside the body: a few cool lamps in the glass beside the gallery, and the heart's
                // Brighter than they were, and more of them: the body is the room's light, and what stands near the balustrade takes its cool from it.
                foreach(float z in new[]{18f,40f,62f,84f,104f,128f,150f,168f,190f,208f,226f,244f}){var l=kit.Lamp(new Vector3(8.6f,RouteLevel(z)+1.2f-kit.Y(8.6f,z),z),12,2.3f,new Color(.6f,.84f,1));l.name="Light in the glass";lights.Add(l);}
                var heartLight=kit.Lamp(new Vector3(1,HallFloor+2-kit.Y(1,316),316),16,3.2f,new Color(.74f,.9f,1));heartLight.name="The road's knot's light";lights.Add(heartLight);
                hold.lights=lights.ToArray();
                // ---- the engine at C: a bronze ring let into the floor, a socket for each fibre at its rim; drawn only, flush, so the Warden's floor stays its own
                var engine=new ZoneKit.Shape();var c=kit.On(C.x,C.y,.02f);
                engine.Slot(0).Disc(c,Vector3.up,FibreIn+.15f,36,BronzeLight,1.5f,BronzeDark).Disc(c+Vector3.down*.005f,Vector3.up,1.5f,24,new Color(.36f,.4f,.44f));
                for(int k=0;k<12;k++){var at=Polar(15+30*k,FibreIn);engine.Slot(0).Box(kit.On(at.x,at.y,.05f),new Vector3(.34f,.1f,.5f),Quaternion.Euler(0,15+30*k,0),BronzeDark,BronzeLight);}
                kit.Built("The engine's ring",engine,Bronze);
                // ---- the engine's works round the heart, beyond the north rim: bronze arms reaching up out of the trench into the glass, gripping the heart
                var works=new ZoneKit.Shape();
                foreach(float bearing in new[]{-42f,-16f,14f,40f,66f})
                {
                    var rim=Polar(bearing,HallRadius+2.2f);var foot=new Vector3(rim.x,HallFloor-TrenchDrop-2.3f,rim.y);var core=new Vector3(1,HallFloor+1.4f,317);
                    var mid=Vector3.Lerp(foot,core,.5f)+Vector3.up*5;
                    works.Slot(0).Tube(new[]{foot,Vector3.Lerp(foot,mid,.6f),mid,Vector3.Lerp(mid,core,.6f)},new[]{.55f,.45f,.38f,.3f},8,BronzeDark,BronzeLight,1,.4f);
                    works.Slot(0).Tube(new[]{mid+Vector3.down*.4f,mid+Vector3.up*.4f},new[]{.7f,.7f},10,BronzeLight,BronzeLight,1,.5f);
                }
                // The heart in the chest over the north rim, a great still knot of the road's light, held in the works' arms: one piece with them, standing on the trench's floor.
                {
                    var core=new Vector3(1,HallFloor+1.4f,317);
                    works.Slot(1).Lump(core,new Vector3(4.2f,3.6f,3.4f),11,.22f,new Color(.58f,.78f,.92f),new Color(.24f,.42f,.6f),14,9);
                    for(int k=0;k<9;k++){var d=Quaternion.Euler(R()*60-30,k*40,0)*Vector3.forward;works.Slot(1).Tube(new[]{core,core+d*3.4f,core+d*6+Vector3.up*(R()*2-1)},new[]{.5f,.3f,.08f},6,new Color(.6f,.8f,.94f),new Color(.3f,.48f,.66f));}
                }
                var held=kit.Built("The engine's works",works,Bronze,Heart);held.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                // (No columns on the hall's rim: set into its west wall one was sunk to its capital, a disc lying on the wall's top, and the other stood over the trench beyond the
                // balustrade; on the floor they would take from the arena's round. The hall's verticals are its four clamps and the record wall.)
                // ---- the clerks' chapel: their desks in rows, a shelf of ledgers on the west wall, a bronze lectern
                var chapel=new ZoneKit.Shape();
                for(int row=0;row<3;row++)for(int k=0;k<2;k++)
                {
                    var at=new Vector3(-23.5f+k*5.6f,Door,43.5f+row*4.4f);chapel.Slot(0).Box(at+Vector3.up*.42f,new Vector3(2.4f,.84f,1),Quaternion.identity,StoneDark,StoneWarm);
                    for(int b=0;b<3;b++)chapel.Slot(1).Box(at+new Vector3(-.7f+b*.65f,.9f,0),new Vector3(.42f,.08f+.06f*b,.32f),Quaternion.Euler(0,R()*20-10,0),new Color(.5f,.36f,.26f),new Color(.82f,.74f,.6f));
                }
                for(float z=41;z<57;z+=2.4f){chapel.Slot(0).Box(new Vector3(-26.4f,Door+1.2f,z),new Vector3(.5f,2.4f,2.2f),Quaternion.identity,StoneDark);for(int s=0;s<3;s++)chapel.Slot(1).Box(new Vector3(-26.1f,Door+.5f+s*.7f,z),new Vector3(.36f,.5f,2f),Quaternion.identity,new Color(.46f,.34f,.24f),new Color(.7f,.6f,.46f));}
                var desks=kit.Built("The clerks' desks",chapel,Masonry,Bronze);
                for(int row=0;row<3;row++)for(int k=0;k<2;k++)ZoneKit.Block(desks,new Vector3(-23.5f+k*5.6f,Door+.42f,43.5f+row*4.4f),new Vector3(2.4f,.84f,1),0,"Clerk's desk");
                ZoneKit.Block(desks,new Vector3(-26.4f,Door+1.2f,49),new Vector3(.5f,2.4f,16),0,"Ledger shelf");
                // (The breach's floor keeps its Singing Glass and the ground kit's crumbs of glass. Lumps of the body's glass built here drew as long white blades across the
                // floor from the fixed camera, over the party: gone.)
            }
            /// <summary>A lamp of the court's: a stone foot, a bronze post with a collar, a cage of four bronze ribs round a globe of their glass lit cool, and its light.</summary>
            static void CourtLamp(ZoneKit kit,Vector2 at)
            {
                var shape=new ZoneKit.Shape();FloorAt(at,out float floor);var foot=new Vector3(at.x,floor,at.y);
                shape.Slot(0).Post(foot+Vector3.down*.1f,.55f,.42f,.34f,10,StoneDark,StoneWarm).Post(foot+Vector3.up*.45f,.12f,.36f,.36f,10,StoneWarm,StoneWarm);
                shape.Slot(1).Post(foot+Vector3.up*.55f,2.55f,.1f,.08f,8,BronzeDark,BronzeLight).Post(foot+Vector3.up*1.7f,.16f,.14f,.14f,8,BronzeLight,BronzeLight)
                    .Post(foot+Vector3.up*3.02f,.12f,.24f,.3f,10,BronzeLight,BronzeLight);
                var globe=foot+Vector3.up*3.5f;
                for(int k=0;k<4;k++){var r=Quaternion.Euler(0,k*90+45,0)*Vector3.forward*.36f;shape.Slot(1).Tube(new[]{globe+Vector3.down*.38f,globe+r+Vector3.down*.1f,globe+r*.9f+Vector3.up*.26f,globe+Vector3.up*.46f},new[]{.03f,.03f,.03f,.03f},4,BronzeLight,BronzeLight,1,1,false);}
                shape.Slot(1).Post(globe+Vector3.up*.44f,.18f,.08f,.02f,8,BronzeLight,BronzeLight);
                shape.Slot(2).Lump(globe,new Vector3(.3f,.36f,.3f),(int)(at.x*7+at.y),.04f,Color.white,new Color(.86f,.92f,1),10,6);
                var lamp=kit.Built("Court lamp "+Mathf.RoundToInt(at.x)+" "+Mathf.RoundToInt(at.y),shape,Masonry,Bronze,Lamp);
                ZoneKit.Block(lamp,foot+Vector3.up*1.6f,new Vector3(.6f,3.2f,.6f),0,"Lamp post");
                kit.Lamp(new Vector3(at.x,3.5f+floor-kit.Y(at.x,at.y),at.y),11,2f,new Color(.9f,.94f,1));
            }
            /// <summary>A column of the court's colonnade, set into the nave's west wall: a square foot, a fluted shaft of pale stone banded in bronze, and a flared capital that carried the
            /// vault, broken off at its head. Drawn only (the wall it stands in is the stop), and faded under the lens like any tall piece.</summary>
            static void Column(ZoneKit kit,Vector2 at,float tall,float floor=float.NaN)
            {
                var shape=new ZoneKit.Shape();if(float.IsNaN(floor))FloorAt(at+new Vector2(1.3f,0),out floor);var foot=new Vector3(at.x,floor,at.y);
                shape.Slot(0).Box(foot+Vector3.up*.4f,new Vector3(1.5f,.8f,1.5f),Quaternion.identity,StoneDark,StoneWarm);
                bool whole=tall>=4.6f;
                shape.Slot(0).Tube(new[]{foot+Vector3.up*.8f,foot+Vector3.up*(tall*.5f),foot+Vector3.up*tall},new[]{.52f,.48f,.44f},16,StoneWarm,new Color(1,.97f,.9f),1,.2f);
                if(whole)shape.Slot(0).Tube(new[]{foot+Vector3.up*tall,foot+Vector3.up*(tall+.5f),foot+Vector3.up*(tall+.62f)},new[]{.48f,.72f,.78f},16,StoneWarm,StoneDark,1,.3f);
                else
                {
                    // Broken off: a jagged head where the shaft snapped, and its pieces at its foot.
                    var rng=new System.Random((int)(at.y*7));
                    for(int k=0;k<5;k++){var a=Quaternion.Euler(0,k*72+(float)rng.NextDouble()*30,0)*Vector3.forward*.26f;shape.Slot(0).Lump(foot+Vector3.up*(tall+.06f+(float)rng.NextDouble()*.28f)+a,new Vector3(.2f,.32f,.2f),k+(int)at.y,.4f,StoneWarm,StoneDark,6,4);}
                    for(int k=0;k<4;k++){float s=.18f+(float)rng.NextDouble()*.22f;shape.Slot(0).Lump(foot+new Vector3(.9f+(float)rng.NextDouble()*1.1f,s*.4f,((float)rng.NextDouble()-.5f)*2.2f),new Vector3(s*1.3f,s*.7f,s),k*13+(int)at.y,.35f,StoneWarm,StoneDark,6,4);}
                }
                foreach(float band in whole?new[]{1.4f,tall*.62f}:new[]{1.4f})shape.Slot(1).Tube(new[]{foot+Vector3.up*band,foot+Vector3.up*(band+.18f)},new[]{.54f,.54f},16,BronzeDark,BronzeLight,1,.5f);
                var column=kit.Built("Column "+Mathf.RoundToInt(at.y),shape,Masonry,Bronze);ZoneKit.Fadeable(column);
            }
            /// <summary>A clerk's stall of the court's, standing against the nave's west wall between two of the colonnade's stumps: a stone desk with a sloped top, a bronze rail, two
            /// ledgers left on it and a third fallen at its foot.</summary>
            static void ClerkStall(ZoneKit kit,Vector2 at)
            {
                FloorAt(at,out float floor);var foot=new Vector3(at.x,floor,at.y);var shape=new ZoneKit.Shape();
                shape.Slot(0).Box(foot+Vector3.up*.42f,new Vector3(.9f,.84f,2.2f),Quaternion.identity,StoneDark,StoneWarm).Box(foot+new Vector3(-.1f,.9f,0),new Vector3(1.05f,.1f,2.36f),Quaternion.Euler(0,0,-12),StoneWarm);
                shape.Slot(1).Box(foot+new Vector3(.42f,.98f,0),new Vector3(.06f,.12f,2.3f),Quaternion.identity,BronzeDark,BronzeLight);
                foreach(var (z,turn) in new[]{(-.5f,8f),(.45f,-14f)})shape.Slot(1).Box(foot+new Vector3(-.12f,1.0f,z),new Vector3(.46f,.07f,.34f),Quaternion.Euler(0,turn,-12),new Color(.42f,.3f,.22f),new Color(.72f,.62f,.48f));
                shape.Slot(1).Box(foot+new Vector3(.75f,.04f,.7f),new Vector3(.44f,.07f,.32f),Quaternion.Euler(0,38,0),new Color(.42f,.3f,.22f),new Color(.72f,.62f,.48f));
                var stall=kit.Built("Clerk's stall "+Mathf.RoundToInt(at.y),shape,Masonry,Bronze);ZoneKit.Block(stall,foot+Vector3.up*.5f,new Vector3(1,1,2.3f),0,"Clerk's stall");
            }
            /// <summary>A capital of the colonnade lying where it fell, on its side, with a drum of its shaft beside it.</summary>
            static void FallenCapital(ZoneKit kit,Vector2 at,float yaw)
            {
                FloorAt(at,out float floor);var foot=new Vector3(at.x,floor,at.y);var turn=Quaternion.Euler(0,yaw,0);var along=turn*Vector3.right;var shape=new ZoneKit.Shape();
                var c=foot+Vector3.up*.74f;
                shape.Slot(0).Tube(new[]{c-along*.32f,c,c+along*.3f},new[]{.46f,.68f,.76f},16,StoneWarm,StoneDark,1,.3f);
                var drum=foot+Vector3.up*.46f+turn*new Vector3(-.4f,0,1.25f);shape.Slot(0).Tube(new[]{drum-along*.5f,drum+along*.5f},new[]{.46f,.46f},14,StoneWarm,StoneDark,1,.25f);
                shape.Slot(1).Tube(new[]{drum-along*.08f,drum+along*.08f},new[]{.5f,.5f},14,BronzeDark,BronzeLight,1,.5f);
                var body=kit.Built("Fallen capital "+Mathf.RoundToInt(at.y),shape,Masonry,Bronze);
                ZoneKit.Block(body,c,new Vector3(.8f,1.48f,1.5f),yaw,"Fallen capital");ZoneKit.Block(body,drum,new Vector3(1,.92f,.92f),yaw,"Fallen drum");
            }
            /// <summary>The bands of shade the body's ribs lay across the floor in the low light from the east, a beat down the nave between the colonnade's stumps (and on down
            /// the gallery, two across the hall clear of its middle). Drawn as shadow only: what casts them stands inside the glass, where the light comes from.</summary>
            static void ShadowRibs(ZoneKit kit)
            {
                var regions=new Dictionary<int,ZoneKit.Shape>();var rng=new System.Random(41);
                void Rib(float z,float x0,float floor,float rise)
                {
                    int region=Mathf.FloorToInt(z/60);if(!regions.TryGetValue(region,out var shape))regions[region]=shape=new ZoneKit.Shape();
                    float lean=((float)rng.NextDouble()-.5f)*1.6f;var path=new Vector3[6];var radii=new float[6];
                    for(int i=0;i<6;i++){float t=i/5f;path[i]=new Vector3(x0+t*16,floor+1+rise*Mathf.Sin(t*Mathf.PI*.5f),z+lean*t);radii[i]=Mathf.Lerp(.75f,.45f,t);}
                    shape.Slot(0).Tube(path,radii,6,Color.white,Color.white,1,.3f);
                }
                foreach(float z in new[]{13f,23f,33f,43f,53f,63f,73f,83f,90f,103f,112f,122f,131f,144f,159f,176f,187f,198f,218f,225f,236f,244f,256f})Rib(z,6,RouteLevel(z),14);
                foreach(float z in new[]{280f,304f})Rib(z,19,HallFloor,12);
                foreach(var (region,shape) in regions)
                {var drawn=kit.Built("The body's rib shadows "+region,shape,Masonry);var r=drawn.GetComponent<MeshRenderer>();r.shadowCastingMode=ShadowCastingMode.ShadowsOnly;r.receiveShadows=false;}
            }
            /// <summary>The tail's very end, beside the great door, where the party first sees the body: the Cantor's tail segment, its plates and its tip, cut in the body's own
            /// dense glass (a grade of a mesh the game already owns, as chapter 10's Mother of Doors was made from the Cantor's), lying along the trench's shallow floor from where the
            /// body's skin thins out toward the door.</summary>
            static void TailEnd(ZoneKit kit)
            {
                var mesh=AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/Generated/Models/CantorTail/CantorTail_clean.fbx").OfType<Mesh>().OrderByDescending(m=>m.vertexCount).FirstOrDefault();
                if(mesh==null){Debug.Log("CHOIR_TAIL_MISSING");return;}
                var v=mesh.vertices;var box=mesh.bounds;int axis=box.size.x>=box.size.y&&box.size.x>=box.size.z?0:box.size.y>=box.size.z?1:2;
                float lo=box.min[axis],hi=box.max[axis];
                // Which end is the tip: the one whose last eighth of the length is the narrower.
                float Spread(bool high){var end=v.Where(p=>high?p[axis]>hi-(hi-lo)*.12f:p[axis]<lo+(hi-lo)*.12f).ToArray();float s=0;var m=Middle(end);foreach(var p in end){var d=p-m;d[axis]=0;s+=d.magnitude;}return s/Mathf.Max(1,end.Length);}
                bool tipHigh=Spread(true)<Spread(false);
                Vector3 EndMiddle(bool high){var end=v.Where(p=>high?p[axis]>hi-(hi-lo)*.06f:p[axis]<lo+(hi-lo)*.06f).ToArray();var m=Middle(end);m[axis]=high?hi:lo;return m;}
                var tipLocal=EndMiddle(tipHigh);var rootLocal=EndMiddle(!tipHigh);
                int minor=Enumerable.Range(0,3).Where(a=>a!=axis).OrderBy(a=>box.size[a]).First();var flat=Vector3.zero;flat[minor]=1;
                // Root inside the skin where it has thinned to a man's width, the tip on the trench floor just inside the door's line.
                var spine=SpinePoint(.8f,out _,out _);
                var root=spine;var tipPlan=new Vector2(5.4f,-5.0f);var tip=new Vector3(tipPlan.x,TrenchLevel(tipPlan)+.32f,tipPlan.y);
                var d=(tip-root).normalized;float scale=Vector3.Distance(tip,root)/Vector3.Distance(tipLocal,rootLocal);
                var turn=Quaternion.FromToRotation((tipLocal-rootLocal).normalized,d);
                var up=(Vector3.up-d*Vector3.Dot(Vector3.up,d)).normalized;var now=turn*flat;now=(now-d*Vector3.Dot(now,d)).normalized;
                turn=Quaternion.AngleAxis(Vector3.SignedAngle(now,up,d),d)*turn;
                var go=new GameObject("The tail's end",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetPositionAndRotation(root-turn*(rootLocal*scale),turn);go.transform.localScale=Vector3.one*scale;
                go.GetComponent<MeshFilter>().sharedMesh=mesh;var drawn=go.GetComponent<MeshRenderer>();drawn.sharedMaterial=Core;drawn.shadowCastingMode=ShadowCastingMode.Off;go.isStatic=true;
                // Rest it on the floor it lies on: its underside a hand into the ground at its lowest.
                float under=drawn.bounds.min.y,ground=Mathf.Min(TrenchLevel(new Vector2(drawn.bounds.center.x,drawn.bounds.center.z)),TrenchLevel(tipPlan));
                go.transform.position+=Vector3.up*(ground-.45f-under);
                Debug.Log("CHOIR_TAIL length="+Vector3.Distance(tip,root).ToString("0.0")+" scale="+scale.ToString("0.00")+" bounds="+drawn.bounds);
            }
            /// <summary>The far wall of each hollow in the body (the breach, the niche): the body's dense glass rising from the trench's floor to above the hollow's floor, a stride
            /// past its edge, so a hollow is a wound in a mass of glass with a back to it, and not a ledge over the trench (whose floor, seven metres down, showed through as a grey
            /// field). Beyond the floor from the camera, never between.</summary>
            static void HollowBacks(ZoneKit kit)
            {
                var shape=new ZoneKit.Shape();var rng=new System.Random(613);float R()=>(float)rng.NextDouble();
                void Back(float x,float z0,float z1,float floor,float rise)
                {
                    for(float z=z0;z<=z1;z+=1.5f)
                    {
                        float deep=TrenchLevel(new Vector2(x,z));var foot=new Vector3(x+R()*.9f,(deep+floor+rise)*.5f,z+R()*.6f);float half=(floor+rise-deep)*.5f;
                        shape.Slot(0).Lump(foot,new Vector3(1.2f+R()*.5f,half,.9f+R()*.4f),(int)(z*10),.32f,new Color(.86f,.96f,1.04f),new Color(.6f,.78f,.92f),7,6,.25f,Quaternion.Euler(R()*10-5,R()*40-20,R()*10-5));
                    }
                }
                Back(16.3f,181.5f,197.5f,RibFloor,3.6f);Back(12.3f,224.6f,236.2f,GalleryFloor,3.2f);
                var drawn=kit.Built("The hollows' backs",shape,Rib);drawn.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
            /// <summary>The heart, beside the landing that is named for it: a dense knot deep in the milky core, the one warm light in the building, its veins running out along the
            /// body (so it reads as alive and not as a lamp). Returns its light, which drifts with the others once the Bridle lets go.</summary>
            static Light WarmHeart(ZoneKit kit)
            {
                var at=new Vector3(10.6f,GalleryFloor-1.9f,258);var shape=new ZoneKit.Shape();var rng=new System.Random(258);
                Color hot=new(1,.66f,.34f),deep=new(.86f,.36f,.22f),cool=new(.55f,.72f,.9f);
                shape.Slot(0).Lump(at,new Vector3(2.1f,1.7f,2.3f),17,.2f,hot,deep,14,9);
                shape.Slot(0).Lump(at+new Vector3(-.5f,.4f,-.3f),new Vector3(1.1f,.9f,1.2f),23,.18f,new Color(1,.86f,.6f),hot,12,8);
                for(int k=0;k<10;k++)
                {
                    var dir=Quaternion.Euler(((float)rng.NextDouble()-.5f)*50,k*36+(float)rng.NextDouble()*20,0)*Vector3.forward;dir.x=Mathf.Abs(dir.x)*.6f+.2f;dir.Normalize();
                    float run=5+(float)rng.NextDouble()*6;var bend=Vector3.up*((float)rng.NextDouble()-.5f)*2;
                    shape.Slot(0).Tube(new[]{at+dir*1.6f,at+dir*run*.5f+bend,at+dir*run+bend*1.4f},new[]{.32f,.18f,.05f},6,hot,cool,1,.4f);
                }
                var heart=kit.Built("The heart",shape,HeartWarm);heart.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                var light=kit.Lamp(new Vector3(at.x-1.8f,at.y+1.6f-kit.Y(at.x-1.8f,at.z),at.z),11,2.6f,new Color(1,.62f,.36f));light.name="The heart's warmth";
                return light;
            }
            /// <summary>The floor: the court's pale gold paving, worn along the walked middle and broken toward the walls; dust and grit drifted in the corners; glass from the body
            /// lying in shards near the balustrade, thickest by the breach; the hall's floor darker round the engine. Shade from the vault's broken ribs drifts over all of it.</summary>
            static void Ground(ZoneKit kit)
            {
                var ground=GroundKit.Open(kit.zone,GroundKit.FindFloor(),71,1.1f,false,v=>{var plan=new Vector2(v.x,v.z);return FloorAt(plan,out float level)<=.15f&&Mathf.Abs(v.y-level)<.3f;});
                float Smooth(float a,float b,float x)=>GroundKit.Smooth(a,b,x);
                bool Hall(Vector2 p)=>Vector2.Distance(p,C)<HallRadius-.3f;
                float ByTheBody(Vector2 p)=>p.y<266?Smooth(EastEdge-2.4f,EastEdge-.3f,p.x):Smooth(HallRadius-3,HallRadius-.4f,Vector2.Distance(p,C));
                foreach(var way in Ways)ground.ClearLane(way,1.4f);
                ground.Look("Canyon",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Oriel/choir-masonry.png"));
                // A court laid this floor: the terrain's own large dressed slabs in courses everywhere (no crazy paving, nothing blended across a room), worn smooth and pale down
                // the walked middle, sand drifted only into the corners and against the west walls, and the body's glass dust only along the balustrade's foot and in the hollows.
                ZoneKit.Texture("Ground/ground-choir-worn",256,(u,v)=>{float t=.88f+.05f*(ZoneKit.Cloud(u,v,4,3)-.5f)+.025f*(ZoneKit.Cloud(u,v,13,9)-.5f);return new Color(t*1.03f,t,t*.92f,1);});
                ZoneKit.Texture("Ground/ground-choir-glassdust",256,(u,v)=>
                {
                    float grain=ZoneKit.Tile(u*64,v*64,64,5),cloud=ZoneKit.Cloud(u,v,8,17);float t=.7f+.14f*(cloud-.5f)+(grain>.86f?.22f:grain<.12f?-.12f:0);
                    return new Color(t*1.02f,t,t*.96f,1);
                });
                bool Under(Vector2 p)=>InTrench(p)&&FloorAt(p,out _)>.05f;
                bool InHollow(Vector2 p){Nearest(p,out var room);return room==Room.Breach||room==Room.Niche;}
                ground.Layers(new Vector4(.07f,.3f,.3f,.1f),1.1f,new GroundKit.Layer("choir-worn",new Color(1,.98f,.93f,.55f),6),new GroundKit.Layer("sand",new Color(.8f,.76f,.68f,.85f),4),
                    new GroundKit.Layer("choir-glassdust",new Color(.98f,.97f,.95f,.8f),3));
                ground.PaintPath(0,Ways[0],1.6f,.95f,.2f,1.1f);
                ground.Paint(1,p=>Under(p)||InHollow(p)||p.y<266&&p.x>EastEdge-4?0:(1-Smooth(.4f,1.6f,ground.Edge(p)+(ground.Noise(p,3,7)-.5f)*1.1f))*(Hall(p)?.6f:1));
                ground.Paint(2,p=>Under(p)||!InHollow(p)?0:.4f+.6f*Smooth(.38f,.58f,ground.Noise(p,3,11)));
                ground.Shade(p=>Under(p)?0:Hall(p)?.08f*(1-Smooth(3,12,Vector2.Distance(p,C))):.12f*Smooth(1.6f,.3f,ground.Edge(p)));
                ground.Decals(new[]{"CracksA","CracksB"},1.6f,p=>Under(p)?0:Hall(p)?.4f:.7f,.9f,2,new Color(.5f,.45f,.4f,.85f));
                ground.Decals(new[]{"SandDrift"},3,p=>Under(p)||InHollow(p)||p.y<266&&p.x>EastEdge-4?0:1-Smooth(.8f,2.2f,ground.Edge(p)),1.6f,3f,new Color(.8f,.76f,.68f,.6f),false);
                ground.Decals(new[]{"Stain"},.8f,p=>Under(p)?0:.5f,2.2f,4,new Color(.46f,.48f,.54f,.45f),false);
                ground.Decals(new[]{"PebblesA","PebblesB"},2.5f,p=>Under(p)?0:InHollow(p)?.8f:ByTheBody(p)*.8f,.6f,1.2f,new Color(.78f,.88f,.96f));
                ground.LampPools(2.6f,new Color(.86f,.92f,1,.4f));
                // Glass from the body: crumbs of it near the balustrade and in the hollows, small pale stones of it rather than standing cards.
                ground.Rocks(4,p=>Under(p)?0:(InHollow(p)?1:ByTheBody(p))*(ground.InAFight(p)?.3f:1),.05f,.14f,new Color(.82f,.92f,1));
                ground.Rocks(1.2f,p=>Under(p)||p.y<266&&p.x>EastEdge-4?0:(1-Smooth(.5f,1.6f,ground.Edge(p)))*.6f,.08f,.22f,new Color(.84f,.78f,.68f));
                ground.Glows(1.2f,p=>Under(p)?0:(InHollow(p)?1:ByTheBody(p)*.4f),.05f,.11f,new Color(.74f,.92f,1,.7f));
                ground.MovingShade("cloud",26,new Vector2(.1f,.06f),.22f,.5f,.04f);
                ground.Finish();
            }

            /// <summary>Where LookDev stands the heroes for stills: the route as it is walked, room by room, and two wide views.</summary>
            internal static LookDev.View[] Views=>new[]
            {
                new LookDev.View("arrival",On0(Arrival)),new LookDev.View("tail",On0(new Vector2(2.4f,-2))),new LookDev.View("nave-start",On0(new Vector2(-1,22))),new LookDev.View("nave",On0(new Vector2(-1.5f,50))),new LookDev.View("nave-low",On0(new Vector2(-1.5f,84))),
                new LookDev.View("nave-sela",On0(NaveTalk)),new LookDev.View("chapel",On0(new Vector2(-19,49))),new LookDev.View("clamp",On0(ClampTalk)),new LookDev.View("clamp-near",On0(new Vector2(-4,138))),
                new LookDev.View("rib-1",On0(new Vector2(-2,160))),new LookDev.View("bay",On0(new Vector2(-5,182))),new LookDev.View("bay-north",On0(new Vector2(-5,198))),new LookDev.View("breach",On0(new Vector2(9,190))),
                new LookDev.View("gallery",On0(new Vector2(-4,222))),new LookDev.View("gallery-north",On0(new Vector2(-4,240))),new LookDev.View("niche",On0(new Vector2(1,228))),new LookDev.View("landing",On0(new Vector2(0,257))),new LookDev.View("overlook",On0(new Vector2(4.6f,96))),
                new LookDev.View("stair",On0(new Vector2(0,270))),new LookDev.View("hall",On0(new Vector2(0,282))),new LookDev.View("hall-north",On0(new Vector2(0,300))),new LookDev.View("record-wall",On0(RecordWall)),
                new LookDev.View("overview-nave",On0(new Vector2(2,70)),false,4),new LookDev.View("overview-descent",On0(new Vector2(0,200)),false,4),new LookDev.View("overview-hall",On0(new Vector2(0,290)),false,3),
            };
            static Vector3 On0(Vector2 p){FloorAt(p,out float level);return new Vector3(p.x,level,p.y);}
        }
    }
}
