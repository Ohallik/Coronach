using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEngine;
using UnityEngine.AI;

namespace Lattice.EditorTools
{
    /// <summary>Many Hands (town and hub). Map brief, coordinates and the freight hall contract: docs/maps/ManyHands.md. Everything of the zone's own is in <see cref="Hands"/>, so nothing
    /// here can collide with the hull's or the enemies' members of the same class.</summary>
    public static partial class ManyHandsBuilder
    {
        static void BuildCity()=>Hands.Build();

        internal static partial class Hands
        {
            // ---------------------------------------------------------------- the plan (x east, z north: north is the back of every view)
            internal const float XMin=-78,XMax=78,ZMin=-66,ZMax=58;
            internal const float GalleryTop=3.6f,HoldBed=-2.6f,HoldWater=-.9f,ChannelBed=-1,ChannelWater=-.42f,RimTall=.5f,RimWide=.9f,WalkHalf=1.2f,HallWall=5.5f,LowWall=1.1f,StoresRoof=4.6f,CliffTop=4.2f;
            static float Box(Vector2 p,float x0,float x1,float z0,float z1)=>Mathf.Max(Mathf.Max(x0-p.x,p.x-x1),Mathf.Max(z0-p.y,p.y-z1));
            /// <summary>1 inside a rectangle (from <paramref name="soft"/> in), 0 outside it: a wall or a terrace with a steep face.</summary>
            static float In(Vector2 p,float x0,float x1,float z0,float z1,float soft)=>1-ZoneKit.Smooth(-soft,0,Box(p,x0,x1,z0,z1));
            static Vector2 Turn(Vector2 v,float degrees){float a=degrees*Mathf.Deg2Rad,c=Mathf.Cos(a),s=Mathf.Sin(a);return new Vector2(v.x*c-v.y*s,v.x*s+v.y*c);}
            static float RoundBox(Vector2 p,Vector2 c,Vector2 half,float round)
            {
                var q=p-c;var d=new Vector2(Mathf.Abs(q.x)-half.x+round,Mathf.Abs(q.y)-half.y+round);
                return new Vector2(Mathf.Max(d.x,0),Mathf.Max(d.y,0)).magnitude+Mathf.Min(Mathf.Max(d.x,d.y),0)-round;
            }
            /// <summary>The raft bay's flooded hold: metres from its edge, negative inside.</summary>
            internal static float Hold(Vector2 p)=>RoundBox(p,new Vector2(-45,-30.5f),new Vector2(30.5f,19),4);
            /// <summary>Driftreed's three rafts, afloat in the hold: the ferry raft by the quay, the weavers' raft with Ona's bench, the mat raft.</summary>
            internal static readonly (string name,Vector2 c,Vector2 half,float round)[] Rafts={("Ferry raft",new(-26,-24),new(6.5f,5),1.5f),("Weavers' raft",new(-50,-38),new(10,6),2),("Mat raft",new(-58,-20),new(9,4.5f),2)};
            internal static float RaftLand(Vector2 p){float d=float.PositiveInfinity;foreach(var (_,c,half,round) in Rafts)d=Mathf.Min(d,RoundBox(p,c,half,round));return d;}
            /// <summary>Walks of plank between the quays and the rafts (each end on an edge): the quay walk, the weavers' walk, the mats walk, and two from the north quay and the arcade.</summary>
            internal static readonly (string name,Vector2 from,Vector2 to)[] Walks=
            {
                ("Quay walk",new(-14.5f,-24),new(-19.5f,-24)),("Weavers' walk",new(-29,-29),new(-42.5f,-32)),("Mats walk",new(-32.5f,-22.5f),new(-49,-21)),
                ("North walk",new(-58,-11.5f),new(-58,-15.5f)),("Arcade walk",new(-24,-11.5f),new(-24,-19)),
            };
            static IEnumerable<(Vector2 from,Vector2 to)> Openings()
            {
                foreach(var (_,a,b) in Walks){var d=(b-a).normalized;yield return (a+d*.5f,a-d*(RimWide+1.6f));yield return (b-d*.5f,b+d*(RimWide+1.6f));}
            }
            static readonly (Vector2 from,Vector2 to)[] OpeningList=Openings().ToArray();
            static float Opening(Vector2 p){float open=0;foreach(var (from,to) in OpeningList)open=Mathf.Max(open,1-ZoneKit.Smooth(WalkHalf,WalkHalf+.25f,ZoneKit.Segment(p,from,to)));return open;}
            static bool OnAWalk(Vector2 p){foreach(var (_,a,b) in Walks)if(ZoneKit.Segment(p,a,b)<WalkHalf+.3f)return true;return false;}
            /// <summary>The garden's channel: the raft bay's water, piped along the arcade and let out under the carrier ribs.</summary>
            static float Channel(Vector2 p)=>Box(p,30,76,19.5f,22.5f);

