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
    /// <summary>Nacre Grave (space combat; rule: a wreck field with a spiral). Map brief, the rule, coordinates and the combat engineer's contract: docs/maps/NacreGrave.md. The field's
    /// turning runs in <see cref="WreckTrack"/>; everything here lays it from the named numbers of <see cref="Nacre"/>, which the map brief, the tests and a route writer read too.</summary>
    public static partial class HomeBuilder
    {
        static void NacreDefinition(ZoneDef z){}
        [ZoneLook.Source] static void NacreLooks()
        {
            // A cold white key off a far star so the families' paint reads true, a rose-violet fill off the Choir's passage beyond, and the tracks' lamps and the beacons what blooms.
            ZoneLook.Register(NacreZone,new ZoneLook.Spec{sun=C(1f,.96f,.92f),sunIntensity=1.35f,shadowStrength=.75f,
                sky=C(.52f,.52f,.6f),equator=C(.42f,.41f,.46f),ground=C(.24f,.23f,.27f),
                contrast=12,saturation=8,splitShadows=C(.42f,.44f,.52f),splitHighlights=C(.6f,.56f,.5f),
                bloomThreshold=.85f,bloomIntensity=.8f,vignette=.22f});
        }

        internal static class Nacre
        {
            // ---------------------------------------------------------------- the field's numbers
            /// <summary>The broken berth at the field's middle; the plane every hull flies on.</summary>
            internal static readonly Vector2 Centre=Vector2.zero;
            internal const float Plane=ZoneKit.FlightPlane;
            /// <summary>The spiral lane: its middle is <see cref="R"/>(turn) from the centre, where turn is degrees clockwise from north; it starts at <see cref="StartTurn"/> on the outer marker
            /// and winds in a turn and a quarter to <see cref="EndTurn"/>, <see cref="Half"/> metres either side of its middle (32 m: the flight frame is 25 deep, so its walls stand at a frame's edges). Between its two passes the old salvage stands 20 m thick.</summary>
            internal const float R0=158,Pitch=52,Half=16,StartTurn=-18,EndTurn=452;
            internal static float R(float turn)=>R0-Pitch*turn/360f;
            /// <summary>The basin round the berth: open water inside <see cref="BasinRim"/>, the berth itself solid inside <see cref="BerthSolid"/>.</summary>
            internal const float BasinRim=70,BerthSolid=30;
            /// <summary>Where the lane's inner wall stops and the lane opens into the basin (its mouth runs from here to its end).</summary>
            internal const float MouthTurn=400;
            /// <summary>The breach: a channel through the inner salvage straight down into the basin, <see cref="BreachHalf"/> either side of the line out from the centre at <see cref="BreachTurn"/>.</summary>
            internal const float BreachTurn=200,BreachHalf=11;
            /// <summary>A point of the plan this far from the centre at this turn.</summary>
            internal static Vector2 On(float turn,float radius){float a=turn*Mathf.Deg2Rad;return Centre+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*radius;}
            /// <summary>A point of the lane: at this turn, <paramref name="offset"/> out from its middle (negative is inward, toward the berth).</summary>
            internal static Vector3 At(float turn,float offset=0,float y=Plane){var p=On(turn,R(turn)+offset);return new Vector3(p.x,y,p.y);}
            internal static Vector3 In(float turn,float radius,float y=Plane){var p=On(turn,radius);return new Vector3(p.x,y,p.y);}
            /// <summary>The way the field turns at a turn (clockwise seen from above, the way the lane winds in).</summary>
            internal static Vector3 Along(float turn){float a=turn*Mathf.Deg2Rad;return new Vector3(Mathf.Cos(a),0,-Mathf.Sin(a));}
            /// <summary>Degrees of turn a breach's half width takes on a circle of this radius.</summary>
            static float Spread(float half,float radius)=>Mathf.Asin(Mathf.Clamp(half/radius,-1,1))*Mathf.Rad2Deg;

            // ---- the tracks (WreckTrack): where each runs, how quick, how many wrecks ride it
            /// <summary>The tow line: along the lane's inner side, 5 m inside its inner wall, from just past the marker to the mouth. Tow-drones gone wrong drag hulls along it toward the
            /// berth, 2.6 m/s at the outer pass and up to 8 inward (the square of the radius).</summary>
            internal const float TowOffset=-11,TowFrom=16,TowTo=446,TowSpeed=2.6f,TowRef=140,TowMax=8;internal const int TowPieces=8;
            /// <summary>The breach race: fresh wreckage on an arc through the inner salvage at <see cref="RaceRadius"/>, crossing the breach at 13 m/s.</summary>
            internal const float RaceRadius=91,RaceFrom=150,RaceTo=250,RaceSpeed=13;internal const int RacePieces=2;
            /// <summary>The berth ring: the wreckage of the berth's own docks, circling it at 15 m/s just outside the basin's pod.</summary>
            internal const float RingRadius=48,RingSpeed=15;internal const int RingPieces=4;

            // ---- the places
            /// <summary>The arrival, and the way home in its frame (lower left: 9 m back along the lane and 4.5 inward, out of the warp's reach). At turn -11 it stood 17 m west,
            /// just past the frame's edge, and the arrival's still was two ships in the dark (pass 3).</summary>
            internal static readonly Vector3 Arrival=At(-5,2),HomeWarp=At(-8,-3);
            /// <summary>The outer marker, and the line across the lane past it where Bryn hails (the crossing's flag is home.nacreReached; the talk sets home.nacreSpoken). The buoy
            /// stands in the arrival's frame, upper right on the outer side, so the first thing seen is the approach's light; at turn 1 inward it was off the frame's corner.</summary>
            internal const float MarkerTurn=8;
            internal static readonly Vector3 MarkerBuoy=At(-1.5f,7);
            /// <summary>Neve's skiff on the outer pass, flying people out by the hull.</summary>
            internal static readonly Vector3 NeveSkiff=At(40,10),NeveHail=At(40,5);
            /// <summary>The tug Welcome Back in the outer pass with her raft of hulls; Bryn's hail and the first parcel's hand on her outer side, so a ship stopped there has her in the
            /// frame's upper half (on the pass's south side outward is south, and a hull below the plane shows further south still). She and her raft lie 6.1 to 7 m out, so the water
            /// between their solids (2.3 m out at the least) and the tow line's marked stretch (7 m in) stays open by 9.3 m with nothing across it: moored 4 m out with her raft from
            /// 2 to 7.5, they shut the lane's quiet side and a ship got past only at the outer edge (chapter 9's routes, November 14). The hail is at the outer edge of open water for
            /// a hull (12.8 m out), a hull and half a metre off her solid, as the routes' check asks.</summary>
            internal const float TugTurn=176;
            internal static readonly Vector3 Tug=At(TugTurn,6.1f),TugHail=At(TugTurn,12.8f),TugParcel=At(TugTurn+4.4f,10.5f);
            /// <summary>Mira's lot lashed to an old hull on the outer wall, late on the outer pass.</summary>
            internal static readonly Vector3 Lot=At(292,12),LotHand=At(292,7);
            /// <summary>Oda's hauler, in the basin just inside the lane's mouth, between the rim and the berth ring: "she's inward of you".</summary>
            internal static readonly Vector3 Hauler=In(62,61),HaulerHail=In(62,66);
            /// <summary>The navigators' pod, holed and lodged against the berth inside the berth ring.</summary>
            internal static readonly Vector3 Pod=In(318,35.5f),PodHail=In(318,39.5f);
            /// <summary>The fights: two tows broken off the line on each pass, two in the basin, and a fragment of the Bridle thrashing on its tether in the breach.</summary>
            internal static readonly Vector3 TowsOuter=At(100,2),TowsInner=At(262,2),TowsBasin=In(242,60),Fragment=In(BreachTurn,98);
            /// <summary>Wrecks in the families' colours on the lane's outer edge, each said once as it is passed.</summary>
            internal static readonly float[] FamilyWrecks={64,150,334};
            /// <summary>Where a ship stands at the breach's mouth looking down it, and in the lane's mouth looking into the basin.</summary>
            internal static readonly Vector3 BreachMouth=At(BreachTurn,-12),LaneMouth=At(430,-6);

            /// <summary>LookDev's stills: where a player arrives and stops (the marker, Neve, the tug, the lot, each beacon, the breach's mouth, the lane's mouth), each pass as it is flown,
            /// the three tracks where they are crossed, and wide views of the spiral, the basin and the field.</summary>
            internal static LookDev.View[] Views=>new[]
            {
                new LookDev.View("arrival",Arrival,true),new("marker",At(2,3),true),new("neve",NeveHail,true),new("outer-pass",At(110,4),true),
                new("tug",TugHail,true),new("tow-line",At(232,-6),true),new("breach-mouth",BreachMouth,true),new("breach",In(BreachTurn,96),true),
                new("lot",LotHand,true),new("inner-pass",At(372,0),true),new("lane-mouth",LaneMouth,true),new("oda",HaulerHail,true),
                new("ring-crossing",In(300,56),true),new("pod",PodHail,true),new("basin-wide",In(60,40),true,3.6f),new("breach-wide",In(BreachTurn,104),true,3.2f),
                new("spiral-wide",new Vector3(0,Plane,30),true,13),new("outer-wide",At(150,0),true,3.4f),
            };

            /// <summary>Whether a point of the plan is open to a hull: in the lane, the breach or the basin (outside the berth). Everything the reef stands on is not.</summary>
            internal static bool Open(Vector2 p)=>InLane(p,0)||InBreach(p,0)||InBasin(p,0);
            internal static bool InLane(Vector2 p,float margin)
            {
                float r=(p-Centre).magnitude,theta=Mathf.Repeat(Mathf.Atan2(p.x-Centre.x,p.y-Centre.y)*Mathf.Rad2Deg,360);
                for(int k=-1;k<=1;k++){float turn=theta+360*k;if(turn<StartTurn||turn>EndTurn)continue;if(Mathf.Abs(r-R(turn))<=Half-margin)return true;}
                // The open strip between the lane's last quarter and the basin, along the mouth.
                if(InMouth(p,margin))return true;
                return false;
            }
            static bool InMouth(Vector2 p,float margin)
            {
                float r=(p-Centre).magnitude,turn=Mathf.Repeat(Mathf.Atan2(p.x-Centre.x,p.y-Centre.y)*Mathf.Rad2Deg,360)+360;
                return turn>=MouthTurn+margin/2&&turn<=EndTurn&&r>=BasinRim-margin&&r<=R(turn)-Half+margin;
            }
            internal static bool InBasin(Vector2 p,float margin){float r=(p-Centre).magnitude;return r<=BasinRim-margin&&r>=BerthSolid+margin;}
            internal static bool InBreach(Vector2 p,float margin)
            {
                var ray=On(BreachTurn,1)-Centre;var across=new Vector2(ray.y,-ray.x);var d=p-Centre;float out_=Vector2.Dot(d,ray),side=Vector2.Dot(d,across);
                return Mathf.Abs(side)<=BreachHalf-margin&&out_>=BasinRim-1&&out_<=R(BreachTurn)-Half+1;
            }

            // ---------------------------------------------------------------- what it is made of
            internal sealed class Stuff{public Material cable,hullGlow,lampGlow,bruiseCore,bruiseEdge,bruiseStreak,berthMetal,berthDark,grease,rope;}
            /// <summary>The families' paint, as each hauler line had it: strong enough to read from the flight camera's height, and never a danger's orange, red or purple.</summary>
            internal static readonly (string name,Color tone)[] Families={("teal",new(.24f,.66f,.62f)),("ochre",new(.9f,.72f,.28f)),("cream",new(.94f,.9f,.8f)),("sky",new(.42f,.62f,.9f)),("green",new(.44f,.74f,.4f))};
            /// <summary>The old salvage spiral's greys and rust, a little apart from each other.</summary>
            internal static readonly (string name,Color tone)[] Old={("grey",new(.46f,.45f,.46f)),("rust",new(.52f,.44f,.38f)),("slate",new(.38f,.41f,.46f)),("ash",new(.5f,.5f,.48f))};
            /// <summary>What the field is made of: ships and station pieces of several builds, never one hull turned five ways.</summary>
            static readonly (string key,float size)[] Hulks={("Hauler",11),("Skiff",6),("PatrolCutter",7),("CompactCutter",7),("TallowStationHull",12),("RingSegmentA",10),("RingSegmentB",10),
                ("DockingPodOffice",6),("DockingPodRepair",6),("DockingPodOutfitter",6),("SeedBarge",9),("TallowMooringCoupler",7),("MusterCrate",3),("DressCargoPallet",3),("DressFuelTank",4)};
            static readonly Dictionary<(Material,string),Material> painted=new();
            /// <summary>A piece in a colour of its own, drawn in copies of its own materials with the colour multiplied in (saved beside the scene): a tint in a property block shows in play
            /// only, and the editor's stills would show every hull in its stock paint.</summary>
            static void Paint(GameObject piece,string name,Color tone)
            {
                foreach(var view in piece.GetComponentsInChildren<Renderer>())
                {
                    var mats=view.sharedMaterials;
                    for(int i=0;i<mats.Length;i++)
                    {
                        var source=mats[i];if(source==null)continue;
                        if(!painted.TryGetValue((source,name),out var copy))
                        {
                            string path="Assets/_Project/Scenes/"+NacreZone+"-"+source.name.Replace(' ','-')+"-"+name+".mat";
                            copy=AssetDatabase.LoadAssetAtPath<Material>(path);var fresh=new Material(source){name=source.name+"-"+name};
                            if(copy==null){copy=fresh;AssetDatabase.CreateAsset(copy,path);}else{copy.CopyPropertiesFromMaterial(fresh);copy.shader=fresh.shader;UnityEngine.Object.DestroyImmediate(fresh);}
                            if(copy.HasProperty("_BaseColor"))copy.SetColor("_BaseColor",source.GetColor("_BaseColor")*tone);
                            EditorUtility.SetDirty(copy);painted[(source,name)]=copy;
                        }
                        mats[i]=copy;
                    }
                    view.sharedMaterials=mats;
                }
            }
            static void Unshadowed(GameObject go,bool casts=false){foreach(var r in go.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=casts?ShadowCastingMode.On:ShadowCastingMode.Off;r.receiveShadows=false;}}
            static Stuff Materials(ZoneKit kit)
            {
                var s=new Stuff();
                var plain=ZoneKit.Texture("Nacre/nacre-plate",256,(u,v)=>{float n=.78f+.12f*(ZoneKit.Cloud(u,v,8,3)-.5f)+.05f*(ZoneKit.Tile(u*64,v*64,64,5)-.5f);
                    float seam=Mathf.Abs(Mathf.Repeat(u*4,1)-.5f)<.012f||Mathf.Abs(Mathf.Repeat(v*4,1)-.5f)<.012f?-.18f:0;return new Color(n+seam,n+seam,n*1.02f+seam,1);});
                s.cable=ZoneKit.Tinted(ZoneKit.Toon("nacre-cable",plain,m=>{m.SetColor("_BaseColor",new Color(.3f,.32f,.36f));m.SetFloat("_RampSteps",3);}));
                s.berthMetal=ZoneKit.Tinted(ZoneKit.Toon("nacre-berth",plain,m=>{m.SetColor("_BaseColor",new Color(.9f,.9f,.92f));m.SetFloat("_RampSteps",4);}));
                s.berthDark=ZoneKit.Tinted(ZoneKit.Toon("nacre-berth-dark",plain,m=>{m.SetColor("_BaseColor",new Color(.42f,.42f,.46f));m.SetFloat("_RampSteps",3);m.SetFloat("_ReceiveShadows",0);}));
                s.rope=ZoneKit.Tinted(ZoneKit.Toon("nacre-rope",plain,m=>{m.SetColor("_BaseColor",Color.white);m.SetFloat("_RampSteps",3);}));
                // The navigators' chart on the pod's hatch: the whole field drawn in grease with a thumb, a spiral and its three tracks and every hull a smudge, on scuffed white plate.
                var chart=ZoneKit.Texture("Nacre/navigators-hatch",256,(u,v)=>
                {
                    float x=u*2-1,y=v*2-1,r=Mathf.Sqrt(x*x+y*y),a=Mathf.Atan2(x,y)/(Mathf.PI*2);
                    float spiral=Mathf.Abs(Mathf.Repeat(r*3.2f+a,1)-.5f),line=1-ZoneKit.Smooth(.03f,.07f,spiral)*(r<.95f&&r>.18f?1:0);
                    float ring=1-ZoneKit.Smooth(.012f,.03f,Mathf.Abs(r-.3f));float blot=ZoneKit.Tile(u*24,v*24,24,9)>.86f&&r<.9f?1:0;float smear=.08f*ZoneKit.Cloud(u,v,6,11);
                    float grease=Mathf.Clamp01(Mathf.Max(1-line,Mathf.Max(ring*.9f,blot*.8f))+smear);float plate=.86f+.08f*(ZoneKit.Tile(u*48,v*48,48,13)-.5f);
                    return new Color(Mathf.Lerp(plate,.16f,grease),Mathf.Lerp(plate,.14f,grease),Mathf.Lerp(plate*1.02f,.12f,grease),1);
                });
                s.grease=ZoneKit.Toon("nacre-hatch",chart,m=>{m.SetColor("_BaseColor",Color.white);m.SetFloat("_RampSteps",4);m.SetFloat("_LightFloor",.55f);});
                // A soft round glow for every halo and the bruise: the shared flare's long spikes read as a lens flare, and from the flight camera as one more star.
                var soft=ZoneKit.Texture("Nacre/soft-glow",128,(u,v)=>{float d=Vector2.Distance(new Vector2(u,v),new Vector2(.5f,.5f))*2;float a=Mathf.Pow(Mathf.Clamp01(1-d),2.4f);return new Color(a,a,a,1);},false);
                Material Soft(string asset,Color colour){var m=kit.Glow(asset,false,colour);m.SetTexture("_BaseMap",soft);return m;}
                s.hullGlow=Soft("Beacon halo",new Color(.5f,1.05f,1.5f,1));
                s.lampGlow=Soft("Window glow",new Color(1.4f,1.1f,.7f,1));
                // The Choir's passage beyond the field: a bruise of light, green-yellow at its edge, blue-violet in it, never the flat purple of a missing shader.
                s.bruiseEdge=Soft("Bruise edge",new Color(.2f,.26f,.12f,1));
                s.bruiseCore=Soft("Bruise core",new Color(.22f,.17f,.36f,1));
                s.bruiseStreak=Soft("Bruise streak",new Color(.26f,.3f,.42f,1));
                return s;
            }

            // ---------------------------------------------------------------- the zone
            internal static void Build()
            {
                painted.Clear();
                var kit=ZoneKit.Flight(NacreZone);
                var scene=WorldBuilder.Begin(kit.zone);
                var stuff=Materials(kit);
                var arrival=WorldBuilder.Spawn("Arrival",Arrival);arrival.transform.rotation=Quaternion.LookRotation(Along(-5));
                kit.DriftingSky();
                var keep=Keep();

                // ---- the field: the walls a hull meets, the old salvage they are drawn as, the berth, the bruise beyond, the wreckage below
                var walls=Walls();
                Reef(kit,keep);
                Berth(kit,stuff);
                Bruise(stuff);
                Below(kit,keep);Shallows(kit,keep);FamilyHulls(kit);
                var glow=kit.Glow("Motes",false,new Color(1.2f,1.15f,1.1f,1));
                foreach(float turn in new[]{20f,110,200,290,380})kit.Drift("Drifting grit",At(turn,0,-2),new Vector3(70,10,70),glow,new Color(.9f,.92f,1,.24f),320,.05f,.16f,.1f,20);

                // ---- the three tracks the field turns on
                var tow=TowLine(kit,stuff);var race=Race(kit,stuff);var ring=BerthRing(kit,stuff);

                // ---- the three lamps, Neve, the lot, the tug's raft
                var hauler=Hauler_(kit,stuff);var tug=TugRaft(kit,stuff);var pod=Pod_(kit,stuff);Neve(kit,stuff);LotCrate(kit,stuff);
                var lamps=new GameObject("The three lamps found",typeof(FlagAll)).GetComponent<FlagAll>();lamps.flags=new[]{"home.odaFound","home.tugFound","home.navigatorsFound"};lamps.result="home.nacreDone";

                // ---- Bryn at the outer marker: the crossing that teaches the spiral, and her hails over the passes
                Marker(kit,stuff);
                var bryn=kit.Crossing("Bryn hails at the outer marker",At(MarkerTurn,0,0),new Vector3(Half*2+2,8,5),"home.nacreReached","BrynNacreApproach","Bryn");
                bryn.transform.rotation=Quaternion.Euler(0,MarkerTurn-90,0);bryn.situation="";
                Hail("hail.spiral",At(70,0),"nacre.hailOuter");Hail("hail.spiral",At(236,0),"nacre.hailInner");

                // ---- the fights, and the first sight of each thing the party is told about
                Fights(kit,out var outer,out var fragment);
                Sight("towdrone",null,outer,0,"nacre.towdroneSeen");
                Sight("fragment",Fragment,fragment,34,"nacre.fragmentSeen");
                for(int i=0;i<FamilyWrecks.Length;i++)Sight("wreck",At(FamilyWrecks[i],12),null,16,"nacre.wreck"+(i+1));
                // Into the inner spiral by the breach, and back out of the basin by the lane's mouth once Oda is found.
                Sight("spiral",BreachMouth,null,10,"nacre.spiralIn");
                var back=Sight("spiral",LaneMouth,null,12,"nacre.spiralOut");back.requiredFlag="home.odaFound";

                // ---- the way home
                var home=WorldBuilder.Warp("Warp — The Hull of Many Hands",HomeWarp,Hull,"",NacreSpawn);home.lockedPrompt="";
                WorldBuilder.Label("Warp — The Hull of Many Hands",HomeWarp+new Vector3(0,-.7f,-5),2.2f);
                WorldBuilder.Label("LINE 904",HaulerHail+new Vector3(0,-.7f,-6),2.6f);WorldBuilder.Label("WELCOME BACK",TugHail+new Vector3(0,-.7f,-6),2.6f);

                WorldBuilder.Save(scene,kit.zone);
                Debug.Log("HOME_COORDS NacreGrave arrival="+Arrival+" home-warp="+HomeWarp+" marker-line=turn "+MarkerTurn+" neve="+NeveHail+" tug="+TugHail+" tug-parcel="+TugParcel+" lot="+LotHand
                    +" hauler="+HaulerHail+" pod="+PodHail+" breach-mouth="+BreachMouth+" lane-mouth="+LaneMouth+" tows-outer="+TowsOuter+" tows-inner="+TowsInner+" tows-basin="+TowsBasin+" fragment="+Fragment
                    +$" tow-line traverse={tow.Traverse:0}s length={tow.Total:0}m race traverse={race.Traverse:0.0}s ring traverse={ring.Traverse:0.0}s walls={walls}");
                Debug.Log("NACRE_GRAVE_OK");
                _=hauler;_=tug;_=pod;
            }

            /// <summary>What must stay readable on screen, each with the room it needs: the ways, every prompt and the three lamps' hulls.</summary>
            static List<(Vector2 at,float clear)> Keep()
            {
                Vector2 F(Vector3 p)=>new(p.x,p.z);
                return new List<(Vector2,float)>{(F(Arrival),6),(F(HomeWarp),7),(F(NeveHail),8),(F(TugHail),10),(F(Lot),7),(F(HaulerHail),10),(F(Hauler),8),(F(Pod),7),(F(PodHail),5),(F(BreachMouth),6),(F(LaneMouth),6)};
            }

            // ---------------------------------------------------------------- the walls
            /// <summary>Every solid line a hull meets: the lane's outer and inner walls, its two ends, the basin's rim, the breach's sides and the mouth's jamb. Each is laid as short
            /// boxes along its curve, each a little longer than its piece so neighbours overlap: no box runs on past a bend into the lane (chapter 6's corridor ran each leg's walls
            /// 72 m past its ends and stood one across a current). Returns the number of boxes.</summary>
            static int Walls()
            {
                var root=new GameObject("Field limits");int count=0;
                void Line(IReadOnlyList<Vector2> line,string name)
                {
                    for(int i=1;i<line.Count;i++)
                    {
                        var a=line[i-1];var b=line[i];var run=b-a;if(run.sqrMagnitude<.01f)continue;
                        var wall=new GameObject(name+" "+i,typeof(BoxCollider));wall.isStatic=true;wall.transform.SetParent(root.transform,true);
                        wall.transform.SetPositionAndRotation(new Vector3((a.x+b.x)*.5f,Plane,(a.y+b.y)*.5f),Quaternion.Euler(0,Mathf.Atan2(run.x,run.y)*Mathf.Rad2Deg,0));
                        wall.GetComponent<BoxCollider>().size=new Vector3(2,12,run.magnitude+.5f);count++;
                    }
                }
                List<Vector2> Curve(float from,float to,Func<float,float> radius,float step=2){var points=new List<Vector2>();for(float t=from;t<to;t+=step)points.Add(On(t,radius(t)));points.Add(On(to,radius(to)));return points;}
                // The lane's outer wall: the field's own edge on the outer pass, and the old salvage between the passes on the inner one.
                Line(Curve(StartTurn,EndTurn,t=>R(t)+Half),"Outer wall");
                // The inner wall, broken by the breach, stopping at the mouth.
                float gapB=Spread(BreachHalf,R(BreachTurn)-Half);
                Line(Curve(StartTurn,BreachTurn-gapB,t=>R(t)-Half),"Inner wall");Line(Curve(BreachTurn+gapB,MouthTurn,t=>R(t)-Half),"Inner wall past the breach");
                // The basin's rim, from the lane's end round to the mouth, broken by the breach.
                float gapR=Spread(BreachHalf,BasinRim);
                Line(Curve(EndTurn-360,BreachTurn-gapR,_=>BasinRim),"Basin rim");Line(Curve(BreachTurn+gapR,MouthTurn,_=>BasinRim),"Basin rim past the breach");
                // The breach's two sides, from the rim's gap to the inner wall's.
                Line(new[]{On(BreachTurn-gapR,BasinRim),On(BreachTurn-gapB,R(BreachTurn-gapB)-Half)},"Breach side west");
                Line(new[]{On(BreachTurn+gapR,BasinRim),On(BreachTurn+gapB,R(BreachTurn+gapB)-Half)},"Breach side east");
                // The lane's two ends, and the jamb that closes the salvage between the rim and the inner wall where the mouth opens.
                Line(new[]{On(StartTurn,R(StartTurn)-Half),On(StartTurn,R(StartTurn)+Half)},"Lane's start");
                Line(new[]{On(EndTurn,R(EndTurn)+Half),On(EndTurn,BasinRim)},"Lane's end");
                Line(new[]{On(MouthTurn,BasinRim),On(MouthTurn,R(MouthTurn)-Half)},"Mouth jamb");
                // The berth itself: nothing flies into it.
                var berth=new GameObject("Berth body",typeof(SphereCollider));berth.isStatic=true;berth.transform.SetParent(root.transform,true);berth.transform.position=new Vector3(Centre.x,Plane,Centre.y);
                berth.GetComponent<SphereCollider>().radius=BerthSolid;count++;
                return count;
            }

            // ---------------------------------------------------------------- the old salvage spiral
            /// <summary>The reef the lane is cut through: old hulks packed along every wall's outside, three rows deep, standing a third of their height over the plane so the wall reads
            /// as a bank of wreckage at the frame's edges, in the salvage's greys and rust and turned any way. A hulk may lean out over the water a little (a hull is stopped 3 m short
            /// of a wall's line by the wall's own thickness and its hull), and is dropped, not moved, if its body would reach more than 2.5 m past the line (seeded: the reef stands
            /// the same every build).</summary>
            static void Reef(ZoneKit kit,List<(Vector2 at,float clear)> keep)
            {
                var rng=new System.Random(904);var root=new GameObject("The old salvage spiral");int made=0,dropped=0;
                void Stand(Vector2 at,float size)
                {
                    var (key,fit)=ReefHulks[rng.Next(ReefHulks.Length)];var old=Old[rng.Next(Old.Length)];var turned=Quaternion.Euler(rng.Next(-50,50),rng.Next(360),rng.Next(-60,60));
                    float s=size*(.7f+.5f*(float)rng.NextDouble())*fit/9f;float rise=.15f+.25f*(float)rng.NextDouble();
                    if(!ZoneKit.Landed(key))return;
                    var piece=kit.Loose(key,new Vector3(at.x,Plane,at.y),s,turned);var box=ZoneKit.Drawn(piece);piece.transform.position+=Vector3.up*(Plane+box.size.y*rise-box.max.y);box=ZoneKit.Drawn(piece);
                    // The body's footprint: dropped if any of it lies more than 2.5 m into open water.
                    bool intrudes=false;
                    for(float x=box.min.x;x<=box.max.x&&!intrudes;x+=1.2f)for(float z=box.min.z;z<=box.max.z&&!intrudes;z+=1.2f)
                    {var q=new Vector2(x,z);if(InLane(q,2.5f)||InBasin(q,2.5f)||InBreach(q,2.5f))intrudes=true;}
                    if(intrudes){UnityEngine.Object.DestroyImmediate(piece);dropped++;return;}
                    Paint(piece,old.name,old.tone);piece.transform.SetParent(root.transform,true);piece.isStatic=true;foreach(var t in piece.GetComponentsInChildren<Transform>())t.gameObject.isStatic=true;
                    Unshadowed(piece,true);made++;
                }
                float Step(float r,float metres)=>metres/r*Mathf.Rad2Deg;
                // Along the outer wall's outside, the lane's whole length: the field's edge, three rows.
                for(float t=StartTurn-6;t<=EndTurn+4;){float r=R(t)+Half;Stand(On(t,r+2.6f+(float)rng.NextDouble()),7);Stand(On(t+Step(r,2.5f),r+7.5f+(float)rng.NextDouble()*2),10);
                    Stand(On(t+Step(r,1.2f),r+14+(float)rng.NextDouble()*4),12);t+=Step(r,5);}
                // Along the inner wall's inside, from the start to the mouth: between the passes, and the salvage between the lane and the basin.
                for(float t=StartTurn-4;t<=MouthTurn;){float r=R(t)-Half;Stand(On(t,r-2.6f-(float)rng.NextDouble()),7);Stand(On(t+Step(r,2.5f),r-7.5f-(float)rng.NextDouble()*2),10);
                    if(t>92)Stand(On(t+Step(r,1.2f),r-14-(float)rng.NextDouble()*4),12);t+=Step(r,5);}
                // Round the basin's rim, outside it, two rows; and the breach's two banks.
                for(float t=92;t<=MouthTurn;t+=Step(BasinRim,4.5f)){Stand(On(t,BasinRim+2.6f+(float)rng.NextDouble()),7);Stand(On(t+Step(BasinRim,2.2f),BasinRim+7.5f+(float)rng.NextDouble()*2),9);}
                var ray=On(BreachTurn,1);var across=new Vector2(ray.y,-ray.x);
                foreach(int side in new[]{-1,1})for(float r=BasinRim+2;r<R(BreachTurn)-Half;r+=4.5f){Stand(On(BreachTurn,r)+across*side*(BreachHalf+2.6f),7);Stand(On(BreachTurn,r+2)+across*side*(BreachHalf+8),10);}
                // Filling the deep salvage between the basin's rim and the outer pass's inner wall.
                for(int k=0;k<260;k++){float t=92+(float)rng.NextDouble()*300,r=BasinRim+10+(float)rng.NextDouble()*(R(t)-Half-BasinRim-20);if(r>BasinRim+8)Stand(On(t,r),11);}
                Debug.Log("NACRE_REEF made="+made+" dropped="+dropped);
            }
            /// <summary>The reef's pieces: whole hulls and station sections, not crates, so the wall's silhouette is ships.</summary>
            static readonly (string key,float size)[] ReefHulks={("Hauler",11),("Skiff",6),("PatrolCutter",7),("CompactCutter",7),("TallowStationHull",12),("RingSegmentA",10),("RingSegmentB",10),
                ("DockingPodOffice",6),("DockingPodRepair",6),("DockingPodOutfitter",6),("SeedBarge",9),("TallowMooringCoupler",7)};

            // ---------------------------------------------------------------- the berth
            /// <summary>The single huge transfer berth, broken open: a ring of station segments round a hub, its docking arms bent or gone, its gash facing the breach the disaster blew
            /// out through. Drawn only (a sphere at the plane is what a hull meets); its lights are out but for a few cold work lamps.</summary>
            static void Berth(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject("The broken berth");var rng=new System.Random(41);
                var c=new Vector3(Centre.x,Plane,Centre.y);
                // The hub: a deck and a mast, under the plane, so the ring reads over it.
                var hub=new ZoneKit.Shape();
                hub.Slot(0).Disc(c+Vector3.down*2.4f,Vector3.up,15,40,new Color(.62f,.62f,.66f).linear,0,new Color(.44f,.44f,.48f).linear);
                hub.Slot(0).Tube(new[]{c+Vector3.down*14,c+Vector3.down*2.4f,c+Vector3.up*3},new[]{5f,4f,2.4f},16,new Color(.5f,.5f,.54f).linear,new Color(.72f,.72f,.76f).linear,1,.25f);
                // Spokes from the hub to the ring, two of them snapped.
                for(int k=0;k<8;k++){float t=k*45+10;if(Mathf.Abs(Mathf.DeltaAngle(t,BreachTurn))<30)continue;var from=c+Vector3.down*2.2f+Quaternion.Euler(0,t,0)*Vector3.forward*14;var to=c+Vector3.down*1.6f+Quaternion.Euler(0,t,0)*Vector3.forward*(k%3==1?21:26);
                    hub.Slot(1).Beam(from,to,1.6f,1.1f,new Color(.56f,.56f,.6f).linear);}
                // The docking arms, out past the ring: some whole with a dead lamp at the end, some bent, the ones toward the gash torn off short.
                for(int k=0;k<10;k++)
                {
                    float t=k*36+4;float torn=Mathf.Abs(Mathf.DeltaAngle(t,BreachTurn));if(torn<24)continue;
                    var root2=c+Quaternion.Euler(0,t,0)*Vector3.forward*27+Vector3.down*1.4f;float bend=k%3==0?28:0;var way=Quaternion.Euler(0,t+bend,0)*Vector3.forward;
                    hub.Slot(1).Beam(root2,root2+way*(torn<60?3.5f:6.5f)+Vector3.down*(k%2==0?1.2f:.4f),1.2f,.9f,new Color(.6f,.6f,.64f).linear);
                }
                var built=kit.Built("Berth hub",hub,stuff.berthMetal,stuff.berthDark);built.transform.SetParent(root.transform,true);Unshadowed(built,true);
                // The ring: station segments end to end round the hub, the stretch toward the breach gone.
                string[] segments={"RingSegmentA","RingSegmentB"};int made=0;
                for(int k=0;k<16;k++)
                {
                    float t=k*22.5f+6;if(Mathf.Abs(Mathf.DeltaAngle(t,BreachTurn))<34)continue;
                    string key=segments[k%2];if(!ZoneKit.Landed(key))continue;
                    var at=c+Quaternion.Euler(0,t,0)*Vector3.forward*25+Vector3.down*(1.5f+(k%3)*.5f);
                    var piece=kit.Loose(key,at,11,Quaternion.Euler(rng.Next(-8,8),t+90,rng.Next(-10,10)));Paint(piece,"berth",new Color(.82f,.82f,.86f).linear);piece.transform.SetParent(root.transform,true);Unshadowed(piece,true);made++;
                }
                // The torn edges either side of the gash: two segments wrenched out and down, and the wreckage that poured out of it, lying toward the breach.
                foreach(int side in new[]{-1,1})
                {
                    float t=BreachTurn+side*30;var at=c+Quaternion.Euler(0,t,0)*Vector3.forward*27+Vector3.down*3;
                    if(ZoneKit.Landed("RingSegmentB")){var piece=kit.Loose("RingSegmentB",at,10,Quaternion.Euler(side*34,t+90+side*40,side*22));Paint(piece,"berth",new Color(.7f,.7f,.74f).linear);piece.transform.SetParent(root.transform,true);Unshadowed(piece,true);}
                }
                // A few cold work lamps still burning on the ring: a light over the berth so it reads as a structure, not a hole.
                foreach(float t in new[]{40f,130,250,320})
                {
                    var lamp=new GameObject("Berth work lamp",typeof(Light));lamp.transform.SetParent(root.transform,true);lamp.transform.position=c+Quaternion.Euler(0,t,0)*Vector3.forward*25+Vector3.up*3;
                    var l=lamp.GetComponent<Light>();l.type=LightType.Point;l.range=16;l.intensity=2.2f;l.color=new Color(.72f,.86f,1);l.shadows=LightShadows.None;
                }
                WorldBuilder.Label("THE NACRE BERTH",c+new Vector3(0,-.7f,-34),3.2f).transform.SetParent(root.transform,true);
                Debug.Log("NACRE_BERTH segments="+made);
            }

            // ---------------------------------------------------------------- the Choir's passage
            /// <summary>Far under the field, where it shows behind the berth: soft light in a bruise's colours, wide enough to stand behind every frame of the basin and the inner pass.</summary>
            static void Bruise(Stuff stuff)
            {
                var root=new GameObject("The Choir's passage beyond");
                void Glow(Material material,Vector2 shows,float depth,float size,float roll)
                {
                    var at=new Vector3(shows.x,Plane-depth,shows.y+depth/Mathf.Tan(ZoneKit.FlightPitch*Mathf.Deg2Rad));
                    var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());quad.name="Bruise";quad.transform.SetParent(root.transform,true);
                    quad.transform.SetPositionAndRotation(at,Quaternion.Euler(ZoneKit.FlightPitch,0,roll));quad.transform.localScale=new Vector3(size,size*.62f,1);
                    var view=quad.GetComponent<Renderer>();view.sharedMaterial=material;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
                }
                Glow(stuff.bruiseEdge,new Vector2(-10,0),300,420,-18);Glow(stuff.bruiseCore,new Vector2(-20,-10),290,240,-24);
                Glow(stuff.bruiseStreak,new Vector2(30,-30),280,300,-30);Glow(stuff.bruiseCore,new Vector2(40,60),300,180,12);
            }

            // ---------------------------------------------------------------- the wreckage below
            /// <summary>Wreckage 6 to 40 m under the plane, fresh and old, so every frame has depth beneath the lane: kept clear of what must be read by where it shows on screen.</summary>
            static void Below(ZoneKit kit,List<(Vector2 at,float clear)> keep)
            {
                var rng=new System.Random(411);var root=new GameObject("Wreckage below");int made=0;
                for(int k=0;k<420;k++)
                {
                    float t=(float)rng.NextDouble()*470-20,r=30+(float)rng.NextDouble()*170;var p=On(t,r);float depth=6+(float)rng.NextDouble()*(float)rng.NextDouble()*34;
                    var at=new Vector3(p.x,Plane-depth,p.y);var (key,fit)=Hulks[rng.Next(Hulks.Length)];float size=(2+(float)rng.NextDouble()*6)*fit/9f;
                    bool fresh=k%4==0;var tone=fresh?Families[rng.Next(Families.Length)]:Old[rng.Next(Old.Length)];var turned=Quaternion.Euler(rng.Next(360),rng.Next(360),rng.Next(360));
                    if(!ZoneKit.ClearOnScreen(at,size*1.2f,keep)||!ZoneKit.Landed(key))continue;
                    var piece=kit.Loose(key,at,size,turned);Paint(piece,tone.name+(fresh?"":"-deep"),fresh?tone.tone*.85f:tone.tone*.7f);
                    piece.transform.SetParent(root.transform,true);piece.isStatic=true;foreach(var tr in piece.GetComponentsInChildren<Transform>())tr.gameObject.isStatic=true;Unshadowed(piece);made++;
                }
                Debug.Log("NACRE_BELOW made="+made);
            }

            /// <summary>Wreckage just under the lane, 2 to 9 m down: small pieces of the families' hulls and the old salvage, lit, so every frame of the lane has something under the
            /// ship and the turning is felt. Under the plane, so a hull passes over them; kept clear of what must be read by where they show.</summary>
            static void Shallows(ZoneKit kit,List<(Vector2 at,float clear)> keep)
            {
                var rng=new System.Random(452);var root=new GameObject("Wreckage in the lane");int made=0;
                string[] keys={"Skiff","PatrolCutter","CompactCutter","DockingPodOffice","DockingPodRepair","MusterCrate","DressCargoPallet","DressFuelTank","DeckCrate","TallowMooringCoupler","RingSegmentA"};
                for(int k=0;k<1000;k++)
                {
                    Vector2 p;
                    if(k%7==0){float t=(float)rng.NextDouble()*360,r=BerthSolid+4+(float)rng.NextDouble()*(BasinRim-BerthSolid-6);p=On(t,r);}
                    else{float t=StartTurn+(float)rng.NextDouble()*(EndTurn-StartTurn),off=((float)rng.NextDouble()*2-1)*(Half+2);p=On(t,R(t)+off);}
                    float depth=2+(float)rng.NextDouble()*7,size=1+(float)rng.NextDouble()*(float)rng.NextDouble()*4;var key=keys[rng.Next(keys.Length)];
                    bool fresh=k%3==0;var tone=fresh?Families[rng.Next(Families.Length)]:Old[rng.Next(Old.Length)];var turned=Quaternion.Euler(rng.Next(360),rng.Next(360),rng.Next(360));
                    var at=new Vector3(p.x,Plane-depth,p.y);
                    if(!ZoneKit.ClearOnScreen(at,size,keep)||!ZoneKit.Landed(key))continue;
                    var piece=kit.Loose(key,at,size,turned);Paint(piece,tone.name,tone.tone);
                    piece.transform.SetParent(root.transform,true);piece.isStatic=true;foreach(var tr in piece.GetComponentsInChildren<Transform>())tr.gameObject.isStatic=true;Unshadowed(piece);made++;
                }
                Debug.Log("NACRE_SHALLOWS made="+made);
            }
            /// <summary>Wrecks in the families' colours at the lane's outer edge (each said once as it is passed: <c>wreck</c>), solid, hulls of the queue that was at the door.</summary>
            static void FamilyHulls(ZoneKit kit)
            {
                var root=new GameObject("The families' wrecks");var rng=new System.Random(3);string[] keys={"PatrolCutter","Skiff","DockingPodOffice"};
                for(int i=0;i<FamilyWrecks.Length;i++)
                {
                    var at=At(FamilyWrecks[i],12.6f);var family=Families[(i*2+1)%Families.Length];var key=keys[i%keys.Length];if(!ZoneKit.Landed(key))continue;
                    var piece=kit.Loose(key,at,6,Quaternion.Euler(rng.Next(-20,20),rng.Next(360),rng.Next(-25,25)));Paint(piece,family.name,family.tone);
                    piece.transform.SetParent(root.transform,true);Unshadowed(piece,true);Solid(root.transform,at,2.8f,"A family's wreck");
                }
            }

            // ---------------------------------------------------------------- the tracks
            /// <summary>A track's lamps: a bead every <paramref name="spacing"/> metres along its line just under the plane, on a dark cable a metre under, lit from a texture the track
            /// writes each frame (and here once, as it stands the moment the zone opens).</summary>
            static void Lamps(ZoneKit kit,Stuff stuff,WreckTrack track,string slug,float from,float to,float spacing)
            {
                int count=Mathf.Max(1,Mathf.FloorToInt((to-from)/spacing)+1);var beads=new ZoneKit.Shape();var cable=new List<Vector3>();var radii=new List<float>();
                for(int k=0;k<count;k++){float a=from+k*spacing;beads.Box(track.PointAt(a)+Vector3.down*.55f,new Vector3(.7f,.24f,1.5f),Quaternion.LookRotation(track.TangentAt(a)),Color.white);}
                for(float a=from;a<=to;a+=3){cable.Add(track.PointAt(a)+Vector3.down*1.05f);radii.Add(.28f);}
                var mesh=beads.Mesh(slug+" lamps");var uv=new Vector2[mesh.vertexCount];for(int v=0;v<uv.Length;v++)uv[v]=new Vector2((v/24+.5f)/count,.5f);mesh.uv=uv;
                mesh=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+kit.zone+"-"+slug+"-lamps.asset");
                var strip=new GameObject(track.trackName+" lamps",typeof(MeshFilter),typeof(MeshRenderer));strip.transform.SetParent(track.transform,true);strip.GetComponent<MeshFilter>().sharedMesh=mesh;
                track.lampStrip=strip.GetComponent<MeshRenderer>();track.lampFrom=from;track.lampSpacing=spacing;track.lampCount=count;
                string path="Assets/_Project/Art/Generated/Nacre/nacre-lamps-"+slug+".asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if(texture==null||texture.width!=count){if(texture!=null)AssetDatabase.DeleteAsset(path);System.IO.Directory.CreateDirectory("Assets/_Project/Art/Generated/Nacre");
                    texture=new Texture2D(count,1,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};AssetDatabase.CreateAsset(texture,path);}
                var pixels=new Color32[count];for(int k=0;k<count;k++)pixels[k]=track.Lamp(k,0);texture.SetPixels32(pixels);texture.Apply(false);EditorUtility.SetDirty(texture);
                track.lampStrip.sharedMaterial=ZoneKit.Toon("nacre-lamps-"+slug,texture,m=>{m.SetColor("_BaseColor",Color.white);m.SetTexture("_EmissionMap",texture);m.SetColor("_EmissionColor",new Color(1.6f,1.6f,1.6f));m.SetFloat("_LightFloor",1);});
                Unshadowed(strip);
                if(cable.Count>1){var line=kit.Built(slug+" cable",new ZoneKit.Shape().Slot(0).Tube(cable,radii,6,new Color(.32f,.34f,.38f),new Color(.32f,.34f,.38f),1,.3f,false),stuff.cable);line.transform.SetParent(track.transform,true);Unshadowed(line);}
            }
            static WreckTrack Track(string name,IEnumerable<Vector3> path,bool closed,float speed,float refRadius,float steep,float min,float max,float pieceRadius,float strike,float throwClear)
            {
                var root=new GameObject(name);var track=root.AddComponent<WreckTrack>();track.trackName=name;track.path=path.ToArray();track.closed=closed;track.centre=Centre;
                track.speed=speed;track.refRadius=refRadius;track.steep=steep;track.minSpeed=min;track.maxSpeed=max;track.pieceRadius=pieceRadius;track.lead=2.5f;track.plane=Plane;
                track.sink=8;track.sinkLength=16;track.strike=strike;track.throwClear=throwClear;track.markRange=55;track.markStep=6;track.markWidth=pieceRadius*2+1;return track;
            }
            /// <summary>A fresh wreck riding a track: a hull in a family's colour, turned and tumbling on its own "Body", optionally hauled by a tow-drone ahead of it on a line.</summary>
            static Transform Rider(ZoneKit kit,Stuff stuff,WreckTrack track,int index,System.Random rng,float size,bool towed)
            {
                var piece=new GameObject(track.trackName+" wreck "+(index+1)).transform;piece.SetParent(track.transform,true);
                var body=new GameObject("Body").transform;body.SetParent(piece,false);
                string[] keys={"Hauler","Skiff","PatrolCutter","DockingPodOffice","SeedBarge","CompactCutter"};var key=keys[(index+rng.Next(keys.Length))%keys.Length];var family=Families[(index*2+rng.Next(2))%Families.Length];
                if(ZoneKit.Landed(key))
                {
                    var hull=kit.Loose(key,Vector3.zero,size,Quaternion.Euler(rng.Next(-30,30),rng.Next(360),rng.Next(-40,40)));Paint(hull,family.name,family.tone);
                    hull.transform.SetParent(body,true);hull.transform.localPosition-=ZoneKit.Drawn(hull).center;Unshadowed(hull,true);
                }
                if(towed)
                {
                    // The drone ahead on its line, still hauling: its body as scenery, nothing to fight (the tow-drones that fight are the encounters').
                    var drone=Drawn("TowDrone",Vector3.zero,2.1f,0)??Drawn("ClaimDrone",Vector3.zero,2.1f,0);
                    if(drone!=null){drone.transform.SetParent(piece,false);drone.transform.localPosition=new Vector3(0,.4f,size*.5f+5.5f);drone.name="Tow-drone hauling";}
                    var rope=kit.Built(track.trackName+" tow line "+(index+1),new ZoneKit.Shape().Slot(0).Rope(new Vector3(0,.4f,size*.4f),new Vector3(0,.5f,size*.5f+4.6f),.25f,.06f,new Color(.9f,.82f,.58f)),stuff.rope);
                    rope.transform.SetParent(piece,false);Unshadowed(rope);
                }
                return piece;
            }
            static WreckTrack TowLine(ZoneKit kit,Stuff stuff)
            {
                var path=new List<Vector3>();for(float t=TowFrom;t<=TowTo;t+=1.5f)path.Add(At(t,TowOffset));
                var track=Track("The tow line",path,false,TowSpeed,TowRef,2,2,TowMax,3.5f,.08f,5);track.phase=12;
                var rng=new System.Random(73);var pieces=new List<Transform>();for(int i=0;i<TowPieces;i++)pieces.Add(Rider(kit,stuff,track,i,rng,7,true));
                track.pieces=pieces.ToArray();track.Pose(0);
                Lamps(kit,stuff,track,"tow-line",track.sinkLength,track.Total-track.sinkLength,2.5f);
                return track;
            }
            static WreckTrack Race(ZoneKit kit,Stuff stuff)
            {
                var path=new List<Vector3>();for(float t=RaceFrom;t<=RaceTo;t+=1)path.Add(In(t,RaceRadius));
                var track=Track("The breach race",path,false,RaceSpeed,RaceRadius,0,RaceSpeed,RaceSpeed,4,.14f,7);track.phase=3;
                var rng=new System.Random(91);var pieces=new List<Transform>();for(int i=0;i<RacePieces;i++)pieces.Add(Rider(kit,stuff,track,i,rng,8,false));
                track.pieces=pieces.ToArray();track.Pose(0);
                Lamps(kit,stuff,track,"breach-race",track.sinkLength,track.Total-track.sinkLength,2.2f);
                return track;
            }
            static WreckTrack BerthRing(ZoneKit kit,Stuff stuff)
            {
                var path=new List<Vector3>();for(float t=0;t<360;t+=4)path.Add(In(t,RingRadius));
                var track=Track("The berth ring",path,true,RingSpeed,RingRadius,0,RingSpeed,RingSpeed,4,.14f,7);track.phase=6;
                var rng=new System.Random(48);var pieces=new List<Transform>();for(int i=0;i<RingPieces;i++)pieces.Add(Rider(kit,stuff,track,i,rng,8,false));
                track.pieces=pieces.ToArray();track.Pose(0);
                Lamps(kit,stuff,track,"berth-ring",0,track.Total-2.2f,2.2f);
                return track;
            }

            // ---------------------------------------------------------------- the three lamps
            /// <summary>An evacuation call: a lamp that blinks a slow double pulse until its flag is set (<see cref="SignalBeacon"/>), with a light and a halo the flight camera reads from its height.</summary>
            static void Beacon(Stuff stuff,Transform under,Vector3 at,string flag)
            {
                var lamp=new GameObject("Evacuation lamp",typeof(SignalBeacon));lamp.transform.SetParent(under,true);lamp.transform.position=at;
                var bulb=GameObject.CreatePrimitive(PrimitiveType.Sphere);UnityEngine.Object.DestroyImmediate(bulb.GetComponent<Collider>());bulb.name="Lamp";bulb.transform.SetParent(lamp.transform,false);bulb.transform.localScale=Vector3.one*.7f;
                bulb.GetComponent<Renderer>().sharedMaterial=stuff.hullGlow;
                var light=new GameObject("Lamp light",typeof(Light)).GetComponent<Light>();light.transform.SetParent(lamp.transform,false);light.transform.localPosition=Vector3.up*1.2f;
                light.type=LightType.Point;light.range=20;light.intensity=3;light.color=new Color(.55f,.8f,1);light.shadows=LightShadows.None;
                var halo=GameObject.CreatePrimitive(PrimitiveType.Quad);UnityEngine.Object.DestroyImmediate(halo.GetComponent<Collider>());halo.name="Lamp halo";halo.transform.SetParent(lamp.transform,false);
                halo.transform.localPosition=Vector3.up*.4f;halo.transform.rotation=Quaternion.Euler(ZoneKit.FlightPitch,0,0);halo.transform.localScale=Vector3.one*5;
                var haloView=halo.GetComponent<Renderer>();haloView.sharedMaterial=stuff.hullGlow;haloView.shadowCastingMode=ShadowCastingMode.Off;
                var beacon=lamp.GetComponent<SignalBeacon>();beacon.flag=flag;beacon.glow=light;beacon.lamp=bulb.GetComponent<Renderer>();beacon.intensity=3;
            }
            /// <summary>A hull the party can't fly through: a sphere at the plane under the drawn body.</summary>
            static void Solid(Transform under,Vector3 at,float radius,string name)
            {
                var solid=new GameObject(name,typeof(SphereCollider));solid.transform.SetParent(under,true);solid.transform.position=new Vector3(at.x,Plane,at.z);solid.isStatic=true;solid.GetComponent<SphereCollider>().radius=radius;
            }
            /// <summary>Oda's hauler: line nine hundred and four, still in the family's ochre, everything they own strapped to the outside, a rope to the next hull, the jammed door, a lamp
            /// in the window and the evacuation lamp on its spine. Somewhere a family has been holding on.</summary>
            static GameObject Hauler_(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject("Oda's hauler, line nine hundred and four");var along=Quaternion.LookRotation(Along(62)).eulerAngles.y;var turn=Quaternion.Euler(0,along,0);
                var top=new Bounds(Hauler,new Vector3(5,2.4f,10));
                if(ZoneKit.Landed("Hauler"))
                {
                    // Fitted by height: at 12 it stood twenty metres long and filled the frame.
                    var hull=kit.Prop("Hauler",Hauler,4.2f,along,false,false);hull.transform.position+=Hauler-ZoneKit.Drawn(hull).center+Vector3.down*.8f;Paint(hull,"oda",new Color(.84f,.78f,.62f));
                    hull.transform.SetParent(root.transform,true);Unshadowed(hull,true);top=ZoneKit.Drawn(hull);
                }
                // Everything they own, strapped to the outside: the kit's own crates, netted pallets and drums, a household's worth, lashed down in rows with lines across the hull, and a
                // roll of bedding at each row's end. The crates, sacks and trunks were drawn in code (pale blocks and flat lumps beside a generated hull: pass 3); now they are kit pieces.
                // Colours are written linear (a vertex colour isn't converted, and as written they came out chalky pastels).
                var s=new ZoneKit.Shape();var rng=new System.Random(904);
                Color L(float r,float g,float b)=>new Color(r,g,b).linear;
                Color[] dyes={L(.42f,.5f,.6f),L(.62f,.52f,.36f),L(.46f,.56f,.4f),L(.56f,.4f,.36f)};
                float halfX=top.size.x*.32f,halfZ=top.size.z*.34f,y=top.max.y-.15f;
                var right=turn*Vector3.right;var ahead=turn*Vector3.forward;var mid=new Vector3(top.center.x,y,top.center.z);
                string[] goods={"DressCargoPallet","MusterCrate","DeckCrate","DressFuelTank","DressCargoPallet","MusterCrate"};int good=0;
                for(int row=0;row<3;row++)
                {
                    float z=-halfZ+row*halfZ;
                    foreach(float x in new[]{-halfX*.55f,halfX*.55f})
                    {
                        string key=goods[good++%goods.Length];if(!ZoneKit.Landed(key))continue;
                        // A metre and a bit each: at a metre and a half the pile hid the hull and its paint (pass 4).
                        var piece=kit.Loose(key,mid+right*x+ahead*z,1.0f+(float)rng.NextDouble()*.25f,turn*Quaternion.Euler(0,rng.Next(-14,14)+(row%2)*90,0));
                        var drawn=ZoneKit.Drawn(piece);piece.transform.position+=Vector3.up*(y-.05f-drawn.min.y);piece.transform.SetParent(root.transform,true);Unshadowed(piece,true);
                    }
                    var tone=dyes[row%dyes.Length];var roll=mid+right*(halfX+.25f)+ahead*z+Vector3.up*.3f;
                    s.Slot(0).Tube(new[]{roll-ahead*.5f,roll+ahead*.5f},new[]{.3f,.3f},10,tone,ZoneKit.Shape.Dim(tone,.8f),1,.5f);
                    // A line across the hull over each row, tied off at both flanks.
                    s.Slot(1).Rope(mid-right*(halfX+.6f)+ahead*z-Vector3.up*.5f,mid+right*(halfX+.6f)+ahead*z-Vector3.up*.5f,-1.7f,.05f,L(.22f,.2f,.18f));
                }
                // The jammed door on the flank, a hand's width open, a plate bent across it.
                s.Slot(2).Box(mid+right*(top.size.x*.5f)+Vector3.down*1.2f,new Vector3(.14f,1.5f,1.1f),turn*Quaternion.Euler(0,22,8),L(.42f,.4f,.36f));
                // A line to the next hull over, so they kept together.
                var tied=mid-right*(halfX+.4f)-ahead*halfZ;s.Slot(3).Rope(tied,Hauler+turn*new Vector3(-9,-.6f,-11),.8f,.07f,L(.92f,.84f,.6f));
                var gear=kit.Built("Oda's hauler gear",s,ZoneKit.Tinted(ZoneKit.Toon("nacre-cloth",null,m=>m.SetFloat("_RampSteps",4))),stuff.rope,stuff.cable,stuff.rope);gear.transform.SetParent(root.transform,true);Unshadowed(gear,true);
                if(ZoneKit.Landed("Skiff")){var next=kit.Loose("Skiff",Hauler+turn*new Vector3(-9,-1.4f,-12),6,turn*Quaternion.Euler(10,30,-14));Paint(next,"teal",Families[0].tone);next.transform.SetParent(root.transform,true);Unshadowed(next,true);}
                // A lamp lit inside, at the nose: somebody is in there.
                var window=GameObject.CreatePrimitive(PrimitiveType.Quad);UnityEngine.Object.DestroyImmediate(window.GetComponent<Collider>());window.name="A lamp in the window";window.transform.SetParent(root.transform,true);
                window.transform.SetPositionAndRotation(mid+ahead*(top.size.z*.42f)+Vector3.up*.2f,Quaternion.Euler(ZoneKit.FlightPitch,0,0));window.transform.localScale=Vector3.one*2.2f;window.GetComponent<Renderer>().sharedMaterial=stuff.lampGlow;
                var warm=new GameObject("Window light",typeof(Light)).GetComponent<Light>();warm.transform.SetParent(window.transform,false);warm.type=LightType.Point;warm.range=8;warm.intensity=1.4f;warm.color=new Color(1,.8f,.55f);warm.shadows=LightShadows.None;
                Beacon(stuff,root.transform,mid-ahead*(top.size.z*.3f)+Vector3.up*1.4f,"home.odaFound");
                Solid(root.transform,Hauler,Mathf.Max(3,Mathf.Min(top.size.x,top.size.z)*.45f),"Oda's hauler body");
                var hail=kit.Discovery("Line nine hundred and four",HaulerHail-Vector3.up*Plane,"Hail — Line nine hundred and four","home.odaFound","OdaHauler","OdaFound","Oda",null,0,WorldBuilder.FlightDockRange);
                hail.requiredFlag="home.nacreSpoken";hail.transform.SetParent(root.transform,true);
                Debug.Log($"NACRE_HAULER size={top.size}");
                return root;
            }
            /// <summary>The tug Welcome Back, as Many Hands will moor her: a squat working hull in the Nacre approach's green with fenders all round, a tall bridge, the tow boom, and a raft of
            /// hulls on lines from every cleat. Bryn's hail and the first parcel's hand beside her.</summary>
            static GameObject TugRaft(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject("The tug Welcome Back");var c=Tug+Vector3.down*.6f;var turn=Quaternion.LookRotation(-Along(TugTurn));
                Color trim=new Color(.9f,.84f,.62f).linear,rubber=new Color(.13f,.125f,.12f);
                // Her hull is a working cutter's, in the Nacre approach's green with cream trim (Many Hands moors her so); fenders hung along both flanks, and her tow boom off the stern.
                // Hull, fenders and boom are laid with the hull unturned, measured, and then turned to the lane together: laid in the lane's frame from fixed numbers, the fenders were
                // eight black drums standing off a hull longer and narrower than the frame they were set out in, and the boom a plank in the air (pass 3).
                var pivot=new GameObject("Her hull").transform;pivot.SetParent(root.transform,false);pivot.position=c;float yaw=turn.eulerAngles.y;
                var s=new ZoneKit.Shape();
                if(ZoneKit.Landed("PatrolCutter"))
                {
                    var body=kit.Prop("PatrolCutter",c,9,0,false,false);body.transform.position+=c-ZoneKit.Drawn(body).center;Paint(body,"tug",new Color(.5f,.9f,.78f));
                    var box=ZoneKit.Drawn(body);bool alongX=box.size.x>=box.size.z;var length=alongX?Vector3.right:Vector3.forward;var across=alongX?Vector3.forward:Vector3.right;
                    float half=alongX?box.extents.x:box.extents.z,side=alongX?box.extents.z:box.extents.x;
                    for(int k=0;k<4;k++)foreach(int sign in new[]{-1,1})
                    {
                        // At .62 of the box's half width: the box takes in her fins, and fenders at its edge stood off her sides (pass 4).
                        var f=box.center+length*(-half*.5f+k*half*.33f)+across*sign*side*.62f-Vector3.up*box.extents.y*.1f;
                        s.Slot(2).Tube(new[]{f-length*.5f,f+length*.5f},new[]{.34f,.34f},10,rubber,ZoneKit.Shape.Dim(rubber,.7f),1,1);
                    }
                    var stern=box.center-length*half*.85f+Vector3.up*box.extents.y*.35f;
                    s.Slot(0).Beam(stern,stern-length*3.6f+Vector3.up*1.6f,.32f,.32f,trim);
                    body.transform.SetParent(pivot,true);Unshadowed(body,true);
                    // The lane's way is her length: a hull that lies along x turns a quarter less than one that lies along z.
                    if(alongX)yaw-=90;
                }
                var hull=kit.Built("The tug Welcome Back",s,stuff.berthMetal,stuff.cable,stuff.berthMetal);hull.transform.SetParent(pivot,true);Unshadowed(hull,true);
                pivot.rotation=Quaternion.Euler(0,yaw,0);
                // The raft: hulls on lines from her cleats, held together as she has held them since the door went. The two out in the salvage lie off to either side, so no line runs
                // across the hail point south of her (two crossed the ship stopped there: pass 3). The four in the lane lie in line with her, 6.5 to 7 m out (their solids 4.3 to 9.5),
                // so the water inside them stays open by 9.3 m (the class comment on Tug) and the strip outside them still leads to her hail, the way chapter 9's routes fly (laid
                // against the wall they shut that strip). Offsets stay positive: tools/home_routes.py reads this list by its pattern.
                var raft=new[]{(At(TugTurn-7,6.5f),"Skiff",6f,1),(At(TugTurn+7,7),"SeedBarge",7f,3),(At(TugTurn-13,6.5f),"DockingPodOffice",5f,4),(At(TugTurn+13,6.5f),"PatrolCutter",6f,2),(At(TugTurn-10,19),"Hauler",4f,0),(At(TugTurn+11,20),"CompactCutter",7f,3)};
                var rng=new System.Random(176);var lines=new ZoneKit.Shape();
                foreach(var (at,key,size,family) in raft)
                {
                    if(ZoneKit.Landed(key)){var p=kit.Loose(key,at+Vector3.down*.8f,size,Quaternion.Euler(rng.Next(-12,12),rng.Next(360),rng.Next(-14,14)));Paint(p,Families[family].name,Families[family].tone);p.transform.SetParent(root.transform,true);Unshadowed(p,true);}
                    lines.Slot(0).Rope(c+(at-c).normalized*2.4f+Vector3.up*.4f,at+(c-at).normalized*size*.35f,1.2f,.07f,new Color(.92f,.84f,.6f));
                    if(Vector2.Distance(new Vector2(at.x,at.z),Centre)<R(TugTurn)+Half-1)Solid(root.transform,at,size*.36f,"Raft hull");
                }
                var built=kit.Built("The tug's lines",lines,stuff.rope);built.transform.SetParent(root.transform,true);Unshadowed(built,true);
                Beacon(stuff,root.transform,c+turn*new Vector3(0,3.4f,-1.2f),"home.tugFound");Solid(root.transform,Tug,3.8f,"The tug's body");
                var hail=kit.Discovery("The tug Welcome Back",TugHail-Vector3.up*Plane,"Hail — The tug Welcome Back","home.tugFound","WelcomeBack","BrynWelcomeBack","Bryn",null,0,WorldBuilder.FlightDockRange);
                hail.requiredFlag="home.nacreSpoken";hail.transform.SetParent(root.transform,true);
                // Neve's first parcel, for the tug (ReturnAddress step 0): handed across her rail.
                var parcel=kit.Discovery("A parcel for the tug",TugParcel-Vector3.up*Plane,"Hand over the parcel","neve.parcelTug","TugParcel",null,"Sela","ReturnAddress",0,WorldBuilder.FlightDockRange);
                parcel.situation="parcel.tug";parcel.transform.SetParent(root.transform,true);
                return root;
            }
            /// <summary>The navigators' pod: a capsule built for two, holed, lodged against the berth inside its ring; the field drawn in grease on its hatch, and its lamp.</summary>
            static GameObject Pod_(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject("The navigators' pod");var c=Pod+Vector3.down*.4f;var turn=Quaternion.LookRotation(Along(318))*Quaternion.Euler(0,0,12);
                var s=new ZoneKit.Shape();Color plate=new(.86f,.84f,.8f);
                s.Slot(0).Tube(new[]{c+turn*new Vector3(0,0,-2.6f),c+turn*new Vector3(0,0,-2),c+turn*new Vector3(0,0,1.8f),c+turn*new Vector3(0,0,2.5f)},new[]{.6f,1.5f,1.5f,.7f},18,plate,ZoneKit.Shape.Dim(plate,.85f),1,.4f);
                // The hole, patched with a coat.
                s.Slot(0).Lump(c+turn*new Vector3(-1.3f,.4f,.6f),new Vector3(.5f,.45f,.6f),3,.3f,new Color(.38f,.4f,.46f).linear,new Color(.3f,.3f,.34f).linear,8,4);
                // The hatch on its top, the field charted on it in grease: a plate laid over the curve. Laid at 1.42 it ran into the hull's own skin and showed as two broken arcs (pass 3).
                s.Slot(1).Quad(c+turn*new Vector3(-.9f,1.56f,-1.4f),c+turn*new Vector3(.9f,1.56f,-1.4f),c+turn*new Vector3(.9f,1.56f,1.2f),c+turn*new Vector3(-.9f,1.56f,1.2f),Vector3.up,Color.white,.38f);
                var built=kit.Built("The navigators' pod",s,stuff.berthMetal,stuff.grease);built.transform.SetParent(root.transform,true);Unshadowed(built,true);
                Beacon(stuff,root.transform,c+turn*new Vector3(0,1.8f,1.9f),"home.navigatorsFound");Solid(root.transform,Pod,2.4f,"The pod's body");
                var hail=kit.Discovery("The navigators' pod",PodHail-Vector3.up*Plane,"Hail — The navigators' pod","home.navigatorsFound","NavigatorsPod","BrynNavigators","Bryn",null,0,WorldBuilder.FlightDockRange);
                hail.requiredFlag="home.nacreSpoken";hail.transform.SetParent(root.transform,true);
                return root;
            }
            /// <summary>Neve's skiff on the outer pass, a pod of survivors in tow: her offer (ReturnAddress), once Bryn has spoken.</summary>
            static void Neve(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject("Neve's skiff");var turn=Quaternion.LookRotation(Along(40));
                if(ZoneKit.Landed("Skiff")){var skiff=kit.Prop("Skiff",NeveSkiff,5.5f,turn.eulerAngles.y,false,false);skiff.transform.position+=NeveSkiff-ZoneKit.Drawn(skiff).center;skiff.transform.SetParent(root.transform,true);Unshadowed(skiff,true);}
                var s=new ZoneKit.Shape();var pod=NeveSkiff+turn*new Vector3(0,-.3f,-6);
                s.Slot(0).Tube(new[]{pod+turn*new Vector3(0,0,-1.4f),pod+turn*new Vector3(0,0,1.4f)},new[]{1f,1f},14,new Color(.84f,.86f,.9f).linear,new Color(.7f,.72f,.78f).linear,1,.4f);
                s.Slot(1).Rope(NeveSkiff+turn*new Vector3(0,0,-2.4f),pod+turn*new Vector3(0,0,1.4f),.5f,.06f,new Color(.92f,.84f,.6f).linear);
                var built=kit.Built("Neve's tow",s,stuff.berthMetal,stuff.rope);built.transform.SetParent(root.transform,true);Unshadowed(built,true);
                Solid(root.transform,NeveSkiff,2.6f,"Neve's skiff body");
                var hail=kit.Discovery("Neve's skiff",NeveHail-Vector3.up*Plane,"Hail — Neve","neve.offered","","NeveReturnOffer","Neve",null,0,WorldBuilder.FlightDockRange);
                hail.requiredFlag="home.nacreSpoken";hail.transform.SetParent(root.transform,true);
            }
            /// <summary>Mira's disputed lot from Nacre: a crate with her tag on it, lashed to an old hull on the outer wall (SecondInventory step 0). Shown only while it can be read.</summary>
            static void LotCrate(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject("Mira's lot at Nacre");
                if(ZoneKit.Landed("TallowStationHull")){var hulk=kit.Loose("TallowStationHull",Lot+new Vector3(0,-2.4f,3),9,Quaternion.Euler(14,Along(292).x>0?40:220,-8));Paint(hulk,"grey",Old[0].tone);hulk.transform.SetParent(root.transform,true);Unshadowed(hulk,true);}
                var point=kit.Discovery("A lot with Mira's tag",LotHand-Vector3.up*Plane,"Read Mira's tag","mira.lotNacre","Consignment",null,"Sela","SecondInventory",0,WorldBuilder.FlightDockRange);
                point.situation="consign.nacre";point.transform.SetParent(root.transform,true);
                // The crate, pale and lashed, under its own small light: unlike everything round it.
                var s=new ZoneKit.Shape();s.Slot(0).Box(Lot+Vector3.down*.2f,new Vector3(2.2f,1.4f,1.6f),Quaternion.LookRotation(Along(292)),new Color(.9f,.86f,.74f).linear,new Color(.96f,.92f,.82f).linear);
                s.Slot(1).Rope(Lot+new Vector3(-1.1f,.5f,0),Lot+new Vector3(-1.4f,-1,2.6f),.3f,.05f,new Color(.92f,.84f,.6f).linear).Rope(Lot+new Vector3(1.1f,.5f,0),Lot+new Vector3(1.6f,-1.2f,2.4f),.3f,.05f,new Color(.92f,.84f,.6f).linear);
                var crate=kit.Built("Mira's lot",s,ZoneKit.Tinted(ZoneKit.Toon("nacre-crate",null,m=>m.SetFloat("_RampSteps",4))),stuff.rope);
                var light=new GameObject("Lot light",typeof(Light)).GetComponent<Light>();light.transform.SetParent(crate.transform,true);light.transform.position=Lot+Vector3.up*2;light.type=LightType.Point;light.range=7;light.intensity=1.8f;light.color=new Color(1,.92f,.8f);
                crate.transform.SetParent(point.transform,true);point.visual=crate;crate.SetActive(false);Solid(root.transform,Lot,1.6f,"Mira's lot body");
            }
            /// <summary>The outer marker: Nacre approach's lit buoy, still calling traffic in, with the crossing Bryn hails on just past it.</summary>
            static void Marker(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject("The outer marker");
                if(ZoneKit.Landed("PulseBeacon")){var buoy=kit.Prop("PulseBeacon",MarkerBuoy+Vector3.down*3,6,30,false,false);buoy.transform.SetParent(root.transform,true);Unshadowed(buoy,true);}
                var light=new GameObject("Marker light",typeof(Light)).GetComponent<Light>();light.transform.SetParent(root.transform,true);light.transform.position=MarkerBuoy+Vector3.up*3;light.type=LightType.Point;light.range=18;light.intensity=2.4f;light.color=new Color(.8f,.9f,1);light.shadows=LightShadows.None;
                Solid(root.transform,MarkerBuoy,1.8f,"Marker body");
                WorldBuilder.Label("NACRE APPROACH",MarkerBuoy+new Vector3(0,-.7f,-6),2.6f).transform.SetParent(root.transform,true);
            }

            // ---------------------------------------------------------------- the fights and what is said
            static void Fights(ZoneKit kit,out EncounterVolume outer,out EncounterVolume fragment)
            {
                string tow=Foe("TowDrone","ClaimDrone");
                outer=kit.FlightFight("Nacre_TowsOuter",TowsOuter,new Vector3(36,8,30),(tow,2));
                kit.FlightFight("Nacre_TowsInner",TowsInner,new Vector3(36,8,30),(tow,2));
                kit.FlightFight("Nacre_TowsBasin",TowsBasin,new Vector3(30,8,30),(tow,2));
                string piece=Foe("BridleFragment",null);fragment=null;
                // Its tether, from a wreck jammed in the breach's west side to where the fragment thrashes.
                var anchor=In(BreachTurn-6,104);var tether=kit.Built("The fragment's tether",new ZoneKit.Shape().Slot(0).Rope(anchor+Vector3.down*.5f,Fragment+Vector3.up*.6f,1.4f,.14f,new Color(.5f,.48f,.46f)),
                    ZoneKit.Tinted(ZoneKit.Toon("nacre-tether",null,m=>m.SetFloat("_RampSteps",3))));Unshadowed(tether);
                if(piece!=null)
                {
                    fragment=kit.FlightFight("Nacre_Fragment",Fragment,new Vector3(22,8,22),(piece,1));fragment.spawners[0].radius=0;fragment.spawners[0].transform.position=Fragment;
                    var show=new GameObject("The fragment on show").transform;show.SetParent(fragment.transform,false);show.position=Fragment;fragment.previewPoint=show;fragment.previewRadius=0;
                }
                else
                {
                    var body=Drawn("BridleFragment",Fragment+Vector3.down*2.2f,4.4f,BreachTurn);
                    if(body!=null){body.name="Bridle fragment on its tether (drawn: its EnemyDef isn't defined yet)";var t=body.AddComponent<Tumble>();t.axis=new Vector3(.2f,1,.4f).normalized;t.rate=14;}
                }
            }
            static void Hail(string situation,Vector3 at,string heard)
            {
                var hail=new GameObject("Bryn on the radio: "+situation,typeof(RadioHail)).GetComponent<RadioHail>();hail.transform.position=at;
                hail.speaker="Bryn";hail.situations=new[]{situation};hail.needs=new[]{"home.nacreSpoken"};hail.heardFlag=heard;hail.silencedBy="";hail.range=30;
            }
            static FirstSight Sight(string situation,Vector3? at,EncounterVolume encounter,float range,string once)
            {
                var go=new GameObject("First sight: "+situation,typeof(FirstSight));if(at is Vector3 p)go.transform.position=p;else if(encounter!=null)go.transform.position=encounter.transform.position;
                var sight=go.GetComponent<FirstSight>();sight.situation=situation;sight.encounter=encounter;sight.range=range;sight.onceFlag=once;sight.requiredFlag="";return sight;
            }
            /// <summary>An enemy's generated body stood as scenery (no brain, no collider), fitted to a height: a tow-drone still hauling, or a fragment before its definition exists.</summary>
            static GameObject Drawn(string id,Vector3 middle,float height,float yaw)
            {
                var prefab=ZoneKit.Prefab("Enemies",id);if(prefab==null)return null;
                var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);PrefabUtility.UnpackPrefabInstance(body,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                foreach(var c in body.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
                foreach(var b in body.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null)UnityEngine.Object.DestroyImmediate(b);
                foreach(var a in body.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(a);
                body.transform.rotation=Quaternion.Euler(0,yaw,0);var box=ModelGeometry.BoundsOf(body);body.transform.localScale*=height/Mathf.Max(.01f,box.size.y);
                box=ModelGeometry.BoundsOf(body);body.transform.position+=middle-box.center;Unshadowed(body,true);
                return body;
            }
        }
        /// <summary>The spawn in Many Hands' hull a ship back from Nacre arrives at (HomeBuilder.Approach stands it).</summary>
        internal const string NacreSpawn="Nacre";
    }
}
