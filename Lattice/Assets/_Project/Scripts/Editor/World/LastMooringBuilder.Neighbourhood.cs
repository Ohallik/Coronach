using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Neighbourhood Afloat (the epilogue town, safe; brief, "NeighbourhoodAfloat"; map docs/maps/NeighbourhoodAfloat.md): seven weeks after the convoy turned back, the districts the
    /// party has known through ten chapters moored together under a new sky, each still the place it came from and joined to the next by gangways with handrails. The moorings and the
    /// travel terminal, the Halo's market with Jun's stall and pear tree docked to Many Hands' arcade, the garden up its stair, the square under a rib from Lastlight, Driftreed's low
    /// street round its raft basin, Tallow's cabins on the east terrace, Lanternwick's lantern gallery, the Crawl's sky-garden. Everyone who came is here, each where the manifest puts them
    /// (<see cref="Homecoming"/>: their own line, their counted visit, their extras); the handrail, the line and the last scene at the fruit stall; then the credits (<c>game.complete</c>).
    /// Rebuild with <c>-mooring-zones NeighbourhoodAfloat</c>, then <c>ZoneLook.Apply</c>, <c>Restage.All</c> for the scene and <c>RouteFacts.Encounters</c>.
    /// </summary>
    public static partial class LastMooringBuilder
    {
        /// <summary>The town's own fields: no bright floor (lamps and decks, not sunlit sand); Mira's counter at pitch eleven is stocked here (the chart stop lists a shop).</summary>
        static void NeighbourhoodDefinition(ZoneDef z){z.brightFloor=false;Neighbourhood.Stock();}
        /// <summary>What really stands in the town (RouteChartTests loads it and looks): Mira's counter, Tovra's anvil, the moorings' cradle.</summary>
        static void NeighbourhoodStop(ChartStop s)
        {
            s.shop=s.bench=s.repair=true;
            s.blurb="Every district you knew, moored together under a new sky: the market and the arcade, the square, the gardens, and everyone who came.";
        }
        /// <summary>A sun nobody here has seen before: low, warm, a little rose in it, and a cool violet-teal fill from a sky with two moons in it. Lamps and lanterns bloom; the decks keep
        /// their own colours under it, so each district still reads as where it came from.</summary>
        [ZoneLook.Source] static void NeighbourhoodLooks()=>ZoneLook.Register(AfloatZone,new ZoneLook.Spec{practicals=true,sun=C(1f,.86f,.72f),sunIntensity=1.3f,sunPitch=36,shadowStrength=.78f,
            sky=C(.5f,.52f,.66f),equator=C(.5f,.44f,.46f),ground=C(.22f,.19f,.2f),contrast=10,saturation=6,splitShadows=C(.44f,.46f,.6f),splitHighlights=C(.6f,.52f,.44f),splitBalance=10,
            bloomThreshold=.9f,bloomIntensity=.62f,bloomTint=C(1f,.9f,.78f),vignette=.2f});

        internal static class Neighbourhood
        {
            // ---------------------------------------------------------------- the plan (x east, z north; the fixed camera looks north-north-east)
            internal const float GardenTop=1.8f,Gap=-30,HullDepth=7,CliffTop=3.2f,BasinBed=-1.4f,BasinWater=-.68f,XMin=-64,XMax=62,ZMin=-22,ZMax=108;
            internal enum Make{Quay,Market,Arcade,Garden,Square,LowStreet,Terrace,Gallery,SkyGarden}
            /// <summary>The districts, each a hull's deck at its own make (docs/maps/NeighbourhoodAfloat.md, "Plan"). The market and the arcade are docked side by side; the garden is docked
            /// to the market's west side a stair's height up; every other district is moored across open sky.</summary>
            internal static readonly (string name,Make make,float x0,float x1,float z0,float z1,float level)[] Decks=
            {
                ("The moorings",Make.Quay,-26,26,-14,8,0),("The market",Make.Market,-24,8,14,42,0),("The arcade",Make.Arcade,8,44,14,42,0),("The garden",Make.Garden,-48,-24,14,46,GardenTop),
                ("The square",Make.Square,-18,18,50,76,0),("The low street",Make.LowStreet,-58,-24,50,78,0),("The east terrace",Make.Terrace,24,56,48,74,0),
                ("The lantern gallery",Make.Gallery,-48,-6,84,100,0),("The sky-garden",Make.SkyGarden,8,50,82,102,0),
            };
            /// <summary>Gangways across the open sky between districts, each three metres wide, from one deck's edge to the other's.</summary>
            internal static readonly (string name,Vector2 a,Vector2 b)[] Gangways=
            {
                ("Gangway to the market",new(0,8),new(0,14)),("Gangway to the arcade",new(18,8),new(18,14)),("Gangway from the market to the square",new(-6,42),new(-6,50)),
                ("Gangway from the arcade to the square",new(14,42),new(14,50)),("Gangway to the low street",new(-18,62),new(-24,62)),("Gangway to the east terrace",new(18,66),new(24,66)),
                ("Gangway to the lantern gallery",new(-10,76),new(-10,84)),("Gangway from the low street to the gallery",new(-40,78),new(-40,84)),("Gangway to the sky-garden",new(12,76),new(12,82)),
                ("Gangway from the terrace to the sky-garden",new(40,74),new(40,82)),
            };
            internal const float GangwayHalf=1.5f;
            /// <summary>The garden stair: up from the market (x -18, level 0) to the garden's edge (x -24, 1.8 m), z 27 to 30. Its south handrail is the one with the loose third bracket.</summary>
            internal const float StairX0=-24,StairX1=-18,StairZ0=27,StairZ1=30;
            /// <summary>Driftreed's basin on the low street: the rafts' water.</summary>
            internal const float BasinX0=-56.5f,BasinX1=-41,BasinZ0=52,BasinZ1=76;

            static (string name,Make make,float x0,float x1,float z0,float z1,float level)? DeckAt(Vector2 p)
            {
                (string,Make,float,float,float,float,float)? best=null;
                foreach(var d in Decks)if(p.x>=d.x0&&p.x<=d.x1&&p.y>=d.z0&&p.y<=d.z1&&(best==null||d.level>best.Value.Item7))best=d;
                return best;
            }
            internal static bool InStair(Vector2 p)=>p.x>StairX0-.01f&&p.x<StairX1&&p.y>StairZ0&&p.y<StairZ1;
            static bool InBasin(Vector2 p)=>p.x>BasinX0&&p.x<BasinX1&&p.y>BasinZ0&&p.y<BasinZ1;
            /// <summary>The ground: each district's deck at its level, the stair a ramp, the raft basin's bed under its water, and open sky (30 m down, cut away after) everywhere else.</summary>
            internal static float Height(Vector2 p)
            {
                if(InStair(p))return GardenTop*Mathf.Clamp01((StairX1-p.x)/(StairX1-StairX0));
                var deck=DeckAt(p);if(deck==null)return Gap;
                if(InBasin(p))return BasinBed;
                return deck.Value.level;
            }
            internal static Make MakeAt(Vector2 p)=>DeckAt(p)?.make??Make.Quay;
            internal static float LevelAt(Vector2 p)=>InStair(p)?Height(p):DeckAt(p)?.level??Gap;

            // ---------------------------------------------------------------- who and what stands where (routes, tests, LookDev and the map read these)
            internal static readonly Vector2 Arrival=new(0,-4),Terminal=new(12,-1.2f),ReturnLegAt=new(9.6f,-2.8f),SurveyOfferAt=new(14.6f,-2.8f),SurveyHomeAt=new(12.1f,-2.9f),
                BrynAt=new(9.8f,.4f),NavigatorAt=new(14.4f,.5f),CleatAt=new(-8,-13.3f),MooringsAt=new(-8,-11.4f),RoadPad=new(20.4f,-7),Cradle=new(-16,3.2f),CradleStand=new(-16,1.6f),
                KottAt=new(-21,-8.6f),
                JunStall=new(-17.4f,23),JunAt=new(-18.7f,23),PearsBefore=new(-16.45f,21.9f),PearsAfter=new(-15.2f,21.9f),LineAt=new(-15.85f,23),RepaintAt=new(-14.3f,23.4f),PearTree=new(-19.2f,24.6f),
                MiraAt=new(-18.8f,18.4f),MiraCounter=new(-17.6f,17.8f),ShopAt=new(-16.2f,16.2f),RailMendAt=new(-20.6f,26.1f),Forge=new(-21.4f,35.4f),Anvil=new(-19.2f,35.8f),TovraAt=new(-16.8f,33.2f),
                TeoAt=new(40.6f,25.2f),BinnaAt=new(26.6f,37.6f),HalAt=new(33.8f,35.4f),IloAt=new(39.6f,37.6f),
                RibAt=new(1,64),HaskAt=new(-1.6f,63.4f),IruAt=new(3,61),RuvaAt=new(-9.4f,57),Table=new(-12.4f,57),Board=new(7,56.6f),PimAt=new(11.6f,55.2f),
                OrrinBoard=new(-12.4f,72.2f),OrrinAt=new(-12.4f,70.4f),DraskAt=new(5,74.5f),DraskWindow=new(5,73.4f),MeretAt=new(15.2f,58.4f),
                MaruAt=new(-39.4f,64),CorraAt=new(-30,73),BrekkAt=new(-33.4f,70.4f),CorraStall=new(-30,71.4f),DovAt=new(-31.9f,56),Smokehouse=new(-31.6f,56),HarbourLamp=new(-41.4f,51.4f),
                AbriAt=new(35.8f,67.4f),OdaAt=new(48.4f,58),NeveAt=new(31,55.4f),DaceAt=new(46,51.6f),IshenAt=new(47.6f,66.6f),
                PellAt=new(-26,91.2f),GalleryLantern=new(-20,95.6f),AumiLanternAt=new(-20,93.4f),JaskAt=new(-36,92),PipAt=new(-13,91.6f),
                DorrowAt=new(24,91),TamAt=new(36,94.4f),GardenerAt=new(-32.6f,22.6f),ShootAt=new(-33.4f,21.2f),PostAt=new(-33,42.6f);
            /// <summary>The three things pinned to the message board, and where each is read from.</summary>
            internal static readonly (string what,string node,string speaker,string flag,Vector2 on,Vector2 read)[] Messages=
            {
                ("A knotted cord","IruTernsMessage","Iru","end.msgTern",new(4.8f,56.7f),new(4.8f,54.9f)),("A seed tag","RuvaSennsMessage","Ruva","end.msgSenn",new(7,56.7f),new(7,54.9f)),
                ("A page of valve drawings","PimQuillsMessage","Pim","end.msgQuill",new(9.2f,56.7f),new(9.2f,54.9f)),
            };
            /// <summary>Everyone with a name: speaker, their own line (null: their only lines are extras), where, facing, whether they have a counted visit, and their extras (node, quest).</summary>
            internal static readonly (string id,string node,Vector2 at,float yaw,bool visit,(string node,string quest)[] extras)[] People=
            {
                ("Keeper","AbriHomecoming",AbriAt,180,true,new[]{("AbriHomeGlass","RelayGlass")}),
                ("Teo","TeoHomecoming",TeoAt,250,true,new[]{("TeoHomeTags","ClaimTags")}),
                ("Maru","MaruHomecoming",MaruAt,130,true,new[]{("MaruHomeHarbourLamp","BeaconKeeper")}),
                ("Hask","HaskHomecoming",HaskAt,200,true,new[]{("HaskHomeStrays","StrayBeasts")}),
                ("Pell","PellHomecoming",PellAt,170,true,new[]{("PellHomeMothersLamp","PellsLamp"),("PellHomeOriel","LumesLantern")}),
                ("Tovra","TovraHomecoming",TovraAt,230,true,new[]{("TovraHomePikes","QuietHands")}),
                ("Oda","OdaHomecoming",OdaAt,250,true,new[]{("OdaHomeStamp","OdaBerth")}),
                ("Dorrow","DorrowHomecoming",DorrowAt,200,true,Array.Empty<(string,string)>()),
                ("Mira","MiraHomecoming",MiraAt,110,true,new[]{("MiraHomeCrystals","MiraCrystals"),("MiraHomeTable","SecondInventory")}),
                ("Ruva","RuvaPassageRules",RuvaAt,120,false,new[]{("RuvaHomePost","MooringPost"),("RuvaHomeShoot","EmptyGardens")}),
                ("Meret","MeretAccount",MeretAt,225,false,Array.Empty<(string,string)>()),
                ("Drask","DraskWatches",DraskAt,180,false,Array.Empty<(string,string)>()),
                ("Orrin","OrrinDirectorship",OrrinAt,170,false,Array.Empty<(string,string)>()),
                ("Jask","JaskNewSky",JaskAt,160,false,new[]{("JaskHomeThree","Questions"),("JaskHomeFare","StowawayFare")}),
                ("Pip","PipRota",PipAt,200,false,new[]{("PipHomeFile","PipsOath")}),
                ("Tam","TamSkyGarden",TamAt,190,false,Array.Empty<(string,string)>()),
                ("Iru",null,IruAt,160,false,new[]{("IruHomeLamps","ShrineLamps")}),
                ("Pim",null,PimAt,230,false,new[]{("PimHomeKey","FourthPump")}),
                ("Neve",null,NeveAt,150,false,new[]{("NeveHomeParcels","NeveParcel"),("NeveHomeSocks","ReturnAddress")}),
                ("Survivor",null,DaceAt,20,false,new[]{("SurvivorHomeLamps","SorrelBeacons")}),
                ("Ishen",null,IshenAt,250,false,new[]{("IshenHomeShelves","IshensRecord"),("IshenHomeMoons","MoonNames")}),
                ("Brekk",null,BrekkAt,140,false,new[]{("BrekkHomeAwning","UnswornRations"),("BrekkHomeKettle","OdrunsKitchen")}),
                ("Corra",null,CorraAt,180,false,new[]{("CorraHomeKettles","KettleTrade")}),
                ("Dov",null,DovAt,90,false,new[]{("DovHomeSmokehouse","SmokehouseOrder"),("DovHomeBell","SchoolBell")}),
                ("Binna",null,BinnaAt,180,false,new[]{("BinnaHomeBaskets","ThreeBaskets")}),
                ("Ilo",null,IloAt,190,false,new[]{("IloHomeBook","ClaimsLedger")}),
                ("Hal",null,HalAt,200,false,new[]{("HalHomeBenches","PortableBenches")}),
            };
            /// <summary>Where a player stands to speak to someone: a step and a half in front of them.</summary>
            internal static Vector2 Before(Vector2 at,float yaw,float by=1.5f){float a=yaw*Mathf.Deg2Rad;return at+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*by;}
            /// <summary>The ways feet go: nothing is put down on or beside these, and nobody with no name stands within two metres of one.</summary>
            internal static readonly Vector2[][] Ways=
            {
                new[]{Arrival,new(0,8),new(0,14),new(0,20),new(-6,30),new(-6,42),new(-6,50),new(-6,58),new(-2,66),new(-10,72),new(-10,76),new(-10,84),new(-10,86.6f),new(-24,87),new(-38,87.4f)},
                new[]{new Vector2(0,-3),ReturnLegAt,SurveyOfferAt},new[]{new Vector2(0,-6),MooringsAt},new[]{new Vector2(0,-5),new Vector2(16.6f,-6.6f)},new[]{new Vector2(0,1),new Vector2(-12,1.6f),CradleStand},
                new[]{new Vector2(0,4),new Vector2(18,6),new Vector2(18,14),new Vector2(18,24),new Vector2(30,27),new Vector2(39.6f,25.6f)},
                new[]{new Vector2(18,24),new Vector2(14,32),new Vector2(14,42),new Vector2(14,50),new Vector2(12,58),new Vector2(12,76),new Vector2(12,82),new Vector2(16,88),new Vector2(30,91.6f),new Vector2(40,90)},
                new[]{new Vector2(-6,30),new Vector2(-10.6f,24.6f),RepaintAt},new[]{new Vector2(-10.6f,24.6f),new Vector2(-14.8f,19.6f),new Vector2(-15.4f,18.6f)},
                new[]{new Vector2(-6,30),new Vector2(-12,28.6f),new Vector2(-17.4f,28.5f),new Vector2(-24,28.5f),new Vector2(-32.4f,28.5f),new Vector2(-32.4f,40)},
                new[]{new Vector2(-6,58),new Vector2(-6,62),new Vector2(-18,62),new Vector2(-24,62),new Vector2(-34,62),new Vector2(-36,64),new Vector2(-36,74),new Vector2(-40,78),new Vector2(-40,84),new Vector2(-40,87.2f)},
                new[]{new Vector2(12,62),new Vector2(14,66),new Vector2(18,66),new Vector2(24,66),new Vector2(36,62),new Vector2(40,62),new Vector2(40,74),new Vector2(40,82),new Vector2(40,90)},
                new[]{new Vector2(0,56),new Vector2(7,54.9f)},new[]{new Vector2(12.6f,54.6f),new Vector2(14.3f,55),new Vector2(13.6f,56.8f)},new[]{new Vector2(-2,66),new Vector2(5,72)},
                new[]{new Vector2(-6,58),new Vector2(-7.3f,55.8f)},new[]{new Vector2(-10,72),new Vector2(-11.6f,68.6f)},
                new[]{new Vector2(-6,30),new Vector2(-14.6f,31.4f)},new[]{new Vector2(-12,28.6f),new Vector2(-17,26.2f),RailMendAt},new[]{new Vector2(36,62),new Vector2(36,65.6f)},new[]{new Vector2(40,62),new Vector2(46.6f,57.6f)},
            };
            internal static float FromWays(Vector2 p){float d=float.PositiveInfinity;foreach(var way in Ways)d=Mathf.Min(d,ZoneKit.FromPath(p,way));return d;}
            /// <summary>The floor signs (manifest 5): text, where, half their width.</summary>
            internal static readonly (string text,Vector2 at,float half)[] Signs=
            {
                ("MOORINGS",new(-4,-8.6f),1.8f),("TRAVEL TERMINAL",new(6.6f,-6.8f),3),("THE SQUARE",new(-2.4f,53.4f),2.2f),("MESSAGE BOARD",new(3.2f,52.4f),2.8f),
                ("EAST TERRACE",new(30.4f,61.6f),2.6f),("GARDEN ARCADE",new(24,22.8f),2.8f),("LOW STREET",new(-30.4f,60),2.2f),("LANTERN GALLERY",new(-30,86.6f),3),
                ("SKY-GARDEN",new(22,85.4f),2.2f),("PITCH ELEVEN",new(-12.4f,16.6f),2.4f),("GARDEN STAIR",new(-13.2f,27.2f),2.4f),
            };
            static bool OnASign(Vector2 p){foreach(var (_,at,half) in Signs)if(Mathf.Abs(p.x-at.x)<half+.6f&&Mathf.Abs(p.y-at.y)<1.4f)return true;return false;}
            /// <summary>Places a player stands to do something: kept clear of anything put down.</summary>
            static IEnumerable<Vector2> Spots()
            {
                yield return Arrival;yield return ReturnLegAt;yield return SurveyOfferAt;yield return SurveyHomeAt;yield return MooringsAt;yield return RoadPad;yield return CradleStand;
                yield return RepaintAt;yield return RailMendAt;yield return ShopAt;yield return AumiLanternAt;yield return Before(Anvil,180,1.4f);
                foreach(var m in Messages)yield return m.read;
                foreach(var p in People)yield return Stand(p.at,p.yaw);
            }
            /// <summary>Where a player stands to speak to someone: in front of them, clear of the counter or sill some of them stand behind.</summary>
            internal static Vector2 Stand(Vector2 at,float yaw)=>Before(at,yaw,2.4f);
            static Vector2[] spots;
            internal static bool KeepsOff(Vector2 p)
            {
                spots??=Spots().ToArray();
                if(FromWays(p)<2||OnASign(p))return true;
                foreach(var person in People)if(Vector2.Distance(p,person.at)<1.6f||ZoneKit.InSightOf(p,person.at,2.4f,4.5f,1.6f))return true;
                foreach(var spot in spots)if(Vector2.Distance(p,spot)<2.4f)return true;
                foreach(var g in Gangways)if(ZoneKit.Segment(p,g.a,g.b)<GangwayHalf+2.2f)return true;
                return false;
            }

            // ---------------------------------------------------------------- townsfolk: the bodies of people who stayed behind, tinted, each doing something
            static readonly string[] Bodies={"OkhariBody","OkhariGrower","KeptHerder","SennBody","QuillBody","AumiBody","TernBody","LumeBody","OdrunBody","GaroBody","DabbBody","HaddaBody","OnaBody","VessaBody","DessikBody"};
            /// <summary>Standing townsfolk: body, where, facing, tint, build, what they carry, what they're doing. Off every way by two metres, off signs, out of the camera's line to the named.</summary>
            internal static readonly (string body,float x,float z,float yaw,Color tint,float build,string carries,string doing)[] Folk=
            {
                // the moorings: people just in off the ships with their bags, a dock hand at the cleats, two at the rail looking down at the new sky
                ("OkhariBody",3,-10.8f,190,new(.92f,1,.86f),1,"pack","looking down at the new sky"),("QuillBody",5,-10.9f,170,new(.86f,.92f,1.04f),.66f,null,"a child at the rail"),
                ("KeptHerder",14,-10.6f,200,new(1.02f,.92f,.86f),1,"coil","a dock hand at the cleats"),("SennBody",-20.4f,-2.4f,90,new(.94f,.88f,.78f),1,"crate","in off a ship with a crate"),
                ("AumiBody",8.6f,2.2f,200,new(.88f,.9f,1.02f),1,"parcel","waiting for the terminal"),("HaddaBody",-14,-7.6f,30,new(1,.94f,.86f),1,"pack","in off a ship"),
                // the market: buyers at the stalls, a porter, a neighbour of Jun's
                ("TernBody",-11.4f,35.2f,20,new(.9f,.86f,.96f),1,null,"buying at the north stalls"),("OdrunBody",-0.6f,35.4f,350,new(1.02f,.94f,.88f),1,"parcel","buying at the north stalls"),
                ("GaroBody",3.6f,22,250,new(.86f,.96f,.9f),1,"crate","a porter by the kiosk"),("LumeBody",-11.6f,19.6f,250,new(.96f,.9f,1),1,null,"waiting at pitch eleven"),
                // the arcade: shoppers at the shutters, someone resting on a bench, a grower with seedlings
                ("OkhariGrower",10.6f,36.6f,10,new(.94f,1.02f,.86f),1,null,"at the first shutter"),("DabbBody",20.6f,36.8f,350,new(.9f,.96f,1.04f),1,"parcel","at the second shutter"),
                ("OnaBody",30.2f,33.4f,150,new(1.04f,.92f,.88f),1,null,"resting by the bench"),("SennBody",24,18.2f,220,new(.92f,.98f,.86f),1,"crate","carrying seedlings"),
                ("KeptHerder",34.4f,18.6f,300,new(.86f,.9f,.98f),1,null,"looking at the islands"),
                // the garden: growers at the beds
                ("OkhariGrower",-40.6f,24,90,new(.86f,.98f,.82f),1,null,"a grower at the beds"),("OkhariBody",-40.6f,36.4f,270,new(.8f,.94f,.86f),1,null,"a grower at the beds"),
                // the square: motion nine round Ruva's table, children, someone reading the board
                ("TernBody",-12.6f,59.2f,180,new(1,.92f,.88f),1,null,"motion nine, for tonnage"),("QuillBody",-14.4f,55.2f,40,new(.88f,.96f,1.02f),1,null,"motion nine, for the tide"),
                ("KeptHerder",-14.2f,59.6f,150,new(.96f,.9f,.86f),1,null,"motion nine, minuting"),("OdrunBody",-10.8f,59.4f,200,new(.92f,.9f,.98f),1,null,"motion nine, against"),
                ("AumiBody",4.2f,58.6f,20,new(1.02f,.96f,.86f),.64f,null,"a child at the board"),("LumeBody",-14.6f,66.4f,130,new(.94f,.98f,1.04f),1,null,"reading Orrin's board"),
                ("HaddaBody",5,67.4f,200,new(.96f,.88f,.9f),1,"parcel","crossing the square"),
                // the low street: a queue at the smokehouse, weavers at the basin
                ("OnaBody",-27.6f,54.2f,270,new(.96f,1.02f,.9f),1,null,"queueing for blue eel"),("GaroBody",-27.4f,57.8f,250,new(.9f,.9f,1.04f),1,null,"queueing for blue eel"),
                ("QuillBody",-39.4f,71.6f,250,new(1.04f,.94f,.86f),1,"coil","untying at noon"),("DabbBody",-27.4f,74.4f,200,new(.92f,.86f,.96f),1,null,"at Corra's stall"),
                // the east terrace: neighbours at their doors, laundry
                ("VessaBody",51.6f,65.2f,270,new(.9f,.94f,1.02f),1,null,"at a cabin door"),("OkhariBody",28.6f,68.6f,180,new(.96f,.9f,.84f),1,"pack","home with the shopping"),
                ("DessikBody",44.4f,69.4f,170,new(.9f,.9f,.94f),1,null,"at a cabin door"),
                // the lantern gallery: Pip's small ones in a row, a lamp trimmer
                ("OkhariBody",-15.6f,93.4f,90,new(1.04f,.96f,.9f),.6f,null,"one of Pip's small ones"),("KeptHerder",-15.6f,91.8f,90,new(.94f,1.02f,.96f),.62f,null,"one of Pip's small ones"),
                ("TernBody",-15.6f,90.2f,90,new(1,.94f,1.04f),.58f,null,"one of Pip's small ones"),("AumiBody",-42.6f,95.6f,10,new(.9f,.96f,1.04f),1,null,"trimming a lamp"),
                // the sky-garden: people in the park
                ("SennBody",16.6f,96,140,new(.94f,1,.9f),1,null,"sitting out in the park"),("OdrunBody",37.4f,87.6f,230,new(1.02f,.92f,.9f),1,null,"watering the beds"),
                ("HaddaBody",34.4f,96.6f,200,new(.92f,.98f,1.04f),.64f,null,"a child in the park"),
            };
            /// <summary>Errands across the town: body, loop, wait, tint, there and back, build, what they carry, name.</summary>
            internal static readonly (string body,Vector2[] loop,float pause,Color tint,bool back,float build,string carries,string name)[] Errands=
            {
                ("GaroBody",new[]{new Vector2(-21.6f,-4.4f),new Vector2(-21.6f,4.6f)},2.4f,new(.92f,.88f,.8f),true,1,"crate","A dock hand carrying crates off the ships"),
                ("OkhariGrower",new[]{new Vector2(-2.4f,24),new Vector2(-2.4f,35)},2.6f,new(.9f,1,.86f),true,1,"pack","A grower carrying produce to the stalls"),
                ("OnaBody",new[]{new Vector2(36.4f,20.2f),new Vector2(23,20.2f)},2.8f,new(1,.92f,.96f),true,1,"parcel","A neighbour with the post along the arcade"),
                ("TernBody",new[]{new Vector2(3.2f,64),new Vector2(3.2f,72.6f)},3,new(.9f,.96f,1),true,1,null,"Someone pacing the square, thinking about motion nine"),
                ("KeptHerder",new[]{new Vector2(-36.6f,53.4f),new Vector2(-36.6f,58.6f)},2.4f,new(.88f,.92f,.86f),true,1,"coil","A weaver carrying line along the low street"),
                ("DabbBody",new[]{new Vector2(28,54),new Vector2(28,51.6f),new Vector2(42,51.6f)},2.6f,new(.94f,.9f,1),true,1,"parcel","A neighbour going round the terrace"),
                ("SennBody",new[]{new Vector2(44.2f,93.6f),new Vector2(44.2f,86)},3,new(.9f,1.02f,.92f),true,1,null,"A gardener walking the sky-garden's rail"),
            };

            // ---------------------------------------------------------------- checks that refuse the plan before anything is built
            static void Refuse()
            {
                var faults=new List<string>();
                void Floor(string what,Vector2 p)
                {
                    float y=Height(p);
                    if(y<=Gap+1){faults.Add($"{what} at {p} stands over open sky");return;}
                    bool flat=Mathf.Abs(Height(p+Vector2.right*.6f)-y)<.06f&&Mathf.Abs(Height(p+Vector2.up*.6f)-y)<.06f&&Mathf.Abs(Height(p-Vector2.right*.6f)-y)<.06f&&Mathf.Abs(Height(p-Vector2.up*.6f)-y)<.06f;
                    if(!flat)faults.Add($"{what} at {p} stands at an edge or on a slope");
                    if(InBasin(p))faults.Add($"{what} at {p} stands in the raft basin");
                }
                foreach(var p in People)Floor(p.id,p.at);
                foreach(var s in Spots())Floor("a place to stand",s);
                foreach(var f in Folk){var p=new Vector2(f.x,f.z);Floor("a townsperson ("+f.doing+")",p);if(FromWays(p)<2)faults.Add($"a townsperson ({f.doing}) at {p} is {FromWays(p):0.0} m from a way");}
                for(int i=0;i<Folk.Length;i++)for(int j=i+1;j<Folk.Length;j++)
                    if(Folk[i].body==Folk[j].body&&Vector2.Distance(new(Folk[i].x,Folk[i].z),new(Folk[j].x,Folk[j].z))<3.4f)faults.Add($"two townsfolk in one body side by side at ({Folk[i].x}, {Folk[i].z})");
                foreach(var (_,at,_) in Signs)Floor("a floor sign",at);
                foreach(var e in Errands)foreach(var p in e.loop)Floor(e.name,p);
                foreach(var way in Ways)for(int i=1;i<way.Length;i++)for(float t=0;t<=1;t+=.1f){var p=Vector2.Lerp(way[i-1],way[i],t);if(Height(p)<=Gap+1&&!OnAGangway(p))faults.Add($"a way crosses open sky at {p} with no gangway");}
                if(faults.Count>0)throw new InvalidOperationException("NeighbourhoodAfloat: the plan puts something where it can't stand:\n"+string.Join("\n",faults.Distinct()));
            }
            static bool OnAGangway(Vector2 p){foreach(var g in Gangways)if(ZoneKit.Segment(p,g.a,g.b)<GangwayHalf-.2f)return true;return false;}

            // ---------------------------------------------------------------- what it's made of
            static Material Plate,Hull,Panel,Corrugated,Wood,Cloth,Leaf,Soil,Weave,LampGlow,Brass,Chalk,Glass,Water,Rope,Dark,Liner,Boards,Pavers,Reed,Cobble,Paint,WetPaint,Ember,BlueGlow,Giant,MoonFace,RingDust,Iron,Walls;
            static void Materials()
            {
                ManyHandsBuilder.Hands.Materials();
                Plate=ManyHandsBuilder.Hands.Plate;Hull=ManyHandsBuilder.Hands.HullPlate;Panel=ManyHandsBuilder.Hands.Panel;Corrugated=ManyHandsBuilder.Hands.Corrugated;Wood=ManyHandsBuilder.Hands.Wood;
                Cloth=ManyHandsBuilder.Hands.Cloth;Leaf=ManyHandsBuilder.Hands.Leaf;Soil=ManyHandsBuilder.Hands.Soil;Weave=ManyHandsBuilder.Hands.Weave;LampGlow=ManyHandsBuilder.Hands.Lamp;
                Brass=ManyHandsBuilder.Hands.Brass;Chalk=ManyHandsBuilder.Hands.Chalk;Glass=ManyHandsBuilder.Hands.Glass;Water=ManyHandsBuilder.Hands.Water;Rope=ManyHandsBuilder.Hands.Rope;Dark=ManyHandsBuilder.Hands.Dark;
                Texture2D Ground(string name)=>AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Ground/"+name+".png");
                Material Floor(string name,string texture)=>ZoneKit.Toon("afloat-"+name,Ground(texture),m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.5f,.48f,.64f));});
                // Each district keeps the floor it was built with: Many Hands' liner tile and garden boards, Lastlight's flags, Driftreed's reed paths, Lanternwick's cobbles.
                Liner=Floor("liner","ground-hands-liner");Boards=Floor("boards","ground-hands-boards");Pavers=Floor("pavers","ground-pavers");Reed=Floor("reed","ground-wend-reedpath");Cobble=Floor("cobble","ground-cobble");
                Paint=ZoneKit.Toon("afloat-paint",null,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.6f,.56f,.6f));});
                // The town's walls in Many Hands' fitted panel, as its own material: what the camera fades (cabins, the shopfronts) turns the fade on in it, and Many Hands' own
                // panel is left as Many Hands' builder makes it.
                Walls=ZoneKit.Toon("afloat-walls",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/ManyHands/hands-panel.png"),m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.5f,.52f,.66f));});
                // The new line: wet, so it shines a little where the old one is chalky and broken.
                WetPaint=ZoneKit.Toon("afloat-wet-paint",null,m=>{m.SetColor("_BaseColor",new Color(1,.84f,.16f));m.SetColor("_EmissionColor",new Color(.42f,.32f,.02f));m.SetColor("_RimColor",new Color(1,1,.86f));m.SetFloat("_RampSteps",5);});
                Ember=ZoneKit.Toon("afloat-ember",null,m=>{m.SetColor("_BaseColor",new Color(1,.5f,.2f));m.SetColor("_EmissionColor",new Color(2.6f,.9f,.24f));});
                BlueGlow=ZoneKit.Toon("afloat-blue-lamp",null,m=>{m.SetColor("_BaseColor",new Color(.6f,.82f,1));m.SetColor("_EmissionColor",new Color(.5f,1.1f,2.2f));});
                Iron=ZoneKit.Toon("afloat-iron",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.42f,.44f,.48f));m.SetColor("_RimColor",new Color(.7f,.72f,.8f));});
                // The new sky's bodies, painted here: a giant in bands of rose and sea-green nobody has a name for, and moons the colour of old pewter.
                var bands=ZoneKit.Texture("Afloat/afloat-giant",512,(u,v)=>
                {
                    float wave=Mathf.Sin(v*Mathf.PI*14+ZoneKit.Cloud(u*2,v,6,5)*3.2f)*.5f+.5f,fine=ZoneKit.Cloud(u*3,v*6,12,9);
                    var rose=new Color(.96f,.72f,.66f);var sea=new Color(.52f,.82f,.78f);var cream=new Color(.98f,.92f,.82f);
                    var c=Color.Lerp(Color.Lerp(sea,rose,wave),cream,Mathf.Clamp01((fine-.55f)*2.2f));
                    float storm=Mathf.Clamp01(1-((u*2%1-.32f)*(u*2%1-.32f)*90+(v-.62f)*(v-.62f)*600));c=Color.Lerp(c,new Color(.86f,.5f,.46f),storm*.8f);
                    return c;
                });
                Giant=ZoneKit.Toon("afloat-giant",bands,m=>{m.SetColor("_BaseColor",Color.white);m.SetTexture("_EmissionMap",bands);m.SetColor("_EmissionColor",new Color(.46f,.42f,.44f));m.SetFloat("_ReceiveShadows",0);m.SetFloat("_RampSteps",6);m.SetColor("_RimColor",new Color(1,.86f,.8f));});
                MoonFace=ZoneKit.Toon("afloat-moon",null,m=>{ZoneKit.Tinted(m);m.SetColor("_EmissionColor",new Color(.2f,.2f,.26f));m.SetFloat("_ReceiveShadows",0);});
                RingDust=ZoneKit.Toon("afloat-ring",null,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_EmissionColor",new Color(.36f,.32f,.34f));m.SetFloat("_ReceiveShadows",0);});
            }
            /// <summary>Each make's floor: its material, its tone, and how many times a metre its texture repeats.</summary>
            static (Material material,Color tone,float texel) FloorOf(Make make)=>make switch
            {
                Make.Quay=>(Plate,new Color(.74f,.6f,.5f),1/6f),Make.Market=>(Plate,new Color(.6f,.68f,.78f),1/6f),Make.Arcade=>(Liner,new Color(1,.94f,.82f),1/4f),
                Make.Garden=>(Boards,new Color(.9f,.74f,.54f),1/4f),Make.Square=>(Pavers,new Color(.86f,.8f,.7f),1/2.6f),Make.LowStreet=>(Reed,new Color(.92f,.84f,.64f),1/4f),
                Make.Terrace=>(Plate,new Color(.8f,.78f,.7f),1/5f),Make.Gallery=>(Cobble,new Color(.84f,.74f,.62f),1/3f),_=>(Pavers,new Color(.82f,.62f,.54f),1/2.4f),
            };

            // ---------------------------------------------------------------- the build
            static ZoneKit kit;
            static readonly List<Npc> named=new();
            internal static bool Build()
            {
                Refuse();
                Materials();
                named.Clear();showsFor.Clear();made=0;
                kit=ZoneKit.Ground(AfloatZone,Height,CliffTop);
                var scene=WorldBuilder.Begin(kit.zone);
                var land=kit.Terrain("Neighbourhood decks",XMin,XMax,ZMin,ZMax,.5f,Plate,Hull,p=>FloorOf(MakeAt(p)).tone,new Color(.62f,.62f,.68f),new Color(.7f,.68f,.68f));
                Trim(land);
                var basin=kit.Water("Raft basin water",BasinX0,BasinX1,BasinZ0,BasinZ1,.5f,BasinWater,Water,3);
                var patch=basin.AddComponent<BiomePatch>();patch.resourceKey="NeighbourhoodAfloat-Raft-basin";patch.palette="River";patch.water=true;
                WorldBuilder.Spawn("Arrival",kit.On(Arrival.x,Arrival.y));
                kit.DriftingSky();

                Floors();
                Marks();
                Rails();
                Gangs();
                NewSky();
                Moorings();
                Market();
                Arcade();
                Garden();
                Square();
                LowStreet();
                Terrace();
                Gallery();
                SkyGarden();
                Convoy();
                Neighbours();
                Story();
                Townsfolk();
                Dressing();
                foreach(var (text,at,_) in Signs)kit.Sign(text,at.x,at.y+(LevelAt(at)>.5f?0:0),1.6f);

                Unhide();
                Bake();
                var motes=kit.Glow("Afloat motes",false,new Color(1.2f,1.05f,.9f,1));
                kit.Drift("Dust in the new light",new Vector3(0,3,42),new Vector3(120,6,128),motes,new Color(1,.9f,.78f,.32f),420,.03f,.09f,.12f,18);
                WorldBuilder.Save(scene,kit.zone);
                Debug.Log("AFLOAT_COORDS arrival="+Arrival+" terminal="+Terminal+" return-leg="+ReturnLegAt+" survey-offer="+SurveyOfferAt+" survey-home="+SurveyHomeAt+" moorings="+MooringsAt+" road-pad="+RoadPad
                    +" cradle="+CradleStand+" repaint="+RepaintAt+" handrail="+RailMendAt+" shop="+ShopAt+" anvil="+Anvil+" aumi-lantern="+AumiLanternAt+" "
                    +string.Join(" ",People.Select(p=>p.id+"="+p.at))+" "+string.Join(" ",Messages.Select(m=>m.flag+"="+m.read)));
                Debug.Log("AFLOAT_OK");
                return true;
            }

            /// <summary>Open sky between the districts: the landform's floor down there and its walls under a hull's depth are cut away, so a gap shows the sky below and each district's
            /// side ends like a hull's. The collider goes with them (nothing to land on down there; every edge has a rail).</summary>
            static void Trim(ZoneKit.Land land)
            {
                static void Keep(Mesh mesh,Func<Vector3,bool> keep)
                {
                    var v=mesh.vertices;
                    for(int s=0;s<mesh.subMeshCount;s++)
                    {
                        var t=mesh.GetTriangles(s);var kept=new List<int>(t.Length);
                        for(int i=0;i+2<t.Length;i+=3){var c=(v[t[i]]+v[t[i+1]]+v[t[i+2]])/3;if(keep(c)){kept.Add(t[i]);kept.Add(t[i+1]);kept.Add(t[i+2]);}}
                        mesh.SetTriangles(kept,s);
                    }
                    mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                }
                Keep(land.floorMesh,c=>c.y>BasinBed-1);
                Keep(land.wallMesh,c=>c.y>-HullDepth);
                Keep(land.collision,c=>c.y>-HullDepth-1);
                var collider=land.root.GetComponent<MeshCollider>();collider.sharedMesh=null;collider.sharedMesh=land.collision;
            }

            // ---------------------------------------------------------------- small helpers
            static Vector3 At(Vector2 p,float up=0)=>kit.On(p.x,p.y,up);
            static Vector3 At(float x,float z,float up=0)=>kit.On(x,z,up);
            static int made;
            static GameObject Made(string name,ZoneKit.Shape s,Transform parent,bool casts,params Material[] materials)
            {
                // The mesh's file is named from the piece's name with only letters, digits and spaces kept (a colon is no file name), and numbered, so two pieces never share one.
                string file=new string(name.Where(c=>char.IsLetterOrDigit(c)||c==' ').ToArray())+" "+(++made);
                var go=kit.Built(file,s,materials);go.name=name;if(parent!=null)go.transform.SetParent(parent,true);
                go.GetComponent<MeshRenderer>().shadowCastingMode=casts?ShadowCastingMode.On:ShadowCastingMode.Off;return go;
            }
            static GameObject Prop(string key,Vector2 at,float size,float yaw,Transform parent,bool solid=true,float up=0,string fallback="DeckCrate")
            {
                var go=kit.Prop(ZoneKit.Kit(key,fallback),new Vector3(at.x,up,at.y),size,yaw,solid);if(parent!=null)go.transform.SetParent(parent,true);return go;
            }
            static void Tint(GameObject go,Color tint){var t=go.GetComponent<PropTint>()??go.AddComponent<PropTint>();t.tint=tint;}
            /// <summary>The height to give a kit piece so that its longest side on the plan is <paramref name="length"/> (Lastlight's own rule for its longhouses).</summary>
            static float Tall(string key,float length)
            {
                var prefab=ZoneKit.Prefab("Environment",key);if(prefab==null)return length*.45f;
                var probe=(GameObject)PrefabUtility.InstantiatePrefab(prefab);var b=ModelGeometry.BoundsOf(probe);UnityEngine.Object.DestroyImmediate(probe);
                return length*b.size.y/Mathf.Max(.01f,Mathf.Max(b.size.x,b.size.z));
            }
            static Transform Root(string name){return new GameObject(name).transform;}
            static float Hash(float a,float b,int salt){float h=Mathf.Sin(a*127.1f+b*311.7f+salt*74.7f)*43758.5453f;return h-Mathf.Floor(h);}
            /// <summary>How tall a name must stand to read at 26 px from the fixed camera (PeopleMarks' size, so a quiet person's name is never under the type floor).</summary>
            static float Type(float pixels){var view=Find<ZoneDef>(AfloatZone).cameraProfile;return pixels/1080f*2*view.distance*Mathf.Tan(view.fov*.5f*Mathf.Deg2Rad)*10;}
            /// <summary>A lamp on a post: its head glows, and a light where the district can spare one.</summary>
            static GameObject LampPost(Vector2 at,float tall,Color pole,bool light,Transform parent,float range=9,float intensity=1.3f,Color? colour=null)
            {
                var foot=At(at);var s=new ZoneKit.Shape().Slot(0).Post(foot,tall,.08f,.06f,8,ZoneKit.Shape.Dim(pole,.7f),pole).Beam(foot+new Vector3(0,tall-.1f,0),foot+new Vector3(.55f,tall-.1f,0),.06f,.06f,pole)
                    .Slot(1).Lump(foot+new Vector3(.55f,tall-.38f,0),new Vector3(.17f,.23f,.17f),3,.04f,Color.white,Color.white,8,5);
                var go=Made("Lamp post",s,parent,true,Brass,LampGlow);ZoneKit.Block(go,foot+Vector3.up*1.1f,new Vector3(.3f,2.2f,.3f),0,"Lamp post");
                if(light){var l=kit.Lamp(new Vector3(at.x+.55f,tall-.5f,at.y),range,intensity,colour??new Color(1,.8f,.54f));l.transform.SetParent(go.transform,true);}
                return go;
            }
            /// <summary>A stall: a counter on posts, a striped awning pitched to the back, goods on the counter (Many Hands' make, which every market in the convoy copied).</summary>
            static GameObject Stall(string name,Vector2 at,float yaw,Color awning,Color edge,Color goods,Transform parent,float wide=3.6f)
            {
                var s=new ZoneKit.Shape();var foot=At(at);var t=Quaternion.Euler(0,yaw,0);Color wood=new(.82f,.66f,.46f),dark=new(.5f,.4f,.3f);float h=wide*.5f;
                s.Slot(0).Box(foot+t*new Vector3(0,.52f,0),new Vector3(wide-.2f,.94f,1),t,dark,wood).Box(foot+t*new Vector3(0,1.02f,0),new Vector3(wide+.1f,.08f,1.25f),t,wood);
                foreach(var (x,z,tall) in new[]{(-h,-.7f,2.3f),(h,-.7f,2.3f),(-h,1.1f,3f),(h,1.1f,3f)})s.Slot(0).Post(foot+t*new Vector3(x,0,z),tall,.07f,.06f,6,dark,wood);
                Vector3 A=foot+t*new Vector3(-h-.25f,3.05f,1.2f),B=foot+t*new Vector3(h+.25f,3.05f,1.2f),C=foot+t*new Vector3(h+.25f,2.2f,-1.05f),D=foot+t*new Vector3(-h-.25f,2.2f,-1.05f);
                for(int k=0;k<6;k++){float a0=k/6f,a1=(k+1)/6f;s.Slot(1).Quad(Vector3.Lerp(A,B,a0),Vector3.Lerp(A,B,a1),Vector3.Lerp(D,C,a1),Vector3.Lerp(D,C,a0),t*new Vector3(0,1,-.5f),k%2==0?awning:Color.Lerp(awning,Color.white,.55f),.8f);}
                s.Slot(1).Quad(D,C,C+Vector3.down*.34f,D+Vector3.down*.34f,t*Vector3.back,edge,.8f);
                int n=Mathf.RoundToInt(wide/.6f);for(int k=0;k<n;k++)s.Slot(2).Lump(foot+t*new Vector3(-h+.4f+k*(wide-.8f)/Mathf.Max(1,n-1),1.16f,.06f*(k%2)),new Vector3(.18f,.13f,.18f),k+(int)(at.x*7),.15f,Color.Lerp(goods,Color.white,.15f*(k%3)),ZoneKit.Shape.Dim(goods,.55f),7,4);
                var go=Made(name,s,parent,true,Wood,Cloth,Dark);ZoneKit.Block(go,foot+Vector3.up*.55f,new Vector3(wide,1.1f,1.2f),yaw,"Stall counter");return go;
            }
            /// <summary>A bench of boards on brass legs, drawn into a shape.</summary>
            static void Bench(ZoneKit.Shape s,Vector3 foot,float yaw)
            {
                var t=Quaternion.Euler(0,yaw,0);Color wood=new(.78f,.6f,.42f);
                s.Slot(0).Box(foot+t*new Vector3(0,.44f,0),new Vector3(1.9f,.07f,.48f),t,wood).Box(foot+t*new Vector3(0,.74f,.24f),new Vector3(1.9f,.32f,.05f),t,wood);
                foreach(float x in new[]{-.8f,.8f})s.Slot(1).Box(foot+t*new Vector3(x,.22f,0),new Vector3(.06f,.44f,.44f),t,new Color(.66f,.48f,.28f));
            }
            /// <summary>A planter of fronds in a box in some hull's paint.</summary>
            static void Planter(ZoneKit.Shape s,Vector3 foot,float size,Color box,System.Random rng)
            {
                s.Slot(0).Box(foot+Vector3.up*size*.3f,new Vector3(size,size*.6f,size),Quaternion.Euler(0,(float)rng.NextDouble()*20-10,0),box,ZoneKit.Shape.Dim(box,.6f));
                for(int k=0;k<5;k++)s.Slot(1).Frond(foot+new Vector3(((float)rng.NextDouble()-.5f)*size*.5f,size*.6f,((float)rng.NextDouble()-.5f)*size*.5f),
                    new Vector3((float)rng.NextDouble()-.5f,0,(float)rng.NextDouble()-.5f),size*.9f,size*.24f,size*.7f,.4f,new Color(.36f,.56f,.3f),new Color(.6f,.82f,.44f));
            }
            /// <summary>A crate stack of goods: the station's own crates, tinted, some with cloth over them.</summary>
            static void Goods(Vector2 at,float yaw,int seed,Transform parent)
            {
                var rng=new System.Random(seed);
                foreach(var (dx,dz,up,size) in new[]{(0f,0f,0f,.62f),(.7f,.1f,0f,.55f),(.3f,.05f,.6f,.5f)})
                {
                    if(rng.NextDouble()<.2f)continue;
                    var c=Prop(rng.NextDouble()<.5?"DeckCrate":"MusterCrate",at+(Vector2)(Quaternion.Euler(0,0,-yaw)*new Vector2(dx,dz)),size,yaw+(float)rng.NextDouble()*20-10,parent,true,up);
                    Tint(c,Color.Lerp(new Color(.94f,.86f,.72f),new Color(.78f,.86f,.92f),(float)rng.NextDouble()));
                }
            }

            // ---------------------------------------------------------------- floors, rails, gangways
            /// <summary>Each district's own floor laid over its deck: two-metre courses whose tone drifts a little, darker toward the rails.</summary>
            static void Floors()
            {
                var root=Root("District floors");
                foreach(var d in Decks)
                {
                    var (material,tone,texel)=FloorOf(d.make);var s=new ZoneKit.Shape();float y=d.level+.02f,inset=.22f;
                    float x0=d.x0+inset,x1=d.x1-inset,z0=d.z0+inset,z1=d.z1-inset;int nx=Mathf.CeilToInt(x1-x0),nz=Mathf.CeilToInt(z1-z0);
                    Color Tone(float x,float z)
                    {
                        float edge=Mathf.Min(Mathf.Min(x-x0,x1-x),Mathf.Min(z-z0,z1-z));float drift=.92f+.14f*Mathf.PerlinNoise(x*.11f+d.x0,z*.11f+d.z0);
                        float rim=Mathf.Lerp(.9f,1,ZoneKit.Smooth(0,2.2f,edge));var c=tone*drift*rim;c.a=1;return c;
                    }
                    // Metre courses, each its own tone of a slow drift (a step of a percent or two between neighbours), darker toward the rails.
                    for(int i=0;i<nx;i++)for(int j=0;j<nz;j++)
                    {
                        float ax=x0+(x1-x0)*i/nx,bx=x0+(x1-x0)*(i+1)/nx,az=z0+(z1-z0)*j/nz,bz=z0+(z1-z0)*(j+1)/nz;
                        // The basin is water, and the stair has its own steps: the floor stops at both.
                        var mid=new Vector2((ax+bx)*.5f,(az+bz)*.5f);if(InBasin(mid)||InStair(mid))continue;
                        s.Quad(new Vector3(ax,y,az),new Vector3(ax,y,bz),new Vector3(bx,y,bz),new Vector3(bx,y,az),Vector3.up,Tone(mid.x,mid.y),texel);
                    }
                    var floor=Made("Floor detail: "+d.name.ToLowerInvariant()+", the deck",s,root,false,material);floor.GetComponent<MeshRenderer>().receiveShadows=true;
                }
                MarketLines(root);
            }
            /// <summary>The Halo's safety lines on the market's plating, along the street: yellow, worn where the carts cross.</summary>
            static void MarketLines(Transform root)
            {
                var s=new ZoneKit.Shape();var yellow=new Color(.94f,.76f,.2f);
                void Line(Vector2 a,Vector2 b,float wide)
                {
                    var run=(b-a);float length=run.magnitude;var d=run/length;var across=new Vector2(-d.y,d.x)*wide*.5f;int n=Mathf.Max(1,Mathf.RoundToInt(length/1.2f));
                    for(int i=0;i<n;i++)
                    {
                        float t0=i/(float)n,t1=(i+1)/(float)n-.02f;var p=a+run*t0;var q=a+run*t1;float worn=Hash(p.x,p.y,3)>.86f?.55f:1;
                        s.Quad(new Vector3(p.x-across.x,.035f,p.y-across.y),new Vector3(q.x-across.x,.035f,q.y-across.y),new Vector3(q.x+across.x,.035f,q.y+across.y),new Vector3(p.x+across.x,.035f,p.y+across.y),Vector3.up,yellow*worn,.5f);
                    }
                }
                Line(new(-22,15.4f),new(6,15.4f),.14f);Line(new(-22,40.6f),new(-9,40.6f),.14f);Line(new(-3,40.6f),new(6,40.6f),.14f);
                Line(new(-11.2f,16),new(-11.2f,27),.12f);Line(new(-.8f,16),new(-.8f,26),.12f);
                // The teal runner down Many Hands' arcade, its diamonds sand-coloured: the liner's own.
                var teal=new Color(.24f,.56f,.58f);
                for(float x=8.4f;x<43.6f;x+=1)s.Quad(new Vector3(x,.03f,27.2f),new Vector3(x+1,.03f,27.2f),new Vector3(x+1,.03f,29.8f),new Vector3(x,.03f,29.8f),Vector3.up,teal,.5f);
                for(float x=9f;x<43;x+=2.2f){var c=new Vector3(x,.034f,28.5f);s.Quad(c+new Vector3(-.5f,0,0),c+new Vector3(0,0,.5f),c+new Vector3(.5f,0,0),c+new Vector3(0,0,-.5f),Vector3.up,new Color(.86f,.74f,.5f),.5f);}
                Made("Floor detail: the market's lines and the arcade's runner",s,root,false,Paint);
            }
            /// <summary>A rail along every edge a body can reach, in the make of the district it belongs to: hazard kick plates on the quays and plating, timber on the boards, brass and
            /// cream along the arcade and the gallery. Open where a gangway lands, and where a district is docked at the same level.</summary>
            static void Rails()
            {
                var root=Root("Rails");var s=new ZoneKit.Shape();var segments=new List<(Vector3 a,Vector3 b)>();
                Color RailPaint(Make m)=>m switch{Make.Quay=>new(.66f,.68f,.72f),Make.Market=>new(.6f,.66f,.72f),Make.Arcade=>new(.86f,.7f,.42f),Make.Garden=>new(.62f,.48f,.34f),Make.Square=>new(.52f,.46f,.4f),
                    Make.LowStreet=>new(.58f,.5f,.36f),Make.Terrace=>new(.7f,.72f,.74f),Make.Gallery=>new(.84f,.66f,.38f),_=>new(.36f,.36f,.4f)};
                bool Landing(Vector2 p){foreach(var g in Gangways){if(Vector2.Distance(p,g.a)<GangwayHalf+.15f||Vector2.Distance(p,g.b)<GangwayHalf+.15f)return true;}return false;}
                foreach(var d in Decks)
                {
                    var edges=new[]{(new Vector2(d.x0,d.z0),new Vector2(d.x1,d.z0),new Vector2(0,-1)),(new Vector2(d.x1,d.z0),new Vector2(d.x1,d.z1),new Vector2(1,0)),
                        (new Vector2(d.x1,d.z1),new Vector2(d.x0,d.z1),new Vector2(0,1)),(new Vector2(d.x0,d.z1),new Vector2(d.x0,d.z0),new Vector2(-1,0))};
                    foreach(var (a,b,outward) in edges)
                    {
                        float length=Vector2.Distance(a,b);var run=(b-a)/length;Vector2? start=null;Vector2 last=a;
                        for(float t=0;t<=length+.01f;t+=.5f)
                        {
                            var p=a+run*Mathf.Min(t,length);var inside=p-outward*.3f;var outside=p+outward*.7f;
                            bool need=LevelAt(outside)<d.level-.6f&&!Landing(p)&&!InStair(outside)&&!InStair(inside);
                            if(need&&start==null)start=inside;
                            if((!need||t>=length)&&start!=null){var end=need?inside:last;if(Vector2.Distance(start.Value,end)>.6f)segments.Add((new Vector3(start.Value.x,d.level,start.Value.y),new Vector3(end.x,d.level,end.y)));start=null;}
                            last=inside;
                        }
                    }
                    foreach(var (a,b) in segments.Skip(segmentsDrawn))RailShape(s,a,b,RailPaint(d.make),d.make==Make.Quay||d.make==Make.Market||d.make==Make.Terrace);
                    segmentsDrawn=segments.Count;
                }
                // The raft basin's rail, at the low street's edge, timber.
                foreach(var (a,b) in new[]{(new Vector3(BasinX1+.3f,0,BasinZ0-.3f),new Vector3(BasinX1+.3f,0,BasinZ1+.3f)),(new Vector3(BasinX0-.3f,0,BasinZ0-.3f),new Vector3(BasinX1+.3f,0,BasinZ0-.3f)),
                    (new Vector3(BasinX0-.3f,0,BasinZ1+.3f),new Vector3(BasinX1+.3f,0,BasinZ1+.3f)),(new Vector3(BasinX0-.3f,0,BasinZ0-.3f),new Vector3(BasinX0-.3f,0,BasinZ1+.3f))})
                {segments.Add((a,b));RailShape(s,a,b,new Color(.58f,.5f,.36f),false);}
                var drawn=Made("Rails",s,root,true,Iron,Paint);
                // Each rail stops a body higher than it is drawn: a 1.24 m box has a top a stride can land on, and the shared edge walk stepped up on it and off the world
                // (NoWalkableEdgeDropsIntoNothing at (8, 42) and (-36, 100)). Nobody jumps, so nobody can tell.
                foreach(var (a,b) in segments)ZoneKit.BlockAlong(drawn,a+Vector3.up*RailBlock*.5f,b+Vector3.up*RailBlock*.5f,.3f,RailBlock,"Rail");
                segmentsDrawn=0;
            }
            static int segmentsDrawn;
            /// <summary>How high a rail's block stands: above the 2 m a stride looks up from, so its top is never ground underfoot.</summary>
            const float RailBlock=2.6f;
            static void RailShape(ZoneKit.Shape s,Vector3 a,Vector3 b,Color paint,bool kick)
            {
                float length=Vector3.Distance(a,b);int posts=Mathf.Max(1,Mathf.RoundToInt(length/2.2f));var run=(b-a).normalized;var across=Vector3.Cross(Vector3.up,run);var hazard=new Color(.94f,.74f,.18f);var soot=new Color(.16f,.16f,.18f);
                for(int i=0;i<=posts;i++){var p=Vector3.Lerp(a,b,i/(float)posts);s.Slot(0).Post(p,1.12f,.05f,.04f,6,ZoneKit.Shape.Dim(paint,.75f),paint);}
                s.Slot(0).Beam(a+Vector3.up*1.1f,b+Vector3.up*1.1f,.08f,.07f,paint).Beam(a+Vector3.up*.6f,b+Vector3.up*.6f,.05f,.05f,ZoneKit.Shape.Dim(paint,.85f));
                if(!kick)return;
                int stripes=Mathf.Max(1,Mathf.RoundToInt(length/.5f));
                for(int i=0;i<stripes;i++)
                {
                    var p=a+run*(length*i/stripes);var q=a+run*(length*(i+1)/stripes);var tone=i%2==0?hazard:soot;
                    s.Slot(1).Quad(p-across*.04f,q-across*.04f,q-across*.04f+Vector3.up*.18f,p-across*.04f+Vector3.up*.18f,-across,tone,.5f).Quad(p+across*.04f,q+across*.04f,q+across*.04f+Vector3.up*.18f,p+across*.04f+Vector3.up*.18f,across,tone,.5f);
                }
            }
            /// <summary>Each gangway: boards across two stringers, a handrail each side on posts, the deck it lands on a little under its ends. A static box under the boards is what the
            /// bake and a body walk on; its sides are blocked a rail's height.</summary>
            static void Gangs()
            {
                var root=Root("Gangways");var rng=new System.Random(88);
                foreach(var (name,a2,b2) in Gangways)
                {
                    var s=new ZoneKit.Shape();var a=new Vector3(a2.x,0,a2.y);var b=new Vector3(b2.x,0,b2.y);var d=(b-a).normalized;var across=Vector3.Cross(Vector3.up,d);
                    var A=a-d*.5f;var B=b+d*.5f;Color plank=new(.74f,.6f,.44f),iron=new(.4f,.42f,.46f),rail=new(.86f,.7f,.42f);
                    s.Slot(0).Deck(A+Vector3.up*.06f,B+Vector3.up*.06f,GangwayHalf*2,.1f,plank,rng,.5f,.05f);
                    foreach(float side in new[]{-1f,1f})
                    {
                        s.Slot(1).Beam(A+across*side*(GangwayHalf-.15f)+Vector3.down*.25f,B+across*side*(GangwayHalf-.15f)+Vector3.down*.25f,.22f,.36f,iron);
                        int posts=Mathf.Max(2,Mathf.RoundToInt(Vector3.Distance(a,b)/1.6f));
                        for(int i=0;i<=posts;i++){var p=Vector3.Lerp(a,b,i/(float)posts)+across*side*GangwayHalf;s.Slot(1).Post(p,1.12f,.05f,.04f,6,iron,iron);}
                        s.Slot(2).Beam(a+across*side*GangwayHalf+Vector3.up*1.1f,b+across*side*GangwayHalf+Vector3.up*1.1f,.09f,.07f,rail).Beam(a+across*side*GangwayHalf+Vector3.up*.6f,b+across*side*GangwayHalf+Vector3.up*.6f,.05f,.05f,ZoneKit.Shape.Dim(rail,.8f));
                    }
                    // Hangers: the gangway's lines up to a post at each end, as a hauler's brow is slung.
                    foreach(var end in new[]{a,b})foreach(float side in new[]{-1f,1f}){var top=end+across*side*(GangwayHalf+.1f)+Vector3.up*2.6f;s.Slot(1).Post(end+across*side*(GangwayHalf+.1f),2.6f,.07f,.06f,6,iron,iron).Slot(3).Rope(top,Vector3.Lerp(a,b,.5f)+across*side*GangwayHalf+Vector3.up*1.1f,.3f,.02f,new Color(.7f,.62f,.48f));}
                    var go=Made(name,s,root,true,Wood,Iron,Brass,Rope);
                    var mid=(A+B)*.5f;float yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg;
                    ZoneKit.Block(go,mid+Vector3.down*.14f,new Vector3(GangwayHalf*2+.2f,.3f,Vector3.Distance(A,B)),yaw,"Gangway boards");
                    // The side rails run on over each deck's edge to meet its rail, and a short rail along the edge either side closes the corner: a gap of a hand at a gangway's
                    // foot was a way off the world between the two.
                    foreach(float side in new[]{-1f,1f})ZoneKit.BlockAlong(go,a-d*.4f+across*side*(GangwayHalf+.1f)+Vector3.up*RailBlock*.5f,b+d*.4f+across*side*(GangwayHalf+.1f)+Vector3.up*RailBlock*.5f,.24f,RailBlock,"Gangway rail");
                    var corners=new ZoneKit.Shape();
                    foreach(var (end,inward) in new[]{(a,-d),(b,d)})foreach(float side in new[]{-1f,1f})
                    {
                        var from=end+inward*.3f+across*side*(GangwayHalf-.05f);var to=end+inward*.3f+across*side*(GangwayHalf+1.2f);
                        RailShape(corners,from,to,new Color(.4f,.42f,.46f),false);ZoneKit.BlockAlong(go,from+Vector3.up*RailBlock*.5f,to+Vector3.up*RailBlock*.5f,.3f,RailBlock,"Gangway corner rail");
                    }
                    Made(name+", its corners",corners,go.transform,true,Iron,Paint);
                }
            }

            // ---------------------------------------------------------------- the new sky
            static Transform inSpace;
            /// <summary>What the gaps show: a ringed giant in bands nobody has named, far below and to the north-east, and two moons (Ishen would like them left unnamed a while). The
            /// panorama's own stars are the zone's sky; these are drawn, unlit by the sun and lit by themselves.</summary>
            static void NewSky()
            {
                var root=Root("The new sky");
                var giant=new ZoneKit.Shape();var at=new Vector3(150,-430,520);const float R=170;
                giant.Slot(0).Lump(at,Vector3.one*R,7,.002f,Color.white,new Color(.72f,.62f,.7f),96,48,1/(R*3),Quaternion.Euler(0,0,96));
                // No ring: drawn edge-on from the town it was a flat pale band across every gap.
                var giantGo=Made("The giant",giant,root,false,Giant);giantGo.GetComponent<MeshRenderer>().receiveShadows=false;
                // What hangs in space round the town (ships at their lines, the convoy's hulls) hangs under the sky's one great drawn thing, as Many Hands' traffic does, so nothing takes
                // a ship moored below the quay for a prop standing on a deck.
                inSpace=giantGo.transform;
                foreach(var (centre,radius,tone) in new[]{(new Vector3(-240,-260,360),26f,new Color(.82f,.8f,.9f)),(new Vector3(60,-150,250),11f,new Color(.92f,.82f,.76f))})
                {
                    var m=new ZoneKit.Shape().Lump(centre,Vector3.one*radius,(int)radius,.04f,tone,ZoneKit.Shape.Dim(tone,.45f),40,20);
                    Made("A moon nobody has named",m,root,false,MoonFace).GetComponent<MeshRenderer>().receiveShadows=false;
                }
            }

            // ---------------------------------------------------------------- the moorings: the quay, forty cleats, the terminal, the ships tied up below
            static void Moorings()
            {
                var root=Root("The moorings");var s=new ZoneKit.Shape();Color iron=new(.3f,.3f,.34f),brass=new(.86f,.68f,.4f);
                // Forty cleats round the quay's edge: low iron horns on a plate, a line on some.
                var cleats=new List<Vector2>();
                for(float x=-24;x<=24.1f;x+=2.4f)cleats.Add(new Vector2(x,-13.35f));
                for(float z=-10.6f;z<=5.4f;z+=2.6f){cleats.Add(new Vector2(-25.35f,z));cleats.Add(new Vector2(25.35f,z));}
                foreach(var x in new[]{-24f,-20.4f,-16.8f,-13.2f,-9.6f,7.4f,10.8f,14.2f,22f}){if(cleats.Count<40)cleats.Add(new Vector2(x,7.35f));}
                foreach(var c in cleats)
                {
                    var on=At(c);bool along=Mathf.Abs(c.y)>13||Mathf.Abs(c.y-7.35f)<.1f;var t=Quaternion.Euler(0,along?0:90,0);
                    s.Slot(0).Box(on+Vector3.up*.05f,new Vector3(.7f,.1f,.34f),t,iron).Box(on+Vector3.up*.2f,new Vector3(.16f,.26f,.16f),t,iron).Beam(on+t*new Vector3(-.34f,.34f,0),on+t*new Vector3(.34f,.34f,0),.12f,.1f,ZoneKit.Shape.Dim(iron,1.3f));
                }
                Made("Forty cleats",s,root,true,Iron);
                // Idra's cutter and Sela's ship on the one cleat, hanging below the quay on their lines.
                foreach(var (key,at,yaw,lineTo) in new[]{("IdraFlight",new Vector3(-10.4f,-2.2f,-19.6f),82f,new Vector3(-10,-1.2f,-16.8f)),("SelaFlight",new Vector3(-5.2f,-1.8f,-18.8f),100f,new Vector3(-5.6f,-.9f,-16.4f))})
                {
                    var prefab=ZoneKit.Prefab("Characters",key);if(prefab==null)continue;
                    var ship=(GameObject)PrefabUtility.InstantiatePrefab(prefab);ship.name=key=="IdraFlight"?"Idra's cutter, tied up":"Sela's ship, tied up";ship.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));
                    ship.transform.localScale=Vector3.one*1.6f;foreach(var b in ship.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;foreach(var c in ship.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
                    ship.transform.SetParent(inSpace,true);
                    var line=new ZoneKit.Shape().Rope(At(CleatAt,.34f),lineTo,.5f,.03f,new Color(.82f,.72f,.52f));Made("A mooring line",line,inSpace,false,Rope);
                }
                // The other ships moored below the quay's south face: a hauler, a skiff, a patrol cutter, each its own make.
                foreach(var (key,at,size,yaw,tint) in new[]{("Hauler",new Vector3(16,-7,-30),14f,95f,new Color(.86f,.76f,.62f)),("Skiff",new Vector3(4.6f,-3.4f,-21),5f,80f,new Color(.84f,.92f,1)),
                    ("PatrolCutter",new Vector3(-22,-4.2f,-23),7f,105f,new Color(.94f,.88f,.8f)),("CompactCutter",new Vector3(28,-4,-12),6f,170f,new Color(.96f,.96f,.94f))})
                {if(!ZoneKit.Landed(key))continue;var ship=kit.Loose(key,at,Tall(key,size),Quaternion.Euler(0,yaw,0),tint,false);ship.name="Moored below: "+key;ship.transform.SetParent(inSpace,true);}
                // The travel terminal: Sela's route chart on a lit table, the nursery's road drawn on it, Bryn and the navigator with the pencil beside it.
                var term=new ZoneKit.Shape();var foot=At(Terminal);Color desk=new(.36f,.4f,.46f),chart=new(.94f,.9f,.78f);
                term.Slot(0).Box(foot+Vector3.up*.45f,new Vector3(5.2f,.9f,1.5f),Quaternion.identity,desk,ZoneKit.Shape.Dim(desk,1.2f)).Slot(1).Box(foot+new Vector3(0,.93f,0),new Vector3(4.9f,.04f,1.3f),Quaternion.Euler(-6,0,0),chart);
                for(int k=0;k<5;k++)term.Slot(2).Beam(foot+new Vector3(-2.2f+k*1.1f,.97f,-.4f),foot+new Vector3(-1.6f+k*1.1f,.97f,.3f+.1f*(k%2)),.03f,.01f,new Color(.3f,.5f,.62f));
                term.Slot(0).Post(foot+new Vector3(-2.4f,0,.6f),2.4f,.06f,.05f,6,desk,desk).Post(foot+new Vector3(2.4f,0,.6f),2.4f,.06f,.05f,6,desk,desk).Beam(foot+new Vector3(-2.4f,2.4f,.6f),foot+new Vector3(2.4f,2.4f,.6f),.08f,.08f,desk);
                var terminal=Made("The travel terminal",term,root,true,Panel,Cloth,Dark);ZoneKit.Block(terminal,foot+Vector3.up*.5f,new Vector3(5.2f,1,1.5f),0,"Terminal");
                kit.Lamp(new Vector3(Terminal.x,2.2f,Terminal.y+.4f),7,1.1f,new Color(.8f,.9f,1));
                if(ZoneKit.Landed("DressKiosk")){var office=Prop("DressKiosk",new Vector2(17.8f,2.6f),2.6f,200,root);office.name="The harbour office";}
                var ring=new ZoneKit.Shape();var rf=new Vector3(8.4f,.05f,-8.6f);ring.Slot(0).Disc(rf,Vector3.up,3,32,new Color(.94f,.74f,.2f),2.7f).Disc(rf,Vector3.up,.5f,16,new Color(.94f,.74f,.2f));
                for(int k=0;k<8;k++){float a=k*Mathf.PI/4;ring.Slot(0).Quad(rf+new Vector3(Mathf.Cos(a)*1,0,Mathf.Sin(a)*1),rf+new Vector3(Mathf.Cos(a+.2f)*1,0,Mathf.Sin(a+.2f)*1),rf+new Vector3(Mathf.Cos(a+.2f)*2.4f,0,Mathf.Sin(a+.2f)*2.4f),rf+new Vector3(Mathf.Cos(a)*2.4f,0,Mathf.Sin(a)*2.4f),Vector3.up,new Color(.16f,.16f,.18f),.5f);}
                Made("Floor detail: a landing ring painted on the quay",ring,root,false,Paint);
                var bryn=Body("BrynBody",BrynAt,190,"Bryn, at the travel terminal",root);var nav=Body("SurvivorNatural",NavigatorAt,175,"The navigator with the pencil",root);Tint(nav,new Color(.86f,.92f,.98f));
                // The nursery's road: a pad at the quay's east end, ringed in the young's colour, from the survey's offer on.
                var pad=new ZoneKit.Shape();var pf=At(RoadPad);
                pad.Slot(0).Disc(pf+Vector3.up*.06f,Vector3.up,3.1f,28,new Color(.42f,.44f,.48f),0,new Color(.34f,.36f,.4f),.5f).Slot(1).Disc(pf+Vector3.up*.065f,Vector3.up,2.8f,32,new Color(.52f,.86f,.84f),2.5f);
                for(int k=0;k<4;k++){float a=k*Mathf.PI*.5f+.785f;var q=pf+new Vector3(Mathf.Cos(a)*3.3f,0,Mathf.Sin(a)*3.3f);pad.Slot(0).Post(q,.7f,.09f,.07f,6,new Color(.2f,.2f,.22f),new Color(.5f,.52f,.56f)).Slot(2).Lump(q+Vector3.up*.78f,new Vector3(.12f,.14f,.12f),k,.04f,Color.white,Color.white,7,4);}
                var padBody=Made("The nursery's road pad",pad,null,false,Panel,Panel,BlueGlow);
                var launch=new GameObject("Launch to the nursery's road",typeof(DockingPad));launch.transform.position=pf;padBody.transform.SetParent(launch.transform,true);launch.transform.SetParent(root,true);
                var dock=launch.GetComponent<DockingPad>();dock.prompt="Launch — The nursery's road, Sorrel's Second Sky";dock.range=4;dock.scene=SorrelZone;dock.spawn="Road";
                Gate("The nursery's road opens with the survey's offer",new[]{launch},"sela.surveyOffered",true,root);
                // The moorings' cradle: the quay's repair console, paid, saving at the arrival.
                var cradle=Prop("DeckConsole",Cradle,1.4f,180,root);cradle.name="Mooring cradle";var bay=cradle.AddComponent<RepairBay>();bay.free=false;bay.saveSpawn="Arrival";bay.prompt="Mooring cradle — repair and save";bay.range=2.6f;
                // Kott at the harbour winch, Idra's crew still: a drum of cable on a frame.
                var winch=Prop("DressCableDrum",new Vector2(-19.4f,-10.6f),1.3f,90,root);winch.name="The harbour winch";
                var kott=Body("IdraNatural",KottAt,60,"Kott, at the harbour winch",root);kott.transform.localScale=Vector3.one*1.1f;Tint(kott,new Color(.7f,.72f,.76f));
                // Crates off the ships along the quay's west end; barrels by the cleats.
                Goods(new(-12.6f,4.6f),0,14,root);Goods(new(9.6f,-10.2f),30,15,root);QuayGoods(root);
                foreach(var (x,z) in new[]{(-17.6f,-10.6f),(-14,-10.4f)}){var b=Prop("DressWaterBarrels",new Vector2(x,z),.9f,(x*31)%360,root);b.name="Barrels by the cleats";}
                foreach(var (x,z,light) in new[]{(-24.6f,-12.6f,true),(-6,6.6f,true),(7,6.6f,false),(24.6f,-12.6f,true),(24.6f,6.6f,false)})LampPost(new Vector2(x,z),3.2f,new Color(.86f,.7f,.42f),light,root);
            }
            static GameObject Body(string body,Vector2 at,float yaw,string name,Transform parent)
            {
                var person=kit.Townsperson(TownLife.Body(body),new Vector3(at.x,0,at.y),yaw,Color.white,"Neve");person.name=name;person.transform.SetParent(parent,true);
                var tint=person.GetComponent<PropTint>();if(tint!=null)UnityEngine.Object.DestroyImmediate(tint);return person;
            }
            static void Gate(string name,GameObject[] targets,string flag,bool showWhenSet,Transform parent)
            {
                var gate=new GameObject(name,typeof(FlagGate)).GetComponent<FlagGate>();gate.flag=flag;gate.showWhenSet=showWhenSet;gate.targets=targets;gate.transform.SetParent(parent,true);
            }

            // ---------------------------------------------------------------- the market: the Halo's market district, docked to the arcade
            static GameObject linesBefore,linesAfter,pearsBefore,pearsAfter,railLoose,railMended;
            static void Market()
            {
                var root=Root("The market");var rng=new System.Random(31);
                // Jun's fruit stall: her counter facing the street, a green-and-cream awning, pears and greens in crates on it; Jun behind it.
                Stall("Jun's fruit stall",JunStall,270,new Color(.5f,.72f,.4f),new Color(.3f,.46f,.26f),new Color(.86f,.84f,.4f),root,3.4f);
                Body("JunBody",JunAt,90,"Jun, behind her pears",root);
                // The pear tree, leaning toward the market's lamp for thirty years: chapter 2's orchard tree in a station planter.
                var planter=Prop("DressPlanter",PearTree,.8f,0,root);planter.name="The pear tree's planter";
                var tree=kit.Prop(ZoneKit.Kit("OrchardTree","DressFanShrub"),new Vector3(PearTree.x,.4f,PearTree.y),3.1f,40,false);tree.name="Jun's pear tree";tree.transform.SetParent(root,true);
                var lamp=new Vector2(-13.4f,30.4f);var toLamp=new Vector3(lamp.x-PearTree.x,0,lamp.y-PearTree.y);tree.transform.RotateAround(At(PearTree,.45f),Vector3.Cross(Vector3.up,toLamp.normalized),10);
                LampPost(lamp,3.4f,new Color(.86f,.7f,.42f),true,root,10,1.5f);
                // The yellow line where the stall stops: the old one worn and broken; the new one, wet, from the moment it is painted.
                linesBefore=Line("The old yellow line",new Vector2(LineAt.x,20.8f),new Vector2(LineAt.x,25.2f),.14f,new Color(.76f,.66f,.3f),true,root,Paint);
                linesAfter=Line("The new yellow line, wet",new Vector2(LineAt.x,20.8f),new Vector2(LineAt.x,25.2f),.16f,Color.white,false,root,WetPaint);
                // Jun's crate of pears: behind the line, and after it is painted, one crate past it.
                pearsBefore=Pears("Jun's crate of pears",PearsBefore,root);pearsAfter=Pears("Jun's crate of pears, one crate past the line",PearsAfter,root);
                // Mira's pitch eleven, next door: a counter of her lots, the shop's front at its south end.
                var mira=new ZoneKit.Shape();var mf=At(MiraCounter);Color crate=new(.84f,.74f,.56f),cloth=new(.62f,.36f,.42f);
                mira.Slot(0).Box(mf+Vector3.up*.5f,new Vector3(1.1f,1,3.2f),Quaternion.identity,crate,ZoneKit.Shape.Dim(crate,1.1f)).Slot(1).Box(mf+new Vector3(0,1.03f,0),new Vector3(1.2f,.06f,3.3f),Quaternion.identity,cloth);
                for(int k=0;k<6;k++)mira.Slot(2).Box(mf+new Vector3(((k%2)-.5f)*.5f,1.16f,-1.2f+k*.5f),new Vector3(.32f,.2f+.06f*(k%3),.26f),Quaternion.Euler(0,k*13,0),Color.Lerp(new Color(.9f,.82f,.6f),new Color(.6f,.74f,.86f),k/6f));
                var counter=Made("Mira's counter at pitch eleven",mira,root,true,Panel,Cloth,Dark);ZoneKit.Block(counter,mf+Vector3.up*.5f,new Vector3(1.1f,1,3.2f),0,"Counter");
                var shop=new GameObject("Mira's counter: the shop",typeof(Shop)).GetComponent<Shop>();shop.transform.position=At(ShopAt,.5f);shop.transform.SetParent(counter.transform,true);
                shop.prompt="Trade — Pitch Eleven";shop.range=2.2f;shop.inventory=Find<ShopInventory>(AfloatZone);
                if(shop.inventory==null)throw new InvalidOperationException("NeighbourhoodAfloat: pitch eleven has no stock (NeighbourhoodDefinition stocks ShopInventory/"+AfloatZone+")");
                Goods(new(-20.6f,17.2f),0,21,root);Goods(new(-19.8f,20.6f),90,22,root);
                // The garden stair: six steps up the garden's side, a handrail each side on brackets. The south rail's third bracket is loose until it is mended.
                Stair(root);
                // Tovra's forge by the stair's foot: the anchor's metal made over into hoes and hinges. A hearth in a brick box, its coals lit; the anvil is the town's bench.
                var forge=new ZoneKit.Shape();var ff=At(Forge);Color brick=new(.56f,.34f,.28f),iron=new(.28f,.28f,.3f);
                forge.Slot(0).Box(ff+Vector3.up*.55f,new Vector3(1.8f,1.1f,1.4f),Quaternion.identity,brick,ZoneKit.Shape.Dim(brick,1.2f)).Box(ff+new Vector3(0,2.1f,.45f),new Vector3(.8f,2,.5f),Quaternion.identity,brick);
                forge.Slot(1).Box(ff+new Vector3(0,1.12f,0),new Vector3(1.3f,.08f,.9f),Quaternion.identity,Color.white);
                foreach(var (dx,dz,yaw) in new[]{(1.4f,.9f,20f),(1.6f,1.4f,-30f),(-1.3f,1.2f,70f)})forge.Slot(2).Beam(ff+new Vector3(dx,.05f,dz),ff+new Vector3(dx+.3f,1.2f,dz+.1f),.05f,.05f,iron);
                var hearth=Made("Tovra's forge",forge,root,true,Panel,Ember,Iron);ZoneKit.Block(hearth,ff+Vector3.up*.6f,new Vector3(1.8f,1.2f,1.4f),0,"Hearth");
                kit.Lamp(new Vector3(Forge.x,1.6f,Forge.y-.6f),6,1.6f,new Color(1,.56f,.24f));
                var anvil=Prop("LatticeAnvil",Anvil,1.6f,180,root);anvil.name="The forge's anvil";var bench=anvil.AddComponent<Bench>();bench.prompt="Fabricate — Tovra's Forge";bench.range=2.4f;
                HalsBench(new Vector2(-22.6f,38.6f),90,root);
                foreach(var (x,z,yaw) in new[]{(-23f,37.2f,0f),(-23f,32.8f,20f)}){var h=Prop("DressCargoPallet",new Vector2(x,z),.5f,yaw,root);h.name="Hoes and hinges, the anchor's metal";Tint(h,new Color(.7f,.66f,.62f));}
                // The rest of the market: stalls along its north side facing the street, a kiosk and a stall by the arcade, crates and barrels at their backs.
                Stall("A greengrocer's stall",new Vector2(-15.6f,37.2f),0,new Color(.86f,.54f,.3f),new Color(.6f,.36f,.2f),new Color(.6f,.8f,.4f),root);
                Stall("A cloth stall",new Vector2(-1,37.2f),0,new Color(.34f,.5f,.8f),new Color(.2f,.3f,.5f),new Color(.86f,.5f,.6f),root);
                Stall("A tinker's stall",new Vector2(4.2f,37.2f),0,new Color(.82f,.3f,.3f),new Color(.5f,.18f,.18f),new Color(.7f,.72f,.76f),root,3.2f);
                if(ZoneKit.Landed("DressKiosk")){var k=Prop("DressKiosk",new Vector2(5.4f,21.6f),2.6f,270,root);k.name="A kiosk";}
                Stall("A fishmonger's stall",new Vector2(3.6f,27.4f),90,new Color(.26f,.5f,.66f),new Color(.16f,.3f,.42f),new Color(.56f,.66f,.76f),root);Stall("A spice stall",new Vector2(3.6f,33.4f),90,new Color(.86f,.42f,.22f),new Color(.56f,.26f,.14f),new Color(.86f,.6f,.3f),root);
                Stall("A baker's stall",new Vector2(-11.2f,36.6f),0,new Color(.92f,.82f,.5f),new Color(.6f,.5f,.3f),new Color(.86f,.7f,.46f),root);Stall("A lamp-oil stall",new Vector2(-4,17.4f),0,new Color(.4f,.34f,.6f),new Color(.26f,.22f,.4f),new Color(.9f,.8f,.5f),root);
                Goods(new(-12.4f,38.8f),0,23,root);Goods(new(7,36),90,24,root);Goods(new(-22.6f,17.6f),0,25,root);
                var ps=new ZoneKit.Shape();foreach(var (x,z) in new[]{(-8f,16.6f),(2.4f,16.8f),(-22.8f,30.8f)})Planter(ps,At(x,z),.9f,new Color(.44f,.5f,.56f),rng);
                Made("Market planters",ps,root,true,Panel,Leaf);
                // Strings of lamps over the street, pole to pole.
                Bunting(new[]{new Vector2(-21.6f,15.2f),new Vector2(-21.6f,41)},new[]{new Vector2(7.4f,15.2f),new Vector2(7.4f,41)},root);
            }
            static GameObject Line(string name,Vector2 a,Vector2 b,float wide,Color tone,bool worn,Transform parent,Material material)
            {
                var s=new ZoneKit.Shape();var run=b-a;float length=run.magnitude;var d=run/length;var across=new Vector2(-d.y,d.x)*wide*.5f;int n=Mathf.Max(1,Mathf.RoundToInt(length/.4f));
                for(int i=0;i<n;i++)
                {
                    if(worn&&Hash(i,a.x,9)>.7f)continue;
                    var p=a+run*(i/(float)n);var q=a+run*((i+1)/(float)n);float y=LevelAt(p)+.04f;
                    s.Quad(new Vector3(p.x-across.x,y,p.y-across.y),new Vector3(q.x-across.x,y,q.y-across.y),new Vector3(q.x+across.x,y,q.y+across.y),new Vector3(p.x+across.x,y,p.y+across.y),Vector3.up,worn?tone*(.8f+.3f*Hash(i,b.y,4)):tone,.5f);
                }
                var go=Made("Floor detail: "+name,s,parent,false,material);return go;
            }
            static GameObject Pears(string name,Vector2 at,Transform parent)
            {
                var crate=Prop("DeckCrate",at,.5f,8,parent);Tint(crate,new Color(.86f,.92f,.66f));crate.name=name;
                var heap=new ZoneKit.Shape();var prng=new System.Random(5);
                for(int i=0;i<16;i++){var p=new Vector3(at.x+(float)(prng.NextDouble()-.5)*.42f,.52f+(float)prng.NextDouble()*.1f,at.y+(float)(prng.NextDouble()-.5)*.42f);heap.Lump(p,new Vector3(.06f,.08f,.06f),i,.06f,new Color(.84f,.86f,.4f),new Color(.56f,.62f,.26f),7,4);}
                Made("Pears",heap,crate.transform,false,Cloth);return crate;
            }
            /// <summary>The garden stair: drawn steps over the ramp the bodies walk, a handrail each side. The south rail is the loose one (its third bracket hangs, the rail sags) until mended.</summary>
            static void Stair(Transform root)
            {
                var s=new ZoneKit.Shape();Color step=new(.78f,.66f,.5f),riser=new(.5f,.42f,.34f);int steps=6;float run=(StairX1-StairX0)/steps;
                for(int i=0;i<steps;i++)
                {
                    float x1=StairX1-i*run,x0=x1-run,top=GardenTop*(i+1)/steps;
                    s.Slot(0).Box(new Vector3((x0+x1)*.5f,top*.5f,(StairZ0+StairZ1)*.5f),new Vector3(run,top,StairZ1-StairZ0-.1f),Quaternion.identity,riser,step);
                }
                Made("Floor detail: the garden stair's steps",s,root,true,Wood);
                Vector3 R(float x,float z,float up)=>new(x,GardenTop*Mathf.Clamp01((StairX1-x)/(StairX1-StairX0))+up,z);
                Color brass=new(.86f,.7f,.42f),iron=new(.34f,.34f,.38f);
                GameObject Rail(string name,float z,bool loose)
                {
                    var r=new ZoneKit.Shape();
                    for(int i=0;i<=4;i++)
                    {
                        float x=StairX1-.2f-i*(StairX1-StairX0-.4f)/4;bool third=loose&&i==2;
                        r.Slot(0).Post(R(x,z,0),third?.7f:1.05f,.05f,.04f,6,iron,iron);
                        if(third)r.Slot(0).Beam(R(x,z,.7f),R(x+.1f,z+.25f,.55f),.05f,.04f,iron);
                    }
                    if(!loose)r.Slot(1).Beam(R(StairX1-.2f,z,1.02f),R(StairX0+.2f,z,1.02f),.08f,.07f,brass);
                    else{r.Slot(1).Beam(R(StairX1-.2f,z,1.02f),R(-21,z+.1f,.7f),.08f,.07f,brass).Beam(R(-21,z+.1f,.7f),R(StairX0+.2f,z,1.02f),.08f,.07f,brass);}
                    var go=Made(name,r,root,true,Iron,Brass);return go;
                }
                railLoose=Rail("The garden stair's south handrail, its third bracket loose",StairZ0+.15f,true);railMended=Rail("The garden stair's south handrail, mended",StairZ0+.15f,false);
                var north=Rail("The garden stair's north handrail",StairZ1-.15f,false);
                // The stair's sides drop toward the market: blocked a rail's height above the steps where the drop is more than a step.
                foreach(var z in new[]{StairZ0+.15f,StairZ1-.15f})
                {
                    ZoneKit.Block(north,new Vector3(-22.9f,(GardenTop+1.2f)*.5f,z),new Vector3(2.2f,GardenTop+1.2f,.24f),0,"Stair side");
                    ZoneKit.Block(north,new Vector3(-20.8f,(GardenTop*.62f+1.2f)*.5f,z),new Vector3(2,GardenTop*.62f+1.2f,.24f),0,"Stair side");
                }
            }
            /// <summary>One of Hal's portable benches, unfolded at a shop: a hinged top on folding legs, a vice at one end.</summary>
            static void HalsBench(Vector2 at,float yaw,Transform parent)
            {
                var s=new ZoneKit.Shape();var foot=At(at);var t=Quaternion.Euler(0,yaw,0);Color top=new(.74f,.58f,.4f),leg=new(.36f,.36f,.4f);
                s.Slot(0).Box(foot+t*new Vector3(0,.86f,0),new Vector3(1.8f,.08f,.7f),t,ZoneKit.Shape.Dim(top,.8f),top);
                foreach(var (x,z) in new[]{(-.75f,-.28f),(.75f,-.28f),(-.75f,.28f),(.75f,.28f)})s.Slot(1).Beam(foot+t*new Vector3(x,0,z),foot+t*new Vector3(x*.9f,.84f,-z*.6f),.05f,.05f,leg);
                s.Slot(1).Box(foot+t*new Vector3(.95f,.95f,0),new Vector3(.18f,.16f,.3f),t,leg);
                var go=Made("One of Hal's benches",s,parent,true,Wood,Iron);ZoneKit.Block(go,foot+Vector3.up*.45f,new Vector3(1.8f,.9f,.7f),yaw,"Hal's bench");
            }
            /// <summary>Strings of little lamps between two rows of poles.</summary>
            static void Bunting(Vector2[] west,Vector2[] east,Transform parent)
            {
                var s=new ZoneKit.Shape();var lamps=new ZoneKit.Shape();Color pole=new(.5f,.42f,.34f);
                foreach(var p in west.Concat(east))s.Slot(0).Post(At(p),3.8f,.06f,.05f,6,pole,pole);
                for(int i=0;i<west.Length;i++)
                {
                    var a=At(west[i],3.7f);var b=At(east[i],3.7f);s.Slot(1).Rope(a,b,.6f,.015f,new Color(.3f,.28f,.26f),4,16);
                    for(int k=1;k<16;k++){float t=k/16f;var p=Vector3.Lerp(a,b,t)+Vector3.down*.6f*4*t*(1-t)+Vector3.down*.12f;lamps.Lump(p,Vector3.one*.07f,k,.02f,Color.white,Color.white,6,4);}
                    // Pennants between the lamps, in every district's colours, two-sided cloth.
                    var run=(b-a).normalized;
                    for(int k=0;k<24;k++){float t=(k+.5f)/24f;var p=Vector3.Lerp(a,b,t)+Vector3.down*.6f*4*t*(1-t);var tone=Color.HSVToRGB((k*.137f+i*.31f)%1,.45f,.92f);
                        s.Slot(2).Triangle(p-run*.18f,p+run*.18f,p+Vector3.down*.42f,Vector3.Cross(run,Vector3.up),tone,.5f);}
                }
                var go=Made("Strings of lamps",s,parent,true,Wood,Rope,Cloth);foreach(var p in west.Concat(east))ZoneKit.Block(go,At(p,1.9f),new Vector3(.2f,3.8f,.2f),0,"Pole");
                Made("The strings' lamps",lamps,parent,false,LampGlow);
            }

            // ---------------------------------------------------------------- Many Hands' arcade: shopfronts, the runner, Xylos's islands off its rail
            static void Arcade()
            {
                var root=Root("The arcade");var rng=new System.Random(41);var s=new ZoneKit.Shape();
                // The shopfronts along the north wall: Many Hands' liner arcade, a colonnade of cream panels, each front its own colour of roll shutter, some up with the shop open.
                Color cream=new(.94f,.9f,.8f),teal=new(.22f,.54f,.56f);Color[] shutters={new(.66f,.32f,.24f),new(.26f,.34f,.52f),new(.46f,.6f,.38f),new(.82f,.62f,.3f),new(.6f,.3f,.28f),new(.44f,.5f,.56f),new(.7f,.5f,.36f),new(.24f,.56f,.58f)};
                // A passage between the first and second fronts, where the gangway to the square lands.
                var fronts=new[]{(8.4f,12.2f),(16f,20.1f),(20.3f,24.4f),(24.6f,28.7f),(28.9f,33f),(33.2f,37.3f),(37.5f,41.6f)};
                for(int i=0;i<fronts.Length;i++)
                {
                    float x0=fronts[i].Item1,x1=fronts[i].Item2,cx=(x0+x1)*.5f;bool open=i%3!=1;var c=shutters[i];
                    s.Slot(0).Box(new Vector3(cx,1.9f,41.4f),new Vector3(x1-x0,3.8f,1),Quaternion.identity,cream,ZoneKit.Shape.Dim(cream,1.05f));
                    s.Slot(0).Box(new Vector3(x0+.15f,1.9f,40.75f),new Vector3(.3f,3.8f,.3f),Quaternion.identity,ZoneKit.Shape.Dim(cream,.9f)).Box(new Vector3(cx,3.95f,40.8f),new Vector3(x1-x0+.2f,.3f,.5f),Quaternion.identity,teal);
                    if(open){s.Slot(1).Box(new Vector3(cx,3.1f,40.85f),new Vector3(3.4f,.9f,.1f),Quaternion.identity,c);s.Slot(2).Box(new Vector3(cx,1.2f,40.86f),new Vector3(3.2f,2.2f,.05f),Quaternion.identity,new Color(.16f,.14f,.14f));
                        for(int k=0;k<4;k++)s.Slot(3).Lump(new Vector3(x0+.8f+k*.85f,1.1f,40.6f),new Vector3(.22f,.2f,.18f),i*7+k,.2f,Color.Lerp(c,Color.white,.4f),ZoneKit.Shape.Dim(c,.6f),7,4);}
                    else for(int k=0;k<9;k++)s.Slot(1).Box(new Vector3(cx,.25f+k*.38f,40.86f),new Vector3(3.4f,.34f,.06f),Quaternion.identity,k%2==0?c:ZoneKit.Shape.Dim(c,.8f));
                }
                var frontage=Made("The arcade's shopfronts",s,root,true,Walls,Corrugated,Dark,Cloth);
                foreach(var (x0,x1) in fronts)ZoneKit.Block(frontage,new Vector3((x0+x1)*.5f,1.9f,41.4f),new Vector3(x1-x0,3.8f,1),0,"Shopfront");
                // Binna's window: three baskets in it, red, blue and yellow (there if her baskets were brought home).
                var baskets=new ZoneKit.Shape();foreach(var (dx,c) in new[]{(-.8f,new Color(.8f,.26f,.22f)),(0f,new Color(.26f,.42f,.8f)),(.8f,new Color(.92f,.8f,.26f))})
                    baskets.Tube(new[]{new Vector3(BinnaAt.x+dx,1.5f,40.7f),new Vector3(BinnaAt.x+dx,1.95f,40.7f)},new[]{.24f,.3f},10,ZoneKit.Shape.Dim(c,.7f),c);
                var basketsGo=Made("Binna's three baskets",baskets,root,true,Weave);showsFor["BinnaHomeBaskets"]=new[]{basketsGo};
                // Dressing along the arcade: benches, planters, lamp posts, a bench of Hal's, the arcade lamp with the crystals in it.
                var dress=new ZoneKit.Shape();
                foreach(var (x,z,yaw) in new[]{(12f,33.2f,180f),(22f,33.2f,180f),(38.6f,33.4f,180f),(30.4f,17.6f,0f)})Bench(dress,At(x,z),yaw);
                foreach(var (x,z) in new[]{(9.6f,17),(27.6f,17),(41,17)})Planter(dress,At(x,z),1,new Color(.24f,.54f,.56f),rng);
                var dressed=Made("The arcade's benches and planters",dress,root,true,Wood,Leaf);
                foreach(var (x,z,yaw) in new[]{(12f,33.2f,180f),(22f,33.2f,180f),(38.6f,33.4f,180f),(30.4f,17.6f,0f)})ZoneKit.Block(dressed,At(x,z,.45f),new Vector3(1.9f,.9f,.5f),yaw,"Bench");
                foreach(var (x,z) in new[]{(9.6f,17),(27.6f,17),(41,17)})ZoneKit.Block(dressed,At(x,z,.3f),new Vector3(1,.6f,1),0,"Planter");
                HalsBench(new Vector2(HalAt.x,37.8f),0,root);
                foreach(var (x,z,light) in new[]{(16.6f,24.6f,true),(30f,31.6f,true),(41.6f,31f,false)})LampPost(new Vector2(x,z),3.4f,new Color(.86f,.7f,.42f),light,root);
                var crystals=new ZoneKit.Shape();var lampAt=At(30,31.6f,3.4f);for(int k=0;k<5;k++)crystals.Lump(lampAt+new Vector3(.55f+Mathf.Cos(k*1.3f)*.2f,-.2f-.1f*k,Mathf.Sin(k*1.3f)*.2f),new Vector3(.05f,.12f,.05f),k,.1f,new Color(.7f,.9f,1),new Color(.4f,.6f,.9f),6,4);
                showsFor["MiraHomeCrystals"]=new[]{Made("The crystals in the arcade lamp",crystals,root,false,BlueGlow)};
                Bunting(new[]{new Vector2(9,16.2f),new Vector2(9,39.4f)},new[]{new Vector2(43.4f,16.2f),new Vector2(43.4f,39.4f)},root);
                // Xylos's islands, forty of them in two rows, moored off the arcade's east rail below its level: gardens on rafts of root, a hut on some.
                var isles=new ZoneKit.Shape();var irng=new System.Random(47);
                for(int row=0;row<2;row++)for(int k=0;k<9;k++)
                {
                    var c=new Vector3(50+row*9+(float)irng.NextDouble()*2,-2.6f-row*1.4f-(float)irng.NextDouble(),12+k*4.6f+(float)irng.NextDouble()*1.4f);float r=1.6f+(float)irng.NextDouble()*.8f;
                    isles.Slot(0).Lump(c+Vector3.down*.8f,new Vector3(r,1,r*.9f),k*3+row,.25f,new Color(.42f,.34f,.26f),new Color(.26f,.2f,.16f),12,6).Slot(1).Disc(c+Vector3.up*.18f,Vector3.up,r*.92f,14,new Color(.42f,.58f,.3f));
                    for(int f=0;f<4;f++)isles.Slot(2).Fern(c+new Vector3(((float)irng.NextDouble()-.5f)*r,.2f,((float)irng.NextDouble()-.5f)*r),.7f+(float)irng.NextDouble()*.5f,5,irng,new Color(.3f,.46f,.26f),new Color(.56f,.78f,.4f));
                    if(irng.NextDouble()<.35){isles.Slot(3).Box(c+new Vector3(0,.8f,0),new Vector3(1.2f,1.2f,1),Quaternion.Euler(0,(float)irng.NextDouble()*90,0),new Color(.7f,.56f,.4f),new Color(.6f,.36f,.26f));}
                }
                Made("Xylos's islands, in two rows",isles,inSpace,true,Soil,Leaf,Leaf,Wood);
                Goods(new(41,21.4f),0,41,root);
            }

            // ---------------------------------------------------------------- the garden: Many Hands' and Xylos's beds, up the stair
            static void Garden()
            {
                var root=Root("The garden");var s=new ZoneKit.Shape();var rng=new System.Random(51);float y=GardenTop;Color bed=new(.5f,.4f,.3f),soil=new(.36f,.27f,.2f),hoop=new(.62f,.6f,.58f);
                var beds=new GameObject("The garden's beds");beds.transform.SetParent(root,true);
                foreach(var (x0,x1,z0,z1) in new[]{(-45f,-41.4f,17f,43f),(-38.4f,-34.6f,31.4f,43f),(-30.6f,-26.6f,32f,43f),(-38.4f,-34.6f,17f,26f)})
                {
                    var c=new Vector3((x0+x1)*.5f,y,(z0+z1)*.5f);var size=new Vector3(x1-x0,.5f,z1-z0);s.Slot(0).Box(c+Vector3.up*.25f,size,Quaternion.identity,bed,soil);
                    for(float x=x0+.4f;x<x1-.2f;x+=.7f)for(float z=z0+.4f;z<z1-.2f;z+=.7f){if(rng.NextDouble()<.5)s.Slot(1).Fern(new Vector3(x,y+.5f,z),.32f+.2f*(float)rng.NextDouble(),5,rng,new Color(.22f,.38f,.22f),new Color(.44f,.66f,.34f));else s.Slot(2).Lump(new Vector3(x,y+.62f,z),new Vector3(.22f,.16f,.22f),(int)(x*z),.25f,new Color(.5f,.72f,.4f),new Color(.3f,.46f,.26f),8,5);}
                    for(float z=z0+1;z<z1;z+=3){var path=new List<Vector3>();for(int k=0;k<=8;k++){float a=k/8f*Mathf.PI;path.Add(new Vector3(c.x-Mathf.Cos(a)*(size.x*.5f+.1f),y+.4f+Mathf.Sin(a)*1.7f,z));}s.Slot(3).Tube(path,Enumerable.Repeat(.04f,path.Count).ToArray(),5,hoop,hoop);}
                    ZoneKit.Block(beds,c+Vector3.up*.25f,size,0,"Bed");
                }
                var drawn=Made("The garden's beds and hoops",s,root,true,Wood,Leaf,Leaf,Iron);drawn.transform.SetParent(beds.transform,true);
                // The one shoot from chapter 7's jar of thousand-year-old seed, in a bed of its own with the gardener weeding round it (there if the gardens were put right).
                var shoot=new ZoneKit.Shape();var sf=At(ShootAt);shoot.Slot(0).Tube(new[]{sf,sf+Vector3.up*.5f},new[]{.32f,.36f},12,new Color(.56f,.42f,.32f),new Color(.66f,.5f,.36f)).Slot(1).Frond(sf+Vector3.up*.5f,Vector3.forward,.5f,.12f,.4f,.1f,new Color(.3f,.5f,.26f),new Color(.6f,.86f,.4f));
                var jar=Made("The jar with one shoot",shoot,root,true,Wood,Leaf);ZoneKit.Block(jar,sf+Vector3.up*.25f,new Vector3(.7f,.5f,.7f),0,"Jar");
                var gardener=Body("Gardener",GardenerAt,200,"The gardener, weeding round the shoot",root);
                showsFor["RuvaHomeShoot"]=new[]{jar,gardener};
                // Senn's post at the end of the garden row: carved, nothing tied to it, patted smooth on one side.
                var post=new ZoneKit.Shape();var pf=At(PostAt);post.Post(pf,1.6f,.16f,.12f,8,new Color(.5f,.38f,.28f),new Color(.66f,.5f,.36f));for(int k=0;k<4;k++)post.Disc(pf+Vector3.up*(.4f+k*.3f),Vector3.up,.19f,8,new Color(.4f,.3f,.22f),.15f);
                var postGo=Made("Senn's post",post,root,true,Wood);ZoneKit.Block(postGo,pf+Vector3.up*.8f,new Vector3(.36f,1.6f,.36f),0,"Post");showsFor["RuvaHomePost"]=new[]{postGo};
                var tools=new ZoneKit.Shape();foreach(var (x,z) in new[]{(-28.8f,18f)})Planter(tools,At(x,z),1.1f,new Color(.5f,.42f,.3f),rng);Made("Garden planters",tools,root,true,Wood,Leaf);
                foreach(var (x,z) in new[]{(-25.6f,22f),(-25.6f,40f)})LampPost(new Vector2(x,z),3,new Color(.62f,.48f,.34f),z<30,root);
            }

            // ---------------------------------------------------------------- the square: Lastlight's rib over it, Ruva's table, the board, Orrin's board, Meret's room, Drask's window
            static GameObject chalked;
            static void Square()
            {
                var root=Root("The square");var rng=new System.Random(61);
                // The rib from Tern's roof, over the square: a town wants something overhead that used to be going somewhere.
                var rib=kit.Prop(ZoneKit.Kit("RibArch","AncelaneArch"),new Vector3(RibAt.x,0,RibAt.y),10,0,false);rib.name="The rib from Lastlight, over the square";rib.transform.SetParent(root,true);
                // Its feet came with Lastlight's snow drifted round them (baked into the model): let into the paving, so the square shows bone, not snowbanks, under a warm sky.
                rib.transform.position+=Vector3.down*1.6f;
                // And its crown carries Lastlight's snow cap and icicles, white from every view of the square: the bone's own material warmed so the cap reads as weathered bone.
                // (Its own copy: Lastlight's rib keeps its snow.)
                foreach(var drawnRib in rib.GetComponentsInChildren<Renderer>())
                {
                    var src=drawnRib.sharedMaterial;if(src==null)continue;
                    drawnRib.sharedMaterial=ZoneKit.Toon("afloat-rib-bone",src.GetTexture("_BaseMap") as Texture2D,m=>{m.shader=src.shader;m.CopyPropertiesFromMaterial(src);m.shaderKeywords=src.shaderKeywords;m.SetColor("_BaseColor",new Color(.88f,.76f,.6f));});
                }
                ZoneKit.Fadeable(rib);
                var ribBox=ZoneKit.Drawn(rib);bool spanX=ribBox.extents.x>=ribBox.extents.z;var spanDir=spanX?Vector3.right:Vector3.forward;float spanHalf=Mathf.Max(ribBox.extents.x,ribBox.extents.z);
                foreach(int side in new[]{-1,1}){var leg=new GameObject("Rib leg",typeof(BoxCollider));leg.isStatic=true;leg.transform.SetParent(rib.GetComponentInChildren<Renderer>().transform,true);leg.transform.position=new Vector3(ribBox.center.x,1.25f,ribBox.center.z)+spanDir*side*(spanHalf-.6f);leg.GetComponent<BoxCollider>().size=new Vector3(1.1f,2.5f,1.1f);}
                Debug.Log($"AFLOAT_RIB centre={ribBox.center} size={ribBox.size}");
                // Iru's three high lamps on the rib (there if the shrine's lamps were kept).
                var lamps=new ZoneKit.Shape();foreach(var x in new[]{-spanHalf*.55f,0,spanHalf*.55f}){var p=new Vector3(ribBox.center.x,ribBox.max.y-(x==0?.6f:2.2f),ribBox.center.z)+spanDir*x;lamps.Slot(0).Rope(p+Vector3.up*.4f,p,.02f,.02f,new Color(.3f,.28f,.26f)).Slot(1).Lump(p+Vector3.down*.2f,new Vector3(.22f,.3f,.22f),(int)x,.05f,Color.white,Color.white,8,5);}
                var ribLamps=Made("Iru's three lamps on the rib",lamps,rib.GetComponentInChildren<Renderer>().transform,false,Rope,LampGlow);showsFor["IruHomeLamps"]=new[]{ribLamps};
                kit.Lamp(new Vector3(RibAt.x,4.6f,RibAt.y-1),12,1.4f,new Color(1,.78f,.52f));
                // Lastlight's kept longhouse behind it, on the square's north edge: the herders' own, where the count is kept.
                var longhouse=kit.Prop(ZoneKit.Kit("KeptLonghouse","CanyonHouse"),new Vector3(-2.6f,0,73.8f),Tall("KeptLonghouse",7),0,true);longhouse.name="Lastlight's kept longhouse";longhouse.transform.SetParent(root,true);
                // Ruva's table: motion nine, since breakfast. Benches either side, papers, a jug, her hammer under the minutes.
                var t=new ZoneKit.Shape();var tf=At(Table);Color wood=new(.64f,.5f,.36f);
                t.Slot(0).Box(tf+Vector3.up*.76f,new Vector3(4.4f,.08f,1.2f),Quaternion.identity,wood,ZoneKit.Shape.Dim(wood,1.15f));
                foreach(var (x,z) in new[]{(-2f,-.5f),(2f,-.5f),(-2f,.5f),(2f,.5f)})t.Slot(0).Box(tf+new Vector3(x,.37f,z),new Vector3(.08f,.74f,.08f),Quaternion.identity,ZoneKit.Shape.Dim(wood,.7f));
                foreach(float z in new[]{-1.05f,1.05f})t.Slot(0).Box(tf+new Vector3(0,.42f,z),new Vector3(4,.07f,.36f),Quaternion.identity,wood);
                for(int k=0;k<9;k++)t.Slot(1).Box(tf+new Vector3(-1.8f+k*.45f,.82f,((k*37)%5-2)*.1f),new Vector3(.3f,.01f,.42f),Quaternion.Euler(0,(k*29)%30-15,0),new Color(.94f,.92f,.86f));
                t.Slot(2).Box(tf+new Vector3(1.3f,.88f,.2f),new Vector3(.08f,.08f,.36f),Quaternion.Euler(0,30,0),new Color(.4f,.32f,.24f)).Box(tf+new Vector3(1.3f,.88f,.38f),new Vector3(.2f,.12f,.12f),Quaternion.Euler(0,30,0),new Color(.5f,.5f,.54f));
                var table=Made("Ruva's table, motion nine",t,root,true,Wood,Cloth,Iron);ZoneKit.Block(table,tf+Vector3.up*.45f,new Vector3(4.4f,.9f,2.6f),0,"Table");
                // The message board: three things pinned to it, under a little roof, a lamp.
                var b=new ZoneKit.Shape();var bf=At(Board);Color frame=new(.5f,.4f,.3f);
                b.Slot(0).Box(bf+new Vector3(0,1.55f,0),new Vector3(6.6f,1.7f,.18f),Quaternion.identity,frame,ZoneKit.Shape.Dim(frame,1.1f)).Box(bf+new Vector3(0,2.6f,-.1f),new Vector3(7,.12f,.6f),Quaternion.Euler(-14,0,0),new Color(.4f,.32f,.26f));
                foreach(float x in new[]{-3.2f,3.2f})b.Slot(0).Post(bf+new Vector3(x,0,0),2.6f,.09f,.08f,6,frame,frame);
                b.Slot(1).Box(bf+new Vector3(0,1.55f,-.1f),new Vector3(6.2f,1.5f,.02f),Quaternion.identity,new Color(.82f,.74f,.6f));
                for(int k=0;k<14;k++)b.Slot(2).Box(bf+new Vector3(-2.9f+(k*1.37f)%5.8f,1.0f+(k*.53f)%1.1f,-.13f),new Vector3(.32f,.4f,.01f),Quaternion.Euler(0,0,(k*17)%16-8),Color.Lerp(new Color(.96f,.94f,.88f),new Color(.88f,.84f,.7f),(k%3)/2f));
                // The cord, the seed tag, the valve drawings, each on its own peg.
                var m0=Messages[0].on;b.Slot(3).Rope(new Vector3(m0.x-.3f,1.9f,m0.y-.2f),new Vector3(m0.x+.3f,1.4f,m0.y-.2f),.08f,.02f,new Color(.7f,.5f,.3f)).Rope(new Vector3(m0.x,1.9f,m0.y-.2f),new Vector3(m0.x-.1f,1.2f,m0.y-.2f),.05f,.02f,new Color(.7f,.5f,.3f));
                var m1=Messages[1].on;b.Slot(2).Box(new Vector3(m1.x,1.6f,m1.y-.22f),new Vector3(.22f,.12f,.01f),Quaternion.Euler(0,0,8),new Color(.86f,.74f,.46f));
                var m2=Messages[2].on;b.Slot(2).Box(new Vector3(m2.x,1.55f,m2.y-.22f),new Vector3(.5f,.66f,.01f),Quaternion.Euler(0,0,-4),new Color(.92f,.92f,.96f));
                var board=Made("The message board",b,root,true,Wood,Panel,Cloth,Rope);ZoneKit.Block(board,bf+Vector3.up*1.3f,new Vector3(6.8f,2.6f,.4f),0,"Board");
                kit.Lamp(new Vector3(Board.x,2.4f,Board.y-1.2f),6,1.1f,new Color(1,.84f,.6f));
                // Orrin's new board: chalk in columns, motions, rotas, the convoy's names; after the handrail, one more line in his hand.
                var o=new ZoneKit.Shape();var of=At(OrrinBoard);Color dark=new(.16f,.2f,.18f);
                o.Slot(0).Box(of+new Vector3(0,1.6f,0),new Vector3(4.2f,2,.14f),Quaternion.identity,frame).Slot(1).Box(of+new Vector3(0,1.6f,-.08f),new Vector3(3.9f,1.8f,.02f),Quaternion.identity,dark);
                foreach(float x in new[]{-2f,2f})o.Slot(0).Post(of+new Vector3(x,0,0),2.7f,.07f,.06f,6,frame,frame);
                for(int c=0;c<3;c++)for(int r=0;r<7;r++)if(Hash(c,r,2)>.15f)o.Slot(2).Box(of+new Vector3(-1.6f+c*1.25f+Hash(r,c,5)*.2f,2.3f-r*.22f,-.1f),new Vector3(.6f+Hash(c,r,7)*.4f,.04f,.01f),Quaternion.identity,new Color(.92f,.92f,.88f));
                var obody=Made("Orrin's new board",o,root,true,Wood,Dark,Chalk);ZoneKit.Block(obody,of+Vector3.up*1.3f,new Vector3(4.2f,2.6f,.3f),0,"Board");
                var line=new ZoneKit.Shape().Box(of+new Vector3(.5f,.82f,-.1f),new Vector3(1.6f,.05f,.01f),Quaternion.identity,new Color(.96f,.96f,.9f));chalked=Made("Orrin's line: the handrail, Vosk",line,root,false,Chalk);
                Gate("Orrin chalks the handrail up",new[]{chalked},"end.handrail",true,root);chalked.SetActive(false);
                Bunting(new[]{new Vector2(-17.2f,55.2f),new Vector2(-17.2f,70.2f)},new[]{new Vector2(17.2f,55.2f),new Vector2(17.2f,70.2f)},root);
                // Meret's plain room off the square: two walls standing, two low (the camera's side), the door open; a table with the list on it, a chair she doesn't use.
                Room(root);
                // Drask's window over the square: a cabin on the square's north edge, its window open, Drask inside at the sill.
                Cabin(root);
                // The square's dressing: benches round its edge, planters, lamp posts.
                var dress=new ZoneKit.Shape();
                foreach(var (x,z,yaw) in new[]{(-15f,52.6f,90f),(-15f,69.6f,90f),(15f,72f,270f)})Bench(dress,At(x,z),yaw);
                
                var dressed=Made("The square's benches and planters",dress,root,true,Wood,Leaf);
                foreach(var (x,z,yaw) in new[]{(-15f,52.6f,90f),(-15f,69.6f,90f),(15f,72f,270f)})ZoneKit.Block(dressed,At(x,z,.45f),new Vector3(1.9f,.9f,.5f),yaw,"Bench");
                
                foreach(var (x,z,light) in new[]{(-16.8f,64.8f,true),(16.6f,53.6f,true),(-7.4f,74.6f,true),(16.8f,69f,false)})LampPost(new Vector2(x,z),3.4f,new Color(.6f,.5f,.4f),light,root);
            }
            /// <summary>One plain piece of a building, its own mesh and its own block: a room drawn as one mesh stood its person inside that mesh's box (NobodyStandsInsideAProp).</summary>
            static GameObject Wall(string name,Vector3 centre,Vector3 size,Color tone,Color top,Material material,Transform parent,bool solid=true,float yaw=0)
            {
                var s=new ZoneKit.Shape().Box(centre,size,Quaternion.Euler(0,yaw,0),tone,top);var go=Made(name,s,parent,true,material);
                if(solid)ZoneKit.Block(go,centre,size+new Vector3(.04f,0,.04f),yaw,name);return go;
            }
            static void Room(Transform root)
            {
                var room=Root("Meret's plain room");room.SetParent(root,true);Color wall=new(.82f,.8f,.74f),trim=new(.56f,.52f,.46f),wood=new(.56f,.44f,.32f);float x0=12.8f,x1=17.4f,z0=56,z1=60.6f;
                // Two walls standing, two low (the camera's side), the door open on its hinge into the room.
                Wall("Meret's room: the back wall",new Vector3((x0+x1)*.5f,1.4f,z1),new Vector3(x1-x0+.2f,2.8f,.24f),wall,trim,Panel,room);
                Wall("Meret's room: the side wall",new Vector3(x1,1.4f,(z0+z1)*.5f),new Vector3(.24f,2.8f,z1-z0),wall,trim,Panel,room);
                Wall("Meret's room: the low wall to the square",new Vector3(x0,.55f,(z0+z1)*.5f),new Vector3(.24f,1.1f,z1-z0),wall,trim,Panel,room);
                Wall("Meret's room: the low front, west of the door",new Vector3((x0+13.4f)*.5f,.55f,z0),new Vector3(13.4f-x0,1.1f,.24f),wall,trim,Panel,room);
                Wall("Meret's room: the low front, east of the door",new Vector3((15.2f+x1)*.5f,.55f,z0),new Vector3(x1-15.2f,1.1f,.24f),wall,trim,Panel,room);
                Wall("Meret's room: the door, open",new Vector3(15.1f,1,56.7f),new Vector3(.06f,2,1.5f),wood,wood,Wood,room,false,-12);
                // Her table, the list on it (a thick one), the chair pushed in: she stands.
                var t=new ZoneKit.Shape();t.Slot(0).Box(new Vector3(16.4f,.74f,59.6f),new Vector3(1.4f,.06f,.8f),Quaternion.identity,new Color(.6f,.48f,.36f)).Slot(1).Box(new Vector3(16.3f,.84f,59.6f),new Vector3(.5f,.14f,.36f),Quaternion.identity,new Color(.94f,.92f,.86f));
                foreach(var (x,z) in new[]{(15.8f,59.3f),(17f,59.3f),(15.8f,59.9f),(17f,59.9f)})t.Slot(0).Box(new Vector3(x,.36f,z),new Vector3(.06f,.72f,.06f),Quaternion.identity,new Color(.4f,.32f,.24f));
                var table=Made("Meret's table, the list on it",t,room,true,Wood,Cloth);ZoneKit.Block(table,new Vector3(16.4f,.4f,59.6f),new Vector3(1.4f,.8f,.8f),0,"Table");
                var c=new ZoneKit.Shape().Box(new Vector3(16.4f,.46f,58.7f),new Vector3(.5f,.06f,.5f),Quaternion.identity,wood).Box(new Vector3(16.4f,.8f,58.95f),new Vector3(.5f,.6f,.06f),Quaternion.identity,wood);
                foreach(var (x,z) in new[]{(16.2f,58.5f),(16.6f,58.5f),(16.2f,58.9f),(16.6f,58.9f)})c.Box(new Vector3(x,.22f,z),new Vector3(.05f,.44f,.05f),Quaternion.identity,ZoneKit.Shape.Dim(wood,.7f));
                Made("The chair she doesn't use",c,room,true,Wood);
                kit.Lamp(new Vector3(16,2.2f,59.4f),5,.9f,new Color(1,.86f,.66f));
            }
            static void Cabin(Transform root)
            {
                var cabin=Root("Drask's cabin over the square");cabin.SetParent(root,true);Color wall=new(.62f,.66f,.7f),trim=new(.42f,.44f,.48f),warm=new(1,.86f,.6f);
                float wx=DraskWindow.x,x0=wx-3.8f,x1=wx+3.8f,z0=DraskWindow.y,z1=75.8f,h=5.2f,wl=wx-1.1f,wr=wx+1.1f;
                // The front: solid either side of the window, the sill knee-high, the window open (Drask leans on the sill), a second storey above with lit windows.
                var parts=new List<GameObject>
                {
                    Wall("Drask's cabin: the front, west of the window",new Vector3((x0+wl)*.5f,h*.5f,z0),new Vector3(wl-x0,h,.3f),wall,trim,Walls,cabin),
                    Wall("Drask's cabin: the front, east of the window",new Vector3((wr+x1)*.5f,h*.5f,z0),new Vector3(x1-wr,h,.3f),wall,trim,Walls,cabin),
                    Wall("Drask's cabin: the sill",new Vector3(wx,.5f,z0),new Vector3(2.2f,1,.34f),trim,trim,Walls,cabin),
                    Wall("Drask's cabin: over the window",new Vector3(wx,(2.4f+h)*.5f,z0),new Vector3(2.2f,h-2.4f,.3f),wall,trim,Walls,cabin),
                    Wall("Drask's cabin: the back",new Vector3(wx,h*.5f,z1),new Vector3(x1-x0,h,.3f),wall,trim,Walls,cabin),
                    Wall("Drask's cabin: the west side",new Vector3(x0,h*.5f,(z0+z1)*.5f),new Vector3(.3f,h,z1-z0),wall,trim,Walls,cabin),
                    Wall("Drask's cabin: the east side",new Vector3(x1,h*.5f,(z0+z1)*.5f),new Vector3(.3f,h,z1-z0),wall,trim,Walls,cabin),
                    Wall("Drask's cabin: the roof",new Vector3(wx,h+.15f,(z0+z1)*.5f),new Vector3(x1-x0+.6f,.3f,z1-z0+.6f),trim,trim,Walls,cabin,false),
                };
                var lit=new ZoneKit.Shape().Box(new Vector3(wx-2.4f,3.9f,z0-.17f),new Vector3(1,.9f,.02f),Quaternion.identity,warm).Box(new Vector3(wx+2.4f,3.9f,z0-.17f),new Vector3(1,.9f,.02f),Quaternion.identity,warm);
                parts.Add(Made("Drask's cabin: the lit windows upstairs",lit,cabin,false,LampGlow));
                foreach(var part in parts)ZoneKit.Fadeable(part);
                kit.Lamp(new Vector3(wx,2.2f,74.6f),4,.8f,new Color(1,.84f,.6f));
            }

            // ---------------------------------------------------------------- the low street: Driftreed's raft basin, the smokehouse, Corra's stall under Brekk's awning
            static void LowStreet()
            {
                var root=Root("The low street");var rng=new System.Random(71);
                // Reed rafts on the basin: the weavers', the mat raft, the school raft (its bell over the door if the school's bell came home).
                foreach(var (x,z,yaw,size) in new[]{(-50.4f,58.4f,20f,7f),(-47.6f,69.4f,-10f,6.2f),(-52.4f,73.4f,60f,5f)})
                {if(!ZoneKit.Landed("ReedRaft"))break;var raft=kit.Prop("ReedRaft",new Vector3(x,BasinWater-.2f,z),Tall("ReedRaft",size),yaw,false,false);raft.name="A reed raft on the basin";raft.transform.SetParent(root,true);}
                var bell=new ZoneKit.Shape();var bp=new Vector3(-46.4f,BasinWater,62.6f);bell.Slot(0).Post(bp,2.6f,.06f,.05f,6,new Color(.4f,.32f,.24f),new Color(.5f,.4f,.3f)).Beam(bp+Vector3.up*2.5f,bp+new Vector3(.8f,2.5f,0),.06f,.06f,new Color(.5f,.4f,.3f))
                    .Slot(1).Tube(new[]{bp+new Vector3(.8f,2.3f,0),bp+new Vector3(.8f,1.9f,0)},new[]{.1f,.24f},12,new Color(.8f,.62f,.32f),new Color(.9f,.72f,.38f));
                showsFor["DovHomeBell"]=new[]{Made("Iri's bell over the school door",bell,root,true,Wood,Brass)};
                // The harbour lamp at the basin's mouth: two short, one long.
                var hl=LampPost(HarbourLamp,4,new Color(.58f,.5f,.36f),true,root,10,1.4f,new Color(1,.76f,.44f));hl.name="The harbour lamp";
                // The smokehouse: it floats, as Dov said it would. A hut with a counter to the street, smoke going up.
                var s=new ZoneKit.Shape();var sf=At(Smokehouse);Color reed=new(.7f,.6f,.42f),dark=new(.36f,.3f,.24f);
                s.Slot(0).Box(sf+new Vector3(-2,1.4f,0),new Vector3(2.2f,2.8f,3.6f),Quaternion.identity,reed,dark).Box(sf+new Vector3(.6f,.5f,0),new Vector3(.6f,1,3),Quaternion.identity,dark,reed);
                s.Slot(1).Box(sf+new Vector3(-2,3.2f,0),new Vector3(2.8f,.8f,4.2f),Quaternion.Euler(0,0,8),new Color(.6f,.44f,.3f));
                for(int k=0;k<5;k++)s.Slot(2).Lump(sf+new Vector3(.6f,1.1f,-1.2f+k*.6f),new Vector3(.22f,.08f,.12f),k,.1f,new Color(.3f,.4f,.56f),new Color(.2f,.26f,.36f),7,4);
                var smoke=Made("Dov's smokehouse",s,root,true,Weave,Wood,Cloth);ZoneKit.Block(smoke,sf+new Vector3(-2,1.4f,0),new Vector3(2.2f,2.8f,3.6f),0,"Smokehouse");ZoneKit.Block(smoke,sf+new Vector3(.6f,.5f,0),new Vector3(.6f,1,3),0,"Counter");
                showsFor["DovHomeSmokehouse"]=Array.Empty<GameObject>();
                // Corra's stall on the low street, Brekk's awning over it (the first one he's put on a wall that's staying), three kettles on her hob if she kept three.
                Stall("Corra's stall, Brekk's awning",CorraStall,0,new Color(.42f,.3f,.56f),new Color(.26f,.18f,.36f),new Color(.66f,.66f,.7f),root,3.6f);
                var kettles=new ZoneKit.Shape();for(int k=0;k<3;k++){var kp=At(CorraStall.x-.9f+k*.9f,CorraStall.y,1.07f);kettles.Lump(kp+Vector3.up*.14f,new Vector3(.17f,.14f,.17f),k,.02f,new Color(.78f,.62f,.4f),new Color(.5f,.38f,.26f),10,6).Beam(kp+new Vector3(.16f,.18f,0),kp+new Vector3(.3f,.28f,0),.03f,.03f,new Color(.6f,.46f,.3f));}
                showsFor["CorraHomeKettles"]=new[]{Made("Three kettles on Corra's hob",kettles,root,true,Brass)};
                
                foreach(var (x,z,yaw) in new[]{(-38.4f,54f,0f),(-27.4f,66.6f,90f)}){var c=Prop("DressCargoPallet",new Vector2(x,z),.7f,yaw,root);c.name="Mats rolled for the noon untying";Tint(c,new Color(.86f,.78f,.56f));}
                foreach(var (x,z,light) in new[]{(-25.2f,59.6f,true),(-38.8f,74f,true)})LampPost(new Vector2(x,z),3.2f,new Color(.58f,.5f,.36f),light,root);
            }

            // ---------------------------------------------------------------- the east terrace: Tallow's cabins
            static void Terrace()
            {
                var root=Root("The east terrace");var s=new ZoneKit.Shape();var box=new ZoneKit.Shape();var blocks=new GameObject("The cabins");blocks.transform.SetParent(root,true);var rng=new System.Random(81);
                Color[] paints={new(.84f,.86f,.84f),new(.9f,.86f,.74f),new(.6f,.66f,.7f),new(.74f,.82f,.84f)};
                // Two rows of cabins: along the east edge facing west, along the north edge facing south. Each a door, a window, a number; window boxes on some, cabin nine's the best.
                void Cabin(Vector2 c,float yaw,int number,bool box9)
                {
                    var t=Quaternion.Euler(0,yaw,0);var foot=At(c);var paint=paints[number%paints.Length];
                    s.Slot(0).Box(foot+t*new Vector3(0,1.5f,0),new Vector3(4,3,3),t,paint,ZoneKit.Shape.Dim(paint,.8f)).Box(foot+t*new Vector3(0,3.1f,0),new Vector3(4.3f,.2f,3.3f),t,new Color(.42f,.44f,.48f));
                    s.Slot(1).Box(foot+t*new Vector3(-.8f,1.05f,-1.52f),new Vector3(.9f,2.1f,.04f),t,new Color(.36f,.3f,.26f)).Box(foot+t*new Vector3(.9f,1.7f,-1.52f),new Vector3(1,.8f,.04f),t,new Color(1,.86f,.6f));
                    if(box9||rng.NextDouble()<.4){box.Slot(0).Box(foot+t*new Vector3(.9f,1.2f,-1.7f),new Vector3(1.1f,.22f,.3f),t,new Color(.5f,.38f,.28f));for(int k=0;k<4;k++)box.Slot(1).Lump(foot+t*new Vector3(.5f+k*.27f,1.42f,-1.7f),new Vector3(.12f,.12f,.12f),number*4+k,.3f,box9?new Color(.94f,.5f,.6f):new Color(.5f,.74f,.42f),new Color(.3f,.5f,.28f),7,4);}
                    ZoneKit.Block(blocks,foot+t*new Vector3(0,1.5f,0),new Vector3(4,3,3),yaw,"Cabin");
                }
                int n=1;foreach(var z in new[]{51.6f,56.4f,61.2f,66f,70.8f})Cabin(new Vector2(53.8f,z),270,n++,false);
                // The last of the row stands east of the sky-garden gangway's foot (x 38.5 to 41.5): at 40.4 it shut the gangway off from the terrace.
                foreach(var x in new[]{26.6f,31.2f,35.8f,45.2f})Cabin(new Vector2(x,71.8f),0,n++==9?9:n,x==35.8f);
                var cabins=Made("Tallow's cabins",s,root,true,Walls,Dark);ZoneKit.Fadeable(cabins);Made("Window boxes",box,root,true,Wood,Leaf);
                // Oda's door: the empty frame Mother hung beside it.
                var frame=new ZoneKit.Shape();var fp=new Vector3(52.24f,1.6f,59.6f);frame.Box(fp+new Vector3(0,.4f,0),new Vector3(.05f,.06f,.9f),Quaternion.identity,new Color(.7f,.56f,.34f)).Box(fp-new Vector3(0,.4f,0),new Vector3(.05f,.06f,.9f),Quaternion.identity,new Color(.7f,.56f,.34f))
                    .Box(fp+new Vector3(0,0,.42f),new Vector3(.05f,.86f,.06f),Quaternion.identity,new Color(.7f,.56f,.34f)).Box(fp-new Vector3(0,0,.42f),new Vector3(.05f,.86f,.06f),Quaternion.identity,new Color(.7f,.56f,.34f));
                Made("The empty frame beside Oda's door",frame,root,false,Brass);
                // Ishen's corridor: forty shelves through an open door, the Coronach on the first.
                var sh=new ZoneKit.Shape();for(int r=0;r<4;r++)sh.Slot(0).Box(new Vector3(51.6f,.5f+r*.6f,66),new Vector3(.4f,.04f,2.4f),Quaternion.identity,new Color(.6f,.48f,.34f));
                for(int k=0;k<14;k++)sh.Slot(1).Box(new Vector3(51.6f,.66f+(k%4)*.6f,65+(k*.37f)%2),new Vector3(.3f,.28f,.08f+.05f*(k%3)),Quaternion.identity,Color.Lerp(new Color(.6f,.3f,.26f),new Color(.3f,.4f,.6f),(k%5)/4f));
                var shelves=Made("Ishen's shelves",sh,root,true,Wood,Cloth);showsFor["IshenHomeShelves"]=new[]{shelves};
                // Dace's four lamps on the outer rail: low masts, blue lights, calling nothing.
                var dl=new ZoneKit.Shape();foreach(var x in new[]{30f,36f,42f,48f}){var p=new Vector3(x,0,48.5f);dl.Slot(0).Post(p,1.7f,.05f,.04f,6,new Color(.3f,.32f,.36f),new Color(.4f,.42f,.46f)).Slot(1).Lump(p+Vector3.up*1.8f,new Vector3(.14f,.18f,.14f),(int)x,.03f,Color.white,Color.white,8,5);}
                showsFor["SurvivorHomeLamps"]=new[]{Made("Dace's four lamps on the outer rail",dl,root,false,Iron,BlueGlow)};
                // Socks in the window over lock nine, as a flag.
                var socks=new ZoneKit.Shape();for(int k=0;k<3;k++)socks.Quad(new Vector3(52.2f,2.1f,51.2f+k*.3f),new Vector3(52.2f,2.1f,51.4f+k*.3f),new Vector3(52.2f,1.7f,51.45f+k*.3f),new Vector3(52.2f,1.7f,51.25f+k*.3f),Vector3.left,Color.HSVToRGB(k*.3f,.4f,.9f),.5f);
                showsFor["NeveHomeSocks"]=new[]{Made("Socks in the window over lock nine",socks,root,false,Cloth)};
                                foreach(var (x,z,light) in new[]{(28,57.6f,true),(44.4f,62.4f,true),(50,69.4f,false)})LampPost(new Vector2(x,z),3.2f,new Color(.7f,.72f,.74f),light,root);
                // Laundry across the terrace's north-west corner.
                var line=new ZoneKit.Shape();var a=At(25.2f,73.4f);var b=At(25.2f,66.8f);line.Slot(0).Post(a,2.4f,.05f,.04f,6,new Color(.4f,.4f,.44f),new Color(.4f,.4f,.44f)).Post(b,2.4f,.05f,.04f,6,new Color(.4f,.4f,.44f),new Color(.4f,.4f,.44f)).Slot(1).Rope(a+Vector3.up*2.3f,b+Vector3.up*2.3f,.2f,.012f,new Color(.7f,.64f,.5f),4,8);
                for(int k=0;k<5;k++){var p=Vector3.Lerp(a,b,.15f+k*.17f)+Vector3.up*(2.2f-.2f*Mathf.Sin((k+1)*Mathf.PI/6));line.Slot(2).Quad(p+new Vector3(0,0,-.25f),p+new Vector3(0,0,.25f),p+new Vector3(0,-.6f,.22f),p+new Vector3(0,-.6f,-.22f),Vector3.left,Color.HSVToRGB(.1f+.17f*k,.35f,.9f),.5f);}
                var poles=Made("Laundry on the terrace",line,root,true,Iron,Rope,Cloth);ZoneKit.Block(poles,a+Vector3.up*1.2f,new Vector3(.2f,2.4f,.2f),0,"Pole");ZoneKit.Block(poles,b+Vector3.up*1.2f,new Vector3(.2f,2.4f,.2f),0,"Pole");
            }

            // ---------------------------------------------------------------- the lantern gallery: Lanternwick's lamps
            static void Gallery()
            {
                var root=Root("The lantern gallery");
                foreach(var (x,z,size) in new[]{(-44f,98.4f,7.5f),(-32f,98.6f,8.5f),(-8.6f,98.4f,7f)})
                {var tower=Prop(ZoneKit.Kit("LanternTower","LampPost"),new Vector2(x,z),size,180,root,true);tower.name="A lantern tower";}
                var great=Prop(ZoneKit.Kit("GreatLantern","LampPost"),GalleryLantern,3.6f,180,root,true);great.name="The gallery lantern";
                kit.Lamp(new Vector3(GalleryLantern.x,3,GalleryLantern.y-1),10,1.8f,new Color(1,.78f,.46f));
                var arch=kit.Arch("The gallery's arch",new Vector3(-10,0,85.8f),7,0);arch.transform.SetParent(root,true);
                foreach(var x in new[]{-46f,-34f,-28f,-22f,-16f})LampPost(new Vector2(x,84.7f),3.2f,new Color(.84f,.66f,.38f),Mathf.Abs(x+31)<4||Mathf.Abs(x+16)<1,root);
                foreach(var x in new[]{-38f,-26f,-14f})LampPost(new Vector2(x,98.6f),3.6f,new Color(.84f,.66f,.38f),false,root);
                // Pell's mother's lamp, first on the round (there if it came back), and the Oriel flame's twin (there if Lume's lantern was carried).
                var mothers=Prop("LampPost",new Vector2(-28.6f,94),1.8f,0,root,true);mothers.name="Pell's mother's lamp";showsFor["PellHomeMothersLamp"]=new[]{mothers};
                var oriel=new ZoneKit.Shape();var op=At(-23,96.6f);oriel.Slot(0).Post(op,1.4f,.1f,.08f,8,new Color(.4f,.42f,.5f),new Color(.6f,.62f,.7f)).Slot(1).Lump(op+Vector3.up*1.6f,new Vector3(.22f,.34f,.22f),3,.05f,Color.white,Color.white,8,5);
                var orielGo=Made("The Oriel flame's twin",oriel,root,true,Iron,BlueGlow);ZoneKit.Block(orielGo,op+Vector3.up*.8f,new Vector3(.3f,1.6f,.3f),0,"Lamp");showsFor["PellHomeOriel"]=new[]{orielGo};
                var dress=new ZoneKit.Shape();var rng=new System.Random(91);Bench(dress,At(-40,95.4f),180);Bench(dress,At(-17,96.4f),180);
                var dressed=Made("Gallery benches and planters",dress,root,true,Wood,Leaf);foreach(var (x,z) in new[]{(-40,95.4f),(-17,96.4f)})ZoneKit.Block(dressed,At(x,z,.45f),new Vector3(1.9f,.9f,.5f),0,"Bench");
                
            }

            // ---------------------------------------------------------------- the sky-garden: the Crawl's deck, a park now
            static void SkyGarden()
            {
                var root=Root("The sky-garden");var s=new ZoneKit.Shape();var rng=new System.Random(101);Color iron=new(.3f,.3f,.33f),brass=new(.92f,.72f,.42f),bed=new(.5f,.42f,.34f),soil=new(.36f,.27f,.2f);
                var blocks=new GameObject("The sky-garden's beds");blocks.transform.SetParent(root,true);
                foreach(var (x0,x1,z0,z1) in new[]{(10f,18f,97.6f,99f),(26f,38f,97.6f,99f),(45.6f,47f,86f,97f)})
                {
                    var c=new Vector3((x0+x1)*.5f,0,(z0+z1)*.5f);var size=new Vector3(x1-x0,.6f,z1-z0);s.Slot(0).Box(c+Vector3.up*.3f,size,Quaternion.identity,bed,soil);
                    for(float x=x0+.3f;x<x1-.2f;x+=.5f)for(float z=z0+.25f;z<z1-.1f;z+=.5f)s.Slot(1).Fern(new Vector3(x,.6f,z),.32f+.15f*(float)rng.NextDouble(),5,rng,new Color(.2f,.32f,.22f),new Color(.36f,.56f,.34f));
                    ZoneKit.Block(blocks,c+Vector3.up*.3f,size,0,"Raised bed");
                }
                // Trees in tubs on gimbal rings, still hung level as they were on the Crawl, though nothing leans any more.
                foreach(var (x,z,seed) in new[]{(14.4f,91f,1),(32.6f,86.2f,2),(42.4f,95.6f,3),(20.6f,97.4f,4),(20f,94.4f,5),(31.2f,95.8f,6)})
                {
                    var foot=At(x,z);foreach(int side in new[]{-1,1})s.Slot(2).Post(foot+new Vector3(side*.85f,0,0),1.3f,.06f,.05f,6,iron,iron);
                    s.Slot(4).Tube(Enumerable.Range(0,25).Select(i=>foot+new Vector3(Mathf.Cos(i*Mathf.PI/12)*.85f,1.25f,Mathf.Sin(i*Mathf.PI/12)*.85f)).ToArray(),Enumerable.Repeat(.05f,25).ToArray(),6,brass,brass,1,1,false);
                    s.Slot(0).Tube(new[]{foot+new Vector3(0,.35f,0),foot+new Vector3(0,1.1f,0)},new[]{.48f,.62f},14,new Color(.5f,.4f,.3f),new Color(.6f,.48f,.36f)).Slot(0).Tube(new[]{foot+new Vector3(0,1,0),foot+new Vector3(.1f,2.4f,0),foot+new Vector3(-.05f,3.2f,.1f)},new[]{.1f,.08f,.05f},7,new Color(.36f,.28f,.22f),new Color(.4f,.32f,.24f));
                    var r=new System.Random(seed);for(int k=0;k<8;k++)s.Slot(3).Lump(foot+new Vector3(((float)r.NextDouble()-.5f)*1.4f,2.9f+(float)r.NextDouble()*.9f,((float)r.NextDouble()-.5f)*1.4f),new Vector3(.6f,.46f,.6f),seed*9+k,.3f,new Color(.36f,.5f,.32f),new Color(.2f,.3f,.22f),9,5);
                    ZoneKit.Block(blocks,foot+Vector3.up*.6f,new Vector3(1.9f,1.2f,1.2f),0,"Gimbal tub");
                }
                foreach(var (x,z,yaw) in new[]{(17f,88.6f,90f),(28.6f,95.6f,180f),(36f,85.2f,0f)}){Bench(s,At(x,z),yaw);ZoneKit.Block(blocks,At(x,z,.45f),new Vector3(1.9f,.9f,.5f),yaw,"Bench");}
                var drawn=Made("The sky-garden",s,root,true,Wood,Leaf,Iron,Leaf,Brass);drawn.transform.SetParent(blocks.transform,true);
                // Tam's doorframe with the names cut in it, stood up in the park.
                var df=new ZoneKit.Shape();var dp=At(TamAt.x+1.6f,TamAt.y+1.2f);foreach(float dx in new[]{-.5f,.5f})df.Box(dp+new Vector3(dx,1.05f,0),new Vector3(.14f,2.1f,.14f),Quaternion.identity,new Color(.6f,.46f,.32f));
                df.Box(dp+new Vector3(0,2.15f,0),new Vector3(1.14f,.14f,.14f),Quaternion.identity,new Color(.6f,.46f,.32f));for(int k=0;k<6;k++)df.Box(dp+new Vector3(-.5f,.9f+k*.14f,-.075f),new Vector3(.1f,.012f,.01f),Quaternion.identity,new Color(.3f,.22f,.16f));
                var frame=Made("Tam's doorframe, names cut in it",df,root,true,Wood);ZoneKit.Block(frame,dp+new Vector3(-.5f,1.05f,0),new Vector3(.16f,2.1f,.16f),0,"Frame");ZoneKit.Block(frame,dp+new Vector3(.5f,1.05f,0),new Vector3(.16f,2.1f,.16f),0,"Frame");
                foreach(var (x,z,light) in new[]{(10.6f,84.4f,true),(30f,83.4f,false),(48.6f,99.6f,true),(36.4f,90.6f,true)})LampPost(new Vector2(x,z),3.2f,new Color(.36f,.36f,.4f),light,root);
            }

            // ---------------------------------------------------------------- the convoy, moored round the town: every hull a place the player knows
            static void Convoy()
            {
                // Sizes are lengths: a hull is its length long, whatever its height.
                void Hull(string key,Vector3 at,float size,float yaw,Color tint){if(!ZoneKit.Landed(key))return;var hull=kit.Loose(key,at,Tall(key,size),Quaternion.Euler(0,yaw,0),tint,false);hull.name="The convoy, moored: "+key;hull.transform.SetParent(inSpace,true);}
                Hull("TallowStationHull",new Vector3(-30,-6,126),26,10,new Color(.9f,.86f,.8f));Hull("Hauler",new Vector3(26,-4,124),20,-15,new Color(.86f,.74f,.62f));
                Hull("SeedBarge",new Vector3(70,-5,80),12,80,new Color(.86f,.94f,.8f));Hull("CrawlTread",new Vector3(76,-7,108),14,30,new Color(.8f,.76f,.72f));
                Hull("RingSegmentA",new Vector3(-78,-6,98),14,70,new Color(.84f,.86f,.9f));Hull("ReedRaft",new Vector3(-74,-3,60),9,20,new Color(.94f,.88f,.7f));Hull("ReedRaft",new Vector3(-72,-4,40),8,80,new Color(.9f,.86f,.72f));
                Hull("SeedHouse",new Vector3(-74,-5,12),9,30,new Color(.9f,.96f,.86f));Hull("TallowMooringCoupler",new Vector3(66,-6,48),8,40,new Color(.86f,.86f,.9f));
                Hull("RingSegmentB",new Vector3(70,-8,-6),12,-20,new Color(.88f,.84f,.86f));Hull("PatrolCutter",new Vector3(-50,-6,-10),7,40,new Color(.92f,.9f,.86f));
            }

            // ---------------------------------------------------------------- everyone with a name
            static readonly Dictionary<string,GameObject[]> showsFor=new();
            static readonly Dictionary<string,Npc> byId=new();
            static void Neighbours()
            {
                var root=Root("Neighbours");byId.Clear();
                foreach(var (id,node,at,yaw,visit,extras) in People)
                {
                    var holder=new GameObject("Neighbour: "+DisplayNames.Speaker(id)).transform;holder.SetParent(root,true);
                    var npc=kit.Person(id,new Vector3(at.x,0,at.y),yaw,"",false,null,null,node??extras[0].node);
                    npc.repeatNode=node??extras[0].node;npc.postNode="";npc.postFlag="";npc.finalFlag="";npc.finalNode="";npc.transform.SetParent(holder,true);
                    foreach(var label in npc.GetComponentsInChildren<TextMeshPro>(true))label.fontSize=Mathf.Max(label.fontSize,Type(26)/Mathf.Max(.01f,label.transform.lossyScale.y));
                    named.Add(npc);byId[id]=npc;
                    var home=holder.gameObject.AddComponent<Homecoming>();home.speaker=id;home.npc=npc;home.onlyExtras=node==null;
                    home.extras=extras.Select(e=>new Homecoming.Extra{node=e.node,needs="quest."+e.quest+".complete",shows=showsFor.TryGetValue(e.node,out var shows)?shows:Array.Empty<GameObject>()}).ToArray();
                    if(visit){var point=kit.Discovery("Homecoming: "+DisplayNames.Speaker(id),new Vector3(at.x,0,at.y),"Talk to "+DisplayNames.Speaker(id),"end.visit"+(id=="Keeper"?"Abri":id),"Homecoming",node,id,"Homecomings",0,3.2f);point.transform.SetParent(holder,true);home.point=point;}
                    if(id=="Orrin")home.quietFrom="end.handrail";
                }
            }

            // ---------------------------------------------------------------- the story: the arrival, the messages, the terminal, the moorings, the handrail, the line, the last scene
            static void Story()
            {
                var root=Root("The epilogue");
                var all=new GameObject("The epilogue begins on arriving",typeof(FlagAll)).GetComponent<FlagAll>();all.flags=new[]{"visited.NeighbourhoodAfloat"};all.result="end.epilogue";all.transform.SetParent(root,true);
                foreach(var (what,node,speaker,flag,_,read) in Messages){var p=kit.Discovery("The message board: "+what.ToLowerInvariant(),new Vector3(read.x,0,read.y),"Read the message",flag,"",node,speaker,null,0,1.9f);p.transform.SetParent(root,true);}
                var aumi=kit.Discovery("The gallery lantern: Aumi's message",new Vector3(AumiLanternAt.x,0,AumiLanternAt.y),"Read the message","end.msgAumi","","PellAumisMessage","Pell",null,0,2.6f);aumi.transform.SetParent(root,true);
                // The travel terminal: Bryn and Sela's return leg, the survey's offer (the same flag as Many Hands' chart table: offered once, heard once), the road filed once it's flown.
                var leg=kit.Discovery("The travel terminal: the return leg",new Vector3(ReturnLegAt.x,0,ReturnLegAt.y),"Stop and listen","end.routeFiled","","SelaReturnLeg","Sela",null,0,2.2f);leg.transform.SetParent(root,true);
                var offer=kit.Discovery("The travel terminal: the nursery on the chart",new Vector3(SurveyOfferAt.x,0,SurveyOfferAt.y),"Look at the nursery on the chart","sela.surveyOffered","","SelaLastSurveyOffer","Sela",null,0,2.2f);
                offer.requiredFlag="end.planSaid";offer.transform.SetParent(root,true);
                // Waits on the road being flown, not on the quest's step: LastSurvey's step 1 is the talk's flag, and a point that waits on a step counts only an Interact step's report.
                var home=kit.Discovery("The travel terminal: the road filed",new Vector3(SurveyHomeAt.x,0,SurveyHomeAt.y),"Put the road on the chart","sela.surveyFiled","SurveyHome","SelaSurveyHome","Sela",null,0,2.2f);
                home.requiredFlag="end.surveyFlown";home.transform.SetParent(root,true);
                // The moorings: Idra's cutter and Irun's ship on the one cleat, and her crew's word after if her crew was brought home.
                var cleat=kit.Discovery("The moorings: one cleat",new Vector3(MooringsAt.x,0,MooringsAt.y),"Look at the moorings","end.cleatSeen","","IdraTiedUp","Idra",null,0,2.6f);cleat.transform.SetParent(root,true);
                var idra=new GameObject("Idra at the moorings",typeof(Homecoming)).GetComponent<Homecoming>();idra.transform.SetParent(root,true);idra.speaker="Idra";idra.point=cleat;
                idra.extras=new[]{new Homecoming.Extra{node="IdraHomeCrew",needs="quest.CutterDown.complete",shows=Array.Empty<GameObject>()}};
                // The handrail, from Orrin's offer; then the line, from the handrail. Both are step 14's arrow.
                var rail=kit.Discovery("The garden stair: the handrail",new Vector3(RailMendAt.x,0,RailMendAt.y),"Mend the handrail","end.railMended","Handrail",null,"Taren",null,0,2.4f);rail.requiredFlag="end.handrail";rail.transform.SetParent(root,true);
                var paint=kit.Discovery("The fruit stall: the line",new Vector3(RepaintAt.x,0,RepaintAt.y),"Repaint the line","end.linePainted","Handrail",null,"Taren",null,0,2.4f);paint.requiredFlag="end.railMended";paint.transform.SetParent(root,true);
                Gate("The handrail mended",new[]{railMended},"end.railMended",true,root);Gate("The handrail loose",new[]{railLoose},"end.railMended",false,root);
                Gate("The new line",new[]{linesAfter},"end.linePainted",true,root);Gate("The old line",new[]{linesBefore},"end.linePainted",false,root);
                Gate("The pears one crate past it",new[]{pearsAfter},"end.linePainted",true,root);Gate("The pears where the stall stops",new[]{pearsBefore},"end.linePainted",false,root);
                railMended.SetActive(false);linesAfter.SetActive(false);pearsAfter.SetActive(false);
                // The last scene: once the crate is seen past the line, Taren says it; nothing is heard after his last word.
                var last=new GameObject("The last scene: Jun's pears",typeof(AfterTalk)).GetComponent<AfterTalk>();last.transform.position=At(RepaintAt);last.transform.SetParent(root,true);
                last.flag="end.linePainted";last.node="JunLastLine";last.speaker="Taren";last.heardFlag="game.complete";last.delay=2.5f;last.range=14;
                var quiet=new GameObject("Nothing after his last word",typeof(QuietAfter)).GetComponent<QuietAfter>();quiet.flag="end.linePainted";quiet.seconds=1e6f;quiet.transform.SetParent(root,true);
            }

            // ---------------------------------------------------------------- townsfolk
            static void Townsfolk()
            {
                var root=Root("Town life");Physics.SyncTransforms();var inside=new List<string>();
                foreach(var f in Folk)
                {
                    var p=new Vector2(f.x,f.z);
                    // Their feet on the deck, not in a bed or a bench: a child put down in the sky-garden's bed was "sunk 0.77 m" (NothingFloatsOrSinksIn).
                    if(Physics.CheckCapsule(At(p,.45f),At(p,1.4f),.3f,~0,QueryTriggerInteraction.Ignore))inside.Add($"{f.doing} ({f.x}, {f.z})");
                    TownLife.Stand(kit,root,new TownLife.Folk("Townsperson: "+f.doing,f.body,f.x,f.z,f.yaw,f.tint,f.build,f.carries));
                }
                if(inside.Count>0)throw new InvalidOperationException("NeighbourhoodAfloat: townsfolk stand inside something: "+string.Join("; ",inside));
                foreach(var e in Errands)TownLife.Walk(kit,root,new TownLife.Errand(e.name,e.body,e.loop,e.pause,e.tint,e.back,e.build,e.carries));
                // Every errand's loop clear of anything solid at a body's height: a walker stands still for good at whatever is in its way.
                Physics.SyncTransforms();var blocked=new List<string>();
                foreach(var e in Errands)
                    for(int k=0;k<e.loop.Length-(e.back?1:0);k++)
                    {
                        var a=At(e.loop[k],.95f);var next=e.loop[(k+1)%e.loop.Length];var b=At(next,.95f);var run=b-a;run.y=0;
                        foreach(var hit in Physics.SphereCastAll(a,.4f,run.normalized,run.magnitude,~0,QueryTriggerInteraction.Ignore))
                            if(hit.collider.GetComponentInParent<WalkLoop>()==null)blocked.Add($"{e.name}: {hit.collider.name} at ({hit.point.x:0.0}, {hit.point.z:0.0})");
                    }
                if(blocked.Count>0)throw new InvalidOperationException("NeighbourhoodAfloat: an errand runs into something: "+string.Join("; ",blocked.Distinct()));
            }

            // ---------------------------------------------------------------- what a busy town leaves about, and who stands about in it
            /// <summary>Nothing solid within <paramref name="r"/> of a body's middle here (a sphere clear of the floor).</summary>
            static bool Clear(Vector2 p,float r){Physics.SyncTransforms();return !Physics.CheckSphere(At(p,1.2f),r,~0,QueryTriggerInteraction.Ignore);}
            static bool NearBasin(Vector2 p)=>p.x>BasinX0-1.6f&&p.x<BasinX1+1.6f&&p.y>BasinZ0-1.6f&&p.y<BasinZ1+1.6f;
            /// <summary>On this make's deck, clear of its rails by <paramref name="margin"/>, at its level, off the stair and away from the basin.</summary>
            static bool OnDeck(Vector2 p,Make make,float margin)
            {
                foreach(var d in Decks)if(d.make==make&&p.x>d.x0+margin&&p.x<d.x1-margin&&p.y>d.z0+margin&&p.y<d.z1-margin)
                    return !InStair(p)&&!InBasin(p)&&!NearBasin(p)&&Mathf.Abs(Height(p)-d.level)<.01f&&Mathf.Abs(StairX0-p.x)>1.4f;
                return false;
            }
            static bool OnErrand(Vector2 p){foreach(var e in Errands)for(int k=1;k<e.loop.Length;k++)if(ZoneKit.Segment(p,e.loop[k-1],e.loop[k])<1.9f)return true;return false;}
            static bool Free(Vector2 p,Make make,float margin)=>OnDeck(p,make,margin)&&!KeepsOff(p)&&!OnErrand(p);
            /// <summary>Clusters of a district's own clutter (crates and barrels on the quays, planters and tables on the arcade), each where nothing walks, stands or is looked at.</summary>
            static void Dress(Make make,int clusters,int seed,Transform parent,Color? tint,params (string key,float size,float spread)[] kinds)
            {
                var rng=new System.Random(seed);var d=Decks.First(x=>x.make==make);int placed=0;
                for(int tries=0;placed<clusters&&tries<clusters*40;tries++)
                {
                    var c=new Vector2(Mathf.Lerp(d.x0,d.x1,(float)rng.NextDouble()),Mathf.Lerp(d.z0,d.z1,(float)rng.NextDouble()));
                    if(!Free(c,make,2.8f)||!Clear(c,1))continue;
                    placed++;int n=1+rng.Next(3);
                    for(int k=0;k<n;k++)
                    {
                        var (key,size,spread)=kinds[rng.Next(kinds.Length)];var p=k==0?c:c+new Vector2(((float)rng.NextDouble()-.5f)*2*spread,((float)rng.NextDouble()-.5f)*2*spread);
                        float s=size*(.85f+.3f*(float)rng.NextDouble()),yaw=(float)rng.NextDouble()*360;
                        if(k>0&&(!Free(p,make,2.6f)||!Clear(p,.5f)))continue;
                        var go=Prop(key,p,s,yaw,parent);go.name="Dressing: "+key;
                        if(tint is Color t)Tint(go,Color.Lerp(t,Color.white,(float)rng.NextDouble()*.45f));
                    }
                }
            }
            /// <summary>Each district's clutter in its own kind, then its people.</summary>
            static void Dressing()
            {
                var root=Root("Dressing");var folk=Root("Town life, standing about");crowd.Clear();
                Color cargo=new(.9f,.84f,.74f),reed=new(.92f,.82f,.56f);
                Dress(Make.Quay,20,201,root,cargo,("DeckCrate",.8f,1.2f),("MusterCrate",.9f,1.2f),("DressCargoPallet",1.1f,1.4f),("DressCableDrum",1.2f,1.2f),("DressFuelTank",1.5f,1),("DressPowerCell",1.2f,1.2f),("HushwellCrates",1.1f,1.2f),("DressWorkLamp",1.8f,1));
                Dress(Make.Market,12,202,root,cargo,("DeckCrate",.7f,1),("DressCargoPallet",.9f,1.2f),("DressPlanter",.8f,1),("HushwellCrates",1,1.2f),("MusterCrate",.8f,1),("DressShelfRack",1.6f,1));
                Dress(Make.Arcade,14,203,root,null,("DressPlanter",.9f,1.2f),("DressGalleyTable",.9f,1.4f),("DressFanShrub",1.4f,1),("DressSucculent",.7f,1),("DeckCrate",.7f,1.2f),("DressGalleyTable",.9f,1.4f));
                Dress(Make.Square,12,204,root,null,("DressPlanter",.9f,1.2f),("DressFanShrub",1.5f,1.2f),("DeckCrate",.7f,1.2f),("DressGalleyTable",.9f,1.4f),("DressSucculent",.7f,1),("DressGalleyTable",.9f,1.4f));
                Dress(Make.LowStreet,9,205,root,reed,("DressWaterBarrels",1,1.2f),("DressCargoPallet",.9f,1.4f),("HushwellCrates",1,1.2f),("DressCableDrum",1,1.2f),("DeckCrate",.7f,1));
                Dress(Make.Terrace,16,206,root,null,("DressPlanter",.8f,1),("DressFanShrub",1.3f,1),("DressGalleyTable",.9f,1.4f),("DressSucculent",.6f,1),("DeckCrate",.7f,1),("DressPlanter",.8f,1));
                Dress(Make.Gallery,8,207,root,null,("DressPlanter",.8f,1),("DressFanShrub",1.3f,1),("LanternShard",1.2f,1),("DressSucculent",.6f,1));
                Dress(Make.SkyGarden,16,208,root,null,("DressPlanter",.9f,1.2f),("DressFanShrub",1.5f,1.2f),("DressSucculent",.7f,1),("DressGalleyTable",.9f,1.4f),("DressPlanter",.9f,1.2f));
                Dress(Make.Garden,5,209,root,null,("DressPlanter",.8f,1),("DressWaterBarrels",1,1.2f),("DeckCrate",.6f,1));
                Crowd(Make.Quay,13,301,folk);Crowd(Make.Market,8,302,folk);Crowd(Make.Arcade,9,303,folk);Crowd(Make.Square,9,304,folk);Crowd(Make.LowStreet,5,305,folk);
                Crowd(Make.Terrace,8,306,folk);Crowd(Make.Gallery,5,307,folk);Crowd(Make.SkyGarden,9,308,folk);Crowd(Make.Garden,3,309,folk);
                Debug.Log($"AFLOAT_DRESSED crowd={crowd.Count}");
            }
            static readonly List<(string body,Vector2 at)> crowd=new();
            static readonly string[] Carried={"crate","pack","coil","parcel"};
            /// <summary>Who stands about in a district: the bodies of people who stayed behind, each turned to the street, no two of one body near each other, a few in twos talking,
            /// some carrying, some children. Off every way, out of the camera's line to anyone with a name, off the spots and the errands.</summary>
            static void Crowd(Make make,int count,int seed,Transform root)
            {
                var rng=new System.Random(seed);var d=Decks.First(x=>x.make==make);int placed=0;
                bool Taken(string body,Vector2 p)=>crowd.Any(c=>c.body==body&&Vector2.Distance(c.at,p)<4.5f)||Folk.Any(f=>f.body==body&&Vector2.Distance(new Vector2(f.x,f.z),p)<4.5f);
                // Clear() looks at chest height; a raised bed or a bench is lower than that, and a child stood in the sky-garden's bed (NothingFloatsOrSinksIn): their feet too.
                bool Room(Vector2 p)=>Free(p,make,2.7f)&&Clear(p,.6f)&&!Physics.CheckCapsule(At(p,.45f),At(p,1.7f),.34f,~0,QueryTriggerInteraction.Ignore)&&!crowd.Any(c=>Vector2.Distance(c.at,p)<1)&&!Folk.Any(f=>Vector2.Distance(new Vector2(f.x,f.z),p)<1)&&!People.Any(n=>Vector2.Distance(n.at,p)<2.2f);
                for(int tries=0;placed<count&&tries<count*50;tries++)
                {
                    var p=new Vector2(Mathf.Lerp(d.x0,d.x1,(float)rng.NextDouble()),Mathf.Lerp(d.z0,d.z1,(float)rng.NextDouble()));
                    string body=Bodies[rng.Next(Bodies.Length)];if(!Room(p)||Taken(body,p))continue;
                    // Turned toward the nearest way, where the town goes by.
                    Vector2 near=p;float best=float.PositiveInfinity;
                    foreach(var way in Ways)for(int i=1;i<way.Length;i++){var a=way[i-1];var b=way[i];var ab=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(.0001f,ab.sqrMagnitude));var q=a+ab*t;float dd=Vector2.Distance(p,q);if(dd<best){best=dd;near=q;}}
                    float yaw=Mathf.Atan2(near.x-p.x,near.y-p.y)*Mathf.Rad2Deg+(float)(rng.NextDouble()-.5)*50;
                    bool child=rng.NextDouble()<.14;string carries=!child&&rng.NextDouble()<.3?Carried[rng.Next(Carried.Length)]:null;
                    var tint=new Color(.84f+.22f*(float)rng.NextDouble(),.84f+.22f*(float)rng.NextDouble(),.84f+.22f*(float)rng.NextDouble());
                    TownLife.Stand(kit,root,new TownLife.Folk("Townsperson: about the "+make.ToString().ToLowerInvariant(),body,p.x,p.y,yaw,tint,child?.62f:1,carries));crowd.Add((body,p));placed++;Physics.SyncTransforms();
                    // Half the time somebody to talk to, facing them.
                    if(rng.NextDouble()<.45)
                    {
                        float a=yaw*Mathf.Deg2Rad;var q=p+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*1.15f;string other=Bodies[rng.Next(Bodies.Length)];
                        if(!Room(q)||Taken(other,q)||other==body)continue;
                        TownLife.Stand(kit,root,new TownLife.Folk("Townsperson: talking, about the "+make.ToString().ToLowerInvariant(),other,q.x,q.y,yaw+180,new Color(.9f,.92f+.1f*(float)rng.NextDouble(),.88f),1));crowd.Add((other,q));placed++;Physics.SyncTransforms();
                    }
                }
            }
            /// <summary>What people have done to the floor: loading bays and oil on the quay, stall bays and drains on the market's plating, doormats before the cabins, the herders' count
            /// ring under the rib, rugs at the arcade's shopfronts. Flat, laid a finger over each deck.</summary>
            static void Marks()
            {
                var root=Root("Floor marks");var s=new ZoneKit.Shape();var rng=new System.Random(121);
                void Rect(Vector2 c,Vector2 size,float yaw,Color tone,float y){var t=Quaternion.Euler(0,yaw,0);var h=new Vector3(size.x*.5f,0,size.y*.5f);var o=new Vector3(c.x,y,c.y);
                    s.Quad(o+t*new Vector3(-h.x,0,-h.z),o+t*new Vector3(-h.x,0,h.z),o+t*new Vector3(h.x,0,h.z),o+t*new Vector3(h.x,0,-h.z),Vector3.up,tone,.5f);}
                void Outline(Vector2 c,Vector2 size,Color tone,float y,float w=.12f){Rect(c+new Vector2(0,size.y*.5f),new Vector2(size.x,w),0,tone,y);Rect(c-new Vector2(0,size.y*.5f),new Vector2(size.x,w),0,tone,y);
                    Rect(c+new Vector2(size.x*.5f,0),new Vector2(w,size.y),0,tone,y);Rect(c-new Vector2(size.x*.5f,0),new Vector2(w,size.y),0,tone,y);}
                // The quay: two loading bays outlined in yellow with black chevrons, oil where the ships' cargo stands, a berth number painted at each gangway.
                foreach(var (c,size) in new[]{(new Vector2(-17.6f,-3),new Vector2(6,9)),(new Vector2(17.6f,1.6f),new Vector2(5,6))})
                {
                    Outline(c,size,new Color(.94f,.74f,.2f),.032f,.16f);
                    for(float x=-size.x*.5f+.4f;x<size.x*.5f-.2f;x+=.8f)Rect(c+new Vector2(x,-size.y*.5f+.35f),new Vector2(.36f,.5f),35,new Color(.16f,.16f,.18f),.034f);
                }
                
                // The market's plating: bays round each stall, drains in rows.
                foreach(var c in new[]{new Vector2(-17.4f,23),new Vector2(-17.6f,17.8f),new Vector2(-15.6f,37.2f),new Vector2(-1,37.2f),new Vector2(4.2f,37.2f),new Vector2(-11.2f,36.6f),new Vector2(-4,17.4f)})
                    Outline(c,new Vector2(4.6f,2.6f),new Color(.9f,.74f,.24f),.032f);
                foreach(var c in new[]{new Vector2(3.6f,27.4f),new Vector2(3.6f,33.4f)})Outline(c,new Vector2(2.6f,4.6f),new Color(.9f,.74f,.24f),.032f);
                for(float x=-20;x<6;x+=5.2f)foreach(float z in new[]{20.4f,34.6f}){var p=new Vector2(x,z);if(FromWays(p)<.6f)continue;Rect(p,new Vector2(.8f,.8f),0,new Color(.2f,.22f,.26f),.031f);for(int b=0;b<4;b++)Rect(p+new Vector2(-.3f+b*.2f,0),new Vector2(.06f,.7f),0,new Color(.36f,.4f,.46f),.033f);}
                // Doormats before Tallow's cabins, each its own weave.
                foreach(var z in new[]{51.6f,56.4f,61.2f,66f,70.8f})Rect(new Vector2(51.6f,z+.8f),new Vector2(.9f,1.3f),0,Color.HSVToRGB((z*.037f)%1,.35f,.7f),.03f);
                foreach(var x in new[]{26.6f,31.2f,35.8f,40.4f})Rect(new Vector2(x-.8f,69.6f),new Vector2(1.3f,.9f),0,Color.HSVToRGB((x*.031f)%1,.35f,.72f),.03f);
                // The herders' count ring under the rib: a circle of notches, one for each who came.
                for(int k=0;k<61;k++){float a=k*Mathf.PI*2/61;var p=new Vector2(RibAt.x+Mathf.Cos(a)*4.6f,RibAt.y-.6f+Mathf.Sin(a)*3);Rect(p,new Vector2(.1f,.5f),-a*Mathf.Rad2Deg,new Color(.42f,.3f,.24f),.035f);}
                // Rugs before the arcade's open fronts.
                foreach(var (x,c) in new[]{(10.3f,new Color(.7f,.36f,.3f)),(22.3f,new Color(.3f,.42f,.62f)),(31f,new Color(.62f,.5f,.28f)),(35.2f,new Color(.4f,.56f,.38f))})Rect(new Vector2(x,39.2f),new Vector2(3,1.2f),0,c,.034f);
                Made("Floor detail: what people have done to the decks",s,root,false,Paint); // not "... floor": the terrain's own mesh is the one " floor" (FloorAndWallMeetInShade)
            }
            /// <summary>The quay's own: containers stacked at its two ends, in a dozen hulls' paints, and a shelter of benches under an awning where people wait for the terminal.</summary>
            static void QuayGoods(Transform root)
            {
                var s=new ZoneKit.Shape();var blocks=new GameObject("Containers on the quay");blocks.transform.SetParent(root,true);
                Color[] paints={new(.66f,.32f,.24f),new(.82f,.62f,.3f),new(.44f,.5f,.56f),new(.24f,.56f,.58f),new(.46f,.6f,.38f),new(.26f,.34f,.52f)};int pi=0;
                void Container(Vector3 foot,float yaw,Color paint){var t=Quaternion.Euler(0,yaw,0);s.Slot(0).Box(foot+Vector3.up*1.3f,new Vector3(2.44f,2.6f,6.1f),t,paint,ZoneKit.Shape.Dim(paint,1.1f),.36f);
                    for(int k=0;k<4;k++)s.Slot(1).Box(foot+t*new Vector3(-.85f+k*.57f,1.3f,3.07f),new Vector3(.07f,2.4f,.07f),t,new Color(.16f,.16f,.18f));}
                foreach(var (x,z,yaw,high) in new[]{(-23.6f,-9.2f,0f,2),(-23.6f,2f,0f,3),(23.6f,1.4f,0f,2)})
                {for(int h=0;h<high;h++)Container(new Vector3(x+(h==1?.15f:0),h*2.62f,z),yaw+(h==1?2:0),paints[pi++%paints.Length]);ZoneKit.Block(blocks,new Vector3(x,high*1.31f,z),new Vector3(2.5f,high*2.62f,6.2f),yaw,"Container stack");}
                var drawn=Made("Containers on the quay",s,blocks.transform,true,Corrugated,Dark);ZoneKit.Fadeable(drawn);
                // The waiting shelter: an awning on four posts, two benches under it, bags on one.
                var w=new ZoneKit.Shape();var c=new Vector3(-10,0,-6.4f);Color post=new(.36f,.4f,.46f),cloth=new(.3f,.56f,.58f);
                foreach(var (dx,dz) in new[]{(-2.2f,-1.1f),(2.2f,-1.1f),(-2.2f,1.1f),(2.2f,1.1f)})w.Slot(0).Post(c+new Vector3(dx,0,dz),dz<0?2.4f:2.8f,.07f,.06f,6,post,post);
                for(int k=0;k<6;k++){float a0=k/6f,a1=(k+1)/6f;var A=c+new Vector3(-2.5f,2.85f,1.3f);var B=c+new Vector3(2.5f,2.85f,1.3f);var C=c+new Vector3(2.5f,2.4f,-1.3f);var D=c+new Vector3(-2.5f,2.4f,-1.3f);
                    w.Slot(1).Quad(Vector3.Lerp(A,B,a0),Vector3.Lerp(A,B,a1),Vector3.Lerp(D,C,a1),Vector3.Lerp(D,C,a0),Vector3.up,k%2==0?cloth:Color.Lerp(cloth,Color.white,.6f),.8f);}
                Bench(w,c+new Vector3(-1.1f,0,.4f),180);Bench(w,c+new Vector3(1.1f,0,.4f),180);
                w.Slot(1).Lump(c+new Vector3(1.6f,.62f,.4f),new Vector3(.3f,.18f,.2f),3,.15f,new Color(.6f,.5f,.4f),new Color(.4f,.32f,.26f),8,4);
                var shelter=Made("The waiting shelter",w,root,true,Iron,Cloth);ZoneKit.Block(shelter,c+new Vector3(0,.45f,.4f),new Vector3(4.2f,.9f,.6f),0,"Benches");
                foreach(var (dx,dz) in new[]{(-2.2f,-1.1f),(2.2f,-1.1f),(-2.2f,1.1f),(2.2f,1.1f)})ZoneKit.Block(shelter,c+new Vector3(dx,1.2f,dz),new Vector3(.16f,2.4f,.16f),0,"Post");
            }

            // ---------------------------------------------------------------- finishing
            /// <summary>A block made before the piece it stands for goes under the drawn piece it was made for (no invisible walls).</summary>
            static void Unhide()
            {
                foreach(var box in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
                {
                    if(box.isTrigger||box.GetComponentInParent<Renderer>()!=null||box.GetComponentInChildren<Renderer>()!=null||box.transform.parent==null)continue;
                    var drawn=box.transform.parent.GetComponentInChildren<Renderer>();if(drawn!=null)box.transform.SetParent(drawn.transform,true);
                }
            }
            /// <summary>Must-reach: every place a player stands, by the plan. Every person is out of the bake and carves the paths only where they stand.</summary>
            static void Bake()
            {
                var points=new List<Vector2>(Spots());foreach(var w in Ways)points.Add(w[w.Length-1]);
                var people=UnityEngine.Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).Select(n=>n.gameObject).ToArray();
                foreach(var one in people)if(one.GetComponent<NavMeshObstacle>()==null){var hole=one.AddComponent<NavMeshObstacle>();hole.shape=NavMeshObstacleShape.Capsule;hole.radius=.4f;hole.height=1.9f;hole.center=Vector3.up*.95f;hole.carving=true;hole.carveOnlyStationary=true;}
                foreach(var one in people)one.SetActive(false);
                try
                {
                    kit.BakeNavigation(new Bounds(new Vector3(-1,2,43),new Vector3(124,12,134)),Arrival,Array.Empty<Vector2>());
                    var data=UnityEngine.Object.FindFirstObjectByType<GroundNavigation>().data;var instance=NavMesh.AddNavMeshData(data);var cut=new List<string>();
                    try
                    {
                        NavMesh.SamplePosition(At(Arrival),out var start,2,NavMesh.AllAreas);
                        foreach(var p in points.Distinct())
                        {
                            var path=new NavMeshPath();
                            if(!NavMesh.SamplePosition(At(p),out var hit,2.5f,NavMesh.AllAreas)||!NavMesh.CalculatePath(start.position,hit.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)cut.Add(p.ToString());
                        }
                        // Each way walks as drawn: a leg whose path is far longer than the leg is shut somewhere (a cabin stood across the sky-garden gangway's foot,
                        // and every end was still reached the long way round).
                        foreach(var way in Ways)for(int k=1;k<way.Length;k++)
                        {
                            var path=new NavMeshPath();float straight=Vector2.Distance(way[k-1],way[k]);
                            if(!NavMesh.SamplePosition(At(way[k-1]),out var from,2.5f,NavMesh.AllAreas)||!NavMesh.SamplePosition(At(way[k]),out var to,2.5f,NavMesh.AllAreas)
                                ||!NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete){cut.Add($"{way[k-1]}>{way[k]}");continue;}
                            float walked=0;for(int c=1;c<path.corners.Length;c++)walked+=Vector3.Distance(path.corners[c-1],path.corners[c]);
                            if(walked>straight*1.5f+6)cut.Add($"{way[k-1]}>{way[k]} walks {walked:0} m for {straight:0}");
                        }
                        if(cut.Count>0)throw new InvalidOperationException("NeighbourhoodAfloat: a way doesn't walk as drawn: "+string.Join(" ",cut));
                        foreach(var one in people)one.SetActive(true);
                        var holes=EdgeHoles();
                        if(holes.Count>0)Debug.LogWarning($"AFLOAT_EDGE_HOLES {holes.Count}: "+string.Join(" | ",holes.Take(40)));else Debug.Log("AFLOAT_EDGES_OK");
                    }
                    finally{instance.Remove();}
                    if(cut.Count>0)throw new InvalidOperationException("NeighbourhoodAfloat: no path from the arrival to "+string.Join(" ",cut));
                    Debug.Log($"AFLOAT_ROUTES_OK points={points.Distinct().Count()}");
                }
                finally{foreach(var one in people)one.SetActive(true);}
            }

            /// <summary>The shared edge walk (QaPlanWorldTests.NoWalkableEdgeDropsIntoNothing) over every rim edge of the bake, not a stride of them: from each, half a metre at a
            /// time outward, standing on whatever is highest underfoot, stopped by anything a body can't fit past. Where it finds no floor at all, it says where, and what it stood on
            /// to get there: a crate by a rail, or a head, is a step over the rail.</summary>
            static List<string> EdgeHoles()
            {
                Physics.SyncTransforms();
                var mesh=NavMesh.CalculateTriangulation();var v=mesh.vertices;var t=mesh.indices;
                Vector3Int Key(Vector3 p)=>new(Mathf.RoundToInt(p.x*20),Mathf.RoundToInt(p.y*20),Mathf.RoundToInt(p.z*20));
                var edges=new Dictionary<(Vector3Int,Vector3Int),(int count,Vector3 mid,Vector3 outward)>();
                for(int i=0;i+2<t.Length;i+=3)for(int e=0;e<3;e++)
                {
                    Vector3 a=v[t[i+e]],b=v[t[i+(e+1)%3]],c=v[t[i+(e+2)%3]];var along=b-a;along.y=0;if(along.sqrMagnitude<.01f)continue;
                    var toC=c-a;toC.y=0;var outward=new Vector3(-along.z,0,along.x).normalized;if(Vector3.Dot(outward,toC)>0)outward=-outward;
                    var ka=Key(a);var kb=Key(b);bool order=ka.x<kb.x||ka.x==kb.x&&(ka.y<kb.y||ka.y==kb.y&&ka.z<=kb.z);var key=order?(ka,kb):(kb,ka);
                    edges[key]=edges.TryGetValue(key,out var seen)?(seen.count+1,seen.mid,seen.outward):(1,(a+b)*.5f,outward);
                }
                var rim=edges.Values.Where(e=>e.count==1).Select(e=>(e.mid,e.outward)).ToList();
                // In play each person carves a hole round them (NavMeshObstacle), whose rim looks in at them: walk in from every side, over them and on.
                foreach(var npc in UnityEngine.Object.FindObjectsByType<Npc>(FindObjectsSortMode.None))for(int k=0;k<16;k++)
                {var dir=Quaternion.Euler(0,k*22.5f,0)*Vector3.forward;var from=npc.transform.position-dir*.75f;if(NavMesh.SamplePosition(from,out var on,.5f,NavMesh.AllAreas))rim.Add((on.position,dir));}
                // Only what the party can reach, as the test reads it: a roof's own scrap of bake is nobody's edge.
                NavMesh.SamplePosition(At(Arrival),out var start,2,NavMesh.AllAreas);var path=new NavMeshPath();
                rim=rim.Where(e=>NavMesh.SamplePosition(e.mid,out var on,.25f,NavMesh.AllAreas)&&NavMesh.CalculatePath(start.position,on.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete).ToList();
                var holes=new List<string>();var touching=new Collider[32];
                foreach(var (mid,outward) in rim)
                {
                    float y=mid.y;var trail=new List<string>();
                    for(float d=.5f;d<=3;d+=.5f)
                    {
                        var p=mid+outward*d;p.y=y;float ground=float.NegativeInfinity;Collider under=null;
                        foreach(var hit in Physics.RaycastAll(p+Vector3.up*2,Vector3.down,62,~0,QueryTriggerInteraction.Ignore))if(hit.point.y>ground){ground=hit.point.y;under=hit.collider;}
                        if(under==null){holes.Add($"({p.x:0.0}, {p.z:0.0}) from ({mid.x:0.0}, {mid.z:0.0}) over {string.Join(" > ",trail)}");break;}
                        var feet=new Vector3(p.x,ground,p.z);
                        if(Physics.OverlapCapsuleNonAlloc(feet+Vector3.up*.47f,feet+Vector3.up*1.55f,.33f,touching,~0,QueryTriggerInteraction.Ignore)>0)break;
                        if(y-ground>6)break;
                        if(ground>y+.05f||trail.Count==0)trail.Add($"{under.name}@{ground:0.0}");
                        y=ground;
                    }
                }
                return holes;
            }

            // ---------------------------------------------------------------- the stall's stock, and LookDev's stills
            internal static void Stock()
            {
                ProgressionBuilder.Stall(AfloatZone,"PITCH ELEVEN",Sell("RepairGel",30),Sell("RepairSalve",72),Sell("ChargeCell",34),Sell("ChargeBank",72),Sell("SyncTonic",104),Sell("RelinkSpike",104),
                    Sell("PlatingWax",60),Sell("SurgeDraught",60),Sell("Quickening",60),Sell("EmittersT5",1480),Sell("EdgesT5",1560),Sell("ModulesT5",1600),Sell("ScrapAlloy",16,3),Sell("HaloRivet",54,3));
            }
            /// <summary>LookDev's stills (registered in LookDev.Views): the arrival, each district as it is first walked and each place a player stops, the last scene, wide views.</summary>
            internal static LookDev.View[] Views=>new[]
            {
                V("arrival",Arrival),V("terminal",new(12,-4.4f)),V("moorings",new(-8,-10)),V("market",new(-6,28)),V("stall",new(-13.6f,22.6f)),V("stair",new(-18,26)),V("forge",new(-15.6f,32.4f)),
                V("pitch-eleven",new(-14.6f,17.4f)),V("arcade",new(22,26)),V("arcade-east",new(36,26)),V("garden",new(-33,29),GardenTop),V("square",new(-2,58)),V("ruva",new(-9,55)),
                V("board",new(7,53.6f)),V("meret",new(13.6f,55)),V("orrin",new(-11,67)),V("rib",new(0,60)),V("low-street",new(-33,62)),V("basin",new(-38,68)),V("terrace",new(36,60)),
                V("oda",new(44,57)),V("gallery",new(-26,88)),V("gallery-east",new(-14,88)),V("sky-garden",new(26,89)),V("tam",new(34,91)),
                new LookDev.View("overview",new Vector3(0,0,40),false,4.4f),new LookDev.View("overview-north",new Vector3(0,0,80),false,3.2f),new LookDev.View("overview-south",new Vector3(0,0,14),false,2.6f),
            };
            static LookDev.View V(string name,Vector2 at,float floor=0)=>new(name,new Vector3(at.x,floor,at.y));
        }
    }
}