            /// <summary>The deck. Level plate everywhere a body walks; the lamp gallery a terrace 3.6 m up at the back with its stair up the spine's north end; the freight hall's walls tall to
            /// north and east and knee-high to south and west (the camera's side); the cold stores' roofs south of the galley; the raft bay's hold with Driftreed's rafts on its water, every
            /// edge of hold and raft a knee-high rim a body stops at, open where a walk lands; the garden's channel, kerbed.</summary>
            internal static float Height(Vector2 p)
            {
                float y=GalleryTop*In(p,-24,24,38,70,.3f);
                if(p.x>-3.4f&&p.x<3.4f&&p.y>23&&p.y<39)y=Mathf.Max(y,GalleryTop*Mathf.Clamp01((p.y-24)/14)*In(p,-3,3,18,42,.3f));
                y=Mathf.Max(y,HallWall*In(p,11,53,-17,-15.5f,.25f));
                y=Mathf.Max(y,HallWall*In(p,51,53,-54,-15.5f,.25f));
                if(!(p.x>HallDoorFrom&&p.x<HallDoorTo))y=Mathf.Max(y,LowWall*In(p,11,53,-54,-52.5f,.25f));
                y=Mathf.Max(y,LowWall*In(p,11,12.5f,-54,-15.5f,.25f));
                y=Mathf.Max(y,StoresRoof*In(p,53,90,-80,-24,.3f));
                float hold=Hold(p);
                if(hold<RimWide+.2f)
                {
                    if(hold>=0)y=Mathf.Max(y,RimTall*ZoneKit.Smooth(0,.25f,hold)*(1-ZoneKit.Smooth(RimWide-.3f,RimWide,hold))*(1-Opening(p)));
                    else
                    {
                        float raft=RaftLand(p),bank=Mathf.Lerp(0,HoldBed,ZoneKit.Smooth(0,.7f,-hold));
                        float onRaft=raft<0?RimTall*ZoneKit.Smooth(-RimWide,-RimWide+.3f,raft)*(1-Opening(p)):Mathf.Lerp(0,HoldBed,ZoneKit.Smooth(0,.7f,raft));
                        y=Mathf.Max(bank,onRaft);
                    }
                }
                float channel=Channel(p);
                if(channel<0)y=Mathf.Lerp(y,ChannelBed,ZoneKit.Smooth(0,.5f,-channel));
                else if(channel<.7f)y=Mathf.Max(y,.45f*ZoneKit.Smooth(0,.2f,channel)*(1-ZoneKit.Smooth(.45f,.7f,channel)));
                return y;
            }
            /// <summary>Which make of deck a point is on: the hauler's plate (spine, dock, yards, the hall), the liner's (arcade, galley), the station's (cabins, gallery), the carriers' (garden),
            /// the rafts, the hold's bed.</summary>
            internal enum Make{Hauler,Liner,Station,Carrier,Raft,Bed,Hall}
            internal static Make MakeAt(Vector2 p)
            {
                float hold=Hold(p);
                if(hold<0)return RaftLand(p)<.3f?Make.Raft:Make.Bed;
                if(p.y>37.5f&&p.x>-24.5f&&p.x<24.5f)return Make.Station;
                if(p.x<-27.5f&&p.y>15.5f)return Make.Station;
                if(p.x>25.5f&&p.y>16.5f)return Make.Carrier;
                if(p.x>-36.5f&&p.x<53&&p.y>-11.5f&&p.y<15.5f||p.x>=53&&p.y>-16&&p.y<16.5f)return Make.Liner;
                if(p.x>12.5f&&p.x<51&&p.y>-52.5f&&p.y<-17)return Make.Hall;
                return Make.Hauler;
            }
            /// <summary>The floor's own colour by make, paler where feet go and darker toward walls, rims and kerbs.</summary>
            static Color Tone(Vector2 p)
            {
                var make=MakeAt(p);
                if(make==Make.Bed){float silt=.8f+.2f*Mathf.PerlinNoise(p.x*.08f,p.y*.08f);return new Color(.16f*silt,.2f*silt,.19f*silt,1);}
                Color tone=make switch
                {
                    Make.Liner=>new Color(.92f,.86f,.74f),Make.Station=>new Color(.6f,.76f,.76f),Make.Carrier=>new Color(.74f,.64f,.48f),Make.Raft=>new Color(.86f,.76f,.54f),
                    Make.Hall=>new Color(.64f,.62f,.6f),_=>new Color(.74f,.7f,.62f)
                };
                // Each hull its own paint under the wear: the hauler's plate goes from rust-brown at the dock to grey up the spine.
                if(make==Make.Hauler)tone=Color.Lerp(tone,new Color(.68f,.56f,.46f),ZoneKit.Smooth(-40,-62,p.y)*.6f);
                float worn=0;foreach(var way in Ways)worn=Mathf.Max(worn,1-ZoneKit.Smooth(.9f,2.6f,ZoneKit.FromPath(p,way)));
                float mottle=.92f+.16f*Mathf.PerlinNoise(p.x*.19f+3,p.y*.19f+7);
                tone=Color.Lerp(tone,Color.Lerp(tone,Color.white,.35f),worn*.5f);
                float hold=Hold(p);if(hold>=0&&hold<RimWide+1.4f)tone=Color.Lerp(tone,new Color(.42f,.44f,.4f),(1-ZoneKit.Smooth(RimWide,RimWide+1.4f,hold))*.55f);
                if(make==Make.Raft&&RaftLand(p)>-RimWide-.3f&&Opening(p)<.5f)tone=new Color(.58f,.52f,.36f);
                return new Color(tone.r*mottle,tone.g*mottle,tone.b*mottle,1);
            }

