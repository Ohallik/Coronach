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
    /// <summary>Xylos Nearspace (space combat; rule: moving cargo). Map brief and evidence: docs/maps/XylosNearspace.md.</summary>
    public static partial class XylosBuilder
    {
        // ---------------------------------------------------------------- the lane
        /// <summary>The lane from the arrival beacon to the delta beacon, about 1.66 km: north out of the warp, bending north-east past the weather mirror and east over the planet's limb.
        /// Everything in the zone is placed by how far along it lies and how far to starboard, so the lane can be redrawn without touching what stands on it.</summary>
        internal static readonly Vector2[] Lane={new(0,0),new(30,240),new(130,520),new(300,800),new(520,1000),new(760,1120),new(980,1180)};
        internal const float LaneHalf=62;
        static float LaneLength{get{float total=0;for(int i=1;i<Lane.Length;i++)total+=Vector2.Distance(Lane[i-1],Lane[i]);return total;}}
        /// <summary>The point of the lane a distance along it, and which way it runs there.</summary>
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
        internal static Vector3 Beside(float s,float starboard,float y=ZoneKit.FlightPlane){var p=Along(s,out var h)+StarboardOf(s)*starboard;return new Vector3(p.x,y,p.y);}
        static Vector2 StarboardOf(float s){Along(s,out var h);return new Vector2(h.y,-h.x);}
        static float HeadingAt(float s){Along(s,out var h);return Mathf.Atan2(h.x,h.y)*Mathf.Rad2Deg;}
        static Vector2 Plan(Vector3 p)=>new(p.x,p.z);

        /// <summary>Teo's three barges, numbered as his barks count them: where each lies snagged (along, to starboard, heading), where it comes to rest once cut free (along, to starboard)
        /// and how far it swings round on the way. One is the first barge, two the one over the mirror's dark half, three the far one the claim drones sit on.</summary>
        internal static readonly (float s,float side,float yaw,float restS,float restSide,float turn)[] Barges={(252,25,38,296,7,-30),(952,-28,-48,990,6,38),(1438,-31,24,1474,8,-34)};
        internal const float MirrorS=905,BeaconS=1650;
        static Vector3 BargeAt(int n)=>Beside(Barges[n].s,Barges[n].side);
        static Vector3 BargeRest(int n)=>Beside(Barges[n].restS,Barges[n].restSide);
        /// <summary>Which way a snagged barge's lane-side flank faces, on the plan.</summary>
        static Vector3 LaneSide(int n)
        {
            var b=Barges[n];var right=Quaternion.Euler(0,b.yaw,0)*Vector3.right;var toLane=Beside(b.s,0)-BargeAt(n);
            return Vector3.Dot(right,toLane)>=0?right:-right;
        }
        /// <summary>Where a barge's three knots sit: its lane-side flank, its bow and its stern, a body clear of the hull so fire and blade can reach each.</summary>
        static Vector3[] Knots(int n)
        {
            var at=BargeAt(n);var fore=Quaternion.Euler(0,Barges[n].yaw,0)*Vector3.forward;var flank=LaneSide(n);
            return new[]{at+flank*3.9f,at+fore*9.6f+flank*.8f,at-fore*9.6f+flank*.8f};
        }
        /// <summary>The two podskimmer nests, off the lane: along, to starboard.</summary>
        static readonly (float s,float side)[] Nests={(612,-46),(1236,45)};

        /// <summary>What must stay readable under the lane: ways in and out, the barges where they lie and where they come to rest, the mirror's point and the nests' caches, each with the
        /// room it needs. Backdrop shows south of where it stands (ZoneKit.ShowsAt), so it is kept clear by where it shows.</summary>
        static List<(Vector2 at,float clear)> NearspaceKeep()
        {
            var keep=new List<(Vector2,float)>{(new Vector2(-16,-2),11),(new Vector2(0,6),8),(Plan(Beside(BeaconS,0)),13),(Plan(Beside(MirrorS,-14)),9)};
            for(int n=0;n<Barges.Length;n++){keep.Add((Plan(BargeAt(n)),15));keep.Add((Plan(BargeRest(n)),13));}
            // Hulljack's stake off the far barge's far side (ClaimStake), by where it shows: it lies about 3.5 m under the plane.
            keep.Add((Plan(ClaimSide)+new Vector2(0,-4),13));
            foreach(var (s,side) in Nests)keep.Add((Plan(Beside(s,side)),9));
            return keep;
        }

        static void Nearspace()
        {
            var kit=ZoneKit.Flight("XylosNearspace");
            var scene=WorldBuilder.Begin(kit.zone);
            float length=LaneLength;var keep=NearspaceKeep();
            WorldBuilder.Spawn("Arrival",new Vector3(0,1,6));WorldBuilder.Spawn("Home",new Vector3(0,1,10));
            WorldBuilder.Spawn("Rootmarket",Beside(BeaconS-24,-4));
            kit.DriftingSky();

            // ---- what the lane flies over: the planet, the nebula's bands, and the stream of what the storm threw up here
            Planet(kit);
            var soft=kit.Glow("Band",true,new Color(1,1,1,1));
            var bands=new GameObject("Nebula bands");
            foreach(var (s,side,deep,wide,span,tone) in new[]{(40f,-30f,-60f,90f,300f,new Color(.5f,.9f,.55f,.42f)),(150f,34f,-80f,110f,340f,new Color(.95f,.8f,.4f,.36f)),(330f,-24f,-70f,100f,320f,new Color(.4f,.85f,.75f,.4f)),
                (470f,40f,-95f,120f,340f,new Color(.9f,.85f,.45f,.3f)),(1530f,30f,-80f,110f,320f,new Color(.5f,.9f,.6f,.36f)),(1620f,-30f,-60f,90f,260f,new Color(.95f,.8f,.4f,.34f))})
            {
                // Lying along the lane far below it: a soft ribbon whose ends fade, never a bar.
                var from=Beside(s-span*.5f,side,deep);var to=Beside(s+span*.5f,side,deep);
                var band=ZoneKit.Strand(bands.transform,from,to,soft,wide,tone,new Color(tone.r,tone.g,tone.b,0));band.alignment=LineAlignment.TransformZ;band.transform.rotation=Quaternion.Euler(90,0,0);
                band.SetPosition(0,band.transform.InverseTransformPoint(from));band.SetPosition(1,band.transform.InverseTransformPoint(to));
            }
            DebrisStream(kit,keep,length);
            var glow=kit.Glow();
            for(float s=0;s<length;s+=260)
            {
                // Seed dust in the stream, a box to each stretch of the lane: one box over a bent lane would spend most of its motes where nobody flies.
                var dust=kit.Drift("Drifting seed dust",Beside(s+130,0,-3),new Vector3(LaneHalf*2+10,16,280),glow,new Color(.92f,1,.62f,.26f),420);
                dust.transform.rotation=Quaternion.Euler(0,HeadingAt(s+130),0);
            }

            // ---- ways in and out
            // No lock: nobody stands here without the lane. It lands above Tallow with the dock still ahead, as the Wick's does.
            WorldBuilder.Warp("Warp — Tallow Approach",new Vector3(-16,1,-2),"TallowApproach","","Xylos").lockedPrompt="";
            var beaconAt=Beside(BeaconS,0);
            var land=WorldBuilder.Warp("Land — Rootmarket",beaconAt,"Rootmarket","xylos.bargesFree","Arrival");
            land.lockedPrompt="The delta beacon is dark: cut Teo Marrow's three seed barges free";land.range=WorldBuilder.FlightDockRange;
            DeltaBeacon(kit,beaconAt);
            var count=new GameObject("Barges free",typeof(FlagAll)).GetComponent<FlagAll>();count.flags=new[]{"xylos.barge1","xylos.barge2","xylos.barge3"};count.result="xylos.bargesFree";

            // ---- the three barges
            for(int n=0;n<Barges.Length;n++)SeedBarge(kit,n);

            // ---- what's in the way. Skimmers first and few, to teach; then the swarms that strip each barge, and the claim drones on the far one's far side.
            kit.FlightFight("Lane_0",Beside(130,0),new Vector3(90,6,26),("Podskimmer",3)).transform.rotation=Quaternion.Euler(0,HeadingAt(130),0);
            kit.FlightFight("Lane_1",Beside(720,0),new Vector3(90,6,26),("Podskimmer",5)).transform.rotation=Quaternion.Euler(0,HeadingAt(720),0);
            kit.FlightFight("Lane_2",Beside(1180,0),new Vector3(90,6,26),("Podskimmer",4),("ClaimDrone",1)).transform.rotation=Quaternion.Euler(0,HeadingAt(1180),0);
            for(int n=0;n<Nests.Length;n++)Nest(kit,n);
            WeatherMirror(kit);

            // ---- voices on the radio. Each starts as the lead ship crosses a line across the whole lane, so nobody flies round one.
            DiscoveryPoint Radio(string name,float s,string flag,string node,string speaker)
            {
                var line=kit.Crossing(name,Beside(s,0,0),new Vector3(LaneHalf*2+4,8,5),flag,node,speaker);line.transform.rotation=Quaternion.Euler(0,HeadingAt(s),0);return line;
            }
            Radio("Teo hails from the first barge",46,"xylos.teoHailed","TeoBargeHail","Teo");
            // Between the first barge and the mirror, once: the patrol holds whether or not a barge is free yet.
            Radio("The Compact patrol hails",470,"xylos.patrolHeard","PatrolHail","Patrol");
            // On the approach to the far barge, before anything there wakes: everyone hears what the grey drones are, and the side quest it starts is theirs to take or leave.
            Radio("Teo sees the claim drones",Barges[2].s-52,"xylos.claimSeen","TeoClaimDrones","Teo");
            void Hail(string situation,string heard,params string[] needs)
            {
                var hail=new GameObject("Teo on the radio: "+situation,typeof(RadioHail)).GetComponent<RadioHail>();hail.transform.position=Beside(length*.5f,0);
                hail.speaker="Teo";hail.situations=new[]{situation};hail.needs=needs;hail.heardFlag=heard;hail.silencedBy="";hail.range=4000;
            }
            // A line from Teo as each barge comes free, about that barge and not a count, so any order works; and his call once all three are.
            for(int n=1;n<=3;n++)Hail("hail.barge"+n,"xylos.teoBarge"+n,"xylos.barge"+n);
            Hail("hail.landing","xylos.teoLanding","xylos.barge1","xylos.barge2","xylos.barge3");

            kit.FlightCorridor(Lane,LaneHalf,10);
            NoBrokenMeshes(kit.zone);
            WorldBuilder.Save(scene,kit.zone);
            Debug.Log("XYLOS_COORDS XylosNearspace arrival=(0,1,6) home-warp=(-16,1,-2) beacon="+beaconAt+" rootmarket-spawn="+Beside(BeaconS-24,-4)+" barges="+string.Join(" ",Enumerable.Range(0,3).Select(n=>BargeAt(n)+"->"+BargeRest(n)))
                +" knots="+string.Join(" ",Enumerable.Range(0,3).SelectMany(n=>Knots(n)).Select(k=>k.ToString("0.0")))+" nests="+string.Join(" ",Nests.Select(n=>Beside(n.s,n.side)))+" mirror-point="+Beside(MirrorS,-14)
                +" hails="+Beside(46,0)+" "+Beside(470,0)+" fights="+Beside(130,0)+" "+Beside(720,0)+" "+Beside(1180,0)+" length="+length.ToString("0"));
            Debug.Log("XYLOS_NEARSPACE_OK");
        }

        // ---------------------------------------------------------------- the planet
        const float GlobeX=620,GlobeZ=760,GlobeY=-250,GlobeRadius=560,GlobeTall=190;
        /// <summary>The delta on the globe's map (the unit square it is painted on): how much a point belongs to the fan of channels below the great river's mouth, and where it lies from the mouth.</summary>
        static float GlobeFan(float u,float v,out float reach,out float bearing)
        {
            var from=new Vector2(u-.42f,v-.6f);reach=from.magnitude;bearing=Mathf.Atan2(from.y,from.x);
            return Mathf.Clamp01(1-Mathf.Abs(Mathf.DeltaAngle(bearing*Mathf.Rad2Deg,-40))/38f)*Mathf.Clamp01(1-reach/.3f)*Mathf.Clamp01(reach/.02f);
        }
        /// <summary>The land of Xylos over that map: above nothing is land; below it sea, a river or a channel of the delta. Smooth everywhere and about one unit to thirteen
        /// metres near its edge (measured: a median .078 a metre), so a texture holding it (as alpha .5 plus this) cut at one level gives a coast as crisp as the screen, and the
        /// same number says how far offshore a shallow is. One long river and its delta, not a web: the river is a single level of slow noise.</summary>
        internal static float GlobeLand(float u,float v)
        {
            float fan=GlobeFan(u,v,out float reach,out float bearing);
            float coast=(ZoneKit.Cloud(u,v,4,41)-.43f+.1f*fan)*42,river=(Mathf.Abs(ZoneKit.Cloud(u,v,3,77)-.5f)-.009f)*60;
            float braid=Mathf.Abs(Mathf.Sin(bearing*12+ZoneKit.Cloud(u,v,9,3)*7+reach*30));
            float channel=braid-(.26f+.18f*(1-fan))+2*(1-ZoneKit.Smooth(.04f,.12f,fan));
            return Mathf.Min(coast,Mathf.Min(river,channel));
        }
        /// <summary>Xylos, under the far two thirds of the lane: a flattened globe laid out flat from above, its crown 60 m under the mirror and its limb passing under the first barge and
        /// again under the delta beacon. Flattened so the whole of it sits inside the camera's far plane; from a camera that only ever looks down on it, that reads as a globe.
        /// The camera sees a patch of it about 115 m across, so it is built to be looked at from that near, in layers a few metres apart: sea, the land's dark bank, the land
        /// (cut along its coast by the shader, its fields, forest and shore the ground kit's tiling layers), cloud shadow and cloud. The gaps between them are what move as the lane does.</summary>
        static void Planet(ZoneKit kit)
        {
            var root=new GameObject("Xylos below");
            // One sheet of the globe: a grid over its disc, points past the limb drawn in onto it, every normal straight up. What the sheet has at a point (its layers' weights) is
            // asked of paint; where all three corners of a triangle have nothing, there is no triangle.
            Mesh Sheet(string name,float cell,Func<float,float,Vector4?> paint)
            {
                int n=Mathf.CeilToInt(GlobeRadius*2/cell)+1;var slot=new int[n*n];var has=new bool[n*n];var outside=new bool[n*n];
                var at=new Vector3[n*n];var map=new Vector2[n*n];var weight=new Vector4[n*n];var tone=new Color32[n*n];
                for(int z=0;z<n;z++)for(int x=0;x<n;x++)
                {
                    int i=z*n+x;float dx=(x*cell-GlobeRadius)/GlobeRadius,dz=(z*cell-GlobeRadius)/GlobeRadius,r2=dx*dx+dz*dz;
                    if(r2>1){float pull=1/Mathf.Sqrt(r2);dx*=pull;dz*=pull;r2=1;outside[i]=true;}
                    at[i]=new Vector3(GlobeX+dx*GlobeRadius,GlobeY+GlobeTall*Mathf.Sqrt(Mathf.Max(0,1-r2)),GlobeZ+dz*GlobeRadius);map[i]=new Vector2(dx*.5f+.5f,dz*.5f+.5f);
                    var w=paint(map[i].x,map[i].y);has[i]=w.HasValue;weight[i]=w??Vector4.zero;tone[i]=Color.Lerp(Color.white,new Color(.5f,.86f,.86f),r2*r2*r2);slot[i]=-1;
                }
                var points=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var weights=new List<Vector4>();var tones=new List<Color32>();var faces=new List<int>();
                int Use(int i){if(slot[i]<0){slot[i]=points.Count;points.Add(at[i]);normals.Add(Vector3.up);uv.Add(map[i]);weights.Add(weight[i]);tones.Add(tone[i]);}return slot[i];}
                void Face(int a,int b,int c){if(outside[a]&&outside[b]&&outside[c]||!has[a]&&!has[b]&&!has[c])return;faces.Add(Use(a));faces.Add(Use(b));faces.Add(Use(c));}
                for(int z=0;z<n-1;z++)for(int x=0;x<n-1;x++){int i=z*n+x;Face(i,i+n,i+n+1);Face(i,i+n+1,i+1);}
                var mesh=new Mesh{name=name,indexFormat=points.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};
                mesh.SetVertices(points);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetUVs(2,weights);mesh.SetColors(tones);mesh.SetTriangles(faces,0);mesh.RecalculateBounds();
                Debug.Log("XYLOS_GLOBE "+name+" points="+points.Count);
                return ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+kit.zone+"-"+name.Replace(' ','-')+".asset");
            }
            void Layer(string name,Mesh mesh,Material material,Vector3 moved)
            {
                var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root.transform,false);go.transform.position=moved;go.GetComponent<MeshFilter>().sharedMesh=mesh;
                var view=go.GetComponent<MeshRenderer>();view.sharedMaterial=material;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
            }
            var sea=Sheet("Xylos sea",20,(u,v)=>new Vector4(1,0,0,0));
            var land=Sheet("Xylos land",5,(u,v)=>
            {
                // Field, forest, shore: each on or off within a cell, so the shader's cut along them is as sharp as the grid allows and its own noise makes it ragged.
                float ground=GlobeLand(u,v);if(ground<-1.5f)return null;
                float forest=ZoneKit.Cloud(u,v,13,5),fan=GlobeFan(u,v,out _,out _);
                return new Vector4(Mathf.Clamp01(.5f+Mathf.Max(.44f-forest,fan*.6f-.1f)*40),Mathf.Clamp01(.5f+(forest-.52f)*40)*(1-Mathf.Clamp01(fan*3)),Mathf.Clamp01(.5f+(.3f-ground)*6),0);
            });
            var cloud=Sheet("Xylos cloud",20,(u,v)=>Vector4.zero);
            Layer("Xylos sea",sea,Sea,Vector3.zero);Layer("Xylos banks",land,LandBank,Vector3.up*1.1f);Layer("Xylos land",land,Land,Vector3.up*2.4f);
            // A cloud's shadow lies on the land a little down-sun of it; the gap between the two is what says the cloud is above.
            // Low: the mirror hangs 26 m over the land where it lies lowest, and cloud above that stood in its panes.
            Layer("Xylos cloud shade",cloud,CloudShade,new Vector3(5,3.6f,-6));Layer("Xylos clouds",cloud,Clouds,Vector3.up*14);
        }

        // ---------------------------------------------------------------- snagvine
        /// <summary>Snagvine as it is drawn everywhere in the lane: a round stem thick enough to read from the flight camera, running smoothly through the points it is given, thorned
        /// along its length, swelling into a knot every few metres with a bud of violet light sunk in it. Stem and thorns go in one slot of the shape, buds in another.</summary>
        static void Snagvine(ZoneKit.Shape into,IReadOnlyList<Vector3> through,float stout,System.Random rng,int stemSlot=0,int budSlot=1,bool tapers=false,float knotEvery=3.4f)
        {
            // A smooth line through the points, a ring every half metre or so.
            var line=new List<Vector3>();
            for(int i=0;i<through.Count-1;i++)
            {
                Vector3 a=through[Mathf.Max(0,i-1)],b=through[i],c=through[i+1],d=through[Mathf.Min(through.Count-1,i+2)];
                int steps=Mathf.Max(2,Mathf.CeilToInt(Vector3.Distance(b,c)/Mathf.Max(.45f,stout*1.6f)));
                for(int k=0;k<steps;k++){float t=k/(float)steps;line.Add(.5f*(2*b+(c-a)*t+(2*a-5*b+4*c-d)*t*t+(3*b-a-3*c+d)*t*t*t));}
            }
            line.Add(through[through.Count-1]);
            float total=0;var run=new float[line.Count];for(int i=1;i<line.Count;i++){total+=Vector3.Distance(line[i],line[i-1]);run[i]=total;}
            float phase=(float)rng.NextDouble()*knotEvery;var girth=new float[line.Count];
            for(int i=0;i<line.Count;i++)
            {
                float near=Mathf.Abs(Mathf.Repeat(run[i]+phase+knotEvery*.5f,knotEvery)-knotEvery*.5f);
                girth[i]=stout*(tapers?Mathf.Lerp(1,.3f,run[i]/Mathf.Max(.01f,total)):1)*(1+.55f*Mathf.Exp(-near*near/(stout*stout*2.2f)));
            }
            Color deep=new(.42f,.62f,.4f),umber=new(.74f,.56f,.38f);bool turned=rng.Next(2)==0;
            into.Slot(stemSlot).Tube(line,girth,7,turned?deep:umber,turned?umber:deep,2,.3f);
            // Thorns and buds by distance along it, each off the stem at its own angle.
            float nextThorn=.4f+(float)rng.NextDouble()*.6f,nextKnot=knotEvery-phase;if(nextKnot<.6f)nextKnot+=knotEvery;
            for(int i=1;i<line.Count-1;i++)
            {
                var along=(line[i+1]-line[i-1]).normalized;var side=Vector3.Cross(along,Mathf.Abs(along.y)>.9f?Vector3.right:Vector3.up).normalized;
                while(run[i]>=nextThorn)
                {
                    var outward=Quaternion.AngleAxis((float)rng.NextDouble()*360,along)*side;float reach=girth[i]*(1.9f+1.2f*(float)rng.NextDouble());
                    into.Slot(stemSlot).Tube(new[]{line[i]+outward*girth[i]*.6f,line[i]+outward*(girth[i]+reach)+along*reach*.35f},new[]{girth[i]*.36f,.012f},5,new Color(.5f,.42f,.3f),new Color(.95f,.86f,.66f),1,.5f,false);
                    nextThorn+=(.55f+.75f*(float)rng.NextDouble())*Mathf.Max(.7f,stout*2.4f);
                }
                if(run[i]>=nextKnot)
                {
                    var outward=Quaternion.AngleAxis((float)rng.NextDouble()*360,along)*side;
                    into.Slot(budSlot).Lump(line[i]+outward*girth[i]*.62f,Vector3.one*girth[i]*.6f,rng.Next(999),.12f,Color.white,new Color(.55f,.5f,.62f),12,7);
                    nextKnot+=knotEvery;
                }
            }
            into.Slot(stemSlot);
        }

        // ---------------------------------------------------------------- the debris stream
        /// <summary>What the storm lifted off the delta, strung out along the lane below the flight plane, thickest where the barges were caught. Backdrop, never an obstacle. It is
        /// things, each with its own outline: husks curled like horns where a pod split, seeds in bunches and alone, root balls with their roots still spread, lengths of decking,
        /// forked boughs, rags of thatch, thorned lengths of snagvine and glass from the mirror, built as one mesh to each stretch of the lane; and among them whole pieces of the
        /// town the storm took (<see cref="Wreckage"/>).</summary>
        static void DebrisStream(ZoneKit kit,List<(Vector2 at,float clear)> keep,float length)
        {
            var rng=new System.Random(53);var root=new GameObject("Debris stream");
            const float Stretch=170;
            float R()=>(float)rng.NextDouble();
            Color Dim(float r,float g,float b,float by)=>new(r*by,g*by,b*by,1);
            for(float from=-30;from<length+30;from+=Stretch)
            {
                var shape=new ZoneKit.Shape();int made=0;
                for(float s=from;s<from+Stretch;s+=.42f)
                {
                    // Draws first, every time, so dropping one piece moves no other; what a piece needs for its own shape comes from a seed drawn here. Most of the stream runs under
                    // the lane itself (two draws summed pile toward the middle), where the camera is looking; most pieces are small, one in ten is something big enough to steer by.
                    float side=(R()+R()-1)*(LaneHalf+6),deep=-3.6f-R()*9,size=.5f+R()*R()*1.8f,turn=R()*360,tilt=R()*360,spread=R(),shade=.75f+.25f*R(),along=R()*5,more=2.2f+2*R();
                    int kind=rng.Next(12),seed=rng.Next(1000),piece=rng.Next(1<<20);bool big=rng.Next(10)==0,aside=rng.Next(2)==0,thin=rng.Next(4)==0;
                    // Anything big lies deeper, and a root ball is never bigger than a ship: one at the plane's own depth read as an obstacle across the lane.
                    if(big){size+=kind==4||kind==5?more*.3f:more;deep-=3;}
                    // The stream thickens round each barge and over the mirror.
                    bool thick=Barges.Any(b=>Mathf.Abs(s-b.s)<70)||Mathf.Abs(s-MirrorS)<120;if(!thick&&thin)continue;
                    // Round a barge half the stream lies on the barge's own side and beyond it: its far flank is where its claim tag is, and where the claim drones wait at the third.
                    foreach(var b in Barges)if(aside&&Mathf.Abs(s-b.s)<48)side=b.side*1.9f+(spread-.5f)*46;
                    var at=Beside(s+along,side,deep);
                    if(!ZoneKit.ClearOnScreen(at,size*1.6f,keep))continue;
                    var turned=Quaternion.Euler(tilt,turn,tilt*.37f);var own=new System.Random(piece);float O()=>(float)own.NextDouble();made++;
                    switch(kind)
                    {
                        case 0:case 1:
                        {
                            // Half a pod's husk: a horn curled the way it dried, blunt where it split and drawn to a point. At the point the float sine of pi is a hair under
                            // zero, and a fractional power of that is not a number: every horn made its whole stretch of the stream unbounded, culled and logged on each load.
                            float bend=(70+own.Next(60))*Mathf.Deg2Rad,sweep=size*(1.5f+O());var horn=new Vector3[9];var girth=new float[9];
                            for(int k=0;k<9;k++){float t=k/8f,a=(t-.5f)*bend;horn[k]=at+turned*new Vector3(Mathf.Sin(a)*sweep,(1-Mathf.Cos(a))*sweep,0);girth[k]=size*.46f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(Mathf.PI*(.08f+.92f*t))),.7f);}
                            shape.Slot(0).Tube(horn,girth,8,Dim(1,1,1,shade),Dim(.62f,.54f,.46f,shade),2,.3f);
                            break;
                        }
                        case 2:
                        {
                            // Seeds still clinging together.
                            int seeds=4+own.Next(5);
                            for(int k=0;k<seeds;k++)shape.Slot(1).Lump(at+turned*new Vector3((O()-.5f)*size*1.6f,(O()-.5f)*size,(O()-.5f)*size*1.6f),Vector3.one*(.2f+.22f*O()),own.Next(999),.1f,Dim(.95f,.88f,.68f,shade),new Color(.5f,.46f,.38f),6,4);
                            break;
                        }
                        case 3:shape.Slot(1).Lump(at,Vector3.one*Mathf.Min(.5f,size*.3f),seed,.1f,Dim(.9f,.84f,.68f,shade),new Color(.5f,.46f,.38f),6,4);break;
                        case 4:case 5:
                        {
                            // A root ball torn out whole: a knot of earth with its roots still spread.
                            float core=size*.42f;int roots=4+own.Next(3);
                            shape.Slot(2).Lump(at,new Vector3(core,core*.8f,core),seed,.3f,Dim(.8f,.74f,.66f,shade),new Color(.32f,.28f,.26f),7,4,.25f,turned);
                            for(int k=0;k<roots;k++)
                            {
                                var dir=turned*Quaternion.Euler(own.Next(140)-70,k*360f/roots+own.Next(40),0)*Vector3.forward;float reach=size*(1.5f+1.3f*O());var bend=turned*Vector3.up*reach*.25f*(O()-.3f);
                                shape.Slot(2).Tube(new[]{at+dir*core*.6f,at+dir*reach*.55f+bend,at+dir*reach},new[]{core*.34f,core*.2f,.03f},6,Dim(.74f,.68f,.6f,shade),Dim(.5f,.44f,.38f,shade),1,.4f);
                            }
                            break;
                        }
                        case 6:
                        {
                            // A length of somebody's deck: boards still pinned to a joist.
                            int boards=3+own.Next(4);float run=size*(1.6f+O());
                            for(int k=0;k<boards;k++){float each=run*(.6f+.4f*O());shape.Slot(6).Box(at+turned*new Vector3((k-(boards-1)*.5f)*.34f,0,(run-each)*.5f*(k%2==0?1:-1)),new Vector3(.3f,.06f,each),turned,Dim(.6f,.5f,.36f,shade),Dim(.86f,.72f,.5f,shade));}
                            shape.Slot(6).Box(at+turned*new Vector3(0,-.09f,run*.2f),new Vector3(boards*.34f+.2f,.1f,.14f),turned,Dim(.5f,.4f,.3f,shade));
                            break;
                        }
                        case 7:
                        {
                            // A rag of snagvine: a thorned length of it, bent as it was torn.
                            float reach=Mathf.Max(2.4f,size*2.6f),stout=Mathf.Max(.17f,size*.13f);
                            Snagvine(shape,new[]{at+turned*new Vector3(-reach*.5f,0,0),at+turned*new Vector3(-reach*.15f,reach*.22f,reach*.1f),at+turned*new Vector3(reach*.2f,-reach*.1f,reach*.24f),at+turned*new Vector3(reach*.5f,reach*.12f,0)},stout,own,3,5,true,2.4f);
                            break;
                        }
                        case 8:
                        {
                            // A bough: a forked limb, bare.
                            float reach=size*(2.2f+O());var fork=at+turned*new Vector3(0,0,reach*.15f);Color near=Dim(.8f,.74f,.66f,shade),far=Dim(.56f,.5f,.44f,shade);
                            shape.Slot(2).Tube(new[]{at+turned*new Vector3(0,0,-reach*.5f),fork},new[]{size*.2f,size*.15f},6,near,near,1,.3f)
                                .Tube(new[]{fork,at+turned*new Vector3(reach*.28f,reach*.05f,reach*.5f)},new[]{size*.14f,.03f},6,near,far,1,.3f)
                                .Tube(new[]{fork,at+turned*new Vector3(-reach*.22f,-reach*.08f,reach*.42f)},new[]{size*.12f,.03f},6,near,far,1,.3f);
                            break;
                        }
                        case 9:
                        {
                            // A rag of thatch off a roof, folded where it tore: never bigger than a doorway (a big one read as a flat green carpet laid across the lane).
                            float rag=Mathf.Min(size,1.4f),wide=rag*(1+O()),tall=rag*(.7f+.6f*O()),fold=rag*(.35f+.4f*O());var up=turned*Vector3.up;
                            Vector3 P(float x,float y,float z)=>at+turned*new Vector3(x,y,z);
                            shape.Slot(7).Quad(P(-wide,fold,-tall),P(-wide*.85f,fold*.8f,tall),P(0,0,tall*.9f),P(0,0,-tall*.95f),up,Dim(.62f,.72f,.42f,shade),.5f)
                                .Quad(P(0,0,-tall*.95f),P(0,0,tall*.9f),P(wide,fold*.6f,tall*.8f),P(wide*.9f,fold*.7f,-tall*.9f),up,Dim(.54f,.64f,.38f,shade),.5f);
                            break;
                        }
                        default:
                        {
                            // Mirror glass: a flat shard, commoner the nearer the mirror.
                            float near=Mathf.Clamp01(1-Mathf.Abs(s-MirrorS)/420f);if(O()>.2f+.8f*near){made--;break;}
                            Vector3 a=turned*new Vector3(-size,0,-size*.4f),b=turned*new Vector3(size*.3f,0,-size),c=turned*new Vector3(size*.9f,0,size*.5f);
                            shape.Slot(4).Triangle(at+a,at+b,at+c,turned*Vector3.up,Dim(1,1,1,shade),.2f);
                            break;
                        }
                    }
                }
                if(made==0)continue;
                var part=kit.Built("Debris stream "+Mathf.RoundToInt(from+30),shape.Slot(7),Husk,Cloth,Bark,Snag,Pane,SnagGlow,Plank,Thatch);part.transform.SetParent(root.transform,true);
                var view=part.GetComponent<MeshRenderer>();view.shadowCastingMode=ShadowCastingMode.Off;
            }
            Wreckage(kit,keep,length,root);
        }
        /// <summary>Whole things the storm took, turning under the lane among the small stuff: root arches, a sluice's frame, market stalls with their awnings, a hut's roof, uprooted
        /// spice bushes. Chapter 3's own kit pieces, any way up, drawn only and below the plane, each weathered a little differently; the big ones lie deeper so nothing reaches the plane.</summary>
        static void Wreckage(ZoneKit kit,List<(Vector2 at,float clear)> keep,float length,GameObject root)
        {
            var rng=new System.Random(211);float R()=>(float)rng.NextDouble();
            (string key,float small,float large)[] kinds={("RootArch",4.5f,8),("SluiceGate",3,4.6f),("MarketAwning",2.6f,3.6f),("SpicePodPlant",1.6f,2.6f),("RootArch",3,5),("SeedHouse",4,6),("SluiceGate",2.4f,3.4f),("SpicePodPlant",2,3)};
            int placed=0;
            for(float s=20;s<length;s+=21)
            {
                var (key,small,large)=kinds[rng.Next(kinds.Length)];
                float size=small+R()*(large-small),side=(R()*2-1)*(LaneHalf+4),deep=-4.5f-R()*8-size*.5f,pitch=R()*360,yaw=R()*360,roll=R()*360,shade=.55f+.3f*R(),warm=R(),along=R()*8;
                var at=Beside(s+along,side,deep);
                if(!ZoneKit.Landed(key)||!ZoneKit.ClearOnScreen(at,size*.8f,keep))continue;
                var wreck=kit.Loose(key,at,size,Quaternion.Euler(pitch,yaw,roll),new Color(shade*(.95f+.1f*warm),shade,shade*(1.02f-.12f*warm)));wreck.name="Storm wreck "+key;wreck.transform.SetParent(root.transform,true);placed++;
                foreach(var view in wreck.GetComponentsInChildren<Renderer>())view.shadowCastingMode=ShadowCastingMode.Off;
            }
            // The first thing out of the warp: a root arch the storm tore off the town, turning under the lane just ahead and to starboard of the arrival, where the keep round the
            // arrival leaves the frame otherwise to haze and a few husks. Placed by hand after the draws, so nothing else in the stream moves.
            var first=new Vector3(9,-8,23);
            if(ZoneKit.Landed("RootArch")&&ZoneKit.ClearOnScreen(first,3,keep))
            {
                var arch=kit.Loose("RootArch",first,6,Quaternion.Euler(38,70,112),new Color(.82f,.8f,.74f));arch.name="Storm wreck RootArch (first)";arch.transform.SetParent(root.transform,true);placed++;
                foreach(var view in arch.GetComponentsInChildren<Renderer>())view.shadowCastingMode=ShadowCastingMode.Off;
            }
            Debug.Log("XYLOS_WRECKAGE pieces="+placed);
        }

        // ---------------------------------------------------------------- a seed barge and what holds it
        /// <summary>A seed barge until its model is taken in (kit key SeedBarge): a ribbed pod hull lying level, open along the top on a deck of pale seeds, lashed with three bands of green
        /// vine, a curled stem at the prow. Built about its own middle, so the object can be moved and turned.</summary>
        static GameObject BargeBody(ZoneKit kit,int n)
        {
            GameObject barge;
            if(ZoneKit.Landed("SeedBarge"))
            {
                // The generated hull, bow along +z, fitted by its length (a piece is fitted by height otherwise) so the knots' places round it hold. A piece stands about its own middle already.
                barge=kit.Prop("SeedBarge",Vector3.zero,4.2f,0,true,false);barge.transform.localScale*=16.4f/Mathf.Max(.1f,ZoneKit.Drawn(barge).size.z);
                FitHull(barge);
            }
            else
            {
                var shape=new ZoneKit.Shape();
                shape.Slot(0).Lump(Vector3.zero,new Vector3(2.5f,1.9f,8.4f),7+n,.05f,Color.white,new Color(.62f,.56f,.5f),14,8,.12f);
                for(int k=0;k<7;k++)shape.Slot(2).Lump(new Vector3((k%2-.5f)*.9f,1.5f+.1f*(k%3),-5.4f+k*1.8f),Vector3.one*(.78f+.1f*(k%3)),31+k,.06f,new Color(1,.96f,.84f),new Color(.72f,.66f,.54f),8,5);
                foreach(float z in new[]{-4.8f,0f,4.8f})
                {
                    float girth=Mathf.Sqrt(Mathf.Max(.05f,1-z*z/(8.4f*8.4f)));var band=new Vector3[13];var thick=new float[13];
                    for(int k=0;k<13;k++){float a=k*Mathf.PI*2/12;band[k]=new Vector3(Mathf.Cos(a)*2.62f*girth,Mathf.Sin(a)*2f*girth,z);thick[k]=.24f;}
                    shape.Slot(1).Tube(band,thick,6,Color.white,Color.white,1,.5f,false).Lump(new Vector3(0,2.05f*girth,z),Vector3.one*.42f,3,.2f,Color.white,new Color(.6f,.7f,.5f),6,4);
                }
                // The stem it grew on, curled over the prow, and a leaf of a rudder at the stern.
                var stem=new Vector3[7];var taper=new float[7];for(int k=0;k<7;k++){float a=k/6f*Mathf.PI*1.3f;stem[k]=new Vector3(0,.4f+Mathf.Sin(a)*1.5f,8.1f+(1-Mathf.Cos(a))*1.1f);taper[k]=Mathf.Lerp(.42f,.08f,k/6f);}
                shape.Slot(0).Tube(stem,taper,6,Color.white,new Color(.7f,.8f,.5f),1,.4f);
                shape.Slot(1).Triangle(new Vector3(0,.2f,-8.1f),new Vector3(0,2.4f,-10.2f),new Vector3(0,-1.3f,-9.8f),Vector3.right,Color.white).Triangle(new Vector3(0,.2f,-8.1f),new Vector3(0,2.4f,-10.2f),new Vector3(0,-1.3f,-9.8f),Vector3.left,Color.white);
                barge=kit.Built("Seed barge "+(n+1),shape.Slot(2),Husk,Vine,Cloth);
                var hull=barge.AddComponent<BoxCollider>();hull.size=new Vector3(4.6f,3.6f,16.4f);
            }
            barge.name="Seed barge "+(n+1);return barge;
        }
        /// <summary>The mesh a barge is solid as, beside the scene like its other built meshes.</summary>
        internal const string HullMesh="Assets/_Project/Scenes/XylosNearspace-BargeHull.asset";
        /// <summary>How many sides the hull's outline on the plan is taken in.</summary>
        const int HullSides=48;
        /// <summary>A barge is solid where it is drawn: its outline on the plan (the furthest point of the model each way round), stood from its keel to its top, as one convex
        /// shape. The one box a piece is given stood out past the pointed bow and stern by two metres a side, so fire from beside the hull at the bow or stern knot, in plain
        /// sight past the prow, stopped in clear space (first player run, November 13: ninety seconds of fire and the knot untouched). Slabs across its length did the same
        /// at the stern by their corners. Returns the outline's corners.</summary>
        internal static int FitHull(GameObject barge)
        {
            foreach(var old in barge.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(old);
            var points=new List<Vector3>();var toBarge=barge.transform.worldToLocalMatrix;
            foreach(var filter in barge.GetComponentsInChildren<MeshFilter>())
            {
                if(filter.sharedMesh==null)continue;var toWorld=filter.transform.localToWorldMatrix;
                foreach(var v in filter.sharedMesh.vertices)points.Add(toBarge.MultiplyPoint3x4(toWorld.MultiplyPoint3x4(v)));
            }
            if(points.Count==0)throw new InvalidOperationException(barge.name+" has no mesh to take a solid shape from");
            var ring=new List<Vector3>();
            for(int k=0;k<HullSides;k++)
            {
                float a=k*Mathf.PI*2/HullSides,cos=Mathf.Cos(a),sin=Mathf.Sin(a);Vector3 far=points[0];float most=float.NegativeInfinity;
                foreach(var p in points){float d=p.x*cos+p.z*sin;if(d>most){most=d;far=p;}}
                var corner=new Vector3(far.x,0,far.z);if(ring.Count==0||(ring[ring.Count-1]-corner).sqrMagnitude>1e-5f)ring.Add(corner);
            }
            if(ring.Count>1&&(ring[0]-ring[ring.Count-1]).sqrMagnitude<1e-5f)ring.RemoveAt(ring.Count-1);
            int n=ring.Count;if(n<3)throw new InvalidOperationException(barge.name+"'s outline has "+n+" corners");
            float low=points.Min(p=>p.y),high=points.Max(p=>p.y);var corners=new Vector3[n*2];var faces=new List<int>();
            for(int i=0;i<n;i++)
            {
                corners[i]=ring[i]+Vector3.up*low;corners[n+i]=ring[i]+Vector3.up*high;int next=(i+1)%n;
                faces.AddRange(new[]{i,n+i,next,next,n+i,n+next});
                if(i>0&&i<n-1){faces.AddRange(new[]{0,i,i+1});faces.AddRange(new[]{n,n+i+1,n+i});}
            }
            // One asset for the three barges (one model at one size), written in place so a barge already holding it keeps it.
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(HullMesh);
            if(mesh==null){mesh=new Mesh{name="Seed barge hull"};AssetDatabase.CreateAsset(mesh,HullMesh);}
            mesh.Clear();mesh.vertices=corners;mesh.triangles=faces.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var solid=barge.AddComponent<MeshCollider>();solid.sharedMesh=mesh;solid.convex=true;
            return n;
        }
        /// <summary>Refits the three barges in the built scene (the fix for a scene built before FitHull), touching nothing else in it.</summary>
        public static void FitBargeHulls()=>BatchTools.Run(()=>
        {
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/XylosNearspace.unity");
            var barges=UnityEngine.Object.FindObjectsByType<SnaggedBarge>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            if(barges.Length!=3)throw new InvalidOperationException("XylosNearspace holds "+barges.Length+" snagged barges, not three");
            int corners=0;foreach(var barge in barges)corners=FitHull(barge.gameObject);
            AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log("BARGE_HULLS_OK barges="+barges.Length+" corners="+corners);
        });
        static void SeedBarge(ZoneKit kit,int n)
        {
            var site=Barges[n];Vector3 at=BargeAt(n),rest=BargeRest(n),flank=LaneSide(n);var knots=Knots(n);int number=n+1;
            var barge=BargeBody(kit,n);barge.transform.SetPositionAndRotation(at,Quaternion.Euler(0,site.yaw,0));
            // Not static: it moves once, when it's cut free, and it has to take its box with it.
            barge.isStatic=false;foreach(var part in barge.GetComponentsInChildren<Transform>())part.gameObject.isStatic=false;
            var held=barge.AddComponent<SnaggedBarge>();held.knotsEncounter="Barge"+number+"_Knots";held.freedFlag="xylos.barge"+number;
            // Out past its own length before it turns into the lane, so it never sweeps its stern across where a ship is likely waiting.
            held.drift=new[]{Vector3.Lerp(at,rest,.45f)+flank*4,rest};held.driftSeconds=12;held.turn=site.turn;

            // What holds it: a tangle of snagvine on its far side, below the plane, and from each knot a vine down into the tangle and another lashed over the hull.
            var snag=new GameObject("Snagvine holding barge "+number);var mass=at-flank*15+Vector3.down*7;var weed=new ZoneKit.Shape();var rng=new System.Random(90+n);var hullTurn=Quaternion.Euler(0,site.yaw,0);
            Vector3 InMass(float x,float y,float z)=>mass+hullTurn*new Vector3(x,y,z);
            float Roll(float span)=>((float)rng.NextDouble()-.5f)*span;
            for(int k=0;k<9;k++)
            {
                // Stems as thick as a mast wound through each other: each a slow coil of a turn or so about its own tilted axis, so the tangle has depth and no two cross flat.
                var centre=InMass(Roll(9),Roll(3),Roll(15));var tilt=Quaternion.Euler(Roll(140),Roll(360),Roll(140));
                float wide=2.6f+(float)rng.NextDouble()*3.2f,turns=.8f+(float)rng.NextDouble()*.7f,rise=Roll(5);var coil=new List<Vector3>();
                for(int j=0;j<=10;j++){float t=j/10f,a=t*turns*Mathf.PI*2;coil.Add(centre+tilt*new Vector3(Mathf.Cos(a)*wide*(1-.3f*t),rise*(t-.5f),Mathf.Sin(a)*wide*.7f*(1-.3f*t)));}
                Snagvine(weed,coil,.34f+.26f*(float)rng.NextDouble(),rng);
            }
            for(int k=0;k<5;k++)
            {
                // Tendrils reaching up past the hull: the weed is climbing it.
                var foot=InMass(Roll(8),0,Roll(18));var reach=at-flank*(2.6f+(float)rng.NextDouble())+hullTurn*Vector3.forward*Roll(14)+Vector3.up*Roll(2);
                var mid=Vector3.Lerp(foot,reach,.5f)+Vector3.up*(2.2f+Roll(1.5f))+hullTurn*Vector3.forward*Roll(3);
                Snagvine(weed,new[]{foot,Vector3.Lerp(foot,mid,.5f)+Vector3.down*.6f,mid,reach},.5f,rng,0,1,true);
            }
            // The heart of it: a few dark boles the stems grow out of, each with a bud alight.
            for(int k=0;k<3;k++)
            {
                var bole=InMass(Roll(6),Roll(2),Roll(10));float size=1.2f+(float)rng.NextDouble()*.7f;
                weed.Slot(0).Lump(bole,new Vector3(size,size*.8f,size*1.1f),k+n*10,.3f,new Color(.5f,.6f,.42f),new Color(.3f,.26f,.2f),9,6).Slot(1).Lump(bole+Vector3.up*size*.62f+hullTurn*Vector3.right*Roll(.6f),Vector3.one*size*.3f,k,.1f,Color.white,new Color(.55f,.5f,.62f),12,7);
            }
            var weedBody=kit.Built("Snagvine mass "+number,weed.Slot(1),Snag,SnagGlow);weedBody.transform.SetParent(snag.transform,true);weedBody.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            held.whileSnagged=snag;held.roots=new Transform[3];held.cables=new GameObject[3];
            for(int k=0;k<3;k++)
            {
                var root=new GameObject("Knot root "+(k+1)).transform;root.SetParent(snag.transform,true);root.position=knots[k];held.roots[k]=root;
                // Two vines a knot, in one mesh that goes when the knot does (SnaggedBarge): one down under the hull into the tangle, one lashed back over the hull to its far side.
                var cable=new ZoneKit.Shape();float station=k==1?6.5f:k==2?-6.5f:0;var into=InMass(0,.6f,station*.9f);
                Snagvine(cable,new[]{knots[k],Vector3.Lerp(knots[k],into,.35f)+Vector3.down*1.6f+flank*1.5f,Vector3.Lerp(knots[k],into,.7f)+Vector3.down*1.2f,into},.3f,rng,0,1,false,2.6f);
                var over=at+hullTurn*new Vector3(0,2.5f,station);
                Snagvine(cable,new[]{knots[k],Vector3.Lerp(knots[k],over,.55f)+Vector3.up*.7f,over,over-flank*2.9f+Vector3.down*2.4f,Vector3.Lerp(over-flank*3.2f+Vector3.down*4,into,.6f)},.24f,rng,0,1,false,2.6f);
                var drawn=kit.Built("Snagvine cable "+number+" "+(k+1),cable.Slot(1),Snag,SnagGlow);drawn.name="Snagvine cable "+(k+1);drawn.transform.SetParent(snag.transform,true);drawn.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                held.cables[k]=drawn;
            }

            // The knots are an encounter's enemies, stood on show from the start (inert until the party comes within reach of the barge) so the barge is visibly held from across the lane.
            var encounter=WorldBuilder.Encounter(held.knotsEncounter,at,new Vector3(58,6,58),"SnagvineNode",1);
            var first=encounter.spawners[0];first.transform.position=knots[0];first.radius=0;
            encounter.spawners=new[]{first,ZoneKit.Spawn(encounter,"SnagvineNode",1,knots[1],0),ZoneKit.Spawn(encounter,"SnagvineNode",1,knots[2],0)};
            var show=new GameObject("Knots on show").transform;show.SetParent(encounter.transform,false);show.position=at;encounter.previewPoint=show;encounter.previewRadius=0;
            // What the objective's arrow finds: the story step is three reports named SeedBarge, and the panel points at the nearest thing still wanted under that name. This point is
            // wanted until the barge's own flag is set (by the barge, when its knots are gone) and is never offered: within its reach the knots' fight has already begun.
            var wanted=kit.Discovery("Seed barge "+number+" is held",new Vector3(knots[0].x,ZoneKit.FlightPlane,knots[0].z),"Cut the snagvine knots",held.freedFlag,"SeedBarge",null,"Sela",null,0,7);
            wanted.previewEncounter=encounter;wanted.transform.SetParent(snag.transform,true);

            // Podskimmers stripping it: on the lane side, so they meet the party before the knots do.
            var swarm=kit.FlightFight("Barge"+number+"_Skimmers",at+flank*16,new Vector3(46,6,40),("Podskimmer",n==2?4:5));swarm.transform.rotation=Quaternion.Euler(0,site.yaw,0);

            // Hulljack's claim tag, on the hull's far flank and travelling with it: offered only while Teo's side quest wants the tags burned off.
            var tagAt=at-flank*2.9f+Quaternion.Euler(0,site.yaw,0)*Vector3.forward*(n==1?-3:3);
            var tag=kit.Discovery("Claim tag on barge "+number,new Vector3(tagAt.x,ZoneKit.FlightPlane,tagAt.z),"Burn off the claim tag","xylos.tag"+number,"ClaimTag",null,"Sela","ClaimTags",0,6.5f);tag.situation="tag";
            tag.transform.SetParent(barge.transform,true);
            var plate=GameObject.CreatePrimitive(PrimitiveType.Cube);plate.name="Claim tag";UnityEngine.Object.DestroyImmediate(plate.GetComponent<Collider>());plate.transform.SetParent(tag.transform,false);
            plate.transform.localPosition=new Vector3(0,1.2f,0);plate.transform.localScale=new Vector3(1.5f,.9f,.2f);plate.transform.rotation=Quaternion.LookRotation(flank);plate.GetComponent<Renderer>().sharedMaterial=ClaimTag;
            var blink=new GameObject("Claim tag light",typeof(Light));blink.transform.SetParent(plate.transform,false);blink.transform.localPosition=Vector3.back*2;
            var l=blink.GetComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.45f,.12f);l.range=9;l.intensity=3;l.shadows=LightShadows.None;
            tag.visual=plate;plate.SetActive(false);

            if(n==0)WorldBuilder.Label("SEED BARGE",at+flank*8+Vector3.down*2,6).transform.SetParent(snag.transform,true);
            if(n==2)
            {
                // The far barge: Hulljack's drones are tagging it from the side the lane doesn't see. Going round to the tag is going to them; cutting the knots from the lane isn't.
                var far=at-flank*26;
                var claim=kit.FlightFight("Barge3_Claim",far,new Vector3(52,6,26),("ClaimDrone",3));claim.transform.rotation=Quaternion.LookRotation(flank);
                foreach(var spawner in claim.spawners)spawner.transform.position=far-flank*4;
                ClaimStake(kit,at,flank,Quaternion.Euler(0,site.yaw,0));
            }
        }
        /// <summary>What Hulljack has staked out on the far barge's far side, so that side of the lane has a subject: four claim buoys fencing a volume with lines of orange light, a
        /// tow line from the barge's hull out to a marker drum, and along the tow the cargo pods already cut out of the barge, netted, a claim tag lit on every one. All of it below
        /// the plane and drawn only: the claim drones' fight is flown over it. The first thing in the lane that is plainly somebody's, and plainly not the growers'.</summary>
        static void ClaimStake(ZoneKit kit,Vector3 at,Vector3 flank,Quaternion hullTurn)
        {
            var root=new GameObject("Hulljack's claim");var fore=hullTurn*Vector3.forward;Color grey=new(.5f,.52f,.56f),dark=new(.4f,.42f,.47f);
            Vector3 Out(float along,float away,float deep){var p=at-flank*away+fore*along;p.y=deep;return p;}
            var corners=new[]{Out(-9,18,-3.8f),Out(9,19,-4.2f),Out(10,36,-3.6f),Out(-8,35,-4f)};var fence=new ZoneKit.Shape();
            for(int k=0;k<corners.Length;k++)
            {
                var buoy=kit.Loose(ZoneKit.Kit("DressSurveyBeacon","HushwellMarker"),corners[k],2.6f,Quaternion.Euler(0,k*37,0),new Color(.62f,.64f,.7f));buoy.name="Claim buoy";buoy.transform.SetParent(root.transform,true);
                Vector3 head=corners[k]+Vector3.up*2.9f,next=corners[(k+1)%corners.Length]+Vector3.up*2.9f;
                fence.Tube(new[]{head,next},new[]{.045f,.045f},5,Color.white,Color.white,1,1,false).Lump(head,Vector3.one*.2f,k,.05f,Color.white,Color.white,7,4);
                var lamp=new GameObject("Claim buoy light",typeof(Light));lamp.transform.SetParent(buoy.transform,true);lamp.transform.position=head;
                var glow=lamp.GetComponent<Light>();glow.type=LightType.Point;glow.color=new Color(1,.5f,.14f);glow.range=11;glow.intensity=3;glow.shadows=LightShadows.None;
            }
            Vector3 hitch=at-flank*2.2f+Vector3.down*1.4f,marker=Out(1,30,-3.6f);var tow=new ZoneKit.Shape();var rng=new System.Random(77);
            Vector3 Tow(float t)=>Vector3.Lerp(hitch,marker,t)+Vector3.down*1.6f*4*t*(1-t)+fore*Mathf.Sin(t*Mathf.PI*2)*1.2f;
            var path=new List<Vector3>();for(int k=0;k<=14;k++)path.Add(Tow(k/14f));
            tow.Slot(0).Tube(path,new[]{.11f},6,grey,dark,1,2,false);
            for(int k=0;k<6;k++)
            {
                // Strung along the outer half of it, clear of the weed under the hull's side.
                var tie=Tow(.5f+k*.08f);var pod=tie+Vector3.down*(1.3f+.3f*(k%2));var turn=Quaternion.Euler(rng.Next(40)-20,rng.Next(360),rng.Next(40)-20);
                tow.Slot(0).Tube(new[]{tie,pod+Vector3.up*.7f},new[]{.05f,.05f},4,grey,grey,1,2,false);
                tow.Slot(1).Lump(pod,new Vector3(.95f,.78f,1.25f),k+5,.07f,new Color(1,.93f,.72f),new Color(.66f,.56f,.4f),10,6,.25f,turn);
                // Netted: two bands of grey line round each pod.
                foreach(float across in new[]{-.4f,.4f})
                {
                    var band=new Vector3[11];float girth=Mathf.Sqrt(1-across*across/1.56f);
                    for(int j=0;j<11;j++){float a=j*Mathf.PI*2/10;band[j]=pod+turn*new Vector3(Mathf.Cos(a)*.99f*girth,Mathf.Sin(a)*.82f*girth,across);}
                    tow.Slot(0).Tube(band,new[]{.05f},5,dark,dark,1,2,false);
                }
                tow.Slot(2).Box(pod+Vector3.up*.9f,new Vector3(.95f,.07f,.62f),Quaternion.Euler(-24,k*50,0),Color.white);
            }
            // Hulljack's marker at the tow's end: a drum with a lit band, and over it the claim itself on a board big enough to read from the lane.
            tow.Slot(0).Tube(new[]{marker+Vector3.down*1.6f,marker+Vector3.down*1.1f,marker+Vector3.up*1.1f,marker+Vector3.up*1.6f},new[]{.5f,1.15f,1.15f,.5f},10,dark,new Color(.6f,.62f,.68f),3,.4f)
                .Tube(new[]{marker+Vector3.up*1.5f,marker+Vector3.up*2.2f},new[]{.09f,.09f},5,dark,dark,1,1,false);
            tow.Slot(2).Tube(new[]{marker+Vector3.down*.22f,marker+Vector3.up*.22f},new[]{1.19f,1.19f},10,Color.white,Color.white,1,1,false).Box(marker+Vector3.up*2.3f,new Vector3(3.2f,.1f,1.9f),Quaternion.Euler(-30,hullTurn.eulerAngles.y+90,0),Color.white);
            var towed=kit.Built("Claim tow",tow.Slot(2),Cloth,Husk,ClaimTag);towed.transform.SetParent(root.transform,true);towed.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            var fenced=kit.Built("Claim fence",fence,ClaimLine);fenced.transform.SetParent(root.transform,true);fenced.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            var mark=new GameObject("Claim marker light",typeof(Light));mark.transform.SetParent(root.transform,true);mark.transform.position=marker+Vector3.up*3;
            var beam=mark.GetComponent<Light>();beam.type=LightType.Point;beam.color=new Color(1,.5f,.14f);beam.range=16;beam.intensity=3.4f;beam.shadows=LightShadows.None;
        }
        /// <summary>The middle of what Hulljack has staked out, off the far barge's far side and beyond the weed that holds it.</summary>
        internal static Vector3 ClaimSide=>BargeAt(2)-LaneSide(2)*27;
        static Material ClaimLine=>ZoneKit.Toon("xylos-claim-line",null,m=>{m.SetColor("_BaseColor",new Color(1,.42f,.1f));m.SetColor("_EmissionColor",new Color(1.5f,.42f,.05f));});
        static Material ClaimTag=>ZoneKit.Toon("xylos-claim-tag",null,m=>{m.SetColor("_BaseColor",new Color(1,.5f,.14f));m.SetColor("_EmissionColor",new Color(2.6f,1.1f,.2f));});

        // ---------------------------------------------------------------- the weather mirror
        /// <summary>The landmark: a disc 190 m across lying under the lane north of it, tilted to the camera, cracked clean across with one half slipped, its panes parted along every
        /// seam and one still catching the sun. Loose panes ride up through the flight plane round the second barge: cover, and tighter flying. Built here, pane by pane; there is no model to wait for.</summary>
        static void WeatherMirror(ZoneKit kit)
        {
            var centre=Beside(MirrorS,-62,-34);var tilt=Quaternion.Euler(-16,HeadingAt(MirrorS)-20,7);var rng=new System.Random(71);
            var shape=new ZoneKit.Shape();float[] rings={0,30,62,95};int[] sectors={5,9,13};
            Vector3 Place(float radius,float angle,float lift,bool slipped)
            {
                var flat=new Vector3(Mathf.Cos(angle)*radius,lift,Mathf.Sin(angle)*radius);
                // The crack: everything one side of a chord has slid along it and dropped.
                if(slipped)flat+=new Vector3(9,-5,-4);
                return centre+tilt*flat;
            }
            int lit=rng.Next(sectors[1]);
            for(int ring=0;ring<3;ring++)for(int k=0;k<sectors[ring];k++)
            {
                float a0=(k+.06f)*Mathf.PI*2/sectors[ring]+ring*.37f,a1=(k+.94f)*Mathf.PI*2/sectors[ring]+ring*.37f,r0=rings[ring]+(ring==0?0:1.6f),r1=rings[ring+1]-1.6f;
                float lift=((float)rng.NextDouble()-.5f)*2.4f,mid=(a0+a1)*.5f;bool slipped=Mathf.Cos(mid-.5f)*((r0+r1)*.5f)<-6,gone=ring==2&&rng.Next(7)==0;float shade=.62f+.3f*(float)rng.NextDouble();
                if(gone)continue;
                bool caught=ring==1&&k==lit;var tone=caught?Color.white:new Color(shade*.9f,shade,shade*1.08f);var normal=tilt*Vector3.up;
                shape.Slot(caught?1:0);
                int steps=ring==2?3:2;
                for(int j=0;j<steps;j++)
                {
                    float b0=Mathf.Lerp(a0,a1,j/(float)steps),b1=Mathf.Lerp(a0,a1,(j+1)/(float)steps);
                    if(r0<.5f)shape.Triangle(Place(0,b0,lift,slipped),Place(r1,b0,lift,slipped),Place(r1,b1,lift,slipped),normal,tone,.03f);
                    else shape.Quad(Place(r0,b0,lift,slipped),Place(r1,b0,lift,slipped),Place(r1,b1,lift,slipped),Place(r0,b1,lift,slipped),normal,tone,.03f);
                }
            }
            // Its frame: a rim in broken arcs and the spokes the panes sat on, dark under the glass.
            var frame=new Color(.2f,.24f,.3f);shape.Slot(2);
            foreach(var (from,to) in new[]{(.1f,1.9f),(2.3f,3.6f),(4.0f,5.9f)})
            {
                var arc=new List<Vector3>();var thick=new List<float>();for(float a=from;a<=to;a+=.12f){arc.Add(Place(97,a,-1.2f,Mathf.Cos(a-.5f)*97<-6));thick.Add(1.5f);}
                shape.Tube(arc,thick,6,frame,frame,1,.1f);
            }
            for(int k=0;k<6;k++){float a=k*Mathf.PI/3+.2f;shape.Tube(new[]{Place(4,a,-1.6f,false),Place(96,a,-1.6f,Mathf.Cos(a-.5f)*96<-6)},new[]{1.1f,.8f},5,frame,frame,1,.1f);}
            var mirror=kit.Built("Weather mirror",shape.Slot(2),Pane,PaneLit,Cloth);mirror.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            // The pane that still works throws its light up at the lane.
            var sun=new GameObject("Caught sun",typeof(Light));sun.transform.SetParent(mirror.transform,true);sun.transform.position=centre+tilt*new Vector3(Mathf.Cos((lit+.5f)*Mathf.PI*2/sectors[1]+.37f)*46,14,Mathf.Sin((lit+.5f)*Mathf.PI*2/sectors[1]+.37f)*46);
            var glow=sun.GetComponent<Light>();glow.type=LightType.Point;glow.color=new Color(1,.92f,.7f);glow.range=70;glow.intensity=3;glow.shadows=LightShadows.None;

            // Loose panes up at the flight plane, round where the second barge lies: solid, each a slab a ship can put between itself and a swarm.
            var loose=new GameObject("Loose mirror panes");var barge=BargeAt(1);
            foreach(var (dx,dz,size,yaw,roll) in new[]{(-20f,-16f,8f,20f,24f),(15f,-22f,10f,-50f,-18f),(24f,9f,7f,70f,30f),(-9f,22f,9f,-15f,-26f),(-30f,6f,6.5f,110f,16f),(6f,34f,8.5f,40f,-22f),(36f,-9f,6f,-80f,28f)})
            {
                var at=new Vector3(barge.x+dx,ZoneKit.FlightPlane,barge.z+dz);var turn=Quaternion.Euler(roll,yaw,roll*.4f);
                var pane=new ZoneKit.Shape().Slot(0).Box(Vector3.zero,new Vector3(size,.5f,size*.62f),Quaternion.identity,new Color(.8f,.86f,.95f),null,.08f).Slot(1).Box(Vector3.zero,new Vector3(size+.5f,.34f,size*.62f+.5f),Quaternion.identity,frame);
                var slab=kit.Built("Loose pane "+dx+" "+dz,pane,Pane,Cloth);slab.transform.SetParent(loose.transform,true);slab.transform.SetPositionAndRotation(at,turn);
                // Its box stands up through the plane whatever way the pane has rolled: a ship can't slip under a slab it sees edge on.
                var box=slab.AddComponent<BoxCollider>();box.size=new Vector3(size,3.4f,size*.62f);slab.isStatic=true;
            }
            var look=kit.Discovery("The weather mirror",Beside(MirrorS,-14,0)+Vector3.up,"Look at the weather mirror","xylos.mirrorSeen","","SelaWeatherMirror","Sela",null,0,7.5f);
            WorldBuilder.Label("WEATHER MIRROR",Beside(MirrorS,-14,-1)+new Vector3(0,0,-5),6);
        }

        // ---------------------------------------------------------------- nests and the beacon
        /// <summary>A podskimmer nest off the lane: husks they have stripped and hung together, a swarm at home, and what they have hoarded.</summary>
        static void Nest(ZoneKit kit,int n)
        {
            var at=Beside(Nests[n].s,Nests[n].side);var rng=new System.Random(140+n);var shape=new ZoneKit.Shape();
            for(int k=0;k<14;k++)
            {
                float a=k*.7f+n,reach=4.5f+5*(float)rng.NextDouble();var hang=at+new Vector3(Mathf.Cos(a)*reach,-3f-3.5f*(float)rng.NextDouble(),Mathf.Sin(a)*reach);float size=.9f+.9f*(float)rng.NextDouble();
                shape.Slot(0).Lump(hang,new Vector3(size,size*1.25f,size),k+n*20,.16f,Color.white,new Color(.5f,.44f,.4f),9,6);
                shape.Slot(1).Tube(new[]{hang+Vector3.up*size,at+Vector3.down*1.5f+(hang-at)*.25f},new[]{.12f,.12f},4,Color.white,Color.white,1,.5f,false);
            }
            for(int k=0;k<6;k++)shape.Slot(2).Lump(at+new Vector3(Mathf.Cos(k*1.1f)*2.2f,-2.2f-.3f*k,Mathf.Sin(k*1.1f)*2.2f),Vector3.one*.6f,60+k,.08f,new Color(1,.95f,.8f),new Color(.7f,.62f,.5f),7,4);
            var nest=kit.Built("Podskimmer nest "+(n+1),shape.Slot(2),Husk,Vine,Cloth);nest.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            kit.FlightFight("Nest_"+(n+1),at,new Vector3(40,6,40),("Podskimmer",5));
            // The hoard: one whole seed pod with a lantern's worth of sap still in it, at the plane where a ship can reach it.
            var pod=kit.Built("Nest hoard "+(n+1),new ZoneKit.Shape().Lump(Vector3.zero,new Vector3(1,1.3f,1),7,.08f,new Color(1,.9f,.6f),new Color(.8f,.6f,.3f),10,6),Lantern);pod.transform.position=at;
            var hoard=n==0?LootCache.On(pod,"xylos.nest1","Podskimmer hoard",120,ProgressionBuilder.Drop("AmberSap",3,1),ProgressionBuilder.Drop("Rootfibre",2,1))
                :LootCache.On(pod,"xylos.nest2","Podskimmer hoard",90,ProgressionBuilder.AnyPart(3,GearSlot.Emitter,1),ProgressionBuilder.Drop("AmberSap",2,1));
            hoard.range=WorldBuilder.FlightDockRange;
        }
        /// <summary>The delta beacon: root pylons round the landing mark, their seed lanterns dark until the three barges are free. Below the plane and drawn only.</summary>
        static void DeltaBeacon(ZoneKit kit,Vector3 at)
        {
            var shape=new ZoneKit.Shape();var lamps=new ZoneKit.Shape();
            for(int k=0;k<5;k++)
            {
                float a=k*Mathf.PI*2/5+.3f;var foot=at+new Vector3(Mathf.Cos(a)*9,-9,Mathf.Sin(a)*9);var head=at+new Vector3(Mathf.Cos(a)*6.4f,-1.2f,Mathf.Sin(a)*6.4f);
                shape.Tube(new[]{foot,Vector3.Lerp(foot,head,.5f)+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*1.6f,head},new[]{.9f,.6f,.34f},7,new Color(.7f,.7f,.7f),Color.white,1,.3f);
                lamps.Lump(head+Vector3.up*.5f,Vector3.one*.62f,k,.05f,Color.white,Color.white,8,5);
            }
            // The bole the pylons stand in: a mound wound of roots, each running out from a stump in the middle and down over the mound's edge. A squashed lump of bark here read
            // from above as a wooden table top, its texture pinched to a point in the middle.
            var bole=at+Vector3.down*10.5f;
            shape.Lump(bole+Vector3.down*.7f,new Vector3(5.6f,1.4f,5.6f),4,.16f,new Color(.5f,.48f,.46f),new Color(.26f,.24f,.23f),28,12,.12f);
            shape.Tube(new[]{bole+Vector3.up*.2f,bole+Vector3.up*1.2f,bole+Vector3.up*1.9f},new[]{2.3f,1.8f,1.2f},14,new Color(.66f,.62f,.58f),new Color(.86f,.82f,.76f),3,.3f);
            for(int k=0;k<14;k++)
            {
                float a=k*Mathf.PI*2/14+.21f*(k%3),reach=6.4f+.35f*((k*7)%5);var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var side=Vector3.Cross(Vector3.up,dir)*(k%2==0?.6f:-.6f);
                shape.Tube(new[]{bole+Vector3.up*1.4f+dir*1.1f,bole+Vector3.up*1.2f+dir*reach*.42f+side,bole+Vector3.up*.1f+dir*reach*.82f+side*.5f,bole+Vector3.down*1.6f+dir*reach},
                    new[]{.95f,.78f,.5f,.14f},8,new Color(.74f,.7f,.64f),new Color(.92f,.88f,.82f),1,.3f);
            }
            kit.Built("Delta beacon",shape,Bark).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            var dark=kit.Built("Delta beacon lanterns dark",lamps,Cloth);dark.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;dark.AddComponent<PropTint>().tint=new Color(.3f,.27f,.22f);
            var lit=new GameObject("Delta beacon lit");var lanterns=UnityEngine.Object.Instantiate(dark,lit.transform);lanterns.name="Delta beacon lanterns";UnityEngine.Object.DestroyImmediate(lanterns.GetComponent<PropTint>());lanterns.GetComponent<MeshRenderer>().sharedMaterial=Lantern;
            var light=new GameObject("Delta beacon light",typeof(Light));light.transform.SetParent(lit.transform,false);light.transform.position=at+Vector3.up*2;
            var l=light.GetComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.8f,.45f);l.range=30;l.intensity=3.2f;l.shadows=LightShadows.None;
            var opens=new GameObject("The delta beacon lights",typeof(FlagGate)).GetComponent<FlagGate>();opens.flag="xylos.bargesFree";opens.targets=new[]{lit};lit.SetActive(false);
            var unlit=new GameObject("The delta beacon is dark",typeof(FlagGate)).GetComponent<FlagGate>();unlit.flag="xylos.bargesFree";unlit.showWhenSet=false;unlit.targets=new[]{dark};
            WorldBuilder.Label("DELTA BEACON",at+new Vector3(0,-1,-9),6);
        }
    }
}
