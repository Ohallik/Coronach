using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>The Aurora Shear (space combat; rule: riding the currents). Map brief, plan and coordinates: docs/maps/AuroraShear.md. Everything of the zone's own is in
    /// <see cref="ShearZone"/>, so nothing here can collide with the other Hoarfell files' members.</summary>
    public static partial class HoarfellBuilder
    {
        static void BuildAuroraShear()=>ShearZone.Build();

        /// <summary>One ice crystal: an irregular prism of <paramref name="sides"/> faces from its foot along <paramref name="axis"/>, tapering, ending in a point of facets. Every face
        /// flat (its own normal), so it reads as cut ice and never as a smooth lump. Shared by the Shear's ring and Lastlight's ice.</summary>
        internal static void IcePrism(ZoneKit.Shape shape,Vector3 foot,Vector3 axis,float length,float radius,int sides,int seed,Color tip,Color root)
        {
            var rng=new System.Random(seed);axis.Normalize();
            var across=Vector3.Cross(axis,Mathf.Abs(axis.y)>.9f?Vector3.right:Vector3.up).normalized;var other=Vector3.Cross(axis,across);
            float shoulder=length*(.62f+.18f*(float)rng.NextDouble()),taper=.55f+.3f*(float)rng.NextDouble();
            var low=new Vector3[sides];var high=new Vector3[sides];float turn=(float)rng.NextDouble()*6.283f;
            for(int k=0;k<sides;k++)
            {
                float a=turn+k*6.283f/sides+((float)rng.NextDouble()-.5f)*.5f,r=radius*(.75f+.45f*(float)rng.NextDouble());
                var dir=across*Mathf.Cos(a)+other*Mathf.Sin(a);low[k]=foot+dir*r;high[k]=foot+axis*shoulder+dir*r*taper;
            }
            var point=foot+axis*length+(across*((float)rng.NextDouble()-.5f)+other*((float)rng.NextDouble()-.5f))*radius*.3f;
            for(int k=0;k<sides;k++)
            {
                int n=(k+1)%sides;var mid=(low[k]+low[n]+high[k]+high[n])*.25f;var outward=mid-(foot+axis*shoulder*.5f);
                float shade=.86f+.14f*(float)rng.NextDouble();
                shape.Quad(low[k],low[n],high[n],high[k],outward,ZoneKit.Shape.Dim(Color.Lerp(root,tip,.35f),shade),.35f);
                shape.Triangle(high[k],high[n],point,(high[k]+high[n]+point)/3-(foot+axis*shoulder),ZoneKit.Shape.Dim(tip,shade),.35f);
            }
        }

        internal static class ShearZone
        {
            // ---------------------------------------------------------------- the lane
            /// <summary>From the arrival north through the ring to its gap over Hoarfell, about 1.5 km. Everything is placed by how far along it lies and how far to starboard (<see cref="Beside"/>).</summary>
            internal static readonly Vector2[] Lane={new(0,0),new(20,260),new(-30,560),new(30,860),new(0,1160),new(-20,1420),new(-20,1500)};
            internal const float Half=72;
            internal const float HailS=200,CutterS=215,CutterSide=16,TowEndS=320,TowEndSide=34,LanesS=520,TenderS=560,TenderSide=-4,CacheS=640,CacheSide=-50,
                PodS=1020,PodSide=60,LandS=1470,LandSide=-20,FromTownS=1440,FromTownSide=-12,DescentFightS=1360,DescentFightSide=-14,CorridorOverrun=15;
            /// <summary>The currents (docs/maps/AuroraShear.md): each a line of (along, starboard) points in the direction it flows as laid, its half width, its push, the flag it turns
            /// round on, and its light. C5 is laid north to south: it runs against the lane.</summary>
            internal static readonly (string name,(float s,float side)[] line,float half,float push,string turns,Color tone)[] Currents=
            {
                // It begins past the arrival: once turned it runs south, and a party coming back from Many Hands arrived inside it and was carried onto the lane's back wall.
                ("The first current",new[]{(26f,-10f),(120f,-10f),(230f,-12f),(340f,-10f)},9,6,"hoar.idraHailed",new(.45f,1f,.7f,1)),
                // Out to 50 m to starboard at most: at 56 its edge ran into the next stretch's starboard wall where the lane bends right at 560.
                ("Idra's lane",new[]{(250f,34f),(380f,50f),(520f,50f),(640f,40f),(745f,10f)},9,6,"",new(.4f,.95f,.85f,1)),
                ("The short lane",new[]{(350f,-12f),(480f,-6f),(600f,-4f),(720f,0f)},9,7,"",new(.5f,1f,.62f,1)),
                ("The fast lane",new[]{(750f,2f),(900f,8f),(1040f,4f),(1180f,-4f),(1320f,-10f)},11,9,"",new(.36f,1f,.9f,1)),
                ("The counter-current",new[]{(1240f,48f),(1100f,52f),(960f,44f),(840f,40f)},8,6,"",new(.76f,.5f,1f,1)),
                // It ends short of the way down: run on past it, it held a hull flown in from the north 13 m off the warp (QaPlanEarlyTests' dock check).
                ("The descent",new[]{(1310f,-10f),(1380f,-15f),(1438f,-18f)},10,7,"",new(.45f,1f,.78f,1)),
            };
            /// <summary>Where the tow takes the party (from the cutter's wait across to Idra's lane), and where the cutter leads after: it waits at each for the lead ship.</summary>
            internal static readonly (float s,float side)[] Tow={(CutterS,CutterSide),(232,22),(258,30),(290,34),(TowEndS,TowEndSide)};
            internal static readonly (float s,float side)[] Leads={(420,52),(560,52),(700,26),(800,6),(960,6),(1140,-4),(1300,-10),(1440,-18)};
            /// <summary>LookDev's stills of the Shear (registered in LookDev.Views): the named places along the lane, the lane between them, two wide.</summary>
            internal static LookDev.View[] Views=>new[]{new LookDev.View("arrival",new Vector3(0,1,8),true),new LookDev.View("first-current",Beside(110,-8),true),new LookDev.View("hail",Beside(HailS,0),true),
                new LookDev.View("cutter",Beside(CutterS,CutterSide-4),true),new LookDev.View("tow-end",Beside(TowEndS,TowEndSide),true),new LookDev.View("idras-lane",Beside(450,50),true),
                new LookDev.View("short-lane",Beside(LanesS,TenderSide),true),new LookDev.View("tender",Beside(TenderS,TenderSide),true),new LookDev.View("cache",Beside(CacheS,CacheSide),true),
                new LookDev.View("lane-0700",Beside(700,10),true),new LookDev.View("fast-lane",Beside(860,4),true),new LookDev.View("counter-current",Beside(980,46),true),
                new LookDev.View("wreck",Beside(PodS,PodSide-4),true),new LookDev.View("fast-1",Beside(1080,2),true),new LookDev.View("lane-1200",Beside(1200,-4),true),
                new LookDev.View("descent",Beside(1380,-14),true),new LookDev.View("way-down",Beside(LandS,LandSide),true),new LookDev.View("ring-wide",Beside(680,0),true,3.5f),
                new LookDev.View("descent-wide",Beside(1360,10),true,3f)};
            static float Length{get{float total=0;for(int i=1;i<Lane.Length;i++)total+=Vector2.Distance(Lane[i-1],Lane[i]);return total;}}
            internal static Vector2 Along(float s,out Vector2 heading)
            {
                heading=Vector2.up;
                for(int i=1;i<Lane.Length;i++)
                {
                    float leg=Vector2.Distance(Lane[i-1],Lane[i]);heading=(Lane[i]-Lane[i-1])/leg;
                    if(s<=leg||i==Lane.Length-1)return Lane[i-1]+heading*s;
                    s-=leg;
                }
                return Lane[0];
            }
            /// <summary>A point beside the lane: so far along, so far to starboard (port is negative), at the flight plane unless told.</summary>
            internal static Vector3 Beside(float s,float starboard,float y=ZoneKit.FlightPlane){var p=Along(s,out var h)+new Vector2(h.y,-h.x)*starboard;return new Vector3(p.x,y,p.y);}
            static float Heading(float s){Along(s,out var h);return Mathf.Atan2(h.x,h.y)*Mathf.Rad2Deg;}
            static Vector2 Flat(Vector3 p)=>new(p.x,p.z);
            /// <summary>Where to stand a thing so far under the plane that it shows on screen at this point of the plane (the flight camera looks down at 48 degrees: ZoneKit.ShowsAt).</summary>
            static Vector3 ShowingAt(Vector3 onPlane,float depth)=>new(onPlane.x,ZoneKit.FlightPlane-depth,onPlane.z+depth/Mathf.Tan(ZoneKit.FlightPitch*Mathf.Deg2Rad));
            /// <summary>An enemy of the chapter's roster if the combat engineer has defined it, else a stand-in that fights the same way, so the lane is flyable before the roster lands.</summary>
            static string Foe(string id,string standIn)=>Find<EnemyDef>(id)!=null?id:standIn;
            static Vector3[] Line((float s,float side)[] points,float y=ZoneKit.FlightPlane)=>points.Select(p=>Beside(p.s,p.side,y)).ToArray();
            /// <summary>How far a plan point is from the nearest current's line, less that current's half width (negative inside one).</summary>
            static float FromCurrents(Vector2 p)
            {
                float best=float.PositiveInfinity;
                foreach(var c in Currents){var line=Line(c.line).Select(Flat).ToArray();best=Mathf.Min(best,ZoneKit.FromPath(p,line)-c.half);}
                return best;
            }
            /// <summary>How near a shard's middle may stand to the way Idra's line takes the two ships, past the shard's own size: the partner is held four metres to starboard of the
            /// lead, a hull is up to 2.2 m (Idra's, the biggest), and two more to spare. In the first player run (November 14) the tow dragged both hulls through a shard at (19.1, 255.9)
            /// on its way to her line, because the currents had carried them 160 m on through her call; D275 holds them now, and this keeps the line itself clear for good.</summary>
            internal const float TowRoom=8.2f;
            /// <summary>The way the line takes the ships, on the plan: from the first current's width where it crosses her hail (a ship riding it is held there through her call, D275) to
            /// the cutter's wait, then along her tow to where it lets go.</summary>
            static float FromTow(Vector2 p)
            {
                Vector2 Plan(float s,float side)=>Flat(Beside(s,side));
                var held=new[]{Plan(HailS,-20),Plan(HailS,0)};var wait=Plan(Tow[0].s,Tow[0].side);
                float best=Mathf.Min(ZoneKit.FromPath(p,held),Mathf.Min(ZoneKit.FromPath(p,new[]{held[0],wait}),ZoneKit.FromPath(p,new[]{held[1],wait})));
                return Mathf.Min(best,ZoneKit.FromPath(p,Tow.Select(t=>Plan(t.s,t.side)).ToArray()));
            }
            /// <summary>What must stay readable over the backdrop, each with the room it needs.</summary>
            static List<(Vector2 at,float clear)> Keep()
            {
                var keep=new List<(Vector2,float)>{(new Vector2(0,6),5),(new Vector2(-14,-4),7),(Flat(Beside(CutterS,CutterSide)),7),(Flat(Beside(PodS,PodSide)),10),(Flat(Beside(CacheS,CacheSide)),7),
                    (Flat(Beside(LandS,LandSide)),16),(Flat(Beside(FromTownS,FromTownSide)),5)};
                return keep;
            }

            // ---------------------------------------------------------------- what the Shear is made of
            internal static Material Ice,IceDeep,Shelf,Plate,Current,Streak,Curtain,Hull,PlanetFace,Lamp;
            static void Materials(ZoneKit kit)
            {
                // Ice: clear and cracked, painted near neutral and tinted in the vertices (white at the tips, blue toward the roots): planes of fracture running through it, a few
                // trapped bubbles, frost along the edges. The shader's ice pools the deep colour inside and puts a fresnel sheen on every edge, so a shard reads as glass in the dark.
                var ice=ZoneKit.Texture("Hoarfell/hoar-ice",512,(u,v)=>
                {
                    float crack=1-Mathf.Abs(2*ZoneKit.Tile(u*3+v*1.3f,v*5,5,7)-1),fine=1-Mathf.Abs(2*ZoneKit.Tile(u*9,v*7+u*2,9,11)-1),cloud=ZoneKit.Cloud(u,v,6,3);
                    float bubble=ZoneKit.Tile(u*48,v*48,48,13)>.94f?.12f:0;
                    float tone=.7f+.18f*(cloud-.5f)+.22f*Mathf.Pow(crack,14)+.1f*Mathf.Pow(fine,18)+bubble;
                    return new Color(tone*.96f,tone,tone*1.04f,1);
                });
                Ice=ZoneKit.Toon("hoar-ice",ice,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_ICE_ON","_Ice");m.SetVector("_IceParams",new Vector4(2.4f,.55f,.45f,.4f));m.SetColor("_IceDeepColor",new Color(.14f,.32f,.5f));
                    m.SetColor("_IceRimColor",new Color(.6f,1f,.95f));m.SetColor("_ShadowTint",new Color(.42f,.52f,.7f));m.SetFloat("_LightFloor",.34f);m.SetColor("_RimColor",new Color(.7f,.95f,1f));
                    m.SetTexture("_EmissionMap",ice);m.SetColor("_EmissionColor",new Color(.05f,.09f,.12f));});
                // The ring's far pieces: the same ice, a little dimmer, so the band below never outshines the lane.
                IceDeep=ZoneKit.Toon("hoar-ice-deep",ice,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_ICE_ON","_Ice");m.SetVector("_IceParams",new Vector4(2.6f,.4f,.5f,.25f));m.SetColor("_IceDeepColor",new Color(.1f,.22f,.38f));
                    m.SetColor("_IceRimColor",new Color(.5f,.85f,.9f));m.SetColor("_ShadowTint",new Color(.36f,.44f,.62f));m.SetFloat("_LightFloor",.3f);m.SetColor("_RimColor",new Color(.5f,.8f,.9f));});
                // A current's ribbon: chevrons pointing the way it flows (u runs along it, one to six metres), a brighter rail along each edge, a body of soft light between, all faded
                // to nothing at its sides. Scrolled by AuroraCurrent at the current's own speed.
                var chevrons=ZoneKit.Texture("Hoarfell/hoar-current",256,(u,v)=>
                {
                    float across=Mathf.Abs(v-.5f)*2,edge=Mathf.Pow(Mathf.Sin(v*Mathf.PI),.7f);
                    float stripe=Mathf.Repeat(u+across*.42f,1),chevron=ZoneKit.Smooth(.0f,.06f,stripe)*(1-ZoneKit.Smooth(.16f,.3f,stripe));
                    float rail=ZoneKit.Smooth(.72f,.86f,across)*(1-ZoneKit.Smooth(.9f,1f,across));
                    float body=.18f+.1f*ZoneKit.Cloud(u,v,4,5);
                    return new Color(1,1,1,Mathf.Clamp01((body+.75f*chevron*(1-across*.35f)+.45f*rail)*edge));
                },true,true);
                Current=kit.Glow("Current",false,Color.white);Current.SetTexture("_BaseMap",chevrons);
                Streak=kit.Glow("Streak",false,new Color(1.3f,1.6f,1.5f,1));
                // An aurora curtain seen from above: rays across it (u runs along the curtain), its lower hem bright and fading upward (v), each ray its own length.
                var rays=ZoneKit.Texture("Hoarfell/hoar-curtain",512,(u,v)=>
                {
                    float ray=ZoneKit.Tile(u*64,.5f,64,9),reach=.35f+.65f*ZoneKit.Tile(u*16,3.5f,16,4),fold=ZoneKit.Cloud(u,v,3,8);
                    float hem=Mathf.Pow(1-v,1.6f)*ZoneKit.Smooth(0,.08f,v),lift=ZoneKit.Smooth(reach,reach-.35f,v);
                    float side=Mathf.Pow(Mathf.Sin(u*Mathf.PI*2*3+fold*2)*.5f+.5f,.6f);
                    return new Color(1,1,1,Mathf.Clamp01((hem*.9f+.55f*lift*(.4f+.6f*ray))*(.5f+.5f*side)));
                },true,true);
                Curtain=kit.Glow("Curtain",false,Color.white);Curtain.SetTexture("_BaseMap",rays);
                // Hoarfell's face for the half the camera sees: an ice cap, pale sea ice cut by dark leads, storm bands, and spiral storms; painted denser than the screen's pixels at
                // the planet's depth (Wendmire's moons: a face spent on the half that shows).
                const int N=1024;
                var face=ZoneKit.Texture("Hoarfell/hoar-planet",N,(u,v)=>
                {
                    float wu=u+.06f*(ZoneKit.Cloud(u,v,5,21)-.5f),wv=v+.04f*(ZoneKit.Cloud(u,v,5,22)-.5f);
                    float lat=Mathf.Abs(wv-.5f)*2,cap=ZoneKit.Smooth(.62f,.7f,lat+.05f*(ZoneKit.Cloud(u,v,9,23)-.5f));
                    // Leads in the sea ice as faint grey veins, not lines: drawn dark and hard, they read as a map's contours.
                    float leads=1-Mathf.Abs(2*ZoneKit.Tile(wu*14,wv*10,14,24)-1),dark=Mathf.Pow(leads,40)*.08f;
                    float band=Mathf.Sin((wv*9+ZoneKit.Cloud(u,v,3,25)*1.6f)*Mathf.PI*2)*.5f+.5f;
                    float storm=ZoneKit.Smooth(.55f,.85f,ZoneKit.Cloud(wu+.08f*Mathf.Sin(wv*40),wv,7,26));
                    float tone=.66f+.12f*band-dark+.18f*storm+.06f*(ZoneKit.Tile(u*256,v*256,256,27)-.5f);tone=Mathf.Lerp(tone,.96f,cap);
                    return new Color(tone*.9f,tone*.96f,tone*1.04f,1);
                });
                PlanetFace=ZoneKit.Toon("hoar-planet",face,m=>{m.SetColor("_BaseColor",new Color(.72f,.8f,.9f));m.SetTexture("_EmissionMap",face);m.SetColor("_EmissionColor",new Color(.2f,.24f,.3f));
                    m.SetFloat("_ReceiveShadows",0);m.SetFloat("_RampSteps",6);m.SetColor("_RimColor",new Color(.4f,.9f,.8f));ZoneKit.With(m,"_DETAIL_ON","_Detail");m.SetColor("_DetailTintA",new Color(.84f,.86f,.9f));
                    m.SetColor("_DetailTintB",new Color(1.12f,1.12f,1.1f));m.SetVector("_DetailParams",new Vector4(.04f,.9f,.2f,.35f));});
                // A wreck's hull: painted plate, hazard orange and dark stripes, scorched.
                var plate=ZoneKit.Texture("Hoarfell/hoar-hazard",256,(u,v)=>
                {
                    float stripe=Mathf.Repeat((u+v)*4,1)<.5f?1:0,scorch=ZoneKit.Cloud(u,v,5,31);
                    float tone=.78f-.32f*ZoneKit.Smooth(.5f,.8f,scorch);return stripe>0?new Color(tone*1.1f,tone*.56f,tone*.2f,1):new Color(tone*.26f,tone*.26f,tone*.28f,1);
                });
                Hull=ZoneKit.Toon("hoar-hazard",plate,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_RimColor",new Color(1,.8f,.6f));});
                Lamp=ZoneKit.Toon("hoar-lamp",null,m=>{m.SetColor("_BaseColor",new Color(1,.86f,.6f));m.SetColor("_EmissionColor",new Color(2.6f,1.9f,1.1f));});
                // The shelf: old ice in broad soft drifts of tone with a few long cracks, nothing small enough to repeat (the ring's ice, laid over forty metres, read as wallpaper).
                var shelf=ZoneKit.Texture("Hoarfell/hoar-shelf",512,(u,v)=>
                {
                    float drift=ZoneKit.Cloud(u,v,3,61),fine=ZoneKit.Cloud(u,v,9,62),crack=1-Mathf.Abs(2*ZoneKit.Tile(u*3+v*.7f,v*2,3,63)-1);
                    float tone=.78f+.14f*(drift-.5f)+.05f*(fine-.5f)-.16f*Mathf.Pow(crack,60);return new Color(tone*.96f,tone,tone*1.03f,1);
                });
                Shelf=ZoneKit.Toon("hoar-shelf",shelf,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_ICE_ON","_Ice");m.SetVector("_IceParams",new Vector4(2.4f,.45f,.4f,.3f));m.SetColor("_IceDeepColor",new Color(.16f,.34f,.52f));
                    m.SetColor("_IceRimColor",new Color(.6f,.95f,1f));m.SetColor("_ShadowTint",new Color(.42f,.52f,.7f));m.SetFloat("_LightFloor",.34f);});
                // Plain plating for the pod, its colour in its vertices: in the hazard stripes all over, a round pod read as a beach ball.
                Plate=ZoneKit.Toon("hoar-plate",null,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_RimColor",new Color(1,.85f,.7f));});
            }

            internal static void Build()
            {
                var kit=ZoneKit.Flight(Shear);
                var scene=WorldBuilder.Begin(kit.zone);
                Materials(kit);
                var keep=Keep();
                WorldBuilder.Spawn("Arrival",new Vector3(0,1,6));WorldBuilder.Spawn(Town,Beside(FromTownS,FromTownSide));
                kit.DriftingSky();

                // ---- the rule: the currents, each a ribbon of light whose chevrons and streaks run the way it flows
                var currents=new List<AuroraCurrent>();
                foreach(var (name,line,half,push,turns,tone) in Currents)currents.Add(Stream(kit,name,Line(line),half,push,turns,tone));

                // ---- what the lane flies through and over: the ring's shards at the plane (cover), its broken band below, curtains of aurora, Hoarfell
                Shards(kit,keep);
                Ring(kit,keep);
                Curtains(kit);
                Planet(kit);

                // ---- ways in and out
                WorldBuilder.Warp("Warp — The Hull of Many Hands",new Vector3(-14,1,-4),"ManyHandsHull","","Hoarfell").lockedPrompt="";
                var landAt=Beside(LandS,LandSide);
                var land=WorldBuilder.Warp("Land — Lastlight",landAt,Town,"hoar.idraHailed","Arrival");
                land.lockedPrompt="The way down through the ring is the striped cutter's: follow it";land.range=WorldBuilder.FlightDockRange;
                RingGap(kit,landAt);

                // ---- Idra: the hail across the lane, the tow, the cutter that leads
                DiscoveryPoint Across(string name,float s,float side,float wide,string flag,string node,string speaker,string required=null)
                {
                    var line=kit.Crossing(name,Beside(s,side,0),new Vector3(wide,8,5),flag,node,speaker,required);line.transform.rotation=Quaternion.Euler(0,Heading(s),0);return line;
                }
                var hail=Across("Idra hails the lane",HailS,0,Half*2+4,"hoar.idraHailed","IdraShearHail","Idra");hail.report="CurrentTurns";
                // Across the whole lane, both lanes: compulsory (manifest), and across the short lane only, a party that followed her cutter down her own lane never heard it.
                Across("Idra calls the short lane",LanesS,0,Half*2+4,"hoar.tenderSeen","IdraShearLanes","Idra","hoar.idraHailed");
                Cutter(kit);
                void Hail(string situation,float s,string heard,float range,params string[] needs)
                {
                    var call=new GameObject("Idra on the radio: "+situation,typeof(RadioHail)).GetComponent<RadioHail>();call.transform.position=Beside(s,0);
                    call.speaker="Idra";call.situations=situation.Split(',');call.needs=needs;call.heardFlag=heard;call.silencedBy="";call.range=range;
                }
                Hail("hail.tender",480,"hoar.hailTender",40,"hoar.idraHailed");Hail("hail.fastlane",760,"hoar.hailFastlane",40,"hoar.idraHailed");

                // ---- the downed cutter under the ice shelf, and the Muster's haul crate
                Wreck(kit);
                var cacheAt=Beside(CacheS,CacheSide);var crate=new GameObject("Muster haul crate");crate.transform.position=cacheAt;
                var box=new ZoneKit.Shape().Slot(0).Box(cacheAt+Vector3.down*.6f,new Vector3(2.4f,1.4f,1.6f),Quaternion.Euler(8,25,-6),new Color(.42f,.44f,.46f),new Color(.58f,.6f,.62f),.6f)
                    .Slot(1).Box(cacheAt+Vector3.down*.6f+Quaternion.Euler(8,25,-6)*new Vector3(0,.71f,0),new Vector3(2.5f,.08f,.3f),Quaternion.Euler(8,25,-6),new Color(1,.62f,.26f));
                var drawn=kit.Built("Muster haul crate",box,Hull,Lamp);drawn.transform.SetParent(crate.transform,true);
                var cache=LootCache.On(crate,"hoar.haul","Muster haul crate",140,ProgressionBuilder.Drop("RimeGlass",3,1),ProgressionBuilder.Drop("ScrapAlloy",4,1),ProgressionBuilder.Drop("ChargeCell",1,.6f));
                cache.range=WorldBuilder.FlightDockRange;

                // ---- the Muster on the lanes (HoarfellBuilder.Enemies.cs): four tenders and eight skiffs, as the chapter's XP is planned. Each fight stands in a current 18 to 22 m wide
                // with no ice in it, so there is room for each one's answer: out of a tender's 2.6 m circle toward it and round its 3.2 m burning wake; out of a skiff's 2.6 m lane as
                // its tell ends. Chapter 6's skiff is CurrentSkiff (a strafer at this chapter's weight), not chapter 2's MusterSkiff (a shooter at Sync 9). Each fight rides its current
                // with the party once begun (AuroraCurrent.Float): Fast_0 still going at 1062 carries the party into Fast_1's box, and the two wings are one fight.
                string tender=Foe("MusterTender","ClaimHauler"),skiff=Foe("CurrentSkiff","Podskimmer");
                kit.FlightFight("Shear_Tender",Beside(TenderS,TenderSide),new Vector3(30,6,34),(tender,1)).transform.rotation=Quaternion.Euler(0,Heading(TenderS),0);
                kit.FlightFight("Lane_0",Beside(600,44),new Vector3(30,6,30),(skiff,2)).transform.rotation=Quaternion.Euler(0,Heading(600),0);
                kit.FlightFight("Fast_0",Beside(860,4),new Vector3(34,6,34),(tender,1),(skiff,2)).transform.rotation=Quaternion.Euler(0,Heading(860),0);
                kit.FlightFight("Fast_1",Beside(1080,2),new Vector3(36,6,36),(tender,1),(skiff,2)).transform.rotation=Quaternion.Euler(0,Heading(1080),0);
                kit.FlightFight("Descent_0",Beside(DescentFightS,DescentFightSide),new Vector3(34,6,34),(tender,1),(skiff,2)).transform.rotation=Quaternion.Euler(0,Heading(DescentFightS),0);

                // Each stretch's walls run 15 m past its ends, not the half width: run 72 m past the right-hand bend at 560, the starboard wall stood across Idra's lane (a ship hit
                // it; the route writer's finding). The sharpest bend (21 degrees, at 560) is closed on its outside by 72 x tan(10.4) = 13.2 m.
                kit.FlightCorridor(Lane,Half,CorridorOverrun);
                WorldBuilder.Save(scene,kit.zone);
                string P(Vector3 p)=>"("+p.x.ToString("0.0")+","+p.y.ToString("0")+","+p.z.ToString("0.0")+")";
                Debug.Log("HOARFELL_COORDS AuroraShear arrival=(0,1,6) home-warp=(-14,1,-4) hail="+P(Beside(HailS,0))+" cutter="+P(Beside(CutterS,CutterSide))+" tow-end="+P(Beside(TowEndS,TowEndSide))
                    +" lanes="+P(Beside(LanesS,TenderSide))+" tender="+P(Beside(TenderS,TenderSide))+" cache="+P(cacheAt)+" lane0="+P(Beside(600,44))+" fast0="+P(Beside(860,4))+" pod="+P(Beside(PodS,PodSide))
                    +" fast1="+P(Beside(1080,2))+" descent0="+P(Beside(DescentFightS,DescentFightSide))+" from-town="+P(Beside(FromTownS,FromTownSide))+" land="+P(landAt)+" length="+Length.ToString("0")
                    +" currents="+string.Join(" ",Currents.Select(c=>c.name.Replace(' ','-')+":"+string.Join(">",Line(c.line).Select(P)))));
                Debug.Log("AURORA_SHEAR_OK");
            }

            // ---------------------------------------------------------------- the currents
            /// <summary>One current: its ribbon (a strip at the plane with uv along its length), its streaks of light (one system to each stretch), and the <see cref="AuroraCurrent"/>
            /// that carries ships and turns it round.</summary>
            static AuroraCurrent Stream(ZoneKit kit,string name,Vector3[] path,float half,float push,string turns,Color tone)
            {
                var root=new GameObject(name);const float Period=6;
                // The ribbon a hand under the plane, so the ships fly over it and nothing they cast lands in it.
                var mesh=new Mesh{name=name};var points=new List<Vector3>();var uv=new List<Vector2>();var colours=new List<Color>();var tris=new List<int>();
                float run=0;int rows=0;
                for(int i=0;i<path.Length;i++)
                {
                    // Corners are mitred, and the strip is cut into rows a metre or two long so its colour can fade in at its ends.
                    var before=i>0?(path[i]-path[i-1]).normalized:(path[1]-path[0]).normalized;var after=i<path.Length-1?(path[i+1]-path[i]).normalized:before;
                    var dir=(before+after).normalized;var side=Vector3.Cross(Vector3.up,dir).normalized;float mitre=1/Mathf.Max(.5f,Vector3.Dot(side,Vector3.Cross(Vector3.up,before).normalized));
                    int steps=i==0?1:Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(path[i-1],path[i])/2));
                    for(int k=i==0?0:1;k<=(i==0?0:steps);k++)
                    {
                        Vector3 at;Vector3 s;
                        if(i==0){at=path[0];s=side;}
                        else{float t=k/(float)steps;at=Vector3.Lerp(path[i-1],path[i],t);var segDir=(path[i]-path[i-1]).normalized;s=k==steps?side*mitre:Vector3.Cross(Vector3.up,segDir).normalized;if(k>0)run+=Vector3.Distance(path[i-1],path[i])/steps;}
                        at.y=.3f;
                        points.Add(at-s*half);points.Add(at+s*half);uv.Add(new Vector2(run/Period,0));uv.Add(new Vector2(run/Period,1));
                        colours.Add(tone);colours.Add(tone);rows++;
                    }
                }
                float total=run;
                for(int r=0;r<rows;r++){float along=uv[r*2].x*Period,fade=ZoneKit.Smooth(0,14,along)*ZoneKit.Smooth(total,total-14,along);colours[r*2]=colours[r*2+1]=new Color(tone.r,tone.g,tone.b,fade);}
                for(int r=1;r<rows;r++){int a=(r-1)*2,b=r*2;tris.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
                mesh.SetVertices(points);mesh.SetUVs(0,uv);mesh.SetColors(colours);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                var saved=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+kit.zone+"-"+name.Replace(' ','-').Replace("'","")+"-ribbon.asset");
                var ribbon=new GameObject(name+" ribbon",typeof(MeshFilter),typeof(MeshRenderer));ribbon.transform.SetParent(root.transform,false);
                ribbon.GetComponent<MeshFilter>().sharedMesh=saved;var view=ribbon.GetComponent<MeshRenderer>();view.sharedMaterial=Current;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
                // Streaks: stretched motes carried along each stretch at the current's speed, a little over the ribbon.
                var systems=new List<ParticleSystem>();var flows=new List<Vector3>();
                for(int i=1;i<path.Length;i++)
                {
                    var a=path[i-1];var b=path[i];var d=b-a;d.y=0;float leg=d.magnitude;var flow=d/leg;
                    var go=new GameObject(name+" streaks "+i,typeof(ParticleSystem));go.transform.SetParent(root.transform,false);
                    go.transform.SetPositionAndRotation(new Vector3((a.x+b.x)*.5f,.6f,(a.z+b.z)*.5f),Quaternion.LookRotation(flow));
                    var system=go.GetComponent<ParticleSystem>();system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                    float life=Mathf.Max(2,leg/9);int count=Mathf.Clamp((int)(leg*1.2f),40,320);
                    var main=system.main;main.loop=true;main.prewarm=true;main.duration=life;main.startLifetime=life;main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(.12f,.3f);
                    main.startColor=new Color(tone.r,tone.g,tone.b,.8f);main.maxParticles=count;main.simulationSpace=ParticleSystemSimulationSpace.World;main.playOnAwake=true;
                    var emission=system.emission;emission.rateOverTime=count/life;
                    var shape=system.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(half*1.6f,.4f,leg);
                    var velocity=system.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.World;
                    velocity.x=new ParticleSystem.MinMaxCurve(flow.x*9);velocity.y=new ParticleSystem.MinMaxCurve(0);velocity.z=new ParticleSystem.MinMaxCurve(flow.z*9);
                    var fade=system.colorOverLifetime;fade.enabled=true;var ramp=new Gradient();
                    ramp.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.2f),new GradientAlphaKey(1,.8f),new GradientAlphaKey(0,1)});fade.color=ramp;
                    var draw=go.GetComponent<ParticleSystemRenderer>();draw.sharedMaterial=Streak;draw.renderMode=ParticleSystemRenderMode.Stretch;draw.velocityScale=.32f;draw.lengthScale=2.2f;
                    draw.shadowCastingMode=ShadowCastingMode.Off;draw.receiveShadows=false;
                    systems.Add(system);flows.Add(flow);
                }
                var current=root.AddComponent<AuroraCurrent>();current.path=path.Select(p=>new Vector3(p.x,ZoneKit.FlightPlane,p.z)).ToArray();current.half=half;current.push=push;current.turnFlag=turns??"";
                current.ribbons=new Renderer[]{view};current.streaks=systems.ToArray();current.streakFlow=flows.ToArray();current.period=Period;current.streakSpeed=9;
                return current;
            }

            // ---------------------------------------------------------------- the ring
            /// <summary>Ice at the plane, between the currents: clusters of crystals that are cover (solid, each a box a hull stops at), several builds: a lone blade, a slab broken
            /// off the ring, a cluster of three to five. None stands in a current, on a way in or out, within a prompt's room, or by the way Idra's line takes the ships
            /// (<see cref="FromTow"/>; as built on November 12 none did, so the rule drops nothing and every later draw is where it was).</summary>
            static void Shards(ZoneKit kit,List<(Vector2 at,float clear)> keep)
            {
                var rng=new System.Random(61);var root=new GameObject("Ring shards");float length=Length;
                for(float from=0;from<length;from+=150)
                {
                    var shape=new ZoneKit.Shape();var blocks=new List<(Vector3 at,Vector3 size,float yaw)>();
                    for(int k=0;k<14;k++)
                    {
                        float s=from+(float)rng.NextDouble()*150,side=((float)rng.NextDouble()*2-1)*(Half-6),size=1.6f+(float)rng.NextDouble()*2.6f;int build=rng.Next(3),seed=rng.Next(9999);float yaw=(float)rng.NextDouble()*360;
                        var at=Beside(s,side);var p=Flat(at);
                        if(FromCurrents(p)<size+3||keep.Any(kept=>Vector2.Distance(kept.at,p)<kept.clear+size+3)||Vector2.Distance(p,Flat(Beside(HailS,0)))<6||FromTow(p)<size+TowRoom)continue;
                        var turn=Quaternion.Euler(0,yaw,0);var foot=at+Vector3.down*2.4f;
                        Color tip=new(.92f,.98f,1),root2=new(.45f,.66f,.86f);
                        if(build==0)IcePrism(shape,foot,turn*new Vector3(.25f,1,.1f),size*2.2f,size*.42f,6,seed,tip,root2);
                        else if(build==1){IcePrism(shape,foot,turn*new Vector3(1,.35f,0),size*2.4f,size*.7f,4,seed,tip,root2);IcePrism(shape,foot+turn*new Vector3(.4f,0,.6f),turn*new Vector3(-.3f,1,.2f),size*1.2f,size*.32f,5,seed+1,tip,root2);}
                        else for(int c=0;c<3+seed%3;c++){var lean=turn*Quaternion.Euler(0,c*97,0)*new Vector3(.45f,1,0);IcePrism(shape,foot+turn*new Vector3((c%2)*.6f-.3f,0,(c/2)*.6f-.3f),lean,size*(1.1f+.6f*((seed+c)%3)/2f),size*.3f,5+c%2,seed+c*7,tip,root2);}
                        blocks.Add((new Vector3(at.x,ZoneKit.FlightPlane,at.z),new Vector3(size*1.3f,6,size*1.3f),yaw));
                    }
                    if(blocks.Count==0)continue;
                    var part=kit.Built("Ring shards "+Mathf.RoundToInt(from),shape,Ice);part.transform.SetParent(root.transform,true);part.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                    foreach(var (at,size,yaw) in blocks)ZoneKit.Block(part,at,size,yaw,"Ice shard");
                    var bounds=part.GetComponent<MeshFilter>().sharedMesh.bounds;if(float.IsNaN(bounds.size.x)||float.IsInfinity(bounds.size.x))throw new InvalidOperationException("AuroraShear: shards from "+from+" have a point that isn't a number");
                }
            }
            /// <summary>The ring's broken band under the lane from 380 to 980 m along, 12 to 90 m down: ice pieces at every size, thickest toward the band's middle, kept clear of what
            /// must be read by where they show. Elsewhere a thinner scatter, so no stretch of the lane is bare sky. One mesh to each 170 m.</summary>
            static void Ring(ZoneKit kit,List<(Vector2 at,float clear)> keep)
            {
                var rng=new System.Random(67);var root=new GameObject("The ring below");float length=Length;
                for(float from=-20;from<length+20;from+=170)
                {
                    var shape=new ZoneKit.Shape();int made=0;
                    for(float s=from;s<from+170;s+=.7f)
                    {
                        bool band=s>380&&s<980;float bandMiddle=1-Mathf.Abs((s-680)/300f);
                        if(!band&&rng.Next(4)!=0){rng.Next();rng.Next();rng.Next();rng.Next();continue;}
                        float side=((float)rng.NextDouble()+(float)rng.NextDouble()-1)*(Half+30),deep=12+(float)rng.NextDouble()*(band?78:40),size=.4f+(float)rng.NextDouble()*(float)rng.NextDouble()*(band?5.5f:3f);
                        int seed=rng.Next(99999);if(band&&rng.NextDouble()>.35f+.65f*bandMiddle)continue;
                        var at=Beside(s,side,ZoneKit.FlightPlane-deep);
                        if(!ZoneKit.ClearOnScreen(at,size*1.2f,keep))continue;
                        var axis=Quaternion.Euler((seed%90)-45,seed%360,(seed%60)-30)*Vector3.up;made++;
                        // Deeper is bluer and dimmer, so the band has depth and never outshines the lane.
                        float dim=Mathf.Lerp(1,.55f,deep/90f);Color tip=ZoneKit.Shape.Dim(new Color(.9f,.97f,1),dim),foot=ZoneKit.Shape.Dim(new Color(.36f,.56f,.8f),dim);
                        if(seed%5==0)for(int c=0;c<3;c++)IcePrism(shape,at,Quaternion.Euler(c*40,c*120,0)*axis,size*1.6f,size*.36f,5,seed+c,tip,foot);
                        else IcePrism(shape,at-axis*size,axis,size*2.2f,size*.55f,4+seed%3,seed,tip,foot);
                    }
                    if(made==0)continue;
                    var part=kit.Built("Ring band "+Mathf.RoundToInt(from+20),shape,IceDeep);part.transform.SetParent(root.transform,true);
                    var view=part.GetComponent<MeshRenderer>();view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
                    var bounds=part.GetComponent<MeshFilter>().sharedMesh.bounds;if(float.IsNaN(bounds.size.x)||float.IsInfinity(bounds.size.x))throw new InvalidOperationException("AuroraShear: the ring from "+from+" has a point that isn't a number");
                }
            }
            /// <summary>Curtains of aurora far under the lane, lying the way the storm runs: green with a violet crest, each faded in at both ends so none cuts a straight edge across the
            /// sky (Wendmire's bands). Haze on purpose, never the subject.</summary>
            static void Curtains(ZoneKit kit)
            {
                var root=new GameObject("Aurora curtains");
                foreach(var (s,side,deep,wide,span,low,high) in new[]{(80f,-40f,-60f,70f,300f,new Color(.4f,1f,.6f,.5f),new Color(.7f,.5f,1f,.3f)),(330f,50f,-90f,90f,360f,new Color(.36f,1f,.8f,.46f),new Color(.6f,.46f,1f,.3f)),
                    (640f,-50f,-110f,100f,380f,new Color(.44f,1f,.62f,.44f),new Color(.86f,.5f,.96f,.28f)),(960f,46f,-80f,90f,360f,new Color(.4f,.96f,.9f,.46f),new Color(.66f,.5f,1f,.3f)),
                    (1260f,-36f,-100f,96f,360f,new Color(.46f,1f,.66f,.44f),new Color(.8f,.5f,1f,.28f))})
                {
                    var from=Beside(s-span*.5f,side,deep);var to=Beside(s+span*.5f,side,deep);
                    var band=ZoneKit.Strand(root.transform,from,to,Curtain,wide,low,high);var fade=new Gradient();
                    fade.SetKeys(new[]{new GradientColorKey(low,0),new GradientColorKey(Color.Lerp(low,high,.5f),.5f),new GradientColorKey(low,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(low.a,.3f),new GradientAlphaKey(low.a,.7f),new GradientAlphaKey(0,1)});
                    band.colorGradient=fade;band.alignment=LineAlignment.TransformZ;band.transform.rotation=Quaternion.Euler(90,0,0);band.textureMode=LineTextureMode.Tile;
                    band.positionCount=11;for(int k=0;k<11;k++){var p=Vector3.Lerp(from,to,k/10f)+Vector3.right*Mathf.Sin(k*.9f+s)*14;band.SetPosition(k,band.transform.InverseTransformPoint(p));}
                }
            }
            /// <summary>Hoarfell under the last stretch of the lane: a whole ball 760 m down, its poles at its limbs (where a sphere's uv pinches), lit by the scene's star. At 300 m across
            /// and 560 down it filled the frame from a hundred metres along and read as a pale wall behind every current.</summary>
            static void Planet(ZoneKit kit)
            {
                const float Radius=170,Depth=760;var at=ShowingAt(Beside(1500,90),Depth);
                var ball=kit.Built("Hoarfell",new ZoneKit.Shape().Lump(at,Vector3.one*Radius,7,.004f,Color.white,new Color(.5f,.56f,.7f),96,48,1/(Radius*3),Quaternion.Euler(0,0,90)),PlanetFace);
                var view=ball.GetComponent<MeshRenderer>();view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
            }
            /// <summary>The ring's gap at the way down: the ice falls away in a ring of great shards round it, lamps the Kept keep on them, and Hoarfell beyond.</summary>
            static void RingGap(ZoneKit kit,Vector3 landAt)
            {
                var shape=new ZoneKit.Shape();var lamps=new ZoneKit.Shape();var rng=new System.Random(71);
                for(int k=0;k<12;k++)
                {
                    float a=k*Mathf.PI*2/12+(float)rng.NextDouble()*.2f;var foot=ShowingAt(landAt,10)+new Vector3(Mathf.Cos(a)*22,0,Mathf.Sin(a)*18);
                    IcePrism(shape,foot,new Vector3(Mathf.Cos(a)*.5f,1,Mathf.Sin(a)*.5f),5+(float)rng.NextDouble()*4,1.6f+(float)rng.NextDouble(),5,k*13,new Color(.92f,.98f,1),new Color(.4f,.6f,.84f));
                    if(k%3==0){var top=foot+new Vector3(Mathf.Cos(a)*1.4f,6.2f,Mathf.Sin(a)*1.4f);lamps.Slot(0).Lump(top,new Vector3(.4f,.55f,.4f),k,.05f,Color.white,Color.white,8,5);kit.Lamp(top+Vector3.up*.6f,16,2.2f,new Color(1,.8f,.52f));}
                }
                kit.Built("The ring's gap",shape,Ice).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                kit.Built("The gap's lamps",lamps,Lamp).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                WorldBuilder.Label("THE WAY DOWN",landAt+new Vector3(0,-1,-7),5);
            }

            // ---------------------------------------------------------------- Idra's cutter
            /// <summary>The striped cutter (Idra's own flight body, drawn only): waiting beside the first current, then the tow and the lead (<see cref="CutterTow"/>).</summary>
            static void Cutter(ZoneKit kit)
            {
                var holder=new GameObject("Idra's cutter",typeof(CutterTow));var tow=holder.GetComponent<CutterTow>();
                var body=Body("IdraFlight",out float height);body.name="Striped cutter";body.transform.SetParent(holder.transform,false);
                var line=new GameObject("Tow line",typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.SetParent(holder.transform,false);
                line.widthMultiplier=.14f;line.numCapVertices=2;line.sharedMaterial=Streak;line.startColor=line.endColor=new Color(1,.7f,.36f,1);line.shadowCastingMode=ShadowCastingMode.Off;line.useWorldSpace=true;
                tow.flag="hoar.idraHailed";tow.doneFlag="hoar.towed";tow.cutter=body.transform;tow.line=line;tow.tow=Line(Tow);tow.lead=Line(Leads);
                body.transform.position=tow.tow[0];
                // Her running lights, so the cutter is the brightest thing near the party while it leads.
                var lights=new ZoneKit.Shape();lights.Lump(new Vector3(0,.9f,-1.6f),new Vector3(.22f,.22f,.22f),3,.05f,Color.white,Color.white,8,5);
                var glow=kit.Built("Cutter running light",lights,Lamp);glow.transform.SetParent(body.transform,false);glow.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                _=height;
            }
            /// <summary>An instance of a character prefab used as a drawn body: its colliders and anything that would act taken off.</summary>
            static GameObject Body(string id,out float height)
            {
                var prefab=ZoneKit.Prefab("Characters",id)??ZoneKit.Prefab("Ships","PatrolCutter");
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
                foreach(var b in go.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null)UnityEngine.Object.DestroyImmediate(b);
                height=ZoneKit.Drawn(go).size.y;return go;
            }
            /// <summary>Idra's third cutter, down in the ice shelf off the fast lane's starboard side: the shelf a broad slab under the plane with a broken lip, the cutter driven into
            /// that lip nose first, its pod whole and lit (Kott's hand-lamp), the point that tows it in (`Tow the crew pod`, offered while CutterDown waits for it).</summary>
            static void Wreck(ZoneKit kit)
            {
                var at=Beside(PodS,PodSide);var shelf=new ZoneKit.Shape();var rng=new System.Random(83);
                // The shelf: a slab of old ice, 40 m across, its top 5 m under the plane, its south lip broken into blocks and blades.
                var centre=at+new Vector3(4,-5.6f,14);
                for(int k=0;k<9;k++){float a=k*.7f;var block=centre+new Vector3(Mathf.Cos(a)*12,-(float)rng.NextDouble(),Mathf.Sin(a)*9);shelf.Slot(0).Box(block,new Vector3(14+(float)rng.NextDouble()*6,2.2f,10+(float)rng.NextDouble()*4),Quaternion.Euler(0,a*40,(float)rng.NextDouble()*4),new Color(.62f,.78f,.92f),new Color(.9f,.96f,1f),.06f);}
                for(int k=0;k<10;k++){float x=-14+k*3.2f;IcePrism(shelf.Slot(1),new Vector3(at.x+x,at.y-6,at.z+3+(float)rng.NextDouble()*2),new Vector3(((float)rng.NextDouble()-.5f)*.6f,1,-.4f),2.5f+(float)rng.NextDouble()*2.5f,.9f,5,k*17,new Color(.94f,.99f,1),new Color(.46f,.66f,.88f));}
                kit.Built("The ice shelf",shelf,Shelf,Ice).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                // The cutter in the lip, nose down and rolled: Idra's own body, so it is plainly one of hers.
                var wreck=Body("IdraFlight",out _);wreck.name="Downed cutter";wreck.transform.SetPositionAndRotation(at+new Vector3(0,-2.4f,1.6f),Quaternion.Euler(24,200,-18));
                // The pod: a round crew pod on the cutter's back, its lamp blinking one long flash then nothing (Kott, saving the cell).
                var podShape=new ZoneKit.Shape().Slot(0).Lump(at+new Vector3(-1.2f,-1.6f,-.8f),new Vector3(1.3f,.9f,1),4,.03f,new Color(.9f,.5f,.22f),new Color(.36f,.34f,.34f),14,7)
                    .Slot(1).Lump(at+new Vector3(-1.2f,-.5f,-1.4f),new Vector3(.26f,.26f,.26f),5,.04f,Color.white,Color.white,8,5);
                var pod=kit.Built("Crew pod",podShape,Plate,Lamp);pod.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                var light=kit.Lamp(at+new Vector3(-1.2f,.4f,-1.4f),10,1.6f,new Color(1,.86f,.6f));
                var blink=pod.AddComponent<LampRhythm>();blink.count=LampRhythm.Count.LongLongShort;blink.retuned=blink.count;blink.litFlag=blink.retunedFlag="";blink.darkFlag="hoar.podFound";
                blink.lights=new[]{light};blink.lamps=new Renderer[]{pod.GetComponent<MeshRenderer>()};blink.colour=new Color(1,.86f,.6f);blink.intensity=1.6f;blink.emission=2;blink.offset=0;
                var point=kit.Discovery("The downed cutter's pod",at,"Tow the crew pod","hoar.podFound","CrewPod","IdraCutterFound","Sela","CutterDown",0,7);point.situation="";
                WorldBuilder.Label("ICE SHELF",at+new Vector3(0,-1,-8),5);
            }
        }
    }
}
