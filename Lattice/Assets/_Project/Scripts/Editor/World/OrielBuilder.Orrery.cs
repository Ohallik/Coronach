using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>The Orrery (space combat; rule: one readable machine). Map brief, the machine's rule, coordinates and the combat engineer's contract: docs/maps/Orrery.md. The machine
    /// itself runs in <see cref="OrreryRail"/>; everything here lays it from the same numbers. Everything of the zone's own is in <see cref="Orrery"/>, so nothing here can collide
    /// with the other Oriel files' members.</summary>
    public static partial class OrielBuilder
    {
        static void BuildOrrery()=>Orrery.Build();

        internal static class Orrery
        {
            // ---------------------------------------------------------------- the machine's numbers
            /// <summary>Oriel's centre on the plan: every rail is a circle round it, and the lane runs straight in toward it from the arrival.</summary>
            internal static readonly Vector2 Centre=new(0,1300);
            internal const float Half=36,Plane=ZoneKit.FlightPlane,GateHalf=6,RingDrop=2.6f,RingTall=27,LaneEnd=900;
            /// <summary>Each ring leans back over its rail by this many degrees: stood upright, a ring 27 m tall filled the whole flight frame and hid the moon going by behind it; leaning,
            /// it reads as an arch over the rail, and the moon passes under it.</summary>
            internal const float Tilt=28;
            static float Sin(float degrees)=>Mathf.Sin(degrees*Mathf.Deg2Rad);static float Cos(float degrees)=>Mathf.Cos(degrees*Mathf.Deg2Rad);
            internal const float HailZ=20,TalkZ=90,FarGateZ=850,FromCampZ=866,LandZ=880;
            /// <summary>The three gates of a row inside the lane, and the rings that go on round the rail past the lane's walls (drawn only), as arc lengths along the row.</summary>
            internal static readonly float[] LaneGates={-24,0,24},PastWalls={-52,52,-80,80,-108,108};
            /// <summary>One rail: its moon's name, where it crosses the lane, its moon's speed, radius and direction (+1 east), what it looks like, and what else it carries.</summary>
            internal readonly struct Rail
            {
                public readonly string name,flag;public readonly float z,speed,moonRadius;public readonly int direction;public readonly Color tone;public readonly bool named,broken;
                public Rail(string name,float z,float speed,float moonRadius,int direction,Color tone,string flag=null,bool broken=false)
                {this.name=name;this.z=z;this.speed=speed;this.moonRadius=moonRadius;this.direction=direction;this.tone=tone;this.flag=flag;named=flag!=null;this.broken=broken;}
                public float Radius=>Centre.y-z;
                /// <summary>How far outside the rail each ring's middle stands: as near as the leaning ring can be and still clear the moon passing under it by 1.3 m.</summary>
                public float RingOut=>(moonRadius+1.3f+RingDrop*Sin(Tilt))/Cos(Tilt);
                /// <summary>Where the leaning rings cross the flight plane, outside the rail: the gate row a ship goes through. Near enough that a ship waiting at a gate sees the moon go
                /// by beyond it (the frame shows about 12 m north of the ship).</summary>
                public float GateOut=>RingOut-RingDrop*Mathf.Tan(Tilt*Mathf.Deg2Rad);
                /// <summary>The machine's green wave: each moon crosses the lane about eleven metres a second after the one before it, the first four seconds after the zone opens.</summary>
                public float Phase=>Mathf.Repeat(4+(z-160)/11,OrreryRail.Period);
            }
            /// <summary>The nine moons, outermost first, in the order Ishen names them (manifest section 1). Neighbouring rails turn opposite ways, like gears.</summary>
            internal static readonly Rail[] Rails=
            {
                new("The Steward",160,8,5.5f,-1,new(.94f,.9f,.8f),"oriel.stewardRead"),
                new("The Weigher",250,9.5f,5.5f,1,new(.7f,.76f,.88f),"oriel.weigherRead"),
                new("The Clerk of Roads",340,9,5,-1,new(.9f,.7f,.46f),null,true),
                new("The Lamplighter",430,10.5f,5.5f,1,new(1f,.84f,.56f),"oriel.lamplighterRead"),
                new("The Reeve",520,10,5,-1,new(.84f,.58f,.48f)),
                new("The Second Gardener",552,10,5,1,new(.7f,.84f,.64f)),
                new("The Bellfounder",640,12,6,-1,new(.6f,.8f,.74f)),
                new("The Almoner",730,12.5f,5.5f,1,new(.84f,.76f,.94f)),
                new("The Warden",820,13,7,-1,new(.58f,.58f,.66f)),
            };
            /// <summary>Ishen's three later names (hail.moon), each a little short of its rail's gates.</summary>
            internal static readonly (int rail,string flag)[] LaterNames={(6,"oriel.moon7Named"),(7,"oriel.moon8Named"),(8,"oriel.moon9Named")};
            /// <summary>A point of the plan at this arc length on a circle of this radius round the centre (as <see cref="OrreryRail.On"/>).</summary>
            internal static Vector2 On(float along,float radius){float a=along/radius;return Centre+new Vector2(Mathf.Sin(a),-Mathf.Cos(a))*radius;}
            internal static Vector3 At(float along,float radius,float y=Plane){var p=On(along,radius);return new Vector3(p.x,y,p.y);}
            internal static Vector3 Tangent(float along,float radius){float a=along/radius;return new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));}
            internal static Vector3 Gate(int rail,int gate)=>At(LaneGates[gate],Rails[rail].Radius+Rails[rail].GateOut);
            /// <summary>Where a route waits in a moon's lee, braked, short of a rail's middle gate (tools/oriel_routes.py, <c>cross</c>): six metres out from the row.</summary>
            internal static Vector3 LeeWait(int rail)=>Gate(rail,1)+Vector3.back*6;
            /// <summary>How much of a rail-gate's ring is drawn open at its top, in degrees either side of straight up: a ship waiting at a gate saw the whole ring and nothing of the
            /// moon going by beyond it. What stays is the ring wherever it crosses the plane (the ring's own solid), its lit lower arc and its plinth; a lattice arch of two slim
            /// chords closes the gap, so it still reads as a ring.</summary>
            internal const float OpenAbove=56;
            /// <summary>How much of the ring's band is drawn outside its inner edge: everything of the ring past its opening (band, lamp panels, plinth, the bar's ends) drawn this much
            /// nearer the opening. Whole, a ring 22 m across filled the frame of a ship waiting six metres short of it. The opening at the plane is the ring's own, the solid
            /// rims stand where they stood, and the row's truss is drawn out to the slimmer band.</summary>
            internal const float Slim=.4f;
            /// <summary>Oriel: its centre and radius. The game's camera draws nothing past 1000 m, and the first Oriel (380 m across its radius, 800 m down) was wholly past it: nobody
            /// flying the lane ever saw the planet. Here every point of it the camera can see is inside 960 m from the moment its rim comes into the top of the frame (between
            /// rails 4 and 5) to the landing, the camera drawn back to 60 m included; it grows from there to half the frame at the far gate, east of the lane and up, so the ships
            /// at the frame's middle fly over its night side.</summary>
            internal static readonly Vector3 OrielAt=new(80,-360,1300);internal const float OrielRadius=165;
            /// <summary>The flight camera's eye with the lead at this point (Flight: 48 degrees down, 26 m back; the profile's own numbers when it is there).</summary>
            static Vector3 EyeOver(Vector3 lead)
            {
                var profile=Find<CameraProfile>("Flight");float pitch=profile!=null?profile.pitch:ZoneKit.FlightPitch,back=profile!=null?profile.distance:26;
                return lead-Quaternion.Euler(pitch,0,0)*Vector3.forward*back;
            }
            internal static Vector3 Warden(int rail,int side)=>At(side*30,Rails[rail].Radius-8.5f);
            /// <summary>Where to stand a thing <paramref name="depth"/> metres under the plane so that it shows on screen at this point of the plane (the flight camera looks down at 48 degrees).</summary>
            static Vector3 ShowingAt(Vector2 onPlane,float depth)=>new(onPlane.x,Plane-depth,onPlane.y+depth/Mathf.Tan(ZoneKit.FlightPitch*Mathf.Deg2Rad));
            /// <summary>An enemy of the chapter's roster if the combat engineer has defined it, else a stand-in that fights the same way, so the lane is flyable before the roster lands.</summary>
            static string Foe(string id,string standIn){if(Find<EnemyDef>(id)!=null)return id;Debug.Log("ORRERY_STAND_IN "+standIn+" for "+id);return standIn;}

            /// <summary>LookDev's stills of the Orrery: the arrival, the talk, each rail's gates as a ship waits at them, the double rail, the far gate and the landing, and wide views of the machine.</summary>
            internal static LookDev.View[] Views
            {
                get
                {
                    var views=new List<LookDev.View>{new("arrival",new Vector3(0,1,8),true),new("hail",new Vector3(0,1,26),true),new("talk",new Vector3(0,1,86),true),new("machine",new Vector3(0,1,190),true,5.2f)};
                    for(int i=0;i<Rails.Length;i++){var g=Gate(i,1);views.Add(new($"rail-{i+1}",g+new Vector3(0,0,-4),true));views.Add(new($"rail-{i+1}-east",Gate(i,2)+new Vector3(0,0,-3),true));
                        // Where the routes wait in the moon's lee: the frame a player stops in at every rail.
                        views.Add(new($"lee-{i+1}",LeeWait(i),true));}
                    views.Add(new("between-5-6",new Vector3(0,1,532),true));views.Add(new("lane-0380",new Vector3(-10,1,380),true));views.Add(new("lane-0600",new Vector3(12,1,595),true));
                    views.Add(new("far-gate",new Vector3(0,1,852),true));views.Add(new("landing",new Vector3(0,1,876),true));
                    views.Add(new("inner-wide",new Vector3(0,1,760),true,6));views.Add(new("overview",new Vector3(0,1,480),true,16));
                    return views.ToArray();
                }
            }

            // ---------------------------------------------------------------- the zone
            internal static void Build()
            {
                var kit=ZoneKit.Flight(OrreryZone);
                var scene=WorldBuilder.Begin(kit.zone);
                var stuff=Materials(kit);
                WorldBuilder.Spawn("Arrival",new Vector3(0,1,6));WorldBuilder.Spawn(SkyfieldsZone,new Vector3(0,1,FromCampZ));
                kit.DriftingSky();

                // What must stay readable on screen, each with the room it needs: the ways, the talk, every gate row and every rail's crossing of the lane.
                var keep=new List<(Vector2 at,float clear)>{(new(-14,-4),7),(new(0,6),4),(new(0,LandZ),9),(new(0,FarGateZ),6)};
                foreach(var rail in Rails)for(float x=-30;x<=30;x+=10){keep.Add((new Vector2(x,rail.z-rail.GateOut),5));keep.Add((new Vector2(x,rail.z),rail.moonRadius+1.5f));}

                var gateMeasure=MeasureRing();var opened=OpenRing(kit,gateMeasure);
                var rails=new OrreryRail[Rails.Length];
                for(int i=0;i<Rails.Length;i++)rails[i]=LayRail(kit,stuff,i,gateMeasure,opened);
                Armature(kit,stuff,gateMeasure,opened.footBelow>0?opened.footBelow:gateMeasure.middle);Debris(kit,stuff,keep);Planet(kit,stuff,keep);
                var glow=kit.Glow("Motes",false,new Color(1.3f,1.2f,1f,1));
                for(float z=0;z<LaneEnd;z+=180){var dust=kit.Drift("Drifting motes",new Vector3(0,-3,z+90),new Vector3(Half*2+12,14,180),glow,new Color(1,.92f,.76f,.28f),380,.05f,.18f,.12f,18);}

                // ---- the ways in and out
                WorldBuilder.Warp("Warp — The Hull of Many Hands",new Vector3(-14,1,-4),"ManyHandsHull","","Oriel").lockedPrompt="";
                WorldBuilder.Label("Warp — The Hull of Many Hands",new Vector3(-14,.3f,-11),2.2f);
                var land=WorldBuilder.Warp("Land — The Skyfields",new Vector3(0,1,LandZ),SkyfieldsZone,"","Arrival");land.lockedPrompt="";land.range=WorldBuilder.FlightDockRange;
                WorldBuilder.Label("Land — The Skyfields",new Vector3(0,.3f,LandZ-8),2.6f);

                // ---- the story along it
                Hail("Lume",new[]{"hail.pod"},HailZ,"oriel.podHailed",30);
                var talk=kit.Crossing("Ishen names the moons",new Vector3(0,0,TalkZ),new Vector3(Half*2+2,8,4),"oriel.lightsRead","IshenMoonNames","Ishen");
                // As the lead comes up to the talk the view draws back over the first two rails, so the machine is on screen whole while Ishen names its parts and teaches its lights.
                var vantage=new GameObject("The machine in view",typeof(VantageView));vantage.transform.position=new Vector3(0,1,TalkZ);
                var view=vantage.GetComponent<VantageView>();view.enter=TalkZ-40;view.exit=TalkZ-25;view.untilFlag="oriel.lightsRead";
                view.shows=new Bounds(new Vector3(0,0,(Rails[0].z+Rails[1].z)*.5f-10),new Vector3(130,10,Rails[1].z-Rails[0].z+40));
                string needed="oriel.lightsRead";
                foreach(var (rail,flag) in LaterNames){Hail("Ishen",new[]{"hail.moon"},Rails[rail].z-Rails[rail].GateOut-26,flag,30,needed);needed=flag;}
                var far=kit.Crossing("The far gate",new Vector3(0,0,FarGateZ),new Vector3(Half*2+2,8,4),"oriel.crossed","","Sela");far.report="FarGate";far.situation="";
                CourierPod(kit,stuff);

                kit.FlightCorridor(new[]{new Vector2(0,0),new Vector2(0,LaneEnd)},Half);
                WorldBuilder.Save(scene,kit.zone);
                Debug.Log("ORIEL_COORDS Orrery arrival=(0,1,6) home=(-14,1,-4) hail=(0,1,"+HailZ+") talk=(0,1,"+TalkZ+") far-gate=(0,1,"+FarGateZ+") from-camp=(0,1,"+FromCampZ+") land=(0,1,"+LandZ+") "
                    +string.Join(" ",Rails.Select((r,i)=>$"rail{i+1}[{r.name}] z={r.z} gates={string.Join("/",LaneGates.Select((g,k)=>Gate(i,k).ToString("0.0")))} wardens={Warden(i,-1):0.0}/{Warden(i,1):0.0} phase={r.Phase:0.0} out={r.GateOut:0.0}")));
                Debug.Log("ORRERY_OK");
            }

            /// <summary>Lume's and Ishen's lines over the flying: a few barks once the lead is near, once a save.</summary>
            static void Hail(string speaker,string[] situations,float z,string heard,float range,params string[] needs)
            {
                var hail=new GameObject(speaker+" on the radio: "+string.Join(",",situations),typeof(RadioHail)).GetComponent<RadioHail>();hail.transform.position=new Vector3(0,1,z);
                hail.speaker=speaker;hail.situations=situations;hail.needs=needs;hail.heardFlag=heard;hail.silencedBy="";hail.range=range;
            }

            // ---------------------------------------------------------------- a rail, its moon, its gates
            /// <summary>The ring's own proportions, measured off its mesh: the height its middle stands at (where its bar sticks furthest out), and the half widths of its opening and of the
            /// ring itself at the height a ship passes through it. With these the solid rims stand exactly where the bronze is.</summary>
            internal readonly struct RingFit{public readonly float middle,face,inner,outer;public RingFit(float m,float f,float i,float o){middle=m;face=f;inner=i;outer=o;}}
            static RingFit MeasureRing()
            {
                var probe=WorldBuilder.Piece(ZoneKit.Kit("RailGate","WarpBeacon"),Vector3.zero,Vector3.one*RingTall,"Rock",false);
                try
                {
                    var bounds=ModelGeometry.BoundsOf(probe);var points=new List<Vector3>();
                    foreach(var filter in probe.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh!=null)foreach(var v in filter.sharedMesh.vertices)points.Add(filter.transform.TransformPoint(v)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z));
                    if(points.Count==0)return new RingFit(RingTall*.55f,0,RingTall*.2f,RingTall*.44f);
                    // The bar through the ring is at the ring's middle and is its furthest reach sideways: its tips give the ring's middle and the plane of its face.
                    var tips=points.OrderByDescending(p=>Mathf.Abs(p.x)).Take(40).ToList();float middle=tips.Average(p=>p.y),face=tips.Average(p=>p.z);
                    // Where the flight plane cuts the leaning ring (RingDrop over its middle, along the lean), what is solid across it: its opening is the widest gap round x 0.
                    float cut=middle+RingDrop/Cos(Tilt);
                    var band=points.Where(p=>Mathf.Abs(p.y-cut)<.35f&&Mathf.Abs(p.z-face)<2.5f).Select(p=>p.x).OrderBy(x=>x).ToList();
                    float inner=band.Where(x=>x<0).DefaultIfEmpty(-RingTall*.2f).Max(),innerHigh=band.Where(x=>x>0).DefaultIfEmpty(RingTall*.2f).Min();
                    float outer=band.Count>0?Mathf.Max(-band[0],band[band.Count-1]):RingTall*.44f;
                    var fit=new RingFit(middle,face,Mathf.Min(-inner,innerHigh),outer);
                    Debug.Log($"ORRERY_RING middle={fit.middle:0.00} face={fit.face:0.00} opening-half={fit.inner:0.00} rim-half={fit.outer:0.00} depth={bounds.size.z:0.00} wide={bounds.size.x:0.00} tall={bounds.size.y:0.00}");
                    return fit;
                }
                finally{UnityEngine.Object.DestroyImmediate(probe);}
            }

            /// <summary>The rail-gate drawn open at its top: each of its meshes cut along two lines out from the ring's middle, <see cref="OpenAbove"/> either side of straight up, and
            /// what was above them left out; and the arch that closes the gap, in the gate's own space: a bronze cap on each cut end and two slim chords from cap to cap, tied.</summary>
            internal sealed class Opened{public Mesh[] meshes=Array.Empty<Mesh>();public Mesh arch;public float rimHalf,footBelow;}
            static Opened OpenRing(ZoneKit kit,RingFit ring)
            {
                var probe=WorldBuilder.Piece(ZoneKit.Kit("RailGate","WarpBeacon"),Vector3.zero,Vector3.one*RingTall,"Rock",false);
                try
                {
                    var bounds=ModelGeometry.BoundsOf(probe);var foot=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                    var filters=probe.GetComponentsInChildren<MeshFilter>();var opened=new Opened{meshes=new Mesh[filters.Length]};
                    // The band where the plane cuts it, as a ring: its inner and outer radius round the ring's middle.
                    float drop=RingDrop/Cos(Tilt),inner=Mathf.Sqrt(ring.inner*ring.inner+drop*drop),outer=Mathf.Sqrt(ring.outer*ring.outer+drop*drop);
                    // A point of a mesh as the ring sees it: across, up from its middle, and through it (+ toward the rail, the way the ring leans).
                    Func<Vector3,Vector3> RingOf(Transform t)=>local=>{var p=t.TransformPoint(local)-foot;return new Vector3(p.x,p.y-ring.middle,p.z-ring.face);};
                    // How deep the band is near its top, front to back.
                    float front=float.PositiveInfinity,back=float.NegativeInfinity;
                    foreach(var filter in filters)
                    {
                        if(filter.sharedMesh==null)continue;var ringOf=RingOf(filter.transform);
                        foreach(var v in filter.sharedMesh.vertices)
                        {
                            var p=ringOf(v);float r=new Vector2(p.x,p.y).magnitude;
                            if(p.y>0&&r>inner-.3f&&r<outer+.3f&&Mathf.Abs(p.x)<p.y*Mathf.Tan((OpenAbove+12)*Mathf.Deg2Rad)){front=Mathf.Min(front,p.z);back=Mathf.Max(back,p.z);}
                        }
                    }
                    if(float.IsInfinity(front)){front=-1.2f;back=1.2f;}
                    // The plane crosses the leaning band higher on its rail side: the opening stops short of where the band's inner edge meets the plane there, so every rim a
                    // ship flies past at the plane is the ring's own bronze.
                    float highest=drop+Mathf.Max(0,back)*Mathf.Tan(Tilt*Mathf.Deg2Rad);
                    float atPlane=highest<inner?Mathf.Atan2(Mathf.Sqrt(inner*inner-highest*highest),highest)*Mathf.Rad2Deg:0;
                    float open=Mathf.Min(OpenAbove,atPlane-4);if(open<20)throw new InvalidOperationException($"Orrery: the rail-gate's band meets the plane {atPlane:0} degrees from its top: too little to open");
                    float sin=Sin(open),cos=Cos(open);int kept=0,cut=0;
                    // Slimmed: a point past the opening drawn nearer it, by its distance from the ring's middle.
                    float Slimmed(float r)=>r<=inner?r:inner+(r-inner)*Slim;
                    for(int f=0;f<filters.Length;f++)
                    {
                        var source=filters[f].sharedMesh;if(source==null)continue;var t=filters[f].transform;var ringOf=RingOf(t);
                        Vector3 Warp(Vector3 local)
                        {
                            var p=ringOf(local);float r=new Vector2(p.x,p.y).magnitude;if(r<=inner)return local;
                            float k=Slimmed(r)/r;return t.InverseTransformPoint(foot+new Vector3(p.x*k,p.y*k+ring.middle,p.z+ring.face));
                        }
                        opened.meshes[f]=ZoneKit.SaveMesh(Cut(source,ringOf,Warp,sin,cos,ref kept,ref cut),"Assets/_Project/Scenes/"+kit.zone+"-RailGate-open-"+f+".asset");
                    }
                    outer=Slimmed(outer);opened.rimHalf=Mathf.Sqrt(Mathf.Max(0,outer*outer-drop*drop));opened.footBelow=Slimmed(ring.middle);
                    // The arch, laid in the ring's own frame and turned into the wrapper's space.
                    Vector3 Out(float x,float y,float z)=>probe.transform.InverseTransformPoint(foot+new Vector3(x,y+ring.middle,z+ring.face));
                    Vector3 Round(float degrees,float radius,float z)=>Out(radius*Sin(degrees),radius*Cos(degrees),z);
                    var shape=new ZoneKit.Shape();var tone=new Color(.8f,.6f,.36f);float mid=(front+back)*.5f,deep=back-front;var through=(Out(0,0,1)-Out(0,0,0)).normalized;
                    foreach(int side in new[]{-1,1})
                    {
                        // A cap over each cut end of the band: a bronze collar across it, a little proud of it all round.
                        float a=side*open;var along=Round(a,outer+.15f,mid)-Round(a,inner-.15f,mid);
                        shape.Box((Round(a,outer+.15f,mid)+Round(a,inner-.15f,mid))*.5f,new Vector3(.5f,along.magnitude,deep+.2f),Quaternion.LookRotation(through,along.normalized),ZoneKit.Shape.Dim(tone,.9f),null,.5f);
                    }
                    foreach(float radius in new[]{inner+.35f,outer-.35f})
                    {
                        var path=new List<Vector3>();var radii=new List<float>();
                        for(float a=-open;a<=open+.01f;a+=open/12){path.Add(Round(a,radius,mid));radii.Add(.17f);}
                        shape.Tube(path,radii,8,tone,ZoneKit.Shape.Dim(tone,.92f),1,.4f,false);
                    }
                    // Ties between the chords, slanting one way and then the other: the truss's own lattice.
                    for(int k=0;k<6;k++)
                    {
                        float a=-open+k*open/3,b=a+open/3;
                        shape.Tube(new[]{Round(a,inner+.35f,mid),Round(b,outer-.35f,mid)},new[]{.09f,.09f},6,ZoneKit.Shape.Dim(tone,.85f),ZoneKit.Shape.Dim(tone,.85f),1,.4f,false);
                        shape.Tube(new[]{Round(a,outer-.35f,mid),Round(b,inner+.35f,mid)},new[]{.09f,.09f},6,ZoneKit.Shape.Dim(tone,.85f),ZoneKit.Shape.Dim(tone,.85f),1,.4f,false);
                    }
                    opened.arch=ZoneKit.SaveMesh(shape.Mesh("RailGate arch"),"Assets/_Project/Scenes/"+kit.zone+"-RailGate-arch.asset");
                    Debug.Log($"ORRERY_RING_OPEN above={open:0.0} (asked {OpenAbove}, band meets the plane at {atPlane:0.0}) inner={inner:0.00} outer={outer:0.00} (slim {Slim}) rim-half={opened.rimHalf:0.00} foot={opened.footBelow:0.00} band={front:0.00}..{back:0.00} triangles kept={kept} cut={cut}");
                    return opened;
                }
                finally{UnityEngine.Object.DestroyImmediate(probe);}
            }
            /// <summary>One vertex of a mesh being cut: everything a vertex carries, and how far it is past each of the two cutting lines (negative is kept).</summary>
            struct Corner{public Vector3 p,n;public Vector4 t;public Vector2 uv,uv2;public Color c;public float right,left;public int from;}
            static Corner Between(Corner a,Corner b,float s)=>new(){p=Vector3.Lerp(a.p,b.p,s),n=Vector3.Lerp(a.n,b.n,s).normalized,t=Vector4.Lerp(a.t,b.t,s),uv=Vector2.Lerp(a.uv,b.uv,s),uv2=Vector2.Lerp(a.uv2,b.uv2,s),
                c=Color.Lerp(a.c,b.c,s),right=Mathf.Lerp(a.right,b.right,s),left=Mathf.Lerp(a.left,b.left,s),from=-1};
            /// <summary>What of a polygon lies where <paramref name="past"/> is not above nought (Sutherland and Hodgman, one line).</summary>
            static List<Corner> Keep(List<Corner> polygon,Func<Corner,float> past)
            {
                var kept=new List<Corner>();
                for(int i=0;i<polygon.Count;i++)
                {
                    var a=polygon[i];var b=polygon[(i+1)%polygon.Count];float fa=past(a),fb=past(b);
                    if(fa<=0)kept.Add(a);
                    if(fa<0&&fb>0||fa>0&&fb<0)kept.Add(Between(a,b,fa/(fa-fb)));
                }
                return kept;
            }
            /// <summary>A copy of a mesh with what lies between two lines out from the ring's middle, <paramref name="sin"/>/<paramref name="cos"/> of the angle either side of
            /// straight up, left out: a triangle across a line is cut along it, so the edge is clean. Every point is drawn where <paramref name="warp"/> puts it (slimmed); which side of
            /// a line it is on is read where it was.</summary>
            static Mesh Cut(Mesh source,Func<Vector3,Vector3> ring,Func<Vector3,Vector3> warp,float sin,float cos,ref int keptCount,ref int cutCount)
            {
                var v=source.vertices;var n=source.normals;var tg=source.tangents;var uv=source.uv;var uv2=source.uv2;var col=source.colors;int count=v.Length;
                bool hasN=n.Length==count,hasT=tg.Length==count,hasUv=uv.Length==count,hasUv2=uv2.Length==count,hasC=col.Length==count;
                var corners=new Corner[count];
                for(int i=0;i<count;i++)
                {
                    var p=ring(v[i]);
                    corners[i]=new Corner{p=warp(v[i]),n=hasN?n[i]:Vector3.up,t=hasT?tg[i]:Vector4.zero,uv=hasUv?uv[i]:Vector2.zero,uv2=hasUv2?uv2[i]:Vector2.zero,c=hasC?col[i]:Color.white,
                        right=p.y*cos-p.x*sin,left=p.y*cos+p.x*sin,from=i};
                }
                var points=new List<Vector3>();var normals=new List<Vector3>();var tangents=new List<Vector4>();var uvs=new List<Vector2>();var uvs2=new List<Vector2>();var colours=new List<Color>();
                var reuse=new Dictionary<int,int>();
                int Add(Corner c)
                {
                    if(c.from>=0&&reuse.TryGetValue(c.from,out int known))return known;
                    points.Add(c.p);normals.Add(c.n);tangents.Add(c.t);uvs.Add(c.uv);uvs2.Add(c.uv2);colours.Add(c.c);
                    if(c.from>=0)reuse[c.from]=points.Count-1;return points.Count-1;
                }
                var subs=new List<int>[source.subMeshCount];
                for(int s=0;s<source.subMeshCount;s++)
                {
                    var tris=source.GetTriangles(s);var output=subs[s]=new List<int>();
                    for(int i=0;i+2<tris.Length;i+=3)
                    {
                        var polygon=new List<Corner>{corners[tris[i]],corners[tris[i+1]],corners[tris[i+2]]};
                        // Kept: right of the right-hand line, or else left of the left-hand one (the two parts can't overlap).
                        var one=Keep(polygon,c=>c.right);var two=Keep(Keep(polygon,c=>c.left),c=>-c.right);
                        bool whole=one.Count==3&&one.TrueForAll(c=>c.from>=0)||two.Count==3&&two.TrueForAll(c=>c.from>=0);
                        if(whole)keptCount++;else if(one.Count>=3||two.Count>=3)cutCount++;
                        foreach(var part in new[]{one,two})if(part.Count>=3)
                        {
                            int first=Add(part[0]);
                            for(int k=1;k+1<part.Count;k++){output.Add(first);output.Add(Add(part[k]));output.Add(Add(part[k+1]));}
                        }
                    }
                }
                var mesh=new Mesh{name=source.name+" (open)",indexFormat=points.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};
                mesh.SetVertices(points);if(hasN)mesh.SetNormals(normals);if(hasT)mesh.SetTangents(tangents);if(hasUv)mesh.SetUVs(0,uvs);if(hasUv2)mesh.SetUVs(1,uvs2);if(hasC)mesh.SetColors(colours);
                mesh.subMeshCount=subs.Length;for(int s=0;s<subs.Length;s++)mesh.SetTriangles(subs[s],s);
                mesh.RecalculateBounds();if(!hasN)mesh.RecalculateNormals();
                return mesh;
            }

            static OrreryRail LayRail(ZoneKit kit,Stuff stuff,int index,RingFit ring,Opened opened)
            {
                var rail=Rails[index];float r=rail.Radius,rowRadius=r+rail.GateOut;
                var root=new GameObject(rail.name);
                var machine=root.AddComponent<OrreryRail>();machine.railName=rail.name;machine.centre=Centre;machine.radius=r;machine.direction=rail.direction;machine.speed=rail.speed;
                machine.phase=rail.Phase;machine.moonRadius=rail.moonRadius;machine.lead=2.6f;machine.plane=Plane;machine.laneHalf=Half;machine.gateOut=rail.GateOut;machine.gateHalf=ring.inner;
                machine.strike=.2f;machine.throwClear=7;

                // ---- the light-rail: a bronze rod a metre under the plane, round the planet as far as the eye goes, with its lamps along the top
                const float Drawn=260;var path=new List<Vector3>();var radii=new List<float>();
                for(float u=-Drawn;u<=Drawn;u+=3){path.Add(At(u,r,Plane-1));radii.Add(.36f);}
                var rod=kit.Built(rail.name+" rail",new ZoneKit.Shape().Slot(0).Tube(path,radii,8,new Color(.78f,.58f,.34f),new Color(.78f,.58f,.34f),1,.4f),stuff.bronze);
                rod.transform.SetParent(root.transform,true);Unshadowed(rod);
                const float LampFrom=-150,LampSpacing=1.5f;int lamps=Mathf.RoundToInt((-LampFrom*2)/LampSpacing)+1;
                var beads=new ZoneKit.Shape();
                for(int k=0;k<lamps;k++){float u=LampFrom+k*LampSpacing;beads.Box(At(u,r,Plane-.58f),new Vector3(.3f,.2f,.62f),Quaternion.LookRotation(Tangent(u,r)),Color.white);}
                var mesh=beads.Mesh(rail.name+" lamps");var uv=new Vector2[mesh.vertexCount];for(int v=0;v<uv.Length;v++)uv[v]=new Vector2((v/24+.5f)/lamps,.5f);mesh.uv=uv;
                mesh=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+kit.zone+"-"+rail.name.Replace(' ','-')+"-lamps.asset");
                var strip=new GameObject(rail.name+" lamps",typeof(MeshFilter),typeof(MeshRenderer));strip.transform.SetParent(root.transform,true);strip.GetComponent<MeshFilter>().sharedMesh=mesh;
                machine.lampStrip=strip.GetComponent<MeshRenderer>();machine.lampFrom=LampFrom;machine.lampSpacing=LampSpacing;machine.lampCount=lamps;
                // Lit for a still as the machine stands the moment the zone opens; play writes its own each frame.
                machine.lampStrip.sharedMaterial=LampMaterial(kit,stuff,rail.name,lamps,k=>machine.Lamp(k,0));Unshadowed(strip);

                // ---- the moon, threaded on the rail, posed where it is when the zone opens
                var moon=Moon(kit,stuff,index);moon.transform.SetParent(root.transform,true);machine.moon=moon.transform;machine.moonBody=moon.transform.Find("Body");
                machine.Pose(0);

                // ---- the gate row: three rings in the lane, more round the rail past its walls, on a girder under the plane; truss between the lane's rings, so a ship crosses only through one
                // Each ring stood with its middle RingDrop under the plane, RingOut outside the rail, then leant back over the rail about its own bar.
                machine.gateAt=LaneGates.Concat(PastWalls).ToArray();var lampsOf=new List<Renderer>();float ringRadius=r+rail.RingOut;
                for(int i=0;i<machine.gateAt.Length;i++)
                {
                    float along=machine.gateAt[i]*ringRadius/rowRadius;var pivot=At(along,ringRadius,Plane-RingDrop);var inward=new Vector3(Centre.x-pivot.x,0,Centre.y-pivot.z).normalized;
                    var foot=pivot-inward*ring.face;foot.y=Plane-RingDrop-ring.middle;
                    var piece=kit.Prop(ZoneKit.Kit("RailGate","WarpBeacon"),foot,RingTall,Quaternion.LookRotation(inward).eulerAngles.y,false,false);piece.name=rail.name+" gate "+(i+1);
                    piece.transform.RotateAround(pivot,Vector3.Cross(Vector3.up,inward),Tilt);
                    piece.transform.SetParent(root.transform,true);
                    // The ring drawn open at its top (OpenAbove): the same mesh everywhere it crosses the plane and below, so the solid rims are where its bronze is.
                    var filters=piece.GetComponentsInChildren<MeshFilter>();
                    for(int f=0;f<filters.Length&&f<opened.meshes.Length;f++)if(opened.meshes[f]!=null)filters[f].sharedMesh=opened.meshes[f];
                    // Lit for a still as the gate stands the moment the zone opens; play sets its light by property block every frame.
                    bool shut=machine.GateShut(i,machine.MoonAt(0));
                    foreach(var view in piece.GetComponentsInChildren<Renderer>()){view.sharedMaterial=shut?stuff.gateAmber:stuff.gateGreen;view.shadowCastingMode=ShadowCastingMode.Off;}
                    lampsOf.Add(piece.GetComponentInChildren<Renderer>());
                    // Its arch: two slim bronze chords and their ties, in the wrapper's own space, so it leans with the ring. Added after the lamp band is taken, and never lit by it.
                    if(opened.arch!=null)
                    {
                        var arch=new GameObject("Arch",typeof(MeshFilter),typeof(MeshRenderer));arch.transform.SetParent(piece.transform,false);
                        arch.GetComponent<MeshFilter>().sharedMesh=opened.arch;var drawn=arch.GetComponent<MeshRenderer>();drawn.sharedMaterial=stuff.bronze;drawn.shadowCastingMode=ShadowCastingMode.Off;drawn.receiveShadows=false;
                    }
                }
                machine.gateLamps=lampsOf.ToArray();
                Truss(kit,stuff,root,rail,rowRadius,ring,opened.rimHalf>0?opened.rimHalf:ring.outer);

                // ---- the names, the wardens, the shards
                if(rail.named)
                {
                    var keystone=Gate(index,1)+Vector3.up*(ring.outer+.4f-RingDrop);
                    WorldBuilder.Label(rail.name.ToUpperInvariant(),new Vector3(keystone.x,Plane-.5f,keystone.z-2.4f),2.4f).transform.SetParent(root.transform,true);
                    // What the objective's arrow finds for the Names of the Moons, never offered (range nought): the rail reads it as the ship passes, open and slow, and no button can.
                    var point=kit.Discovery(rail.name+"'s keystone",Gate(index,1)-Vector3.up*Plane,"Read the keystone",rail.flag,"NamedGate",null,"Sela","MoonNames",0,0);
                    point.requiredFlag=index==0?"":Rails.Take(index).Last(x=>x.named).flag;point.transform.SetParent(root.transform,true);
                    machine.named=point;machine.namedGate=1;machine.namedSpeaker="Ishen";machine.namedSituation="hail.survey";machine.chartFlag="ishen.moonsRead";machine.slowly=8;
                }
                // The wardens' own track, 8.5 m inside the rail and clear of its moon: a straight light-rail across the lane that the drones ride and fire down (a ForceLine, which is
                // what the Rail Warden's behaviour keeps to: EnemyRider). Its lamps are a thin white line, so it reads as a second, lesser rail and never as a gate's signal.
                var trackFrom=At(-Half-6,r-8.5f);var trackTo=At(Half+6,r-8.5f);
                var line=new GameObject(rail.name+" warden track",typeof(ForceLine)).GetComponent<ForceLine>();line.transform.SetParent(root.transform,true);line.transform.position=trackFrom;line.end=trackTo;line.width=3;line.clamp=0;
                var track=new ZoneKit.Shape().Slot(0).Tube(new[]{trackFrom+Vector3.down*1.1f,trackTo+Vector3.down*1.1f},new[]{.16f,.16f},6,new Color(.7f,.52f,.32f),new Color(.7f,.52f,.32f),1,.4f)
                    .Slot(1).Box((trackFrom+trackTo)*.5f+Vector3.down*.92f,new Vector3(.08f,.06f,Vector3.Distance(trackFrom,trackTo)),Quaternion.LookRotation(trackTo-trackFrom),Color.white);
                var drawnTrack=kit.Built(rail.name+" warden track",track,stuff.bronze,stuff.trackLight);drawnTrack.transform.SetParent(root.transform,true);Unshadowed(drawnTrack);
                string warden=Foe("RailWarden","ChoristerDrifter");
                var watch=kit.FlightFight("Rail_"+(index+1),Warden(index,-1),new Vector3(6,6,6),(warden,1));watch.transform.SetParent(root.transform,true);
                var spawners=new List<Spawner>{watch.spawners[0]};spawners.Add(ZoneKit.Spawn(watch,warden,1,Warden(index,1),0));watch.spawners[0].radius=0;
                foreach(var (spawner,side) in spawners.Select((s,k)=>(s,k==0?-1:1)))
                {
                    var along=Tangent(side*30,r)*-side;spawner.transform.SetPositionAndRotation(Warden(index,side),Quaternion.LookRotation(along));spawner.faceAsPlaced=true;spawner.radius=0;
                }
                watch.spawners=spawners.ToArray();
                // Flying into either drone wakes its rail: a second box at the other one.
                var other=watch.gameObject.AddComponent<BoxCollider>();other.isTrigger=true;other.size=new Vector3(6,6,6);other.center=watch.transform.InverseTransformPoint(Warden(index,1));
                var shown=new GameObject("Wardens on show").transform;shown.SetParent(watch.transform,false);shown.position=At(0,r-8.5f);watch.previewPoint=shown;watch.previewRadius=0;
                machine.wardens=watch;
                if(rail.broken)
                {
                    var shards=new List<EncounterVolume>();string shard=Foe("Moonshard","Shellmine");
                    for(int k=0;k<3;k++)
                    {
                        // Out past the lane's wall, with a trigger nobody can fly into: the rail moves its spawner to the moon's leading face and begins it as the moon comes into the lane.
                        var calf=kit.FlightFight("Shards_"+k,At(rail.direction*(120+k*6),r),new Vector3(.2f,.2f,.2f),(shard,1));calf.spawners[0].radius=0;calf.transform.SetParent(root.transform,true);shards.Add(calf);
                    }
                    machine.shards=shards.ToArray();
                }
                return machine;
            }

            /// <summary>The solid of a gate row at the plane: each lane ring's two rims, and a girder of truss from rim to rim and out to the lane's walls. Drawn as bronze chords with braces
            /// between them, so where a ship can't pass there is something to see.</summary>
            static void Truss(ZoneKit kit,Stuff stuff,GameObject root,Rail rail,float rowRadius,RingFit ring,float drawnRim)
            {
                var shape=new ZoneKit.Shape();var solid=new List<(float from,float to)>();
                // Between the lane's rings and out past its walls; the rims themselves are made solid where the ring crosses the plane (measured off the whole ring, so the
                // solid and the gates' rule stand as they always have).
                for(int i=0;i<LaneGates.Length;i++)solid.Add((LaneGates[i]-ring.outer,LaneGates[i]-ring.inner));
                for(int i=0;i<LaneGates.Length;i++)solid.Add((LaneGates[i]+ring.inner,LaneGates[i]+ring.outer));
                List<(float from,float to)> Spans(float rim)
                {
                    var spans=new List<(float from,float to)>{(-Half-6,LaneGates[0]-rim)};
                    for(int i=1;i<LaneGates.Length;i++)spans.Add((LaneGates[i-1]+rim,LaneGates[i]-rim));
                    spans.Add((LaneGates[LaneGates.Length-1]+rim,Half+6));return spans;
                }
                solid.AddRange(Spans(ring.outer));
                var tone=new Color(.74f,.54f,.32f);
                // Drawn out to the slimmed ring's own rim, so all the solid between its band and the next has truss over it.
                foreach(var (from,to) in Spans(Mathf.Min(drawnRim,ring.outer)))
                {
                    // Two chords, braced: a lattice girder the frame reads as machinery, not a wall.
                    for(int c=0;c<2;c++){float y=Plane+(c==0?-.7f:.9f);var path=new List<Vector3>();for(float u=from;u<=to+.01f;u+=Mathf.Max(.5f,(to-from)/8))path.Add(At(u,rowRadius,y));
                        for(int k=1;k<path.Count;k++)shape.Beam(path[k-1],path[k],.5f,.4f,ZoneKit.Shape.Dim(tone,.9f+.1f*c),null,.5f);}
                    int braces=Mathf.Max(1,Mathf.RoundToInt((to-from)/1.8f));
                    for(int k=0;k<braces;k++){float a=Mathf.Lerp(from,to,k/(float)braces),b=Mathf.Lerp(from,to,(k+1)/(float)braces);
                        shape.Beam(At(k%2==0?a:b,rowRadius,Plane-.7f),At(k%2==0?b:a,rowRadius,Plane+.9f),.22f,.22f,ZoneKit.Shape.Dim(tone,.8f),null,.5f);}
                }
                var girder=kit.Built(rail.name+" truss",shape,stuff.bronze);girder.transform.SetParent(root.transform,true);Unshadowed(girder);
                foreach(var (from,to) in solid)
                {
                    var a=At(from,rowRadius);var b=At(to,rowRadius);
                    ZoneKit.BlockAlong(girder,a,b,1.4f,10,"Gate row solid").transform.position=(a+b)*.5f;
                }
            }

            /// <summary>A moon of the machine, threaded on its rail: a cratered globe in its own colour that turns on the rail (<c>Body</c>), and the bronze bosses where the rail goes
            /// in and out of it, which don't. The broken one is two halves pulling apart round a split that glows; the last one, the Warden, wears a ring.</summary>
            static GameObject Moon(ZoneKit kit,Stuff stuff,int index)
            {
                var rail=Rails[index];float R=rail.moonRadius;var root=new GameObject(rail.name+" (moon)");var body=new GameObject("Body");body.transform.SetParent(root.transform,false);
                var shape=new ZoneKit.Shape();var rng=new System.Random(300+index);
                // Poles along the rail, where the bosses hide the texture's pinch; the face laid twice round it and once pole to pole.
                var poles=Quaternion.Euler(90,0,0);var tone=rail.tone;var under=ZoneKit.Shape.Dim(rail.tone,.62f);
                if(rail.broken)
                {
                    shape.Slot(0).Lump(new Vector3(-.5f,.2f,0),new Vector3(R*.92f,R,R),11,.12f,tone,under,40,22,1/(3*R),poles);
                    shape.Slot(0).Lump(new Vector3(R*.7f,-.4f,.3f),new Vector3(R*.55f,R*.72f,R*.68f),23,.16f,tone,under,26,14,1/(3*R),poles);
                    // The split between them, lit from inside: amber, the colour of a gate shut.
                    shape.Slot(1).Disc(new Vector3(R*.38f,-.1f,0),Vector3.right,R*.62f,20,new Color(1,.62f,.24f),0,new Color(1,.4f,.1f));
                    for(int k=0;k<5;k++){var bit=new Vector3(-R*.2f+(float)rng.NextDouble()*R*1.4f,R*(.4f+.5f*(float)rng.NextDouble())*(k%2==0?1:-1),R*(.9f+.4f*(float)rng.NextDouble())*(k<3?1:-1));
                        shape.Slot(0).Lump(bit,Vector3.one*(.5f+(float)rng.NextDouble()*.9f),k+40,.2f,tone,under,10,6,.3f);}
                }
                else shape.Slot(0).Lump(Vector3.zero,Vector3.one*R,7+index,.07f,tone,under,48,24,1/(3*R),poles);
                if(index==Rails.Length-1)shape.Slot(2).Disc(Vector3.zero,new Vector3(0,.94f,.34f),R*1.75f,48,new Color(.9f,.86f,.74f),R*1.38f,new Color(.7f,.66f,.58f));
                // Every slot declared, empty or not: a renderer with more materials than its mesh has parts draws its last part again in each extra one (the split's amber over a whole moon).
                var globe=kit.Built(rail.name+" moon",shape.Slot(2),stuff.MoonFor(index),stuff.split,stuff.bronze);globe.transform.SetParent(body.transform,false);Unshadowed(globe);
                var collar=new ZoneKit.Shape();
                foreach(int end in new[]{-1,1})
                {
                    var at=Vector3.forward*end*(R*.98f);
                    collar.Tube(new[]{at-Vector3.forward*end*.9f,at+Vector3.forward*end*.6f},new[]{1.2f,.8f},14,new Color(.84f,.64f,.36f),new Color(.72f,.52f,.3f),1,.5f);
                    collar.Tube(new[]{at+Vector3.forward*end*.6f,at+Vector3.forward*end*1.1f},new[]{.55f,.45f},10,new Color(.9f,.72f,.42f),new Color(.9f,.72f,.42f),1,.5f);
                }
                var bosses=kit.Built(rail.name+" collar",collar,stuff.bronze);bosses.transform.SetParent(root.transform,false);Unshadowed(bosses);
                return root;
            }

            // ---------------------------------------------------------------- the backdrop: the machine's frame, what it has shed, and the planet it turns round
            /// <summary>The orrery's frame, all bronze and drawn only. Under each gate row the girder its rings' plinths stand on (where the leaning plinths come down); radial spokes
            /// toward the planet, open girders of two chords and braces, off the lane's middle and deep (a solid spoke straight under the lane read as a road the ships flew along);
            /// and under the lane where each row crosses it, a gear wheel lying flat, turning (<see cref="Turning"/>) the way its rail does.</summary>
            static void Armature(ZoneKit kit,Stuff stuff,RingFit ring,float footBelow)
            {
                var shape=new ZoneKit.Shape();var tone=new Color(.66f,.48f,.3f);
                foreach(var rail in Rails)
                {
                    // The plinths' feet once the rings lean (and are slimmed: footBelow is how far under the ring's middle the plinth now ends): south of the ring's middle,
                    // and up a little from where they stood.
                    float south=footBelow*Sin(Tilt)+ring.face*Cos(Tilt),row=rail.Radius+rail.RingOut+south,y=Plane-RingDrop-footBelow*Cos(Tilt)+ring.face*Sin(Tilt)-1;
                    // A rod, not a plank: with the rings slimmed it comes up near the plane, and 2.4 m across it lay over the bottom of a waiting ship's frame like a floor.
                    for(float u=-140;u<140;u+=6)shape.Beam(At(u,row,y),At(u+6,row,y),1f,.8f,ZoneKit.Shape.Dim(tone,.86f+.14f*Mathf.Repeat(u*.37f,1)),null,.4f);
                }
                foreach(var (x,y) in new[]{(-20f,-46f),(20f,-46f),(-74f,-38f),(74f,-38f)})
                {
                    // Two chords a spoke's depth apart, braced every six metres.
                    for(float z=-60;z<1080;z+=12)
                    {
                        foreach(float c in new[]{-1.6f,1.6f})shape.Beam(new Vector3(x+c,y,z),new Vector3(x+c,y,z+12),.7f,.7f,ZoneKit.Shape.Dim(tone,.82f+.18f*Mathf.Repeat(z*.07f,1)),null,.3f);
                        shape.Beam(new Vector3(x-1.6f,y,z),new Vector3(x+1.6f,y,z+6),.32f,.32f,ZoneKit.Shape.Dim(tone,.72f),null,.3f);
                        shape.Beam(new Vector3(x+1.6f,y,z+6),new Vector3(x-1.6f,y,z+12),.32f,.32f,ZoneKit.Shape.Dim(tone,.72f),null,.3f);
                    }
                }
                var frame=kit.Built("The orrery's frame",shape,stuff.bronze);Unshadowed(frame);
                var gears=new GameObject("The orrery's gears");
                for(int i=0;i<Rails.Length;i++)
                {
                    var rail=Rails[i];var hub=new Vector3(0,-40,rail.z-rail.GateOut);float R=7+i%3;
                    var wheel=new ZoneKit.Shape();
                    wheel.Disc(Vector3.zero,Vector3.up,R,48,ZoneKit.Shape.Dim(tone,1.05f),R*.3f,ZoneKit.Shape.Dim(tone,.8f));
                    for(int s=0;s<6;s++){float a=s*Mathf.PI/3;wheel.Beam(new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*R*.3f,new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*R*.92f,.8f,.5f,ZoneKit.Shape.Dim(tone,.9f),null,.4f);}
                    for(int t=0;t<28;t++){float a=t*Mathf.PI*2/28;var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));wheel.Box(dir*(R+.5f),new Vector3(1,1.2f,1.1f),Quaternion.LookRotation(dir),ZoneKit.Shape.Dim(tone,.95f));}
                    wheel.Post(Vector3.down*2,3,R*.3f,R*.26f,16,ZoneKit.Shape.Dim(tone,.7f),ZoneKit.Shape.Dim(tone,1.1f));
                    var gear=kit.Built(rail.name+" gear",wheel,stuff.bronze);gear.transform.SetParent(gears.transform,false);gear.transform.position=hub;Unshadowed(gear);
                    var turning=gear.AddComponent<Turning>();turning.axis=Vector3.up;turning.degrees=-rail.direction*rail.speed*.9f;
                }
            }
            /// <summary>What the moons have shed: cratered fragments, a split half-moon now and then, bronze collar pieces and lengths of old rail, 3 to 26 m down, thickest in the broken
            /// moon's wake. One mesh to each 120 m stretch; every draw taken whether or not its piece is kept, so a change to what is kept off moves nothing else.</summary>
            static void Debris(ZoneKit kit,Stuff stuff,List<(Vector2 at,float clear)> keep)
            {
                var rng=new System.Random(71);var root=new GameObject("What the moons have shed");
                for(float from=-40;from<LaneEnd+40;from+=120)
                {
                    var shape=new ZoneKit.Shape();int made=0;
                    for(float z=from;z<from+120;z+=.8f)
                    {
                        float x=((float)rng.NextDouble()*2-1)*(Half+30),deep=3+(float)rng.NextDouble()*23,size=.4f+(float)rng.NextDouble()*(float)rng.NextDouble()*3.2f;int kind=rng.Next(10),seed=rng.Next(999);
                        float tilt=(float)rng.NextDouble()*360,turn=(float)rng.NextDouble()*360;var tone=Rails[rng.Next(Rails.Length)].tone;int sparse=rng.Next(5);
                        bool wake=z>Rails[2].z&&z<Rails[3].z;if(!wake&&sparse<2)continue;
                        // Shallow pieces small, so nothing near the plane is big enough to read as something in the way.
                        if(deep<8)size=Mathf.Min(size,.9f);
                        var at=new Vector3(x,Plane-deep,z);if(!ZoneKit.ClearOnScreen(at,size*1.4f,keep))continue;made++;
                        var turned=Quaternion.Euler(tilt,turn,tilt*.5f);
                        if(kind<6)shape.Slot(0).Lump(at,new Vector3(size,size*(.6f+.4f*(seed%5)/4f),size*(.8f+.2f*(seed%3)/2f)),seed,.22f,tone,ZoneKit.Shape.Dim(tone,.55f),14,8,1/(2.4f*size),turned);
                        else if(kind<8)
                        {
                            // A half-moon split clean: a dome, its broken face lit amber where it is still warm.
                            shape.Slot(0).Lump(at,new Vector3(size*1.3f,size*.7f,size*1.3f),seed,.1f,tone,ZoneKit.Shape.Dim(tone,.6f),18,9,1/(2.4f*size),turned);
                            shape.Slot(1).Disc(at+turned*Vector3.down*size*.05f,turned*Vector3.down,size*1.05f,14,new Color(1,.56f,.2f),0,new Color(.6f,.3f,.12f));
                        }
                        else if(kind<9)shape.Slot(2).Tube(new[]{at,at+turned*Vector3.forward*size*3},new[]{size*.18f,size*.18f},8,new Color(.8f,.6f,.36f),new Color(.6f,.44f,.28f),1,.5f);
                        else shape.Slot(2).Disc(at,turned*Vector3.up,size*.9f,16,new Color(.84f,.64f,.38f),size*.6f,new Color(.64f,.46f,.28f));
                    }
                    if(made==0)continue;
                    var part=kit.Built("Shed "+Mathf.RoundToInt(from+40),shape.Slot(2),stuff.moonRock,stuff.split,stuff.bronze);part.transform.SetParent(root.transform,true);Unshadowed(part);
                    var bounds=part.GetComponent<MeshFilter>().sharedMesh.bounds;if(float.IsNaN(bounds.size.x)||float.IsInfinity(bounds.size.x))throw new InvalidOperationException("Orrery: the debris from "+from+" has a point that isn't a number");
                }
            }
            // ---------------------------------------------------------------- Oriel
            /// <summary>How Oriel is turned: its map's poles at the top and bottom of the disc as the far gate sees it, the middle of its map facing that eye. The face is painted
            /// for that view (<see cref="OrielFace"/>): u across the disc left to right, v down it.</summary>
            static Quaternion OrielTurn()
            {
                var eye=(EyeOver(new Vector3(0,Plane,FarGateZ+2))-OrielAt).normalized;var up=Quaternion.Euler(ZoneKit.FlightPitch,0,0)*Vector3.up;
                return Quaternion.LookRotation(eye,(up-eye*Vector3.Dot(up,eye)).normalized);
            }
            /// <summary>A direction of Oriel's own map: <paramref name="u"/> half way round from its left limb to its right, <paramref name="v"/> from the top pole to the bottom.</summary>
            static Vector3 OrielDir(float u,float v){float lon=u*Mathf.PI,lat=v*Mathf.PI;return new Vector3(Mathf.Sin(lat)*Mathf.Cos(lon),Mathf.Cos(lat),Mathf.Sin(lat)*Mathf.Sin(lon));}
            static Vector3 Rotated(Vector3 p,Vector3 axis,float angle){float c=Mathf.Cos(angle),s=Mathf.Sin(angle);return p*c+Vector3.Cross(axis,p)*s+axis*Vector3.Dot(p,axis)*(1-c);}
            /// <summary>Oriel's road fibres, severed: great circles of its map, each from where it starts (a direction), turning about its axis from <c>from</c> to <c>to</c>
            /// radians. Most run out from the Glass Choir (<see cref="ChoirOnMap"/>), where the first Bridle holds them; three from a lesser junction west; three lie alone, cut at
            /// both ends; one long one joins the two junctions.</summary>
            internal static readonly Vector2 ChoirOnMap=new(.63f,.47f),WestKnotOnMap=new(.22f,.34f);
            static List<(Vector3 start,Vector3 axis,float from,float to,bool cutAtStart)> fibreArcs;
            static List<(Vector3 start,Vector3 axis,float from,float to,bool cutAtStart)> FibreArcs()
            {
                if(fibreArcs!=null)return fibreArcs;var arcs=new List<(Vector3,Vector3,float,float,bool)>();
                void From(Vector2 at,float bearing,float from,float to,bool cutAtStart)
                {
                    var h=OrielDir(at.x,at.y);var north=(Vector3.up-h*h.y).normalized;var east=Vector3.Cross(north,h);float b=bearing*Mathf.Deg2Rad;
                    arcs.Add((h,Vector3.Cross(h,north*Mathf.Cos(b)+east*Mathf.Sin(b)).normalized,from,to,cutAtStart));
                }
                foreach(var (bearing,reach) in new[]{(12f,1.15f),(58f,.5f),(104f,1.3f),(151f,.42f),(197f,.9f),(243f,1.25f),(291f,.62f),(330f,.34f)})From(ChoirOnMap,bearing,0,reach,false);
                foreach(var (bearing,reach) in new[]{(30f,.45f),(150f,.4f),(250f,.3f)})From(WestKnotOnMap,bearing,0,reach,false);
                foreach(var (u,v,bearing,half) in new[]{(.4f,.22f,100f,.45f),(.78f,.74f,20f,.4f),(.3f,.58f,160f,.3f)})From(new Vector2(u,v),bearing,-half,half,true);
                var a=OrielDir(ChoirOnMap.x,ChoirOnMap.y);var c=OrielDir(WestKnotOnMap.x,WestKnotOnMap.y);
                arcs.Add((a,Vector3.Cross(a,c).normalized,0,Mathf.Acos(Mathf.Clamp(Vector3.Dot(a,c),-1,1)),false));
                return fibreArcs=arcs;
            }
            /// <summary>How far (radians) a direction of the map is from the nearest fibre, and how far along that fibre it is (0 at its start, 1 at its cut end).</summary>
            static float FibreDistance(Vector3 d,out float tip)
            {
                float best=9;tip=0;
                foreach(var (start,axis,from,to,_) in FibreArcs())
                {
                    float off=Vector3.Dot(d,axis);if(Mathf.Abs(off)>.035f)continue;
                    var flat=(d-axis*off).normalized;float angle=Mathf.Atan2(Vector3.Dot(flat,Vector3.Cross(axis,start)),Vector3.Dot(flat,start));
                    float gap=angle>=from&&angle<=to?Mathf.Abs(Mathf.Asin(off)):9;
                    foreach(float end in new[]{from,to})if(Mathf.Abs(end)>1e-5f)gap=Mathf.Min(gap,Mathf.Acos(Mathf.Clamp(Vector3.Dot(d,Rotated(start,axis,end)),-1,1)));
                    if(gap<best){best=gap;tip=Mathf.Clamp01(Mathf.Abs(angle)/Mathf.Max(Mathf.Abs(from),Mathf.Abs(to),1e-3f));}
                }
                return best;
            }
            /// <summary>Oriel, ahead and below the inner rails, east of the lane (<see cref="OrielAt"/>): a whole globe built here, its map painted for the face the camera sees
            /// (<see cref="OrielFace"/>), lit from the scene's own sun. That light is laid in its vertices (a sphere under the toon ramp's six steps showed six flat rings): the day
            /// side full, a soft terminator, the night side nearly black, where the vertex's alpha lets the night map shine (the fibres' own light, the glass). Its severed road
            /// fibres are lines of light laid on it as geometry, crisp at any distance, brighter by night; the Glass Choir where most of them meet is a knot of glass shards.</summary>
            static void Planet(ZoneKit kit,Stuff stuff,List<(Vector2 at,float clear)> keep)
            {
                _=keep;var turn=OrielTurn();
                var key=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l=>l.type==LightType.Directional);
                var sun=key!=null?-key.transform.forward:-(Quaternion.Euler(48,-35,0)*Vector3.forward);
                var eye=(EyeOver(new Vector3(0,Plane,FarGateZ+2))-OrielAt).normalized;
                float Day(Vector3 n)=>ZoneKit.Smooth(-.12f,.5f,Vector3.Dot(n,sun));
                // The globe: the map's u laid twice round (the camera sees one whole sheet across the disc), v pole to pole.
                const int Around=192,Rings=96;var points=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var tones=new List<Color>();var faces=new List<int>();
                for(int j=0;j<=Rings;j++)for(int k=0;k<=Around;k++)
                {
                    float lat=j/(float)Rings*Mathf.PI,lon=k/(float)Around*Mathf.PI*2;
                    var n=turn*new Vector3(Mathf.Sin(lat)*Mathf.Cos(lon),Mathf.Cos(lat),Mathf.Sin(lat)*Mathf.Sin(lon));float day=Day(n);
                    float shade=(.05f+.95f*day)*(.72f+.28f*Mathf.Clamp01(Vector3.Dot(n,eye)));
                    points.Add(OrielAt+n*OrielRadius);normals.Add(n);uv.Add(new Vector2(k/(float)Around*2,j/(float)Rings));tones.Add(new Color(shade,shade,shade,1-day));
                }
                void Face(int a,int b,int c){var n=Vector3.Cross(points[b]-points[a],points[c]-points[a]);bool front=Vector3.Dot(n,points[a]-OrielAt)>=0;faces.Add(a);faces.Add(front?b:c);faces.Add(front?c:b);}
                for(int j=0;j<Rings;j++)for(int k=0;k<Around;k++){int a=j*(Around+1)+k,b=a+1,c=a+Around+1,d=c+1;if(j>0)Face(a,b,d);if(j<Rings-1)Face(a,d,c);}
                var mesh=new Mesh{name="Oriel",indexFormat=IndexFormat.UInt32};mesh.SetVertices(points);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetColors(tones);mesh.SetTriangles(faces,0);mesh.RecalculateBounds();
                var globe=new GameObject("Oriel",typeof(MeshFilter),typeof(MeshRenderer));globe.GetComponent<MeshFilter>().sharedMesh=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+kit.zone+"-Oriel.asset");
                globe.GetComponent<MeshRenderer>().sharedMaterial=stuff.planet;Unshadowed(globe);
                // The fibres, a little over the ground, only on the side the lane ever sees; each piece's alpha (its light) and tone by where it lies, day or night.
                var fibres=new ZoneKit.Shape();var tip=new Color(.44f,.41f,.34f);
                Color Lit(Vector3 n){float night=1-Day(n);var c=Color.Lerp(tip,new Color(.36f,.34f,.3f),night);c.a=.08f+.92f*night;return c;}
                void Strand(List<Vector3> dirs,float thick,float thin)
                {
                    for(int s=0;s+1<dirs.Count;s+=8)
                    {
                        int e=Mathf.Min(dirs.Count-1,s+9);var path=new List<Vector3>();var radii=new List<float>();
                        for(int i=s;i<=e;i++){path.Add(OrielAt+dirs[i]*(OrielRadius+.5f));radii.Add(Mathf.Lerp(thick,thin,i/(float)(dirs.Count-1)));}
                        fibres.Tube(path,radii,6,Lit(dirs[s]),Lit(dirs[e]),1,.3f,false);
                    }
                }
                foreach(var (start,axis,from,to,_) in FibreArcs())
                {
                    var run=new List<Vector3>();
                    for(float a=from;a<=to+.0001f;a+=.008f)
                    {
                        var d=turn*Rotated(start,axis,a);
                        if(Vector3.Dot(d,eye)>-.08f){run.Add(d);continue;}
                        if(run.Count>1)Strand(run,.5f,.5f);run.Clear();
                    }
                    // A severed end thins to nothing (the glass painted round it is where it frays): three short strands splayed off it read as arrowheads.
                    if(run.Count>1)Strand(run,.5f,.06f);
                }
                var lines=kit.Built("Oriel's fibres",fibres,stuff.fibre);Unshadowed(lines);
                // The Glass Choir: a knot of glass shards standing out of the ground where the fibres meet, seen from orbit as a glint.
                var choir=new ZoneKit.Shape();var rng=new System.Random(37);var hub=turn*OrielDir(ChoirOnMap.x,ChoirOnMap.y);
                var side=Vector3.Cross(hub,Vector3.up).normalized;var other=Vector3.Cross(hub,side);
                for(int k=0;k<7;k++)
                {
                    float around=k*Mathf.PI*2/7+(float)rng.NextDouble()*.5f,spread=k==0?0:1.2f+2.4f*(float)rng.NextDouble();
                    var foot=OrielAt+hub*(OrielRadius-.5f)+(side*Mathf.Cos(around)+other*Mathf.Sin(around))*spread;
                    var lean=(hub+(side*Mathf.Cos(around)+other*Mathf.Sin(around))*(k==0?0:.35f)).normalized;float tall=k==0?9:3.5f+3*(float)rng.NextDouble();
                    choir.Tube(new[]{foot,foot+lean*tall*.7f,foot+lean*tall},new[]{tall*.16f,tall*.12f,.05f},5,new Color(.86f,.96f,1f,.9f),new Color(1f,1f,1f,1f),1,.3f,true);
                }
                var glass=kit.Built("The Glass Choir from orbit",choir,stuff.choirGlass);Unshadowed(glass);
            }
            /// <summary>Lume's courier pod: a small rounded hull with a glass canopy and two lamps astern, trailing the party (<see cref="CourierPod"/>).</summary>
            static void CourierPod(ZoneKit kit,Stuff stuff)
            {
                var pod=new GameObject("Lume's courier pod",typeof(CourierPod));pod.transform.position=new Vector3(-3.5f,.2f,-2);
                var shape=new ZoneKit.Shape().Slot(0).Lump(Vector3.zero,new Vector3(.9f,.6f,1.7f),5,.02f,new Color(.86f,.84f,.8f),new Color(.46f,.44f,.42f),18,10)
                    .Slot(1).Lump(new Vector3(0,.35f,.45f),new Vector3(.55f,.32f,.7f),9,.01f,new Color(.55f,.8f,.95f),new Color(.3f,.42f,.5f),14,8)
                    .Slot(2).Lump(new Vector3(-.45f,0,-1.6f),Vector3.one*.22f,1,0,Color.white,Color.white,8,6).Lump(new Vector3(.45f,0,-1.6f),Vector3.one*.22f,2,0,Color.white,Color.white,8,6);
                var body=kit.Built("Courier pod",shape,stuff.pod,stuff.canopy,stuff.podLamp);body.transform.SetParent(pod.transform,false);body.transform.localPosition=Vector3.zero;Unshadowed(body);
                var flier=pod.GetComponent<CourierPod>();flier.goneFlag="chapter7.complete";
            }

            static void Unshadowed(GameObject go){foreach(var r in go.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}}

            // ---------------------------------------------------------------- materials
            internal sealed class Stuff
            {
                public Material bronze,gateGreen,gateAmber,split,moonRock,planet,fibre,choirGlass,pod,canopy,podLamp,lampTemplate,trackLight;public Material[] moons;
                public Material MoonFor(int index)=>moons[index];
            }
            static Stuff Materials(ZoneKit kit)
            {
                var s=new Stuff();
                // Bronze: brushed metal with a little grain, its colour in the vertices, so one material covers rails, truss, collars and the frame.
                var brushed=ZoneKit.Texture("Oriel/orrery-bronze",512,(u,v)=>{float grain=ZoneKit.Tile(u*256,v*12,256,3),patch=ZoneKit.Cloud(u,v,6,9);float t=.7f+.12f*(grain-.5f)+.16f*(patch-.5f);return new Color(t,t,t,1);});
                s.bronze=ZoneKit.Toon("orrery-bronze",brushed,m=>{ZoneKit.Tinted(m);m.SetFloat("_LightFloor",.32f);m.SetColor("_RimColor",new Color(1f,.82f,.56f));m.SetColor("_ShadowTint",new Color(.5f,.42f,.48f));
                    m.SetTexture("_EmissionMap",brushed);m.SetColor("_EmissionColor",new Color(.06f,.045f,.03f));});
                // The rail-gate's own coat with its lamp panels made a lamp: the panels go a pale neutral in the albedo (so an unlit panel reads as glass, not as a colour) and are the mask
                // the gate's light shines through, green or amber as the machine says (OrreryRail.GateColour, by property block in play; these two for a still).
                string albedoPath=Path.Combine(Application.dataPath,"_Project/Art/Generated/Models/RailGate/RailGate_base_color.png");
                Texture2D gateAlbedo=null,gatePanels=null;
                if(File.Exists(albedoPath))
                {
                    var source=new Texture2D(2,2,TextureFormat.RGBA32,false);source.LoadImage(File.ReadAllBytes(albedoPath));
                    bool Panel(Color c){Color.RGBToHSV(c,out float h,out float sat,out float val);return h>.17f&&h<.46f&&sat>.32f&&val>.42f||h>.085f&&h<.18f&&sat>.48f&&val>.7f;}
                    Color Sample(float u,float v)=>source.GetPixelBilinear(u,v);
                    gateAlbedo=ZoneKit.Texture("Oriel/orrery-gate-albedo",1024,(u,v)=>{var c=Sample(u,v);if(!Panel(c))return c;float g=.38f+.08f*c.grayscale;return new Color(g,g*.98f,g*.95f,1);},false);
                    // A texel lights only where most of its neighbours are panel too: single bronze texels that passed the colour test lit as green speckle all over the rim.
                    const float Texel=1f/1024;
                    bool Lit(float u,float v){int n=0;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(Panel(Sample(u+dx*Texel,v+dy*Texel)))n++;return n>=7;}
                    gatePanels=ZoneKit.Texture("Oriel/orrery-gate-panels",1024,(u,v)=>Lit(u,v)?Color.white:Color.black,false);
                    UnityEngine.Object.DestroyImmediate(source);
                }
                var gateSource=ZoneKit.Prefab("Environment","RailGate");var original=gateSource!=null?gateSource.GetComponentInChildren<Renderer>()?.sharedMaterial:null;
                Material Gate(string name,Color light)=>ZoneKit.Toon(name,gateAlbedo??(original!=null?original.GetTexture("_BaseMap") as Texture2D:null),m=>{
                    if(original!=null){m.SetFloat("_RampSteps",original.GetFloat("_RampSteps"));m.SetColor("_ShadowTint",original.GetColor("_ShadowTint"));}
                    m.SetColor("_RimColor",new Color(1f,.84f,.58f));m.SetFloat("_LightFloor",.3f);
                    if(gatePanels!=null)m.SetTexture("_EmissionMap",gatePanels);m.SetColor("_EmissionColor",light);});
                // Saturated and not too bright: bloomed, a strong green and a strong amber both went to cream and could not be told apart.
                s.gateGreen=Gate("orrery-gate-green",new Color(.16f,1.05f,.3f));s.gateAmber=Gate("orrery-gate-amber",new Color(1.5f,.52f,.05f));
                // A moon's face: a field of craters, laid twice round the globe; the globe's colour is in its vertices, and it lights itself a little so its dark side keeps its colour.
                var face=Craters("Oriel/orrery-moon-face",61);
                Material MoonMaterial(string name,float self)=>ZoneKit.Toon(name,face,m=>{ZoneKit.Tinted(m);m.SetTexture("_EmissionMap",face);m.SetColor("_EmissionColor",new Color(self,self*.96f,self*.9f));m.SetFloat("_RampSteps",5);
                    m.SetFloat("_LightFloor",.28f);m.SetColor("_RimColor",new Color(.95f,.9f,.8f));ZoneKit.With(m,"_DETAIL_ON","_Detail");m.SetColor("_DetailTintA",new Color(.82f,.82f,.84f));
                    m.SetColor("_DetailTintB",new Color(1.12f,1.1f,1.08f));m.SetVector("_DetailParams",new Vector4(.12f,1.6f,.28f,.32f));});
                s.moonRock=MoonMaterial("orrery-moon",.12f);s.moons=Rails.Select((r,i)=>s.moonRock).ToArray();
                s.split=ZoneKit.Toon("orrery-split",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(1,.7f,.36f));m.SetColor("_EmissionColor",new Color(2.4f,1.2f,.3f));});
                // Oriel: its day map lit by the light laid in its vertices (the toon ramp's own steps off: a white shadow tint), its night map shining where their alpha says night,
                // and a pale blue rim of air on the sunlit limb.
                OrielFace("Oriel/oriel-face","Oriel/oriel-night",out var land,out var night);
                // Dim enough that its brightest land stays under the bloom's threshold: at .8 the whole face bloomed and read soft.
                s.planet=ZoneKit.Toon("oriel-planet",land,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.74f,.73f,.7f));m.SetColor("_ShadowTint",Color.white);m.SetFloat("_ReceiveShadows",0);
                    m.SetTexture("_EmissionMap",night);m.SetColor("_EmissionColor",new Color(1.7f,1.7f,1.7f));m.SetColor("_RimColor",new Color(.3f,.48f,.7f));m.SetFloat("_RimPower",3);});
                // A fibre: pale gold by day, and by night its own light (its vertices' alpha), which blooms.
                s.fibre=ZoneKit.Toon("oriel-fibre",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_EmissionColor",new Color(2.6f,2.3f,1.6f));m.SetColor("_ShadowTint",new Color(.8f,.8f,.8f));});
                s.choirGlass=ZoneKit.Toon("oriel-choir-glass",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.8f,.9f,.96f));m.SetColor("_EmissionColor",new Color(.9f,1.25f,1.5f));
                    m.SetColor("_RimColor",new Color(.9f,1f,1f));m.SetFloat("_RimPower",2);});
                s.pod=ZoneKit.Toon("orrery-pod",brushed,m=>{ZoneKit.Tinted(m);m.SetColor("_RimColor",new Color(.9f,.92f,1f));});
                s.canopy=ZoneKit.Toon("orrery-pod-canopy",null,m=>{ZoneKit.Tinted(m);m.SetColor("_EmissionColor",new Color(.12f,.2f,.26f));m.SetColor("_RimColor",new Color(.9f,1f,1f));});
                s.podLamp=ZoneKit.Toon("orrery-pod-lamp",null,m=>{m.SetColor("_BaseColor",new Color(1,.86f,.6f));m.SetColor("_EmissionColor",new Color(2.4f,1.8f,1f));});
                s.lampTemplate=ZoneKit.Toon("orrery-lamp",null,m=>{m.SetColor("_BaseColor",Color.white);m.SetColor("_EmissionColor",new Color(2.6f,2.6f,2.6f));m.SetFloat("_LightFloor",1);});
                s.trackLight=ZoneKit.Toon("orrery-track-light",null,m=>{m.SetColor("_BaseColor",new Color(.92f,.94f,1f));m.SetColor("_EmissionColor",new Color(1.1f,1.15f,1.3f));m.SetFloat("_LightFloor",1);});
                return s;
            }
            /// <summary>A rail's lamp material, with the lamps lit as the machine stands at a moment: one texel a lamp, point-sampled. Play swaps in its own texture.</summary>
            static Material LampMaterial(ZoneKit kit,Stuff stuff,string rail,int count,Func<int,Color32> colour)
            {
                string slug=rail.Replace(' ','-').ToLowerInvariant();string path="Assets/_Project/Art/Generated/Oriel/orrery-lamps-"+slug+".asset";
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if(texture==null||texture.width!=count){if(texture!=null)AssetDatabase.DeleteAsset(path);texture=new Texture2D(count,1,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};AssetDatabase.CreateAsset(texture,path);}
                var pixels=new Color32[count];for(int k=0;k<count;k++)pixels[k]=colour(k);texture.SetPixels32(pixels);texture.Apply(false);EditorUtility.SetDirty(texture);
                return ZoneKit.Toon("orrery-lamps-"+slug,texture,m=>{m.SetColor("_BaseColor",Color.white);m.SetTexture("_EmissionMap",texture);m.SetColor("_EmissionColor",new Color(1.5f,1.5f,1.5f));m.SetFloat("_LightFloor",1);});
            }
            /// <summary>Craters at every size, as a real field falls off (the count over a size going as one over its square), each a bowl lit from the sheet's upper left with a hard lip and a
            /// pale apron, a few young ones throwing rays; highlands of broken grain and a few dark seas. Tiles across, as it is laid round a globe. After the Spore Veil's moons.</summary>
            static Texture2D Craters(string file,int seed)
            {
                const int N=1024;var tone=new float[N*N];var rng=new System.Random(seed);
                for(int y=0;y<N;y++)for(int x=0;x<N;x++)
                {
                    float u=(x+.5f)/N,v=(y+.5f)/N,sea=ZoneKit.Smooth(.54f,.58f,ZoneKit.Cloud(u,v,3,seed));
                    float high=.78f+.1f*(ZoneKit.Cloud(u,v,12,seed+3)-.5f)+.07f*(ZoneKit.Cloud(u,v,48,seed+4)-.5f)+.06f*(ZoneKit.Tile(u*256,v*256,256,seed+5)-.5f);
                    tone[y*N+x]=Mathf.Lerp(high,.5f+.05f*(ZoneKit.Cloud(u,v,20,seed+6)-.5f),sea);
                }
                const float Lx=-.7071f,Ly=.7071f;
                for(int k=0;k<2600;k++)
                {
                    float r=Mathf.Min(.06f,.0025f/Mathf.Sqrt(Mathf.Max(.0017f,(float)rng.NextDouble()))),cx=(float)rng.NextDouble(),cy=.05f+.9f*(float)rng.NextDouble();bool young=rng.Next(9)==0,rays=k<4;
                    float apron=young?2.3f:1.7f,reach=rays?10:apron;
                    int x0=Mathf.FloorToInt((cx-r*reach)*N),x1=Mathf.CeilToInt((cx+r*reach)*N),y0=Mathf.Max(0,Mathf.FloorToInt((cy-r*reach)*N)),y1=Mathf.Min(N-1,Mathf.CeilToInt((cy+r*reach)*N));
                    for(int py=y0;py<=y1;py++)for(int px=x0;px<=x1;px++)
                    {
                        int wx=((px%N)+N)%N;float dx=(wx+.5f)/N-cx;dx-=Mathf.Round(dx);float dy=(py+.5f)/N-cy,d=Mathf.Sqrt(dx*dx+dy*dy)/r;if(d>reach)continue;
                        float facing=d>.001f?(dx*Lx+dy*Ly)/(d*r):0,add=0;
                        if(d<.94f){add=-.03f-.05f*(1-d*d)-.24f*facing*ZoneKit.Smooth(.2f,.92f,d);if(r>.022f&&d<.2f)add+=.16f*(1-d/.2f);}
                        else if(d<1.12f)add=.11f+.06f*facing;
                        else if(d<apron)add=(young?.1f:.05f)*Mathf.Pow(1-(d-1.12f)/(apron-1.12f),2);
                        if(rays&&d>1.2f){float around=Mathf.Atan2(dy,dx)/(Mathf.PI*2)+.5f;add+=.12f*ZoneKit.Smooth(.6f,.86f,ZoneKit.Tile(around*40,k,40,k))*Mathf.Pow(1-(d-1.2f)/(reach-1.2f),1.5f);}
                        tone[py*N+wx]+=add;
                    }
                }
                return ZoneKit.Texture(file,N,(u,v)=>{int x=Mathf.Min(N-1,(int)(u*N)),y=Mathf.Min(N-1,(int)(v*N));float t=tone[y*N+x];return new Color(t,t,t,1);});
            }
            /// <summary>Oriel's face from orbit, painted for the half the camera sees (u across the disc left to right, v down it: <see cref="OrielTurn"/>), with its night map. Three
            /// lands laid out for that face (a great continent east of the middle with the Glass Choir on it, a lesser one west, islands south, where the planet first comes into
            /// the frame), coasts sharp and broken at every scale, pale shelves round them and deep blue sea; on land, broad climates (pale gold inland, olive by the coasts) under
            /// a fine mosaic of scrub and field, ranges as thin dark lines, rivers; along each fibre a belt of garden green, and glass glittering where a fibre frays and round
            /// the Choir; ice at both poles (where a globe's map pinches); one storm's curl. The first face's patches of gold and green, all one size with hard edges, and its
            /// cream blobs of cloud read as camouflage; smooth climates with no grain under them read soft. By night: each fibre's own light, the glass faintly. Painted on
            /// every core at once.</summary>
            static void OrielFace(string dayFile,string nightFile,out Texture2D day,out Texture2D night)
            {
                const int N=2048;var dayPixels=new Color[N*N];var nightPixels=new Color[N*N];FibreArcs();
                var choir=OrielDir(ChoirOnMap.x,ChoirOnMap.y);
                static float Bump(float u,float v,float u0,float v0,float ru,float rv){float du=Mathf.Repeat(u-u0+.5f,1)-.5f,dv=v-v0;return Mathf.Exp(-(du*du/(ru*ru)+dv*dv/(rv*rv)));}
                static float S(float a,float b,float x)=>ZoneKit.Smooth(a,b,x);
                System.Threading.Tasks.Parallel.For(0,N,y=>
                {
                    for(int x=0;x<N;x++)
                    {
                        float u=(x+.5f)/N,v=(y+.5f)/N;var d=OrielDir(u,v);
                        float wu=u+.05f*(ZoneKit.Cloud(u,v,3,5)-.5f)+.012f*(ZoneKit.Tile(u*48,v*48,48,8)-.5f),wv=v+.05f*(ZoneKit.Cloud(u,v,3,6)-.5f)+.012f*(ZoneKit.Tile(u*48,v*48,48,9)-.5f);
                        float shape=.55f*Bump(u,v,.66f,.46f,.2f,.27f)+.32f*Bump(u,v,.2f,.34f,.11f,.13f)+.22f*Bump(u,v,.42f,.86f,.2f,.05f)+.2f*Bump(u,v,.9f,.2f,.07f,.1f);
                        float L=.55f*ZoneKit.Cloud(wu,wv,4,11)+.45f*ZoneKit.Cloud(wu,wv,12,12)-.5f+shape;const float Coast=.2f;
                        float land=S(Coast-.0015f,Coast+.0015f,L);
                        // Sea: deep, paler over the shelves.
                        float shelf=Mathf.Pow(S(Coast-.1f,Coast,L),2.2f);var sea=Color.Lerp(new Color(.03f,.085f,.15f),new Color(.09f,.27f,.32f),shelf)*(.9f+.2f*ZoneKit.Cloud(u,v*2,6,21));
                        // Land: broad climates (pale gold inland, olive by the coasts) and a fine mosaic over them, small hard-edged patches of scrub where it is wetter
                        // and pale fields where drier, so the face has grain at the screen's own scale (smooth gradients alone read soft); ranges and their spurs as thin dark
                        // lines; rivers.
                        float inland=S(Coast,Coast+.22f,L),dry=Mathf.Clamp01(.65f*inland+.55f*(ZoneKit.Cloud(u,v,2,31)-.5f)+.1f);
                        var ground=Color.Lerp(new Color(.4f,.44f,.31f),new Color(.68f,.58f,.38f),S(.2f,.75f,dry))*(.88f+.24f*ZoneKit.Cloud(u,v,24,32));
                        float mosaic=ZoneKit.Tile(u*160,v*160,160,61)*.6f+ZoneKit.Tile(u*320,v*320,320,62)*.4f;
                        ground=Color.Lerp(ground,new Color(.3f,.34f,.22f),S(.6f,.63f,mosaic)*(1-S(.55f,.85f,dry))*.4f);
                        ground=Color.Lerp(ground,new Color(.8f,.72f,.52f),S(.66f,.69f,ZoneKit.Tile(u*200,v*200,200,63))*S(.45f,.75f,dry)*.35f);
                        float ridge=1-Mathf.Abs(2*ZoneKit.Cloud(wu,wv,6,33)-1),ranges=S(.35f,.55f,ZoneKit.Cloud(u,v,3,34))*S(.04f,.12f,L-Coast);
                        ground=Color.Lerp(ground,new Color(.3f,.24f,.18f),S(.93f,.965f,ridge)*ranges*.8f);
                        float spur=1-Mathf.Abs(2*ZoneKit.Cloud(wu,wv,24,36)-1);
                        ground=Color.Lerp(ground,new Color(.3f,.24f,.18f),S(.95f,.975f,spur)*ranges*.5f);
                        float river=S(.988f,.996f,1-Mathf.Abs(2*ZoneKit.Cloud(wu,wv,9,35)-1))*S(.01f,.05f,L-Coast)*(1-S(.55f,.85f,dry));
                        ground=Color.Lerp(ground,new Color(.08f,.2f,.22f),river*.85f)*(.9f+.2f*ZoneKit.Tile(u*768,v*768,768,17));
                        // The fibres: garden green along them, glass where they fray and round the Choir. Kept off the map's edges, which lie at the limbs.
                        float fd=FibreDistance(d,out float tip),edge=S(0,.05f,u)*S(1,.95f,u);
                        ground=Color.Lerp(ground,new Color(.56f,.66f,.44f),S(.006f,.002f,fd)*land*edge*.6f);
                        float glass=Mathf.Max(S(.03f,0,fd)*S(.6f,1,tip),S(.9965f,.9993f,Vector3.Dot(d,choir)));
                        float glassy=Mathf.Clamp01(glass*(ZoneKit.Tile(u*900,v*900,900,19)>.78f?1:.45f))*land*edge;
                        ground=Color.Lerp(ground,new Color(.8f,.9f,.96f),glassy);
                        var c=Color.Lerp(sea,ground,land);
                        // Ice at the poles.
                        float ice=S(.07f,.04f,Mathf.Min(v,1-v)+.03f*(ZoneKit.Cloud(u,v,10,41)-.5f));c=Color.Lerp(c,new Color(.76f,.79f,.82f),ice);
                        // Weather: one storm's curl over the southern sea. Streaks of cloud read either as smears (soft) or as white worms (hard): none.
                        float su=Mathf.Repeat(u-.36f+.5f,1)-.5f,sv=v-.7f,r=Mathf.Sqrt(su*su+sv*sv)+1e-6f,arm=.5f+.5f*Mathf.Cos(2*Mathf.Atan2(sv,su)+9*Mathf.Log(r/.05f));
                        float storm=S(.7f,.95f,arm)*S(.05f,.012f,r)*S(.003f,.008f,r);
                        c=Color.Lerp(c,new Color(.9f,.91f,.92f),Mathf.Clamp01(storm*.6f));c.a=1;
                        dayPixels[y*N+x]=c;
                        // By night: the fibres' light and the glass.
                        var glow=(new Color(1,.86f,.55f)*(.9f*S(.004f,0,fd))+new Color(.5f,.42f,.28f)*(.25f*S(.02f,0,fd)))*edge+new Color(.25f,.4f,.5f)*(.5f*glassy);glow.a=1;
                        nightPixels[y*N+x]=glow;
                    }
                });
                day=Large(dayFile,N,dayPixels);night=Large(nightFile,N,nightPixels);
            }
            /// <summary>A painted texture larger than the kit's own painter keeps (it caps them at 1024): Oriel's face, which covers more of the screen than any one sheet of 1024 can.</summary>
            static Texture2D Large(string file,int size,Func<float,float,Color> paint)
            {
                var pixels=new Color[size*size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)pixels[y*size+x]=paint((x+.5f)/size,(y+.5f)/size);
                return Large(file,size,pixels);
            }
            static Texture2D Large(string file,int size,Color[] pixels)
            {
                string path="Assets/_Project/Art/Generated/"+file+".png";Directory.CreateDirectory(Path.GetDirectoryName(path));
                var made=new Texture2D(size,size,TextureFormat.RGB24,false);
                made.SetPixels(pixels);var bytes=made.EncodeToPNG();UnityEngine.Object.DestroyImmediate(made);
                bool changed=!File.Exists(path)||!File.ReadAllBytes(path).SequenceEqual(bytes);
                if(changed){File.WriteAllBytes(path,bytes);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);}
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer!=null&&(importer.maxTextureSize!=size||!importer.mipmapEnabled||importer.wrapMode!=TextureWrapMode.Repeat)){importer.maxTextureSize=size;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.SaveAndReimport();}
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }
    }
}