            // ---------------------------------------------------------------- who and what stands where (plan coordinates; routes, tests, LookDev and the map brief read these)
            internal const float HallDoorFrom=28,HallDoorTo=36;
            internal static readonly Vector2 Arrival=new(-4,-60),TallowPad=new(-16,-61.5f),HullPad=new(8,-62),AbriDock=new(4,-56.4f),DoorChalk=new(11.5f,-56.6f),IloDock=new(-10,-57.6f),
                MeretDock=new(-22,-58.6f),MeretCutter=new(-29,-62),FreightHallSpawn=new(32,-59),HallDoor=new(32,-52.5f),F=new(32,-34.5f),HallTalk=new(32,-50.6f),DessikHeld=new(16.2f,-48.2f),
                Cradle=new(-10.6f,-44.6f),Gantry=new(0,-32),
                Crossing=new(0,3),Corra=new(-24,7.6f),Dov=new(-12,7.6f),Hadda=new(18,7.6f),Press=new(22.4f,11.2f),BenchAt=new(20.6f,9.2f),ArcadeCradle=new(30,11),ArcadeSpawn=new(26,4),Clock=new(10.5f,10.6f),
                Binna=new(66,11.6f),GalleyDoor=new(54.8f,6.6f),MaruWall=new(75.6f,0),
                Ruva=new(42.6f,31),Teo=new(57.6f,40),Garo=new(62,28),Dabb=new(74.6f,25.6f),Racks=new(76,48),JaskHid=new(76.9f,49.6f),JaskLook=new(73.6f,49.2f),JaskGarden=new(46.4f,47),
                Maru=new(-25,-21.4f),Ona=new(-50,-35.2f),Pim=new(-56,-18.6f),JaskFerry=new(-22.4f,-26.4f),RingsAt=new(-19,-9.6f),TideBoard=new(-58,14.4f),
                Patrol=new(-60,50),MeretCabins=new(-61.6f,51.8f),Breach=new(-62,55.6f),
                ChartTable=new(0,47.2f),ChartTalk=new(0,45.2f),Pell=new(8,47),GreatLamp=new(19.8f,53.2f),Abri=new(-11,43.6f),AbriDesk=new(-11,45.6f),IloDesk=new(-17.4f,45.6f),Ilo=new(-17.4f,43.6f),
                JaskLamps=new(-5,51.2f),StairFoot=new(0,23),StairTop=new(0,38.6f);
            /// <summary>The three gallery lamps Pell holds steady, and the scopes round the chart table they are sighted through (west, north, east).</summary>
            internal static readonly Vector2[] GalleryLamps={new(-12,56.4f),new(0,56.8f),new(12,56.4f)},Scopes={new(-2.9f,47.2f),new(0,49.5f),new(2.9f,47.2f)};
            /// <summary>Where the dinner sits (manifest 8.4): Meret, Ruva, Jask and Teo on the table's north side facing the camera, Ilo and Abri across from them.</summary>
            internal static readonly Vector2 MeretSeat=new(61.8f,.9f),RuvaSeat=new(59.4f,.9f),JaskSeat=new(64.2f,.9f),TeoSeat=new(66.6f,.9f),IloSeat=new(61,-3),AbriSeat=new(63.6f,-3);
            /// <summary>The freight hall (docs/maps/ManyHands.md, the contract): its four fixed stacks and six loose crates, each from F.</summary>
            internal static readonly Vector2[] HallStacks={new(42.5f,-29.5f),new(22,-28.5f),new(22.5f,-41),new(42,-41.5f)},HallCrates={new(24.5f,-33),new(39.5f,-36.5f),new(31,-25.5f),new(33,-44.5f),new(25.5f,-45.5f),new(44.2f,-35.6f)};
            static readonly Vector2 GalleyDoorWay=new(56,2.4f);
            /// <summary>The ways feet go: nothing is scattered on or beside these, nobody with no name stands within two metres of one, and the ground kit keeps them clear.</summary>
            internal static readonly Vector2[][] Ways=
            {
                new[]{Arrival,new Vector2(0,-55),new Vector2(0,-40),new Vector2(0,-20),Crossing,new Vector2(0,20),StairFoot,StairTop,ChartTalk},
                new[]{Arrival,new Vector2(-12,-59.6f),MeretDock},new[]{new Vector2(0,-57),new Vector2(12,-59.4f),FreightHallSpawn,new Vector2(32,-50),new Vector2(31.4f,-47.6f)},
                new[]{new Vector2(0,-45),new Vector2(-8.6f,-45)},
                new[]{new Vector2(0,-24),new Vector2(-14.5f,-24),new Vector2(-24,-24),new Vector2(-31,-23),new Vector2(-32.5f,-22.5f),new Vector2(-49,-21),Pim},new[]{new Vector2(-24,-24),new Vector2(-29,-29),new Vector2(-42.5f,-32),new Vector2(-44.75f,-32.5f),new Vector2(-44.5f,-33.6f),new Vector2(-48,-34.3f)},
                new[]{new Vector2(-24,3),new Vector2(-40,2),new Vector2(-58,-4),new Vector2(-58,-11.5f),new Vector2(-58,-15.5f)},new[]{new Vector2(-24,-2),new Vector2(-24,-11.5f),new Vector2(-24,-19)},
                new[]{new Vector2(-34,3),Crossing,new Vector2(50,3),GalleyDoorWay,new Vector2(66,3.6f)},new[]{GalleyDoorWay,new Vector2(55.6f,4.6f),new Vector2(55.6f,10.4f),new Vector2(64.8f,10.4f)},
                new[]{new Vector2(-24,3),new Vector2(-24,6)},new[]{new Vector2(-12,3),new Vector2(-12,6)},new[]{new Vector2(18,3),new Vector2(18,6)},new[]{new Vector2(29,3),new Vector2(29,8.6f)},
                new[]{new Vector2(-19,3),new Vector2(-19,-8.4f)},new[]{GalleyDoorWay,new Vector2(56.3f,-3.4f),new Vector2(58.4f,-5.2f),new Vector2(74,-5.2f)},
                new[]{new Vector2(0,20),new Vector2(16,20.4f),new Vector2(27,24.2f),new Vector2(44,24.2f),new Vector2(60,24.2f),new Vector2(73.4f,24.2f)},new[]{new Vector2(44,24.2f),new Vector2(44,52)},
                new[]{new Vector2(60,24.2f),new Vector2(60,52)},new[]{new Vector2(73.4f,24.2f),new Vector2(73.4f,47.4f)},
                new[]{new Vector2(0,19.6f),new Vector2(-16,20.4f),new Vector2(-30,20),new Vector2(-52,20),new Vector2(-52,30.6f),new Vector2(-52,40.6f),new Vector2(-52,50.2f),new Vector2(-58,50.2f)},
                new[]{new Vector2(-52,30.6f),new Vector2(-70,30.6f)},new[]{new Vector2(-52,40.6f),new Vector2(-34,40.6f)},
                new[]{new Vector2(0,40.4f),new Vector2(7,45.4f)},new[]{new Vector2(0,40.4f),new Vector2(-11,42),new Vector2(-17.4f,42)},new[]{new Vector2(7,45.4f),new Vector2(12.6f,48.6f)},
            };
            internal static float FromWays(Vector2 p){float d=float.PositiveInfinity;foreach(var way in Ways)d=Mathf.Min(d,ZoneKit.FromPath(p,way));return d;}
            /// <summary>The floor signs (manifest 5): their spots and half their widths, for what is scattered to keep off.</summary>
            internal static readonly (string text,Vector2 at,float half)[] Signs=
            {
                ("DOCK",new(-9,-63.4f),1.6f),("FREIGHT SPINE",new(-3.6f,-48),2.8f),("FREIGHT HALL",new(18,-56.6f),2.8f),("ARCADE",new(-6,-.4f),2),("GALLEY",new(57.4f,-1.6f),2),
                ("GARDEN",new(21,22.8f),2),("RAFT BAY",new(-10.4f,-27.6f),2.2f),("LAMP GALLERY",new(-6,21),2.8f),("CABINS",new(-24,23.6f),2),("CHART TABLE",new(-3.4f,43),2.6f),
            };
            static bool OnASign(Vector2 p){foreach(var (_,at,half) in Signs)if(Mathf.Abs(p.x-at.x)<half+.6f&&Mathf.Abs(p.y-at.y)<1.4f)return true;return false;}
            /// <summary>Everyone with a name, wherever they stand over the chapter: nothing is put down between the camera and any of them.</summary>
            internal static readonly Vector2[] Named={AbriDock,IloDock,MeretDock,DessikHeld,Corra,Dov,Hadda,Binna,MaruWall,Ruva,Teo,Garo,Dabb,JaskGarden,Maru,Ona,Pim,JaskFerry,Patrol,MeretCabins,
                Pell,Abri,Ilo,JaskLamps,MeretSeat,RuvaSeat,JaskSeat,TeoSeat,IloSeat,AbriSeat,GalleyDoor};
            /// <summary>Places a player stands to do something: kept clear of anything put down.</summary>
            internal static readonly Vector2[] Spots={Arrival,TallowPad,HullPad,DoorChalk,FreightHallSpawn,HallDoor,HallTalk,Cradle,BenchAt,ArcadeCradle,ArcadeSpawn,JaskLook,RingsAt,ChartTalk,
                StairFoot,StairTop,Scopes[0],Scopes[1],Scopes[2]};
            internal static bool KeepsOff(Vector2 p)
            {
                if(FromWays(p)<1.8f||OnASign(p)||OnAWalk(p))return true;
                foreach(var person in Named)if(ZoneKit.InSightOf(p,person,2.4f,4.5f,1.6f))return true;
                foreach(var spot in Spots)if(Vector2.Distance(p,spot)<2.6f)return true;
                // Nothing by the rails: a crate's top beside the ship's edge is a step off the world over the rail.
                if(p.x<XMin+2.5f||p.x>XMax-2.5f||p.y<ZMin+2.5f||p.y>ZMax-2.5f)return true;
                // Nothing put down on a walker's loop: a walker stands still for good at whatever is in its way.
                foreach(var (_,loop,_,_,_,back,_) in Errands)for(int k=0;k<loop.Length-(back?1:0);k++)if(ZoneKit.Segment(p,loop[k],loop[(k+1)%loop.Length])<2.4f)return true;
                return false;
            }

