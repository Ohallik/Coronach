using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>The Spore Veil (space combat; rule: visibility is the resource). Map brief and evidence: docs/maps/SporeVeil.md.</summary>
    public static partial class WendmireBuilder
    {
        // ---------------------------------------------------------------- the lane
        /// <summary>From the arrival north through the Veil to the tide-gate over the mire, about 1.6 km, bending west, east and back. Everything in the zone is placed by how far along the
        /// lane it lies and how far to starboard (<see cref="VeilBeside"/>), so the lane can be redrawn without touching what stands on it.</summary>
        internal static readonly Vector2[] VeilLane={new(0,0),new(-40,260),new(30,560),new(150,820),new(140,1080),new(60,1300),new(40,1560),new(36,1620)};
        internal const float VeilHalf=72;
        /// <summary>The four beacons' places along the lane (the fourth is the dead one), the tide-gate, and where a ship back from town arrives.</summary>
        internal const float Beacon1=40,Beacon2=520,Beacon3=1000,DeadBeaconS=1380,GateS=1560,FromTownS=1530;
        /// <summary>The two off-lane pockets (along, to starboard): a slipped beacon and a cache in each.</summary>
        internal static readonly (float s,float side)[] Pockets={(300,-56),(1150,58)};
        /// <summary>The swarms hanging in leg two's field, on show and asleep (along, to starboard).</summary>
        /// <remarks>Three rows of three across the lane, 145 m apart. In a row the swarms stand 48 m apart and the outermost 20 to 28 m in from the lane's edge, so no line across a row
        /// passes further than 28 m from a swarm (a lit ship's light carries 30) and none comes nearer than 18 m to a cloud between them (a dark ship's gap is 36 m of open lane).
        /// Six swarms down the lane's middle let a ship flying the edges lit go by out of reach of every one.</remarks>
        internal static readonly (float s,float side)[] Field={(615,-46),(611,2),(619,50),(760,-52),(756,-4),(764,44),(905,-44),(901,4),(909,52)};
        /// <summary>Leg one's spore pods: the stretches they hang across.</summary>
        internal static readonly float[] PodBanks={150,260,400};
        static float VeilLength{get{float total=0;for(int i=1;i<VeilLane.Length;i++)total+=Vector2.Distance(VeilLane[i-1],VeilLane[i]);return total;}}
        internal static Vector2 VeilAlong(float s,out Vector2 heading)
        {
            heading=Vector2.up;
            for(int i=1;i<VeilLane.Length;i++)
            {
                float leg=Vector2.Distance(VeilLane[i-1],VeilLane[i]);heading=(VeilLane[i]-VeilLane[i-1])/leg;
                if(s<=leg||i==VeilLane.Length-1)return VeilLane[i-1]+heading*s;
                s-=leg;
            }
            return VeilLane[0];
        }
        /// <summary>A point beside the lane: so far along, so far to starboard (port is negative), at the flight plane unless told.</summary>
        internal static Vector3 VeilBeside(float s,float starboard,float y=ZoneKit.FlightPlane){var p=VeilAlong(s,out var h)+new Vector2(h.y,-h.x)*starboard;return new Vector3(p.x,y,p.y);}
        static float VeilHeading(float s){VeilAlong(s,out var h);return Mathf.Atan2(h.x,h.y)*Mathf.Rad2Deg;}
        static Vector2 Flat(Vector3 p)=>new(p.x,p.z);
        /// <summary>Where to stand a thing <paramref name="depth"/> metres under the plane so that it shows on screen at this point of the plane: the flight camera looks down at 48 degrees,
        /// so a thing below the lane shows south of where it is (ZoneKit.ShowsAt).</summary>
        static Vector3 ShowingAt(Vector3 onPlane,float depth)=>new(onPlane.x,ZoneKit.FlightPlane-depth,onPlane.z+depth/Mathf.Tan(ZoneKit.FlightPitch*Mathf.Deg2Rad));
        /// <summary>An enemy of the chapter's roster if the combat engineer has defined it, else a stand-in that fights the same way, so the lane is flyable before the roster lands.</summary>
        static string Foe(string id,string standIn)=>Find<EnemyDef>(id)!=null?id:standIn;

        /// <summary>What must stay readable under the lane, each with the room it needs: ways in and out, the beacons, the pockets, the gate.</summary>
        static List<(Vector2 at,float clear)> VeilKeep()
        {
            // The arrival's room is a hull's width: kept clearer, the first frame of the zone was half bare sky.
            var keep=new List<(Vector2,float)>{(new Vector2(-14,-4),6),(new Vector2(0,6),3.5f),(Flat(VeilBeside(GateS,0)),16),(Flat(VeilBeside(DeadBeaconS,-12)),12)};
            foreach(float s in new[]{Beacon1,Beacon2,Beacon3})keep.Add((Flat(VeilBeside(s,-12)),9));
            foreach(var (s,side) in Pockets)keep.Add((Flat(VeilBeside(s,side)),11));
            return keep;
        }

        // ---------------------------------------------------------------- what the Veil is made of
        internal static Material Pod,Spot,Lace,Strand,MireWater,MireEdge,ReedClump,MoonPale,MoonViolet,MoonGreen,BeaconGlow;
        static void VeilMaterials()
        {
            // A spore pod's skin: soft ribs from stem to tip (the lump's u runs round it), a fine wandering vein between each pair, and a faint sheen down the middle of each rib.
            // The first pass painted a crazed network of veins over it, and every pod read as a grey rock.
            var pod=ZoneKit.Texture("Wendmire/wend-pod",512,(u,v)=>
            {
                float rib=Mathf.Abs(Mathf.Sin(u*Mathf.PI*10)),wander=ZoneKit.Cloud(u,v,6,31)*.6f,vein=Mathf.Abs(Mathf.Sin((u*10+wander)*Mathf.PI*2));
                float tone=.62f+.26f*Mathf.Pow(rib,.6f)-(vein<.05f?.1f:0)+.06f*(ZoneKit.Cloud(u,v,12,4)-.5f);
                return new Color(tone,tone,tone,1);
            });
            // Lace: the flat colonies the spores grow into, a web of cells with dark holes.
            var lace=ZoneKit.Texture("Wendmire/wend-lace",512,(u,v)=>
            {
                float best=9,second=9;
                for(int k=0;k<22;k++){float cx=ZoneKit.Tile(k*3.1f,.5f,64,7),cy=ZoneKit.Tile(.5f,k*2.3f,64,8);
                    for(int ox=-1;ox<=1;ox++)for(int oy=-1;oy<=1;oy++){float d=Mathf.Sqrt((u-cx-ox)*(u-cx-ox)+(v-cy-oy)*(v-cy-oy));if(d<best){second=best;best=d;}else if(d<second)second=d;}}
                float wall=second-best,tone=wall<.012f?.9f:wall<.03f?.7f:.36f+.12f*ZoneKit.Cloud(u,v,12,3);
                return new Color(tone,tone,tone,1);
            });
            // A strand's skin: long fibres, twisted.
            var fibre=ZoneKit.Texture("Wendmire/wend-fibre",256,(u,v)=>
            {
                float twist=Mathf.Abs(Mathf.Sin((u*6+v*2)*Mathf.PI)),grain=ZoneKit.Tile(u*40,v*6,40,5);
                float tone=.5f+.32f*Mathf.Pow(twist,.6f)+.12f*(grain-.5f);return new Color(tone,tone,tone,1);
            });
            // A moon's face, for the half of it the camera sees: the sheet is one face, half the globe round and pole to pole (see Moons), so all of it is spent where it shows.
            // Highlands of fine broken grain; seas with hard, wandering shores, mottled inside and crossed by low wrinkle ridges; craters at every size from a few texels to a
            // tenth of the face, each a bowl shaded from one side with a hard lit lip and a pale apron (the big ones terraced, with a central peak; the young ones throwing long
            // bright rays). The first face had forty craters and soft seas, and at the size the arrival shows the pale moon it read as a low ball with flat blotches.
            Texture2D Moon(string file,int seed)
            {
                const int N=1024;var tone=new float[N*N];var rng=new System.Random(seed);
                float Wrap(float d)=>d-Mathf.Round(d);
                for(int y=0;y<N;y++)for(int x=0;x<N;x++)
                {
                    float u=(x+.5f)/N,v=(y+.5f)/N;
                    float wu=u+.07f*(ZoneKit.Cloud(u,v,5,seed+1)-.5f),wv=v+.07f*(ZoneKit.Cloud(u,v,5,seed+2)-.5f);
                    float sea=ZoneKit.Smooth(.5f,.535f,ZoneKit.Cloud(wu,wv,3,seed));
                    float high=.76f+.1f*(ZoneKit.Cloud(u,v,12,seed+3)-.5f)+.08f*(ZoneKit.Cloud(u,v,48,seed+4)-.5f)+.06f*(ZoneKit.Tile(u*256,v*256,256,seed+5)-.5f);
                    float ridge=1-Mathf.Abs(2*ZoneKit.Cloud(u,v,14,seed+8)-1);
                    float mare=.44f+.06f*(ZoneKit.Cloud(u,v,20,seed+6)-.5f)+.04f*(ZoneKit.Cloud(u,v,80,seed+7)-.5f)+.07f*Mathf.Pow(ridge,14);
                    tone[y*N+x]=Mathf.Lerp(high,mare,sea);
                }
                bool Sea(float u,float v){float wu=u+.07f*(ZoneKit.Cloud(u,v,5,seed+1)-.5f),wv=v+.07f*(ZoneKit.Cloud(u,v,5,seed+2)-.5f);return ZoneKit.Cloud(wu,wv,3,seed)>.52f;}
                // Light from the sheet's upper left, as a bowl reads best: its near wall dark, its far wall and lip lit.
                const float Lx=-.7071f,Ly=.7071f;
                void Crater(float cx,float cy,float r,float strength,bool young,bool rays)
                {
                    float apron=young?2.4f:1.7f,reach=rays?14:apron;int x0=Mathf.FloorToInt((cx-r*reach)*N),x1=Mathf.CeilToInt((cx+r*reach)*N),y0=Mathf.Max(0,Mathf.FloorToInt((cy-r*reach)*N)),y1=Mathf.Min(N-1,Mathf.CeilToInt((cy+r*reach)*N));
                    int raySeed=rng.Next(1000);
                    for(int py=y0;py<=y1;py++)for(int px=x0;px<=x1;px++)
                    {
                        int wx=((px%N)+N)%N;float dx=Wrap((wx+.5f)/N-cx),dy=(py+.5f)/N-cy,d=Mathf.Sqrt(dx*dx+dy*dy)/r;if(d>reach)continue;
                        float facing=d>.001f?(dx*Lx+dy*Ly)/(d*r):0,add=0;
                        if(d<.94f)
                        {
                            add=-.03f-.05f*(1-d*d)-.24f*facing*ZoneKit.Smooth(.2f,.92f,d);
                            if(r>.022f&&d<.2f)add+=.16f*(1-d/.2f);
                            if(r>.032f&&d>.5f)add+=.035f*Mathf.Sin(d*Mathf.PI*7);
                        }
                        else if(d<1.12f)add=.11f+.06f*facing;
                        else if(d<apron)add=(young?.1f:.055f)*Mathf.Pow(1-(d-1.12f)/(apron-1.12f),2);
                        if(rays&&d>1.2f)
                        {
                            float around=Mathf.Atan2(dy,dx)/(Mathf.PI*2)+.5f,streak=ZoneKit.Smooth(.6f,.86f,ZoneKit.Tile(around*48,raySeed,48,raySeed));
                            add+=.13f*streak*Mathf.Pow(1-(d-1.2f)/(reach-1.2f),1.5f);
                        }
                        tone[py*N+wx]+=add*strength;
                    }
                }
                // Many small and few large, as a real field falls off: the count above a size goes as one over its square, from two and a half texels to a fifteenth of the face (about
                // thirty craters over twenty texels across, a handful over forty). Drawn evenly between the sizes, the face was all big bowls and read as foam. A sea floods its old
                // craters to ghosts. A bowl is only a little darker for its depth: its walls, one lit and one in shadow, and its lip carry it.
                for(int k=0;k<3200;k++)
                {
                    float r=Mathf.Min(.065f,.0025f/Mathf.Sqrt(Mathf.Max(.0015f,(float)rng.NextDouble()))),cx=(float)rng.NextDouble(),cy=.03f+.94f*(float)rng.NextDouble();bool young=rng.Next(8)==0;
                    float strength=Sea(cx,cy)?(young?.8f:.35f):1;Crater(cx,cy,r,strength,young,false);
                }
                for(int k=0;k<3;k++)Crater((float)rng.NextDouble(),.2f+.6f*(float)rng.NextDouble(),.018f+.014f*(float)rng.NextDouble(),1,true,true);
                return ZoneKit.Texture(file,N,(u,v)=>
                {
                    int x=Mathf.Min(N-1,(int)(u*N)),y=Mathf.Min(N-1,(int)(v*N));float t=tone[y*N+x]+.03f*(ZoneKit.Tile(u*512,v*512,512,seed+9)-.5f);
                    return new Color(t,t,t,1);
                });
            }
            var moonA=Moon("Wendmire/wend-moon-a",11);var moonB=Moon("Wendmire/wend-moon-b",23);var moonC=Moon("Wendmire/wend-moon-c",37);
            // The mire from 35 m up: still dark water, long slow swells, and the sky's light lying on it in broad soft patches. Laid one repeat to 30 m: at one to 5 m its scum read as
            // a stamped wallpaper.
            var water=ZoneKit.Texture("Wendmire/wend-mire-water",512,(u,v)=>
            {
                float swell=Mathf.Sin((v*6+ZoneKit.Cloud(u,v,3,5)*2.4f)*Mathf.PI*2)*.5f+.5f,sheen=ZoneKit.Smooth(.45f,.8f,ZoneKit.Cloud(u,v,3,17));
                float tone=.34f+.08f*swell+.16f*sheen+.04f*(ZoneKit.Cloud(u,v,24,2)-.5f);return new Color(tone*.9f,tone*1.04f,tone,1);
            });
            // The pods glow a little from inside (the Veil is bioluminescent), so their own colour holds on the side away from the star: unlit, they went grey.
            Pod=ZoneKit.Toon("wend-pod",pod,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.6f,.5f,.78f));m.SetFloat("_LightFloor",.42f);m.SetColor("_RimColor",new Color(.6f,1f,.75f));
                m.SetTexture("_EmissionMap",pod);m.SetColor("_EmissionColor",new Color(.16f,.13f,.2f));});
            // A pod's lit tip: its own light, green, so the drift reads as alive on the dark side of the Veil. Not white: bloomed, a pale spot read as snow.
            Spot=ZoneKit.Toon("wend-spot",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.55f,1,.66f));m.SetColor("_EmissionColor",new Color(.36f,1.1f,.56f));});
            // Lit a little from inside, as the pods are, and its cells less deep: with dark holes the stacked shelves read as cracked stone.
            Lace=ZoneKit.Toon("wend-lace",lace,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetTexture("_EmissionMap",lace);m.SetColor("_EmissionColor",new Color(.14f,.24f,.17f));m.SetFloat("_LightFloor",.36f);});
            Strand=ZoneKit.Toon("wend-strand",fibre,m=>{ZoneKit.Tinted(m);m.SetFloat("_LightFloor",.3f);m.SetColor("_EmissionColor",new Color(.04f,.03f,.07f));});
            // A moon lights itself: under the toon ramp a globe went to two flat bands with a hard line between them (Xylos). A detail layer keeps the face crisp nearer in.
            Material MoonOf(string name,Texture2D face,Color tone)=>ZoneKit.Toon(name,face,m=>{m.SetColor("_BaseColor",tone*.62f);m.SetTexture("_EmissionMap",face);m.SetColor("_EmissionColor",tone*.36f);m.SetFloat("_ReceiveShadows",0);
                m.SetFloat("_RampSteps",6);m.SetColor("_RimColor",Color.black);ZoneKit.With(m,"_DETAIL_ON","_Detail");m.SetColor("_DetailTintA",new Color(.8f,.8f,.84f));m.SetColor("_DetailTintB",new Color(1.15f,1.12f,1.1f));
                m.SetVector("_DetailParams",new Vector4(.05f,.9f,.2f,.35f));});
            MoonPale=MoonOf("wend-moon-pale",moonA,new Color(.86f,.84f,.92f));MoonViolet=MoonOf("wend-moon-violet",moonB,new Color(.74f,.62f,.9f));MoonGreen=MoonOf("wend-moon-green",moonC,new Color(.66f,.84f,.72f));
            MireWater=ZoneKit.Toon("wend-mire-water",water,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.36f,.5f,.48f));m.SetColor("_EmissionColor",new Color(.03f,.06f,.06f));m.SetFloat("_RampSteps",4);});
            // The same water where the mire thins out of the Veil: its vertices' alpha takes it from whole to nothing, drawn over the starfield after everything solid.
            MireEdge=ZoneKit.Toon("wend-mire-edge",water,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.36f,.5f,.48f));m.SetColor("_EmissionColor",new Color(.03f,.06f,.06f));m.SetFloat("_RampSteps",4);
                m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.SetOverrideTag("RenderType","Transparent");m.renderQueue=(int)RenderQueue.Transparent-30;});
            ReedClump=ZoneKit.Toon("wend-reed-clump",null,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetFloat("_LightFloor",.3f);});
            BeaconGlow=ZoneKit.Toon("wend-beacon-glow",null,m=>{m.SetColor("_BaseColor",new Color(1,.8f,.46f));m.SetColor("_EmissionColor",new Color(2.4f,1.6f,.6f));});
        }

        static void SporeVeil()
        {
            var kit=ZoneKit.Flight(Veil);
            var scene=WorldBuilder.Begin(kit.zone);
            float length=VeilLength;var keep=VeilKeep();
            WorldBuilder.Spawn("Arrival",new Vector3(0,1,6));WorldBuilder.Spawn(Town,VeilBeside(FromTownS,4));
            kit.DriftingSky();

            // ---- what the lane flies over: three moons far below, the nebula's bands, the drift of spores, and at the end the mire itself
            Moons(kit);
            // Soft across its width on both sides (the curtain texture rose from a hard edge, which crossed the sky as a straight line), and faded along its length below.
            // Inside a band: long threads of glow running its length, finer ones between, a soft body behind them, and dust lanes where it thins nearly to nothing, all wandering
            // across it together. A plain soft band read as a smooth two-colour wash. Its brightest is no brighter than the plain one was, so nothing in the lane is harder to read.
            var across=ZoneKit.Texture("Wendmire/wend-band",512,(u,v)=>
            {
                float edge=Mathf.Pow(Mathf.Sin(v*Mathf.PI),1.6f),w=v+.2f*(ZoneKit.Cloud(u,v,3,61)-.5f);
                float threads=1-Mathf.Abs(2*ZoneKit.Tile(u*5,w*20,20,63)-1),fine=1-Mathf.Abs(2*ZoneKit.Tile(u*9,w*44,44,67)-1),body=ZoneKit.Cloud(u,v,4,71);
                float lane=ZoneKit.Smooth(.54f,.68f,ZoneKit.Tile(u*3.2f,w*9,9,73));
                float glow=.36f*body+.5f*Mathf.Pow(threads,4)+.3f*Mathf.Pow(fine,8);
                return new Color(1,1,1,Mathf.Clamp01(edge*glow*(1-.82f*lane)*1.55f));
            },false,true);
            var soft=kit.Glow("Band",false,new Color(1,1,1,1));soft.SetTexture("_BaseMap",across);var bands=new GameObject("Nebula bands");
            foreach(var (s,side,deep,wide,span,tone) in new[]{(60f,-34f,-70f,110f,320f,new Color(.55f,.95f,.62f,.42f)),(240f,40f,-90f,130f,360f,new Color(.7f,.5f,.95f,.36f)),(520f,-30f,-80f,120f,340f,new Color(.45f,.9f,.75f,.4f)),
                (760f,36f,-100f,140f,360f,new Color(.75f,.55f,1f,.34f)),(1000f,-40f,-85f,120f,340f,new Color(.55f,.95f,.6f,.38f)),(1240f,30f,-95f,130f,360f,new Color(.7f,.5f,.95f,.34f))})
            {
                var from=VeilBeside(s-span*.5f,side,deep);var to=VeilBeside(s+span*.5f,side,deep);
                // Faded in at both ends, peaking in the middle: a band that started at full strength cut a straight edge across the sky where it began.
                var band=ZoneKit.Strand(bands.transform,from,to,soft,wide,tone,tone);var fade=new Gradient();
                // A third hue through its middle: the green bands go teal there, the violet ones rose.
                var middle=tone.g>tone.b?new Color(.36f,.84f,.96f):new Color(.96f,.52f,.8f);
                fade.SetKeys(new[]{new GradientColorKey(tone,0),new GradientColorKey(middle,.5f),new GradientColorKey(tone,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(tone.a,.35f),new GradientAlphaKey(tone.a,.65f),new GradientAlphaKey(0,1)});band.colorGradient=fade;band.alignment=LineAlignment.TransformZ;band.transform.rotation=Quaternion.Euler(90,0,0);
                band.positionCount=9;for(int k=0;k<9;k++)band.SetPosition(k,band.transform.InverseTransformPoint(Vector3.Lerp(from,to,k/8f)));
            }
            SporeDrift(kit,keep,length);
            Mire(kit,keep);
            var glow=kit.Glow("Spores",false,new Color(1.1f,1.6f,1.2f,1));
            for(float s=0;s<length;s+=240)
            {
                // Spores in the light, a box to each stretch of the lane: one box over a bent lane would spend most of its motes where nobody flies.
                var dust=kit.Drift("Drifting spores",VeilBeside(s+120,0,-2.5f),new Vector3(VeilHalf*2+10,14,260),glow,new Color(.7f,1,.78f,.32f),520,.06f,.22f,.16f,18);
                dust.transform.rotation=Quaternion.Euler(0,VeilHeading(s+120),0);
            }

            // ---- ways in and out
            WorldBuilder.Warp("Warp — Tallow Approach",new Vector3(-14,1,-4),"TallowApproach","","Wendmire").lockedPrompt="";
            var gateAt=VeilBeside(GateS,0);
            var land=WorldBuilder.Warp("Land — Driftreed",gateAt,Town,"wend.beaconRelit","Arrival");
            land.lockedPrompt="The tide-gate stays shut while a beacon in the lane is dark: relight it";land.range=WorldBuilder.FlightDockRange;
            TideGate(kit,gateAt);

            // ---- the beacons: three lit, one dead, two off the lane keeping the wrong time
            Beacon(kit,"Beacon 1",VeilBeside(Beacon1,-12,-4.2f),LampRhythm.Count.TwoShortOneLong,null,null,0);
            Beacon(kit,"Beacon 2",VeilBeside(Beacon2,-12,-4.2f),LampRhythm.Count.TwoShortOneLong,null,null,.7f);
            Beacon(kit,"Beacon 3",VeilBeside(Beacon3,-12,-4.2f),LampRhythm.Count.TwoShortOneLong,null,null,1.3f);
            Beacon(kit,"The dead beacon",VeilBeside(DeadBeaconS,-12,-4.2f),LampRhythm.Count.TwoShortOneLong,"wend.beaconRelit",null,0);
            for(int n=0;n<Pockets.Length;n++)Beacon(kit,"Off-lane beacon "+(n+1),VeilBeside(Pockets[n].s,Pockets[n].side,-4.2f),LampRhythm.Count.ShortLongShort,null,"wend.offBeacon"+(n+1),n*.9f);
            DiscoveryPoint Line(string name,float s,string flag,string node,string speaker)
            {
                var line=kit.Crossing(name,VeilBeside(s,0,0),new Vector3(VeilHalf*2+4,8,5),flag,node,speaker);line.transform.rotation=Quaternion.Euler(0,VeilHeading(s),0);return line;
            }
            Line("Pass the first beacon",Beacon1,"wend.beacon1","","Sela");Line("Pass the second beacon",Beacon2,"wend.beacon2","","Sela");Line("Pass the third beacon",Beacon3,"wend.beacon3","","Sela");
            WorldBuilder.Label("BEACON",VeilBeside(Beacon1,-12,-1)+new Vector3(0,0,-6),5);
            // The dead beacon: what the main quest's arrow finds (report DeadBeacon), and the talk that relights it (it sets wend.beaconRelit itself, as the hand goes to the lamp).
            var dead=kit.Discovery("The dead beacon's lamp",VeilBeside(DeadBeaconS,-12,0),"Relight the dark beacon","wend.beaconRelit","DeadBeacon","MaruDeadBeacon","Sela",null,0,7);
            WorldBuilder.Label("DEAD BEACON",VeilBeside(DeadBeaconS,-12,-1)+new Vector3(0,0,-7),5);
            for(int n=0;n<Pockets.Length;n++)
            {
                var tune=kit.Discovery("Off-lane beacon "+(n+1)+"'s lamp",VeilBeside(Pockets[n].s,Pockets[n].side,0),"Retune the beacon","wend.offBeacon"+(n+1),"OffLaneBeacon",null,"Sela","BeaconKeeper",0,7);
                tune.situation="tune";
                Cache(kit,n);
            }

            // ---- what's in the way. Leg one: pods to thread. Leg two: the swarm field. Leg three: the ray round the dead beacon.
            string pod=Foe("SporePod","Shellmine"),swarm=Foe("SporeSwarm","MireflySwarm"),ray=Foe("VeilRay","ChoristerDart");
            var rng=new System.Random(83);
            for(int b=0;b<PodBanks.Length;b++)
            {
                // A bank of pods across the lane with gaps a ship can thread, on show from the start.
                float s=PodBanks[b];var bank=kit.FlightFight("Pods_"+b,VeilBeside(s,0),new Vector3(VeilHalf*2,6,10),(pod,1));bank.transform.rotation=Quaternion.Euler(0,VeilHeading(s),0);
                var spawners=new List<Spawner>{bank.spawners[0]};bank.spawners[0].transform.position=VeilBeside(s,-44+(float)rng.NextDouble()*6);bank.spawners[0].radius=0;
                for(float side=-30;side<=46;side+=13+(float)rng.NextDouble()*6)if(Mathf.Abs(side-(b-1)*10)>8)spawners.Add(ZoneKit.Spawn(bank,pod,1,VeilBeside(s+(float)rng.NextDouble()*16-8,side),0));
                bank.spawners=spawners.ToArray();var show=new GameObject("Pods on show").transform;show.SetParent(bank.transform,false);show.position=VeilBeside(s,0);bank.previewPoint=show;bank.previewRadius=0;
            }
            var drawn=new List<EncounterVolume>();
            var first=kit.FlightFight("Lane_0",VeilBeside(460,0),new Vector3(70,6,24),(swarm,2));first.transform.rotation=Quaternion.Euler(0,VeilHeading(460),0);drawn.Add(first);
            for(int k=0;k<Field.Length;k++)
            {
                // Hung in the lane, asleep and on show: a dark ship wakes one only by flying into its cloud; a lit one wakes every one within the field's reach.
                var (s,side)=Field[k];var at=VeilBeside(s,side);
                var cloud=kit.FlightFight("Field_"+k,at,new Vector3(12,6,12),(swarm,k%3==1&&k!=4?2:1));
                var show=new GameObject("Swarm on show").transform;show.SetParent(cloud.transform,false);show.position=at;cloud.previewPoint=show;cloud.previewRadius=0;
                drawn.Add(cloud);
            }
            var pocket=kit.FlightFight("Pocket_B",VeilBeside(Pockets[1].s,Pockets[1].side-14),new Vector3(30,6,30),(swarm,2));drawn.Add(pocket);
            // The ray glides a loop of about 22 m round where it is set (its Strafer tune): set 20 m to starboard of the dead beacon, its near pass goes by the lamp and its far pass is
            // forty metres off, which is the window to relight it in. The encounter is the whole circuit, so it is awake as a ship comes in on it.
            var rayAt=VeilBeside(DeadBeaconS,8);
            var hunt=kit.FlightFight("Veil_Ray",rayAt,new Vector3(84,6,84),(ray,1));hunt.spawners[0].transform.position=rayAt;hunt.spawners[0].radius=0;
            var field=new GameObject("Spores come to light",typeof(GlowField)).GetComponent<GlowField>();field.drawn=drawn.ToArray();field.reach=30;

            // ---- Maru on the radio. Her hail starts as the lead ship crosses a line across the whole lane, just inside the first beacon; her leg calls are barks over the flying.
            Line("Maru hails the lane",52,"wend.maruHailed","MaruVeilHail","Maru");
            // At the mouth of pocket A, for a ship that strays toward the slipped beacon: once, and never after she has asked in town.
            var stray=kit.Crossing("Maru calls a stray ship",VeilBeside(Pockets[0].s,Pockets[0].side+24,0),new Vector3(10,8,46),"wend.strayed","MaruVeilStray","Maru");
            stray.transform.rotation=Quaternion.Euler(0,VeilHeading(Pockets[0].s),0);
            var asked=new GameObject("Maru has asked in town",typeof(FlagGate)).GetComponent<FlagGate>();asked.flag="maru.comesHeard";asked.showWhenSet=false;asked.targets=new[]{stray.gameObject};
            void Hail(string situation,float s,string heard,float range,params string[] needs)
            {
                var hail=new GameObject("Maru on the radio: "+situation,typeof(RadioHail)).GetComponent<RadioHail>();hail.transform.position=VeilBeside(s,0);
                hail.speaker="Maru";hail.situations=situation.Split(',');hail.needs=needs;hail.heardFlag=heard;hail.silencedBy="";hail.range=range;
            }
            Hail("hail.pods",115,"wend.hailPods",40);Hail("hail.swarm",545,"wend.hailSwarm",40);Hail("hail.ray",1030,"wend.hailRay",40);
            Hail("hail.relit,hail.gate",DeadBeaconS,"wend.hailRelit",4000,"wend.beaconRelit");

            kit.FlightCorridor(VeilLane,VeilHalf,18);
            WorldBuilder.Save(scene,kit.zone);
            Debug.Log("WENDMIRE_COORDS SporeVeil arrival=(0,1,6) home-warp=(-14,1,-4) beacons="+string.Join(" ",new[]{Beacon1,Beacon2,Beacon3}.Select(s=>VeilBeside(s,-12).ToString("0.0")))
                +" dead-beacon="+VeilBeside(DeadBeaconS,-12).ToString("0.0")+" gate="+gateAt.ToString("0.0")+" from-town="+VeilBeside(FromTownS,4).ToString("0.0")
                +" pockets="+string.Join(" ",Pockets.Select(p=>VeilBeside(p.s,p.side).ToString("0.0")))+" stray="+VeilBeside(Pockets[0].s,Pockets[0].side+24).ToString("0.0")
                +" field="+string.Join(" ",Field.Select(f=>VeilBeside(f.s,f.side).ToString("0.0")))+" pods="+string.Join(" ",PodBanks.Select(s=>VeilBeside(s,0).ToString("0.0")))
                +" lane0="+VeilBeside(460,0).ToString("0.0")+" ray="+rayAt.ToString("0.0")+" hail="+VeilBeside(52,0).ToString("0.0")+" length="+length.ToString("0"));
            Debug.Log("SPORE_VEIL_OK");
        }

        // ---------------------------------------------------------------- the beacons
        /// <summary>A Driftreed pulse beacon out in the Veil: the generated lamp post on a woven float with roots trailing under it, under the plane and drawn only, its lamp keeping a count
        /// (<see cref="LampRhythm"/>) with a light that reaches the lane and a halo the flight camera reads from its height.</summary>
        static void Beacon(ZoneKit kit,string name,Vector3 at,LampRhythm.Count count,string litFlag,string retunedFlag,float offset)
        {
            var root=new GameObject(name);root.transform.position=at;var rng=new System.Random(name.Length*31+(int)at.z);
            var floatShape=new ZoneKit.Shape().Slot(0).Lump(at+Vector3.down*.4f,new Vector3(2.6f,.7f,2.6f),rng.Next(99),.12f,Color.white,new Color(.6f,.56f,.5f),16,6);
            for(int k=0;k<7;k++)
            {
                float a=k*Mathf.PI*2/7+(float)rng.NextDouble();var from=at+new Vector3(Mathf.Cos(a)*1.8f,-.6f,Mathf.Sin(a)*1.8f);var path=new Vector3[5];var thick=new float[5];
                for(int j=0;j<5;j++){float t=j/4f;path[j]=from+new Vector3(Mathf.Cos(a)*t*1.6f,-t*5.5f,Mathf.Sin(a)*t*1.6f)+new Vector3(Mathf.Sin(t*5+k),0,Mathf.Cos(t*4+k))*.5f*t;thick[j]=Mathf.Lerp(.18f,.04f,t);}
                floatShape.Slot(1).Tube(path,thick,5,new Color(.7f,.62f,.86f),new Color(.5f,.84f,.66f),1,.5f);
            }
            var body=kit.Built(name+" float",floatShape,Weave,Strand);body.transform.SetParent(root.transform,true);body.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            GameObject post;Renderer[] lamps;
            if(ZoneKit.Landed("PulseBeacon")){post=kit.Prop("PulseBeacon",at,6.4f,30,false,false);lamps=post.GetComponentsInChildren<Renderer>();}
            else
            {
                var stand=new ZoneKit.Shape().Slot(0).Post(at,5.6f,.16f,.12f,7,new Color(.6f,.5f,.36f),new Color(.86f,.74f,.5f)).Slot(1).Lump(at+Vector3.up*5.9f,new Vector3(.45f,.6f,.45f),3,.05f,Color.white,Color.white,9,6);
                post=kit.Built(name+" post",stand,Plank,BeaconGlow);lamps=new[]{post.GetComponent<Renderer>()};
            }
            post.transform.SetParent(root.transform,true);foreach(var r in post.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
            var top=ZoneKit.Drawn(post).max.y;
            var light=new GameObject("Beacon light",typeof(Light));light.transform.SetParent(root.transform,false);light.transform.position=new Vector3(at.x,top-.4f,at.z);
            var l=light.GetComponent<Light>();l.type=LightType.Point;l.range=26;l.shadows=LightShadows.None;
            // A halo at the lamp, so the count reads from the flight camera's height and not only on the float.
            var halo=GameObject.CreatePrimitive(PrimitiveType.Quad);halo.name="Beacon halo";UnityEngine.Object.DestroyImmediate(halo.GetComponent<Collider>());halo.transform.SetParent(root.transform,false);
            halo.transform.position=new Vector3(at.x,top-.3f,at.z);halo.transform.rotation=Quaternion.Euler(48,0,0);halo.transform.localScale=Vector3.one*5.5f;
            var haloView=halo.GetComponent<Renderer>();haloView.sharedMaterial=BeaconHalo(kit);haloView.shadowCastingMode=ShadowCastingMode.Off;haloView.receiveShadows=false;
            var rhythm=root.AddComponent<LampRhythm>();rhythm.count=count;rhythm.retuned=LampRhythm.Count.TwoShortOneLong;rhythm.litFlag=litFlag??"";rhythm.retunedFlag=retunedFlag??"";rhythm.darkFlag="";
            rhythm.lights=new[]{l};rhythm.lamps=lamps;rhythm.halos=new[]{haloView};rhythm.colour=new Color(1,.8f,.48f);rhythm.intensity=4.2f;rhythm.emission=2.4f;rhythm.offset=offset;
        }
        static Material halo;
        static Material BeaconHalo(ZoneKit kit)=>halo!=null&&AssetDatabase.Contains(halo)&&AssetDatabase.GetAssetPath(halo).Contains(kit.zone)?halo:halo=kit.Glow("Beacon halo",false,new Color(1.6f,1.2f,.7f,1));
        /// <summary>A cache in a pocket: a woven basket of spore caps tied to a drifting float, at the plane where a ship can take it.</summary>
        static void Cache(ZoneKit kit,int n)
        {
            var at=VeilBeside(Pockets[n].s+6,Pockets[n].side+(n==0?8:-8));
            var shape=new ZoneKit.Shape().Slot(0).Tube(new[]{at+Vector3.down*.7f,at+Vector3.up*.5f},new[]{1.1f,1.3f},12,new Color(.8f,.68f,.46f),new Color(.95f,.84f,.58f),3,.6f,false)
                .Slot(1).Lump(at+Vector3.up*.5f,new Vector3(1.1f,.5f,1.1f),n,.2f,new Color(.6f,1,.8f),new Color(.3f,.6f,.5f),9,5);
            var basket=kit.Built("Spore-cap basket "+(n+1),shape,Weave,Spot);basket.transform.position=Vector3.zero;
            var point=new GameObject("Pocket cache "+(n+1));point.transform.position=at;basket.transform.SetParent(point.transform,true);
            var cache=n==0?LootCache.On(point,"wend.pocketA","Pilots' cache",110,ProgressionBuilder.Drop("Glowspore",3,1),ProgressionBuilder.Drop("ReedWeave",2,1))
                :LootCache.On(point,"wend.pocketB","Pilots' cache",140,ProgressionBuilder.AnyPart(3,GearSlot.Drive,1),ProgressionBuilder.Drop("Glowspore",2,1));
            cache.range=WorldBuilder.FlightDockRange;
        }

        // ---------------------------------------------------------------- the backdrop
        /// <summary>Wendmire's three moons, far under the lane, each where it shows under one leg: the pale one under leg one, the violet one under leg two, the green one under leg three.
        /// 380 to 430 m down, so their painted faces are denser than the screen's pixels there (a face nearer in went soft, as Xylos's planet did).</summary>
        static void Moons(ZoneKit kit)
        {
            foreach(var (name,s,side,depth,radius,face) in new[]{("The pale moon",230f,90f,440f,90f,MoonPale),("The violet moon",760f,-120f,400f,60f,MoonViolet),("The green moon",1180f,110f,420f,72f,MoonGreen)})
            {
                // A whole ball, not a flattened dome: from a camera that looks down at 48 degrees the dome's rim read as the edge of a lens. Lit by the scene's star across its face,
                // so it has a terminator, and darker underneath in its own colour.
                var at=ShowingAt(VeilBeside(s,side),depth);
                // Its poles turned to the limbs, east and west: a sphere's pole is where its uv pinches, and turned away from the camera the other one faced it. Its face is laid
                // twice round it and once pole to pole, so the half the camera sees has the whole sheet: before, it had a quarter of it, and the face went soft.
                var dome=kit.Built(name,new ZoneKit.Shape().Lump(at,Vector3.one*radius,7,.008f,Color.white,new Color(.5f,.48f,.6f),96,48,1/(radius*3),Quaternion.Euler(0,0,90)),face);
                var view=dome.GetComponent<MeshRenderer>();view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
            }
        }
        /// <summary>The Veil's drift, the whole length of the lane: spore pods with their lit spots, flat colonies of lace, strands trailing between them, 4 to 13 m under the plane and piled
        /// toward the lane's middle, thicker round the beacons, the swarm field and the dead beacon. One mesh to each 170 m stretch, every draw taken whether or not its piece is kept.</summary>
        static void SporeDrift(ZoneKit kit,List<(Vector2 at,float clear)> keep,float length)
        {
            var rng=new System.Random(59);var root=new GameObject("Spore drift");const float Stretch=170;
            // Each pod's skin goes from its stem colour to its tip colour. Pale lilac on every one read as one pod copied down the lane: plum to rose, teal to mint, coral to
            // gold, ochre to cream, violet to lilac, moss to lime.
            (Color stem,Color tip)[] skins={(new(.6f,.34f,.72f),new(.96f,.58f,.8f)),(new(.26f,.62f,.64f),new(.6f,.98f,.82f)),(new(.94f,.5f,.4f),new(1f,.82f,.46f)),
                (new(.76f,.6f,.3f),new(.98f,.92f,.7f)),(new(.5f,.4f,.86f),new(.82f,.72f,1f)),(new(.4f,.62f,.32f),new(.78f,.96f,.46f))};
            Color[] spots={new(.6f,1f,.72f),new(.86f,.64f,1f),new(.6f,.94f,1f),new(1f,.86f,.5f)};
            for(float from=-30;from<length+30;from+=Stretch)
            {
                var shape=new ZoneKit.Shape();int made=0;
                for(float s=from;s<from+Stretch;s+=.5f)
                {
                    float side=((float)rng.NextDouble()+(float)rng.NextDouble()-1)*(VeilHalf+8),deep=-4-(float)rng.NextDouble()*9,size=.5f+(float)rng.NextDouble()*(float)rng.NextDouble()*2f,turn=(float)rng.NextDouble()*360,tilt=(float)rng.NextDouble()*360;
                    int kind=rng.Next(10),seed=rng.Next(1000),skin=rng.Next(skins.Length),spot=rng.Next(spots.Length);float shade=.75f+.25f*(float)rng.NextDouble();if(rng.Next(14)==0)size+=2+2.4f*(float)rng.NextDouble();
                    bool thick=Mathf.Abs(s-Beacon1)<60||Mathf.Abs(s-Beacon2)<60||Mathf.Abs(s-Beacon3)<60||Mathf.Abs(s-DeadBeaconS)<90||s>560&&s<980;
                    if(!thick&&rng.Next(3)==0)continue;
                    var at=VeilBeside(s+(float)rng.NextDouble()*5,side,deep);
                    if(!ZoneKit.ClearOnScreen(at,size*1.3f,keep))continue;
                    var turned=Quaternion.Euler(tilt*.3f,turn,tilt*.2f);made++;
                    if(kind<4)
                    {
                        // A seed pod lying any way in the drift, ribbed from stem to tip, in one of four builds: a spindle pointed at both ends, a teardrop swollen toward its tip, a
                        // gourd waisted between two bulbs, a long curved horn. Each bends a little, and its lit tip is where the spores come out (a horn's runs in a band instead).
                        var pose=Quaternion.Euler(tilt,turn,90+tilt*.5f);int build=seed%4;float podLength=size*(build==3?3.1f:2.3f),wide=size*(build==3?.34f:build==1?.56f:.48f),bend=(seed%7-3)*.06f;
                        var path=new Vector3[11];var radii=new float[11];
                        for(int p=0;p<=10;p++)
                        {
                            float t=p/10f,round=Mathf.Max(0,Mathf.Sin(Mathf.PI*t));
                            float profile=build switch{0=>Mathf.Pow(round,.8f),1=>Mathf.Pow(Mathf.Max(0,Mathf.Sin(Mathf.PI*Mathf.Pow(t,.62f))),.9f),2=>Mathf.Pow(round,.55f)*(.72f+.28f*Mathf.Cos(t*Mathf.PI*4)),_=>Mathf.Pow(round,.6f)*Mathf.Lerp(1,.45f,t)};
                            path[p]=at+pose*new Vector3(Mathf.Sin(t*Mathf.PI)*bend*podLength,(t-.5f)*podLength,0);radii[p]=Mathf.Max(.02f,wide*profile);
                        }
                        var (stem,tipTone)=skins[skin];
                        shape.Slot(0).Tube(path,radii,14,ZoneKit.Shape.Dim(stem,shade),ZoneKit.Shape.Dim(tipTone,shade),1,.5f/Mathf.Max(.5f,size));
                        if(build==3)for(int b=0;b<3;b++){var ring=path[6+b];shape.Slot(1).Lump(ring,Vector3.one*Mathf.Max(.08f,radii[6+b]*1.08f),seed+b,.06f,spots[spot],ZoneKit.Shape.Dim(spots[spot],.7f),8,5);}
                        else shape.Slot(1).Lump(path[10]+(path[10]-path[9]).normalized*size*.06f,Vector3.one*Mathf.Max(.1f,size*.2f),seed,.08f,spots[spot],ZoneKit.Shape.Dim(spots[spot],.7f),8,5);
                    }
                    else if(kind<6)
                    {
                        // A colony of lace: two to four shelves stacked a little apart and turned, each a plate of cells with a thickness to it, its rim curled under in a roll and
                        // beaded with spores. Flat single plates read as coins.
                        int shelves=2+seed%3;var top=ZoneKit.Shape.Dim(seed%3==0?new Color(.8f,.62f,.96f):new Color(.56f+.24f*(seed%5)/4f,.88f,.66f),shade);var under=ZoneKit.Shape.Dim(new Color(.4f,.32f,.56f),shade);
                        for(int l=0;l<shelves;l++)
                        {
                            float plate=size*1.35f*(1-.24f*l),rim=Mathf.Max(.06f,plate*.09f);
                            var tilted=turned*Quaternion.Euler((seed*7+l*29)%40-20,(seed*13+l*71)%360,(seed*11+l*43)%40-20);var n=tilted*Vector3.up;
                            var c=at+turned*new Vector3(((seed+l*3)%5-2)*size*.18f,l*size*.42f,((seed*3+l)%5-2)*size*.18f);
                            shape.Slot(2).Disc(c+n*rim,n,plate,24,top,0,ZoneKit.Shape.Dim(top,.8f),.6f).Disc(c-n*rim,-n,plate*.98f,24,under,0,under,.6f);
                            var roll=new Vector3[25];var rolls=new float[25];var u0=Vector3.Cross(n,Mathf.Abs(n.y)>.9f?Vector3.right:Vector3.up).normalized;var v0=Vector3.Cross(n,u0);
                            for(int k=0;k<=24;k++){float a=k*Mathf.PI*2/24;roll[k]=c+(u0*Mathf.Cos(a)+v0*Mathf.Sin(a))*plate;rolls[k]=rim*(1.1f+.35f*Mathf.Sin(a*3+seed));}
                            shape.Slot(2).Tube(roll,rolls,6,ZoneKit.Shape.Dim(top,.9f),ZoneKit.Shape.Dim(top,.9f),1,.6f,false);
                            for(int b=0;b<4;b++){float a=b*1.57f+l+seed;shape.Slot(1).Lump(c+(u0*Mathf.Cos(a)+v0*Mathf.Sin(a))*plate+n*rim,Vector3.one*Mathf.Max(.06f,size*.07f),b+seed,.05f,spots[spot],ZoneKit.Shape.Dim(spots[spot],.7f),6,4);}
                        }
                    }
                    else if(kind<8)
                    {
                        // A strand, trailing in a slow S, with spores strung along it like beads.
                        var path=new Vector3[9];var thick2=new float[9];for(int k=0;k<9;k++){float t=k/8f;path[k]=at+turned*new Vector3(Mathf.Sin(t*5)*size,Mathf.Sin(t*3)*size*.3f,(t-.5f)*size*5);thick2[k]=size*Mathf.Lerp(.16f,.07f,t);}
                        shape.Slot(3).Tube(path,thick2,7,ZoneKit.Shape.Dim(new Color(.7f,.56f,.9f),shade),ZoneKit.Shape.Dim(new Color(.5f,.86f,.7f),shade),1,.4f);
                        for(int k=1;k<8;k+=2)shape.Slot(1).Lump(path[k],Vector3.one*Mathf.Max(.09f,size*.17f),k+seed,.05f,spots[spot],ZoneKit.Shape.Dim(spots[spot],.7f),7,4);
                    }
                    else
                    {
                        // A loose spore: a bead of light.
                        shape.Slot(1).Lump(at,Vector3.one*Mathf.Min(.35f,size*.2f),seed,.05f,spots[spot],ZoneKit.Shape.Dim(spots[spot],.6f),6,4);
                    }
                }
                if(made==0)continue;
                var part=kit.Built("Spore drift "+Mathf.RoundToInt(from+30),shape.Slot(3),Pod,Spot,Lace,Strand);part.transform.SetParent(root.transform,true);
                part.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                // A point that isn't a number takes the whole stretch's bounds with it, and the stretch is culled from every frame (a pod's profile went below nothing at its tip).
                var bounds=part.GetComponent<MeshFilter>().sharedMesh.bounds;if(float.IsNaN(bounds.size.x)||float.IsInfinity(bounds.size.x))throw new InvalidOperationException("SporeVeil: the drift from "+from+" has a point that isn't a number");
            }
        }
        /// <summary>The mire under the last stretch of the lane, 35 m down: dark water, peat islands with reed beds, mangrove crowns at the edges, and floating rafts of scum. Where the
        /// lane comes down to Driftreed, and what the tide-gate's ring lies on.</summary>
        static void Mire(ZoneKit kit,List<(Vector2 at,float clear)> keep)
        {
            const float Depth=35;var rng=new System.Random(97);var centre=ShowingAt(VeilBeside(GateS-60,0),Depth);
            // Whole to Solid metres from its middle, then thinning to nothing over Fade more. A square sheet's near edge crossed the screen as a ruled line, the starfield
            // below it and the water above reading as teal fog (nov24 and nov25-whole s23 193 to 211). The sheet's draws are still taken, so every island and raft lies where it did.
            const float Solid=150,Fade=72;
            for(int k=0;k<18*16;k++)rng.NextDouble();
            var water=new ZoneKit.Shape();var still=new Color(.74f,.93f,.87f);
            Color Thinned(float t){var c=still;c.a=1-ZoneKit.Smooth(0,1,t);return c;}
            water.Slot(0).Disc(centre,Vector3.up,Solid,96,still,0,null,1/30f);
            for(int k=0;k<6;k++)water.Slot(1).Disc(centre,Vector3.up,Solid+Fade*(k+1)/6f,96,Thinned(k/6f),Solid+Fade*k/6f,Thinned((k+1)/6f),1/30f);
            kit.Built("The mire below",water,MireWater,MireEdge).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            // What lies on the water lies on the whole of it: on the thinning edge an island hung in the starfield.
            bool Whole(Vector3 p,float reach)=>Vector2.Distance(Flat(p),Flat(centre))+reach<Solid;
            var islands=new ZoneKit.Shape();var reeds=new ZoneKit.Shape();
            for(int k=0;k<70;k++)
            {
                var at=centre+new Vector3((float)rng.NextDouble()*380-190,0,(float)rng.NextDouble()*340-150);float size=3+(float)rng.NextDouble()*9;int seed=rng.Next(999);
                if(!ZoneKit.ClearOnScreen(at,size+4,keep))continue;
                // Off the whole water its draws are still taken, into shapes nobody keeps, so the next island lies where it did.
                bool whole=Whole(at,size+2);var onIslands=whole?islands:new ZoneKit.Shape();var onReeds=whole?reeds:new ZoneKit.Shape();
                // Peat with moss on its back, a ragged edge, and reeds thick round it.
                onIslands.Slot(0).Lump(at,new Vector3(size,.9f,size*.8f),seed,.18f,new Color(.44f,.56f,.32f),new Color(.3f,.24f,.2f),20,6,.4f,Quaternion.Euler(0,seed,0));
                for(int r=0;r<(int)(size*3f);r++)
                {
                    float a=r*2.4f+seed,reach=size*.8f*(float)rng.NextDouble();var foot=at+new Vector3(Mathf.Cos(a)*reach,.6f,Mathf.Sin(a)*reach);
                    onReeds.Frond(foot,new Vector3(Mathf.Cos(a+1),0,Mathf.Sin(a+1)),1.4f+(float)rng.NextDouble(),.3f,2.2f+(float)rng.NextDouble()*1.5f,.6f,new Color(.3f,.42f,.26f),new Color(.66f,.8f,.44f),3);
                }
                // Mangroves on the bigger islands: roots arching into the water, a crown over them.
                if(size>8)
                {
                    for(int r=0;r<6;r++){float a=r*1.05f+seed;var top=at+Vector3.up*3.2f;var foot=at+new Vector3(Mathf.Cos(a)*size*.6f,-.2f,Mathf.Sin(a)*size*.6f);
                        onIslands.Slot(1).Tube(new[]{foot,Vector3.Lerp(foot,top,.5f)+Vector3.up*1.4f,top},new[]{.22f,.3f,.4f},6,new Color(.5f,.44f,.4f),new Color(.66f,.6f,.54f),1,.3f);}
                    onIslands.Slot(2).Lump(at+Vector3.up*5.6f,new Vector3(size*.7f,2.4f,size*.6f),seed,.3f,new Color(.44f,.6f,.36f),new Color(.2f,.3f,.22f),14,6,.3f);
                }
            }
            // Wendmire's lilies on the open water, rafts of pads with a glowing bud in one of three, thickest round the tide-gate: the ring of lamps lies among them.
            var gateAt=ShowingAt(VeilBeside(GateS,0),Depth);var pads=new ZoneKit.Shape();
            for(int k=0;k<420;k++)
            {
                bool near=k<240;float reach=near?14+(float)rng.NextDouble()*46:(float)rng.NextDouble()*190,a=(float)rng.NextDouble()*6.283f;
                var at=(near?gateAt:centre)+new Vector3(Mathf.Cos(a)*reach,.05f,Mathf.Sin(a)*reach*.9f);float size=.8f+(float)rng.NextDouble()*1.6f;int seed=rng.Next(999);
                if(!ZoneKit.ClearOnScreen(at,size,keep))continue;
                var onPads=Whole(at,size*1.6f)?pads:new ZoneKit.Shape();
                for(int r=0;r<1+seed%3;r++)
                {
                    var c=at+new Vector3(Mathf.Cos(seed+r*2.1f)*size*.9f,.01f*r,Mathf.Sin(seed+r*2.1f)*size*.9f);float green=.26f+.16f*(float)rng.NextDouble();
                    onPads.Slot(0).Disc(c,Vector3.up,size*(.5f+.2f*r),12,new Color(.2f,green,.24f),0,new Color(.14f,green*.7f,.2f));
                    if(seed%3==0&&r==0)onPads.Slot(1).Lump(c+Vector3.up*.2f,new Vector3(.35f,.3f,.35f),seed,.15f,new Color(.7f,1,.8f),new Color(.5f,.8f,.7f),8,5);
                }
            }
            kit.Built("Lilies below",pads,ReedClump,Spot).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            kit.Built("Peat islands",islands,Loam,Bark,Leaf).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            kit.Built("Reed beds below",reeds,ReedClump).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        /// <summary>The tide-gate: a ring of Driftreed's pulse beacons on floats on the mire's surface, seen from above, keeping the town's count together; the landing is its middle.</summary>
        static void TideGate(ZoneKit kit,Vector3 gateAt)
        {
            const float Depth=35,Ring=15;var middle=ShowingAt(gateAt,Depth);
            for(int k=0;k<10;k++)
            {
                float a=k*Mathf.PI*2/10;var at=middle+new Vector3(Mathf.Cos(a)*Ring,1.2f,Mathf.Sin(a)*Ring*.9f);
                Beacon(kit,"Tide-gate lamp "+(k+1),at,LampRhythm.Count.TwoShortOneLong,null,null,0);
            }
            var lines=new ZoneKit.Shape();
            for(int k=0;k<10;k++){float a=k*Mathf.PI*2/10,b=(k+1)*Mathf.PI*2/10;lines.Rope(middle+new Vector3(Mathf.Cos(a)*Ring,.6f,Mathf.Sin(a)*Ring*.9f),middle+new Vector3(Mathf.Cos(b)*Ring,.6f,Mathf.Sin(b)*Ring*.9f),.6f,.12f,new Color(.86f,.76f,.54f));}
            kit.Built("Tide-gate lines",lines,Plank).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            WorldBuilder.Label("TIDE-GATE",gateAt+new Vector3(0,-2,-9),6);
        }
    }
}
