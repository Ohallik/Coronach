using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// The Meridian Crawl (town, safe; brief, "MeridianCrawl (town, safe) and Terminator"): a town on enormous treads walking the twilight band of a tidally locked world, west,
    /// ahead of the dawn, for two hundred years. Map brief, plan, coordinates and the contract with the Terminator: docs/maps/MeridianCrawl.md. The town engineer's file; everything
    /// of the zone's own is in <see cref="Crawl"/>, so nothing here can collide with the other chapter 9 files' members.
    /// <para>The deck never moves in the frame; the world goes past it (<see cref="CrawlMotion"/>): the plain slides east under the town, the treads' belts run and their wheels turn,
    /// everything that hangs swings together. <c>-crawl-greybox</c> builds the decks, rails, treads, ways, services and story without the dressing (the blockout);
    /// <c>-crawl-home</c> builds it as it stands once tread-hall four is back and Lo is on it, for stills only (never saved as the zone: rebuild without it).</para>
    /// </summary>
    public static partial class HomeBuilder
    {
        /// <summary>The Crawl's own stall (opened by its <c>Shop</c>, not a talk: the script gives the town no seller), its bed, and a dark floor's plates.</summary>
        static void CrawlDefinition(ZoneDef z)
        {
            z.brightFloor=false;
            // The Lashed Stall: provisions at chapter 9's prices, the light Choirwrought frame and the scout drive a town that travels light would sell, rivets and scrap by the three.
            ProgressionBuilder.Stall(CrawlZone,"THE LASHED STALL",Sell("RepairGel",30),Sell("RepairSalve",72),Sell("ChargeCell",34),Sell("ChargeBank",72),Sell("SyncTonic",104),Sell("RelinkSpike",104),
                Sell("PlatingWax",60),Sell("SurgeDraught",60),Sell("Quickening",60),Sell("FramesT5Light",1520),Sell("DrivesT5Scout",1400),Sell("ScrapAlloy",16,3),Sell("HaloRivet",54,3));
            var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(Crawl.Bed);if(bed!=null){z.ambientAudio=bed;z.ambientVolume=Crawl.BedLevel;}
        }
        /// <summary>What really stands in the town: the stall, the tread shop's bench, the oil shop's cradle (RouteChartTests loads the zone and looks).</summary>
        static void CrawlStop(ChartStop s){s.shop=s.bench=s.repair=true;}
        /// <summary>The dawn is always behind the town, low in the east-south-east, and never moves: a warm key at sixteen degrees, so every shadow is long and points west-north-west;
        /// a cold blue fill from the night ahead so iron stays iron in the shade; a little violet haze far off. Lamps and lit windows bloom; nothing else does.</summary>
        [ZoneLook.Source] static void CrawlLooks()
        {
            ZoneLook.Register(CrawlZone,new ZoneLook.Spec{sun=C(1f,.79f,.6f),sunIntensity=1.6f,sunPitch=Crawl.SunPitch,shadowStrength=.8f,
                sky=C(.36f,.41f,.58f),equator=C(.47f,.42f,.45f),ground=C(.17f,.14f,.15f),
                fog=true,fogColor=C(.52f,.45f,.52f),fogStart=70,fogEnd=260,
                contrast=12,saturation=6,splitShadows=C(.4f,.45f,.62f),splitHighlights=C(.64f,.53f,.42f),
                bloomThreshold=.95f,bloomIntensity=.55f,vignette=.22f});
        }

        internal static class Crawl
        {
            // ---------------------------------------------------------------- the plan (docs/maps/MeridianCrawl.md): x east, z north; the town walks west, the dawn is east
            internal const float Deck=0,Plain=-7.5f,Raised=2.4f,CliffTop=4,Speed=1.4f,SunPitch=16,SunYaw=-75;
            /// <summary>The treads: the belt's top run over the deck, its width, a plate's length; the plain's texture repeat (a whole number of plates, so the ruts' prints line up).</summary>
            internal const float BeltTop=1.1f,BeltWide=3,BeltThick=.35f,Pitch=.9f,PlainTile=21.6f;
            internal const string Bed="Assets/_Project/Audio/Ambience/AMB_MeridianCrawl.ogg";internal const float BedLevel=.286f;
            /// <summary>The halls coupled end to end, head first: name, deck rectangle, and the seconds its lurch follows the head's (each rides its own treads).</summary>
            internal static readonly (string name,float x0,float x1,float z0,float z1,float lag)[] Halls=
                {("Helm",-88,-60,-9,9,0),("Market",-54,-20,-10,10,.35f),("Yard",-14,18,-10,10,.7f),("Garden",24,60,-10,10,1.05f)};
            /// <summary>The coupling gaps between halls, each crossed by a bridge on the spine (z -6 to 0), the plain open below.</summary>
            internal static readonly (float x0,float x1)[] Couplings={(-60,-54),(-20,-14),(18,24)};
            internal const float SpineZ=-3,BridgeSouth=-6,BridgeNorth=0,Lip=60,HallWest=66;
            /// <summary>The raised parts: the helm platform and its ramp; the sky-garden and its stair.</summary>
            internal static readonly Rect HelmDeck=Rect.MinMaxRect(-88,-7,-74,7),HelmRamp=Rect.MinMaxRect(-74,-2.4f,-66,2.4f),GardenDeck=Rect.MinMaxRect(28,2.5f,50,10),GardenStair=Rect.MinMaxRect(35.5f,-2.5f,40.5f,2.5f);
            /// <summary>Tread-hall four's porch and the gangway onto it, once it is back (ridden in: <c>home.rideDone</c>, set by the Terminator's ride).</summary>
            internal static readonly Rect Porch=Rect.MinMaxRect(66,-7,72,1),Gangway=Rect.MinMaxRect(59.7f,-6,66.2f,0);
            internal const float PadRadius=3.4f;
            internal static readonly Vector2 Arrival=new(2,1.2f),Pad=new(2,6),YardSpawn=new(-9,1.6f),Cradle=new(-9,5.2f),TreadShop=new(11.5f,5.4f),PostRack=new(12.5f,-8.9f),ParcelHand=new(12.5f,-5.6f),
                Stall=new(-37,1.4f),StallHand=new(-37,-.6f),TamAt=new(-41,-6.8f),TamHand=new(-41,-5),DorrowAt=new(-79,.6f),DorrowHand=new(-77,.2f),HelmFoot=new(-66,0),HelmHead=new(-74,0),
                StairFoot=new(38,-2.5f),StairHead=new(38,2.5f),MiraLot=new(47,8.6f),MiraHand=new(46.5f,6.4f),Tail=new(56,-3),DropCage=new(60.8f,5.4f),CageHand=new(58.4f,5.4f),
                GapSpawn=new(55.5f,-1),DorrowLip=new(58.4f,.8f),LoAt=new(70,-1.2f),LoHand=new(68.6f,-3),LoBench=new(70.4f,-4.8f),HeskAt=new(70.6f,-5.6f);
            /// <summary>The ways feet go: nothing is put down within reach of them (<see cref="NothingStandsOnTheWays"/>), the ground kit keeps them clear and paints them worn, the bake joins them.</summary>
            internal static readonly Vector2[][] Ways=
            {
                // The spine starts a step east of the helm ramp's foot, and the way up turns onto the ramp from in front of it: walked north along x -66 it ran down the ramp's side rail.
                new[]{new Vector2(-64.5f,-3),new Vector2(-60,-3),new Vector2(-54,-3),new Vector2(-20,-3),new Vector2(-14,-3),new Vector2(18,-3),new Vector2(24,-3),new Vector2(58,-3)},
                new[]{new Vector2(-64.5f,-3),new Vector2(-64.5f,0),HelmFoot,HelmHead,DorrowHand},
                new[]{new Vector2(-37,-3),StallHand},new[]{new Vector2(-41,-3),TamHand},
                new[]{new Vector2(2,-3),Arrival,new Vector2(2,3)},new[]{new Vector2(-9,-3),YardSpawn,new Vector2(-9,3.4f)},
                new[]{new Vector2(11.5f,-3),new Vector2(11.5f,3.6f)},new[]{new Vector2(12.5f,-3),ParcelHand},
                new[]{new Vector2(38,-3),StairFoot,StairHead,new Vector2(38,4.6f),MiraHand},
                new[]{new Vector2(55,-3),new Vector2(55,2),CageHand},
            };
            /// <summary>Onto tread-hall four, walked only once it is back: held to the same rule.</summary>
            internal static readonly Vector2[] ToTheHall={new(58,-3),new(66,-3),LoHand};
            internal static readonly (Vector2 at,float half)[] Signs={(new(-69.5f,-6.4f),1.4f),(new(-28,-7),3.4f),(new(33.2f,-6.6f),2.8f),(new(54,-7.6f),3.8f)};
            static readonly string[] SignWords={"HELM","MARKET STREET","SKY-GARDEN","TREAD-HALL FOUR"};

            /// <summary>LookDev's stills (registered in LookDev.Views): where a player arrives and each place where a player stops, at its floor's height, the couplings as they are
            /// crossed, and wide views.</summary>
            internal static LookDev.View[] Views=>new[]
            {
                V("arrival",Arrival),V("pad",Pad+new Vector2(-1,-2.6f)),V("yard-west",new Vector2(-9,2)),V("tread-shop",new Vector2(11.5f,3)),V("post-rack",new Vector2(12.5f,-5)),
                V("yard-rail",new Vector2(-2,6.5f)),V("coupling-yard",new Vector2(21,-3)),V("coupling-market",new Vector2(-17,-3)),V("coupling-helm",new Vector2(-57,-3)),
                V("market-east",new Vector2(-25,-3)),V("rail-walk",new Vector2(-25,6)),V("stall",StallHand),V("tam",TamHand),V("market-west",new Vector2(-48,-3)),
                V("helm-foot",new Vector2(-64,-2)),V("helm",DorrowHand,Raised),V("helm-north",new Vector2(-76,3.2f),Raised),
                V("garden-foot",new Vector2(32,-3)),V("stair",StairHead,Raised),V("sky-garden",new Vector2(41,6),Raised),V("mira-lot",MiraHand,Raised),
                V("tail",Tail),V("drop-cage",CageHand),V("gap",GapSpawn),V("tread-hall-four",LoHand),
                new LookDev.View("overview",new Vector3(-10,0,4),false,4.6f),new LookDev.View("overview-tail",new Vector3(50,0,0),false,2.6f),new LookDev.View("overview-head",new Vector3(-62,0,0),false,2.6f),
            };
            static LookDev.View V(string name,Vector2 at,float floor=Deck)=>new(name,new Vector3(at.x,floor,at.y));

            // ---------------------------------------------------------------- the ground: decks at 0, the helm platform and the sky-garden at 2.4, the plain 7.5 m under everything
            static float Smooth(float a,float b,float x)=>ZoneKit.Smooth(a,b,x);
            static float Inside(Vector2 p,float x0,float x1,float z0,float z1)=>Mathf.Min(Mathf.Min(p.x-x0,x1-p.x),Mathf.Min(p.y-z0,z1-p.y));
            static float Inside(Vector2 p,Rect r)=>Inside(p,r.xMin,r.xMax,r.yMin,r.yMax);
            static float Step(float inside)=>Smooth(-.2f,.2f,inside);
            /// <summary>Metres inside the nearest hall's deck; below nought, over the plain.</summary>
            internal static float OnDeck(Vector2 p){float d=float.NegativeInfinity;foreach(var h in Halls)d=Mathf.Max(d,Inside(p,h.x0,h.x1,h.z0,h.z1));return d;}
            static int HallAt(Vector2 p){int best=0;float d=float.NegativeInfinity;for(int i=0;i<Halls.Length;i++){var h=Halls[i];float e=Inside(p,h.x0,h.x1,h.z0,h.z1);if(e>d){d=e;best=i;}}return best;}
            internal static float Height(Vector2 p)
            {
                float y=Mathf.Lerp(Deck,Raised,Step(Inside(p,HelmDeck)));
                // The ramp and the stair run half a metre on into the raised deck they climb to, at its height: where both edges eased off at one seam the floor dipped .6 m in .4,
                // a notch the bake couldn't climb, so nobody reached the helm.
                var ramp=Rect.MinMaxRect(HelmRamp.xMin-.5f,HelmRamp.yMin,HelmRamp.xMax,HelmRamp.yMax);var stairs=Rect.MinMaxRect(GardenStair.xMin,GardenStair.yMin,GardenStair.xMax,GardenStair.yMax+.5f);
                y=Mathf.Lerp(y,Mathf.Lerp(Deck,Raised,Mathf.InverseLerp(HelmRamp.xMax,HelmRamp.xMin,p.x)),Step(Inside(p,ramp)));
                y=Mathf.Lerp(y,Raised,Step(Inside(p,GardenDeck)));
                y=Mathf.Lerp(y,Mathf.Lerp(Deck,Raised,Mathf.InverseLerp(GardenStair.yMin,GardenStair.yMax,p.y)),Step(Inside(p,stairs)));
                return Mathf.Lerp(Plain,y,Step(OnDeck(p)));
            }
            static float FromWays(Vector2 p){float d=float.PositiveInfinity;foreach(var way in Ways)d=Mathf.Min(d,ZoneKit.FromPath(p,way));return d;}
            /// <summary>The deck's own colour: each hall's plate a little its own (the helm's bluer, the market's warmer), paler where feet go, grimed toward the rails.</summary>
            static Color Tone(Vector2 p)
            {
                if(OnDeck(p)<-.3f)return new Color(.3f,.27f,.25f);
                float mottle=.92f+.16f*Mathf.PerlinNoise(p.x*.21f+3,p.y*.21f+9),patch=Mathf.PerlinNoise(p.x*.07f+5,p.y*.07f+1);
                var tone=HallAt(p) switch{0=>new Color(.6f,.61f,.66f),1=>new Color(.68f,.62f,.56f),2=>new Color(.62f,.61f,.6f),_=>new Color(.62f,.63f,.58f)};
                if(Inside(p,GardenDeck)>-.2f)tone=new Color(.66f,.6f,.52f);
                tone=Color.Lerp(tone,tone*1.08f,patch);
                float worn=1-Smooth(.8f,2.6f,FromWays(p));tone=Color.Lerp(tone,new Color(.84f,.8f,.74f),worn*.38f);
                tone*=Mathf.Lerp(.72f,1,Smooth(.15f,1.6f,OnDeck(p)));
                return new Color(tone.r*mottle,tone.g*mottle,tone.b*mottle,1);
            }
            /// <summary>On a coupling's bridge (the spine's way over each gap), or the gangway onto tread-hall four.</summary>
            internal static bool OnABridge(Vector2 p){foreach(var (x0,x1) in Couplings)if(p.x>=x0-.6f&&p.x<=x1+.6f&&p.y>=BridgeSouth&&p.y<=BridgeNorth)return true;return p.x>=Gangway.xMin-.1f&&p.x<=Gangway.xMax&&p.y>=Gangway.yMin&&p.y<=Gangway.yMax;}
            /// <summary>Refuses anything put down off a deck, on a ramp's side or on a riser: a coordinate a metre out is a body in mid-air over the plain.</summary>
            static void OnTheDeck(ZoneKit kit,string what,params Vector2[] spots)
            {
                foreach(var at in spots)
                {
                    // A way crosses each coupling gap on its bridge, which is a block over the plain and not the landform.
                    if(OnABridge(at))continue;
                    float y=kit.Y(at.x,at.y),spread=0;
                    foreach(var (dx,dz) in new[]{(.6f,0f),(-.6f,0f),(0f,.6f),(0f,-.6f)})spread=Mathf.Max(spread,Mathf.Abs(kit.Y(at.x+dx,at.y+dz)-y));
                    bool ramp=Inside(at,HelmRamp)>-.1f||Inside(at,GardenStair)>-.1f;
                    if(OnDeck(at)<.4f||(!ramp&&spread>.25f))throw new InvalidOperationException($"MeridianCrawl: {what} at {at} is not on a deck (height {y:0.00}, rise {spread:0.00} over .6 m, {OnDeck(at):0.00} m in from the edge)");
                }
            }

            // ---------------------------------------------------------------- what the town is made of
            internal static Material Plate,Skirt,Iron,Brass,Timber,Cloth,Window,Lamp,Glass,Dark,Soil,Leaf,Rope,Belt,PlainLook,Track,Stone,Scrub,Copper;
            static Material[] coats;static readonly string[] CoatNames={"rust","olive","slate","soot","teal"};
            static float Hash(int a,int b,int seed){uint h=(uint)(a*374761393+b*668265263+seed*1442695041);h=(h^(h>>13))*1274126177;h^=h>>16;return (h&0xffff)/65535f;}
            internal static void Materials()
            {
                // The deck: riveted plate laid in courses, each plate its own length and tone, a dark seam and a row of rivets round every edge; sixteen metres to a repeat.
                const float Span=16;var lay=new System.Random(91);var courses=new List<(float z0,float z1,float[] breaks,float[] tones)>();
                for(float at=0;at<Span-.01f;)
                {
                    float deep=Mathf.Min(Span-at,.8f+(float)lay.NextDouble()*.5f);if(Span-at-deep<.7f)deep=Span-at;
                    var breaks=new List<float>{0};var tones=new List<float>();float x=(float)lay.NextDouble()*1.8f;
                    // Plates a metre to two long and each its own tone: at two to three metres and a narrow spread of tone the yard read as one plain sheet (look pass 01).
                    while(x<Span-1.1f){breaks.Add(x);tones.Add(.44f+.26f*(float)lay.NextDouble());x+=1.0f+(float)lay.NextDouble()*1.2f;}
                    tones.Add(.44f+.26f*(float)lay.NextDouble());courses.Add((at,at+deep,breaks.ToArray(),tones.ToArray()));at+=deep;
                }
                float Rivet(float along,float start,float across){float d=Mathf.Repeat(along-start,.2f)-.1f;return Mathf.Sqrt(d*d+across*across);}
                var plate=ZoneKit.Texture("Crawl/crawl-plate",1024,(u,v)=>
                {
                    float mx=u*Span,my=v*Span;var c=courses[courses.Count-1];foreach(var each in courses)if(my<each.z1){c=each;break;}
                    int k=0;for(int i=1;i<c.breaks.Length;i++)if(mx>=c.breaks[i])k=i;
                    float x0=c.breaks[k],x1=k+1<c.breaks.Length?c.breaks[k+1]:Span+c.breaks[1];
                    float left=mx-x0,right=x1-mx,bottom=my-c.z0,top=c.z1-my,edge=Mathf.Min(Mathf.Min(left,right),Mathf.Min(bottom,top));
                    float tone=c.tones[Mathf.Min(k,c.tones.Length-1)]+.07f*(ZoneKit.Tile(u*200,v*200,200,3)-.5f)+.07f*(ZoneKit.Cloud(u,v,10,5)-.5f);
                    // Scuffed bright in streaks along the way feet go, pitted here and there.
                    tone+=.05f*(ZoneKit.Tile(u*400,v*24,400,8)-.5f);if(ZoneKit.Tile(u*320,v*320,320,9)>.94f)tone-=.08f;
                    tone-=edge<.018f?.3f:edge<.05f?.06f:0;
                    float r=Mathf.Min(Mathf.Min(Rivet(mx,x0+.1f,bottom-.08f),Rivet(mx,x0+.1f,top-.08f)),Mathf.Min(Rivet(my,c.z0+.1f,left-.08f),Rivet(my,c.z0+.1f,right-.08f)));
                    if(r<.02f)tone+=.13f;else if(r<.032f)tone-=.11f;
                    // Some plates newer patches in a warmer primer, some weathered to rust; a darker bloom of grime toward one corner of each.
                    float own=Hash((int)(c.z0*10),k,17);var tint=own>.86f?new Color(1.1f,.92f,.8f):own<.1f?new Color(.9f,.95f,1.04f):Color.white;
                    tone*=.9f+.14f*Mathf.Clamp01((left+bottom)/2.4f);
                    return new Color(tone*tint.r,tone*.985f*tint.g,tone*.965f*tint.b,1);
                });
                // The skirts down to the plain and the sky-garden's face: plate panels in courses, rivets along every seam, a lit porthole in every other panel of the hold.
                var skirtLit=new Color[512*512];
                var skirt=ZoneKit.Texture("Crawl/crawl-skirt",512,(u,v)=>
                {
                    float mu=u*7,mv=v*5;int col=Mathf.FloorToInt(mu/1.4f),row=Mathf.FloorToInt(mv/1.25f);float pu=mu-col*1.4f,pv=mv-row*1.25f;
                    float edge=Mathf.Min(Mathf.Min(pu,1.4f-pu),Mathf.Min(pv,1.25f-pv));
                    float tone=.42f+.14f*Hash(col,row,5)+.08f*(ZoneKit.Tile(u*96,v*96,96,4)-.5f)+.05f*(ZoneKit.Cloud(u,v,6,2)-.5f)+.05f*(pv/1.25f-.5f);
                    tone-=edge<.02f?.28f:edge<.05f?.06f:0;
                    float r=Mathf.Min(Mathf.Min(Rivet(mu,col*1.4f+.1f,pv-.07f),Rivet(mu,col*1.4f+.1f,1.25f-pv-.07f)),Mathf.Min(Rivet(mv,row*1.25f+.1f,pu-.07f),Rivet(mv,row*1.25f+.1f,1.4f-pu-.07f)));
                    if(r<.02f)tone+=.12f;else if(r<.032f)tone-=.1f;
                    float d=Vector2.Distance(new Vector2(pu,pv),new Vector2(.7f,.62f));bool port=row==2&&(col%2==0);
                    int px=Mathf.Min(511,(int)(u*512)),py=Mathf.Min(511,(int)(v*512));skirtLit[py*512+px]=Color.black;
                    if(port&&d<.26f)
                    {
                        if(d>.2f)return new Color(.86f,.66f,.38f,1)*(.85f+.3f*(1-(d-.2f)/.06f));
                        skirtLit[py*512+px]=new Color(1,.72f,.38f,1)*(1-.5f*d/.2f);return new Color(.95f,.74f,.46f,1);
                    }
                    return new Color(tone,tone*.98f,tone*.96f,1);
                });
                var skirtGlow=ZoneKit.Texture("Crawl/crawl-skirt-glow",512,(u,v)=>skirtLit[Mathf.Min(511,(int)(v*512))*512+Mathf.Min(511,(int)(u*512))]);
                // Built iron: plate with rivet rows, painted near neutral so a shape's vertex colour says what it is.
                var iron=ZoneKit.Texture("Crawl/crawl-iron",256,(u,v)=>
                {
                    float mu=u*2,mv=v*2;float pu=Mathf.Repeat(mu,1),pv=Mathf.Repeat(mv,.5f);
                    float tone=.6f+.08f*(ZoneKit.Cloud(u,v,8,3)-.5f)+.06f*(ZoneKit.Tile(u*96,v*96,96,5)-.5f);
                    float edge=Mathf.Min(Mathf.Min(pu,1-pu),Mathf.Min(pv,.5f-pv));tone-=edge<.012f?.22f:0;
                    float r=Mathf.Min(Rivet(mu,.05f,pv-.04f),Rivet(mu,.05f,.5f-pv-.04f));if(r<.014f)tone+=.12f;else if(r<.022f)tone-=.08f;
                    return new Color(tone,tone,tone*.98f,1);
                });
                var timber=ZoneKit.Texture("Crawl/crawl-timber",512,(u,v)=>
                {
                    float row=v*5;int board=Mathf.FloorToInt(row);float across=row-board,seam=Mathf.Min(across,1-across);
                    float own=ZoneKit.Tile(board*3.7f,.5f,5,41),grain=ZoneKit.Tile(u*8+board*2.3f,v*80,80,6);
                    float tone=.58f+.16f*(own-.5f)+.1f*(grain-.5f)-(seam<.05f?.28f:0);return new Color(tone*1.04f,tone,tone*.9f,1);
                });
                var cloth=ZoneKit.Texture("Crawl/crawl-cloth",256,(u,v)=>{float weave=(Mathf.Sin(u*Mathf.PI*96)*Mathf.Sin(v*Mathf.PI*96))*.04f;float tone=.8f+weave+.08f*(ZoneKit.Cloud(u,v,6,4)-.5f);return new Color(tone,tone,tone,1);});
                // A tread plate, one along u: the plate with its raised grouser bar across it, bolted at both edges; the gap between plates dark.
                var belt=ZoneKit.Texture("Crawl/crawl-belt",256,(u,v)=>
                {
                    float tone=.38f+.06f*(ZoneKit.Tile(u*64,v*64,64,2)-.5f);
                    if(u<.05f||u>.95f)return new Color(.08f,.075f,.07f,1);
                    if(u>.38f&&u<.6f)tone+=.16f;else if(u>=.6f&&u<.67f)tone-=.12f;
                    float edge=Mathf.Min(u-.05f,.95f-u);if(edge<.04f)tone+=.1f;
                    if(v<.06f||v>.94f)tone-=.08f;
                    foreach(float bu in new[]{.2f,.82f})foreach(float bv in new[]{.13f,.87f}){float d=new Vector2((u-bu)*1,(v-bv)*3.3f).magnitude;if(d<.035f)return new Color(.86f,.68f,.36f,1);}
                    return new Color(tone,tone*.97f,tone*.94f,1);
                });
                // The plain: a cracked hardpan, pale crust in cells with dark cracks between, grit and stones; painted warm and near neutral, tinted toward the dawn by the mesh.
                var plain=ZoneKit.Texture("Crawl/crawl-plain",1024,(u,v)=>
                {
                    // Cells about two metres across with thin cracks: at eighteen to a repeat, with heavy cracks, the plain read as a paved square (look pass 01).
                    const int P=9;float wob=.18f*(ZoneKit.Cloud(u,v,12,31)-.5f);float cx=u*P+wob,cy=v*P-wob;int ix=Mathf.FloorToInt(cx),iy=Mathf.FloorToInt(cy);float d1=9,d2=9;int own=0;
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {
                        int gx=((ix+dx)%P+P)%P,gy=((iy+dy)%P+P)%P;float fx=ix+dx+.15f+.7f*Hash(gx,gy,11),fy=iy+dy+.15f+.7f*Hash(gx,gy,12);
                        float d=Mathf.Sqrt((cx-fx)*(cx-fx)+(cy-fy)*(cy-fy));if(d<d1){d2=d1;d1=d;own=gx*31+gy;}else if(d<d2)d2=d;
                    }
                    float tone=.58f+.07f*Hash(own,7,3)+.12f*(ZoneKit.Cloud(u,v,14,6)-.5f)+.09f*(ZoneKit.Tile(u*300,v*300,300,7)-.5f);
                    if(d2-d1<.018f)tone-=.2f;else if(d2-d1<.04f)tone-=.05f;
                    float grit=ZoneKit.Cloud(u,v,9,13);if(grit>.62f)tone-=.12f*(grit-.62f)/.38f+(ZoneKit.Tile(u*500,v*500,500,4)>.7f?.08f:0);
                    if(ZoneKit.Tile(u*180,v*180,180,21)>.9f)tone+=.1f;
                    return new Color(tone,tone*.92f,tone*.8f,1);
                });
                // A tread's track in the plain: the grousers' prints across a pressed band, its edges fading out; one print a plate.
                var track=ZoneKit.Texture("Crawl/crawl-track",512,(u,v)=>
                {
                    float across=Mathf.Abs(v-.5f)*2,alpha=(1-Smooth(.74f,.98f,across))*.62f;float print=Mathf.Repeat(u*PlainTile/Pitch,1);
                    float tone=.42f+.06f*(ZoneKit.Tile(u*240,v*24,240,3)-.5f);if(print>.38f&&print<.62f){tone-=.12f;alpha=Mathf.Min(.85f,alpha*1.3f);}
                    return new Color(tone,tone*.9f,tone*.78f,alpha);
                },true,true);
                var stone=ZoneKit.Texture("Crawl/crawl-stone",256,(u,v)=>{float t=.6f+.14f*(ZoneKit.Cloud(u,v,6,8)-.5f)+.08f*(ZoneKit.Tile(u*64,v*64,64,9)-.5f);return new Color(t,t*.95f,t*.9f,1);});
                Plate=ZoneKit.Toon("crawl-plate",plate,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.5f,.52f,.66f));m.SetTextureScale("_BaseMap",new Vector2(.5f,.5f));});
                Skirt=ZoneKit.Toon("crawl-skirt",skirt,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.46f,.48f,.62f));m.SetTexture("_EmissionMap",skirtGlow);m.SetColor("_EmissionColor",new Color(1.6f,1.1f,.6f));});
                Iron=ZoneKit.Toon("crawl-iron",iron,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.48f,.5f,.64f));});
                Brass=ZoneKit.Toon("crawl-brass",iron,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.98f,.76f,.44f));m.SetColor("_RimColor",new Color(1,.86f,.6f));m.SetFloat("_RampSteps",5);});
                Copper=ZoneKit.Toon("crawl-copper",iron,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.86f,.52f,.36f));m.SetFloat("_RampSteps",5);});
                Timber=ZoneKit.Toon("crawl-timber",timber,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.5f,.48f,.62f));});
                Cloth=ZoneKit.Toon("crawl-cloth",cloth,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_ShadowTint",new Color(.52f,.5f,.64f));});
                Window=ZoneKit.Toon("crawl-window",null,m=>{m.SetColor("_BaseColor",new Color(1,.8f,.5f));m.SetColor("_EmissionColor",new Color(1.7f,1.1f,.48f));});
                Lamp=ZoneKit.Toon("crawl-lamp",null,m=>{m.SetColor("_BaseColor",new Color(1,.84f,.56f));m.SetColor("_EmissionColor",new Color(2.3f,1.5f,.62f));});
                Glass=ZoneKit.Toon("crawl-glass",null,m=>{ZoneKit.TwoSided(m);m.SetColor("_BaseColor",new Color(.78f,.86f,.86f));m.SetColor("_EmissionColor",new Color(.16f,.2f,.2f));});
                Dark=ZoneKit.Toon("crawl-dark",null,m=>m.SetColor("_BaseColor",new Color(.07f,.065f,.07f)));
                Soil=ZoneKit.Toon("crawl-soil",stone,m=>{m.SetColor("_BaseColor",new Color(.34f,.26f,.2f));});
                Leaf=ZoneKit.Toon("crawl-leaf",null,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_ShadowTint",new Color(.42f,.46f,.6f));});
                Rope=ZoneKit.Toon("crawl-rope",null,m=>m.SetColor("_BaseColor",new Color(.66f,.58f,.46f)),"Universal Render Pipeline/Unlit");
                Belt=ZoneKit.Toon("crawl-belt",belt,m=>{m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.46f,.48f,.62f));m.SetColor("_RimColor",new Color(1,.8f,.56f));});
                PlainLook=ZoneKit.Toon("crawl-plain",plain,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.5f,.48f,.66f));});
                Track=ZoneKit.Toon("crawl-track",track,m=>
                {
                    m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);
                    m.SetOverrideTag("RenderType","Transparent");m.renderQueue=(int)RenderQueue.Transparent-40;m.SetColor("_ShadowTint",new Color(.5f,.48f,.66f));
                });
                Stone=ZoneKit.Toon("crawl-stone",stone,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.5f,.48f,.66f));});
                Scrub=ZoneKit.Toon("crawl-scrub",null,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_ShadowTint",new Color(.5f,.48f,.62f));});
                Coats();
                AssetDatabase.SaveAssets();
            }
            /// <summary>The Crawlers' coats: Dorrow's body (the one Crawler body there is) re-dyed in its jacket only, so the brass of the goggles and the buckle, the soot of the face and
            /// the dark of the leggings stay as they are: rust, olive, slate, soot and a faded teal. Each a copy of his material with the dyed map.</summary>
            static void Coats()
            {
                string from="Assets/_Project/Art/Generated/Models/DorrowBody/";var body=AssetDatabase.LoadAssetAtPath<Material>(from+"DorrowBody_Toon.mat");
                coats=new Material[CoatNames.Length];if(body==null){Debug.LogWarning("HOME_CRAWL_NO_COATS: DorrowBody's material isn't taken in");return;}
                var source=new Texture2D(2,2);source.LoadImage(File.ReadAllBytes(from+"DorrowBody_base_color.png"));int size=source.width;var px=source.GetPixels();UnityEngine.Object.DestroyImmediate(source);
                var dyes=new (float hue,float sat,float val)[]{(8,1f,.9f),(80,.62f,.84f),(214,.3f,.92f),(28,.22f,.55f),(172,.5f,.8f)};
                for(int c=0;c<CoatNames.Length;c++)
                {
                    var (hue,sat,val)=dyes[c];
                    var map=ZoneKit.Texture("Crawl/crawler-coat-"+CoatNames[c],size,(u,v)=>
                    {
                        var o=px[Mathf.Min(size-1,(int)(v*size))*size+Mathf.Min(size-1,(int)(u*size))];Color.RGBToHSV(o,out float h,out float s,out float val0);float deg=h*360;
                        // The jacket's ochre; the brighter brass (over .64) and the near-black leathers (under .2) are left alone.
                        float k=deg>=16&&deg<=64&&s>.36f?Mathf.Clamp01((s-.36f)/.1f)*Mathf.Clamp01((val0-.2f)/.06f)*Mathf.Clamp01((.64f-val0)/.06f):0;
                        if(k<=0)return o;
                        var dyed=Color.HSVToRGB(hue/360,Mathf.Clamp01(s*sat),Mathf.Clamp01(val0*val));return Color.Lerp(o,dyed,k);
                    });
                    string path="Assets/_Project/Resources/WorldMaterials/crawler-coat-"+CoatNames[c]+".mat";
                    var made=new Material(body){name="crawler-coat-"+CoatNames[c]};made.SetTexture("_BaseMap",map);
                    var prior=AssetDatabase.LoadAssetAtPath<Material>(path);if(prior==null){AssetDatabase.CreateAsset(made,path);prior=made;}else{EditorUtility.CopySerialized(made,prior);UnityEngine.Object.DestroyImmediate(made);EditorUtility.SetDirty(prior);}
                    coats[c]=prior;
                }
            }

            // ---------------------------------------------------------------- the zone
            static bool greybox,home;
            static readonly List<CrawlMotion.Hanging> hanging=new();static readonly List<CrawlMotion.Rolling> rolling=new();static readonly List<Renderer> belts=new(),plainParts=new();
            static readonly List<(string body,Vector2[] loop,bool back,string name)> errands=new();
            internal static void Build()
            {
                greybox=DevArgs.Has("-crawl-greybox");home=DevArgs.Has("-crawl-home");
                hanging.Clear();rolling.Clear();belts.Clear();plainParts.Clear();errands.Clear();
                Materials();
                var kit=ZoneKit.Ground(CrawlZone,Height,CliffTop);kit0=kit;
                var scene=WorldBuilder.Begin(kit.zone);
                // The dawn behind the town, low in the east-south-east: ZoneLook keeps this yaw and sets the pitch.
                var key=GameObject.Find("KeyLight");if(key!=null)key.transform.rotation=Quaternion.Euler(SunPitch,SunYaw,0);
                // The skirts darker from the deck's edge down: at .95 where they met the plate, every deck edge drew the floor and the wall's textures meeting at full contrast.
                var land=kit.Terrain("Crawl decks",-94,96,-18,18,.5f,Plate,Skirt,Tone,new Color(.6f,.58f,.62f),new Color(.68f,.66f,.7f));
                CutThePlain(land);

                OnTheDeck(kit,"someone with a name",DorrowAt,TamAt,DorrowLip);OnTheDeck(kit,"a floor sign",Signs.Select(s=>s.at).ToArray());
                OnTheDeck(kit,"a place to stand",Arrival,YardSpawn,Cradle,TreadShop,StallHand,TamHand,DorrowHand,HelmHead,StairHead,MiraHand,Tail,CageHand,GapSpawn,ParcelHand,new Vector2(2,3));
                foreach(var way in Ways)OnTheDeck(kit,"a way's corner",way);

                WorldBuilder.Spawn("Arrival",kit.On(Arrival.x,Arrival.y));WorldBuilder.Spawn("Yard",kit.On(YardSpawn.x,YardSpawn.y));
                var gap=WorldBuilder.Spawn("Gap",kit.On(GapSpawn.x,GapSpawn.y));gap.transform.rotation=Quaternion.Euler(0,90,0);

                var motion=new GameObject("The Crawl on the move",typeof(CrawlMotion)).GetComponent<CrawlMotion>();motion.speed=Speed;motion.heading=Vector3.right;
                ThePlain(kit,motion);
                Rails(kit);
                BuildCouplings(kit);
                Treads(kit);
                BuildPad(kit);
                Services(kit);
                BuildTail(kit);
                TreadHallFour(kit);
                Alongside(kit);
                if(!greybox){Helm(kit);Market(kit);Yard(kit);Garden(kit);People(kit);}
                for(int i=0;i<Signs.Length;i++)kit.Sign(SignWords[i],Signs[i].at.x,Signs[i].at.y);
                Story(kit);

                motion.plain=plainParts.ToArray();motion.plainTile=PlainTile;motion.belts=belts.ToArray();motion.beltPitch=Pitch;
                motion.hanging=hanging.ToArray();motion.rolling=rolling.ToArray();
                motion.lean=3;motion.swing=5;motion.period=3.6f;motion.jolt=4;motion.joltEvery=9.5f;
                // An editor still shows every hanging thing as play does at the top of its hall's swing, so they lean together in a still.
                foreach(var h in hanging)if(h.pivot!=null){var axis=h.pivot.parent!=null?h.pivot.parent.InverseTransformDirection(h.axis):h.axis;h.pivot.localRotation=Quaternion.AngleAxis((motion.lean+motion.swing)*(h.reach<=0?1:h.reach),axis)*h.rest;}

                SeatBlocks();
                NothingStandsOnTheWays();
                ErrandsWalkClear(kit);
                Bake(kit);
                if(home)Home();
                if(!greybox)Ground(kit,land);
                WorldBuilder.Save(scene,kit.zone);
                Debug.Log("HOME_CRAWL_COORDS arrival="+Arrival+" pad="+Pad+" yard-spawn="+YardSpawn+" cradle="+Cradle+" tread-shop="+TreadShop+" post-rack="+PostRack+" parcel-hand="+ParcelHand
                    +" stall="+Stall+" stall-hand="+StallHand+" tam="+TamAt+" dorrow="+DorrowAt+" dorrow-hand="+DorrowHand+" helm-ramp="+HelmFoot+"->"+HelmHead+" stair="+StairFoot+"->"+StairHead
                    +" mira-lot="+MiraLot+" mira-hand="+MiraHand+" tail="+Tail+" drop-cage="+DropCage+" cage-hand="+CageHand+" gap-spawn="+GapSpawn+" dorrow-lip="+DorrowLip+" lo="+LoAt+" lo-hand="+LoHand
                    +" hanging="+hanging.Count+" belts="+belts.Count+(greybox?" GREYBOX":"")+(home?" HOME-STILLS":""));
                Debug.Log("HOME_CRAWL_OK");
            }
            /// <summary>The landform's floor at the plain's level is cut out of what is drawn: the plain is drawn by its own moving sheet. The collider keeps it, so a body or a ray
            /// that goes over a rail finds ground, and the bake's bounds keep it out of the paths.</summary>
            static void CutThePlain(ZoneKit.Land land)
            {
                var mesh=land.floorMesh;var v=mesh.vertices;var t=mesh.triangles;var keep=new List<int>(t.Length);
                for(int i=0;i+2<t.Length;i+=3)if(v[t[i]].y>Plain+1||v[t[i+1]].y>Plain+1||v[t[i+2]].y>Plain+1){keep.Add(t[i]);keep.Add(t[i+1]);keep.Add(t[i+2]);}
                mesh.SetTriangles(keep,0);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            }
            static Vector3 At(ZoneKit kit,Vector2 p,float up=0)=>kit.On(p.x,p.y,up);
            static Vector3 OnFloor(Vector2 p,float floor,float up=0)=>new(p.x,floor+up,p.y);
            static float LagAt(float x){float lag=0;foreach(var h in Halls)if(x>=h.x0-3)lag=h.lag;return lag;}
            static void Swings(Transform pivot,float reach=1,float extraLag=0)=>hanging.Add(new CrawlMotion.Hanging{pivot=pivot,rest=pivot.localRotation,axis=Vector3.forward,reach=reach,lag=LagAt(pivot.position.x)+extraLag});
            /// <summary>A built shape whose points are about its own origin, stood with that origin at <paramref name="at"/>: what a pivot swings or a wheel turns.</summary>
            static GameObject Pivoted(ZoneKit kit,string name,ZoneKit.Shape shape,Vector3 at,Transform parent,params Material[] materials)
            {
                var go=kit.Built(name,shape,materials);go.transform.position=at;if(parent!=null)go.transform.SetParent(parent,true);return go;
            }
            static int unique;
            static string Name(string what)=>what+" "+(++unique);

            // ---------------------------------------------------------------- the plain going past
            /// <summary>The plain: one sheet from far ahead of the helm to far behind the tail and well out on both sides, warm toward the dawn and darkening toward the night ahead,
            /// whose texture <see cref="CrawlMotion"/> slides east; stones and scrub in strips of the plain carried east and brought round out of sight; and the treads' tracks laid
            /// behind every hall's belts (and the tread-halls alongside), which start at the head: nothing has marked the plain ahead of the town.</summary>
            static void ThePlain(ZoneKit kit,CrawlMotion motion)
            {
                var holder=new GameObject("The plain going past");
                const float X0=-212,X1=212,Z0=-64,Z1=148,Cell=4;int nx=Mathf.RoundToInt((X1-X0)/Cell),nz=Mathf.RoundToInt((Z1-Z0)/Cell);
                var points=new Vector3[(nx+1)*(nz+1)];var uv=new Vector2[points.Length];var tone=new Color[points.Length];var normals=new Vector3[points.Length];
                for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++)
                {
                    int k=j*(nx+1)+i;var p=new Vector3(X0+i*Cell,Plain+.02f,Z0+j*Cell);points[k]=p;uv[k]=new Vector2(p.x/PlainTile,p.z/PlainTile);normals[k]=Vector3.up;tone[k]=Dawn(p);
                }
                var tris=new List<int>();for(int j=0;j<nz;j++)for(int i=0;i<nx;i++){int a=j*(nx+1)+i,b=a+1,c=a+nx+1,d=c+1;tris.AddRange(new[]{a,c,d,a,d,b});}
                var mesh=new Mesh{name="Crawl plain",indexFormat=IndexFormat.UInt32};mesh.vertices=points;mesh.uv=uv;mesh.colors=tone;mesh.normals=normals;mesh.SetTriangles(tris,0);mesh.RecalculateBounds();
                mesh=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+kit.zone+"-Plain.asset");
                var sheet=new GameObject("The plain",typeof(MeshFilter),typeof(MeshRenderer));sheet.GetComponent<MeshFilter>().sharedMesh=mesh;
                var view=sheet.GetComponent<MeshRenderer>();view.sharedMaterial=PlainLook;view.shadowCastingMode=ShadowCastingMode.Off;sheet.transform.SetParent(holder.transform,true);plainParts.Add(view);
                // Tracks: two to a belt's width, behind the town's lanes from the helm's tail, and behind each tread-hall alongside.
                var lanes=new List<(float z,float from)>{(Halls[0].z0-BeltWide*.5f,Halls[0].x1-1),(Halls[0].z1+BeltWide*.5f,Halls[0].x1-1),(-11.5f,Halls[1].x1-1),(11.5f,Halls[1].x1-1)};
                foreach(var (x,z,_,_,length) in AlongsideHalls)foreach(float side in new[]{-1f,1f})lanes.Add((z+side*(AlongsideWide*.5f-1.6f),x+length*.5f-2));
                int n=0;foreach(var (z,from) in lanes)
                {
                    var strip=new Mesh{name="Crawl track"};var sp=new List<Vector3>();var su=new List<Vector2>();var st=new List<int>();var sn=new List<Vector3>();
                    for(float x=from;x<=X1+.1f;x+=8){foreach(float side in new[]{-1f,1f}){sp.Add(new Vector3(x,Plain+.05f,z+side*BeltWide*.5f));su.Add(new Vector2(x/PlainTile,side<0?0:1));sn.Add(Vector3.up);}}
                    for(int i=0;i+3<sp.Count;i+=2)st.AddRange(new[]{i,i+1,i+3,i,i+3,i+2});
                    strip.SetVertices(sp);strip.SetUVs(0,su);strip.SetNormals(sn);strip.SetTriangles(st,0);strip.RecalculateBounds();
                    strip=ZoneKit.SaveMesh(strip,"Assets/_Project/Scenes/"+kit.zone+"-Track-"+(n++)+".asset");
                    var go=new GameObject("Floor detail tread track",typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=strip;var r=go.GetComponent<MeshRenderer>();
                    r.sharedMaterial=Track;r.shadowCastingMode=ShadowCastingMode.Off;go.transform.SetParent(holder.transform,true);plainParts.Add(r);
                }
                // Stones and scrub, in ten strips the motion carries round: never in a lane a belt runs in, and never where a tread-hall alongside rides.
                var rng=new System.Random(23);var tiles=new List<Transform>();const int Tiles=10;float span=(X1-X0)/Tiles;
                bool Clear(float z)=>!(Mathf.Abs(Mathf.Abs(z)-11.5f)<2.2f)&&!(z>21&&z<47);
                for(int k=0;k<Tiles;k++)
                {
                    var s=new ZoneKit.Shape();
                    for(int i=0;i<70;i++)
                    {
                        float x=((float)rng.NextDouble()-.5f)*span,roll=(float)rng.NextDouble(),z=roll<.3f?-8.6f+17.2f*(float)rng.NextDouble():roll<.55f?13.6f+7f*(float)rng.NextDouble():47+50*(float)rng.NextDouble();
                        float kind=(float)rng.NextDouble(),size=.25f+(float)rng.NextDouble()*.9f;int seed=rng.Next(1000);if(!Clear(z))continue;
                        var at=new Vector3(x,Plain,z);float warm=.85f+.25f*(float)rng.NextDouble();
                        if(kind<.5f)s.Slot(0).Lump(at+Vector3.up*size*.3f,new Vector3(size,size*.62f,size*.85f),seed,.32f,new Color(.7f,.64f,.58f)*warm,new Color(.4f,.36f,.34f),9,5,.5f,Quaternion.Euler(0,seed%360,0));
                        else if(kind<.8f){var sr=new System.Random(seed);s.Slot(1).Fern(at,.35f+size*.4f,6,sr,new Color(.46f,.42f,.3f),new Color(.78f,.66f,.44f)*warm);}
                        else for(int q=0;q<4;q++)s.Slot(0).Lump(at+new Vector3(((float)rng.NextDouble()-.5f)*1.4f,.07f,((float)rng.NextDouble()-.5f)*1.4f),Vector3.one*(.08f+.1f*(float)rng.NextDouble()),seed+q,.3f,new Color(.66f,.6f,.54f),new Color(.4f,.36f,.34f),6,3);
                    }
                    var tile=Pivoted(kit,"Floor detail plain stones "+k,s,new Vector3(X0+span*(k+.5f),0,0),holder.transform,Stone,Scrub);tiles.Add(tile.transform);
                }
                motion.drift=tiles.ToArray();motion.driftFrom=X0;motion.driftSpan=X1-X0;
            }
            /// <summary>The plain's light: the dawn's warmth toward the tail, the night's blue toward the head, and dusk closing in far out to either side.</summary>
            static Color Dawn(Vector3 p)
            {
                float east=Smooth(-200,170,p.x),far=Smooth(30,130,Mathf.Abs(p.z-12));
                var c=Color.Lerp(new Color(.5f,.52f,.66f),new Color(1.06f,.9f,.74f),east);return Color.Lerp(c,c*.6f,far);
            }

            // ---------------------------------------------------------------- rails: every deck edge, every coupling bridge, the raised parts
            static IEnumerable<(Vector2 a,Vector2 b,float floor)> RailLines()
            {
                const float In=.25f,NaN=float.NaN;
                for(int i=0;i<Halls.Length;i++)
                {
                    var h=Halls[i];float x0=h.x0+In,x1=h.x1-In,z0=h.z0+In,z1=h.z1-In;
                    yield return (new(x0,z0),new(x1,z0),NaN);
                    if(h.name=="Garden"){yield return (new(x0,z1),new(GardenDeck.xMin-.2f,z1),NaN);yield return (new(GardenDeck.xMin+.2f,z1),new(GardenDeck.xMax-.2f,z1),Raised);yield return (new(GardenDeck.xMax+.2f,z1),new(x1,z1),NaN);}
                    else yield return (new(x0,z0==h.z0+In?z1:z1),new(x1,z1),NaN);
                    bool westOpen=i>0,eastOpen=true;
                    if(h.name=="Helm"){yield return (new(x0,z0),new(x0,HelmDeck.yMin-.2f),NaN);yield return (new(x0,HelmDeck.yMin+.2f),new(x0,HelmDeck.yMax-.2f),Raised);yield return (new(x0,HelmDeck.yMax+.2f),new(x0,z1),NaN);}
                    else if(westOpen){yield return (new(x0,z0),new(x0,BridgeSouth-.2f),NaN);yield return (new(x0,BridgeNorth+.2f),new(x0,z1),NaN);}
                    if(eastOpen){yield return (new(x1,z0),new(x1,BridgeSouth-.2f),NaN);yield return (new(x1,BridgeNorth+.2f),new(x1,z1),NaN);}
                }
                // The helm platform's edges and its ramp's sides; the sky-garden's edges and its stair's sides.
                yield return (new(HelmDeck.xMin+.3f,HelmDeck.yMin+In),new(HelmDeck.xMax-In,HelmDeck.yMin+In),Raised);yield return (new(HelmDeck.xMin+.3f,HelmDeck.yMax-In),new(HelmDeck.xMax-In,HelmDeck.yMax-In),Raised);
                yield return (new(HelmDeck.xMax-In,HelmDeck.yMin+In),new(HelmDeck.xMax-In,HelmRamp.yMin),Raised);yield return (new(HelmDeck.xMax-In,HelmRamp.yMax),new(HelmDeck.xMax-In,HelmDeck.yMax-In),Raised);
                yield return (new(HelmRamp.xMin,HelmRamp.yMin+In),new(HelmRamp.xMax-.6f,HelmRamp.yMin+In),NaN);yield return (new(HelmRamp.xMin,HelmRamp.yMax-In),new(HelmRamp.xMax-.6f,HelmRamp.yMax-In),NaN);
                yield return (new(GardenDeck.xMin+In,GardenDeck.yMin+In),new(GardenStair.xMin,GardenDeck.yMin+In),Raised);yield return (new(GardenStair.xMax,GardenDeck.yMin+In),new(GardenDeck.xMax-In,GardenDeck.yMin+In),Raised);
                yield return (new(GardenDeck.xMin+In,GardenDeck.yMin+In),new(GardenDeck.xMin+In,GardenDeck.yMax-In),Raised);yield return (new(GardenDeck.xMax-In,GardenDeck.yMin+In),new(GardenDeck.xMax-In,GardenDeck.yMax-In),Raised);
                // The stair's side rails start a little way up it: from its foot they ended half a metre off the spine, and a body walking the spine met their ends (route 55).
                yield return (new(GardenStair.xMin+In,GardenStair.yMin+.7f),new(GardenStair.xMin+In,GardenStair.yMax),NaN);yield return (new(GardenStair.xMax-In,GardenStair.yMin+.7f),new(GardenStair.xMax-In,GardenStair.yMax),NaN);
                // The strip of deck behind the helm platform is railed off where it opens: a hero walked in there stood behind 2.4 m of platform from the camera (D272).
                yield return (new(HelmDeck.xMax+.25f,HelmDeck.yMax+.1f),new(HelmDeck.xMax+.25f,Halls[0].z1-.25f),Deck);
                // The coupling bridges' rails, at the deck's height over the gap.
                foreach(var (x0,x1) in Couplings){yield return (new(x0-.2f,BridgeSouth+In),new(x1+.2f,BridgeSouth+In),Deck);yield return (new(x0-.2f,BridgeNorth-In),new(x1+.2f,BridgeNorth-In),Deck);}
            }
            /// <summary>A rail along a line: posts no more than 1.9 m apart, a top and a middle rail, a kick plate, a rope lashed round a post here and there; on a static box a body stops at.
            /// Over a ramp each piece stands on the lowest floor under it and its rails follow the floor.</summary>
            static void Rails(ZoneKit kit)
            {
                var holder=new GameObject("Rails");var shape=new ZoneKit.Shape();Color post=new(.3f,.3f,.33f),rail=new(.46f,.44f,.44f),kick=new(.36f,.35f,.37f);var rng=new System.Random(5);
                foreach(var (a,b,floor) in RailLines())
                {
                    float length=Vector2.Distance(a,b);if(length<.3f)continue;int pieces=Mathf.Max(1,Mathf.CeilToInt(length/1.9f));
                    float Y(Vector2 p)=>float.IsNaN(floor)?kit.Y(p.x,p.y):floor;
                    for(int k=0;k<pieces;k++)
                    {
                        var p0=Vector2.Lerp(a,b,k/(float)pieces);var p1=Vector2.Lerp(a,b,(k+1)/(float)pieces);float y0=Y(p0),y1=Y(p1);
                        var f0=new Vector3(p0.x,y0,p0.y);var f1=new Vector3(p1.x,y1,p1.y);
                        shape.Slot(0).Post(f0,1.12f,.07f,.06f,6,post,post).Slot(1).Disc(f0+Vector3.up*1.13f,Vector3.up,.08f,6,new Color(.9f,.7f,.4f));
                        shape.Slot(0).Beam(f0+Vector3.up*1.05f,f1+Vector3.up*1.05f,.07f,.07f,rail,new Color(.6f,.58f,.56f)).Beam(f0+Vector3.up*.56f,f1+Vector3.up*.56f,.05f,.05f,rail).Beam(f0+Vector3.up*.1f,f1+Vector3.up*.1f,.03f,.18f,kick);
                        if(rng.NextDouble()<.12){for(int turn=0;turn<3;turn++){var ring=new Vector3[9];var thick=new float[9];for(int q=0;q<9;q++){float ang=q*Mathf.PI*2/8;ring[q]=f0+new Vector3(Mathf.Cos(ang)*.1f,.5f+turn*.07f,Mathf.Sin(ang)*.1f);thick[q]=.025f;}shape.Slot(2).Tube(ring,thick,4,new Color(.74f,.64f,.48f),new Color(.74f,.64f,.48f),1,1,false);}}
                        float low=Mathf.Min(y0,y1),high=Mathf.Max(y0,y1);
                        ZoneKit.BlockAlong(holder,new Vector3(p0.x,(low+high)*.5f+.6f,p0.y),new Vector3(p1.x,(low+high)*.5f+.6f,p1.y),.3f,1.2f+(high-low),"Rail");
                        if(k==pieces-1)shape.Slot(0).Post(f1,1.12f,.07f,.06f,6,post,post);
                    }
                }
                kit.Built("Rails drawn",shape,Iron,Brass,Rope).transform.SetParent(holder.transform,true);
            }

            // ---------------------------------------------------------------- the couplings: bridges over the gaps, the drawbars and chains under them
            static void BuildCouplings(ZoneKit kit)
            {
                var holder=new GameObject("Couplings");int n=0;
                foreach(var (x0,x1) in Couplings)
                {
                    var bridge=new GameObject("Coupling bridge "+(++n));bridge.transform.SetParent(holder.transform,true);var s=new ZoneKit.Shape();var rng=new System.Random(40+n);
                    Color plate=new(.5f,.5f,.52f),iron=new(.3f,.3f,.33f);float mid=(x0+x1)*.5f;
                    // The bridge: grating plates across a frame, its two stringers, lapped onto both decks.
                    s.Slot(0).Deck(new Vector3(x0-.35f,0,-3),new Vector3(x1+.35f,0,-3),6,.12f,plate,rng,.55f,.05f);
                    foreach(float z in new[]{BridgeSouth+.1f,BridgeNorth-.1f})s.Slot(0).Beam(new Vector3(x0-.4f,-.3f,z),new Vector3(x1+.4f,-.3f,z),.25f,.45f,iron);
                    // The drawbar under it: a beam from skirt to skirt, its knuckle and pin in the middle; the safety chains each side; hoses and cables slung across.
                    s.Slot(0).Beam(new Vector3(x0-1.2f,-2.5f,-3),new Vector3(x1+1.2f,-2.5f,-3),.9f,.9f,iron).Tube(new[]{new Vector3(mid,-3.3f,-3),new Vector3(mid,-1.7f,-3)},new[]{.7f,.7f},14,new Color(.38f,.36f,.36f),new Color(.44f,.42f,.4f));
                    s.Slot(1).Tube(new[]{new Vector3(mid,-1.6f,-3),new Vector3(mid,-1.45f,-3)},new[]{.32f,.24f},10,Color.white,Color.white);
                    foreach(float z in new[]{-7.5f,1.5f})s.Slot(0).Rope(new Vector3(x0-.2f,-1.2f,z),new Vector3(x1+.2f,-1.2f,z),1.4f,.09f,new Color(.26f,.26f,.28f),6,10);
                    s.Slot(2).Rope(new Vector3(x0-.2f,-.9f,4),new Vector3(x1+.2f,-.9f,4),1.8f,.14f,new Color(.24f,.2f,.18f),8,10).Rope(new Vector3(x0-.2f,-1.6f,6.2f),new Vector3(x1+.2f,-1.6f,6.2f),1.2f,.06f,new Color(.7f,.5f,.3f),5,10);
                    var drawn=kit.Built("Coupling "+n,s,Iron,Brass,Rope);drawn.transform.SetParent(bridge.transform,true);
                    ZoneKit.Block(drawn,new Vector3(mid,-.15f,(BridgeSouth+BridgeNorth)*.5f),new Vector3(x1-x0+.7f,.3f,BridgeNorth-BridgeSouth),0,"Bridge deck");
                }
            }

            // ---------------------------------------------------------------- the treads
            /// <summary>Each hall's two belts, outboard of its north and south rails: the top run 1.1 m over the deck, the bottom on the plain; a sprocket and an idler 7.8 m across
            /// and road wheels between, all turning at the town's pace (<see cref="Turning"/>), the side frame, and the dust trailing off behind.</summary>
            static void Treads(ZoneKit kit)
            {
                var holder=new GameObject("Treads");
                float rc=(BeltTop-Plain)*.5f-BeltThick*.5f,yc=(BeltTop+Plain)*.5f,big=rc-BeltThick*.5f-.06f,small=1.05f;
                var bigWheel=Wheel(kit,"Tread sprocket",big,BeltWide-.3f,true);var road=Wheel(kit,"Tread road wheel",small,BeltWide-.5f,false);
                var dust=DustLook(kit);
                foreach(var h in Halls)foreach(int side in new[]{-1,1})
                {
                    float zc=side<0?h.z0-BeltWide*.5f:h.z1+BeltWide*.5f,xa=h.x0+.5f+rc+BeltThick*.5f,xb=h.x1-.5f-rc-BeltThick*.5f;
                    var tread=new GameObject("Tread, "+h.name+(side<0?" south":" north"));tread.transform.SetParent(holder.transform,true);
                    var belt=BeltMesh(kit,h.name+(side<0?"-S":"-N"),xa,xb,yc,rc,zc);belt.transform.SetParent(tread.transform,true);belts.Add(belt.GetComponent<Renderer>());
                    foreach(float x in new[]{xa,xb})Turn(bigWheel,new Vector3(x,yc,zc),tread.transform,Speed/big);
                    int count=Mathf.Max(1,Mathf.FloorToInt((xb-xa)/2.9f)-1);
                    for(int k=1;k<=count;k++)Turn(road,new Vector3(Mathf.Lerp(xa,xb,k/(float)(count+1)),Plain+BeltThick+small,zc),tread.transform,Speed/small);
                    var frame=new ZoneKit.Shape();float outer=zc+side*(BeltWide*.5f+.12f);Color iron=new(.3f,.3f,.33f);
                    frame.Beam(new Vector3(xa,yc,outer),new Vector3(xb,yc,outer),.3f,.8f,iron,new Color(.5f,.46f,.42f));
                    for(int k=1;k<=count;k++){float x=Mathf.Lerp(xa,xb,k/(float)(count+1));frame.Beam(new Vector3(x,yc,outer),new Vector3(x,Plain+BeltThick+small,outer),.25f,.25f,iron);}
                    foreach(float x in new[]{xa,xb})frame.Disc(new Vector3(x,yc,outer+side*.08f),new Vector3(0,0,side),.55f,14,new Color(.86f,.66f,.38f));
                    kit.Built("Tread frame "+h.name+(side<0?" south":" north"),frame,Iron).transform.SetParent(tread.transform,true);
                    Dust(kit,dust,new Vector3(xb+3.6f,Plain+.6f,zc),tread.transform);
                }
            }
            static void Turn(Mesh mesh,Vector3 at,Transform parent,float radiansPerSecond)
            {
                var go=new GameObject("Turning wheel",typeof(MeshFilter),typeof(MeshRenderer),typeof(Turning));go.transform.position=at;go.transform.SetParent(parent,true);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=new[]{Iron,Brass,Dark};
                var turning=go.GetComponent<Turning>();turning.axis=Vector3.forward;turning.degrees=radiansPerSecond*Mathf.Rad2Deg;
            }
            /// <summary>A wheel about its own centre, its axle along z: a rim, spokes and a hub on both faces, and teeth round a sprocket. Saved once, drawn by every tread.</summary>
            static Mesh Wheel(ZoneKit kit,string name,float radius,float wide,bool sprocket)
            {
                var s=new ZoneKit.Shape();Color iron=new(.32f,.31f,.33f),rim=new(.44f,.42f,.42f);float half=wide*.5f;
                s.Slot(0).Tube(new[]{new Vector3(0,0,-half),new Vector3(0,0,half)},new[]{radius,radius},sprocket?40:20,rim,rim,6,.3f);
                foreach(int f in new[]{-1,1})
                {
                    var face=new Vector3(0,0,f*(half+.02f));
                    s.Slot(2).Disc(face,new Vector3(0,0,f),radius*.86f,sprocket?32:16,new Color(.08f,.08f,.08f));
                    s.Slot(0).Disc(face+new Vector3(0,0,f*.01f),new Vector3(0,0,f),radius,sprocket?40:20,iron,radius*.86f,rim);
                    int spokes=sprocket?6:5;for(int k=0;k<spokes;k++){float a=k*Mathf.PI*2/spokes;var dir=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);s.Slot(0).Beam(face+dir*radius*.18f,face+dir*radius*.88f,radius*.11f,.16f,iron,rim);}
                    s.Slot(1).Disc(face+new Vector3(0,0,f*.1f),new Vector3(0,0,f),radius*.2f,14,Color.white,radius*.08f).Tube(new[]{face,face+new Vector3(0,0,f*.18f)},new[]{radius*.09f,radius*.07f},10,Color.white,Color.white);
                }
                if(sprocket)for(int k=0;k<18;k++){float a=k*Mathf.PI*2/18;var dir=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);s.Slot(0).Box(dir*(radius+.08f),new Vector3(.34f,.22f,wide*.7f),Quaternion.LookRotation(Vector3.forward,dir),rim);}
                var go=kit.Built(name,s,Iron,Brass,Dark);var mesh=go.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(go);return mesh;
            }
            /// <summary>One belt: a loop round its two wheels in the side plane, its plates' u running the way they go (the top run west, the bottom east), starting under the middle
            /// of the bottom run where the seam is never seen. Outer face, inner face and both edges.</summary>
            static GameObject BeltMesh(ZoneKit kit,string id,float xa,float xb,float yc,float rc,float zc)
            {
                var path=new List<(Vector2 p,Vector2 n)>();float mid=(xa+xb)*.5f;
                path.Add((new(mid,yc-rc),Vector2.down));path.Add((new(xb,yc-rc),Vector2.down));
                for(int i=1;i<=24;i++){float a=(-90+180f*i/24)*Mathf.Deg2Rad;var n=new Vector2(Mathf.Cos(a),Mathf.Sin(a));path.Add((new Vector2(xb,yc)+n*rc,n));}
                path.Add((new(xa,yc+rc),Vector2.up));
                for(int i=1;i<=24;i++){float a=(90+180f*i/24)*Mathf.Deg2Rad;var n=new Vector2(Mathf.Cos(a),Mathf.Sin(a));path.Add((new Vector2(xa,yc)+n*rc,n));}
                path.Add((new(mid,yc-rc),Vector2.down));
                var v=new List<Vector3>();var uv=new List<Vector2>();var nm=new List<Vector3>();var t=new List<int>();float run=0;float half=BeltWide*.5f,th=BeltThick*.5f;
                void Face(Func<Vector2,Vector2,float,(Vector3 a,Vector3 b,Vector3 n,float va,float vb)> at)
                {
                    int start=v.Count;run=0;
                    for(int i=0;i<path.Count;i++)
                    {
                        if(i>0)run+=Vector2.Distance(path[i].p,path[i-1].p);
                        var (a,b,n,va,vb)=at(path[i].p,path[i].n,run);v.Add(a);v.Add(b);nm.Add(n);nm.Add(n);uv.Add(new Vector2(run/Pitch,va));uv.Add(new Vector2(run/Pitch,vb));
                    }
                    for(int i=0;i<path.Count-1;i++){int a=start+i*2;t.AddRange(new[]{a,a+1,a+3,a,a+3,a+2});}
                }
                Face((p,n,_)=>(new Vector3(p.x+n.x*th,p.y+n.y*th,zc-half),new Vector3(p.x+n.x*th,p.y+n.y*th,zc+half),new Vector3(n.x,n.y,0),0,1));
                Face((p,n,_)=>(new Vector3(p.x-n.x*th,p.y-n.y*th,zc+half),new Vector3(p.x-n.x*th,p.y-n.y*th,zc-half),new Vector3(-n.x,-n.y,0),0,1));
                Face((p,n,_)=>(new Vector3(p.x+n.x*th,p.y+n.y*th,zc+half),new Vector3(p.x-n.x*th,p.y-n.y*th,zc+half),Vector3.forward,.02f,.1f));
                Face((p,n,_)=>(new Vector3(p.x-n.x*th,p.y-n.y*th,zc-half),new Vector3(p.x+n.x*th,p.y+n.y*th,zc-half),Vector3.back,.02f,.1f));
                // Winding: each quad is made to face its own normal.
                for(int i=0;i<t.Count;i+=3){var a=v[t[i]];var b=v[t[i+1]];var c=v[t[i+2]];if(Vector3.Dot(Vector3.Cross(b-a,c-a),nm[t[i]])<0){int s=t[i+1];t[i+1]=t[i+2];t[i+2]=s;}}
                var mesh=new Mesh{name="Crawl belt "+id};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetNormals(nm);mesh.SetTriangles(t,0);mesh.RecalculateBounds();
                mesh=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+kit.zone+"-Belt-"+id+".asset");
                var go=new GameObject("Tread belt",typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=Belt;return go;
            }
            static Material DustLook(ZoneKit kit)
            {
                string path="Assets/_Project/Scenes/"+kit.zone+"-Dust-soft.mat";var dust=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(dust==null){dust=new Material(Resources.Load<Material>("Effects/flare_01"));AssetDatabase.CreateAsset(dust,path);}
                dust.SetColor("_BaseColor",new Color(.72f,.62f,.52f,.4f));EditorUtility.SetDirty(dust);return dust;
            }
            /// <summary>Dust thrown up behind a belt where it leaves the plain: it rises and stays where it rose on the plain, so it trails away behind the town (world space, carried
            /// east at the plain's own pace).</summary>
            static void Dust(ZoneKit kit,Material look,Vector3 at,Transform parent)
            {
                var go=new GameObject("Tread dust",typeof(ParticleSystem));go.transform.position=at;go.transform.SetParent(parent,true);
                var ps=go.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=ps.main;main.loop=true;main.prewarm=true;main.duration=8;main.startLifetime=new ParticleSystem.MinMaxCurve(6,9);main.startSpeed=new ParticleSystem.MinMaxCurve(.2f,.6f);
                main.startSize=new ParticleSystem.MinMaxCurve(1.4f,3.6f);main.startColor=new Color(.74f,.64f,.54f,.3f);main.maxParticles=90;main.simulationSpace=ParticleSystemSimulationSpace.World;main.playOnAwake=true;
                main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
                var emission=ps.emission;emission.rateOverTime=6;
                var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(1.6f,.4f,BeltWide);
                var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
                velocity.x=new ParticleSystem.MinMaxCurve(Speed*.95f,Speed*1.05f);velocity.y=new ParticleSystem.MinMaxCurve(.25f,.5f);velocity.z=new ParticleSystem.MinMaxCurve(-.2f,.2f);
                var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.6f,1,1.6f));
                var fade=ps.colorOverLifetime;fade.enabled=true;var ramp=new Gradient();
                ramp.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.15f),new GradientAlphaKey(.6f,.6f),new GradientAlphaKey(0,1)});fade.color=ramp;
                var view=go.GetComponent<ParticleSystemRenderer>();view.sharedMaterial=look;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
            }

            // ---------------------------------------------------------------- the pad, where the party lands and launches
            static void BuildPad(ZoneKit kit)
            {
                var holder=new GameObject("Landing pad");var at=At(kit,Pad);holder.transform.position=at;var s=new ZoneKit.Shape();
                Color plate=new(.46f,.46f,.5f),ring=new(.88f,.82f,.66f),iron=new(.3f,.3f,.33f);
                s.Slot(0).Disc(at+Vector3.up*.06f,Vector3.up,PadRadius,36,plate,0,new Color(.52f,.52f,.56f),.25f).Tube(new[]{at+Vector3.down*.1f,at+Vector3.up*.06f},new[]{PadRadius+.05f,PadRadius},36,iron,iron,8,1,false);
                s.Slot(1).Disc(at+Vector3.up*.065f,Vector3.up,PadRadius-.5f,36,ring,PadRadius-.75f).Disc(at+Vector3.up*.065f,Vector3.up,.5f,14,ring);
                // Four mooring clamps at the quarters: a ship lands on a deck that's moving, so it's held down before anyone gets out.
                for(int k=0;k<4;k++)
                {
                    float a=k*Mathf.PI*.5f+.785f;var q=at+new Vector3(Mathf.Cos(a)*(PadRadius-.15f),0,Mathf.Sin(a)*(PadRadius-.15f));var turn=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);
                    s.Slot(2).Box(q+Vector3.up*.18f,new Vector3(.5f,.36f,.7f),turn,new Color(.9f,.72f,.42f),new Color(1,.84f,.56f)).Slot(0).Box(q+Vector3.up*.42f+turn*new Vector3(-.18f,0,0),new Vector3(.18f,.3f,.5f),turn,iron);
                }
                var body=kit.Built("Landing pad drawn",s,Iron,Cloth,Brass);body.transform.SetParent(holder.transform,true);ZoneKit.Block(body,at+Vector3.down*.02f,new Vector3(PadRadius*1.3f,.16f,PadRadius*1.3f),0,"Pad deck");
                kit.Way(holder,"Launch — The Decks",DecksZone,"Arrival","home.treadRecovered","Bring tread-hall four in for Chief Engineer Dorrow first",4,new Vector2(Pad.x+4.4f,Pad.y-3.6f));
                if(greybox)return;
                LampPost(kit,new Vector2(Pad.x-PadRadius-.8f,Pad.y+2.2f),Deck,0,holder.transform);LampPost(kit,new Vector2(Pad.x+PadRadius+.8f,Pad.y+2.2f),Deck,180,holder.transform);
            }

            // ---------------------------------------------------------------- the services: the oil shop's cradle, the tread shop's bench, the Lashed Stall, the post rack
            static void Services(ZoneKit kit)
            {
                var cradle=kit.Prop("DeckConsole",new Vector3(Cradle.x,0,Cradle.y),1.4f,180).AddComponent<RepairBay>();cradle.free=false;cradle.saveSpawn="Yard";cradle.prompt="Oil-shop cradle — repair and save";cradle.name="Oil-shop cradle";
                var anvil=kit.Prop("LatticeAnvil",new Vector3(TreadShop.x,0,TreadShop.y),1.6f,180);anvil.name="The tread shop's anvil";var bench=anvil.AddComponent<Bench>();bench.prompt="Fabricate — The Tread Shop";
                // The stall: a counter under an awning, everything on it strapped down; the Shop prompt stands at its front.
                var stall=Booth(kit,"The Lashed Stall",Stall,4.2f,new Color(.74f,.3f,.24f),3);
                var shop=new GameObject("The Lashed Stall's counter",typeof(Shop)).GetComponent<Shop>();shop.transform.position=At(kit,Stall+new Vector2(0,-.6f),.5f);shop.transform.SetParent(stall.transform,true);
                shop.prompt="Trade — The Lashed Stall";shop.range=2.6f;shop.inventory=Find<ShopInventory>(CrawlZone);
                if(shop.inventory==null)throw new InvalidOperationException("MeridianCrawl: the Lashed Stall has no stock (CrawlDefinition stocks ShopInventory/"+CrawlZone+")");
                // The post rack against the south rail: pigeonholes, parcels in them, the mail runner takes what's handed over at a walk.
                var rack=new ZoneKit.Shape();var foot=At(kit,PostRack);Color wood=new(.6f,.48f,.36f);
                rack.Slot(0).Box(foot+Vector3.up*.9f,new Vector3(2.6f,1.8f,.5f),Quaternion.identity,wood,new Color(.72f,.6f,.44f));
                for(int r=0;r<3;r++)for(int c=0;c<5;c++){var cell=foot+new Vector3(-1.04f+c*.52f,.35f+r*.55f,-.26f);rack.Slot(1).Box(cell,new Vector3(.44f,.44f,.04f),Quaternion.identity,new Color(.1f,.09f,.08f));if(Hash(r,c,4)>.45f)rack.Slot(2).Box(cell+new Vector3(0,-.08f,-.04f),new Vector3(.3f,.2f,.18f),Quaternion.Euler(0,(Hash(c,r,6)-.5f)*20,0),new Color(.86f,.8f,.66f));}
                var rackBody=kit.Built("Post rack",rack,Timber,Dark,Cloth);ZoneKit.Block(rackBody,foot+Vector3.up*.9f,new Vector3(2.6f,1.8f,.5f),0,"Post rack");
                var parcel=kit.Discovery("A parcel for the Crawl",new Vector3(PostRack.x,0,PostRack.y+.6f),"Hand over the parcel","neve.parcelCrawl","CrawlParcel",null,"Sela","ReturnAddress",1,3.4f);parcel.situation="parcel.crawl";
                // What's handed over: Neve's parcel, a corded box on the rack's shelf, seen only while it can be handed over.
                var box=new ZoneKit.Shape();box.Slot(0).Box(foot+new Vector3(.2f,1.92f,0),new Vector3(.46f,.24f,.34f),Quaternion.Euler(0,12,0),new Color(.9f,.86f,.74f)).Slot(1).Box(foot+new Vector3(.2f,1.92f,0),new Vector3(.48f,.05f,.06f),Quaternion.Euler(0,12,0),new Color(.6f,.24f,.2f));
                var visual=kit.Built("Neve's parcel",box,Cloth,Cloth);visual.transform.SetParent(parcel.transform,true);parcel.visual=visual;visual.SetActive(false);
                rackBody.transform.SetParent(parcel.transform.parent,true);
            }
            /// <summary>A market booth: a counter of planks under a striped awning on four posts, goods on it strapped down, a sign hanging from the awning's front rail and a string of
            /// goods off its side (both swing with the hall). Its front faces the street (south).</summary>
            static GameObject Booth(ZoneKit kit,string name,Vector2 at,float wide,Color awning,int seed)
            {
                var booth=new GameObject(name);var f=At(kit,at);booth.transform.position=f;var s=new ZoneKit.Shape();var rng=new System.Random(seed);Color wood=new(.58f,.46f,.34f),iron=new(.3f,.3f,.33f);float h=wide*.5f;
                s.Slot(0).Box(f+new Vector3(0,.5f,0),new Vector3(wide,1,.7f),Quaternion.identity,wood,new Color(.7f,.58f,.42f));
                foreach(float x in new[]{-h,h}){s.Slot(0).Post(f+new Vector3(x,0,1.6f),2.75f,.07f,.06f,6,iron,iron).Post(f+new Vector3(x,0,-.3f),2.35f,.07f,.06f,6,iron,iron);}
                s.Slot(1).Beam(f+new Vector3(-h-.1f,2.33f,-.32f),f+new Vector3(h+.1f,2.33f,-.32f),.06f,.06f,Color.white);
                // The awning in stripes, lashed to its frame at the corners.
                int stripes=Mathf.Max(3,Mathf.RoundToInt(wide/.6f));
                for(int k=0;k<stripes;k++){float x0=-h+wide*k/stripes,x1=-h+wide*(k+1)/stripes;var tone=k%2==0?awning:new Color(.9f,.86f,.76f);
                    s.Slot(2).Quad(f+new Vector3(x0,2.78f,1.75f),f+new Vector3(x1,2.78f,1.75f),f+new Vector3(x1,2.32f,-.5f),f+new Vector3(x0,2.32f,-.5f),Vector3.up,tone,.5f);}
                // Goods on the counter, under a strap.
                for(int k=0;k<6;k++){var g=f+new Vector3(-h+.35f+(wide-.7f)*k/5f,1.08f,-.05f+(float)rng.NextDouble()*.2f);var c=Color.HSVToRGB((float)rng.NextDouble(),.4f,.8f);s.Slot(3).Lump(g,new Vector3(.16f,.1f,.14f),rng.Next(99),.25f,c,ZoneKit.Shape.Dim(c,.6f),8,4);}
                s.Slot(4).Beam(f+new Vector3(-h,1.13f,0),f+new Vector3(h,1.13f,0),.05f,.02f,new Color(.3f,.24f,.2f));
                var body=kit.Built(name+" booth",s,Timber,Brass,Cloth,Stone,Rope);body.transform.SetParent(booth.transform,true);ZoneKit.Block(body,f+new Vector3(0,.5f,0),new Vector3(wide,1,.7f),0,name);
                ZoneKit.Fadeable(body);
                if(greybox)return booth;
                // The hanging sign, a board on two chains from the front rail.
                var sign=new ZoneKit.Shape();var board=new Color(.86f,.8f,.66f);
                sign.Slot(0).Box(new Vector3(0,-.62f,0),new Vector3(1.0f,.42f,.05f),Quaternion.identity,board,ZoneKit.Shape.Dim(board,1.1f)).Slot(1).Box(new Vector3(0,-.62f,-.03f),new Vector3(.7f,.12f,.01f),Quaternion.identity,awning);
                foreach(float x in new[]{-.4f,.4f})sign.Slot(2).Tube(new[]{new Vector3(x*.4f,0,0),new Vector3(x,-.42f,0)},new[]{.012f,.012f},4,new Color(.3f,.3f,.3f),new Color(.3f,.3f,.3f));
                var pivot=Pivoted(kit,name+" sign",sign,f+new Vector3(.35f*(seed%2==0?1:-1),2.3f,-.34f),booth.transform,Timber,Cloth,Iron);Swings(pivot.transform,1);
                // A string of goods off the awning's side: pots, dried fruit, a lamp.
                var hang=new ZoneKit.Shape();hang.Slot(2).Tube(new[]{Vector3.zero,new Vector3(0,-1.0f,0)},new[]{.012f,.012f},4,new Color(.4f,.36f,.3f),new Color(.4f,.36f,.3f));
                for(int k=0;k<4;k++){var c=Color.HSVToRGB(.02f+.12f*(float)rng.NextDouble(),.55f,.7f);hang.Slot(0).Lump(new Vector3(0,-.25f-k*.22f,0),new Vector3(.09f,.1f,.09f),seed*7+k,.2f,c,ZoneKit.Shape.Dim(c,.6f),8,4);}
                hang.Slot(1).Lump(new Vector3(0,-1.1f,0),new Vector3(.08f,.11f,.08f),3,.02f,Color.white,Color.white,6,4);
                var goods=Pivoted(kit,name+" goods",hang,f+new Vector3((seed%2==0?-1:1)*(h+.05f),2.5f,-.3f),booth.transform,Stone,Lamp,Rope);Swings(goods.transform,1.3f);
                return booth;
            }
            /// <summary>A lamp post with a lantern hanging from its arm on a chain: the lantern swings with the hall. Optional light.</summary>
            static GameObject LampPost(ZoneKit kit,Vector2 at,float floor,float yaw,Transform parent,bool light=false)
            {
                var post=new GameObject("Lamp post");var f=new Vector3(at.x,float.IsNaN(floor)?kit.Y(at.x,at.y):floor,at.y);post.transform.position=f;if(parent!=null)post.transform.SetParent(parent,true);
                var s=new ZoneKit.Shape();Color iron=new(.28f,.28f,.31f);var arm=Quaternion.Euler(0,yaw,0)*Vector3.right*.7f;
                s.Slot(0).Post(f,3.3f,.08f,.06f,6,iron,iron).Beam(f+Vector3.up*3.2f,f+Vector3.up*3.2f+arm,.06f,.06f,iron).Beam(f+Vector3.up*2.9f,f+Vector3.up*3.18f+arm*.6f,.04f,.04f,iron);
                s.Slot(1).Disc(f+Vector3.up*3.32f,Vector3.up,.1f,8,Color.white);
                var drawn=kit.Built(Name("Lamp post"),s,Iron,Brass);drawn.transform.SetParent(post.transform,true);ZoneKit.Block(drawn,f+Vector3.up*1,new Vector3(.24f,2,.24f),0,"Lamp post");
                var lantern=new ZoneKit.Shape();
                lantern.Slot(2).Tube(new[]{Vector3.zero,new Vector3(0,-.45f,0)},new[]{.012f,.012f},4,new Color(.3f,.3f,.3f),new Color(.3f,.3f,.3f));
                lantern.Slot(1).Lump(new Vector3(0,-.68f,0),new Vector3(.13f,.19f,.13f),(int)(at.x*3),.03f,Color.white,Color.white,8,5).Slot(0).Box(new Vector3(0,-.46f,0),new Vector3(.26f,.05f,.26f),Quaternion.identity,new Color(.86f,.66f,.38f)).Box(new Vector3(0,-.9f,0),new Vector3(.22f,.04f,.22f),Quaternion.identity,new Color(.86f,.66f,.38f));
                var pivot=Pivoted(kit,Name("Lantern"),lantern,f+Vector3.up*3.18f+arm,post.transform,Brass,Lamp,Rope);Swings(pivot.transform,1.15f);
                if(light){var l=kit.Lamp(new Vector3(at.x,0,at.y),8,1.2f,new Color(1,.78f,.5f));l.transform.position=f+Vector3.up*2.5f+arm;}
                return post;
            }

            // ---------------------------------------------------------------- the tail: the dawn-readers, the broken coupling, the lip and its gate, the drop cage
            static void BuildTail(ZoneKit kit)
            {
                var holder=new GameObject("The tail");var s=new ZoneKit.Shape();Color iron=new(.3f,.3f,.33f),brass=new(.92f,.72f,.42f);
                // The tail's half of the coupling to tread-hall four, broken: the knuckle under the lip with a snapped link, the safety chains hanging into the gap.
                s.Slot(0).Beam(new Vector3(Lip-1.5f,-2.5f,-3),new Vector3(Lip+1.2f,-2.5f,-3),.9f,.9f,iron).Tube(new[]{new Vector3(Lip+1.2f,-3.3f,-3),new Vector3(Lip+1.2f,-1.7f,-3)},new[]{.7f,.7f},14,new Color(.38f,.36f,.36f),new Color(.44f,.42f,.4f));
                var drawn=kit.Built("The tail's coupling",s,Iron);drawn.transform.SetParent(holder.transform,true);
                var link=new ZoneKit.Shape();
                link.Slot(0).Tube(new[]{Vector3.zero,new Vector3(0,-1.2f,0)},new[]{.05f,.05f},5,new Color(.26f,.26f,.28f),new Color(.26f,.26f,.28f)).Box(new Vector3(0,-1.55f,0),new Vector3(.5f,.7f,.24f),Quaternion.Euler(0,0,14),new Color(.4f,.36f,.34f));
                link.Slot(1).Box(new Vector3(.12f,-1.92f,0),new Vector3(.2f,.1f,.26f),Quaternion.Euler(0,0,40),new Color(.9f,.62f,.36f));
                var snapped=Pivoted(kit,"The snapped link",link,new Vector3(Lip+1.5f,-1.6f,-3),drawn.transform,Iron,Copper);Swings(snapped.transform,.7f);
                foreach(float z in new[]{-7.2f,1.6f})
                {
                    var chain=new ZoneKit.Shape();chain.Tube(new[]{Vector3.zero,new Vector3(.2f,-2.4f,0),new Vector3(.35f,-3.8f,0)},new[]{.07f,.07f,.07f},6,new Color(.26f,.26f,.28f),new Color(.3f,.3f,.32f));
                    var hangs=Pivoted(kit,"A safety chain hanging",chain,new Vector3(Lip+.1f,-.4f,z),drawn.transform,Iron);Swings(hangs.transform,1.4f);
                }
                // The lip's gate: a chain across the gap where the gangway lands, until tread-hall four is in and Dorrow has said so; a carving obstacle keeps paths off it.
                var gate=new GameObject("The lip's gate");gate.transform.position=new Vector3(Lip-.25f,Deck,-3);gate.transform.SetParent(holder.transform,true);
                var g=new ZoneKit.Shape();foreach(float z in new[]{BridgeSouth+.15f,BridgeNorth-.15f})g.Slot(0).Post(new Vector3(Lip-.25f,Deck,z),1.2f,.09f,.07f,6,iron,iron);
                g.Slot(1).Rope(new Vector3(Lip-.25f,1.0f,BridgeSouth+.15f),new Vector3(Lip-.25f,1.0f,BridgeNorth-.15f),.35f,.05f,new Color(.3f,.3f,.32f),6,10).Rope(new Vector3(Lip-.25f,.55f,BridgeSouth+.15f),new Vector3(Lip-.25f,.55f,BridgeNorth-.15f),.25f,.05f,new Color(.3f,.3f,.32f),6,10);
                g.Slot(2).Box(new Vector3(Lip-.32f,.75f,-3),new Vector3(.03f,.4f,.9f),Quaternion.identity,new Color(.9f,.86f,.72f));
                var gateDrawn=kit.Built("The lip's gate drawn",g,Iron,Iron,Cloth);gateDrawn.transform.SetParent(gate.transform,true);
                var box=gate.AddComponent<BoxCollider>();box.center=new Vector3(0,.6f,0);box.size=new Vector3(.3f,1.2f,BridgeNorth-BridgeSouth);
                var carve=gate.AddComponent<NavMeshObstacle>();carve.carving=true;carve.shape=NavMeshObstacleShape.Box;carve.center=new Vector3(0,.6f,0);carve.size=new Vector3(.8f,1.2f,BridgeNorth-BridgeSouth);
                // A seal, as every way shut mid-zone is: what opens it (here the hall coupling and Dorrow's word, CouplesIn) shows or hides it, and a check that walks every state opens it.
                gate.AddComponent<Membrane>();
                // Shut until tread-hall four has rolled in, coupled and been greeted (CouplesIn, below): a flag alone opened it onto a porch still seven seconds out.
                lipGate=gate;
                // The drop cage on its davit over the gap: the way down to the run.
                var cage=new GameObject("The drop cage");cage.transform.position=new Vector3(DropCage.x,Deck,DropCage.y);cage.transform.SetParent(holder.transform,true);
                var d=new ZoneKit.Shape();var foot=new Vector3(Lip-.9f,Deck,DropCage.y+1.6f);
                d.Slot(0).Post(foot,3.6f,.18f,.14f,8,iron,new Color(.4f,.38f,.38f)).Beam(foot+Vector3.up*3.5f,new Vector3(DropCage.x+.2f,3.5f,DropCage.y),.2f,.24f,iron).Beam(foot+Vector3.up*2.2f,foot+new Vector3(1.2f,3.45f,-.6f),.12f,.12f,iron);
                d.Slot(1).Tube(new[]{foot+new Vector3(.3f,1.1f,0),foot+new Vector3(-.3f,1.1f,0)},new[]{.35f,.35f},12,brass,brass);
                var davit=kit.Built("The drop cage's davit",d,Iron,Brass);davit.transform.SetParent(cage.transform,true);ZoneKit.Block(davit,foot+Vector3.up*1.8f,new Vector3(.5f,3.6f,.5f),0,"Davit");
                var c=new ZoneKit.Shape();Color bar=new(.36f,.34f,.34f);
                for(int k=0;k<4;k++){var corner=new Vector3((k%2==0?-1:1)*.95f,0,(k<2?-1:1)*.95f);c.Slot(0).Beam(corner+Vector3.down*3.2f,corner+Vector3.down*1.0f,.08f,.08f,bar);}
                foreach(float y in new[]{-3.2f,-2.1f,-1.0f}){c.Slot(0).Box(new Vector3(0,y,-.95f),new Vector3(1.98f,.07f,.07f),Quaternion.identity,bar).Box(new Vector3(0,y,.95f),new Vector3(1.98f,.07f,.07f),Quaternion.identity,bar).Box(new Vector3(-.95f,y,0),new Vector3(.07f,.07f,1.98f),Quaternion.identity,bar).Box(new Vector3(.95f,y,0),new Vector3(.07f,.07f,1.98f),Quaternion.identity,bar);}
                c.Slot(0).Box(new Vector3(0,-3.24f,0),new Vector3(2,.08f,2),Quaternion.identity,new Color(.42f,.4f,.4f));
                foreach(var corner in new[]{new Vector3(-.95f,-1,-.95f),new Vector3(.95f,-1,-.95f),new Vector3(-.95f,-1,.95f),new Vector3(.95f,-1,.95f)})c.Slot(1).Tube(new[]{corner,Vector3.zero},new[]{.02f,.02f},4,new Color(.3f,.3f,.3f),new Color(.3f,.3f,.3f));
                c.Slot(2).Lump(new Vector3(0,-1.25f,0),new Vector3(.11f,.15f,.11f),2,.02f,Color.white,Color.white,6,4);
                var hangs2=Pivoted(kit,"The drop cage itself",c,new Vector3(DropCage.x+.2f,3.3f,DropCage.y),cage.transform,Iron,Rope,Lamp);Swings(hangs2.transform,.45f);
                // The way down is on a holder of its own with its sign: once tread-hall four is home and its talk said, the ground behind the town is in daylight, and the cage stays up.
                var down=new GameObject("The way down to the run");down.transform.position=cage.transform.position;down.transform.SetParent(cage.transform,true);
                kit.Way(down,"Ride the drop cage down — The Terminator",TerminatorZone,"Arrival","home.crawlAsked","Ask Chief Engineer Dorrow at the helm first",3.2f,null);
                var downSign=WorldBuilder.Label("Ride the drop cage down — The Terminator",kit.On(CageHand.x-2.2f,CageHand.y-2.2f,.3f),2.2f);
                var stays=new GameObject("The cage stays up once number four is home",typeof(FlagGate)).GetComponent<FlagGate>();stays.flag="home.treadRecovered";stays.showWhenSet=false;stays.targets=new[]{down,downSign};
                if(greybox)return;
                // The dawn-readers' instruments at the tail rail: the brass dawn glass on its tripod, aimed east, and the slate of readings on its easel.
                var r=new ZoneKit.Shape();var glass=At(kit,new Vector2(57.4f,-8.2f));
                foreach(float a in new[]{0f,120f,240f}){var leg=Quaternion.Euler(0,a,0)*new Vector3(.45f,0,0);r.Slot(0).Beam(glass+leg,glass+Vector3.up*1.25f,.05f,.05f,new Color(.4f,.32f,.24f));}
                r.Slot(1).Tube(new[]{glass+new Vector3(-.5f,1.3f,0),glass+new Vector3(.9f,1.55f,0)},new[]{.12f,.16f},12,brass,new Color(1,.82f,.5f)).Disc(glass+new Vector3(.92f,1.555f,0),Vector3.right,.15f,12,new Color(.4f,.5f,.6f));
                var easel=At(kit,new Vector2(53.6f,-8.6f));r.Slot(0).Beam(easel,easel+new Vector3(0,1.5f,.25f),.05f,.05f,new Color(.4f,.32f,.24f)).Beam(easel+new Vector3(.5f,0,0),easel+new Vector3(.5f,1.5f,.25f),.05f,.05f,new Color(.4f,.32f,.24f));
                r.Slot(2).Box(easel+new Vector3(.25f,1.15f,.12f),new Vector3(.9f,.65f,.04f),Quaternion.Euler(-12,0,0),new Color(.16f,.17f,.18f));
                for(int k=0;k<5;k++)r.Slot(3).Box(easel+new Vector3(-.05f+k*.13f,1.0f+.07f*(k%3),.09f),new Vector3(.1f,.02f,.005f),Quaternion.Euler(-12,0,(k-2)*8),new Color(.92f,.92f,.88f));
                var readings=kit.Built("The dawn-readers' instruments",r,Timber,Brass,Dark,Cloth);readings.transform.SetParent(holder.transform,true);
                ZoneKit.Block(readings,glass+Vector3.up*.6f,new Vector3(1,1.2f,1),0,"Dawn glass");ZoneKit.Block(readings,easel+new Vector3(.25f,.75f,.12f),new Vector3(.9f,1.5f,.4f),0,"Slate easel");
            }

            // ---------------------------------------------------------------- tread-hall four, which comes back from the run
            static GameObject hallDrawn,hallWalked,loHere,loBench,lipGate;
            /// <summary>The generated tread-hall (the chapter's own <c>CrawlTread</c>) fitted to <paramref name="length"/> along its treads, which run along x; its feet on the plain.</summary>
            internal static GameObject CrawlTread(ZoneKit kit,string name,Vector3 centreOnPlain,float length,float yaw,Color tint)
            {
                var hall=kit.Prop(ZoneKit.Kit("CrawlTread","OutpostHab"),new Vector3(centreOnPlain.x,centreOnPlain.y,centreOnPlain.z),10,0,false,false);hall.name=name;
                var b=ZoneKit.Drawn(hall);bool alongZ=b.size.z>b.size.x;if(alongZ){hall.transform.rotation=Quaternion.Euler(0,90+yaw,0);}else hall.transform.rotation=Quaternion.Euler(0,yaw,0);
                b=ZoneKit.Drawn(hall);float scale=length/Mathf.Max(.01f,b.size.x);hall.transform.localScale*=scale;b=ZoneKit.Drawn(hall);
                hall.transform.position+=new Vector3(centreOnPlain.x-b.center.x,centreOnPlain.y-b.min.y,centreOnPlain.z-b.center.z);
                hall.AddComponent<PropTint>().tint=tint;Debug.Log($"HOME_CRAWL_TREADHALL {name} size={ZoneKit.Drawn(hall).size} alongZ={alongZ}");
                return hall;
            }
            static void TreadHallFour(ZoneKit kit)
            {
                var root=new GameObject("Tread-hall four");
                // What rolls in: the hall itself and its gangway, drawn only. What is walked on stands in its coupled place throughout (in the bake), shown from the third coupling.
                hallDrawn=new GameObject("Tread-hall four, drawn");hallDrawn.transform.SetParent(root.transform,true);
                var hall=CrawlTread(kit,"Tread-hall four's hall",new Vector3(HallWest+13,Plain,-3),26,0,new Color(.98f,.96f,.94f));hall.transform.SetParent(hallDrawn.transform,true);
                var s=new ZoneKit.Shape();var rng=new System.Random(81);Color plate=new(.5f,.5f,.52f),iron=new(.3f,.3f,.33f);
                s.Slot(0).Deck(new Vector3(Gangway.xMin,Deck,-3),new Vector3(Gangway.xMax,Deck,-3),6,.14f,plate,rng,.55f,.05f).Box(new Vector3(Porch.center.x,-.25f,Porch.center.y),new Vector3(Porch.width,.5f,Porch.height),Quaternion.identity,new Color(.44f,.44f,.47f),new Color(.56f,.55f,.56f));
                foreach(float z in new[]{BridgeSouth+.25f,BridgeNorth-.25f})s.Slot(0).Beam(new Vector3(Gangway.xMin,1.05f,z),new Vector3(Gangway.xMax,1.05f,z),.07f,.07f,new Color(.46f,.44f,.44f)).Beam(new Vector3(Gangway.xMin,.56f,z),new Vector3(Gangway.xMax,.56f,z),.05f,.05f,new Color(.46f,.44f,.44f));
                for(float x=Gangway.xMin;x<=Gangway.xMax+.01f;x+=1.6f)foreach(float z in new[]{BridgeSouth+.25f,BridgeNorth-.25f})s.Slot(0).Post(new Vector3(x,Deck,z),1.12f,.07f,.06f,6,iron,iron);
                foreach(var (a,b) in new[]{(new Vector2(Porch.xMin,Porch.yMin+.25f),new Vector2(Porch.xMax,Porch.yMin+.25f)),(new Vector2(Porch.xMin,Porch.yMax-.25f),new Vector2(Porch.xMax,Porch.yMax-.25f)),(new Vector2(Porch.xMax-.25f,Porch.yMin),new Vector2(Porch.xMax-.25f,Porch.yMax))})
                    s.Slot(0).Beam(new Vector3(a.x,1.05f,a.y),new Vector3(b.x,1.05f,b.y),.07f,.07f,new Color(.46f,.44f,.44f)).Post(new Vector3(a.x,Deck,a.y),1.12f,.07f,.06f,6,iron,iron).Post(new Vector3(b.x,Deck,b.y),1.12f,.07f,.06f,6,iron,iron);
                kit.Built("Tread-hall four's gangway and porch",s,Iron).transform.SetParent(hallDrawn.transform,true);
                // Its own treads turn with the town once it is coupled: hubs on its sprockets.
                hallWalked=new GameObject("Tread-hall four's porch, walked");hallWalked.transform.SetParent(root.transform,true);
                ZoneKit.Block(hallWalked,new Vector3((Gangway.xMin+Gangway.xMax)*.5f,-.15f,-3),new Vector3(Gangway.width,.3f,Gangway.height),0,"Gangway deck");
                ZoneKit.Block(hallWalked,new Vector3(Porch.center.x,-.25f,Porch.center.y),new Vector3(Porch.width,.5f,Porch.height),0,"Porch deck");
                foreach(var (a,b) in new[]{(new Vector2(Gangway.xMin,BridgeSouth+.25f),new Vector2(Gangway.xMax,BridgeSouth+.25f)),(new Vector2(Gangway.xMin,BridgeNorth-.25f),new Vector2(Gangway.xMax,BridgeNorth-.25f)),
                    (new Vector2(Porch.xMin,Porch.yMin+.25f),new Vector2(Porch.xMax,Porch.yMin+.25f)),(new Vector2(Porch.xMin,Porch.yMax-.25f),new Vector2(Porch.xMax,Porch.yMax-.25f)),(new Vector2(Porch.xMax-.25f,Porch.yMin),new Vector2(Porch.xMax-.25f,Porch.yMax))})
                    ZoneKit.BlockAlong(hallWalked,new Vector3(a.x,.6f,a.y),new Vector3(b.x,.6f,b.y),.3f,1.2f,"Porch rail");
                // The porch's stand-in floor for a still: the blocks have nothing drawn, so they go under the drawn porch (SeatBlocks puts them there if not).
                var back=new GameObject("Tread-hall four is back",typeof(FlagGate)).GetComponent<FlagGate>();back.flag="home.rideDone";back.showWhenSet=true;back.targets=new[]{hallDrawn,hallWalked};
                var comes=new GameObject("Tread-hall four couples in",typeof(CouplesIn)).GetComponent<CouplesIn>();comes.flag="home.rideDone";comes.doneFlag="home.treadRecovered";comes.hall=hallDrawn.transform;comes.from=new Vector3(30,0,0);comes.seconds=7;
                comes.shut=lipGate!=null?new[]{lipGate}:Array.Empty<GameObject>();comes.opensWith="home.treadRecovered";
                // Lo, Hal's apprentice, on the porch from the party's homecoming: the folding bench handed over at the porch's rail; once it's handed over, it works.
                loHere=new GameObject("Lo on tread-hall four");loHere.transform.SetParent(hallWalked.transform,true);
                var bench=kit.Discovery("A bench for Lo",new Vector3(LoHand.x,Deck,LoHand.y),"Hand over the bench","bench.lo","PortableBench",null,"Sela","PortableBenches",0,2.6f);bench.situation="bench.lo";
                bench.transform.position=new Vector3(LoHand.x+.6f,Deck,LoHand.y);bench.transform.SetParent(loHere.transform,true);
                var lo=Townsfolk("Hal",new Vector3(LoAt.x,Deck,LoAt.y),200,new Color(.92f,.98f,1.04f));lo.name="Lo, Hal's apprentice";lo.transform.localScale=Vector3.one*.9f;lo.transform.SetParent(loHere.transform,true);
                var folded=new ZoneKit.Shape();var f=new Vector3(LoBench.x,Deck,LoBench.y);Color wood=new(.68f,.52f,.36f);
                folded.Slot(0).Box(f+Vector3.up*.86f,new Vector3(2,.08f,.8f),Quaternion.identity,wood,ZoneKit.Shape.Dim(wood,1.15f));
                foreach(int side in new[]{-1,1})folded.Slot(0).Beam(f+new Vector3(side*.8f,0,-.35f),f+new Vector3(side*.8f,.84f,.35f),.06f,.06f,ZoneKit.Shape.Dim(wood,.8f)).Beam(f+new Vector3(side*.8f,0,.35f),f+new Vector3(side*.8f,.84f,-.35f),.06f,.06f,ZoneKit.Shape.Dim(wood,.8f));
                folded.Slot(1).Box(f+new Vector3(-.7f,1,-.2f),new Vector3(.24f,.2f,.18f),Quaternion.identity,new Color(.4f,.42f,.46f));
                loBench=kit.Built("Lo's folding bench",folded,Timber,Iron);loBench.transform.SetParent(loHere.transform,true);ZoneKit.Block(loBench,f+Vector3.up*.45f,new Vector3(2,.9f,.8f),0,"Folding bench");
                var works=new GameObject("Lo's folding bench works",typeof(Bench)).GetComponent<Bench>();works.transform.position=f+new Vector3(0,.8f,-.9f);works.prompt="Fabricate — Lo's folding bench";works.range=2.4f;works.transform.SetParent(loBench.transform,true);
                var handed=new GameObject("Lo's bench is up once it's handed over",typeof(FlagGate)).GetComponent<FlagGate>();handed.flag="bench.lo";handed.showWhenSet=true;handed.targets=new[]{loBench};
                Where("Lo is on tread-hall four from the homecoming",loHere,"home.returned",null,null,null);
                if(greybox)return;
                // Hesk's pot: a Crawler on the porch with the pot that has been on for a day, there from the hall's return until the party goes home.
                var hesk=Crawler(kit,"Townsperson, with Hesk's pot",new Vector3(HeskAt.x,Deck,HeskAt.y),230,3,.92f,"scarf","pot");hesk.transform.SetParent(hallWalked.transform,true);
                Where("Hesk's pot comes out of tread-hall four",hesk,"home.treadRecovered",null,null,"home.returned");
            }
            static void Where(string name,GameObject who,string from,string awayFrom,string backAt,string goneAt,bool live=false)
            {
                var where=new GameObject(name,typeof(Whereabouts)).GetComponent<Whereabouts>();where.from=from??"";where.awayFrom=awayFrom??"";where.backAt=backAt??"";where.goneAt=goneAt??"";where.live=live;where.targets=new[]{who};
                who.SetActive(string.IsNullOrEmpty(from));
            }
            /// <summary>Someone of the Halo's with no name, standing in a borrowed body tinted apart (Lo, in Hal's workshop coat).</summary>
            static GameObject Townsfolk(string body,Vector3 at,float yaw,Color tint)
            {
                var person=new GameObject("Townsperson");person.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));person.isStatic=true;
                var model=(GameObject)PrefabUtility.InstantiatePrefab(ZoneKit.Prefab("Characters",body)??ZoneKit.Prefab("Characters","Neve"));model.transform.SetParent(person.transform,false);model.name="Generated civilian";
                var box=person.AddComponent<BoxCollider>();box.size=new Vector3(.7f,1.9f,.7f);box.center=Vector3.up*.95f;person.AddComponent<PropTint>().tint=tint;
                foreach(var rig in model.GetComponentsInChildren<Animator>()){rig.cullingMode=AnimatorCullingMode.CullCompletely;PrefabUtility.RecordPrefabInstancePropertyModifications(rig);}
                return person;
            }

            // ---------------------------------------------------------------- the tread-halls alongside
            internal const float AlongsideWide=20;
            /// <summary>The town is wider than its spine: tread-halls ride alongside to the north across a strip of open plain, each the generated tread-hall, each its own tint and
            /// heading so no two read as copies; with their tracks behind them. Seen past every north rail and in the wide views.</summary>
            internal static readonly (float x,float z,float yaw,Color tint,float length)[] AlongsideHalls=
                {(-66,34,0,new Color(.94f,.96f,1.04f),26),(-20,35.5f,180,new Color(1.04f,.98f,.92f),28),(26,33,0,new Color(.96f,1,.98f),24),(70,36,180,new Color(1.02f,.96f,.96f),26)};
            static void Alongside(ZoneKit kit)
            {
                var holder=new GameObject("Tread-halls alongside");int n=0;
                foreach(var (x,z,yaw,tint,length) in AlongsideHalls)
                {
                    var hall=CrawlTread(kit,"Tread-hall alongside "+(++n),new Vector3(x,Plain,z),length,yaw,tint);hall.transform.SetParent(holder.transform,true);
                    foreach(var ps in new[]{-1,1})Dust(kit,DustLook(kit),new Vector3(x+length*.5f+1,Plain+.6f,z+ps*(AlongsideWide*.5f-1.6f)),hall.transform);
                }
            }

            // ---------------------------------------------------------------- the story, wired into the town (manifest section 2; docs/chapters/ch09-wiring.md)
            static void Story(ZoneKit kit)
            {
                // Dorrow at the helm until he goes to the gap to watch number four come in.
                var dorrow=kit.Person("Dorrow",new Vector3(DorrowAt.x,0,DorrowAt.y),150,"",false,null,null,"DorrowFirst");dorrow.repeatNode="DorrowRepeat";
                Where("Dorrow is at the helm until number four is coupled",dorrow.gameObject,null,null,null,"home.rideDone");
                // Dorrow at the lip, from the ride in (home.rideDone, the Terminator's) until he goes to the Halo with the party: no talk of his own (the hall's homecoming is an AfterTalk).
                var lip=Townsfolk("DorrowBody",new Vector3(DorrowLip.x,Deck,DorrowLip.y),90,Color.white);lip.name="Dorrow at the lip";
                Where("Dorrow watches number four come in",lip,"home.rideDone",null,null,"home.returned");
                var talk=new GameObject("Number four comes home",typeof(AfterTalk)).GetComponent<AfterTalk>();talk.transform.position=new Vector3(DorrowLip.x,Deck,DorrowLip.y);
                talk.flag="home.rideDone";talk.node="DorrowTreadHome";talk.speaker="Dorrow";talk.heardFlag="home.treadRecovered";talk.delay=2.5f;talk.range=0;
                // Tam at the market street's south rail, watching the treads with a notebook, until the Halo.
                var tam=kit.Person("Tam",new Vector3(TamAt.x,0,TamAt.y),160,"",false,null,null,"TamFirst");tam.repeatNode="TamFirst";
                Where("Tam is on the Crawl until the Halo",tam.gameObject,null,null,null,"home.returned");
                // Arriving is reaching the Crawl.
                var reached=new GameObject("Arriving is reaching the Crawl",typeof(FlagAll)).GetComponent<FlagAll>();reached.flags=new[]{"visited."+CrawlZone};reached.result="home.crawlReached";
                // Mira's lot, lent to the Crawl nine years ago and still wrapped: lashed upright against the sky-garden's north rail. Its tag is what's read.
                var lot=new GameObject("Mira's lot on the sky-garden");var at=At(kit,MiraLot);lot.transform.position=at;var s=new ZoneKit.Shape();Color canvas=new(.82f,.76f,.62f),cord=new(.5f,.4f,.3f);
                s.Slot(0).Lump(at+Vector3.up*.95f,new Vector3(.7f,.95f,.42f),7,.14f,canvas,ZoneKit.Shape.Dim(canvas,.7f),10,6);
                foreach(float y in new[]{.35f,.95f,1.55f})s.Slot(1).Tube(Enumerable.Range(0,13).Select(i=>at+new Vector3(Mathf.Cos(i*Mathf.PI/6)*.66f,y,Mathf.Sin(i*Mathf.PI/6)*.4f)).ToArray(),Enumerable.Repeat(.025f,13).ToArray(),5,cord,cord,1,1,false);
                s.Slot(1).Rope(at+new Vector3(-.5f,1.6f,0),at+new Vector3(-.4f,1.0f,.82f),.1f,.025f,cord,5,6).Rope(at+new Vector3(.5f,1.6f,0),at+new Vector3(.4f,1.0f,.82f),.1f,.025f,cord,5,6);
                var wrapped=kit.Built("Mira's lot, still wrapped",s,Cloth,Rope);wrapped.transform.SetParent(lot.transform,true);ZoneKit.Block(wrapped,at+Vector3.up*.95f,new Vector3(1.3f,1.9f,.8f),0,"Mira's lot");
                var point=kit.Discovery("A lot with Mira's tag",new Vector3(MiraLot.x-.2f,0,MiraLot.y-1.1f),"Read Mira's tag","mira.lotCrawl","Consignment",null,"Sela","SecondInventory",0,2.8f);point.situation="consign.crawl";
                var tag=new ZoneKit.Shape();tag.Slot(0).Box(at+new Vector3(-.3f,1.2f,-.45f),new Vector3(.22f,.14f,.01f),Quaternion.Euler(0,0,8),new Color(.95f,.92f,.84f)).Slot(1).Tube(new[]{at+new Vector3(-.36f,1.32f,-.44f),at+new Vector3(-.3f,1.52f,-.38f)},new[]{.008f,.008f},4,cord,cord);
                var visual=kit.Built("Mira's tag",tag,Cloth,Rope);visual.transform.SetParent(point.transform,true);point.visual=visual;visual.SetActive(false);
            }

            // ---------------------------------------------------------------- the dressing, place by place (left out of the blockout)
            static GameObject House(ZoneKit kit,string name,float x0,float x1,float z0,float z1,float floor,float tall,Color plate,int seed,float lampSide=0)
            {
                var go=new GameObject(name);var s=new ZoneKit.Shape();var rng=new System.Random(seed);float zm=(z0+z1)*.5f,eave=floor+tall,ridge=eave+(z1-z0)*.5f*1.15f;
                Color trim=new(.9f,.7f,.42f),dark=ZoneKit.Shape.Dim(plate,.7f),roof=new(.3f,.29f,.31f);
                s.Slot(0).Box(new Vector3((x0+x1)*.5f,floor+tall*.5f,zm),new Vector3(x1-x0,tall,z1-z0),Quaternion.identity,plate);
                s.Slot(0).Quad(new Vector3(x0-.3f,eave,z0-.3f),new Vector3(x1+.3f,eave,z0-.3f),new Vector3(x1+.3f,ridge,zm),new Vector3(x0-.3f,ridge,zm),new Vector3(0,1,-1),roof,.5f)
                    .Quad(new Vector3(x0-.3f,eave,z1+.3f),new Vector3(x1+.3f,eave,z1+.3f),new Vector3(x1+.3f,ridge,zm),new Vector3(x0-.3f,ridge,zm),new Vector3(0,1,1),roof,.5f)
                    .Triangle(new Vector3(x0,eave,z0),new Vector3(x0,eave,z1),new Vector3(x0,ridge,zm),Vector3.left,plate).Triangle(new Vector3(x1,eave,z0),new Vector3(x1,eave,z1),new Vector3(x1,ridge,zm),Vector3.right,plate);
                s.Slot(1).Beam(new Vector3(x0-.35f,eave,z0-.32f),new Vector3(x1+.35f,eave,z0-.32f),.1f,.1f,trim).Beam(new Vector3(x0-.35f,ridge+.04f,zm),new Vector3(x1+.35f,ridge+.04f,zm),.12f,.12f,trim);
                int floors=Mathf.Max(1,Mathf.RoundToInt(tall/2.6f)),cols=Mathf.Max(1,Mathf.FloorToInt((x1-x0)/1.7f));float storey=tall/floors;
                for(int f=0;f<floors;f++)
                {
                    float y=floor+storey*f;s.Slot(0).Beam(new Vector3(x0,y+.06f,z0-.04f),new Vector3(x1,y+.06f,z0-.04f),.08f,.12f,dark);
                    for(int c=0;c<cols;c++)
                    {
                        float x=Mathf.Lerp(x0,x1,(c+.5f)/cols);bool door=f==0&&c==cols/2&&seed%3!=0;
                        if(door){s.Slot(3).Box(new Vector3(x,y+1.0f,z0-.03f),new Vector3(1.0f,2.0f,.04f),Quaternion.identity,new Color(.12f,.1f,.09f));s.Slot(1).Beam(new Vector3(x-.56f,y+2.05f,z0-.06f),new Vector3(x+.56f,y+2.05f,z0-.06f),.08f,.08f,trim);continue;}
                        bool lit=rng.NextDouble()<.62;var wc=new Vector3(x,y+storey*.58f,z0-.025f);
                        s.Slot(lit?2:3).Box(wc,new Vector3(.72f,1.0f,.03f),Quaternion.identity,lit?Color.white:new Color(.1f,.1f,.12f));
                        s.Slot(1).Box(wc+new Vector3(0,0,-.02f),new Vector3(.86f,.07f,.03f),Quaternion.identity,trim).Box(wc+new Vector3(0,-.52f,-.03f),new Vector3(.9f,.07f,.06f),Quaternion.identity,trim).Box(wc+new Vector3(0,0,-.02f),new Vector3(.06f,1.0f,.03f),Quaternion.identity,trim);
                    }
                }
                s.Slot(0).Post(new Vector3(x0+.8f,eave,zm+(z1-z0)*.2f),(ridge-eave)+1.4f,.22f,.2f,8,dark,dark).Slot(1).Tube(new[]{new Vector3(x0+.8f,ridge+1.4f,zm+(z1-z0)*.2f),new Vector3(x0+.8f,ridge+1.55f,zm+(z1-z0)*.2f)},new[]{.3f,.3f},8,trim,trim);
                var body=kit.Built(Name(name),s,Iron,Brass,Window,Dark);body.transform.SetParent(go.transform,true);ZoneKit.Block(body,new Vector3((x0+x1)*.5f,floor+tall*.5f,zm),new Vector3(x1-x0,tall,z1-z0),0,name);ZoneKit.Fadeable(body);
                if(lampSide!=0)
                {
                    var bracket=new ZoneKit.Shape();bracket.Slot(0).Beam(Vector3.zero,new Vector3(0,0,-.7f),.05f,.05f,new Color(.28f,.28f,.31f)).Slot(2).Tube(new[]{new Vector3(0,0,-.7f),new Vector3(0,-.4f,-.7f)},new[]{.012f,.012f},4,new Color(.3f,.3f,.3f),new Color(.3f,.3f,.3f));
                    var lamp=new ZoneKit.Shape();lamp.Slot(1).Lump(Vector3.zero,new Vector3(.12f,.17f,.12f),seed,.03f,Color.white,Color.white,8,5).Slot(0).Box(new Vector3(0,.22f,0),new Vector3(.24f,.05f,.24f),Quaternion.identity,trim);
                    float lx=lampSide<0?x0+.6f:x1-.6f;var arm=Pivoted(kit,Name(name+" bracket"),bracket,new Vector3(lx,floor+3.0f,z0),body.transform,Iron,Lamp,Rope);
                    var hung=Pivoted(kit,Name(name+" lamp"),lamp,new Vector3(lx,floor+3.0f-.62f,z0-.7f),arm.transform,Brass,Lamp);
                    var swing=new GameObject("Lantern swing");swing.transform.position=new Vector3(lx,floor+3.0f-.4f,z0-.7f);swing.transform.SetParent(arm.transform,true);hung.transform.SetParent(swing.transform,true);Swings(swing.transform,1.1f);
                }
                return go;
            }
            static void Helm(ZoneKit kit)
            {
                var holder=new GameObject("The helm");var s=new ZoneKit.Shape();Color iron=new(.3f,.3f,.33f),brass=new(.92f,.72f,.42f),plate=new(.52f,.54f,.6f);
                // The wheelhouse at the head: its front windows look west into the night, its south face is lit.
                // Along the platform's north side, its lit face to the camera: standing across its west end, eleven metres deep with a ridge thirteen metres up, it hid
                // anyone on the helm's north half from the fixed camera (look pass 04; D272).
                var house=House(kit,"The wheelhouse",-87.7f,-78.6f,2.6f,6.6f,Raised,4.0f,plate,11);house.transform.SetParent(holder.transform,true);
                // The forward lamp on its own standard at the bow, aimed west into the night (on the wheelhouse's roof it stood on nothing a body could find).
                var lamp=new ZoneKit.Shape();var bow=new Vector3(-87.2f,Raised,1.2f);
                lamp.Slot(0).Post(bow,3.4f,.12f,.09f,8,iron,iron).Tube(new[]{bow+new Vector3(1.0f,3.62f,0),bow+new Vector3(-.4f,3.52f,0)},new[]{.42f,.5f},16,iron,iron).Slot(1).Disc(bow+new Vector3(-.42f,3.52f,0),Vector3.left,.46f,16,Color.white);
                lamp.Slot(2).Box(bow+new Vector3(.3f,3.3f,0),new Vector3(.5f,.3f,.5f),Quaternion.identity,brass);
                var beam=kit.Built("The forward lamp",lamp,Iron,Lamp,Brass);beam.transform.SetParent(holder.transform,true);ZoneKit.Block(beam,bow+Vector3.up*1.7f,new Vector3(.3f,3.4f,.3f),0,"Forward lamp standard");
                var forward=new GameObject("Forward lamp light",typeof(Light)).GetComponent<Light>();forward.type=LightType.Spot;forward.spotAngle=34;forward.range=60;forward.intensity=3;forward.color=new Color(.86f,.92f,1);forward.shadows=LightShadows.None;
                forward.transform.SetPositionAndRotation(bow+new Vector3(-.5f,3.5f,0),Quaternion.Euler(14,-90,0));forward.transform.SetParent(beam.transform,true);
                // The great wheel on its pedestal, a Crawler at it; the speaking tubes; the dawn mirror on its gimbal at the platform's north-east corner, turned to show the east.
                var wheelFoot=new Vector3(-81.2f,Raised,.6f);s.Slot(0).Box(wheelFoot+Vector3.up*.6f,new Vector3(.6f,1.2f,.6f),Quaternion.identity,iron,brass);
                var wheel=new ZoneKit.Shape();wheel.Slot(0).Tube(Enumerable.Range(0,33).Select(i=>new Vector3(0,Mathf.Cos(i*Mathf.PI/16)*1.15f,Mathf.Sin(i*Mathf.PI/16)*1.15f)).ToArray(),Enumerable.Repeat(.07f,33).ToArray(),6,brass,brass,1,1,false);
                for(int k=0;k<8;k++){float a=k*Mathf.PI/4;var d=new Vector3(0,Mathf.Cos(a),Mathf.Sin(a));wheel.Slot(0).Beam(Vector3.zero,d*1.42f,.06f,.06f,brass).Slot(1).Tube(new[]{d*1.2f,d*1.5f},new[]{.05f,.04f},6,new Color(.5f,.36f,.24f),new Color(.6f,.44f,.3f));}
                var spun=Pivoted(kit,"The great wheel",wheel,wheelFoot+new Vector3(.4f,1.7f,0),holder.transform,Brass,Timber);spun.transform.rotation=Quaternion.Euler(0,0,0);
                var turning=spun.AddComponent<Turning>();turning.axis=Vector3.right;turning.degrees=0;
                for(int k=0;k<5;k++){float z=-4.6f+k*.5f;s.Slot(1).Tube(new[]{new Vector3(-82.6f,Raised,z),new Vector3(-82.6f,Raised+1.6f,z),new Vector3(-82.2f,Raised+1.9f,z)},new[]{.07f,.07f,.11f},8,brass,brass);}
                var mirror=new Vector3(-75.6f,Raised,5.2f);s.Slot(0).Post(mirror,1.6f,.08f,.06f,6,iron,iron).Slot(1).Tube(Enumerable.Range(0,25).Select(i=>mirror+new Vector3(Mathf.Cos(i*Mathf.PI/12)*.75f,2.2f+Mathf.Sin(i*Mathf.PI/12)*.75f,0)).ToArray(),Enumerable.Repeat(.06f,25).ToArray(),6,brass,brass,1,1,false);
                s.Slot(3).Disc(mirror+new Vector3(0,2.2f,.02f),new Vector3(-.3f,0,-1),.72f,24,new Color(1,.86f,.66f));
                // A signal mast at the head with flags that swing; a bell on its frame on the deck below.
                var mast=new Vector3(-87.2f,Raised,-6.0f);s.Slot(0).Post(mast,7.5f,.1f,.07f,6,iron,iron);
                var helm=kit.Built("The helm drawn",s,Iron,Brass,Timber,Lamp);helm.transform.SetParent(holder.transform,true);
                ZoneKit.Block(helm,wheelFoot+Vector3.up*.6f,new Vector3(.6f,1.2f,.6f),0,"Wheel pedestal");ZoneKit.Block(helm,mirror+Vector3.up*.8f,new Vector3(.3f,1.6f,.3f),0,"Mirror post");ZoneKit.Block(helm,mast+Vector3.up*1,new Vector3(.3f,2,.3f),0,"Signal mast");
                for(int k=0;k<4;k++)
                {
                    var flag=new ZoneKit.Shape();var c=k%2==0?new Color(.86f,.8f,.6f):new Color(.3f,.5f,.62f);flag.Slot(0).Quad(Vector3.zero,new Vector3(.9f,0,0),new Vector3(.9f,-.6f,0),new Vector3(0,-.6f,0),Vector3.back,c,.5f);
                    var hung=Pivoted(kit,Name("Signal flag"),flag,mast+new Vector3(.08f,6.9f-k*.8f,0),helm.transform,Cloth);Swings(hung.transform,.8f,.05f*k);
                }
                var bell=new ZoneKit.Shape();bell.Slot(0).Tube(new[]{new Vector3(0,-.15f,0),new Vector3(0,-.75f,0)},new[]{.18f,.36f},14,brass,new Color(1,.8f,.5f)).Tube(new[]{Vector3.zero,new Vector3(0,-.15f,0)},new[]{.03f,.03f},5,brass,brass);
                var frameAt=At(kit,new Vector2(-63.2f,5.4f));var fr=new ZoneKit.Shape();fr.Post(frameAt+new Vector3(-.7f,0,0),2.4f,.07f,.06f,6,iron,iron).Post(frameAt+new Vector3(.7f,0,0),2.4f,.07f,.06f,6,iron,iron).Beam(frameAt+new Vector3(-.8f,2.4f,0),frameAt+new Vector3(.8f,2.4f,0),.08f,.08f,iron);
                var frame=kit.Built("The helm's bell frame",fr,Iron);frame.transform.SetParent(holder.transform,true);ZoneKit.Block(frame,frameAt+Vector3.up*1.2f,new Vector3(1.6f,2.4f,.3f),0,"Bell frame");
                var rung=Pivoted(kit,"The helm's bell",bell,frameAt+Vector3.up*2.36f,frame.transform,Brass);Swings(rung.transform,.6f);
                // The steering gear on the helm hall's deck: cable drums, the cables running out over sheaves to both treads.
                var gear=new ZoneKit.Shape();foreach(float z in new[]{-6.6f,6.2f}){var drum=At(kit,new Vector2(-70,z));gear.Slot(0).Box(drum+Vector3.up*.35f,new Vector3(2.2f,.7f,1.2f),Quaternion.identity,iron).Slot(1).Tube(new[]{drum+new Vector3(-.8f,1.0f,0),drum+new Vector3(.8f,1.0f,0)},new[]{.42f,.42f},14,new Color(.6f,.5f,.36f),new Color(.6f,.5f,.36f));
                    gear.Slot(2).Rope(drum+new Vector3(0,1.4f,Mathf.Sign(z)*.4f),new Vector3(-70,1.6f,Mathf.Sign(z)*(Mathf.Abs(Halls[0].z0)+.9f)),.2f,.035f,new Color(.3f,.3f,.3f),5,6);}
                var steering=kit.Built("The steering gear",gear,Iron,Brass,Rope);steering.transform.SetParent(holder.transform,true);
                foreach(float z in new[]{-6.6f,6.2f})ZoneKit.Block(steering,At(kit,new Vector2(-70,z))+Vector3.up*.6f,new Vector3(2.2f,1.2f,1.2f),0,"Steering drum");
                foreach(var at in new[]{new Vector2(-64.5f,-7.6f),new Vector2(-72.8f,-7.6f)})LampPost(kit,at,float.NaN,90,holder.transform);
            }
            static void Market(ZoneKit kit)
            {
                var holder=new GameObject("Market street");
                // The street's north side: houses backed onto the north rail, booths in front of them; the rail walk at the north-east corner opens on the halls alongside.
                foreach(var (x0,x1,tall,plate,seed,lamp) in new[]{(-53.4f,-47.2f,5.2f,new Color(.6f,.5f,.44f),21,1f),(-46.4f,-40.6f,6.4f,new Color(.5f,.54f,.6f),22,-1f),(-39.8f,-33.6f,5.6f,new Color(.62f,.56f,.46f),23,1f),(-32.8f,-29.6f,4.4f,new Color(.54f,.5f,.5f),24,0f)})
                    House(kit,"Market house",x0,x1,4.6f,9.3f,Deck,tall,plate,seed,lamp).transform.SetParent(holder.transform,true);
                foreach(var (x,wide,cloth,seed) in new[]{(-50.4f,3.4f,new Color(.3f,.5f,.62f),31),(-44,3.6f,new Color(.84f,.66f,.3f),32),(-31,3.2f,new Color(.4f,.56f,.36f),34)})
                    Booth(kit,"Market booth",new Vector2(x,1.6f),wide,cloth,seed).transform.SetParent(holder.transform,true);
                // Over the street, off the house fronts: signs on brackets and laundry between the upper floors (north of the street, so nothing hangs between the camera and anyone walking it).
                var line=new ZoneKit.Shape();Color iron=new(.28f,.28f,.31f);
                foreach(var (xa,xb,y) in new[]{(-52.6f,-41.2f,6.2f),(-39.4f,-30.4f,5.9f)})
                {
                    line.Slot(0).Beam(new Vector3(xa,y,4.4f),new Vector3(xa,y,4.0f),.05f,.05f,iron).Beam(new Vector3(xb,y,4.4f),new Vector3(xb,y,4.0f),.05f,.05f,iron).Slot(1).Rope(new Vector3(xa,y,4.0f),new Vector3(xb,y,4.0f),.4f,.015f,new Color(.7f,.64f,.5f),4,12);
                    var rng=new System.Random((int)(xa*10));
                    for(float x=xa+.7f;x<xb-.5f;x+=.9f+(float)rng.NextDouble()*.4f)
                    {
                        float t=(x-xa)/(xb-xa),sag=.4f*4*t*(1-t);var c=Color.HSVToRGB((float)rng.NextDouble(),.35f,.8f);var g=new ZoneKit.Shape();float w=.4f+(float)rng.NextDouble()*.25f,hgt=.5f+(float)rng.NextDouble()*.4f;
                        g.Quad(new Vector3(-w*.5f,0,0),new Vector3(w*.5f,0,0),new Vector3(w*.45f,-hgt,0),new Vector3(-w*.45f,-hgt,0),Vector3.back,c,.5f);
                        var hung=Pivoted(kit,Name("Laundry"),g,new Vector3(x,y-sag,4.0f),null,Cloth);hung.transform.SetParent(holder.transform,true);Swings(hung.transform,1.3f,(float)rng.NextDouble()*.15f);
                    }
                }
                var lines=kit.Built("Market washing lines",line,Iron,Rope);lines.transform.SetParent(holder.transform,true);
                // Laundry must be held by something: each hangs from its line, the line from the house fronts.
                foreach(Transform t in holder.transform.Cast<Transform>().Where(t=>t.name.StartsWith("Laundry")).ToArray())t.SetParent(lines.transform,true);
                // The south side: lashed barrels in a rack, a chocked handcart, sacks under a net; lamp posts on the rail side.
                var s=new ZoneKit.Shape();var blocks=new GameObject("Market street's south side");blocks.transform.SetParent(holder.transform,true);
                void Solid(Vector2 at,Vector3 size){ZoneKit.Block(blocks,At(kit,at,size.y*.5f),size,0,"Market street's south side");}
                foreach(float x in new[]{-52.2f,-51.2f,-50.2f}){var f=At(kit,new Vector2(x,-8.6f));s.Slot(0).Tube(new[]{f,f+Vector3.up*.45f,f+Vector3.up*.9f},new[]{.32f,.38f,.32f},12,new Color(.56f,.44f,.32f),new Color(.66f,.52f,.38f),2,1);}
                s.Slot(1).Beam(At(kit,new Vector2(-52.8f,-8.1f),.5f),At(kit,new Vector2(-49.6f,-8.1f),.5f),.05f,.05f,new Color(.3f,.24f,.2f));Solid(new Vector2(-51.2f,-8.6f),new Vector3(3.2f,.95f,.8f));
                var cart=At(kit,new Vector2(-35,-8.3f));s.Slot(0).Box(cart+Vector3.up*.75f,new Vector3(2.2f,.12f,1.3f),Quaternion.identity,new Color(.58f,.46f,.34f));
                foreach(int side in new[]{-1,1}){s.Slot(2).Tube(new[]{cart+new Vector3(-.3f,.45f,side*.72f),cart+new Vector3(-.3f,.45f,side*.6f)},new[]{.44f,.44f},14,iron,iron,1,1);s.Slot(3).Box(cart+new Vector3(-.3f,.12f,side*.66f),new Vector3(.4f,.24f,.22f),Quaternion.identity,new Color(.82f,.62f,.36f));}
                s.Slot(4).Lump(cart+Vector3.up*1.05f,new Vector3(.9f,.3f,.55f),5,.3f,new Color(.82f,.76f,.62f),new Color(.6f,.54f,.42f),10,4);Solid(new Vector2(-35,-8.3f),new Vector3(2.6f,1.3f,1.5f));
                foreach(var (x,z) in new[]{(-23.6f,-8.4f),(-22.8f,-8.9f),(-24.2f,-9f)}){var f=At(kit,new Vector2(x,z));s.Slot(4).Lump(f+Vector3.up*.3f,new Vector3(.42f,.32f,.36f),(int)(x*7),.2f,new Color(.84f,.78f,.64f),new Color(.6f,.54f,.44f),9,5);}
                Solid(new Vector2(-23.5f,-8.8f),new Vector3(1.8f,.7f,1.2f));
                kit.Built("Market street's south side drawn",s,Timber,Iron,Iron,Brass,Cloth).transform.SetParent(blocks.transform,true);
                foreach(var x in new[]{-46f,-27.4f})LampPost(kit,new Vector2(x,-7.4f),Deck,90,holder.transform,x>-30);
                // The rail walk: a bench by the north rail and a mounted spyglass, where people come to look at the halls alongside.
                var walk=new ZoneKit.Shape();var b=At(kit,new Vector2(-24.6f,8.4f));walk.Slot(0).Box(b+Vector3.up*.45f,new Vector3(2.2f,.08f,.5f),Quaternion.identity,new Color(.58f,.46f,.34f)).Box(b+Vector3.up*.22f,new Vector3(2.2f,.44f,.08f),Quaternion.identity,new Color(.5f,.4f,.3f));
                var scope=At(kit,new Vector2(-21.8f,8.7f));walk.Slot(1).Post(scope,1.2f,.06f,.05f,6,new Color(.9f,.7f,.42f),new Color(.9f,.7f,.42f)).Tube(new[]{scope+new Vector3(0,1.3f,-.3f),scope+new Vector3(0,1.42f,.5f)},new[]{.07f,.1f},10,new Color(.95f,.74f,.44f),new Color(1,.84f,.54f));
                var railWalk=kit.Built("The rail walk",walk,Timber,Brass);railWalk.transform.SetParent(holder.transform,true);ZoneKit.Block(railWalk,b+Vector3.up*.3f,new Vector3(2.2f,.6f,.6f),0,"Rail walk bench");ZoneKit.Block(railWalk,scope+Vector3.up*.6f,new Vector3(.3f,1.2f,.3f),0,"Spyglass post");
                // A melon that has rolled loose: it rolls a little each way as the hall lurches, and a child goes after it.
                var melon=new ZoneKit.Shape();melon.Lump(Vector3.zero,Vector3.one*.18f,3,.06f,new Color(.62f,.7f,.36f),new Color(.4f,.48f,.24f),10,6);
                var rolled=Pivoted(kit,"A melon that has rolled",melon,At(kit,new Vector2(-46,-6.2f),.18f),holder.transform,Leaf);
                rolling.Add(new CrawlMotion.Rolling{body=rolled.transform,rest=rolled.transform.position,along=Vector3.right,reach=1.3f,radius=.18f,lag=Halls[1].lag});
            }
            static void Yard(ZoneKit kit)
            {
                var holder=new GameObject("The yard");
                House(kit,"The oil shop",-13.6f,-5.6f,6.6f,9.3f,Deck,4.6f,new Color(.44f,.46f,.5f),41,1).transform.SetParent(holder.transform,true);
                House(kit,"The tread shop",7.2f,16.8f,6.8f,9.3f,Deck,5.4f,new Color(.56f,.5f,.46f),42,-1).transform.SetParent(holder.transform,true);
                // The oil shop's tank on stilts, pipes down to the treads' greasing points; the tread shop's spare link stood on its end as a sign of the trade (the yard's landmark).
                var s=new ZoneKit.Shape();Color iron=new(.3f,.3f,.33f),brass=new(.92f,.72f,.42f);
                var tank=At(kit,new Vector2(-6.6f,8.2f));foreach(var (dx,dz) in new[]{(-.8f,-.6f),(.8f,-.6f),(-.8f,.6f),(.8f,.6f)})s.Slot(0).Beam(tank+new Vector3(dx,0,dz),tank+new Vector3(dx*.8f,2.4f,dz*.8f),.12f,.12f,iron);
                s.Slot(1).Tube(new[]{tank+Vector3.up*2.4f,tank+Vector3.up*4.6f},new[]{1.1f,1.1f},18,new Color(.46f,.44f,.42f),new Color(.56f,.52f,.48f)).Tube(new[]{tank+Vector3.up*4.6f,tank+Vector3.up*5.0f},new[]{1.1f,.2f},18,brass,brass);
                s.Slot(1).Tube(new[]{tank+new Vector3(.9f,2.6f,-.4f),tank+new Vector3(1.6f,1.2f,-1.2f),tank+new Vector3(1.6f,.1f,-1.6f),new Vector3(-5,.1f,-9.4f)},new[]{.08f,.08f,.08f,.08f},6,brass,brass);
                var link=At(kit,new Vector2(16.2f,2.8f));var t=Quaternion.Euler(0,-25,0);
                s.Slot(0).Box(link+Vector3.up*.25f,new Vector3(2.2f,.5f,1.2f),t,iron).Slot(2).Box(link+Vector3.up*1.9f,new Vector3(3.1f,2.4f,.35f),t,new Color(.4f,.38f,.38f),new Color(.5f,.48f,.46f)).Box(link+Vector3.up*1.9f+t*new Vector3(0,0,-.3f),new Vector3(3,.5f,.3f),t,new Color(.5f,.48f,.46f));
                foreach(var (dx,dy) in new[]{(-1.2f,.9f),(1.2f,.9f),(-1.2f,2.9f),(1.2f,2.9f)})s.Slot(1).Disc(link+Vector3.up*dy+t*new Vector3(dx,0,-.2f),t*Vector3.back,.12f,10,brass);
                var drawn=kit.Built("The yard drawn",s,Iron,Brass,Iron);drawn.transform.SetParent(holder.transform,true);ZoneKit.Fadeable(drawn);
                ZoneKit.Block(drawn,tank+Vector3.up*1.2f,new Vector3(1.8f,2.4f,1.5f),0,"Oil tank stilts");ZoneKit.Block(drawn,link+Vector3.up*1.5f,new Vector3(2.4f,3,1.2f),-25,"Spare link");
                // Lashed cargo: crates under straps, chocked drums.
                var c=new ZoneKit.Shape();var blocks=new GameObject("The yard's cargo");blocks.transform.SetParent(holder.transform,true);
                foreach(var (x,z,w,h) in new[]{(-11.4f,-8.2f,1.4f,1.1f),(-10f,-8.4f,1.2f,.9f),(-11f,-8.3f,1.1f,.7f)})
                {var f=At(kit,new Vector2(x,z));float y=x==-11f?1.1f:0;c.Slot(0).Box(f+Vector3.up*(y+h*.5f),new Vector3(w,h,1),Quaternion.Euler(0,(x*13)%8,0),new Color(.6f,.5f,.38f),new Color(.7f,.6f,.46f));c.Slot(1).Box(f+Vector3.up*(y+h*.5f),new Vector3(w+.04f,.08f,1.04f),Quaternion.Euler(0,(x*13)%8,0),new Color(.32f,.28f,.22f));}
                ZoneKit.Block(blocks,At(kit,new Vector2(-10.7f,-8.3f),1),new Vector3(2.8f,2,1.2f),0,"Lashed crates");
                foreach(float x in new[]{-1.6f,-.6f}){var f=At(kit,new Vector2(x,-8.6f));c.Slot(2).Tube(new[]{f,f+Vector3.up*1.0f},new[]{.36f,.36f},12,new Color(.4f,.42f,.46f),new Color(.5f,.52f,.54f),2,1).Slot(3).Box(f+new Vector3(0,.08f,-.42f),new Vector3(.4f,.16f,.16f),Quaternion.identity,new Color(.86f,.66f,.38f));}
                ZoneKit.Block(blocks,At(kit,new Vector2(-1.1f,-8.6f),.5f),new Vector3(1.8f,1,.8f),0,"Chocked drums");
                kit.Built("The yard's cargo drawn",c,Timber,Rope,Iron,Brass).transform.SetParent(blocks.transform,true);
                foreach(var x in new[]{-4f,9f})LampPost(kit,new Vector2(x,-7.4f),Deck,90,holder.transform,x>0);
            }
            static void Garden(ZoneKit kit)
            {
                var holder=new GameObject("The sky-garden");var s=new ZoneKit.Shape();var rng=new System.Random(61);Color soil=new(.36f,.27f,.2f),bed=new(.5f,.42f,.34f),brass=new(.92f,.72f,.42f),iron=new(.3f,.3f,.33f);
                var blocks=new GameObject("The sky-garden's beds");blocks.transform.SetParent(holder.transform,true);
                // Raised beds along the north rail and the west end, strapped down, growing low dusk greens.
                foreach(var (x0,x1,z0,z1) in new[]{(28.6f,35.2f,8.4f,9.4f),(41.6f,44.6f,8.4f,9.4f),(28.6f,29.8f,3.4f,7.6f)})
                {
                    float y=Raised;var c=new Vector3((x0+x1)*.5f,y,(z0+z1)*.5f);var size=new Vector3(x1-x0,.6f,z1-z0);
                    s.Slot(0).Box(c+Vector3.up*.3f,size,Quaternion.identity,bed,soil);
                    for(float x=x0+.3f;x<x1-.2f;x+=.45f)for(float z=z0+.25f;z<z1-.1f;z+=.45f)s.Slot(1).Fern(new Vector3(x,y+.6f,z),.32f+.15f*(float)rng.NextDouble(),5,rng,new Color(.2f,.32f,.22f),new Color(.36f,.52f,.34f));
                    ZoneKit.Block(blocks,c+Vector3.up*.3f,size,0,"Raised bed");
                }
                // Cold frames: glass on iron, lids chained down.
                foreach(var (x,z) in new[]{(31.4f,5.4f),(44.2f,4.2f)})
                {var f=OnFloor(new Vector2(x,z),Raised);s.Slot(2).Box(f+Vector3.up*.25f,new Vector3(1.8f,.5f,1),Quaternion.identity,iron).Slot(3).Box(f+new Vector3(0,.62f,0),new Vector3(1.7f,.04f,.9f),Quaternion.Euler(-14,0,0),Color.white);ZoneKit.Block(blocks,f+Vector3.up*.35f,new Vector3(1.8f,.7f,1),0,"Cold frame");}
                // The water tank on its stand, a hose coiled.
                var tank=OnFloor(new Vector2(33.4f,6.9f),Raised);s.Slot(2).Tube(new[]{tank,tank+Vector3.up*1.6f},new[]{.75f,.75f},16,new Color(.4f,.46f,.46f),new Color(.5f,.56f,.54f)).Slot(4).Tube(new[]{tank+Vector3.up*1.6f,tank+Vector3.up*1.75f},new[]{.78f,.6f},16,brass,brass);
                ZoneKit.Block(blocks,tank+Vector3.up*.8f,new Vector3(1.5f,1.6f,1.5f),0,"Water tank");
                var drawn=kit.Built("The sky-garden drawn",s,Timber,Leaf,Iron,Glass,Brass);drawn.transform.SetParent(blocks.transform,true);
                // Trees in tubs on gimbal rings, so the tub hangs level whichever way the hall leans: they swing with it.
                foreach(var (x,z,seed) in new[]{(30.6f,1.6f+2.2f,1),(48.4f,4.2f,2),(39.2f,8.6f,3)})
                {
                    var foot=OnFloor(new Vector2(x,z),Raised);var g=new ZoneKit.Shape();
                    foreach(int side in new[]{-1,1})g.Slot(0).Post(foot+new Vector3(side*.85f,0,0),1.3f,.06f,.05f,6,iron,iron);
                    g.Slot(1).Tube(Enumerable.Range(0,25).Select(i=>foot+new Vector3(Mathf.Cos(i*Mathf.PI/12)*.85f,1.25f,Mathf.Sin(i*Mathf.PI/12)*.85f)).ToArray(),Enumerable.Repeat(.05f,25).ToArray(),6,brass,brass,1,1,false);
                    var ring=kit.Built(Name("Gimbal ring"),g,Iron,Brass);ring.transform.SetParent(holder.transform,true);ZoneKit.Block(ring,foot+Vector3.up*.6f,new Vector3(1.8f,1.2f,.3f),0,"Gimbal");
                    var t=new ZoneKit.Shape();var r=new System.Random(seed);
                    t.Slot(0).Tube(new[]{new Vector3(0,-.9f,0),new Vector3(0,-.15f,0)},new[]{.48f,.62f},14,new Color(.5f,.4f,.3f),new Color(.6f,.48f,.36f)).Slot(1).Tube(new[]{new Vector3(0,-.2f,0),new Vector3(.1f,1.2f,0),new Vector3(-.05f,2.0f,.1f)},new[]{.1f,.08f,.05f},7,new Color(.36f,.28f,.22f),new Color(.4f,.32f,.24f));
                    for(int k=0;k<7;k++)t.Slot(2).Lump(new Vector3(((float)r.NextDouble()-.5f)*1.1f,1.7f+(float)r.NextDouble()*.8f,((float)r.NextDouble()-.5f)*1.1f),new Vector3(.55f,.42f,.55f),seed*9+k,.3f,new Color(.3f,.42f,.3f),new Color(.18f,.26f,.2f),9,5);
                    for(int k=0;k<4;k++)t.Slot(3).Lump(new Vector3(((float)r.NextDouble()-.5f)*1.2f,1.5f+(float)r.NextDouble()*.6f,-.5f),Vector3.one*.07f,k,.05f,new Color(.95f,.66f,.3f),new Color(.8f,.46f,.2f),6,4);
                    var tub=Pivoted(kit,Name("Tree in a gimbal tub"),t,foot+Vector3.up*1.25f,ring.transform,Timber,Timber,Leaf,Leaf);Swings(tub.transform,.35f);
                }
                // Vines trailing over the sky-garden's south face, swinging.
                for(float x=28.8f;x<49.6f;x+=1.3f+(float)rng.NextDouble()*.8f)
                {
                    if(x>GardenStair.xMin-.5f&&x<GardenStair.xMax+.5f)continue;
                    var v=new ZoneKit.Shape();float drop=.8f+(float)rng.NextDouble()*1.2f;
                    for(int k=0;k<4;k++)v.Frond(new Vector3(((float)rng.NextDouble()-.5f)*.4f,0,0),new Vector3(((float)rng.NextDouble()-.5f)*.3f,0,-1),drop,.22f,-.2f,drop*.9f,new Color(.24f,.36f,.24f),new Color(.36f,.5f,.32f),3);
                    var vine=Pivoted(kit,Name("Trailing vine"),v,new Vector3(x,Raised+.05f,GardenDeck.yMin+.05f),drawn.transform,Leaf);Swings(vine.transform,.9f);
                }
                // Washing on a line along the garden's east rail, north to south, clear of the walk to Mira's lot (a pole half a metre off it stood on the way).
                var line=new ZoneKit.Shape();var a=OnFloor(new Vector2(49.2f,3.4f),Raised);var b=OnFloor(new Vector2(49.2f,7.4f),Raised);
                line.Slot(0).Post(a,2.2f,.05f,.04f,6,iron,iron).Post(b,2.2f,.05f,.04f,6,iron,iron).Slot(1).Rope(a+Vector3.up*2.1f,b+Vector3.up*2.1f,.18f,.012f,new Color(.7f,.64f,.5f),4,8);
                var poles=kit.Built("The sky-garden's washing line",line,Iron,Rope);poles.transform.SetParent(holder.transform,true);ZoneKit.Block(poles,a+Vector3.up*1,new Vector3(.2f,2,.2f),0,"Washing pole");ZoneKit.Block(poles,b+Vector3.up*1,new Vector3(.2f,2,.2f),0,"Washing pole");
                for(int k=0;k<3;k++){var g=new ZoneKit.Shape();var col=Color.HSVToRGB(.1f+.3f*k,.3f,.85f);g.Quad(new Vector3(0,0,-.25f),new Vector3(0,0,.25f),new Vector3(0,-.6f,.22f),new Vector3(0,-.6f,-.22f),Vector3.left,col,.5f);
                    var hung=Pivoted(kit,Name("Garden laundry"),g,a+new Vector3(0,2.0f-.15f*Mathf.Sin((k+1)*Mathf.PI/4),.9f+k*1.1f),poles.transform,Cloth);Swings(hung.transform,1.2f);}
                foreach(var at in new[]{new Vector2(27.6f,-7.4f),new Vector2(45,-7.4f)})LampPost(kit,at,Deck,90,holder.transform,at.x>40);
            }

            // ---------------------------------------------------------------- the Crawlers
            /// <summary>Where a Crawler's body is in its own space (Dorrow's, at build one): measured once from its mesh in the pose it was bound in, so what's drawn on it sits on it.</summary>
            static float bodyTall=1.3f,backAt=-.28f,frontAt=.24f,headTop=1.3f,neckAt=.95f;static bool measured;
            static void Measure()
            {
                if(measured)return;measured=true;
                var prefab=ZoneKit.Prefab("Characters","DorrowBody");if(prefab==null)return;var probe=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try
                {
                    var skin=probe.GetComponentInChildren<SkinnedMeshRenderer>();if(skin==null)return;var baked=new Mesh();skin.BakeMesh(baked);var to=skin.transform.localToWorldMatrix;
                    var pts=baked.vertices.Select(p=>to.MultiplyPoint3x4(p)).ToArray();UnityEngine.Object.DestroyImmediate(baked);
                    float top=pts.Max(p=>p.y);bodyTall=top;headTop=top;
                    var torso=pts.Where(p=>p.y>top*.45f&&p.y<top*.7f&&Mathf.Abs(p.x)<.18f).ToArray();if(torso.Length>0){backAt=torso.Min(p=>p.z);frontAt=torso.Max(p=>p.z);}
                    var neck=pts.Where(p=>Mathf.Abs(p.x)<.08f&&p.y>top*.6f).ToArray();
                    Debug.Log($"HOME_CRAWL_BODY tall={bodyTall:0.00} back={backAt:0.00} front={frontAt:0.00} verts={pts.Length}");
                }
                finally{UnityEngine.Object.DestroyImmediate(probe);}
            }
            static Transform Bone(GameObject model,string name)=>model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name)??model.transform;
            /// <summary>A Crawler with no name: Dorrow's body in one of the town's coats, at a build, dressed in code (what they wear) and holding what they're working with.
            /// Standing in a static box the bake reads, or walking (a <see cref="WalkLoop"/>, out of the bake).</summary>
            static GameObject Crawler(ZoneKit kit,string name,Vector3 at,float yaw,int coat,float build,string dress,string carry,bool solid=true)
            {
                Measure();
                var person=new GameObject(name);person.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));person.isStatic=solid;
                // The young are in Tam's body (the apprentice's own coat and goggles a size too big), so a street has more than one Crawler in it; everyone else is Dorrow's, re-dyed.
                bool young=build<.75f&&ZoneKit.Prefab("Characters","TamBody")!=null;
                var prefab=young?ZoneKit.Prefab("Characters","TamBody"):ZoneKit.Prefab("Characters","DorrowBody")??ZoneKit.Prefab("Characters","Hal");var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab);model.transform.SetParent(person.transform,false);model.name="Generated civilian";
                if(!young&&coats!=null&&coats.Length>0&&coats[coat%coats.Length]!=null)foreach(var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.sharedMaterial=coats[coat%coats.Length];PrefabUtility.RecordPrefabInstancePropertyModifications(skin);}
                if(solid){var box=person.AddComponent<BoxCollider>();box.size=new Vector3(.7f,1.5f,.7f);box.center=Vector3.up*.75f;}
                person.AddComponent<PropTint>().tint=Color.white;
                foreach(var rig in model.GetComponentsInChildren<Animator>()){rig.cullingMode=AnimatorCullingMode.CullCompletely;PrefabUtility.RecordPrefabInstancePropertyModifications(rig);}
                person.transform.localScale=Vector3.one*build;
                Dress(kit,person,model,dress,coat);Carry(kit,person,carry);
                return person;
            }
            /// <summary>What a Crawler wears over the coat: a pack, a tool rack, a scarf, a second pair of goggles pushed up, a cap, an apron, a rope coil. Each on the bone it moves
            /// with, so an idle shrug carries it.</summary>
            static void Dress(ZoneKit kit,GameObject person,GameObject model,string dress,int coat)
            {
                if(string.IsNullOrEmpty(dress))return;var root=person.transform;
                Vector3 P(float x,float y,float z)=>root.TransformPoint(new Vector3(x,y,z));
                foreach(var item in dress.Split(','))
                {
                    var s=new ZoneKit.Shape();Material[] mats;Transform bone;Color leather=new(.3f,.24f,.2f),brass=new(.92f,.72f,.42f);var scarf=Color.HSVToRGB(new[]{.58f,.02f,.12f,.08f,.95f}[coat%5],.45f,.78f);
                    float h=bodyTall;
                    switch(item.Trim())
                    {
                        case "pack":
                            s.Slot(0).Lump(P(0,h*.6f,backAt-.13f),new Vector3(.24f,.26f,.15f)*root.lossyScale.x,coat+5,.12f,new Color(.5f,.42f,.3f),new Color(.36f,.3f,.22f),9,5);
                            s.Slot(1).Box(P(0,h*.76f,backAt-.12f),new Vector3(.4f,.06f,.2f)*root.lossyScale.x,root.rotation,new Color(.8f,.74f,.6f));
                            foreach(int side in new[]{-1,1})s.Slot(0).Beam(P(side*.12f,h*.72f,backAt-.02f),P(side*.13f,h*.74f,frontAt*.5f),.04f*root.lossyScale.x,.015f,leather);
                            mats=new[]{Cloth,Rope};bone=Bone(model,"Spine02");break;
                        case "rack":
                            foreach(int side in new[]{-1,1})s.Slot(0).Beam(P(side*.16f,h*.42f,backAt-.08f),P(side*.16f,h*1.12f,backAt-.1f),.04f*root.lossyScale.x,.04f*root.lossyScale.x,new Color(.4f,.32f,.24f));
                            for(int k=0;k<3;k++)s.Slot(1).Beam(P(-.12f+k*.12f,h*.5f,backAt-.12f),P(-.12f+k*.12f,h*1.22f,backAt-.14f),.03f*root.lossyScale.x,.03f*root.lossyScale.x,new Color(.5f,.5f,.54f)).Box(P(-.12f+k*.12f,h*1.22f,backAt-.14f),new Vector3(.12f,.05f,.05f)*root.lossyScale.x,root.rotation,new Color(.5f,.5f,.54f));
                            mats=new[]{Timber,Iron};bone=Bone(model,"Spine02");break;
                        case "scarf":
                            var ring=Enumerable.Range(0,13).Select(i=>P(Mathf.Cos(i*Mathf.PI/6)*.2f,h*.73f,(frontAt+backAt)*.5f+Mathf.Sin(i*Mathf.PI/6)*.22f)).ToArray();
                            s.Slot(0).Tube(ring,Enumerable.Repeat(.06f*root.lossyScale.x,13).ToArray(),6,scarf,scarf,1,1,false).Tube(new[]{P(.12f,h*.72f,frontAt-.02f),P(.16f,h*.55f,frontAt+.04f),P(.14f,h*.42f,frontAt+.02f)},new[]{.05f*root.lossyScale.x,.045f*root.lossyScale.x,.04f*root.lossyScale.x},5,scarf,ZoneKit.Shape.Dim(scarf,.8f));
                            mats=new[]{Cloth};bone=Bone(model,"Spine02");break;
                        case "goggles":
                            foreach(int side in new[]{-1,1})s.Slot(0).Disc(P(side*.075f,h*.985f,frontAt*.55f),(root.up*.7f+root.forward*.7f).normalized,.06f*root.lossyScale.x,10,brass,.035f*root.lossyScale.x);
                            // Lenses only, pushed up on the hood: a strap ring round the head caught the rim light and read as a halo over every Crawler (look passes 01, 02).
                            mats=new[]{Brass};bone=Bone(model,"Head");break;
                        case "cap":
                            s.Slot(0).Lump(P(0,h*1.0f,-.01f),new Vector3(.19f,.07f,.2f)*root.lossyScale.x,coat,.05f,ZoneKit.Shape.Dim(scarf,.7f),ZoneKit.Shape.Dim(scarf,.5f),10,4).Box(P(0,h*.985f,.15f),new Vector3(.22f,.025f,.12f)*root.lossyScale.x,root.rotation,ZoneKit.Shape.Dim(scarf,.6f));
                            mats=new[]{Cloth};bone=Bone(model,"Head");break;
                        case "apron":
                            s.Slot(0).Quad(P(-.2f,h*.62f,frontAt+.03f),P(.2f,h*.62f,frontAt+.03f),P(.22f,h*.28f,frontAt-.02f),P(-.22f,h*.28f,frontAt-.02f),root.forward,new Color(.5f,.4f,.3f),.5f);
                            mats=new[]{Cloth};bone=Bone(model,"Hips");break;
                        case "coil":
                            var c=Enumerable.Range(0,13).Select(i=>P(Mathf.Cos(i*Mathf.PI/6)*.26f,h*.6f+Mathf.Sin(i*Mathf.PI/6)*.2f,backAt+.05f+Mathf.Sin(i*Mathf.PI/6)*.12f)).ToArray();
                            for(int k=0;k<3;k++)s.Slot(0).Tube(c.Select(p=>p+root.right*(k*.03f*root.lossyScale.x)).ToArray(),Enumerable.Repeat(.03f*root.lossyScale.x,13).ToArray(),5,new Color(.74f,.64f,.46f),new Color(.74f,.64f,.46f),1,1,false);
                            mats=new[]{Rope};bone=Bone(model,"Spine02");break;
                        default:continue;
                    }
                    var worn=kit.Built(Name(person.name+" "+item.Trim()),s,mats);worn.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;worn.transform.SetParent(bone,true);
                }
            }
            /// <summary>What a Crawler is holding, which is how a still says what they're doing: a long-spouted oil can, an oiler's pole over the rail, a coupling hammer, a slate, a spyglass,
            /// a watering can, a pot, parcels.</summary>
            static void Carry(ZoneKit kit,GameObject who,string carry)
            {
                if(string.IsNullOrEmpty(carry))return;var s=new ZoneKit.Shape();Material[] mats;float h=bodyTall;Color brass=new(.92f,.72f,.42f),iron=new(.34f,.34f,.36f),wood=new(.5f,.4f,.3f);
                switch(carry)
                {
                    case "oilcan":s.Slot(0).Tube(new[]{new Vector3(.3f,h*.38f,.12f),new Vector3(.3f,h*.52f,.12f)},new[]{.1f,.08f},10,brass,brass).Tube(new[]{new Vector3(.3f,h*.5f,.18f),new Vector3(.32f,h*.42f,.7f)},new[]{.02f,.012f},5,brass,brass);mats=new[]{Brass};break;
                    case "pole":s.Slot(0).Tube(new[]{new Vector3(.28f,h*.45f,-.3f),new Vector3(.3f,h*.95f,1.6f)},new[]{.03f,.025f},5,wood,wood).Slot(1).Tube(new[]{new Vector3(.3f,h*.92f,1.55f),new Vector3(.3f,h*.82f,1.85f)},new[]{.08f,.06f},8,brass,brass);mats=new[]{Timber,Brass};break;
                    case "hammer":s.Slot(0).Tube(new[]{new Vector3(.3f,h*.3f,.2f),new Vector3(.32f,h*.75f,.5f)},new[]{.025f,.025f},5,wood,wood).Slot(1).Box(new Vector3(.32f,h*.78f,.52f),new Vector3(.12f,.12f,.3f),Quaternion.Euler(-30,0,0),iron);mats=new[]{Timber,Iron};break;
                    case "slate":s.Slot(0).Box(new Vector3(.12f,h*.5f,.32f),new Vector3(.36f,.26f,.03f),Quaternion.Euler(-35,0,0),new Color(.16f,.17f,.18f)).Slot(1).Box(new Vector3(.18f,h*.52f,.3f),new Vector3(.12f,.015f,.005f),Quaternion.Euler(-35,0,10),new Color(.92f,.92f,.88f));mats=new[]{Dark,Cloth};break;
                    case "glass":s.Slot(0).Tube(new[]{new Vector3(.05f,h*.88f,.18f),new Vector3(.05f,h*.9f,.7f)},new[]{.04f,.06f},10,brass,new Color(1,.84f,.54f));mats=new[]{Brass};break;
                    case "watering":s.Slot(0).Tube(new[]{new Vector3(.3f,h*.3f,.1f),new Vector3(.3f,h*.48f,.1f)},new[]{.13f,.12f},10,new Color(.5f,.6f,.58f),new Color(.56f,.66f,.62f)).Tube(new[]{new Vector3(.3f,h*.36f,.22f),new Vector3(.3f,h*.5f,.55f)},new[]{.025f,.02f},5,new Color(.5f,.6f,.58f),new Color(.5f,.6f,.58f));mats=new[]{Iron};break;
                    case "pot":s.Slot(0).Tube(new[]{new Vector3(0,h*.5f,.36f),new Vector3(0,h*.66f,.36f)},new[]{.17f,.18f},10,new Color(.16f,.15f,.15f),new Color(.26f,.24f,.22f),1,1,false).Slot(1).Lump(new Vector3(0,h*.68f,.36f),new Vector3(.14f,.03f,.14f),2,.1f,new Color(.2f,.12f,.08f),new Color(.12f,.08f,.06f),8,3);mats=new[]{Iron,Soil};break;
                    case "parcels":for(int k=0;k<3;k++)s.Slot(0).Box(new Vector3(0,h*.5f+k*.13f,.3f),new Vector3(.34f-k*.04f,.12f,.24f),Quaternion.Euler(0,k*14,0),new Color(.88f,.82f,.68f));mats=new[]{Cloth};break;
                    case "wrench":s.Slot(0).Beam(new Vector3(.3f,h*.25f,.15f),new Vector3(.3f,h*.62f,.2f),.05f,.03f,iron).Box(new Vector3(.3f,h*.64f,.2f),new Vector3(.18f,.08f,.06f),Quaternion.identity,iron);mats=new[]{Iron};break;
                    default:return;
                }
                var held=kit.Built(Name(who.name+" carries"),s,mats);held.transform.SetParent(who.transform,false);held.transform.localPosition=Vector3.zero;held.transform.localRotation=Quaternion.identity;
                held.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
            /// <summary>People who live here, at work only a moving town has: name, where, floor, facing, coat (rust, olive, slate, soot, teal), build, what they wear, what they hold.
            /// Off every way (two metres), off the signs, out of the camera's line to anyone named; never two neighbours alike.</summary>
            static readonly (string name,Vector2 at,float floor,float yaw,int coat,float build,string dress,string carry)[] Folk=
            {
                // The helm: a mate at the wheel, a signaller at the speaking tubes, a lookout at the forward rail.
                ("Townsperson, at the great wheel",new(-81.9f,.6f),Raised,90,2,1,"goggles",""),("Townsperson, at the speaking tubes",new(-81.8f,-3.6f),Raised,280,4,.94f,"scarf",""),("Townsperson, keeping the forward lookout",new(-86.4f,-1.4f),Raised,260,0,1,"pack","glass"),
                // The market: booth-keepers behind their counters, a cook, shoppers, an oiler at the south rail, a lookout at the rail walk.
                ("Townsperson, keeping a booth",new(-50.4f,2.9f),Deck,180,1,.95f,"apron",""),("Townsperson, keeping a booth",new(-44,3),Deck,180,3,1,"cap",""),("Townsperson, keeping the Lashed Stall",new(-37,2.8f),Deck,180,0,.98f,"apron,goggles",""),
                ("Townsperson, keeping a booth",new(-31,2.9f),Deck,190,2,.94f,"scarf",""),("Townsperson, buying",new(-48.6f,-.4f),Deck,20,4,.9f,"pack","parcels"),
                ("Townsperson, oiling the south tread",new(-49.2f,-8.8f),Deck,180,1,1,"cap","pole"),("Townsperson, at the spyglass",new(-21.9f,7.8f),Deck,0,3,.96f,"coil",""),
                ("Townsperson, on the rail walk",new(-26.4f,8.6f),Deck,330,4,.88f,"scarf",""),
                // The yard: the pad's clamp-hand, oilers on the south tread, a crate-lasher, a child, the tread shop's smith.
                ("Townsperson, chocking the pad's clamps",new(6.6f,3.4f),Deck,300,3,1,"goggles","wrench"),("Townsperson, oiling the south tread",new(-6.4f,-8.8f),Deck,180,2,.96f,"apron","pole"),
                ("Townsperson, lashing the crates",new(-8.6f,-7.6f),Deck,250,4,1,"coil",""),("Townsperson, at the tread shop",new(14.6f,5.4f),Deck,200,0,1.04f,"apron,goggles","hammer"),
                ("A child, trying to stand still",new(-3.6f,4.4f),Deck,140,1,.66f,"scarf",""),
                // The sky-garden and the tail: a gardener tying down, a dawn-reader at the glass, one at the slate.
                ("Townsperson, tying down a tree",new(46.9f,4.1f),Raised,90,1,.96f,"cap",""),("Townsperson, reading the dawn",new(56.6f,-8.2f),Deck,90,2,1,"goggles,pack","glass"),
                ("Townsperson, chalking the readings",new(53.6f,-7.6f),Deck,100,0,.94f,"scarf","slate"),("Townsperson, resting by the gimbal tree",new(32.3f,4.2f),Raised,200,3,.9f,"pack",""),
            };
            /// <summary>People on errands: body, the loop, there and back, what they're about (which is what they hold).</summary>
            static readonly (Vector2[] loop,float floor,int coat,float build,string dress,string carry,string name)[] Errands=
            {
                (new[]{new Vector2(-59.2f,-5.0f),new Vector2(-54.8f,-5.0f)},Deck,0,1,"cap","hammer","A Crawler walking the coupling with a hammer"),
                (new[]{new Vector2(-19.2f,-5.0f),new Vector2(-14.8f,-5.0f)},Deck,3,.96f,"goggles","hammer","A Crawler walking the coupling with a hammer"),
                (new[]{new Vector2(10.2f,-8.0f),new Vector2(16.2f,-8.0f)},Deck,4,.94f,"pack","parcels","The mail runner, who never stops"),
                (new[]{new Vector2(-46.4f,-6.0f),new Vector2(-43.6f,-6.2f)},Deck,2,.68f,"scarf","","A child after the melon that has rolled"),
                (new[]{new Vector2(40.4f,7.6f),new Vector2(44.0f,7.6f)},Raised,2,.98f,"apron","watering","A gardener watering the beds"),
                (new[]{new Vector2(-12.2f,1.8f),new Vector2(-12.2f,4.6f)},Deck,1,1,"rack","oilcan","An oiler going between the oil shop and the treads"),
            };
            static void People(ZoneKit kit)
            {
                var holder=new GameObject("Crawlers");
                OnTheDeck(kit,"a townsperson",Folk.Select(f=>f.at).ToArray());
                foreach(var (name,at,floor,yaw,coat,build,dress,carry) in Folk)Crawler(kit,name,new Vector3(at.x,floor,at.y),yaw,coat,build,dress,carry).transform.SetParent(holder.transform,true);
                foreach(var (loop,floor,coat,build,dress,carry,name) in Errands)
                {
                    var walker=Crawler(kit,name,new Vector3(loop[0].x,floor,loop[0].y),0,coat,build,dress,carry,false);walker.transform.SetParent(holder.transform,true);
                    var body=walker.AddComponent<CapsuleCollider>();body.height=1.5f;body.radius=.35f;body.center=Vector3.up*.75f;
                    var walk=walker.AddComponent<WalkLoop>();walk.path=loop.Select(p=>new Vector3(p.x,floor,p.y)).ToArray();walk.speed=.62f*build;walk.pause=1.6f;walk.thereAndBack=true;
                    errands.Add(("DorrowBody",loop,true,name));
                }
            }

            // ---------------------------------------------------------------- checks and the bake
            /// <summary>Every block under the mesh that draws it (Harrowgate's: a block left under a bare holder is an invisible wall to the world tests).</summary>
            static void SeatBlocks()
            {
                foreach(var box in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    if(box.isTrigger||!box.gameObject.isStatic||box.GetComponentInChildren<Renderer>(true)!=null||box.GetComponentInParent<Renderer>(true)!=null||box.GetComponentInParent<Membrane>(true)!=null)continue;
                    var parent=box.transform.parent;var drawn=parent!=null?parent.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault():null;
                    // Tread-hall four's walked porch has nothing drawn of its own: it goes under the drawn porch that rolls in, which is where it is once coupled.
                    if(drawn==null&&hallWalked!=null&&box.transform.IsChildOf(hallWalked.transform))drawn=hallDrawn.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r=>r.name.StartsWith("Tread-hall four's gangway"));
                    if(drawn!=null&&!(hallWalked!=null&&box.transform.IsChildOf(hallWalked.transform)))box.transform.SetParent(drawn.transform,true);
                    else if(drawn==null)Debug.LogWarning("HOME_CRAWL_BLOCK_UNDRAWN "+box.name+" at "+box.transform.position);
                }
            }
            const float WayClear=.9f,FolkClear=2;
            /// <summary>Refuses the finished town if anything solid the builder stood in it cuts a way: its footprint within <see cref="WayClear"/> of a way's line, a townsperson within
            /// <see cref="FolkClear"/>, or a walker's loop within 1.6 m. Every state of the town at once (tread-hall four in, Lo there). The named stand at the ways' ends.</summary>
            static void NothingStandsOnTheWays()
            {
                var hidden=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>!t.gameObject.activeSelf).Select(t=>t.gameObject).ToArray();
                foreach(var go in hidden)go.SetActive(true);
                try{Check();}finally{foreach(var go in hidden)go.SetActive(false);}
                void Check()
                {
                    Physics.SyncTransforms();var faults=new List<string>();var ways=Ways.Concat(new[]{ToTheHall}).ToArray();
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
                    foreach(var solid in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    {
                        if(!solid.enabled||solid.isTrigger||solid.GetComponentInParent<Npc>(true)!=null||solid.GetComponentInParent<Membrane>(true)!=null||solid.GetComponentInParent<NavMeshObstacle>(true)!=null||solid.GetComponent<WalkLoop>()!=null)continue;
                        // The rails, bridges, gangway and porch are what the ways run between and over.
                        if(solid.name.StartsWith("Rail")||solid.name.StartsWith("Bridge deck")||solid.name.StartsWith("Gangway deck")||solid.name.StartsWith("Porch")||solid.name.StartsWith("Pad deck")||solid is MeshCollider)continue;
                        var bounds=solid.bounds;if(bounds.size.x>40||bounds.size.z>40)continue;
                        var at=new Vector2(bounds.center.x,bounds.center.z);if(bounds.max.y<kit0.Y(at.x,at.y)+.32f)continue;
                        bool folk=solid.name.StartsWith("Townsperson")||solid.name.StartsWith("A child")||solid.transform.name.StartsWith("Lo,");float yaw=0;var half=new Vector2(bounds.extents.x,bounds.extents.z);
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
                    if(faults.Count>0)throw new InvalidOperationException("MeridianCrawl: things stand on the town's ways:\n"+string.Join("\n",faults.Distinct()));
                    Debug.Log("HOME_CRAWL_WAYS_CLEAR");
                }
            }
            static ZoneKit kit0;
            /// <summary>Every errand's loop clear of anything solid at a body's height: a walker stands still for good at whatever is in its way.</summary>
            static void ErrandsWalkClear(ZoneKit kit)
            {
                Physics.SyncTransforms();var blocked=new List<string>();
                foreach(var walk in UnityEngine.Object.FindObjectsByType<WalkLoop>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    var loop=walk.path;
                    for(int k=0;k+1<loop.Length;k++)
                    {
                        var a=loop[k]+Vector3.up*.95f;var b=loop[k+1]+Vector3.up*.95f;var run=b-a;run.y=0;
                        foreach(var hit in Physics.SphereCastAll(a,.38f,run.normalized,run.magnitude,~0,QueryTriggerInteraction.Ignore))
                            if(hit.collider.GetComponentInParent<WalkLoop>()==null&&hit.collider.GetComponentInParent<Npc>()==null)blocked.Add($"{walk.name}: {hit.collider.name} at ({hit.point.x:0.0}, {hit.point.z:0.0})");
                    }
                }
                if(blocked.Count>0)throw new InvalidOperationException("MeridianCrawl: an errand runs into something: "+string.Join("; ",blocked.Distinct()));
            }
            /// <summary>The bake, with tread-hall four in and every obstacle off (the lip's gate carves at play), over the decks only: the plain under the rails stays out of the paths.
            /// Every place a player must stand is listed.</summary>
            static void Bake(ZoneKit kit)
            {
                var shown=new[]{hallDrawn,hallWalked,loHere,loBench}.Where(g=>g!=null).Select(g=>(g,g.activeSelf)).ToArray();foreach(var (g,_) in shown)g.SetActive(true);
                var obstacles=UnityEngine.Object.FindObjectsByType<NavMeshObstacle>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(o=>(o,o.enabled)).ToArray();
                foreach(var (o,_) in obstacles)o.enabled=false;
                var points=new List<Vector2>{DorrowHand,HelmHead,StallHand,TamHand,new Vector2(2,3),YardSpawn,new Vector2(-9,3.4f),new Vector2(11.5f,3.6f),ParcelHand,StairHead,MiraHand,Tail,CageHand,GapSpawn,new Vector2(58,-3)};
                var mustReach=points.Select(p=>kit.On(p.x,p.y)).ToList();mustReach.Add(new Vector3(LoHand.x,Deck,LoHand.y));
                try{kit.BakeNavigation(new Bounds(new Vector3(-8,1.5f,-2),new Vector3(186,13,30)),kit.On(Arrival.x,Arrival.y),mustReach);}
                finally{foreach(var (o,on) in obstacles)o.enabled=on;foreach(var (g,on) in shown)g.SetActive(on);}
            }
            /// <summary>For stills of the town once tread-hall four is back and Lo is on it (<c>-crawl-home</c>): never saved as the zone.</summary>
            static void Home()
            {
                foreach(var g in new[]{hallDrawn,hallWalked,loHere,loBench})if(g!=null)g.SetActive(true);
                foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    if(t.name=="The lip's gate"||t.name=="Dorrow")t.gameObject.SetActive(false);
                    if(t.name=="Dorrow at the lip"||t.name.StartsWith("Townsperson, with Hesk"))t.gameObject.SetActive(true);
                }
            }

            // ---------------------------------------------------------------- the floor's dressing, last
            /// <summary>The ground kit on the decks: boards laid over the plate down the market street, tread plate worn bright in the yard's work places, grit drifted along every rail's
            /// foot and into the corners; oil under the treads' greasing points and the carts, soot by the oil shop, chalked marks at the couplings; lamp pools.</summary>
            static void Ground(ZoneKit kit,ZoneKit.Land land)
            {
                if(!File.Exists(GroundKit.Art+"ground-gravel.png")||!File.Exists(GroundKit.Art+"ground-decals.png")||!File.Exists(GroundKit.Art+"ground-cover.png"))
                {Debug.LogWarning("HOME_CRAWL_GROUND_KIT_PENDING: the ground kit's textures aren't painted yet, so the floor has its vertex tones only");return;}
                ZoneKit.Texture("Ground/ground-crawl-boards",512,(u,v)=>
                {
                    float row=v*8;int board=Mathf.FloorToInt(row);float across=row-board,seam=Mathf.Min(across,1-across);float shift=Hash(board,1,3);
                    float along=Mathf.Repeat(u*2+shift,1);float own=Hash(board,Mathf.FloorToInt(u*2+shift),7),grain=ZoneKit.Tile(u*16+board*1.7f,v*96,96,6);
                    float tone=.62f+.16f*(own-.5f)+.09f*(grain-.5f)-(seam<.06f?.3f:0)-(along<.012f||along>.988f?.25f:0);
                    if(Mathf.Abs(across-.5f)<.06f&&(Mathf.Abs(along-.1f)<.015f||Mathf.Abs(along-.9f)<.015f))tone-=.2f;
                    return new Color(tone*1.04f,tone*.97f,tone*.86f,1);
                });
                ZoneKit.Texture("Ground/ground-crawl-treadplate",512,(u,v)=>
                {
                    float mu=u*16,mv=v*16;int ix=Mathf.FloorToInt(mu),iy=Mathf.FloorToInt(mv);float fu=mu-ix-.5f,fv=mv-iy-.5f;bool flip=((ix+iy)&1)==0;
                    var d=flip?new Vector2(fu*.92f+fv*.38f,-fu*.38f+fv*.92f):new Vector2(fu*.92f-fv*.38f,fu*.38f+fv*.92f);
                    float lozenge=Mathf.Abs(d.x)*.35f+Mathf.Abs(d.y);float tone=.56f+.06f*(ZoneKit.Cloud(u,v,8,3)-.5f);
                    if(lozenge<.16f)tone+=.14f;else if(lozenge<.2f)tone-=.08f;return new Color(tone,tone,tone*.97f,1);
                });
                var ground=GroundKit.Open(kit.zone,land.FloorFilter,97,CliffTop);
                foreach(var way in Ways)ground.ClearLane(way,1.3f);
                ground.Layers(new Vector4(.05f,.34f,.4f,.28f),1.1f,new GroundKit.Layer("crawl-boards",new Color(.9f,.78f,.62f,1),4),new GroundKit.Layer("crawl-treadplate",new Color(.82f,.82f,.84f,1),2),
                    new GroundKit.Layer("gravel",new Color(.34f,.32f,.32f,1),2.4f));
                bool Market(Vector2 p)=>p.x>-54&&p.x<-20;bool Yard(Vector2 p)=>p.x>-14&&p.x<18;
                // The market street's boards, laid over the plate the length of the street and up to the booths.
                ground.Paint(0,p=>Market(p)&&p.y>-6.6f&&p.y<1.2f?1-ZoneKit.Smooth(.2f,.6f,ground.Noise(p,6,4)-.75f):0);
                // The yard's tread plate where work goes on: round the pad, at the oil shop and the tread shop, down to the oilers' places.
                // Tread plate wherever work goes on off the spine: the yard (round the pad, at the shops, down to the oilers), the tail where the readings are taken, the helm's platform.
                bool Work(Vector2 p)=>Yard(p)||(p.x>50&&p.x<60)||Inside(p,HelmDeck)>-.2f;
                // In patches, not a sheet: laid wall to wall, its fine pattern read as one plain grey (look pass 02).
                ground.Paint(1,p=>Work(p)&&Mathf.Abs(p.y-SpineZ)>1.8f?ZoneKit.Smooth(.5f,.66f,ground.Noise(p,3.4f,9)):0);
                // Oil-black grit worked into the plate where the work is: tone the plate alone doesn't have under a dusk light.
                ground.Paint(2,p=>Work(p)?.9f*ZoneKit.Smooth(.54f,.7f,ground.Noise(p,2.6f,23)):0);
                // Grit drifted against every rail's foot and into the corners, a little everywhere it isn't walked.
                ground.Paint(2,p=>{float edge=OnDeck(p);return (1-ZoneKit.Smooth(.25f,1.3f,edge))*.85f*ZoneKit.Smooth(.25f,.55f,ground.Noise(p,1.4f,13))+ZoneKit.Smooth(.6f,.85f,ground.Noise(p,3,17))*.5f*ZoneKit.Smooth(1.2f,2.4f,FromWays(p));});
                ground.Fringe(2,0,.3f,.5f);
                ground.Shade(p=>.06f*(1-ZoneKit.Smooth(.5f,1.8f,FromWays(p)))*-1);
                Color oil=new(.14f,.13f,.14f,.75f),soot=new(.2f,.19f,.19f,.7f),chalk=new(.92f,.9f,.86f,.85f);
                ground.Decals(new[]{"Stain","Scorch"},6,p=>OnDeck(p)<2.6f&&(p.y<-7||p.y>7)?1:0,.8f,1.8f,oil,false,.12f,1);
                ground.Decals(new[]{"Stain"},3,p=>Yard(p)&&p.x<-4?1:0,1,2.2f,soot,false,.1f,2);
                ground.Decals(new[]{"Stain","Scorch","CracksB"},7,p=>Work(p)&&FromWays(p)>1.2f?1:0,.7f,1.6f,oil,false,.12f,5);
                ground.Decals(new[]{"Slabs"},4,p=>Work(p)?1:0,.9f,1.8f,new Color(.46f,.46f,.5f,.75f),false,.1f,6);
                ground.Decals(new[]{"Slabs","CracksA"},2,p=>FromWays(p)>1.6f?1:0,.8f,1.6f,new Color(.5f,.5f,.52f,.7f),true,.1f,3);
                ground.Decals(new[]{"Litter"},2,p=>Market(p)&&FromWays(p)>1.2f?1:0,.6f,1.2f,new Color(.62f,.54f,.44f,.75f),false,.12f,4);
                // Chalk at each coupling's deck ends: the hammer-man's marks, and the hall's number.
                foreach(var (x0,x1) in Couplings)foreach(float x in new[]{x0-.9f,x1+.9f})ground.Strip("SurveyLine",new[]{new Vector2(x,-6.6f),new Vector2(x,.6f)},0,.24f,chalk);
                ground.Strip("SurveyLine",new[]{new Vector2(Lip-.9f,-6.6f),new Vector2(Lip-.9f,.6f)},0,.24f,chalk);
                ground.Cover(new[]{"Shards","Twigs"},2,p=>FromWays(p)>2&&OnDeck(p)<2?1:0,.06f,.16f,new Color(.5f,.48f,.46f,1),2,.12f,8);
                ground.Look("Canyon",null).LampPools(2.4f,new Color(1,.78f,.5f,.42f));
                ground.Finish();
            }
        }
    }
}