            // ---------------------------------------------------------------- the people who live here
            /// <summary>Four peoples, each in bodies of their own, at least three looks to a people (the Vael of Tallow's cabins, the Okhari growers, the Ilu of Driftreed, the Wick-folk who
            /// came about Pell's lamps), each tinted apart from the named person whose body it borrows. Who borrows whose: only those who stayed behind (Quill, Senn, Aumi, Ishen and Brekk
            /// until their chapters) and the chapter 1 bodies, never someone who is aboard.</summary>
            internal static readonly string[] Vael={"Neve","Mira","Hal","Orrin","SurvivorNatural"},Okhari={"OkhariBody","OkhariGrower","SennBody"},Ilu={"QuillBody","MaruBody","OnaBody","DovBody"},
                WickFolk={"AumiBody","IshenBody","BrekkBody"};
            /// <summary>Standing townsfolk: body, where, facing, tint, and what they are doing (the map brief's districts). Off every way by two metres, off the signs, and no two side by side
            /// in one body. Checked against the plan before anything is built (<see cref="Refuse"/>).</summary>
            internal static readonly (string body,float x,float z,float yaw,Color tint,string doing)[] Folk=
            {
                // the dock: the queue of delegates with their forms, in a line toward Abri, one of each people
                ("Orrin",14.4f,-56.4f,275,new(.86f,.82f,.92f),"a delegate with a form"),("OkhariBody",16.4f,-56.5f,272,new(.8f,.92f,.76f),"a delegate with a form"),("AumiBody",18.4f,-56.5f,270,new(.96f,.84f,.74f),"a delegate with a form"),
                ("QuillBody",20.4f,-56.6f,268,new(.84f,.9f,1.04f),"a delegate with a form"),("Mira",-16.4f,-56.2f,100,new(.92f,.8f,.84f),"a delegate with a form"),
                // a rigger at the riggers' cradle, two porters by the containers
                ("Hal",-12.6f,-40.6f,90,new(.8f,.86f,.96f),"a rigger"),("SennBody",10.4f,-26,250,new(.86f,.8f,.7f),"a porter"),("BrekkBody",-12.2f,-30,80,new(.9f,.78f,.7f),"a porter"),
                // the arcade: buyers at Corra's and Dov's, someone reading the clock, a grower selling seedlings on a bench
                ("SurvivorNatural",-27.6f,7,150,new(.76f,.86f,.84f),"a buyer at Corra's"),("OnaBody",-8.8f,7.4f,200,new(1.04f,.92f,.86f),"a buyer at Dov's"),("IshenBody",12.6f,8.4f,20,new(.86f,.84f,.98f),"reading the clock"),
                ("OkhariGrower",36.8f,8.8f,170,new(.94f,1,.84f),"selling seedlings"),("Neve",-33,6,120,new(.9f,.94f,1.04f),"at the arcade's west end"),
                // the galley: two at the south table, a cook's hand at the hatch
                ("Hal",70.4f,-7.4f,10,new(.94f,.86f,.78f),"at the south table"),("OkhariBody",61.6f,-7.4f,350,new(.86f,.8f,.7f),"at the south table"),("DovBody",71.4f,12.6f,190,new(.9f,1,.94f),"a cook's hand"),
                // the garden: growers at the beds, Marrow-2's crew
                ("OkhariGrower",40.6f,40.4f,90,new(.84f,.96f,.8f),"a grower at Marrow-1"),("SennBody",53.4f,47.6f,270,new(.9f,.86f,.74f),"a grower at Marrow-2"),("OkhariBody",62.4f,45.6f,90,new(.74f,.9f,.86f),"Marrow-3's crew"),
                ("Mira",28.4f,36,120,new(.86f,.94f,.84f),"a Vael helping at the beds"),
                // the raft bay: weavers on the mat raft and the north quay, a pilot coiling line
                ("MaruBody",-61.6f,-21.4f,30,new(.9f,.86f,1.06f),"a weaver at the racks"),("QuillBody",-46.4f,-40.2f,190,new(1.04f,.94f,.86f),"a weaver at Ona's bench"),("DovBody",-46,4.4f,200,new(.92f,.98f,1.1f),"a pilot coiling line"),
                ("OnaBody",-66,6.6f,170,new(.98f,.9f,1.04f),"reading the tide lines"),
                // the cabins: neighbours at their doors, laundry
                ("Neve",-66,28.6f,180,new(.82f,.9f,1.02f),"at a cabin door"),("SurvivorNatural",-41,38.4f,190,new(.9f,.84f,.8f),"at a cabin door"),("Orrin",-44,29,170,new(.8f,.86f,.8f),"hanging laundry"),
                ("Hal",-70,38.6f,200,new(.86f,.82f,.94f),"at a cabin door"),
                // the gallery: lamp trimmers who came about the notice
                ("AumiBody",-20,51,30,new(.84f,.9f,.96f),"a lamp trimmer"),("BrekkBody",20.6f,44.4f,200,new(.96f,.88f,.8f),"a lamp trimmer"),
            };
            /// <summary>Children of two peoples, scaled bodies of their own peoples' coats, each in a different one.</summary>
            internal static readonly (string body,float x,float z,float yaw,Color tint)[] Children={("PimBody",-45,8.6f,200,new(1.06f,.94f,.9f)),("OkhariBody",-47.2f,7.6f,160,new(.92f,1.04f,.86f))};
            /// <summary>People on errands between districts (WalkLoop): body, loop, pace, the wait at each stop, tint, there and back, name. Growers carrying trays to the galley, weavers
            /// carrying mats to the cabins, children of both peoples running the spine, a porter with a crate. Off the ways' lines where the named stand, checked as built.</summary>
            internal static readonly (string body,Vector2[] loop,float speed,float pause,Color tint,bool back,string name)[] Errands=
            {
                ("OkhariGrower",new[]{new Vector2(30.2f,26.4f),new Vector2(23.4f,21),new Vector2(9,18),new Vector2(6.6f,10.6f),new Vector2(9.4f,5.4f),new Vector2(40,5.4f),new Vector2(51.6f,5.6f)},1.1f,2.6f,new(.86f,.98f,.78f),true,"A grower carrying a tray to the galley"),
                ("SennBody",new[]{new Vector2(47.6f,27.4f),new Vector2(47.6f,49.6f)},.85f,3,new(.92f,.84f,.76f),true,"A grower carrying seedlings along Marrow-1"),
                ("OnaBody",new[]{new Vector2(-46.4f,-3.2f),new Vector2(-39,10.8f),new Vector2(-42.6f,17.4f),new Vector2(-60.4f,17.6f)},1,2.4f,new(.96f,1.02f,.9f),true,"A weaver carrying a mat to the cabins"),
                ("PimBody",new[]{new Vector2(-3.4f,-50),new Vector2(-3.4f,-14),new Vector2(3.6f,-14),new Vector2(3.6f,-50)},2.2f,.5f,new(1.06f,.96f,1),false,"A child running the spine"),
                ("OkhariBody",new[]{new Vector2(3.6f,-47.4f),new Vector2(-3.6f,-47.4f),new Vector2(-3.6f,-18),new Vector2(3.6f,-18)},2.1f,.6f,new(.9f,1.06f,.9f),false,"A child running the spine after her"),
                ("Orrin",new[]{new Vector2(-4.6f,-36),new Vector2(-4.6f,-21.6f)},.9f,3,new(.84f,.8f,.76f),true,"A porter with a crate"),
                ("Neve",new[]{new Vector2(-36,30.4f),new Vector2(-46.4f,30.4f)},.8f,3.2f,new(.94f,.88f,.98f),true,"A neighbour with laundry"),
                ("IshenBody",new[]{new Vector2(-21,46),new Vector2(-21,54.4f),new Vector2(-6,54.4f)},.8f,3.4f,new(.9f,.94f,.86f),true,"A lamp trimmer walking the gallery"),
            };

