using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;

namespace Lattice.EditorTools
{
    /// <summary>
    /// The Hull of Many Hands (space combat; rule: the salvage frame holds on only at a join, and the hull's own gear is the cover). The outside of the town the party has just walked
    /// through: a freight hauler's keel with the convoy's vessels lashed along it, each a different make and paint, and the four joins Hulljack has filed on. Map brief, the boss arena
    /// contract and evidence: docs/maps/ManyHandsHull.md. The second world engineer's file; its ZoneDef is in <c>ManyHandsBuilder.Define()</c>.
    /// </summary>
    public static partial class ManyHandsBuilder
    {
        static void BuildHull()=>Outside.Build();

        /// <summary>The hull's light (ZoneLook finds it by its attribute): a hard far star from the south-east on bare plating, so every roof and south face the camera sees is lit and
        /// every vessel's step casts a shadow on the next; a cool fill so the shaded side of a hull still reads against space; bloom low enough that a lit window is a window.</summary>
        [ZoneLook.Source] static void HullLooks()
        {
            // The night side: a far cold star low across the decks, so every structure throws a long shadow over the next, a dim blue fill, and the city's own light (windows,
            // pools under lamps, running lights, lit signs) doing the work. Bloom picks up what is lit; danger markings and the frame's tells are brighter than any of it.
            ZoneLook.Register(Hull,new ZoneLook.Spec{sun=new Color(.94f,.93f,.96f),sunIntensity=.95f,sunPitch=34,shadowStrength=.9f,
                sky=new Color(.15f,.15f,.18f),equator=new Color(.11f,.11f,.13f),ground=new Color(.05f,.05f,.06f),
                contrast=18,saturation=16,splitShadows=new Color(.46f,.48f,.53f),splitHighlights=new Color(.55f,.52f,.47f),
                bloomThreshold=1f,bloomIntensity=.65f,bloomScatter=.6f,vignette=.26f});
        }

        /// <summary>The outside of the ship: where everything stands (the map brief's numbers, which LookDev, the tests and the routes read), the vessels, the joins, the fight and the ways.</summary>
        internal static partial class Outside
        {
            // ---------------------------------------------------------------- where things are (x east, z north; the ship's bow is north, its stern and the tug south)
            internal const float Plane=ZoneKit.FlightPlane;
            /// <summary>The four joins Hulljack has filed on, in the order the fight visits them: the raft bay's, the cabins', the garden's, the freight hall's.</summary>
            internal static readonly Vector2[] Joins={new(-44,-29),new(-44,29),new(44,29),new(44,-30)};
            /// <summary>The top of each fight join's coupling drum, where the frame stands: just under the plane, so its feet (at the plane, where they are hit) show where they are
            /// (arena contract: y .2 to .6 over r 9). Everywhere else the joins are collars down on the roofs.</summary>
            internal static readonly float[] JoinTops={.3f,.3f,.3f,.3f};
            /// <summary>The drum's radius round a join's centre.</summary>
            internal const float DrumRadius=9.4f;
            /// <summary>The freight hall's roof door the frame tears through; the dock's pad; the launch's arrival beside it; the fight's checkpoint just outside its door; the claim drones
            /// over the raft bay; the fight door's middle; Hulljack's tug off the stern.</summary>
            internal static readonly Vector2 DoorAt=new(44,-67),DockAt=new(-40,-134),ArrivalAt=new(-30,-126),CheckpointAt=new(-28,-66),DronesAt=new(-58,-84),FightAt=new(0,-10),TugAt=new(62,-198);
            /// <summary>The harbour line: the limits, drawn as a ring of lamp buoys.</summary>
            internal const float XMin=-150,XMax=150,ZMin=-215,ZMax=205;
            /// <summary>The fight door's box over the four joins, centred on <see cref="FightAt"/>. The encounter itself stands at the first join, where the frame waits, because the objective's
            /// arrow points at an encounter's own place: at the box's middle it pointed at empty roof.</summary>
            internal static readonly Vector3 FightBox=new(150,8,104);
            /// <summary>The queue of settlements' ships holding off the port side.</summary>
            internal const float QueueX=-134;
            /// <summary>Every cover piece at the plane: which join it stands at, where, and what it is (mast, boom from a pedestal to a tip, a stack of containers).</summary>
            internal static readonly (int join,string kind,Vector2 at,Vector2 to)[] Cover=
            {
                (0,"mast",new(-64,-20),default),(0,"boom",new(-24,-40),new(-8,-40)),(0,"stack",new(-58,-47),default),
                (1,"mast",new(-26,41),default),(1,"boom",new(-64,40),new(-64,56)),(1,"stack",new(-60,12),default),
                (2,"mast",new(64,38),default),(2,"boom",new(28,43),new(14,55)),(2,"stack",new(62,10),default),
                (3,"mast",new(24,-41),default),(3,"boom",new(64,-42),new(64,-58)),(3,"stack",new(64,-13),default),
            };
            static Vector3 P(Vector2 at,float y=Plane)=>new(at.x,y,at.y);

            // ---------------------------------------------------------------- the vessels: each a block of the ship, its roof's height, its make
            /// <summary>One of the ship's vessels as a block of its plan: where it lies, its roof's height, how deep its walls go before the hull turns in, which of its sides are the ship's
            /// outside (those get the lower hull's slope down to the keel), and its make.</summary>
            sealed class Vessel
            {
                public string name;public Rect plan;public Rect? hole;public float roof,wall=10;public bool south=true,east=true,west=true;public bool outerS,outerE,outerW,outerN;public Make make;
            }
            /// <summary>How a vessel is built and painted: its plating, its paint and stripe, its trim, and what its windows are.</summary>
            sealed class Make
            {
                public Material plate;public Color paint,stripe,trim;public float stripeAt=1.6f,stripeWide=.9f;public int windows,firstRow=1;public float windowW=1.2f,windowH=1.1f,windowGap=2.4f;public float warm=.7f,lit=.75f;public float cell=6;
            }
            internal static Material Heavy,Liner,Ribbed,Trim,Glow,Glass,Crate,Water,Weave,Leaf,Tread,ClaimGlow,Pool,DomeGlass;
            static readonly Rect SpinePlan=Rect.MinMaxRect(-16,-160,16,136),LinerPlan=Rect.MinMaxRect(-72,-26,72,26),GalleyPlan=Rect.MinMaxRect(72,-32,100,28),
                GardenPlan=Rect.MinMaxRect(22,32,98,124),CabinPlan=Rect.MinMaxRect(-104,32,-22,126),RaftPlan=Rect.MinMaxRect(-104,-98,-22,-32),HallPlan=Rect.MinMaxRect(22,-100,66,-34),
                DockPlan=Rect.MinMaxRect(-70,-160,-16,-104),ApronPlan=Rect.MinMaxRect(22,-158,74,-104),BowPlan=Rect.MinMaxRect(-30,136,30,170),RaftHold=Rect.MinMaxRect(-98,-92,-28,-38);
            /// <summary>Tallow's station hull on the cabins' cradle deck: where it stands and how wide.</summary>
            static readonly Vector2 TallowAt=new(-80,104);const float TallowWide=34;
            /// <summary>The breached cabins, the north-west row past Tallow's hull: patched after the second part, with the Steady Hand's cutters latched beside the patch.</summary>
            internal static readonly Vector2 BreachAt=new(-93,119);
            const float SpineY=-5,LinerY=-6.5f,GalleyY=-5.4f,GardenY=-9.5f,GardenTop=-3.6f,GardenEaves=-6,CabinY=-8,RaftRim=-7,RaftWater=-11,HallY=-3.6f,DockY=-6,ApronY=-6,BowY=-3.2f;

            internal static void Build()
            {
                Materials();Signed.Clear();Pools=new ZoneKit.Shape();Stains=new ZoneKit.Shape();MoreGlyphs();
                var kit=ZoneKit.Flight(Hull);
                var scene=WorldBuilder.Begin(kit.zone);
                kit.DriftingSky();
                WorldBuilder.Spawn("Arrival",P(ArrivalAt));WorldBuilder.Spawn("Hulljack",P(CheckpointAt));

                // ---- the ship
                var root=new GameObject("The ship");
                Ship(kit,root.transform);
                // ---- what moves round it, what waits off it, and the sky under it
                Traffic(kit);Queue(kit);Tug(kit);Margins(kit);Backdrop(kit);
                // ---- the fight: the four joins, their cover, the claim drones, the door, the way the frame goes and the way after it
                Fight(kit);
                // ---- ways in and out
                var dock=WorldBuilder.Dock("Dock — Many Hands",P(DockAt),City,"Arrival",WorldBuilder.FlightDockRange);
                WorldBuilder.Label("Dock — Many Hands",P(DockAt+new Vector2(0,-6),.3f),2.2f);
                DockPylon(kit,dock.gameObject);
                // The pad's box from below the hull to above it, so a hull flown into its side meets a wall and not the lip of a thin slab it rides up on (QaPlanEarlyTests' wall check).
                {var pad=dock.GetComponent<BoxCollider>();var world=pad.bounds;pad.size=new Vector3(pad.size.x,3.4f,pad.size.z);pad.center=new Vector3(pad.center.x,dock.transform.InverseTransformPoint(new Vector3(world.center.x,Plane+.6f,world.center.z)).y,pad.center.z);}
                // ---- the city's light on its decks and the wear on them, gathered from everything above
                CityLight(kit,root.transform);
                // ---- the harbour line
                HarbourLine(kit);
                kit.FlightLimits(XMin,XMax,ZMin,ZMax);

                WorldBuilder.Save(scene,kit.zone);
                Debug.Log("MANYHANDS_COORDS ManyHandsHull arrival="+P(ArrivalAt)+" dock="+P(DockAt)+" checkpoint="+P(CheckpointAt)+" joins="+string.Join(" ",Joins.Select((j,i)=>P(j,JoinTops[i]).ToString("0.0")))
                    +" door="+P(DoorAt)+" drones="+P(DronesAt)+" fight-box="+P(FightAt)+" box="+FightBox+" tug="+P(TugAt)+" limits=("+XMin+","+XMax+","+ZMin+","+ZMax+")"
                    +" cover="+string.Join(" ",Cover.Select(c=>c.join+1+c.kind+"("+c.at.x+","+c.at.y+")")));
                Debug.Log("MANYHANDS_HULL_OK");
            }