            // ---------------------------------------------------------------- checks that refuse the plan before anything is built
            static void Refuse()
            {
                var faults=new List<string>();
                void Floor(string what,Vector2 p,float level)
                {
                    float y=Height(p);bool flat=Mathf.Abs(Height(p+Vector2.right*.4f)-y)<.06f&&Mathf.Abs(Height(p+Vector2.up*.4f)-y)<.06f;
                    if(Mathf.Abs(y-level)>.12f||!flat)faults.Add($"{what} at {p} stands at {y:0.00} on uneven ground, not on the floor at {level:0.0}");
                }
                float Level(Vector2 p)=>MakeAt(p)==Make.Station&&p.y>37.5f&&p.x>-24.5f&&p.x<24.5f?GalleryTop:0;
                foreach(var p in Named)Floor("someone with a name",p,Level(p));
                foreach(var p in Spots.Where(s=>s!=StairFoot&&s!=StairTop))Floor("a place to stand",p,Level(p));
                foreach(var f in Folk){var p=new Vector2(f.x,f.z);Floor("a townsperson ("+f.doing+")",p,Level(p));if(FromWays(p)<2)faults.Add($"a townsperson ({f.doing}) at {p} is {FromWays(p):0.0} m from a way");}
                foreach(var c in Children){var p=new Vector2(c.x,c.z);Floor("a child",p,Level(p));if(FromWays(p)<2)faults.Add($"a child at {p} is {FromWays(p):0.0} m from a way");}
                for(int i=0;i<Folk.Length;i++)for(int j=i+1;j<Folk.Length;j++)
                    if(Folk[i].body==Folk[j].body&&Vector2.Distance(new(Folk[i].x,Folk[i].z),new(Folk[j].x,Folk[j].z))<3.4f)faults.Add($"two townsfolk in one body side by side at ({Folk[i].x}, {Folk[i].z})");
                foreach(var (_,at,_) in Signs)Floor("a floor sign",at,Level(at));
                foreach(var (_,loop,_,_,_,_,name) in Errands)foreach(var p in loop)Floor(name,p,Level(p));
                if(faults.Count>0)throw new InvalidOperationException("Many Hands: the plan puts something where it can't stand:\n"+string.Join("\n",faults));
            }