            // ================================================================ materials
            /// <summary>Paints the hull's textures and makes its materials. Everything is painted near grey and coloured in the vertices, so one plating serves every vessel that is that
            /// make, in its own paint.</summary>
            static void Materials()
            {
                // Heavy riveted plates, two by two to a tile laid at six metres (three-metre plates): a seam groove with a lit lip, a row of rivets inside each edge, mottling, a grain,
                // and grime streaking down each plate toward its lower seam. A freight hauler's and a freight hall's.
                var heavy=ZoneKit.Texture("ManyHands/hands-plate-heavy",512,(u,v)=>
                {
                    float pu=Mathf.Repeat(u*2,1),pv=Mathf.Repeat(v*2,1);int plate=(int)(u*2)%2+2*((int)(v*2)%2);
                    float edge=Mathf.Min(Mathf.Min(pu,1-pu),Mathf.Min(pv,1-pv));
                    float tone=.74f+(plate==1?.05f:plate==2?-.045f:plate==3?.025f:0)+.1f*(ZoneKit.Cloud(u,v,6,3)-.5f)+.06f*(ZoneKit.Cloud(u,v,48,9)-.5f);
                    tone-=.08f*ZoneKit.Smooth(.6f,.92f,ZoneKit.Tile(u*48,0,48,5))*ZoneKit.Smooth(.35f,.7f,ZoneKit.Cloud(u,v,8,17))*(.3f+.7f*(1-pv));
                    float rivet=9;
                    foreach(float a in new[]{.045f,.955f})
                    {
                        float alongV=(Mathf.Repeat(pv*9,1)-.5f)/9,alongU=(Mathf.Repeat(pu*9,1)-.5f)/9;
                        rivet=Mathf.Min(rivet,Mathf.Sqrt((pu-a)*(pu-a)+alongV*alongV));rivet=Mathf.Min(rivet,Mathf.Sqrt((pv-a)*(pv-a)+alongU*alongU));
                    }
                    if(rivet<.012f)tone=.9f-.32f*(rivet/.012f);
                    if(edge<.007f)tone*=.36f;else if(edge<.014f)tone=tone*1.05f+.03f;
                    return new Color(tone,tone,tone,1);
                });
                // A liner's long panels, four rows to a four-metre tile, two-metre panels set off row by row, fine seams with flush screws along them, and a soft wear.
                var liner=ZoneKit.Texture("ManyHands/hands-plate-liner",512,(u,v)=>
                {
                    int row=(int)(v*4)%4;float pv=Mathf.Repeat(v*4,1),pu=Mathf.Repeat(u*2+row*.37f,1);
                    float tone=.84f+.035f*((row*7)%3-1)+.05f*(ZoneKit.Cloud(u,v,8,13)-.5f)+.03f*(ZoneKit.Cloud(u,v,64,4)-.5f);
                    float ev=Mathf.Min(pv,1-pv),eu=Mathf.Min(pu,1-pu)*2;
                    if(ev<.022f||eu<.022f)tone*=.6f;
                    float screw=Mathf.Abs(Mathf.Repeat(pu*16,1)-.5f);if((Mathf.Abs(pv-.07f)<.018f||Mathf.Abs(pv-.93f)<.018f)&&screw<.09f)tone*=.78f;
                    return new Color(tone,tone,tone,1);
                });
                // Corrugated plating, twelve ribs to a three-metre tile, lit from one side, broken by a plate seam every metre and a half: the galley's and the garden carriers'.
                var ribbed=ZoneKit.Texture("ManyHands/hands-plate-ribbed",512,(u,v)=>
                {
                    float rib=Mathf.Sin(u*Mathf.PI*2*12);float pv=Mathf.Repeat(v*2,1);
                    float tone=.72f+.13f*rib+.05f*(ZoneKit.Cloud(u,v,6,21)-.5f)+.03f*(ZoneKit.Cloud(u,v,48,2)-.5f);
                    if(Mathf.Min(pv,1-pv)<.012f)tone*=.45f;
                    if(Mathf.Abs(pv-.04f)<.01f&&Mathf.Abs(Mathf.Repeat(u*12,1)-.5f)<.12f)tone=.92f;
                    return new Color(tone,tone,tone,1);
                });
                // A container's side: ribs pressed upright, a rail top and bottom, scuffs.
                var crate=ZoneKit.Texture("ManyHands/hands-container",256,(u,v)=>
                {
                    float rib=Mathf.Sin(u*Mathf.PI*2*10),tone=.74f+.14f*rib+.06f*(ZoneKit.Cloud(u,v,4,7)-.5f);
                    float rail=Mathf.Min(v,1-v);if(rail<.06f)tone=.58f+.2f*(rail/.06f);
                    if(ZoneKit.Cloud(u,v,16,11)>.68f)tone*=.86f;
                    return new Color(tone,tone,tone,1);
                });
                // Tread plate for the walks: raised lozenges in pairs, worn bright on their tops.
                var tread=ZoneKit.Texture("ManyHands/hands-tread",256,(u,v)=>
                {
                    float a=Mathf.Repeat(u*8+v*8,1),b=Mathf.Repeat(u*8-v*8,1);bool lozenge=(Mathf.Repeat(u*8,1)<.5f==Mathf.Repeat(v*8,1)<.5f)?Mathf.Abs(a-.5f)<.08f:Mathf.Abs(b-.5f)<.08f;
                    float tone=.62f+.06f*(ZoneKit.Cloud(u,v,4,5)-.5f);if(lozenge)tone+=.16f;
                    return new Color(tone,tone,tone,1);
                });
                // The raft bay's hold water from above: still and dark, long slow swells, the hold's lamps lying on it in soft patches.
                var water=ZoneKit.Texture("ManyHands/hands-hold-water",512,(u,v)=>
                {
                    float swell=Mathf.Sin((v*5+ZoneKit.Cloud(u,v,3,5)*2.2f)*Mathf.PI*2)*.5f+.5f,sheen=ZoneKit.Smooth(.5f,.82f,ZoneKit.Cloud(u,v,4,17));
                    float tone=.34f+.07f*swell+.18f*sheen+.04f*(ZoneKit.Cloud(u,v,32,2)-.5f);return new Color(tone,tone,tone,1);
                });
                // Driftreed's weave: over-under strands, for the mats drying across the hold.
                var weave=ZoneKit.Texture("ManyHands/hands-weave",256,(u,v)=>
                {
                    float cu=Mathf.Repeat(u*12,1),cv=Mathf.Repeat(v*12,1);bool over=((int)(u*12)+(int)(v*12))%2==0;
                    float strand=over?Mathf.Sin(cv*Mathf.PI):Mathf.Sin(cu*Mathf.PI);float tone=.5f+.36f*strand+.06f*(ZoneKit.Cloud(u,v,8,3)-.5f);
                    return new Color(tone,tone,tone,1);
                });
                Material Plating(string name,Texture2D map,Color shadow)=>ZoneKit.Toon(name,map,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",shadow);m.SetFloat("_RampSteps",4);
                    m.SetColor("_RimColor",new Color(.22f,.24f,.28f));m.SetFloat("_LightFloor",.09f);});
                Heavy=Plating("hands-hull-heavy",heavy,new Color(.5f,.5f,.56f));
                Liner=Plating("hands-hull-liner",liner,new Color(.54f,.54f,.6f));
                Ribbed=Plating("hands-hull-ribbed",ribbed,new Color(.5f,.5f,.56f));
                Crate=Plating("hands-container",crate,new Color(.5f,.5f,.56f));
                Tread=Plating("hands-tread",tread,new Color(.5f,.5f,.56f));
                Trim=ZoneKit.Toon("hands-hull-trim",null,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.48f,.48f,.54f));m.SetFloat("_RampSteps",4);m.SetFloat("_LightFloor",.09f);});
                // Windows, lamps and seam lights: unlit, coloured in their vertices, bright enough to bloom a little.
                Glow=ZoneKit.Toon("hands-hull-glow",null,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetVector("_GlowParams",new Vector4(1.15f,0,0,0));});
                // Light lying on a deck under a lamp: a soft round fall-off painted about the texture's corner, so a disc with its uv centred on nought is brightest in the middle and gone
                // at its rim; added to what is under it, coloured in the vertices, drawn after the decks and before any danger marking (Transparent-10), so a marking always lies over it.
                var pool=ZoneKit.Texture("ManyHands/hands-light-pool",128,(u,v)=>{float du=Mathf.Min(u,1-u),dv=Mathf.Min(v,1-v);float d=Mathf.Sqrt(du*du+dv*dv)/.5f;float t=Mathf.Clamp01(1-d);t=t*t*(3-2*t);return new Color(t,t,t,1);});
                Pool=ZoneKit.Toon("hands-light-pool",pool,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetVector("_GlowParams",new Vector4(1,0,0,0));m.SetFloat("_SrcBlend",(float)BlendMode.One);m.SetFloat("_DstBlend",(float)BlendMode.One);
                    m.SetFloat("_ZWrite",0);m.SetOverrideTag("RenderType","Transparent");m.renderQueue=(int)RenderQueue.Transparent-30;m.SetFloat("_ReceiveShadows",0);});
                // Hulljack's claim tags: the same lit orange as chapter 3's, so the player who burned them off Teo's barges knows them.
                ClaimGlow=ZoneKit.Toon("hands-claim-tag",null,m=>{m.SetColor("_BaseColor",new Color(1,.5f,.14f));m.SetColor("_EmissionColor",new Color(2.4f,1f,.2f));});
                // Glass: the garden's vault and the arcade's promenade roof, a pale tint over what's under it, drawn after everything solid.
                Glass=ZoneKit.Toon("hands-hull-glass",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.78f,.9f,1f,.24f));m.SetColor("_RimColor",new Color(.6f,.8f,1f));m.SetFloat("_RimPower",2.5f);
                    m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.SetOverrideTag("RenderType","Transparent");
                    m.renderQueue=(int)RenderQueue.Transparent-40;m.SetFloat("_ReceiveShadows",0);});
                // The chart room's dome: warm glass lit from the lamp shop under it, so the bow's landmark glows.
                DomeGlass=ZoneKit.Toon("hands-dome-glass",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(1f,.9f,.7f,.42f));m.SetColor("_EmissionColor",new Color(.5f,.36f,.18f));m.SetColor("_RimColor",new Color(1f,.82f,.5f));m.SetFloat("_RimPower",2);
                    m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.SetOverrideTag("RenderType","Transparent");m.renderQueue=(int)RenderQueue.Transparent-40;m.SetFloat("_ReceiveShadows",0);});
                Water=ZoneKit.Toon("hands-hold-water",water,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.34f,.5f,.5f));m.SetColor("_EmissionColor",new Color(.03f,.06f,.06f));m.SetFloat("_RampSteps",4);});
                Weave=ZoneKit.Toon("hands-weave",weave,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetFloat("_LightFloor",.25f);});
                Leaf=ZoneKit.Toon("hands-leaf",null,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetFloat("_LightFloor",.3f);m.SetColor("_EmissionColor",new Color(.02f,.06f,.02f));});
                LightMaterials();
            }

            // ================================================================ the ship
            static Make LinerMake,SpineMake,GalleyMake,GardenMake,CabinMake,RaftMake,HallMake,DockMake,BowMake;
            static void Makes()
            {
                // Each vessel the convoy picked up keeps the paint it came with: the hauler's oxide, the liner's cream and teal, the galley's slate, the garden carriers' bronze,
                // the raft bay's hold green, the freight hall's graphite, the dock's grey, the bow's old brass.
                SpineMake=new Make{plate=Heavy,paint=new Color(.62f,.36f,.3f),stripe=new Color(.88f,.86f,.8f),trim=new Color(.24f,.22f,.22f),windows=2,windowW=2.2f,windowH=.9f,windowGap=5,warm=.8f,lit=.55f};
                LinerMake=new Make{plate=Liner,paint=new Color(.9f,.86f,.76f),stripe=new Color(.2f,.56f,.58f),trim=new Color(.26f,.3f,.32f),stripeAt=1.2f,stripeWide=1.1f,windows=1,windowW=.9f,windowGap=1.9f,warm=.85f,lit=.8f,cell=8};
                GalleyMake=new Make{plate=Ribbed,paint=new Color(.46f,.56f,.68f),stripe=new Color(.86f,.6f,.32f),trim=new Color(.22f,.24f,.28f),windows=3,windowW=2.6f,windowH=1.4f,windowGap=4.2f,warm=1,lit=.9f};
                GardenMake=new Make{plate=Ribbed,paint=new Color(.62f,.5f,.32f),stripe=new Color(.38f,.56f,.32f),trim=new Color(.3f,.24f,.16f),windows=2,windowW=3,windowH=.8f,windowGap=4,warm=.3f,lit=.8f};
                CabinMake=new Make{plate=Heavy,paint=new Color(.6f,.58f,.68f),stripe=new Color(.42f,.74f,.78f),trim=new Color(.22f,.22f,.26f),windows=3,windowW=1.4f,windowH=1.2f,windowGap=2.6f,warm=.75f,lit=.85f};
                RaftMake=new Make{plate=Heavy,paint=new Color(.36f,.48f,.44f),stripe=new Color(.82f,.72f,.5f),trim=new Color(.2f,.24f,.22f),windows=2,windowW=1.8f,windowH=.8f,windowGap=6,warm=.9f,lit=.6f};
                HallMake=new Make{plate=Heavy,paint=new Color(.38f,.4f,.44f),stripe=new Color(.9f,.9f,.86f),trim=new Color(.18f,.19f,.21f),windows=3,windowW=4,windowH=2,windowGap=7,warm=.6f,lit=.7f,cell=7};
                DockMake=new Make{plate=Heavy,paint=new Color(.5f,.52f,.56f),stripe=new Color(.3f,.62f,.7f),trim=new Color(.2f,.21f,.24f),windows=2,windowW=2,windowH=.9f,windowGap=5,warm=.5f,lit=.7f};
                BowMake=new Make{plate=Liner,paint=new Color(.62f,.5f,.34f),stripe=new Color(.28f,.22f,.18f),trim=new Color(.2f,.16f,.12f),stripeAt=.35f,stripeWide=.3f,windows=3,windowW=2.8f,windowH=1.9f,windowGap=3.6f,warm=1,lit=1,firstRow=0};
            }
            static void Ship(ZoneKit kit,Transform root)
            {
                Makes();
                var vessels=new[]
                {
                    new Vessel{name="Freight spine",plan=SpinePlan,roof=SpineY,make=SpineMake,outerS=true},
                    new Vessel{name="Arcade liner",plan=LinerPlan,roof=LinerY,make=LinerMake,outerE=false},
                    new Vessel{name="Galley",plan=GalleyPlan,roof=GalleyY,make=GalleyMake,outerE=true},
                    new Vessel{name="Garden deck",plan=GardenPlan,roof=GardenY,wall=6,make=GardenMake,outerE=true,outerN=true},
                    new Vessel{name="Cabins deck",plan=CabinPlan,roof=CabinY,make=CabinMake,outerW=true,outerN=true},
                    new Vessel{name="Raft bay",plan=RaftPlan,hole=RaftHold,roof=RaftRim,make=RaftMake,outerW=true},
                    new Vessel{name="Freight hall",plan=HallPlan,roof=HallY,wall=12,make=HallMake},
                    new Vessel{name="Stern dock",plan=DockPlan,roof=DockY,make=DockMake,outerS=true,outerW=true},
                    new Vessel{name="Stern apron",plan=ApronPlan,roof=ApronY,make=DockMake,outerS=true,outerE=true},
                    new Vessel{name="Lamp gallery",plan=BowPlan,roof=BowY,wall=14,make=BowMake,south=true,outerN=true,outerE=true,outerW=true},
                };
                int seed=11;
                foreach(var v in vessels)
                {
                    var shape=new ZoneKit.Shape();var rng=new System.Random(seed+=17);
                    Body(shape,v,rng);
                    var body=kit.Built("Hull "+v.name,shape.Slot(2),v.make.plate,Trim,Glow);body.transform.SetParent(root,true);
                }
                // What stands on each, and the joins between them.
                Spine(kit,root);Arcade(kit,root);Galley(kit,root);Garden(kit,root);Cabins(kit,root);RaftBay(kit,root);FreightHall(kit,root);SternDock(kit,root);Apron(kit,root);Bow(kit,root);
                Roofscapes(kit,root);Collars(kit,root);
            }

            /// <summary>A vessel's body: its roof in cells of slightly different tone (a few a different paint where a plate was replaced), each seen wall in bands down to where the hull turns
            /// in, its livery stripe, its windows (lit, dim or dark), the trim along its roof's edge, and on the ship's outside the lower hull's slope in toward the keel.</summary>
            static void Body(ZoneKit.Shape s,Vessel v,System.Random rng)
            {
                var m=v.make;var r=v.plan;float texel=1/(m.plate==Liner?4f:m.plate==Ribbed?3f:6f);
                // A roof with a hole in it (the raft bay's open hold) is the four strips round the hole.
                var roofs=v.hole is Rect h?new[]{Rect.MinMaxRect(r.xMin,r.yMin,r.xMax,h.yMin),Rect.MinMaxRect(r.xMin,h.yMax,r.xMax,r.yMax),Rect.MinMaxRect(r.xMin,h.yMin,h.xMin,h.yMax),Rect.MinMaxRect(h.xMax,h.yMin,r.xMax,h.yMax)}:new[]{r};
                foreach(var part in roofs)
                {
                    int nx=Mathf.Max(1,Mathf.RoundToInt(part.width/m.cell)),nz=Mathf.Max(1,Mathf.RoundToInt(part.height/m.cell));float cx=part.width/nx,cz=part.height/nz;
                    for(int i=0;i<nx;i++)for(int j=0;j<nz;j++)
                    {
                        float shade=.86f+.24f*(float)rng.NextDouble();int roll=rng.Next(9);bool patch=roll==0,repaint=roll==1;
                        var tone=patch?Color.Lerp(m.paint,new Color(.5f,.52f,.55f),.65f)*shade:repaint?Color.Lerp(m.paint,Paints[rng.Next(Paints.Length)],.25f)*shade:ZoneKit.Shape.Dim(m.paint,shade);tone.a=1;
                        var a=new Vector3(part.xMin+i*cx,v.roof,part.yMin+j*cz);
                        s.Slot(0).Quad(a,a+new Vector3(0,0,cz),a+new Vector3(cx,0,cz),a+new Vector3(cx,0,0),Vector3.up,tone,texel);
                        // A plate let in over an old one: a raised weld bead round it, so the patch reads as a plate and not a tone.
                        if(patch){var inset=a+new Vector3(cx*.5f,.03f,cz*.5f);float px=cx*(.5f+.3f*(float)rng.NextDouble()),pz=cz*(.5f+.3f*(float)rng.NextDouble());var bead=ZoneKit.Shape.Dim(m.trim,1.6f);
                            foreach(var (f,t) in new[]{(new Vector3(-px,0,-pz),new Vector3(px,0,-pz)),(new Vector3(px,0,-pz),new Vector3(px,0,pz)),(new Vector3(px,0,pz),new Vector3(-px,0,pz)),(new Vector3(-px,0,pz),new Vector3(-px,0,-pz))})
                                s.Slot(1).Beam(inset+f*.5f,inset+t*.5f,.12f,.05f,bead);}
                        // Wear: grime, oil and rust blots on one plate in five, darker on the working decks than on the liner's.
                        if(rng.Next(5)==0){float depth=m.plate==Liner?.22f:.38f;StainAt(a+new Vector3(cx*(float)rng.NextDouble(),0,cz*(float)rng.NextDouble()),.8f+2f*(float)rng.NextDouble(),new Color(depth,depth*1.05f,depth*1.12f),rng,2+rng.Next(2));}
                    }
                }
                if(v.south)Wall(s,new Vector2(r.xMax,r.yMin),new Vector2(r.xMin,r.yMin),v.roof,v.wall,Vector3.back,m,rng,texel,v.outerS);
                if(v.east)Wall(s,new Vector2(r.xMax,r.yMax),new Vector2(r.xMax,r.yMin),v.roof,v.wall,Vector3.right,m,rng,texel,v.outerE);
                if(v.west)Wall(s,new Vector2(r.xMin,r.yMin),new Vector2(r.xMin,r.yMax),v.roof,v.wall,Vector3.left,m,rng,texel,v.outerW);
                // The roof's edge: a bumper of trim on every side, so where a vessel steps down onto the next its edge is a line.
                foreach(var (a,b) in new[]{(new Vector2(r.xMin,r.yMin),new Vector2(r.xMax,r.yMin)),(new Vector2(r.xMax,r.yMin),new Vector2(r.xMax,r.yMax)),(new Vector2(r.xMax,r.yMax),new Vector2(r.xMin,r.yMax)),(new Vector2(r.xMin,r.yMax),new Vector2(r.xMin,r.yMin))})
                    s.Slot(1).Beam(new Vector3(a.x,v.roof+.12f,a.y),new Vector3(b.x,v.roof+.12f,b.y),.5f,.34f,m.trim,ZoneKit.Shape.Dim(m.trim,1.4f));
                // Running lights along every edge that is the ship's outside: on the roof's bumper, and along the foot of the wall where the lower hull turns in.
                int seed=(int)r.xMin;
                if(v.outerS){Running(s,new Vector2(r.xMin,r.yMin),new Vector2(r.xMax,r.yMin),v.roof+.3f,Vector3.back,seed);Running(s,new Vector2(r.xMin,r.yMin),new Vector2(r.xMax,r.yMin),v.roof-v.wall+.2f,Vector3.back*1.2f,seed+1,8);}
                if(v.outerE){Running(s,new Vector2(r.xMax,r.yMin),new Vector2(r.xMax,r.yMax),v.roof+.3f,Vector3.right,seed+2);Running(s,new Vector2(r.xMax,r.yMin),new Vector2(r.xMax,r.yMax),v.roof-v.wall+.2f,Vector3.right*1.2f,seed+3,8);}
                if(v.outerW){Running(s,new Vector2(r.xMin,r.yMin),new Vector2(r.xMin,r.yMax),v.roof+.3f,Vector3.left,seed+4);Running(s,new Vector2(r.xMin,r.yMin),new Vector2(r.xMin,r.yMax),v.roof-v.wall+.2f,Vector3.left*1.2f,seed+5,8);}
                if(v.outerN)Running(s,new Vector2(r.xMin,r.yMax),new Vector2(r.xMax,r.yMax),v.roof+.3f,Vector3.forward,seed+6);
            }
            /// <summary>One seen wall from <paramref name="from"/> to <paramref name="to"/> (left to right seen from outside), in deck bands three metres high with the livery stripe in the
            /// first, windows in every band but the stripe's, and the lower hull below it on the ship's outside.</summary>
            static void Wall(ZoneKit.Shape s,Vector2 from,Vector2 to,float top,float depth,Vector3 outward,Make m,System.Random rng,float texel,bool outer)
            {
                var along=(to-from);float length=along.magnitude;var dir=new Vector3(along.x,0,along.y)/length;var a0=new Vector3(from.x,0,from.y);
                int bands=Mathf.Max(1,Mathf.RoundToInt(depth/3));float band=depth/bands;int spans=Mathf.Max(1,Mathf.RoundToInt(length/9));float span=length/spans;
                for(int b=0;b<bands;b++)for(int k=0;k<spans;k++)
                {
                    float y0=top-b*band,y1=top-(b+1)*band;var p=a0+dir*k*span;var q=a0+dir*(k+1)*span;var tone=ZoneKit.Shape.Dim(m.paint,(.82f+.12f*(float)rng.NextDouble())*(1-.05f*b));tone.a=1;
                    s.Slot(0).Quad(new Vector3(p.x,y0,p.z),new Vector3(q.x,y0,q.z),new Vector3(q.x,y1,q.z),new Vector3(p.x,y1,p.z),outward,tone,texel);
                }
                // The stripe: the make's livery band, just under the roof's edge.
                {
                    float y0=top-m.stripeAt+m.stripeWide*.5f,y1=y0-m.stripeWide;var p=a0+outward*.04f;var q=p+dir*length;
                    s.Slot(0).Quad(new Vector3(p.x,y0,p.z),new Vector3(q.x,y0,q.z),new Vector3(q.x,y1,q.z),new Vector3(p.x,y1,p.z),outward,m.stripe,texel);
                }
                // Windows: a row to each band under the stripe; lit warm or cool, dim, or dark glass. Portholes on the liner are round.
                if(m.windows>0)for(int b=m.firstRow;b<bands;b++)
                {
                    float mid=top-b*band-band*(b==0?.55f:.45f);
                    for(float x=m.windowGap*.5f+(float)rng.NextDouble()*.4f;x<length-m.windowW;x+=m.windowGap)
                    {
                        var c=a0+dir*x+outward*.05f;c.y=mid;double roll=rng.NextDouble();
                        Color light=roll<m.lit?(rng.NextDouble()<m.warm?new Color(1,.8f,.5f):new Color(.74f,.9f,1f)):roll<m.lit+.1f?new Color(.4f,.34f,.26f):default;
                        if(light.a==0){if(m.windows==1)s.Slot(1).Disc(c,outward,m.windowW*.5f,10,new Color(.12f,.14f,.18f));else Pane(s,1,c,dir,m.windowW,m.windowH,outward,new Color(.12f,.14f,.18f));continue;}
                        light*=.85f+.3f*(float)rng.NextDouble();light.a=1;
                        if(m.windows==1){s.Slot(1).Disc(c-outward*.02f,outward,m.windowW*.62f,12,m.trim);s.Slot(2).Disc(c,outward,m.windowW*.5f,12,light);}
                        else{Pane(s,1,c-outward*.02f,dir,m.windowW+.3f,m.windowH+.3f,outward,m.trim);Pane(s,2,c,dir,m.windowW,m.windowH,outward,light);if(m.windows==3)Pane(s,1,c+outward*.01f,dir,.12f,m.windowH,outward,m.trim);}
                    }
                }
                if(!outer)return;
                // The lower hull: from the wall's foot it turns in under the ship, darker, to the keel.
                {
                    float y0=top-depth,y1=y0-9;var p=a0-outward*0;var q=a0+dir*length;var pi=p-outward*7;var qi=q-outward*7;var tone=ZoneKit.Shape.Dim(m.paint,.55f);tone.a=1;
                    s.Slot(0).Quad(new Vector3(p.x,y0,p.z),new Vector3(q.x,y0,q.z),new Vector3(qi.x,y1,qi.z),new Vector3(pi.x,y1,pi.z),outward+Vector3.down*.8f,tone,texel);
                    s.Slot(1).Beam(new Vector3(p.x,y0,p.z)+outward*.2f,new Vector3(q.x,y0,q.z)+outward*.2f,.6f,.5f,m.trim);
                }
            }
            /// <summary>A flat rectangle on a wall: a window, its frame, a door.</summary>
            static void Pane(ZoneKit.Shape s,int slot,Vector3 centre,Vector3 along,float wide,float tall,Vector3 outward,Color tone)
            {
                var x=along*wide*.5f;var y=Vector3.up*tall*.5f;
                s.Slot(slot).Quad(centre-x-y,centre+x-y,centre+x+y,centre-x+y,outward,tone,.5f);
            }
            /// <summary>The strokes of the capitals the hull's painted words use, in a box one wide and 1.4 tall (y up, which is north on a deck, so the words read from the fixed
            /// camera): each letter a few polylines, laid as flat strokes so they read as paint, not as a pixel font.</summary>
            static readonly Dictionary<char,Vector2[][]> Glyphs=new()
            {
                ['M']=new[]{new Vector2[]{new(0,0),new(0,1.4f),new(.5f,.7f),new(1,1.4f),new(1,0)}},
                ['A']=new[]{new Vector2[]{new(0,0),new(.5f,1.4f),new(1,0)},new Vector2[]{new(.2f,.55f),new(.8f,.55f)}},
                ['N']=new[]{new Vector2[]{new(0,0),new(0,1.4f),new(1,0),new(1,1.4f)}},
                ['Y']=new[]{new Vector2[]{new(0,1.4f),new(.5f,.72f),new(1,1.4f)},new Vector2[]{new(.5f,.72f),new(.5f,0)}},
                ['H']=new[]{new Vector2[]{new(0,0),new(0,1.4f)},new Vector2[]{new(1,0),new(1,1.4f)},new Vector2[]{new(0,.7f),new(1,.7f)}},
                ['D']=new[]{new Vector2[]{new(0,0),new(0,1.4f),new(.55f,1.4f),new(.82f,1.3f),new(.96f,1.08f),new(1,.7f),new(.96f,.32f),new(.82f,.1f),new(.55f,0),new(0,0)}},
                ['S']=new[]{new Vector2[]{new(1,1.18f),new(.8f,1.38f),new(.2f,1.38f),new(0,1.16f),new(0,.92f),new(.2f,.76f),new(.8f,.64f),new(1,.48f),new(1,.22f),new(.8f,.02f),new(.2f,.02f),new(0,.22f)}},
                ['U']=new[]{new Vector2[]{new(0,1.4f),new(0,.3f),new(.15f,.06f),new(.5f,0),new(.85f,.06f),new(1,.3f),new(1,1.4f)}},
                ['L']=new[]{new Vector2[]{new(0,1.4f),new(0,0),new(1,0)}},
                ['J']=new[]{new Vector2[]{new(1,1.4f),new(1,.3f),new(.85f,.06f),new(.5f,0),new(.15f,.06f),new(0,.3f)}},
                ['C']=new[]{new Vector2[]{new(1,1.16f),new(.8f,1.38f),new(.2f,1.38f),new(0,1.1f),new(0,.3f),new(.2f,.02f),new(.8f,.02f),new(1,.24f)}},
                ['K']=new[]{new Vector2[]{new(0,0),new(0,1.4f)},new Vector2[]{new(1,1.4f),new(0,.58f)},new Vector2[]{new(.34f,.86f),new(1,0)}},
            };
            /// <summary>Paints a word on a deck: letters <paramref name="tall"/> metres high along +x from <paramref name="at"/> (their foot's left corner), each stroke a flat quad a little
            /// worn in tone. A letter it has no strokes for is a space.</summary>
            static void Letters(ZoneKit.Shape s,int slot,string word,Vector3 at,float tall,Color tone)
            {
                float unit=tall/1.4f,stroke=tall*.15f,advance=unit*1.36f;var rng=new System.Random(word.Length*7+(int)at.x);int n=0;
                foreach(char ch in word)
                {
                    if(Glyphs.TryGetValue(ch,out var lines))foreach(var line in lines)for(int i=1;i<line.Length;i++)
                    {
                        Vector3 a=at+new Vector3(line[i-1].x*unit,.002f*n,line[i-1].y*unit),b=at+new Vector3(line[i].x*unit,.002f*n,line[i].y*unit);var dir=(b-a).normalized;var side=Vector3.Cross(Vector3.up,dir)*stroke*.5f;
                        // Each stroke runs on half its width past its ends, so a polyline's corners close.
                        a-=dir*stroke*.4f;b+=dir*stroke*.4f;var worn=ZoneKit.Shape.Dim(tone,.9f+.12f*(float)rng.NextDouble());worn.a=1;
                        s.Slot(slot).Quad(a-side,a+side,b+side,b-side,Vector3.up,worn,.5f);n++;
                    }
                    at+=Vector3.right*advance;
                }
            }
            /// <summary>A flat rectangle on a roof: a hatch, a skylight, a painted mark.</summary>
            static void Flat(ZoneKit.Shape s,int slot,Vector3 centre,float wide,float deep,float yaw,Color tone,float texel=.5f)
            {
                var turn=Quaternion.Euler(0,yaw,0);var x=turn*Vector3.right*wide*.5f;var z=turn*Vector3.forward*deep*.5f;
                s.Slot(slot).Quad(centre-x-z,centre-x+z,centre+x+z,centre+x-z,Vector3.up,tone,texel);
            }
            /// <summary>A container: a box of corrugated side with a door end, in its own paint.</summary>
            static void Container(ZoneKit.Shape s,int slot,Vector3 foot,float length,float yaw,Color paint)
            {
                var turn=Quaternion.Euler(0,yaw,0);var size=new Vector3(2.44f,2.6f,length);
                s.Slot(slot).Box(foot+Vector3.up*1.3f,size,turn,paint,ZoneKit.Shape.Dim(paint,1.08f),.42f);
            }
            static readonly Color[] Paints={new(.72f,.3f,.24f),new(.2f,.42f,.6f),new(.3f,.52f,.36f),new(.82f,.78f,.7f),new(.54f,.42f,.3f),new(.86f,.56f,.24f),new(.3f,.34f,.38f),new(.6f,.62f,.66f),new(.24f,.54f,.56f),new(.5f,.28f,.2f),new(.78f,.68f,.42f),new(.4f,.44f,.62f)};

            // ================================================================ what stands on each vessel
            /// <summary>The freight spine: the hauler's keel, its ridge walk with lamps the whole length, container stacks either side in four lengths and a dozen paints, two gantry cranes
            /// over them, mooring bollards, and MANY HANDS along the stern.</summary>
            static void Spine(ZoneKit kit,Transform root)
            {
                var s=new ZoneKit.Shape();var rng=new System.Random(301);float y=SpineY;
                // The ridge walk: tread plate down the keel, lamps every eight metres, rails.
                for(float z=-156;z<134;z+=6){s.Slot(3).Box(new Vector3(0,y+.18f,z+3),new Vector3(4,.36f,6),Quaternion.identity,new Color(.62f,.62f,.6f),null,.5f);}
                for(float z=-152;z<134;z+=8){if(BridgeAt.Contains(new Vector2(0,z)))continue;foreach(float x in new[]{-2.3f,2.3f}){s.Slot(1).Post(new Vector3(x,y+.36f,z),.9f,.06f,.05f,5,new Color(.3f,.3f,.3f),new Color(.4f,.4f,.4f));s.Slot(2).Lump(new Vector3(x,y+1.34f,z),Vector3.one*.16f,(int)z,.05f,new Color(1,.9f,.7f),new Color(1,.82f,.56f),6,4);}
                    PoolAt(new Vector3(0,y+.36f,z),3.1f,PoolWarm*.85f);}
                foreach(float x in new[]{-2.2f,2.2f})s.Slot(1).Beam(new Vector3(x,y+1.1f,-156),new Vector3(x,y+1.1f,134),.08f,.08f,new Color(.35f,.35f,.35f));
                // The hauler's cargo hatches either side of the walk: raised lids with ribs across them, a coaming round each, lugs at the corners, the hold's number band; the stacks stand on them.
                int hold=0;
                for(float z=-152;z<128;z+=10.6f)foreach(float side in new[]{-1f,1f})
                {
                    var hc=new Vector3(side*8,y,z+4.3f);if(new[]{new Vector2(7,0),new Vector2(-9.5f,0),new Vector2(-8,-12),new Vector2(-8,12),new Vector2(8,-12),new Vector2(8,12)}.Any(f=>Vector2.Distance(f,new Vector2(hc.x,hc.z))<8.5f))continue;
                    if(hc.z>BridgeAt.yMin-4.8f&&hc.z<BridgeAt.yMax+4.8f)continue;hold++;var lid=ZoneKit.Shape.Dim(SpineMake.paint,.82f+.1f*(float)rng.NextDouble());lid.a=1;
                    s.Slot(1).Box(hc+Vector3.up*.22f,new Vector3(10.2f,.44f,9.2f),Quaternion.identity,SpineMake.trim);s.Slot(0).Box(hc+Vector3.up*.5f,new Vector3(9.6f,.2f,8.6f),Quaternion.identity,lid,ZoneKit.Shape.Dim(lid,1.08f),1/6f);
                    for(float x=-4.2f;x<=4.3f;x+=1.4f)s.Slot(1).Box(hc+new Vector3(x,.64f,0),new Vector3(.16f,.1f,8.4f),Quaternion.identity,ZoneKit.Shape.Dim(SpineMake.trim,1.6f));
                    foreach(var (dx,dz) in new[]{(-4.9f,-4.4f),(4.9f,-4.4f),(-4.9f,4.4f),(4.9f,4.4f)})s.Slot(1).Box(hc+new Vector3(dx,.6f,dz),new Vector3(.5f,.4f,.5f),Quaternion.identity,new Color(.3f,.28f,.26f));
                    Flat(s,1,hc+new Vector3(0,.62f,-3.6f),9.4f,.7f,0,new Color(.88f,.86f,.8f));
                    // The hold's number on its band, in the hauler's oxide.
                    Letters(s,1,hold.ToString("00"),hc+new Vector3(-.47f,.63f,-3.87f),.55f,ZoneKit.Shape.Dim(SpineMake.paint,.7f));
                }
                // Containers either side of the walk, off the crossing and the joins, on the hatch lids.
                bool Kept(Vector2 p)=>Mathf.Abs(p.y)<34||Joins.Any(j=>Vector2.Distance(j,p)<16)||Vector2.Distance(p,DoorAt)<16||p.y>BridgeAt.yMin-9.5f&&p.y<BridgeAt.yMax+7;
                for(float z=-150;z<128;z+=13.2f)foreach(float side in new[]{-1f,1f})
                {
                    int high=1+rng.Next(2);float length=rng.Next(3)==0?6.1f:12.2f;bool gone=rng.Next(5)==0;var at=new Vector2(side*(5.5f+rng.Next(2)*3.2f),z+(float)rng.NextDouble()*1.2f);
                    if(gone||Kept(at))continue;
                    if(GantryZ.Any(g=>Mathf.Abs(at.y-g)<length*.5f+3.5f))continue;
                    for(int h=0;h<high;h++){var paint=Paints[rng.Next(Paints.Length)];Container(s,4,new Vector3(at.x,y+.6f+h*2.6f,at.y+(h%2)*.3f),length,(float)rng.NextDouble()*2-1,paint);}
                    if(rng.Next(4)==0){var top=new Vector3(at.x,y+.6f+high*2.6f+.02f,at.y);Flat(s,1,top,2.6f,length*.8f,0,new Color(.32f,.4f,.3f));}
                }
                // Bollards and hatches.
                for(float z=-150;z<130;z+=22){foreach(float x in new[]{-14.5f,14.5f}){if(Mathf.Abs(z)<30)continue;s.Slot(1).Post(new Vector3(x,y,z),.7f,.35f,.3f,8,new Color(.3f,.28f,.26f),new Color(.46f,.42f,.38f));}
                    Flat(s,1,new Vector3(-9.5f,y+.02f,z+11),2.6f,2.6f,0,new Color(.3f,.24f,.22f));}
                // Two gantry cranes straddling the stacks, legs on the keel's edges, a trolley and hook on each.
                // Jib cranes along the keel's edges, their arms under the plane: a gantry tall enough to straddle the stacks would stand up through it.
                foreach(var (x,z,yaw) in new[]{(13.5f,-116f,250f),(-13.5f,-84f,70f),(13.5f,-52f,290f),(-13.5f,54f,110f),(13.5f,110f,250f),(-13.5f,122f,80f)})Jib(s,new Vector3(x,y,z),yaw,9,rng);
                // The hauler's bridge at the stern and two gantries across the stacks: the spine's tall pieces, each throwing its shadow down the deck.
                BridgeTower(s,rng);foreach(float g in GantryZ)SpineGantry(s,g,rng);
                // At the crossing, the arcade's clock seen from outside: a short tower on the spine's back with the liner's old clock face on its south side and a lantern on top, and the
                // crossing's skylights round it, lit from the arcade under them.
                {
                    ClockTower(s,y);
                    foreach(var (dx,dz) in new[]{(-8f,-12f),(-8f,12f),(8f,-12f),(8f,12f),(-9.5f,0f)}){Flat(s,1,new Vector3(dx,y+.03f,dz),4.4f,3.2f,0,new Color(.3f,.32f,.34f));Flat(s,2,new Vector3(dx,y+.06f,dz),3.8f,2.6f,0,new Color(.82f,.66f,.44f));PoolAt(new Vector3(dx,y,dz),3.6f,PoolWarm*.7f);}
                }
                var spine=kit.Built("Spine top",s.Slot(4),Heavy,Trim,Glow,Tread,Crate);spine.transform.SetParent(root,true);
                Sign("FREIGHT SPINE",new Vector2(0,-60),y);Sign("MANY HANDS",new Vector2(0,-146),y);
            }
            /// <summary>A gantry crane across a run of the hull: two legs, a bridge, a trolley with its cab and a hook hanging under it.</summary>
            static void Gantry(ZoneKit.Shape s,Vector3 foot,float span,float tall,System.Random rng)
            {
                Color steel=new(.82f,.8f,.74f),dark=new(.3f,.3f,.32f);
                foreach(float side in new[]{-1f,1f})
                {
                    var f=foot+Vector3.right*side*span*.5f;
                    s.Slot(1).Beam(f+new Vector3(0,0,-2.4f),f+new Vector3(0,tall,-.6f),.7f,.7f,steel).Beam(f+new Vector3(0,0,2.4f),f+new Vector3(0,tall,.6f),.7f,.7f,steel).Box(f+Vector3.up*.3f,new Vector3(1.6f,.6f,5.6f),Quaternion.identity,dark);
                }
                s.Slot(1).Beam(foot+new Vector3(-span*.5f-1,tall,0),foot+new Vector3(span*.5f+1,tall,0),1.4f,1.1f,steel);
                float t=(float)rng.NextDouble()*span*.6f-span*.3f;var trolley=foot+new Vector3(t,tall-.9f,0);
                s.Slot(1).Box(trolley,new Vector3(2.2f,1.2f,2.4f),Quaternion.identity,dark,steel).Tube(new[]{trolley,trolley+Vector3.down*4.4f},new[]{.05f,.05f},4,dark,dark,1,1,false).Box(trolley+Vector3.down*4.8f,new Vector3(.9f,.6f,.9f),Quaternion.identity,new Color(.9f,.82f,.3f));
                s.Slot(2).Lump(foot+new Vector3(span*.5f+1,tall+.5f,0),Vector3.one*.22f,3,.05f,new Color(1,.4f,.3f),new Color(1,.3f,.2f),6,4).Lump(foot+new Vector3(-span*.5f-1,tall+.5f,0),Vector3.one*.22f,4,.05f,new Color(1,.4f,.3f),new Color(1,.3f,.2f),6,4);
            }
            /// <summary>A jib crane on a roof's edge: a pedestal, a cab, an arm rising out over the stacks to just under the plane, a hook on a line. Drawn only, and all of it under the
            /// ships, so it is scenery and never something flown through.</summary>
            static void Jib(ZoneKit.Shape s,Vector3 foot,float yaw,float reach,System.Random rng)
            {
                Color steel=new(.82f,.78f,.66f),dark=new(.28f,.28f,.3f);var dir=Quaternion.Euler(0,yaw,0)*Vector3.forward;float top=Plane-1.4f;
                s.Slot(1).Post(foot,1.6f,.7f,.55f,10,dark,steel).Box(foot+Vector3.up*2.1f,new Vector3(1.8f,1.2f,2.2f),Quaternion.LookRotation(dir),steel,ZoneKit.Shape.Dim(steel,1.1f));
                var root=foot+Vector3.up*2.4f;var tip=new Vector3(foot.x,top,foot.z)+dir*reach;
                s.Slot(1).Beam(root,tip,.45f,.55f,steel).Beam(root+Vector3.up*.9f,Vector3.Lerp(root,tip,.55f)+Vector3.up*.2f,.12f,.12f,dark);
                float drop=2.6f+(float)rng.NextDouble()*2;s.Slot(1).Tube(new[]{tip,tip+Vector3.down*drop},new[]{.04f,.04f},4,dark,dark,1,1,false).Box(tip+Vector3.down*(drop+.35f),new Vector3(.7f,.5f,.7f),Quaternion.identity,new Color(.9f,.9f,.86f));
                s.Slot(2).Lump(tip+Vector3.up*.3f,Vector3.one*.14f,(int)foot.z,.05f,new Color(1,.3f,.25f),new Color(1,.3f,.25f),6,3);
            }
            /// <summary>The arcade's old liner: its two funnels with a teal band, the promenade's glass roof along its middle with the arcade's lamps under it, lifeboats in their davits along
            /// its edges, deck lamps and ventilators.</summary>
            static void Arcade(ZoneKit kit,Transform root)
            {
                var s=new ZoneKit.Shape();var glass=new ZoneKit.Shape();var rng=new System.Random(331);float y=LinerY;var m=LinerMake;
                foreach(float side in new[]{-1f,1f})
                {
                    float x0=side<0?-70:18,x1=side<0?-18:70;
                    // The promenade: a raised glazed roof down the middle on ribs, the arcade's warm light under it.
                    for(float x=x0;x<=x1;x+=4)
                    {
                        var ribA=new Vector3(x,y,-7.5f);var ribB=new Vector3(x,y,7.5f);var path=new Vector3[7];for(int k=0;k<7;k++){float t=k/6f,a=t*Mathf.PI;path[k]=Vector3.Lerp(ribA,ribB,t)+Vector3.up*Mathf.Sin(a)*2.2f;}
                        s.Slot(1).Tube(path,new[]{.12f},5,new Color(.22f,.4f,.42f),new Color(.3f,.52f,.54f),1,1,false);
                    }
                    for(float x=x0;x<x1;x+=4)for(int k=0;k<6;k++)
                    {
                        float t0=k/6f,t1=(k+1)/6f;Vector3 At(float xx,float t)=>new Vector3(xx,y+Mathf.Sin(t*Mathf.PI)*2.2f,Mathf.Lerp(-7.5f,7.5f,t));
                        glass.Slot(0).Quad(At(x,t0),At(x+4,t0),At(x+4,t1),At(x,t1),Vector3.up,new Color(.85f,.95f,1f,.3f));
                    }
                    // Under the glass: the arcade's lamps in two rows and its stalls' awnings, warm.
                    for(float x=x0+2;x<x1;x+=3.2f){foreach(float z in new[]{-3.4f,3.4f})s.Slot(2).Lump(new Vector3(x+(float)rng.NextDouble()*.6f,y+.6f,z),new Vector3(.3f,.2f,.3f),(int)x,.05f,new Color(1,.86f,.6f),new Color(1,.78f,.5f),6,4);
                        PoolAt(new Vector3(x,y+.06f,0),3.6f,PoolWarm*.75f);
                        if(rng.Next(3)==0)Flat(s,0,new Vector3(x+1,y+.25f,(rng.Next(2)==0?-1:1)*5.6f),2.2f,1.4f,0,Paints[rng.Next(Paints.Length)]);}
                    Flat(s,2,new Vector3((x0+x1)*.5f,y+.05f,0),x1-x0,4,0,new Color(.8f,.64f,.44f));
                    // Lifeboats in their davits along both edges, deck lamps between.
                    foreach(float z in new[]{-23.5f,23.5f})for(float x=x0+4;x<x1-3;x+=11)
                    {
                        if(Joins.Any(j=>Vector2.Distance(j,new Vector2(x,z))<11))continue;
                        var boat=new Vector3(x,y+1.4f,z);s.Slot(0).Lump(boat,new Vector3(3.2f,.8f,1.1f),(int)x,.04f,new Color(.95f,.92f,.86f),new Color(.6f,.4f,.3f),12,6,.25f);
                        foreach(float dx in new[]{-2.4f,2.4f})s.Slot(1).Tube(new[]{new Vector3(x+dx,y,z+Mathf.Sign(z)*.6f),new Vector3(x+dx,y+2.4f,z+Mathf.Sign(z)*.4f),new Vector3(x+dx,y+2.3f,z-Mathf.Sign(z)*.4f)},new[]{.1f},5,m.trim,m.trim,1,1,false);
                        s.Slot(2).Lump(new Vector3(x+5.5f,y+.9f,z),Vector3.one*.18f,(int)x+1,.05f,new Color(1,.88f,.66f),new Color(1,.8f,.52f),6,4);PoolAt(new Vector3(x+5.5f,y,z-Mathf.Sign(z)*.6f),2.4f,PoolWarm*.8f);
                    }
                    // Mushroom ventilators.
                    for(int k=0;k<5;k++){var at=new Vector3(Mathf.Lerp(x0+6,x1-6,(float)rng.NextDouble()),y,(rng.Next(2)==0?-1:1)*(11+(float)rng.NextDouble()*6));
                        if(Joins.Any(j=>Vector2.Distance(j,new Vector2(at.x,at.z))<12)||UpperDecks.Any(u=>Grown(u,1.5f).Contains(new Vector2(at.x,at.z))))continue;
                        s.Slot(1).Tube(new[]{at,at+Vector3.up*1.6f,at+new Vector3(0,2.1f,.5f)},new[]{.32f,.3f,.42f},10,m.trim,new Color(.2f,.5f,.52f),1,.5f);}
                }
                // Two funnels, aft of the crossing on the liner's east half and on its west, banded teal, a little soot at the lip.
                foreach(var at in new[]{new Vector3(-52,y,-12),new Vector3(58,y,12)})
                {
                    var path=new[]{at,at+Vector3.up*2,at+Vector3.up*4.6f,at+Vector3.up*5.2f};
                    s.Slot(0).Tube(path,new[]{2.4f,2.3f,2.1f,2.15f},16,m.paint,new Color(.88f,.84f,.76f),1,.25f).Tube(new[]{at+Vector3.up*3.3f,at+Vector3.up*4.4f},new[]{2.18f,2.12f},16,m.stripe,m.stripe,1,.25f,false)
                        .Tube(new[]{at+Vector3.up*4.9f,at+Vector3.up*5.25f},new[]{2.16f,2.16f},16,new Color(.16f,.15f,.15f),new Color(.12f,.12f,.12f),1,.25f,false);
                }
                var top=kit.Built("Arcade top",s.Slot(2),Liner,Trim,Glow);top.transform.SetParent(root,true);
                var roofGlass=kit.Built("Arcade promenade glass",glass,ArcadeGlass);roofGlass.transform.SetParent(root,true);roofGlass.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                // The liner's upper decks stepping up either side of the promenade; the district's name on the north-west one's roof.
                LinerDecks(kit,root);
                Sign("ARCADE",new Vector2(-27.75f,15.6f),y+TierOne+TierTwo);
            }
            /// <summary>The galley at the liner's east end: vent stacks with warm light round their skirts and steam off the biggest, ducts along the roof, extractor fans and skylights.</summary>
            static void Galley(ZoneKit kit,Transform root)
            {
                var s=new ZoneKit.Shape();var rng=new System.Random(347);float y=GalleyY;var r=GalleyPlan;
                Vector3[] stacks={new(80,y,-18),new(88,y,-6),new(82,y,8),new(93,y,16),new(90,y,-22)};
                for(int k=0;k<stacks.Length;k++)
                {
                    float tall=2.4f+k%3*.9f,girth=k==1?1.3f:.8f;var at=stacks[k];
                    s.Slot(0).Tube(new[]{at,at+Vector3.up*tall,at+Vector3.up*(tall+.5f)},new[]{girth,girth,girth*1.25f},12,GalleyMake.paint,ZoneKit.Shape.Dim(GalleyMake.paint,1.15f),1,.4f).Disc(at+Vector3.up*(tall+.52f),Vector3.up,girth*1.1f,12,new Color(.16f,.15f,.14f));
                    s.Slot(2).Disc(at+Vector3.up*.05f,Vector3.up,girth+.6f,14,new Color(.8f,.64f,.42f),girth+.1f);PoolAt(at,girth+2.6f,PoolWarm*1.1f);
                }
                // Ducts between them, fans, skylights over the kitchen.
                s.Slot(1).Tube(new[]{stacks[0]+Vector3.up*.6f,new Vector3(84,y+.6f,-12),stacks[1]+Vector3.up*.6f},new[]{.4f},8,GalleyMake.trim,GalleyMake.trim,1,1,false).Tube(new[]{stacks[1]+Vector3.up*.6f,new Vector3(86,y+.6f,2),stacks[2]+Vector3.up*.6f},new[]{.35f},8,GalleyMake.trim,GalleyMake.trim,1,1,false);
                for(int k=0;k<4;k++){var at=new Vector3(76+k*6,y+.05f,22);s.Slot(1).Disc(at,Vector3.up,1.4f,16,new Color(.2f,.2f,.22f));for(int b=0;b<5;b++){float a=b*Mathf.PI*2/5;s.Slot(1).Box(at+new Vector3(Mathf.Cos(a)*.6f,.08f,Mathf.Sin(a)*.6f),new Vector3(1.1f,.04f,.3f),Quaternion.Euler(0,-a*Mathf.Rad2Deg,20),new Color(.5f,.5f,.52f));}}
                for(int k=0;k<6;k++){var sk=new Vector3(76+(k%3)*7,y+.04f,-6+(k/3)*6);Flat(s,2,sk,3.2f,1.8f,0,new Color(.84f,.68f,.46f));PoolAt(sk,2.8f,PoolWarm*.9f);}
                // The galley's lit sign over its roof, facing the lane, the kitchen's light under it.
                SignBoard(s,"GALLEY",new Vector3(87,y,12.5f),1.1f,new Color(1f,.92f,.7f),new Color(.14f,.15f,.18f),1.2f);
                var top=kit.Built("Galley top",s.Slot(2),Ribbed,Trim,Glow);top.transform.SetParent(root,true);
                // Steam off the galley's vents (the vent Taren mends inside): a slow pale drift up off the stacks.
                var steam=kit.Drift("Galley steam",new Vector3(86,y+4,-2),new Vector3(18,4,30),kit.Glow("Steam",false,new Color(1.1f,1.05f,1f,1)),new Color(.9f,.9f,.92f,.16f),160,.6f,1.6f,.25f,9);
                steam.transform.SetParent(root,true);
                Sign("GALLEY",new Vector2(86,-28),y);
            }
            /// <summary>The garden: Teo's three seed carriers lying side by side in a cradle deck, roofed by a glass vault on bronze ribs (the carrier ribs the brief names), green under the
            /// glass: root beds in rows, leaf, water channels, grow-lamps.</summary>
            static void Garden(ZoneKit kit,Transform root)
            {
                var s=new ZoneKit.Shape();var glass=new ZoneKit.Shape();var green=new ZoneKit.Shape();var rng=new System.Random(359);var r=GardenPlan;float y=GardenY;
                // The carriers: Teo's three seed barges from the delta, one to a lane, lying in the deck under the vault (the generated barge where it is in, else a ribbed pod built here).
                float[] lanes={36,60,84};
                for(int k=0;k<3;k++)
                {
                    var at=new Vector3(lanes[k],y+.2f,70+k*9);
                    if(ZoneKit.Landed("SeedBarge"))
                    {
                        var barge=kit.Prop("SeedBarge",at,4.2f,k*4-4,false,false);barge.transform.localScale*=17.5f/Mathf.Max(.1f,ZoneKit.Drawn(barge).size.z);
                        var b=ZoneKit.Drawn(barge);barge.transform.position+=at-new Vector3(b.center.x,b.min.y,b.center.z);
                        barge.name="Seed carrier "+(k+1);barge.transform.SetParent(root,true);
                    }
                    else s.Slot(0).Lump(at+Vector3.up*2,new Vector3(2.5f,2,8.5f),k,.03f,new Color(.86f,.8f,.6f),new Color(.5f,.42f,.3f),20,8,.1f);
                }
                // Root beds, leaf and water on the carriers' backs and between them, so the glass has green under it from every angle.
                for(float z=r.yMin+4;z<r.yMax-3;z+=2.6f)for(float x=r.xMin+3;x<r.xMax-3;x+=2.2f)
                {
                    float lane=lanes.Min(l=>Mathf.Abs(l-x));bool onCarrier=Enumerable.Range(0,3).Any(k=>Mathf.Abs(x-lanes[k])<2.6f&&Mathf.Abs(z-(70+k*9))<9.2f);if(rng.Next(5)==0||onCarrier)continue;
                    var at=new Vector3(x+(float)rng.NextDouble(),y+.35f,z+(float)rng.NextDouble());
                    if(lane>10.6f){green.Slot(1).Quad(at+new Vector3(-1.1f,.05f,-1.3f),at+new Vector3(-1.1f,.05f,1.3f),at+new Vector3(1.1f,.05f,1.3f),at+new Vector3(1.1f,.05f,-1.3f),Vector3.up,new Color(.3f,.55f,.7f));continue;}
                    float size=.5f+(float)rng.NextDouble()*.9f;var leaf=Color.Lerp(new Color(.32f,.6f,.3f),new Color(.6f,.78f,.36f),(float)rng.NextDouble());
                    if(rng.Next(9)==0){green.Slot(0).Lump(at+Vector3.up*.15f,new Vector3(1,.3f,1.2f),rng.Next(999),.1f,new Color(.46f,.34f,.24f),new Color(.3f,.22f,.16f),8,4);continue;}
                    if(rng.Next(3)==0)green.Slot(0).Fern(at,size*1.4f,6,rng,new Color(.3f,.42f,.22f),leaf);
                    else green.Slot(0).Lump(at+Vector3.up*size*.5f,new Vector3(size,size*.7f,size),rng.Next(999),.25f,leaf,ZoneKit.Shape.Dim(leaf,.6f),8,5);
                }
                // The vault: bronze ribs arched across the deck every six metres from its eaves (above the roofs round it) to its crown, the glass between them.
                const float Rise=GardenTop-GardenEaves;
                Vector3 Arch(float z,float t)=>new(Mathf.Lerp(r.xMin,r.xMax,t),GardenEaves+Mathf.Sin(t*Mathf.PI)*Rise,z);
                for(float z=r.yMin;z<=r.yMax+.1f;z+=6)
                {
                    var path=new Vector3[13];for(int k=0;k<13;k++)path[k]=Arch(z,k/12f);
                    s.Slot(1).Tube(path,new[]{.32f},6,new Color(.62f,.44f,.22f),new Color(.82f,.62f,.32f),1,.5f,false);
                    if(z<r.yMax-1)for(int k=0;k<12;k++)glass.Quad(Arch(z,k/12f),Arch(z+6,k/12f),Arch(z+6,(k+1)/12f),Arch(z,(k+1)/12f),Vector3.up,new Color(.82f,.96f,.9f,.28f));
                }
                for(int k=1;k<12;k++){var path=new List<Vector3>();for(float z=r.yMin;z<=r.yMax+.1f;z+=6)path.Add(Arch(z,k/12f));s.Slot(1).Tube(path,new[]{.1f},4,new Color(.6f,.44f,.24f),new Color(.6f,.44f,.24f),1,1,false);}
                // Grow-lamps hung under the ridge, pale green-white.
                for(float z=r.yMin+3;z<r.yMax;z+=6)foreach(float t in new[]{.3f,.5f,.7f}){s.Slot(2).Lump(Arch(z,t)+Vector3.down*.7f,new Vector3(1.2f,.16f,.3f),(int)z,.05f,new Color(.9f,1f,.84f),new Color(.76f,.94f,.66f),8,4);var under=Arch(z,t);PoolAt(new Vector3(under.x,y+.4f,under.z),3.4f,PoolGreen);}
                // The vault's ends: the eaves come down to the deck on a low glazed wall, bronze-framed, so the garden is a room and not a sheet of glass on stilts.
                foreach(float z in new[]{r.yMin,r.yMax})for(int k=0;k<12;k++)glass.Quad(Arch(z,k/12f),Arch(z,(k+1)/12f),new Vector3(Mathf.Lerp(r.xMin,r.xMax,(k+1)/12f),y,z),new Vector3(Mathf.Lerp(r.xMin,r.xMax,k/12f),y,z),z==r.yMin?Vector3.back:Vector3.forward,new Color(.82f,.96f,.9f,.22f));
                foreach(float x in new[]{r.xMin,r.xMax})glass.Quad(new Vector3(x,GardenEaves,r.yMin),new Vector3(x,GardenEaves,r.yMax),new Vector3(x,y,r.yMax),new Vector3(x,y,r.yMin),x==r.xMin?Vector3.left:Vector3.right,new Color(.82f,.96f,.9f,.22f));
                var frame=kit.Built("Garden vault",s.Slot(2),GardenMake.plate,Trim,Glow);frame.transform.SetParent(root,true);
                var growth=kit.Built("Garden beds",green.Slot(1),Leaf,Water);growth.transform.SetParent(root,true);
                var vault=kit.Built("Garden glass",glass,GardenGlass);vault.transform.SetParent(root,true);vault.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                Sign("GARDEN",new Vector2(58,20),LinerY);
            }
            /// <summary>The cabins: Tallow's station hull, the convoy's first home, seated on a cradle deck at the north-west, with rows of cabin blocks lashed round it and their windows lit.</summary>
            static void Cabins(ZoneKit kit,Transform root)
            {
                float y=CabinY;
                if(ZoneKit.Landed("TallowStationHull"))
                {
                    var tallow=kit.Prop("TallowStationHull",P(TallowAt,y+.1f),TallowWide,15,false,false);tallow.name="Tallow's station hull";tallow.transform.SetParent(root,true);
                    var b=ZoneKit.Drawn(tallow);if(b.max.y>-2.6f)tallow.transform.position+=Vector3.down*(b.max.y+2.6f);
                }
                // The cabins themselves round it are its roofscape (Roofscapes, "cabins"): rows of deckhouses with their windows lit.
                Sign("CABINS",new Vector2(-62,44),y);
                // After the second part: the breach the alarm was about, patched, and the Steady Hand's two rescue cutters latched beside it (PatrolRepeat: "Two cutters stay on the seal").
                var sealed_=new GameObject("The breach sealed");sealed_.transform.SetParent(root,true);var patch=new ZoneKit.Shape();var at2=P(BreachAt,y+.05f);
                Flat(patch,0,at2,9,6,12,new Color(.9f,.9f,.86f));Flat(patch,1,at2+Vector3.up*.03f,7.5f,.5f,12,new Color(.2f,.56f,.58f));
                for(int k=0;k<6;k++){var bolt=at2+Quaternion.Euler(0,12,0)*new Vector3(-4+k*1.6f,.06f,2.8f);patch.Slot(2).Lump(bolt,Vector3.one*.14f,k,.05f,new Color(.7f,.95f,1f),new Color(.5f,.8f,.9f),6,4);}
                var drawn=kit.Built("Breach seal",patch.Slot(2),Trim,Trim,Glow);drawn.transform.SetParent(sealed_.transform,true);
                if(ZoneKit.Landed("CompactCutter"))foreach(var (dx,dz,yaw) in new[]{(-4f,-6f,30f),(5f,7f,200f)})
                {var cutter=kit.Prop("CompactCutter",new Vector3(at2.x+dx,y+.4f,at2.z+dz),2.4f,yaw,false,false);cutter.name="Steady Hand cutter";cutter.transform.SetParent(sealed_.transform,true);}
                var gate=new GameObject("The breach sealed (once Dessik Varn is beaten)",typeof(FlagGate)).GetComponent<FlagGate>();gate.flag="bossdown.DessikVarn";gate.targets=new[]{sealed_};sealed_.SetActive(false);
            }
            /// <summary>The raft bay: Driftreed's rafts afloat in an open hold, moored to its rim, mats drying on lines across the hatch, a lamp on each raft.</summary>
            static void RaftBay(ZoneKit kit,Transform root)
            {
                var s=new ZoneKit.Shape();var mats=new ZoneKit.Shape();var rng=new System.Random(389);var r=RaftPlan;var hold=Rect.MinMaxRect(r.xMin+6,r.yMin+6,r.xMax-6,r.yMax-6);
                // The hold: its four inner walls from the rim down to the water (the north wall's face, which looks south, is the one the camera sees: catwalks and lamps on it).
                Color wallTone=ZoneKit.Shape.Dim(RaftMake.paint,.8f);float rim=RaftRim+.02f,water=RaftWater;
                s.Slot(0).Quad(new Vector3(hold.xMin,rim,hold.yMax),new Vector3(hold.xMax,rim,hold.yMax),new Vector3(hold.xMax,water,hold.yMax),new Vector3(hold.xMin,water,hold.yMax),Vector3.back,wallTone,1/6f)
                    .Quad(new Vector3(hold.xMin,rim,hold.yMin),new Vector3(hold.xMin,rim,hold.yMax),new Vector3(hold.xMin,water,hold.yMax),new Vector3(hold.xMin,water,hold.yMin),Vector3.right,ZoneKit.Shape.Dim(wallTone,.8f),1/6f)
                    .Quad(new Vector3(hold.xMax,rim,hold.yMax),new Vector3(hold.xMax,rim,hold.yMin),new Vector3(hold.xMax,water,hold.yMin),new Vector3(hold.xMax,water,hold.yMax),Vector3.left,ZoneKit.Shape.Dim(wallTone,.7f),1/6f);
                for(float x=hold.xMin+2;x<hold.xMax-2;x+=5){s.Slot(2).Lump(new Vector3(x,rim-1.6f,hold.yMax-.2f),new Vector3(.4f,.22f,.12f),(int)x,.05f,new Color(1,.86f,.6f),new Color(1,.76f,.48f),6,4);PoolAt(new Vector3(x,water+.02f,hold.yMax-2.2f),2.6f,PoolWarm*.7f);}
                s.Slot(1).Beam(new Vector3(hold.xMin,rim-2.4f,hold.yMax-.8f),new Vector3(hold.xMax,rim-2.4f,hold.yMax-.8f),1.4f,.14f,RaftMake.trim);
                // The water.
                var w=new ZoneKit.Shape();for(float x=hold.xMin;x<hold.xMax;x+=8)for(float z=hold.yMin;z<hold.yMax;z+=8)
                {float x1=Mathf.Min(hold.xMax,x+8),z1=Mathf.Min(hold.yMax,z+8);float tone=.9f+.1f*(float)rng.NextDouble();w.Quad(new Vector3(x,water,z),new Vector3(x,water,z1),new Vector3(x1,water,z1),new Vector3(x1,water,z),Vector3.up,new Color(tone*.85f,tone,tone*.95f),1/18f);}
                var waterBody=kit.Built("Raft bay water",w,Water);waterBody.transform.SetParent(root,true);waterBody.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                // The rafts, moored in two rows, each with a lamp.
                var rafts=new[]{(-90f,-78f,10f),(-72f,-80f,-6f),(-54f,-77f,14f),(-38f,-80f,4f),(-86f,-58f,-12f),(-66f,-55f,8f),(-46f,-57f,-4f)};
                foreach(var (x,z,yaw) in rafts)
                {
                    var at=new Vector3(x,water+.1f,z);
                    if(ZoneKit.Landed("ReedRaft")){var raft=kit.Prop("ReedRaft",at,1.2f,yaw,false,false);raft.transform.localScale*=12/Mathf.Max(.1f,Mathf.Max(ZoneKit.Drawn(raft).size.x,ZoneKit.Drawn(raft).size.z));raft.name="Driftreed raft";raft.transform.SetParent(root,true);}
                    else s.Slot(0).Box(at+Vector3.up*.3f,new Vector3(6,.6f,10),Quaternion.Euler(0,yaw,0),new Color(.66f,.56f,.36f));
                    s.Slot(2).Lump(at+new Vector3(0,2.2f,0),Vector3.one*.28f,(int)x,.05f,new Color(1,.86f,.58f),new Color(1,.76f,.46f),6,4);PoolAt(at+Vector3.up*.05f,5.2f,PoolWarm*1.1f);
                    s.Slot(1).Rope(at+new Vector3(0,.8f,4),new Vector3(x,rim,hold.yMax+.2f),.6f,.06f,new Color(.8f,.72f,.5f));
                }
                // Mats drying on lines across the hatch: woven panels hung over lines from rim to rim, in Driftreed's dyes.
                for(int k=0;k<5;k++)
                {
                    float z=hold.yMin+5+k*((hold.height-10)/4);mats.Slot(1).Rope(new Vector3(hold.xMin-.5f,rim+.4f,z),new Vector3(hold.xMax+.5f,rim+.4f,z),.8f,.05f,new Color(.82f,.74f,.52f));
                    for(float x=hold.xMin+3;x<hold.xMax-3;x+=3.4f)
                    {
                        if(rng.Next(4)==0)continue;float t=(x-hold.xMin)/hold.width;float sag=.8f*4*t*(1-t);var top=new Vector3(x,rim+.4f-sag,z);
                        var dye=new[]{new Color(.82f,.62f,.36f),new Color(.6f,.7f,.42f),new Color(.78f,.5f,.4f),new Color(.86f,.8f,.6f),new Color(.5f,.62f,.66f)}[rng.Next(5)];
                        mats.Slot(0).Quad(top+new Vector3(-1.4f,0,-.05f),top+new Vector3(1.4f,0,-.05f),top+new Vector3(1.4f,-2.4f,-.4f),top+new Vector3(-1.4f,-2.4f,-.4f),Vector3.back+Vector3.up,dye,.6f);
                    }
                }
                var holdBody=kit.Built("Raft bay hold",s.Slot(2),RaftMake.plate,Trim,Glow);holdBody.transform.SetParent(root,true);
                var hung=kit.Built("Raft bay mats",mats.Slot(1),Weave,Trim);hung.transform.SetParent(root,true);
                Sign("RAFT BAY",new Vector2(-62,-35),RaftRim);
            }
            /// <summary>The freight hall: the tallest roof aft, its roof door (the way Hulljack goes, and the way after it), skylights, a roof crane.</summary>
            static void FreightHall(ZoneKit kit,Transform root)
            {
                var s=new ZoneKit.Shape();var rng=new System.Random(401);float y=HallY;var r=HallPlan;
                for(int k=0;k<8;k++){var at=new Vector3(r.xMin+6+(k%4)*10,y+.04f,r.yMin+8+(k/4)*46);Flat(s,2,at,5,2.4f,0,new Color(.7f,.66f,.54f));Flat(s,1,at+Vector3.up*.01f,5.4f,.2f,0,HallMake.trim);}
                // Chevrons painted round the door's apron: the hall's own white and graphite, not the frame's yellow.
                for(int k=0;k<10;k++){float a=k*Mathf.PI*2/10;var at=new Vector3(DoorAt.x+Mathf.Cos(a)*12.5f,y+.03f,DoorAt.y+Mathf.Sin(a)*10.5f);Flat(s,0,at,2.6f,.9f,-a*Mathf.Rad2Deg+90,new Color(.92f,.92f,.88f));}
                var top=kit.Built("Freight hall top",s.Slot(2),Heavy,Trim,Glow);top.transform.SetParent(root,true);
                RoofDoor(kit,root);
                Sign("FREIGHT HALL",new Vector2(44,-90),y);
            }
            /// <summary>The roof door in its three states: shut (until the frame tears through), torn open with the way after the frame (until the second part is won), patched after.</summary>
            static void RoofDoor(ZoneKit kit,Transform root)
            {
                float y=HallY;var c=P(DoorAt,y);var holder=new GameObject("Freight hall roof door");holder.transform.SetParent(root,true);
                // Shut: two leaves with a seam down the middle, hinges, the hall's chevrons.
                var shut=new ZoneKit.Shape();
                // Two pale leaves on the graphite roof, ribbed across, a dark seam down the middle, hinges, and a lit frame round the whole door so it reads as a door from the lane.
                foreach(float side in new[]{-1f,1f}){shut.Slot(0).Box(c+new Vector3(side*4.6f,.2f,0),new Vector3(9,.4f,13),Quaternion.identity,new Color(.72f,.72f,.68f),new Color(.82f,.82f,.78f),1/6f);
                    for(float z=-5.5f;z<6;z+=1.4f)shut.Slot(1).Box(c+new Vector3(side*4.6f,.42f,z),new Vector3(8.4f,.06f,.18f),Quaternion.identity,new Color(.5f,.5f,.48f));}
                shut.Slot(1).Box(c+new Vector3(0,.42f,0),new Vector3(.3f,.1f,13),Quaternion.identity,new Color(.12f,.12f,.14f));
                for(int k=0;k<4;k++)shut.Slot(1).Box(c+new Vector3(k<2?-9.3f:9.3f,.45f,-4.5f+(k%2)*9),new Vector3(.6f,.5f,1.4f),Quaternion.identity,new Color(.24f,.24f,.26f));
                foreach(var (from,to) in new[]{(new Vector3(-9.6f,.1f,-6.9f),new Vector3(9.6f,.1f,-6.9f)),(new Vector3(-9.6f,.1f,6.9f),new Vector3(9.6f,.1f,6.9f)),(new Vector3(-9.6f,.1f,-6.9f),new Vector3(-9.6f,.1f,6.9f)),(new Vector3(9.6f,.1f,-6.9f),new Vector3(9.6f,.1f,6.9f))})
                    shut.Slot(2).Beam(c+from,c+to,.22f,.1f,new Color(.7f,.92f,1f));
                var shutBody=kit.Built("Roof door shut",shut.Slot(2),Heavy,Trim,Glow);shutBody.transform.SetParent(holder.transform,true);
                var shutGate=new GameObject("Roof door shut (until the frame tears it open)",typeof(FlagGate)).GetComponent<FlagGate>();shutGate.flag="bossdown.Hulljack";shutGate.showWhenSet=false;shutGate.targets=new[]{shutBody};
                // Torn: the leaves wrenched up and bent back, a dark hole with the hall's light coming up out of it, scorch.
                var torn=new ZoneKit.Shape();
                torn.Slot(1).Box(c+new Vector3(0,-.6f,0),new Vector3(17,.2f,12),Quaternion.identity,new Color(.06f,.06f,.07f));
                torn.Slot(2).Box(c+new Vector3(0,-2.4f,0),new Vector3(15,.2f,10),Quaternion.identity,new Color(1,.74f,.42f));
                torn.Slot(0).Box(c+new Vector3(-8.6f,3.2f,1),new Vector3(9,.4f,13),Quaternion.Euler(0,8,78),new Color(.36f,.36f,.38f),null,1/6f).Box(c+new Vector3(9.4f,1.6f,-2),new Vector3(9,.4f,11),Quaternion.Euler(14,-6,-34),new Color(.34f,.34f,.36f),null,1/6f);
                for(int k=0;k<14;k++){float a=k*Mathf.PI*2/14;Flat(torn,1,c+new Vector3(Mathf.Cos(a)*10.5f,.03f,Mathf.Sin(a)*8),3.6f,1.2f,-a*Mathf.Rad2Deg,new Color(.14f,.12f,.11f));}
                // Shown from the frame's going through until the second part is won: two gates, one inside the other, so neither undoes the other's work.
                var tornHolder=new GameObject("Roof door torn (between the two parts)");tornHolder.transform.SetParent(holder.transform,true);
                var tornBody=kit.Built("Roof door torn",torn.Slot(2),Heavy,Trim,Glow);tornBody.transform.SetParent(tornHolder.transform,true);
                var glowUp=new GameObject("Hall light up through the door",typeof(Light));glowUp.transform.SetParent(tornBody.transform,true);glowUp.transform.position=c+Vector3.up*1.5f;
                var l=glowUp.GetComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.74f,.45f);l.range=18;l.intensity=2.6f;l.shadows=LightShadows.None;
                // The way after the frame, at the plane over the hole.
                // The way after the frame, at the plane over the door: always in the scene (a beacon reads its open words as it wakes, so one kept asleep until it opened would
                // wake with its refusal for a name), calling itself only the roof door while shut, and gone once the second part is won.
                var follow=new GameObject("Follow point");follow.transform.SetParent(holder.transform,true);follow.transform.position=P(DoorAt);
                var way=kit.Way(follow,"Follow Varn in — Many Hands freight hall",City,"FreightHall","bossdown.Hulljack","The roof door holds until the salvage frame tears through it",WorldBuilder.FlightDockRange,null);
                // Its sign on the roof below the door. Not the way's own words: while the door holds, the way's prompt is its refusal, so a sign in its words reads as a floor sign
                // then (36 px wanted) though SignStyle sized it as a warp's (30 px) when the zone loaded. In other words it is a floor sign throughout, sized as one.
                WorldBuilder.Label("FREIGHT HALL — the roof door",kit.On(DoorAt.x,DoorAt.y-9,.3f),2.8f);
                var tornGate=new GameObject("Roof door torn (once the frame is through it)",typeof(FlagGate)).GetComponent<FlagGate>();tornGate.flag="bossdown.Hulljack";tornGate.targets=new[]{tornHolder};tornHolder.SetActive(false);
                var tornGone=new GameObject("Roof door torn (until the second part is won)",typeof(FlagGate)).GetComponent<FlagGate>();tornGone.flag="bossdown.DessikVarn";tornGone.showWhenSet=false;tornGone.targets=new[]{tornBody};
                way.unnamedPrompt="The freight hall's roof door";
                var wayGone=new GameObject("The way after the frame (until the second part is won)",typeof(FlagGate)).GetComponent<FlagGate>();wayGone.flag="bossdown.DessikVarn";wayGone.showWhenSet=false;wayGone.targets=new[]{follow};
                // Patched after: plates welded over the hole, work lamps.
                var patched=new ZoneKit.Shape();
                for(int k=0;k<5;k++)patched.Slot(0).Box(c+new Vector3(-7.2f+k*3.6f,.18f,(k%2)*.6f),new Vector3(3.8f,.36f,12.5f+(k%2)),Quaternion.Euler(0,k*2-4,0),k%2==0?new Color(.62f,.6f,.56f):new Color(.5f,.52f,.5f),null,1/6f);
                for(int k=0;k<3;k++)patched.Slot(2).Lump(c+new Vector3(-6+k*6,.9f,-7.4f),new Vector3(.3f,.3f,.3f),k,.05f,new Color(1,.9f,.7f),new Color(1,.8f,.5f),6,4);
                var patchBody=kit.Built("Roof door patched",patched.Slot(2),Heavy,Trim,Glow);patchBody.transform.SetParent(holder.transform,true);
                var patchGate=new GameObject("Roof door patched (once the second part is won)",typeof(FlagGate)).GetComponent<FlagGate>();patchGate.flag="bossdown.DessikVarn";patchGate.targets=new[]{patchBody};patchBody.SetActive(false);
                // Where the frame goes: down through the door.
                var exit=new GameObject("Hulljack goes through the roof door",typeof(BossExit)).GetComponent<BossExit>();exit.transform.position=P(DoorAt);exit.boss="Hulljack";exit.sinks=true;
            }
            /// <summary>The stern dock: two landing pads with ships on them, a control cabin and pods, dock lamps.</summary>
            static void SternDock(ZoneKit kit,Transform root)
            {
                var s=new ZoneKit.Shape();float y=DockY;
                // The ship's name painted across the dock's deck, where every launch comes out, in the city's white: the name Pell chalked on the dock door, made official.
                Letters(s,0,"MANY HANDS",new Vector3(-55,y+.03f,-116.4f),4,new Color(.9f,.9f,.86f));
                foreach(var (at,ship,length,yaw) in new[]{(new Vector2(-48,-149),"Hauler",14f,20f),(new Vector2(-27,-147),"Skiff",9f,-30f)})
                {
                    if(ZoneKit.Landed("LandingPad")){var pad=kit.Prop("LandingPad",P(at,y),12,0,false,false);pad.transform.SetParent(root,true);}
                    if(ZoneKit.Landed(ship)){var parked=kit.Prop(ship,P(at,y+.6f),length,yaw,false,false);parked.name="Parked "+ship;parked.transform.SetParent(root,true);}
                }
                foreach(var (key,at,size,yaw) in new[]{("DockingPodOffice",new Vector2(-63,-124),5.6f,90f),("DockingPodRepair",new Vector2(-63,-151),5f,90f),("DockingPodOutfitter",new Vector2(-63,-137),5f,90f)})
                    if(ZoneKit.Landed(key)){var pod=kit.Prop(key,P(at,y),size,yaw,false,false);pod.transform.SetParent(root,true);}
                for(float x=-66;x<-18;x+=8)s.Slot(2).Lump(new Vector3(x,y+.5f,-158.6f),new Vector3(.3f,.2f,.2f),(int)x,.05f,new Color(.6f,.9f,1f),new Color(.5f,.8f,.9f),6,4);
                for(float x=-66;x<-18;x+=4)Flat(s,0,new Vector3(x,y+.03f,-106),2,.5f,0,new Color(.9f,.9f,.86f));
                var top=kit.Built("Dock top",s.Slot(2),Heavy,Trim,Glow);top.transform.SetParent(root,true);
                DockDressing(kit,root);
                Sign("DOCK",new Vector2(-50,-112),y);
            }
            /// <summary>The pad the party docks at, at the ships' height, held up on a pylon from the dock's deck.</summary>
            static void DockPylon(ZoneKit kit,GameObject pad)
            {
                var s=new ZoneKit.Shape();var at=P(DockAt,DockY);
                s.Slot(0).Tube(new[]{at,at+Vector3.up*3,at+Vector3.up*(Plane-DockY-.6f)},new[]{1.4f,1f,1.2f},10,new Color(.4f,.42f,.46f),new Color(.55f,.57f,.6f),1,.3f);
                foreach(int k in Enumerable.Range(0,4)){float a=k*Mathf.PI/2+Mathf.PI/4;s.Slot(0).Beam(at+new Vector3(Mathf.Cos(a)*4,0,Mathf.Sin(a)*4),at+Vector3.up*(Plane-DockY-1),.4f,.4f,new Color(.36f,.38f,.42f));}
                for(int k=0;k<8;k++){float a=k*Mathf.PI/4;s.Slot(1).Lump(P(DockAt,Plane-.35f)+new Vector3(Mathf.Cos(a)*2.9f,0,Mathf.Sin(a)*2.9f),Vector3.one*.14f,k,.05f,new Color(.6f,.94f,1f),new Color(.5f,.8f,.9f),6,4);}
                var pylon=kit.Built("Dock pylon",s.Slot(1),Trim,Glow);pylon.transform.SetParent(pad.transform,true);
            }
            /// <summary>The stern apron east of the spine: fuel tanks, pallets, cable drums, a gantry.</summary>
            static void Apron(ZoneKit kit,Transform root)
            {
                var rng=new System.Random(421);float y=ApronY;
                foreach(var (key,size) in new[]{("DressFuelTank",4.2f),("DressCargoPallet",1.8f),("DressCableDrum",2.2f),("DressFuelTank",3.6f),("DressPowerCell",2.4f),("DressCargoPallet",1.6f)})
                    for(int k=0;k<4;k++)
                    {
                        var at=new Vector2(Mathf.Lerp(28,70,(float)rng.NextDouble()),Mathf.Lerp(-154,-108,(float)rng.NextDouble()));if(!ZoneKit.Landed(key))continue;
                        var piece=kit.Prop(key,P(at,y),size*(.85f+.3f*(float)rng.NextDouble()),(float)rng.NextDouble()*360,false,false);piece.transform.SetParent(root,true);
                    }
                var s=new ZoneKit.Shape();Gantry(s,new Vector3(48,y,-130),40,4.6f,rng);
                for(int k=-3;k<=3;k++)PoolAt(new Vector3(48+k*6,y,-131.5f),3.4f,PoolCool);
                for(int k=0;k<16;k++)StainAt(new Vector3(Mathf.Lerp(26,72,(float)rng.NextDouble()),y,Mathf.Lerp(-156,-106,(float)rng.NextDouble())),1+(float)rng.NextDouble()*2,new Color(.4f,.42f,.44f),rng,2+rng.Next(2));
                var crane=kit.Built("Apron gantry",s.Slot(2),Trim,Trim,Glow);crane.transform.SetParent(root,true);
            }
            /// <summary>The lamp gallery at the bow: its south face all window with Pell's lamps behind it, its rail of lamps, the chart room's dome.</summary>
            static void Bow(ZoneKit kit,Transform root)
            {
                var s=new ZoneKit.Shape();var r=BowPlan;float y=BowY;
                for(float x=r.xMin+2;x<r.xMax-1;x+=3.6f){s.Slot(1).Post(new Vector3(x,y,r.yMin+.6f),1.1f,.06f,.05f,5,BowMake.trim,BowMake.trim);s.Slot(2).Lump(new Vector3(x,y+1.3f,r.yMin+.6f),new Vector3(.24f,.32f,.24f),(int)x,.05f,new Color(1,.78f,.44f),new Color(1,.6f,.3f),8,5);}
                s.Slot(1).Beam(new Vector3(r.xMin+1,y+1,r.yMin+.6f),new Vector3(r.xMax-1,y+1,r.yMin+.6f),.1f,.1f,BowMake.trim);
                // The chart room: a glazed dome on brass ribs over the gallery's middle, and under it Pell's great lamp, warm through the glass; a ring of her small lamps round its foot.
                var dome=new ZoneKit.Shape();var c=new Vector3(0,y,156);const float DomeR=9,DomeH=2.6f;
                Vector3 Dome(float a,float t)=>c+new Vector3(Mathf.Cos(a)*DomeR*Mathf.Cos(t*Mathf.PI*.5f),DomeH*Mathf.Sin(t*Mathf.PI*.5f),Mathf.Sin(a)*DomeR*Mathf.Cos(t*Mathf.PI*.5f));
                for(int k=0;k<24;k++){float a0=k*Mathf.PI/12,a1=(k+1)*Mathf.PI/12;for(int j=0;j<5;j++)dome.Quad(Dome(a0,j/5f),Dome(a1,j/5f),Dome(a1,(j+1)/5f),Dome(a0,(j+1)/5f),Vector3.up,new Color(1f,.94f,.84f,1));}
                for(int k=0;k<12;k++){float a=k*Mathf.PI/6;var rib=new Vector3[9];for(int j=0;j<9;j++)rib[j]=Dome(a,j/8f)+Vector3.up*.03f;s.Slot(1).Tube(rib,new[]{.13f},5,new Color(.7f,.52f,.26f),new Color(.86f,.66f,.34f),1,1,false);}
                foreach(float t in new[]{.35f,.7f})s.Slot(1).Tube(Enumerable.Range(0,33).Select(k=>Dome(k*Mathf.PI/16,t)+Vector3.up*.03f).ToArray(),new[]{.07f},4,new Color(.7f,.52f,.26f),new Color(.7f,.52f,.26f),1,1,false);
                // The crown: a brass cap and a lamp on it, the chart room's mark from the whole bow.
                s.Slot(1).Disc(c+Vector3.up*(DomeH+.04f),Vector3.up,.9f,16,new Color(.78f,.6f,.32f));s.Slot(2).Lump(c+Vector3.up*(DomeH+.32f),new Vector3(.3f,.3f,.3f),17,.04f,new Color(1,.9f,.66f),new Color(1,.82f,.5f),8,5);
                s.Slot(1).Tube(Enumerable.Range(0,33).Select(k=>Dome(k*Mathf.PI/16,0)+Vector3.up*.1f).ToArray(),new[]{.2f},6,new Color(.62f,.46f,.24f),new Color(.62f,.46f,.24f),1,1,false);
                s.Slot(0).Disc(c+Vector3.up*.02f,Vector3.up,DomeR,24,new Color(.36f,.28f,.2f));s.Slot(1).Post(c,.9f,.22f,.18f,8,new Color(.5f,.38f,.2f),new Color(.7f,.52f,.26f));
                s.Slot(2).Lump(c+Vector3.up*1.25f,new Vector3(.55f,.6f,.55f),9,.04f,new Color(1,.84f,.5f),new Color(1,.7f,.36f),12,8);
                for(int k=0;k<12;k++){float a=k*Mathf.PI/6+.13f;var at=c+new Vector3(Mathf.Cos(a)*(DomeR+.9f),0,Mathf.Sin(a)*(DomeR+.9f));s.Slot(1).Post(at,.7f,.05f,.04f,5,BowMake.trim,BowMake.trim);s.Slot(2).Lump(at+Vector3.up*.85f,new Vector3(.18f,.24f,.18f),k,.05f,new Color(1,.8f,.46f),new Color(1,.64f,.32f),8,5);}
                // Pell's lamps strung out from the dome's crown to the gallery's rail and corners on sagging lines, a lamp every metre and a half, alternately amber and pale.
                var crown=c+Vector3.up*(DomeH+.1f);
                foreach(var end in new[]{new Vector3(r.xMin+1,y+1.2f,r.yMin+.8f),new Vector3(-12,y+1.2f,r.yMin+.8f),new Vector3(0,y+1.2f,r.yMin+.8f),new Vector3(12,y+1.2f,r.yMin+.8f),new Vector3(r.xMax-1,y+1.2f,r.yMin+.8f),new Vector3(r.xMin+1,y+1,r.yMax-1),new Vector3(r.xMax-1,y+1,r.yMax-1)})
                {
                    float sag=.5f;s.Slot(1).Rope(crown,end,sag,.03f,new Color(.4f,.32f,.22f),4,10);float run=Vector3.Distance(crown,end);int n=Mathf.FloorToInt(run/1.5f);
                    for(int i=1;i<n;i++){float t=i/(float)n;var p=Vector3.Lerp(crown,end,t)+Vector3.down*(sag*4*t*(1-t)+.14f);bool amber=i%2==0;s.Slot(2).Lump(p,new Vector3(.13f,.17f,.13f),i,.05f,amber?new Color(1,.74f,.38f):new Color(1,.92f,.74f),amber?new Color(1,.6f,.3f):new Color(1,.86f,.6f),6,4);}
                }
                var top=kit.Built("Bow top",s.Slot(2),Liner,Trim,Glow);top.transform.SetParent(root,true);
                // Under the glass, Pell's lamp shop, lit; round the gallery's deck outside it, a compass rose; at the gallery's south edge its lit sign.
                LampShop(kit,root,c,DomeR);CompassRose(s,1,c,DomeR+1.6f,13);
                SignBoard(s,"LAMPS",new Vector3(-6,y,138.6f),.85f,new Color(1f,.86f,.56f),new Color(.2f,.15f,.1f),.25f);
                foreach(float x in new[]{-20f,20f})PoolAt(new Vector3(x,y,145.4f),3.6f,PoolWarm);
                var glazed=kit.Built("Chart room glass",dome,DomeGlass);glazed.transform.SetParent(root,true);glazed.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                foreach(var x in new[]{-20f,20f})if(ZoneKit.Landed("LampPost")){var lamp=kit.Prop("LampPost",new Vector3(x,y,146),2.2f,0,false,false);lamp.name="Pell's lamp";lamp.transform.SetParent(root,true);}
                Sign("LAMP GALLERY",new Vector2(0,138),y);
            }
            /// <summary>Every join on the ship: where two vessels meet, a raised collar of trim with clamp bands, cables lashed across it, patch plates over the seam and a line of seam lamps.</summary>
            static void Collars(ZoneKit kit,Transform root)
            {
                var s=new ZoneKit.Shape();var rng=new System.Random(433);
                // (from, to, the seam's floor, the gap's width): every line where one vessel meets the next. Each gap is a seam channel a step down from the lower of its two roofs, with
                // the collar along its floor; where two vessels butt together (the galley on the liner, the bow on the spine) the collar sits on the lower roof and there is no gap.
                var lines=new List<(Vector2 a,Vector2 b,float y,float gap)>
                {
                    (new(-72,-29),new(-16,-29),-8.5f,6),(new(16,-30),new(72,-30),-8,8),(new(-72,29),new(-16,29),-9.5f,6),(new(16,29),new(72,29),-10,6),
                    (new(-19,32),new(-19,126),-9.5f,6),(new(19,32),new(19,124),-10.5f,6),(new(-19,-98),new(-19,-32),-8.5f,6),(new(19,-100),new(19,-34),-6.5f,6),
                    (new(72,-32),new(72,28),LinerY,0),(new(72,30),new(98,30),-10,4),(new(-70,-101),new(-22,-101),-8.5f,6),(new(22,-102),new(74,-102),-7.5f,4),(new(-30,136),new(30,136),SpineY,0),
                };
                foreach(var (a,b,y,gap) in lines)
                {
                    var from=new Vector3(a.x,y,a.y);var to=new Vector3(b.x,y,b.y);var dir=(to-from).normalized;var across=Vector3.Cross(Vector3.up,dir);float length=Vector3.Distance(from,to);
                    // The seam's floor: plating a shade darker than anything either side of it, its texture laid along the seam.
                    if(gap>0)s.Slot(2).Quad(from-across*gap*.5f,to-across*gap*.5f,to+across*gap*.5f,from+across*gap*.5f,Vector3.up,new Color(.34f,.34f,.36f),1/6f);
                    from+=Vector3.up*.35f;to+=Vector3.up*.35f;
                    s.Slot(0).Beam(from,to,3.2f,.7f,new Color(.3f,.3f,.32f),new Color(.36f,.36f,.38f));
                    for(float t=2;t<length-1;t+=4.5f)
                    {
                        var at=from+dir*t;s.Slot(0).Box(at+Vector3.up*.2f,new Vector3(4.4f,.9f,.9f),Quaternion.LookRotation(across),new Color(.42f,.42f,.44f),new Color(.5f,.5f,.52f));
                        if(rng.Next(3)==0)s.Slot(0).Rope(at+across*2.5f+Vector3.up*.3f,at-across*2.5f+dir*1.4f+Vector3.up*.3f,-.5f,.08f,new Color(.7f,.66f,.56f));
                        if(rng.Next(4)==0)s.Slot(0).Box(at+dir*2+across*(rng.Next(2)==0?2.4f:-2.4f)+Vector3.down*.3f,new Vector3(2.6f,.2f,2.2f),Quaternion.LookRotation(dir),Paints[rng.Next(Paints.Length)]*.8f);
                    }
                    for(float t=1;t<length;t+=6)s.Slot(1).Lump(from+dir*t+Vector3.up*.42f,new Vector3(.2f,.08f,.2f),(int)t,.05f,new Color(.7f,.92f,1f),new Color(.6f,.82f,.92f),6,3);
                }
                var collars=kit.Built("Joins",s.Slot(2),Trim,Glow,Heavy);collars.transform.SetParent(root,true);
            }
            /// <summary>A district's name lying on its roof, a hand above it: SignStyle lays it flat at load and sizes it for the flight camera. The roofscape keeps off its ground.</summary>
            static void Sign(string text,Vector2 at,float roof){Signed.Add(at);WorldBuilder.Label(text,new Vector3(at.x,roof+.15f,at.y),6);}
            static readonly List<Vector2> Signed=new();

            // ================================================================ roofscapes
            /// <summary>Every roof the camera flies over, dressed as the people of its make keep it, so no frame of the hull is bare plating: each style is a mix of the pieces in
            /// <see cref="Roofscape"/>. Kept off the four drums and their room, the roof door, the dock's pylon, the cover's feet, the funnels and stacks, the signs, Tallow's hull,
            /// the breach, the promenade's glass and the lifeboats' rows.</summary>
            static void Roofscapes(ZoneKit kit,Transform root)
            {
                var discs=new List<(Vector2 at,float r)>{(DoorAt,14),(DockAt,6),(TallowAt,TallowWide*.5f+2),(BreachAt,9),(new(-52,-12),4.5f),(new(58,12),4.5f),(new(7,0),5),(new(-48,-149),7),(new(-27,-147),7)};
                for(float x=-53;x<-16;x+=4.5f)discs.Add((new Vector2(x,-113.6f),3.6f));
                foreach(var j in Joins)discs.Add((j,DrumRadius+2));foreach(var c in Cover)discs.Add((c.at,3.2f));foreach(var sign in Signed)discs.Add((sign,7));
                foreach(var at in new[]{new Vector2(80,-18),new Vector2(88,-6),new Vector2(82,8),new Vector2(93,16),new Vector2(90,-22),new Vector2(0,156)})discs.Add((at,at.y==156?14.5f:3));
                // The look pass's pieces: the dock's tower, floods, gear, the ring and numbers round the pylon and its taxi lines; the galley's and the bow's signs; the bow's lamp posts; the gantries' legs.
                discs.Add((DockTowerAt,4.6f));foreach(var f in DockFloods)discs.Add((f,2.2f));discs.Add((DockAt,9));
                foreach(var at in new[]{new Vector2(-55,-130),new Vector2(-53,-127.5f),new Vector2(-56,-121),new Vector2(-20.5f,-128),new Vector2(-19.5f,-133),new Vector2(-34,-108.4f),new Vector2(-45,-108.6f),new Vector2(-31.4f,-139.6f),new Vector2(-56.6f,-143.6f),new Vector2(-35.6f,-141.6f)})discs.Add((at,2.4f));
                foreach(var pad in new[]{new Vector2(-48,-149),new Vector2(-27,-147)}){var from=DockAt+new Vector2(0,-7.6f);for(float t=0;t<=1;t+=.15f)discs.Add((Vector2.Lerp(from,pad,t),1.6f));}
                discs.Add((new Vector2(87,12.5f),5));discs.Add((new Vector2(-6,138.6f),3.6f));discs.Add((new Vector2(-20,146),2));discs.Add((new Vector2(20,146),2));
                foreach(float g in GantryZ)foreach(float x in new[]{-15f,15f})for(float dz=-6;dz<=6;dz+=3)discs.Add((new Vector2(x,g+dz),2.4f));
                bool Clear(Vector2 p,float half)=>!discs.Any(d=>Vector2.Distance(d.at,p)<d.r+half);
                bool OffDecks(Vector2 p)=>!UpperDecks.Any(u=>Grown(u,4.5f).Contains(p));
                // style weights: deckhouse, vents, pipe, hatch, skylight, tank, aerial, lights, lane, solar, crates, a corner where people sit or keep things, a lido; and each district's
                // own house (1 liner, 2 cabins, 3 raft folk, 4 dock, 5 bow), so the liner's length, the cabins' rows and the bow's stalls don't repeat one another's shapes.
                Roofscape(kit,root,"liner west",Rect.MinMaxRect(-71,-25,-17,25),LinerY,LinerMake,601,new[]{2,1,0,.5f,1.5f,0,.3f,1,.5f,0,0,2,1},p=>Mathf.Abs(p.y)>9.5f&&Mathf.Abs(p.y)<20.5f&&OffDecks(p),Clear,house:1);
                Roofscape(kit,root,"liner east",Rect.MinMaxRect(17,-25,71,25),LinerY,LinerMake,607,new[]{2,1,0,.5f,1.5f,0,.3f,1,.5f,0,0,2,1},p=>Mathf.Abs(p.y)>9.5f&&Mathf.Abs(p.y)<20.5f&&OffDecks(p),Clear,house:1);
                Roofscape(kit,root,"galley",Rect.MinMaxRect(73,-31,99,27),GalleyY,GalleyMake,613,new[]{0,3,3,1,1,2,.5f,1,0,0,0},null,Clear);
                Roofscape(kit,root,"cabins",Rect.MinMaxRect(-103,33,-23,125),CabinY,CabinMake,619,new[]{6,.5f,.5f,.5f,1,1,1.5f,1,.5f,1.5f,.5f,2,0},null,Clear,house:2);
                Roofscape(kit,root,"raft bay rim",RaftPlan,RaftRim,RaftMake,631,new[]{1.5f,1,1,1,0,.5f,.5f,2,0,0,1,1.5f,0},p=>!RaftHold.Contains(p),Clear,RaftHold,house:3);
                Roofscape(kit,root,"freight hall",HallPlan,HallY,HallMake,641,new[]{0,2,1,2,0,1,0,1,2,0,.5f},null,Clear);
                Roofscape(kit,root,"spine",SpinePlan,SpineY,SpineMake,653,new[]{0,1f,0,3,0,0,0,2,0,0,2},p=>Mathf.Abs(p.x)>11.5f&&!Grown(BridgeAt,3).Contains(p),Clear);
                Roofscape(kit,root,"dock",DockPlan,DockY,DockMake,659,new[]{1,.5f,1,1,0,1,.5f,1.5f,2,0,2,2,0},null,Clear,house:4);
                Roofscape(kit,root,"apron",ApronPlan,ApronY,DockMake,661,new[]{.5f,1,2,1,0,1,.5f,1,2,0,1,1,0},null,Clear,house:4);
                Roofscape(kit,root,"bow",BowPlan,BowY,BowMake,673,new[]{1.5f,0,0,.5f,1.5f,0,0,1.5f,.5f,0,0,1.5f,0},p=>p.y<166,Clear,house:5);
            }
            /// <summary>One roof's furniture on a jittered grid about six metres apart (a frame of the play view holds a dozen): <paramref name="weights"/> picks among a deckhouse (its
            /// windows lit on its south face, which the camera sees, a door with a lamp, an aerial or a vent on top), a vent cluster, a pipe run with a valve, a hatch (one in four open on a
            /// lit room), a skylight, a tank, an aerial with a dish, a row of deck lights, a painted lane, a rack of solar panels, a stack of crates. Every draw is taken before
            /// <paramref name="where"/> or a keep-out drops a piece (D213).</summary>
            static void Roofscape(ZoneKit kit,Transform root,string name,Rect r,float y,Make m,int seed,float[] weights,Func<Vector2,bool> where,Func<Vector2,float,bool> clear,Rect? notIn=null,int house=0)
            {
                var s=new ZoneKit.Shape();var rng=new System.Random(seed);float R()=>(float)rng.NextDouble();float total=weights.Sum();float texel=1/(m.plate==Liner?4f:m.plate==Ribbed?3f:6f);
                Color dark=new(.12f,.14f,.18f),warmLight=new(1,.8f,.5f),coolLight=new(.74f,.9f,1f),steel=new(.56f,.57f,.6f);
                for(float z=r.yMin+2.6f;z<r.yMax-2;z+=5.2f)for(float x=r.xMin+2.6f;x<r.xMax-2;x+=5.2f)
                {
                    var at=new Vector2(x+R()*3-1.5f,z+R()*3-1.5f);float a=R(),b=R(),c=R(),pick=R()*total,keep=R();int sub=rng.Next(1000);int kind=0;
                    for(float run=weights[0];kind<weights.Length-1&&pick>run;run+=weights[++kind]){}
                    float half=kind==0?4.5f:kind==2?6:kind==12?5:kind==11?3.5f:3;
                    if(keep>.9f||where!=null&&!where(at)||!clear(at,half)||notIn is Rect hole&&Rect.MinMaxRect(hole.xMin-half,hole.yMin-half,hole.xMax+half,hole.yMax+half).Contains(at))continue;
                    if(at.x-half<r.xMin||at.x+half>r.xMax||at.y-half<r.yMin||at.y+half>r.yMax)continue;
                    var p=new Vector3(at.x,y,at.y);float room=Plane-.9f-y;var paint=Color.Lerp(m.paint,Paints[sub%Paints.Length],.22f);paint=ZoneKit.Shape.Dim(paint,.88f+.2f*c);paint.a=1;
                    switch(kind)
                    {
                        case 0:
                        {
                            // A deckhouse in its district's manner (House), or the plain one: square to the hull so its south face looks at the camera, one floor or two, a row of windows to a floor.
                            if(house!=0){if(!House(s,house,p,m,paint,room,a,b,c,sub,rng,texel))continue;break;}
                            float w=3.6f+a*4.8f,d=2.8f+b*2.6f,h=c<.6f?2.4f:4.6f;if(h+1.1f>room)h=2.4f;if(h+1.1f>room)continue;
                            s.Slot(0).Box(p+Vector3.up*h*.5f,new Vector3(w,h,d),Quaternion.identity,paint,ZoneKit.Shape.Dim(paint,1.08f),texel).Slot(1).Box(p+Vector3.up*(h+.08f),new Vector3(w+.3f,.16f,d+.3f),Quaternion.identity,m.trim);
                            int n=Mathf.Max(1,Mathf.FloorToInt((w-1.6f)/1.5f));
                            for(float row=1.25f;row<h-.5f;row+=2.2f)for(int i=0;i<n;i++)
                            {
                                var wc=p+new Vector3(-w*.5f+1.4f+i*1.5f,row,-d*.5f-.03f);bool lit=rng.Next(5)>0;var tone=rng.Next(4)==0?coolLight:warmLight;
                                if(m.windows==1){s.Slot(1).Disc(wc,Vector3.back,.42f,10,m.trim);s.Slot(lit?2:1).Disc(wc+Vector3.back*.01f,Vector3.back,.32f,10,lit?tone:dark);}
                                else{Pane(s,1,wc,Vector3.right,1,.82f,Vector3.back,m.trim);Pane(s,lit?2:1,wc+Vector3.back*.01f,Vector3.right,.84f,.66f,Vector3.back,lit?tone:dark);}
                            }
                            var door=p+new Vector3(w*.5f-.8f,.95f,-d*.5f-.03f);Pane(s,1,door,Vector3.right,.9f,1.8f,Vector3.back,new Color(.22f,.24f,.26f));s.Slot(2).Lump(door+new Vector3(0,1.15f,-.12f),new Vector3(.16f,.1f,.1f),sub,.05f,warmLight,warmLight,6,3);PoolAt(door+new Vector3(0,-.95f,-1.3f),2,PoolWarm*.8f);
                            foreach(float side in new[]{-1f,1f})if(rng.Next(2)==0){var wc=p+new Vector3(side*(w*.5f+.03f),1.25f,0);Pane(s,rng.Next(4)>0?2:1,wc,Vector3.forward,.84f,.66f,Vector3.right*side,warmLight);}
                            if(sub%3==0)s.Slot(1).Box(p+new Vector3(w*.2f,h+.55f,d*.1f),new Vector3(1.2f,.8f,1),Quaternion.identity,steel,ZoneKit.Shape.Dim(steel,1.15f));
                            else if(sub%3==1){s.Slot(1).Post(p+new Vector3(-w*.3f,h+.16f,0),1.8f+a,.05f,.04f,5,m.trim,m.trim);s.Slot(2).Lump(p+new Vector3(-w*.3f,h+2.1f+a,0),Vector3.one*.1f,sub,.05f,new Color(1,.3f,.25f),new Color(1,.3f,.25f),6,3);}
                            break;
                        }
                        case 1:
                        {
                            // Vents: two or three louvred boxes and a mushroom ventilator.
                            int n=2+sub%2;for(int i=0;i<n;i++){var v=p+new Vector3(i*1.7f-n*.7f,0,(i%2)*.8f);s.Slot(1).Box(v+Vector3.up*.55f,new Vector3(1.3f,1.1f,1),Quaternion.identity,steel,ZoneKit.Shape.Dim(steel,1.15f));
                                for(int k=0;k<3;k++)s.Slot(1).Box(v+new Vector3(0,.3f+k*.28f,-.52f),new Vector3(1.1f,.07f,.06f),Quaternion.identity,dark);}
                            var mv=p+new Vector3(1.8f,0,1.6f);s.Slot(1).Tube(new[]{mv,mv+Vector3.up*1.3f,mv+new Vector3(0,1.7f,-.4f)},new[]{.26f,.24f,.36f},10,m.trim,paint,1,.5f);
                            break;
                        }
                        case 2:
                        {
                            // A pipe run: along, a bend, on, on low supports, a valve wheel at the bend.
                            float rad=.18f+.14f*c;var tone=sub%3==0?steel:sub%3==1?new Color(.5f,.3f,.24f):new Color(.3f,.5f,.52f);var bend=p+new Vector3((a-.5f)*8,.45f,0);var start=bend+Vector3.left*(3+a*4);var end=bend+Vector3.forward*(b<.5f?1:-1)*(3+b*3);
                            s.Slot(1).Tube(new[]{start,bend,end},new[]{rad},8,tone,ZoneKit.Shape.Dim(tone,1.12f),1,.5f);
                            for(float t=0;t<=1;t+=.34f){var at2=Vector3.Lerp(start,bend,t);s.Slot(1).Box(new Vector3(at2.x,y+.2f,at2.z),new Vector3(.3f,.4f,.7f),Quaternion.identity,dark);}
                            s.Slot(1).Disc(bend+Vector3.up*(rad+.3f),Vector3.up,.42f,12,new Color(.7f,.24f,.2f),.32f);
                            break;
                        }
                        case 3:
                        {
                            // A hatch: a dark square in a raised frame with a handle; one in four standing open on a lit room.
                            s.Slot(1).Box(p+Vector3.up*.08f,new Vector3(2.1f,.16f,2.1f),Quaternion.identity,steel);
                            if(sub%4==0){Flat(s,2,p+Vector3.up*.17f,1.7f,1.7f,0,warmLight);PoolAt(p,2.2f,PoolWarm*.7f);s.Slot(1).Box(p+new Vector3(0,.95f,1),new Vector3(1.8f,1.8f,.12f),Quaternion.Euler(-18,0,0),paint);}
                            else{Flat(s,1,p+Vector3.up*.17f,1.8f,1.8f,0,ZoneKit.Shape.Dim(paint,.7f));s.Slot(1).Beam(p+new Vector3(-.4f,.26f,-.6f),p+new Vector3(.4f,.26f,-.6f),.08f,.08f,new Color(.8f,.78f,.7f));}
                            break;
                        }
                        case 4:
                        {
                            // A skylight: the room under it lit, mullions across it.
                            float w=2.4f+a*3,d=1.4f+b*1.2f;s.Slot(1).Box(p+Vector3.up*.12f,new Vector3(w+.3f,.24f,d+.3f),Quaternion.identity,m.trim);Flat(s,2,p+Vector3.up*.25f,w,d,0,sub%5==0?coolLight:warmLight);
                            for(float t=-w*.5f+1;t<w*.5f;t+=1)s.Slot(1).Beam(p+new Vector3(t,.27f,-d*.5f),p+new Vector3(t,.27f,d*.5f),.07f,.04f,m.trim);
                            break;
                        }
                        case 5:
                        {
                            // A tank on a cradle, banded.
                            float rad=.9f+a*.7f,tall=Mathf.Min(1.4f+b*1.8f,room-.1f);if(tall<1)continue;var tone=sub%3==0?new Color(.86f,.86f,.82f):sub%3==1?new Color(.3f,.56f,.58f):steel;
                            s.Slot(1).Post(p,tall,rad,rad,16,ZoneKit.Shape.Dim(tone,.85f),tone).Tube(new[]{p+Vector3.up*tall*.3f,p+Vector3.up*(tall*.3f+.2f)},new[]{rad+.05f},16,dark,dark,1,1,false).Disc(p+Vector3.up*tall,Vector3.up,rad,16,ZoneKit.Shape.Dim(tone,1.1f));
                            break;
                        }
                        case 6:
                        {
                            // An aerial: a mast with a crossbar and a dish, a red light on top.
                            float tall=Mathf.Min(2.6f+a*2.4f,room-.3f);if(tall<1.4f)continue;s.Slot(1).Post(p,tall,.07f,.05f,5,m.trim,steel).Beam(p+new Vector3(-.8f,tall*.7f,0),p+new Vector3(.8f,tall*.7f,0),.05f,.05f,steel);
                            s.Slot(1).Disc(p+new Vector3(.3f,tall*.45f,-.3f),new Vector3(.4f,.5f,-.8f),.7f,12,new Color(.85f,.85f,.82f));s.Slot(2).Lump(p+Vector3.up*(tall+.1f),Vector3.one*.12f,sub,.05f,new Color(1,.3f,.25f),new Color(1,.3f,.25f),6,3);
                            break;
                        }
                        case 7:
                        {
                            // A row of deck lights along a walk.
                            bool along=sub%2==0;var tone=sub%3==0?coolLight:warmLight;for(int i=0;i<5;i++){var l=p+(along?Vector3.right:Vector3.forward)*(i-2)*2.1f;s.Slot(1).Post(l,.4f,.12f,.1f,6,m.trim,m.trim);s.Slot(2).Lump(l+Vector3.up*.5f,new Vector3(.16f,.12f,.16f),sub+i,.05f,tone,tone,6,3);PoolAt(l,1.5f,tone==coolLight?PoolCool:PoolWarm*.8f);}
                            break;
                        }
                        case 8:
                        {
                            // A painted lane: two lines and the chevrons between them, the city's white and teal.
                            bool along=sub%2==0;var dir=along?Vector3.right:Vector3.forward;var across=along?Vector3.forward:Vector3.right;var line=sub%3==0?new Color(.3f,.62f,.64f):new Color(.9f,.9f,.86f);
                            foreach(float side in new[]{-.9f,.9f})Flat(s,1,p+across*side+Vector3.up*.02f,along?10:.18f,along?.18f:10,0,line);
                            for(int i=-2;i<=2;i++)Flat(s,1,p+dir*i*2+Vector3.up*.02f,along?.5f:1.2f,along?1.2f:.5f,along?35:-55,line);
                            break;
                        }
                        case 9:
                        {
                            // A rack of solar panels tilted to the star.
                            for(int i=0;i<3;i++)for(int k=0;k<2;k++){var pc=p+new Vector3((i-1)*1.7f,.6f,(k-.5f)*1.3f);s.Slot(1).Box(pc,new Vector3(1.6f,.06f,1.2f),Quaternion.Euler(-20,0,0),new Color(.14f,.2f,.36f),new Color(.2f,.28f,.48f));s.Slot(1).Post(pc+Vector3.down*.6f,.5f,.04f,.04f,4,m.trim,m.trim);}
                            break;
                        }
                        case 11:Corner(s,house,p,m,room,sub,rng);break;
                        case 12:Lido(s,p,m,sub);break;
                        default:
                        {
                            // Crates, stacked and lashed.
                            int n=2+sub%3;for(int i=0;i<n;i++){float sz=1.1f+.5f*((sub>>i)&1);var cp=p+new Vector3((i%2)*1.5f-.7f,sz*.5f+(i/2)*1.3f,(i/2)*.2f);s.Slot(3).Box(cp,Vector3.one*sz,Quaternion.Euler(0,(i*17+sub)%20-10,0),Paints[(sub+i)%Paints.Length],null,.6f);}
                            break;
                        }
                    }
                }
                // The east liner's bands and the raft bay's six-metre rim keep every piece out: nothing to stand, so nothing is stood (an empty mesh drawn with four materials).
                if(s.Empty)return;
                var built=kit.Built("Roofscape "+name,s.Slot(3),m.plate,Trim,Glow,Crate);built.transform.SetParent(root,true);
            }

            // ================================================================ the fight
            static string Foe(string id,string standIn)=>ZoneKit.Find<EnemyDef>(id)!=null?id:standIn;
            static void Fight(ZoneKit kit)
            {
                var root=new GameObject("Hulljack's fight");
                // The four joins it has filed on: a coupling drum swelling up out of the collar to just under the plane, where the frame stands; an anchor at each (what the boss reads,
                // HullJoin); its claim tags round the drum's rim and a lamp; at the first, the cut it is making.
                for(int n=0;n<Joins.Length;n++)
                {
                    var anchor=new GameObject("Hulljack join "+(n+1),typeof(HullJoin));anchor.transform.SetParent(root.transform,true);anchor.transform.position=P(Joins[n]);anchor.GetComponent<HullJoin>().index=n+1;
                    var c=P(Joins[n],JoinTops[n]);var drum=Drum(c,RoofAt(Joins[n]+Vector2.up*(DrumRadius+1))<RoofAt(Joins[n]+Vector2.down*(DrumRadius+1))?RoofAt(Joins[n]+Vector2.up*(DrumRadius+1)):RoofAt(Joins[n]+Vector2.down*(DrumRadius+1)),n);
                    var drumBody=kit.Built("Coupling drum "+(n+1),drum.Slot(2),Heavy,Trim,Glow);drumBody.transform.SetParent(anchor.transform,true);
                    // Hulljack's claim tags, bolted to the drum's side where the camera sees them and below the plane, out of the fight: the lit orange plates chapter 3's tags were.
                    var tags=new ZoneKit.Shape();
                    foreach(float a in new[]{150f,180f,210f})
                    {
                        var outward=Quaternion.Euler(0,a,0)*Vector3.forward;var at=c+outward*(DrumRadius+.12f)+Vector3.down*1.5f;
                        tags.Slot(0).Box(at,new Vector3(1.6f,1,.14f),Quaternion.LookRotation(outward),new Color(.3f,.3f,.32f));tags.Slot(1).Box(at+outward*.1f,new Vector3(1.3f,.76f,.1f),Quaternion.LookRotation(outward),Color.white);
                    }
                    var drawn=kit.Built("Claim tags at join "+(n+1),tags.Slot(1),Trim,ClaimGlow);drawn.transform.SetParent(anchor.transform,true);
                    var lamp=new GameObject("Claim tag light",typeof(Light));lamp.transform.SetParent(anchor.transform,true);lamp.transform.position=c+new Vector3(0,-1,-DrumRadius-2);
                    var l=lamp.GetComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.55f,.22f);l.range=7;l.intensity=1.6f;l.shadows=LightShadows.None;
                }
                Cut(kit,root.transform);
                foreach(var (join,kindOf,at,to) in Cover)CoverPiece(kit,root.transform,join,kindOf,at,to);
                // The claim drones tagging the raft bay's hatch on the approach: on show from the dock, waking as a ship comes over the hold.
                var drones=kit.FlightFight("Hull_Drones",P(DronesAt),new Vector3(44,6,32),(Foe("ClaimHauler","ClaimDrone"),3));drones.requiredFlag="hands.claimRead";
                var show=new GameObject("Drones on show").transform;show.SetParent(drones.transform,false);show.position=P(DronesAt);drones.previewPoint=show;drones.previewRadius=0;
                // The door: a box over the four joins, the frame clamped on and cutting at the first, on show and inert from the approach.
                var duel=WorldBuilder.Encounter("Hulljack",P(Joins[0]),FightBox,Foe("Hulljack","ClaimDrone"),1);duel.transform.SetParent(root.transform,true);
                duel.GetComponent<BoxCollider>().center=P(FightAt)-P(Joins[0]);
                var post=duel.spawners[0];post.transform.SetPositionAndRotation(P(Joins[0]),Quaternion.Euler(0,180,0));post.faceAsPlaced=true;post.radius=0;
                duel.requiredFlag="hands.claimRead";duel.checkpointSpawn="Hulljack";
                var view=new GameObject("Hulljack preview (the approach)");view.transform.SetParent(root.transform,true);view.transform.position=P(Joins[0]+new Vector2(4,-24));duel.previewPoint=view.transform;duel.previewRadius=26;
                // Where its drones come from (a direction only: the fight launches them a short flight from the frame, from the tug's side).
                var rack=new GameObject("Hulljack tug rack",typeof(ClaimTug));rack.transform.SetParent(root.transform,true);rack.transform.position=P(TugAt+new Vector2(-14,10));
                // The hull is clear once the frame is through the door: the world's flag, for whatever waits on it.
                var cleared=new GameObject("The hull is clear (once the frame is gone)",typeof(FlagAll)).GetComponent<FlagAll>();cleared.transform.SetParent(root.transform,true);
                cleared.flags=new[]{"bossdown.Hulljack"};cleared.result="hands.hullCleared";
            }
            /// <summary>A fight join's coupling drum: the collar between the two vessels swells into a round coupling from the lower roof up to just under the plane, plated on top in rings
            /// with four clamp sockets where a frame's feet go, ribbed and banded down its side, a ring of seam lamps under its rim.</summary>
            static ZoneKit.Shape Drum(Vector3 top,float baseY,int n)
            {
                var s=new ZoneKit.Shape();float R=DrumRadius;var foot=new Vector3(top.x,baseY-.4f,top.z);Color plate=new(.5f,.52f,.56f),band=new(.3f,.31f,.34f),deck=new(.58f,.6f,.62f);
                s.Slot(0).Tube(new[]{foot,top},new[]{R,R},40,ZoneKit.Shape.Dim(plate,.8f),plate,10,1/6f,false);
                // The deck in rings of plates, each a shade apart, so it reads as plating from the play view and not as one grey coin.
                var rng=new System.Random(900+n);float[] rings={0,2.6f,5.4f,R};
                for(int ring=0;ring<3;ring++){int cells=ring==0?6:ring==1?14:24;for(int k=0;k<cells;k++)
                {
                    float a0=k*360f/cells,a1=(k+1)*360f/cells;float r0=rings[ring],r1=rings[ring+1];var tone=ZoneKit.Shape.Dim(deck,.86f+.2f*(float)rng.NextDouble());
                    Vector3 At(float r,float a)=>top+Quaternion.Euler(0,a,0)*Vector3.forward*r;
                    if(r0<.01f)s.Slot(0).Triangle(top,At(r1,a0),At(r1,a1),Vector3.up,tone,1/6f);else s.Slot(0).Quad(At(r0,a0),At(r1,a0),At(r1,a1),At(r0,a1),Vector3.up,tone,1/6f);
                }}
                // Chevrons round the rim in the city's white and teal: the place a frame clamps, painted as one.
                for(int k=0;k<36;k++){var dir=Quaternion.Euler(0,k*10,0)*Vector3.forward;var at=top+dir*(R-.75f)+Vector3.up*.03f;s.Slot(1).Box(at,new Vector3(1.1f,.02f,.42f),Quaternion.LookRotation(Vector3.Cross(Vector3.up,dir))*Quaternion.Euler(0,30,0),k%2==0?new Color(.9f,.9f,.86f):new Color(.24f,.56f,.58f));}
                // A raised rim and two clamp bands round the side.
                s.Slot(1).Tube(Enumerable.Range(0,41).Select(k=>top+Quaternion.Euler(0,k*9,0)*Vector3.forward*R+Vector3.up*.06f).ToArray(),new[]{.22f},6,band,band,1,1,false);
                foreach(float drop in new[]{1.8f,4.2f})if(top.y-drop>baseY+.4f)s.Slot(1).Tube(Enumerable.Range(0,41).Select(k=>top+Quaternion.Euler(0,k*9,0)*Vector3.forward*(R+.08f)+Vector3.down*drop).ToArray(),new[]{.28f},6,band,ZoneKit.Shape.Dim(band,1.2f),1,1,false);
                for(int k=0;k<18;k++){float a=k*20+10;var outward=Quaternion.Euler(0,a,0)*Vector3.forward;s.Slot(1).Box(new Vector3(top.x,(top.y+baseY)*.5f,top.z)+outward*(R+.12f),new Vector3(.5f,top.y-baseY,.3f),Quaternion.LookRotation(outward),band);}
                // The deck: radial seams, an inner ring, four clamp sockets on the diagonals where a frame's feet fit, bolts round each.
                for(int k=0;k<12;k++){var dir=Quaternion.Euler(0,k*30,0)*Vector3.forward;s.Slot(1).Box(top+dir*(R*.62f)+Vector3.up*.02f,new Vector3(.12f,.04f,R*.72f),Quaternion.LookRotation(dir),band);}
                s.Slot(1).Disc(top+Vector3.up*.03f,Vector3.up,R*.3f,24,band,R*.26f);
                foreach(float a in new[]{45f,135f,225f,315f})
                {
                    var at=top+Quaternion.Euler(0,a,0)*Vector3.forward*3.6f+Vector3.up*.04f;s.Slot(1).Disc(at,Vector3.up,1.5f,20,new Color(.18f,.18f,.2f)).Disc(at+Vector3.up*.01f,Vector3.up,1.5f,20,band,1.2f);
                    for(int b=0;b<8;b++)s.Slot(1).Lump(at+Quaternion.Euler(0,b*45,0)*Vector3.forward*1.75f+Vector3.up*.05f,new Vector3(.12f,.06f,.12f),b,.05f,new Color(.6f,.6f,.62f),band,6,3);
                }
                // Seam lamps under the rim, cool white, as along every join.
                for(int k=0;k<24;k++){var outward=Quaternion.Euler(0,k*15,0)*Vector3.forward;s.Slot(2).Lump(top+outward*(R+.2f)+Vector3.down*.7f,new Vector3(.22f,.1f,.22f),k+n*30,.05f,new Color(.7f,.92f,1f),new Color(.6f,.82f,.92f),6,3);}
                return s;
            }
            /// <summary>The cut the frame is making at the first join when the party arrives: a seam gone white-hot along the collar with sparks off it; once the frame is beaten, a scorched line.</summary>
            static void Cut(ZoneKit kit,Transform root)
            {
                var c=P(Joins[0],JoinTops[0]+.75f);var hot=new ZoneKit.Shape();var cold=new ZoneKit.Shape();
                hot.Slot(0).Box(c+Vector3.left*3,new Vector3(9,.12f,.35f),Quaternion.identity,new Color(1,.95f,.8f));hot.Slot(0).Box(c+Vector3.left*7.6f,new Vector3(1.6f,.12f,.6f),Quaternion.identity,new Color(1,.86f,.5f));
                cold.Slot(0).Box(c+Vector3.left*3,new Vector3(9,.1f,.4f),Quaternion.identity,new Color(.08f,.07f,.07f));
                var cutting=kit.Built("The cut at join 1",hot,Glow);cutting.transform.SetParent(root,true);
                var sparks=kit.Drift("Cut sparks",c+Vector3.left*7.6f+Vector3.up*.6f,new Vector3(1.2f,.6f,1.2f),kit.Glow("Sparks",false,new Color(2,1.5f,.8f,1)),new Color(1,.9f,.6f,.9f),60,.05f,.14f,2.4f,.7f);sparks.transform.SetParent(cutting.transform,true);
                var scar=kit.Built("The cut at join 1 cold",cold,Trim);scar.transform.SetParent(root,true);
                var hotGate=new GameObject("The cut glows (until the frame is beaten)",typeof(FlagGate)).GetComponent<FlagGate>();hotGate.flag="bossdown.Hulljack";hotGate.showWhenSet=false;hotGate.targets=new[]{cutting};
                var coldGate=new GameObject("The cut cold (once the frame is beaten)",typeof(FlagGate)).GetComponent<FlagGate>();coldGate.flag="bossdown.Hulljack";coldGate.targets=new[]{scar};scar.SetActive(false);
            }
            /// <summary>One piece of cover at the plane, drawn down to the hull it stands on: a mast (lattice, a dish, a lamp on top, 3.5 m over the plane), a crane's boom (a pedestal from
            /// the roof, a cab, the boom at the ships' height, a hook hanging off its tip), or a cargo tower (a rack of containers up through the plane). Solid where a ship flies.</summary>
            static void CoverPiece(ZoneKit kit,Transform root,int join,string kindOf,Vector2 at,Vector2 to)
            {
                var s=new ZoneKit.Shape();var rng=new System.Random(500+join*10+at.GetHashCode()%7);float roof=RoofAt(at);var foot=P(at,roof);var go=new GameObject("Cover "+(join+1)+" "+kindOf);go.transform.SetParent(root,true);
                Color steel=new(.7f,.68f,.62f),dark=new(.26f,.26f,.28f);
                switch(kindOf)
                {
                    case "mast":
                    {
                        float top=Plane+3.5f;
                        for(int k=0;k<3;k++){float a=k*Mathf.PI*2/3;s.Slot(0).Tube(new[]{foot+new Vector3(Mathf.Cos(a)*.9f,0,Mathf.Sin(a)*.9f),new Vector3(at.x+Mathf.Cos(a)*.35f,top,at.y+Mathf.Sin(a)*.35f)},new[]{.1f,.07f},5,steel,steel,1,1,false);}
                        for(float yy=roof+1.2f;yy<top;yy+=1.4f)s.Slot(0).Tube(Enumerable.Range(0,4).Select(k=>{float a=k*Mathf.PI*2/3;float w=Mathf.Lerp(.9f,.35f,(yy-roof)/(top-roof));return new Vector3(at.x+Mathf.Cos(a)*w,yy,at.y+Mathf.Sin(a)*w);}).ToArray(),new[]{.05f},4,steel,steel,1,1,false);
                        s.Slot(0).Disc(new Vector3(at.x+.6f,top-1.4f,at.y),new Vector3(.7f,.4f,-.6f),1.1f,14,new Color(.86f,.86f,.84f));
                        s.Slot(1).Lump(new Vector3(at.x,top+.15f,at.y),Vector3.one*.22f,join,.05f,new Color(1,.3f,.24f),new Color(1,.2f,.16f),6,4);
                        ZoneKit.Block(go,new Vector3(at.x,(roof+top)*.5f,at.y),new Vector3(1.6f,top-roof,1.6f));
                        break;
                    }
                    case "boom":
                    {
                        var tip=P(to);var dir=(tip-P(at)).normalized;
                        s.Slot(0).Tube(new[]{foot,foot+Vector3.up*(Plane-roof-1.6f)},new[]{1.1f,.9f},10,new Color(.5f,.48f,.44f),steel,1,.3f).Box(P(at,Plane-.6f),new Vector3(3,2,3),Quaternion.LookRotation(dir),new Color(.78f,.74f,.64f),null,.5f);
                        // A box girder as thick as the ships' cover needs (1.3 m), its sides braced in a zig-zag of darker web, a walk rail along its top.
                        var side=Vector3.Cross(Vector3.up,dir);float run=Vector2.Distance(at,to);
                        s.Slot(0).Beam(P(at,Plane+.2f),tip+Vector3.up*.2f,1.3f,1.3f,new Color(.84f,.8f,.7f),new Color(.9f,.86f,.76f));
                        for(float t=.6f;t<run-.6f;t+=1.2f)foreach(float sd in new[]{-.67f,.67f}){var p=P(at,Plane+.2f)+dir*t+side*sd;s.Slot(0).Beam(p+Vector3.up*.55f,p+dir*.6f+Vector3.down*.55f,.12f,.08f,dark).Beam(p+dir*.6f+Vector3.down*.55f,p+dir*1.2f+Vector3.up*.55f,.12f,.08f,dark);}
                        foreach(float sd in new[]{-.5f,.5f})s.Slot(0).Beam(P(at,Plane+1.35f)+side*sd,tip+Vector3.up*1.35f+side*sd,.05f,.05f,dark);
                        s.Slot(0).Tube(new[]{tip,tip+Vector3.down*4.2f},new[]{.04f,.04f},4,dark,dark,1,1,false).Box(tip+Vector3.down*4.6f,new Vector3(.8f,.6f,.8f),Quaternion.identity,new Color(.9f,.9f,.86f));
                        s.Slot(1).Lump(tip+Vector3.up*.6f,Vector3.one*.2f,join,.05f,new Color(1,.3f,.24f),new Color(1,.2f,.16f),6,4);
                        ZoneKit.BlockAlong(go,P(at),tip,1.6f,3);ZoneKit.Block(go,new Vector3(at.x,(roof+Plane+.6f)*.5f,at.y),new Vector3(3,Plane+.6f-roof,3));
                        break;
                    }
                    default:
                    {
                        // A cargo tower: a rack of uprights holding containers four high, the top two at the ships' height.
                        float top=Plane+1.6f;foreach(var (dx,dz) in new[]{(-1.6f,-6.4f),(1.6f,-6.4f),(-1.6f,6.4f),(1.6f,6.4f)})s.Slot(0).Beam(foot+new Vector3(dx,0,dz),new Vector3(at.x+dx,top,at.y+dz),.3f,.3f,dark);
                        for(float yy=top-2.6f;yy>roof-2.4f;yy-=2.6f)Container(s,2,new Vector3(at.x,yy,at.y),12.2f,0,Paints[rng.Next(Paints.Length)]);
                        s.Slot(1).Lump(new Vector3(at.x,top+.25f,at.y+6.2f),Vector3.one*.2f,join,.05f,new Color(1,.3f,.24f),new Color(1,.2f,.16f),6,4);
                        ZoneKit.Block(go,new Vector3(at.x,(roof+top)*.5f,at.y),new Vector3(3.2f,top-roof,13));
                        break;
                    }
                }
                var drawn=kit.Built("Cover "+(join+1)+" "+kindOf,s.Slot(2),Trim,Glow,Crate);drawn.transform.SetParent(go.transform,true);
                // Blocks go under the drawn body (ZoneKit.Block), so the body is built first and the blocks moved under it.
                foreach(var box in go.GetComponents<BoxCollider>())UnityEngine.Object.DestroyImmediate(box);
                foreach(Transform child in go.transform.Cast<Transform>().ToArray())if(child.GetComponent<BoxCollider>()!=null&&child!=drawn.transform)child.SetParent(drawn.transform,true);
            }
            /// <summary>The roof's height under a plan point: the highest vessel whose plan holds it.</summary>
            internal static float RoofAt(Vector2 p)
            {
                float y=-14;
                foreach(var (plan,roof) in new[]{(SpinePlan,SpineY),(LinerPlan,LinerY),(GalleyPlan,GalleyY),(GardenPlan,GardenTop),(CabinPlan,CabinY),(RaftPlan,RaftRim),(HallPlan,HallY),(DockPlan,DockY),(ApronPlan,ApronY),(BowPlan,BowY)})
                    if(plan.Contains(p))y=Mathf.Max(y,roof);
                return y;
            }

            // ================================================================ round the ship
            /// <summary>Traffic on two loops round the ship under the plane: skiffs, cutters and haulers going about the convoy's business, never across the roofs.</summary>
            static void Traffic(ZoneKit kit)
            {
                var root=new GameObject("Traffic");string[] ships={"Skiff","PatrolCutter","Hauler","Skiff","PatrolCutter"};float[] lengths={7,6,10,7,6};
                for(int laneIndex=0;laneIndex<2;laneIndex++)
                {
                    var go=new GameObject("Harbour loop "+laneIndex,typeof(SplineContainer),typeof(TrafficLane));go.transform.SetParent(root.transform,true);var spline=new Spline();
                    float rx=110+laneIndex*8,rz=180+laneIndex*6,y=-3.5f-laneIndex*2.5f;
                    for(int i=0;i<24;i++){float a=i*Mathf.PI/12;spline.Add(new BezierKnot(new float3(Mathf.Sin(a)*rx,y,-6+Mathf.Cos(a)*rz)));}
                    spline.Closed=true;spline.SetTangentMode(TangentMode.AutoSmooth);go.GetComponent<SplineContainer>().Spline=spline;
                    var lane=go.GetComponent<TrafficLane>();lane.speed=5+laneIndex*1.5f;
                    for(int j=0;j<3;j++)
                    {
                        string key=ships[(j+laneIndex*2)%ships.Length];if(!ZoneKit.Landed(key))continue;
                        var ship=WorldBuilder.Piece(key,Vector3.zero,new Vector3(4,1.5f,lengths[(j+laneIndex*2)%lengths.Length]),"Rock",false);ship.isStatic=false;ship.transform.SetParent(root.transform,true);
                        var mover=ship.AddComponent<TrafficShip>();mover.lane=lane;mover.phase=j/3f+laneIndex*.17f;ship.transform.SetPositionAndRotation(lane.Position(mover.phase),Quaternion.LookRotation(lane.Tangent(mover.phase)));
                        foreach(var view in ship.GetComponentsInChildren<Renderer>())view.shadowCastingMode=ShadowCastingMode.Off;
                    }
                }
            }
            /// <summary>The settlements' ships holding off the port side in a line, waiting to be let in (Abri's queue): mismatched hulls with their running lights on.</summary>
            static void Queue(ZoneKit kit)
            {
                var root=new GameObject("The queue to come aboard");var rng=new System.Random(463);var lights=new ZoneKit.Shape();float R()=>(float)rng.NextDouble();
                // Settlements' ships of every make, nose to tail, and here and there a habitat pod towed up on a line behind a skiff: what the convoy is becoming a harbour for.
                string[] keys={"Hauler","Skiff","DockingPodOffice","PatrolCutter","Hauler","Skiff","DockingPodOutfitter","Hauler","PatrolCutter","Skiff","Hauler","DockingPodRepair"};
                for(int k=0;k<keys.Length;k++)
                {
                    var at=new Vector3(QueueX+R()*5-2.5f,-4.2f-R()*2.4f,-88+k*16+R()*3);string key=keys[k];bool pod=key.StartsWith("DockingPod");if(!ZoneKit.Landed(key))continue;
                    var ship=pod?kit.Loose(key,at,4.4f,Quaternion.Euler(R()*10-5,R()*360,R()*10-5)):kit.Prop(key,at,key=="Hauler"?15:key=="Skiff"?10:9,80+R()*20,false,false);
                    ship.name=pod?"Towed habitat":"Waiting ship";ship.transform.SetParent(root.transform,true);
                    if(!pod)ship.AddComponent<PropTint>().tint=Color.Lerp(Color.white,Paints[rng.Next(Paints.Length)],.4f);
                    foreach(var view in ship.GetComponentsInChildren<Renderer>())view.shadowCastingMode=ShadowCastingMode.Off;
                    var b=ZoneKit.Drawn(ship);
                    if(pod)lights.Slot(1).Rope(new Vector3(b.center.x,b.max.y,b.max.z),new Vector3(b.center.x+R()*2-1,b.center.y,b.max.z+5),.6f,.05f,new Color(.7f,.66f,.56f));
                    lights.Slot(0).Lump(new Vector3(b.min.x,b.center.y,b.center.z),Vector3.one*.24f,k,.05f,new Color(1,.3f,.24f),new Color(1,.2f,.2f),6,4).Lump(new Vector3(b.max.x,b.center.y,b.center.z),Vector3.one*.24f,k+19,.05f,new Color(.4f,1,.5f),new Color(.3f,.9f,.4f),6,4);
                }
                // The holding lane they wait in: small floating lamps every eight metres along its inner edge, pale blue, the harbour's own.
                for(float z=-94;z<104;z+=8){var at=new Vector3(QueueX+11,-3.2f,z);lights.Slot(1).Lump(at,new Vector3(.5f,.3f,.5f),(int)z,.05f,new Color(.4f,.42f,.46f),new Color(.3f,.3f,.32f),8,4);lights.Slot(0).Lump(at+Vector3.up*.35f,Vector3.one*.2f,(int)z+3,.05f,new Color(.55f,.85f,1f),new Color(.5f,.8f,1f),6,4);}
                var drawn=kit.Built("Queue running lights",lights.Slot(1),Glow,Trim);drawn.transform.SetParent(root.transform,true);
            }
            /// <summary>Hulljack's tug, standing off the stern: a squat heavy hull in his livery with its bridge, a drone rack, a tow boom and floodlights on the ship it has filed on.</summary>
            static void Tug(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var c=P(TugAt,-6);var rng=new System.Random(521);float R()=>(float)rng.NextDouble();
                Color body=new(.86f,.68f,.2f),dark=new(.16f,.16f,.17f),deck=new(.36f,.36f,.34f),steel=new(.56f,.57f,.6f);
                // A squat heavy hull in his yellow, flat-bottomed, its bow to the west where it faces the ship; a dark plated deck on it with his hazard band round its edge.
                s.Slot(0).Lump(c+Vector3.down*1.6f,new Vector3(25,4.2f,9.5f),9,.02f,body,ZoneKit.Shape.Dim(body,.45f),28,8,.18f);
                s.Slot(0).Box(c+new Vector3(0,2.2f,0),new Vector3(44,1.2f,16),Quaternion.identity,deck,ZoneKit.Shape.Dim(deck,1.12f),1/6f);
                for(int k=0;k<22;k++){foreach(float z in new[]{-8.06f,8.06f})s.Slot(1).Box(c+new Vector3(-21+k*2,2.5f,z),new Vector3(1,.6f,.1f),Quaternion.Euler(0,0,30),k%2==0?body:dark);}
                for(int k=0;k<8;k++){foreach(float x in new[]{-22.06f,22.06f})s.Slot(1).Box(c+new Vector3(x,2.5f,-7+k*2),new Vector3(.1f,.6f,1),Quaternion.Euler(30,0,0),k%2==0?body:dark);}
                Letters(s,1,"HULLJACK",c+new Vector3(-21,2.83f,-6.6f),3,body);
                // The bridge at the stern: two decks, a band of lit windows facing the ship and the camera, a mast with his floodlight and a dish.
                s.Slot(0).Box(c+new Vector3(15,3.9f,0),new Vector3(10,2.2f,10),Quaternion.identity,body,ZoneKit.Shape.Dim(body,1.1f),1/6f).Box(c+new Vector3(16,5.6f,.5f),new Vector3(6.5f,1.4f,7),Quaternion.identity,body,ZoneKit.Shape.Dim(body,1.1f),1/6f);
                for(int k=0;k<4;k++)Pane(s,3,c+new Vector3(11.4f+k*2.4f,4.1f,-5.04f),Vector3.right,1.7f,.9f,Vector3.back,new Color(.8f,.92f,1f));
                for(int k=0;k<3;k++)Pane(s,3,c+new Vector3(13.8f+k*2.1f,5.7f,-3.04f),Vector3.right,1.5f,.7f,Vector3.back,new Color(1,.86f,.6f));
                s.Slot(1).Post(c+new Vector3(18,6.3f,2),.9f,.12f,.1f,6,dark,dark).Disc(c+new Vector3(18.6f,6.8f,1.4f),new Vector3(-.5f,.6f,-.6f),.9f,14,new Color(.85f,.85f,.82f));
                // The drone rack amidships: an open lattice of bays, a claim drone docked in some, an orange tally lamp over each.
                for(int k=0;k<6;k++)
                {
                    var bay=c+new Vector3(-10+(k%3)*4.4f,3.4f,(k/3)*4.6f-2.3f);
                    foreach(float dx in new[]{-1.9f,1.9f})foreach(float dz in new[]{-1.9f,1.9f})s.Slot(1).Beam(bay+new Vector3(dx,-.6f,dz),bay+new Vector3(dx,1.3f,dz),.14f,.14f,steel);
                    s.Slot(1).Beam(bay+new Vector3(-1.9f,1.3f,-1.9f),bay+new Vector3(1.9f,1.3f,-1.9f),.14f,.14f,steel).Beam(bay+new Vector3(-1.9f,1.3f,1.9f),bay+new Vector3(1.9f,1.3f,1.9f),.14f,.14f,steel);
                    if(k%3!=1){var drone=bay+Vector3.up*.2f;s.Slot(0).Lump(drone,new Vector3(.9f,.5f,1.1f),k,.04f,new Color(.62f,.62f,.6f),dark,12,6).Lump(drone+Vector3.left*1,new Vector3(.35f,.3f,.6f),k+3,.04f,dark,dark,8,4).Lump(drone+Vector3.right*1,new Vector3(.35f,.3f,.6f),k+6,.04f,dark,dark,8,4);
                        s.Slot(3).Lump(drone+new Vector3(0,.15f,-1),new Vector3(.22f,.16f,.1f),k,.05f,new Color(1,.6f,.2f),new Color(1,.6f,.2f),6,3);}
                    s.Slot(3).Lump(bay+new Vector3(0,1.5f,-1.9f),new Vector3(.18f,.12f,.12f),k+20,.05f,new Color(1,.6f,.2f),new Color(1,.6f,.2f),6,3);
                }
                // Seized cargo lashed on the foredeck, each piece tagged.
                for(int k=0;k<3;k++){var at=c+new Vector3(-19+k*3.2f,3.1f+(k==1?2.6f:0),k==1?0:(k-1)*3.4f);s.Slot(4).Box(at,new Vector3(2.44f,2.6f,6.1f),Quaternion.Euler(0,90+(k-1)*4,0),Paints[(k*5+2)%Paints.Length],null,.42f);s.Slot(3).Box(at+new Vector3(0,.8f,-1.24f),new Vector3(.9f,.5f,.06f),Quaternion.identity,new Color(1,.55f,.18f));}
                // A tow winch and the tow boom out toward the ship's stern, a lattice girder under the plane, its cable and hook hanging, floodlights along it.
                var winch=c+new Vector3(4,3.4f,4);s.Slot(1).Tube(new[]{winch+Vector3.left*1.6f,winch+Vector3.right*1.6f},new[]{1.1f},14,dark,steel,1,.5f).Box(winch+Vector3.down*.6f,new Vector3(4,1,2.6f),Quaternion.identity,body);
                var boomFoot=c+new Vector3(-4,3.4f,7);var boomTip=boomFoot+new Vector3(-18,2.2f,20);var dir=(boomTip-boomFoot).normalized;var side=Vector3.Cross(Vector3.up,dir).normalized;
                foreach(float sd in new[]{-.5f,.5f}){s.Slot(0).Beam(boomFoot+side*sd+Vector3.up*.5f,boomTip+side*sd+Vector3.up*.25f,.18f,.18f,body).Beam(boomFoot+side*sd-Vector3.up*.5f,boomTip+side*sd-Vector3.up*.25f,.18f,.18f,body);}
                float run=Vector3.Distance(boomFoot,boomTip);for(float t=0;t<run-1;t+=1.4f){var p=boomFoot+dir*t;foreach(float sd in new[]{-.5f,.5f})s.Slot(1).Beam(p+side*sd+Vector3.up*.45f,p+dir*.7f+side*sd-Vector3.up*.45f,.07f,.07f,dark);}
                s.Slot(1).Tube(new[]{boomTip,boomTip+Vector3.down*6},new[]{.06f,.06f},4,dark,dark,1,1,false).Box(boomTip+Vector3.down*6.4f,new Vector3(1,.8f,1),Quaternion.identity,body);
                foreach(var at in new[]{c+new Vector3(18,7.4f,2),boomTip+Vector3.up*.6f,Vector3.Lerp(boomFoot,boomTip,.5f)+Vector3.up*.7f})s.Slot(3).Lump(at,new Vector3(.5f,.34f,.5f),3,.05f,new Color(1,.96f,.86f),new Color(1,.9f,.7f),8,5);
                var tug=kit.Built("Hulljack's tug",s.Slot(4),Heavy,Trim,Trim,Glow,Crate);
                var flood=new GameObject("Tug floodlight",typeof(Light));flood.transform.SetParent(tug.transform,true);flood.transform.position=boomTip+Vector3.down*2;
                var l=flood.GetComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.95f,.85f);l.range=24;l.intensity=2;l.shadows=LightShadows.None;
                WorldBuilder.Label("HULLJACK SALVAGE",c+new Vector3(-4,3,-12),5);
            }
            /// <summary>What stands off the ship inside the harbour line, so no stretch of it is bare sky: to starboard a cargo park (containers and pods tethered off the galley and the
            /// garden on long lines, waiting to be taken in), off the stern dock a line of approach lights out to the harbour line, off the bow a chain of Pell's lamps on floats.
            /// All of it under the plane and drawn only.</summary>
            static void Margins(ZoneKit kit)
            {
                var root=new GameObject("Off the ship");var s=new ZoneKit.Shape();var rng=new System.Random(487);float R()=>(float)rng.NextDouble();
                // The cargo park: in rows off the starboard flank, each tethered back to a bollard on the hull, drifting a little out of line.
                for(int k=0;k<22;k++)
                {
                    var at=new Vector3(112+(k%3)*11+R()*4,-5-R()*4,-70+(k/3)*24+R()*8);var yaw=R()*60-30;var turn=Quaternion.Euler(R()*14-7,yaw,R()*14-7);
                    if(k%4==3)s.Slot(0).Lump(at,new Vector3(2.2f,1.8f,3.4f),k,.04f,Paints[rng.Next(Paints.Length)],new Color(.3f,.3f,.32f),14,8,.2f,turn);
                    else{float length=rng.Next(3)==0?6.1f:12.2f;s.Slot(2).Box(at,new Vector3(2.44f,2.6f,length),turn,Paints[rng.Next(Paints.Length)],null,.42f);if(rng.Next(2)==0)s.Slot(2).Box(at+turn*Vector3.up*2.6f,new Vector3(2.44f,2.6f,length*(rng.Next(2)==0?1:.5f)),turn,Paints[rng.Next(Paints.Length)],null,.42f);}
                    var hull=new Vector3(GalleyPlan.xMax-.5f,GalleyY-1,Mathf.Clamp(at.z,GalleyPlan.yMin+2,GardenPlan.yMax-2));if(at.z>GalleyPlan.yMax)hull.x=GardenPlan.xMax-.5f;
                    s.Slot(1).Rope(at+turn*Vector3.back*1.4f,hull,2.4f+R()*2,.06f,new Color(.74f,.7f,.6f));
                    if(k%5==0)s.Slot(3).Lump(at+turn*Vector3.up*1.6f,Vector3.one*.18f,k,.05f,new Color(1,.86f,.5f),new Color(1,.7f,.4f),6,4);
                }
                // Approach lights off the stern dock, pairs of floating pads stepping out to the harbour line, lit pale blue.
                for(int k=1;k<=4;k++){var at=new Vector3(DockAt.x,-2.5f,DockPlan.yMin-k*12);foreach(float side in new[]{-5f,5f}){var p=at+Vector3.right*side;s.Slot(1).Disc(p,Vector3.up,1.2f,12,new Color(.3f,.32f,.36f)).Box(p+Vector3.down*.4f,new Vector3(1.6f,.6f,1.6f),Quaternion.identity,new Color(.36f,.38f,.42f));s.Slot(3).Disc(p+Vector3.up*.02f,Vector3.up,.7f,12,new Color(.55f,.85f,1f));}}
                // The lamp chain off the bow: Pell's lamps on floats strung along a line out toward the harbour line, warm.
                for(int k=1;k<=3;k++){var at=new Vector3((k%2)*4-2,-2.2f,BowPlan.yMax+k*10);s.Slot(0).Lump(at,new Vector3(1.2f,.8f,1.2f),k,.05f,new Color(.62f,.5f,.34f),new Color(.3f,.24f,.18f),10,6).Post(at+Vector3.up*.6f,1.6f,.08f,.06f,5,new Color(.3f,.24f,.18f),new Color(.4f,.32f,.22f));
                    s.Slot(3).Lump(at+Vector3.up*2.4f,new Vector3(.34f,.44f,.34f),k,.05f,new Color(1,.78f,.44f),new Color(1,.6f,.3f),8,5);if(k>1)s.Slot(1).Rope(at+Vector3.up*1.9f,new Vector3(((k-1)%2)*4-2,-.3f,BowPlan.yMax+(k-1)*10),.8f,.05f,new Color(.7f,.62f,.46f));}
                var drawn=kit.Built("Off the ship",s.Slot(3),Liner,Trim,Crate,Glow);drawn.transform.SetParent(root.transform,true);
            }
            /// <summary>The sky under the ship: soft bands of a slate and amber nebula far down, so the space past the hull's edge has depth, and a thin drift of dust in the lane.</summary>
            static void Backdrop(ZoneKit kit)
            {
                var soft=kit.Glow("Band",true,new Color(1,1,1,1));var bands=new GameObject("Nebula bands");
                foreach(var (x,z,deep,wide,span,tone) in new[]{(-150f,-60f,-90f,120f,420f,new Color(.5f,.6f,.85f,.34f)),(160f,40f,-100f,130f,440f,new Color(.85f,.66f,.45f,.28f)),(20f,-210f,-80f,140f,300f,new Color(.55f,.6f,.9f,.3f)),(0f,210f,-95f,120f,300f,new Color(.8f,.6f,.5f,.26f))})
                {
                    var from=new Vector3(x,deep,z-span*.5f);var to=new Vector3(x,deep,z+span*.5f);
                    var band=ZoneKit.Strand(bands.transform,from,to,soft,wide,tone,new Color(tone.r,tone.g,tone.b,0));band.alignment=LineAlignment.TransformZ;band.transform.rotation=Quaternion.Euler(90,0,0);
                    band.SetPosition(0,band.transform.InverseTransformPoint(from));band.SetPosition(1,band.transform.InverseTransformPoint(to));
                }
                var dust=kit.Drift("Harbour dust",new Vector3(0,-1,0),new Vector3(XMax-XMin,10,ZMax-ZMin),kit.Glow(),new Color(.86f,.9f,1f,.18f),900,.06f,.2f,.1f,20);_=dust;
            }
            /// <summary>The harbour line: lamp buoys round the ship at the limits, a line of light strung between each and the next at the ships' height, so a ship stops at a line it can see.</summary>
            static void HarbourLine(ZoneKit kit)
            {
                var s=new ZoneKit.Shape();var corners=new[]{new Vector2(XMin,ZMin),new Vector2(XMax,ZMin),new Vector2(XMax,ZMax),new Vector2(XMin,ZMax)};var buoys=new List<Vector2>();
                for(int k=0;k<4;k++){var a=corners[k];var b=corners[(k+1)%4];int n=Mathf.CeilToInt(Vector2.Distance(a,b)/22);for(int i=0;i<n;i++)buoys.Add(Vector2.Lerp(a,b,i/(float)n));}
                for(int k=0;k<buoys.Count;k++)
                {
                    var at=P(buoys[k],-1.4f);var next=P(buoys[(k+1)%buoys.Count],Plane);
                    // A marker buoy: a float drum, white with a red band, a cage post up from it to the lamp.
                    s.Slot(0).Post(at+Vector3.down*.6f,1.4f,.85f,.85f,14,new Color(.86f,.86f,.84f),new Color(.92f,.92f,.9f)).Post(at+Vector3.down*.05f,.34f,.88f,.88f,14,new Color(.8f,.22f,.18f),new Color(.8f,.22f,.18f)).Disc(at+Vector3.up*.8f,Vector3.up,.85f,14,new Color(.7f,.7f,.68f));
                    foreach(int k2 in new[]{0,1,2}){float a=k2*Mathf.PI*2/3;s.Slot(0).Beam(at+new Vector3(Mathf.Cos(a)*.6f,.8f,Mathf.Sin(a)*.6f),P(buoys[k],Plane+1.1f)+new Vector3(Mathf.Cos(a)*.15f,0,Mathf.Sin(a)*.15f),.08f,.08f,new Color(.4f,.4f,.42f));}
                    s.Slot(1).Lump(P(buoys[k],Plane+1.4f),Vector3.one*.36f,k,.05f,new Color(1,.86f,.6f),new Color(1,.72f,.44f),8,5);
                    s.Slot(1).Rope(P(buoys[k],Plane+.15f),next+Vector3.up*.15f,.3f,.08f,new Color(1,.8f,.52f),4,11);
                }
                kit.Built("Harbour line",s.Slot(1),Trim,Glow);
            }
        }
    }
}