            // ---------------------------------------------------------------- the build
            internal static ZoneKit Kit;
            internal static void Build()
            {
                Refuse();
                Materials();
                var kit=Kit=ZoneKit.Ground(City,Height,CliffTop);
                var scene=WorldBuilder.Begin(kit.zone);
                var land=kit.Terrain("Many Hands deck",XMin,XMax,ZMin,ZMax,.5f,Plate,HullPlate,Tone,new Color(.6f,.62f,.66f),new Color(.66f,.66f,.68f));
                var hold=kit.Water("Hold water",-78,-12,-52,-10,1,HoldWater,Water,4);
                var channel=kit.Water("Channel water",28,78,18,24,.5f,ChannelWater,Water,2);
                foreach(var sheet in new[]{hold,channel}){var patch=sheet.AddComponent<BiomePatch>();patch.resourceKey="ManyHands-"+sheet.name.Replace(' ','-');patch.palette="River";patch.water=true;}
                WorldBuilder.Spawn("Arrival",kit.On(Arrival.x,Arrival.y));WorldBuilder.Spawn("FreightHall",kit.On(FreightHallSpawn.x,FreightHallSpawn.y));
                WorldBuilder.Spawn("Arcade",kit.On(ArcadeSpawn.x,ArcadeSpawn.y));
                kit.DriftingSky();

                Edges(kit);
                Space(kit);
                Dock(kit);
                Spine(kit);
                FreightHall(kit);
                Arcade(kit);
                Galley(kit);
                Garden(kit);
                RaftBay(kit);
                Cabins(kit);
                LampGallery(kit);
                Forecourts(kit);
                Detail(kit);
                People(kit);
                Story(kit);
                Arrivals.Settle(kit);
                foreach(var (text,at,_) in Signs)kit.Sign(text,at.x,at.y);

                // Every errand's loop clear of anything solid at a body's height: a walker stands still for good at whatever is in its way.
                Physics.SyncTransforms();var blocked=new List<string>();
                foreach(var (_,loop,_,_,_,back,name) in Errands)
                    for(int k=0;k<loop.Length-(back?1:0);k++)
                    {
                        var a=kit.On(loop[k].x,loop[k].y)+Vector3.up*.95f;var next=loop[(k+1)%loop.Length];var b=kit.On(next.x,next.y)+Vector3.up*.95f;var run=b-a;run.y=0;
                        foreach(var hit in Physics.SphereCastAll(a,.4f,run.normalized,run.magnitude,~0,QueryTriggerInteraction.Ignore))
                            if(hit.collider.GetComponentInParent<WalkLoop>()==null)blocked.Add($"{name}: {hit.collider.name} at ({hit.point.x:0.0}, {hit.point.z:0.0})");
                    }
                if(blocked.Count>0)throw new InvalidOperationException("Many Hands: an errand runs into something: "+string.Join("; ",blocked.Distinct()));

                Unhide();
                Bake(kit);
                Ground(kit,land);
                var motes=kit.Glow("Hands motes",false,new Color(1.2f,1.1f,.9f,1));
                kit.Drift("Dust in the light",new Vector3(0,3,-4),new Vector3(150,6,120),motes,new Color(1,.92f,.76f,.35f),360,.03f,.09f,.12f,16);
                WorldBuilder.Save(scene,kit.zone);
                Debug.Log("MANYHANDS_COORDS ManyHands arrival="+Arrival+" freight-hall-spawn="+FreightHallSpawn+" hall-door="+HallDoor+" F="+F+" hall-talk="+HallTalk+" abri-dock="+AbriDock+" ilo-dock="+IloDock
                    +" tallow-pad="+TallowPad+" hull-pad="+HullPad+" door-chalk="+DoorChalk+" cradle="+Cradle+" crossing="+Crossing+" corra="+Corra+" dov="+Dov+" hadda="+Hadda+" bench="+BenchAt
                    +" arcade-cradle="+ArcadeCradle+" binna="+Binna+" galley-door="+GalleyDoor+" ruva="+Ruva+" teo="+Teo+" garo="+Garo+" dabb="+Dabb+" jask-look="+JaskLook+" maru="+Maru+" ona="+Ona
                    +" pim="+Pim+" rings="+RingsAt+" patrol="+Patrol+" chart-table="+ChartTalk+" pell="+Pell+" abri="+Abri+" ilo="+Ilo+" scopes="+string.Join(" ",Scopes.Select(s=>s.ToString())));
                Debug.Log("MANYHANDS_CITY_OK");
            }

            /// <summary>A block made before the piece it stands for was hung beside that piece rather than under it, and a block with nothing drawn over it reads as an invisible wall:
            /// each goes under the drawn piece it was made for (ZoneKit.Block's own rule, applied once everything stands).</summary>
            static void Unhide()
            {
                foreach(var box in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
                {
                    if(box.isTrigger||box.GetComponentInParent<Membrane>()!=null||box.GetComponentInParent<Renderer>()!=null||box.GetComponentInChildren<Renderer>()!=null||box.transform.parent==null)continue;
                    var drawn=box.transform.parent.GetComponentInChildren<Renderer>();if(drawn!=null)box.transform.SetParent(drawn.transform,true);
                }
            }
            /// <summary>Must-reach: every place a player stands, by the plan. The bake refuses the zone if any has no path from the arrival.</summary>
            static void Bake(ZoneKit kit)
            {
                var points=new List<Vector2>{TallowPad+new Vector2(3.2f,0),HullPad+new Vector2(-3.2f,0),DoorChalk,FreightHallSpawn,HallTalk,F+new Vector2(0,-7.5f),Cradle+new Vector2(1.4f,0),BenchAt,
                    ArcadeCradle+new Vector2(0,-1.8f),ArcadeSpawn,JaskLook,RingsAt,ChartTalk,StairFoot,Scopes[0]+new Vector2(-1,0),Scopes[1]+new Vector2(0,1),Scopes[2]+new Vector2(1,0)};
                foreach(var p in Named)points.Add(p+new Vector2(0,-1.4f));
                foreach(var w in Ways)points.Add(w[w.Length-1]);
                // The walks' decks are built shapes: stood in for by static boxes while the bake runs (the Rootmarket and Driftreed way), and kept, since nothing here swings.
                // A seal's carving obstacle cuts the editor's own path check too (the freight hall's cargo door is shut until the frame comes through the roof): every seal is opened for
                // the bake, whose must-reach list is the ship in every state, and put back as it was.
                var seals=UnityEngine.Object.FindObjectsByType<Membrane>(FindObjectsInactive.Include,FindObjectsSortMode.None);var wasOpen=seals.Select(m=>m.Open).ToArray();var wasShown=seals.Select(m=>m.gameObject.activeSelf).ToArray();
                foreach(var seal in seals){seal.gameObject.SetActive(true);seal.SetOpen(true);}
                // Nobody with a name is a hole in the floor while they stand somewhere else (Abri's dock place cut a leg long after she'd gone up to her desk): every person is out of
                // the bake, and carves the paths only where they stand while they are there.
                var people=UnityEngine.Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).Select(n=>n.gameObject).ToArray();
                foreach(var one in UnityEngine.Object.FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(n=>n.gameObject))if(one.GetComponent<NavMeshObstacle>()==null){var hole=one.AddComponent<NavMeshObstacle>();hole.shape=NavMeshObstacleShape.Capsule;hole.radius=.4f;hole.height=1.9f;hole.center=Vector3.up*.95f;hole.carving=true;hole.carveOnlyStationary=true;}
                foreach(var one in people)one.SetActive(false);
                try
                {
                    // Baked once with the arrival alone, then every must-reach point asked of the saved data, so a refusal names every place that can't be reached and not only the first.
                    kit.BakeNavigation(new Bounds(new Vector3(0,3,-4),new Vector3(160,24,130)),Arrival,Array.Empty<Vector2>());
                    var data=UnityEngine.Object.FindFirstObjectByType<GroundNavigation>().data;var instance=NavMesh.AddNavMeshData(data);var cut=new List<string>();
                    try
                    {
                        NavMesh.SamplePosition(At(Arrival),out var start,2,NavMesh.AllAreas);
                        foreach(var p in points.Distinct())
                        {
                            var path=new NavMeshPath();
                            if(!NavMesh.SamplePosition(At(p),out var hit,2.5f,NavMesh.AllAreas)||!NavMesh.CalculatePath(start.position,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)cut.Add(p.ToString());
                        }
                    }
                    finally{instance.Remove();}
                    if(cut.Count>0)throw new InvalidOperationException("Many Hands: no path from the arrival to "+string.Join(" ",cut));
                    Debug.Log($"MANYHANDS_ROUTES_OK points={points.Distinct().Count()}");
                }
                finally{for(int i=0;i<seals.Length;i++){seals[i].SetOpen(wasOpen[i]);seals[i].gameObject.SetActive(wasShown[i]);}foreach(var one in people)one.SetActive(true);}
            }
        }
    }
}
