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
    /// <summary>
    /// The Returning Passage (space combat, the last flight; brief, "ReturningPassage"): the bore of the adult Choir that Stillwater pinned, open again, and the whole convoy running it home
    /// together. The party flies escort down the lit road through its middle, every hull the player has known flowing down the bore around and under them, the Wick-folk's lantern chain
    /// marking the road's edges and pulsing ahead toward the mouth, the young Choir alongside, Mother of Doors far off below; Ilo reads the names. Near the far end Bryn's tug falls behind,
    /// Sela turns back with Taren (D286), the convoy closes to the lanterns, the tug is cleared of tow-drones (<c>Passage_Tug</c>) and taken on a line through the narrows to the far mouth,
    /// and the game cuts to SEVEN WEEKS ON. Demanding and forgiving: a fall restarts the section, never the run (wiring section 6.4).
    /// <para>Map brief, every coordinate, the run's contract, the tug's fight contract and the route writers' numbers: docs/maps/ReturningPassage.md. Every place another file needs is a
    /// constant of <see cref="Passage"/>, by distance along the passage's line (<see cref="Passage.On"/>). The tug Welcome Back, code-built twice before (Many Hands' arrivals, Nacre
    /// Grave), is <see cref="TugKit"/>, shared. Rebuild with <c>-mooring-zones ReturningPassage</c>; <c>-passage-pose turned</c> builds it as it stands after the turn, for stills only.</para>
    /// </summary>
    public static partial class LastMooringBuilder
    {
        /// <summary>The zone's own fields on its ZoneDef, set last inside <c>Define</c>'s lambda for it: the run begins with the lead's own word for it (<c>passage</c>), so the arrival's
        /// stock line ("Clear lanes. Stay sharp.") isn't said over it.</summary>
        static void PassageDefinition(ZoneDef z){z.quietArrival=true;}

        [ZoneLook.Source] static void PassageLooks()
        {
            // A warm key from behind the camera (the light coming down the bore from the open mouth ahead is the lanterns' and the convoy's own) on a violet living interior: a rose
            // haze takes the deep back in layers, so the convoy reads over the bore's floor and ribs; the lanterns and the windows are the gold, and bloom.
            ZoneLook.Register(PassageZone,new ZoneLook.Spec{sun=C(1f,.88f,.74f),sunIntensity=1.25f,sunPitch=40,shadowStrength=.7f,
                sky=C(.46f,.42f,.54f),equator=C(.36f,.31f,.4f),ground=C(.18f,.14f,.22f),
                fog=true,fogColor=C(.15f,.1f,.19f),fogStart=Passage.HazeFrom,fogEnd=Passage.HazeTo,
                contrast=13,saturation=4,splitShadows=C(.42f,.4f,.6f),splitHighlights=C(.64f,.55f,.44f),
                bloomThreshold=.9f,bloomIntensity=.8f,vignette=.22f});
        }

        // ================================================================ the tug Welcome Back (shared)
        /// <summary>Bryn Asta's tug, the <i>Welcome Back</i>, as Nacre's paint has her (HomeBuilder.Nacre's <c>TugRaft</c>, lifted here so every later scene that shows her draws her alike):
        /// a working cutter's hull in the Nacre approach's green with cream trim, fenders hung along both flanks, a tow boom off her stern and her beacon. Drawn only; the caller gives her
        /// a body if she is to be solid.</summary>
        internal static class TugKit
        {
            /// <summary>How long and how tall she is drawn (her cutter's hull as taken in, at <see cref="Scale"/>).</summary>
            internal const float Scale=1.05f;
            static readonly Dictionary<(Material,string),Material> painted=new();
            /// <summary>The tug, her middle at <paramref name="middle"/> (her keel <c>.9</c> m under it), her bow along <paramref name="yaw"/>; under <paramref name="parent"/> if given. Her
            /// paint is saved beside <paramref name="zone"/>'s scene.</summary>
            internal static GameObject WelcomeBack(ZoneKit kit,Vector3 middle,float yaw,Transform parent=null)
            {
                var root=new GameObject("The tug Welcome Back");root.transform.SetPositionAndRotation(middle,Quaternion.Euler(0,yaw,0));if(parent!=null)root.transform.SetParent(parent,true);
                Color trim=new Color(.9f,.84f,.62f).linear,rubber=new(.13f,.125f,.12f);
                var s=new ZoneKit.Shape();
                var body=ConvoyKit.Hull("PatrolCutter",0,0,0,0,Scale);
                if(body!=null)
                {
                    // Laid unturned at the origin, measured, then turned to her heading with everything hung on her: laid in the heading's frame from fixed numbers, Nacre's fenders were
                    // black drums standing off a hull longer and narrower than the frame (its pass 3).
                    body.name="Her hull";Paint(kit.zone,body,"tug",new Color(.5f,.9f,.78f));
                    var box=ZoneKit.Drawn(body);bool alongX=box.size.x>=box.size.z;
                    if(alongX){body.transform.rotation=Quaternion.Euler(0,-90,0);box=ZoneKit.Drawn(body);}
                    body.transform.position+=new Vector3(-box.center.x,-.25f-box.center.y,-box.center.z);box=ZoneKit.Drawn(body);
                    float half=box.extents.z,side=box.extents.x;
                    for(int k=0;k<4;k++)foreach(int sign in new[]{-1,1})
                    {
                        var f=new Vector3(sign*side*.62f,-box.extents.y*.1f-.25f,-half*.5f+k*half*.33f);
                        s.Slot(1).Tube(new[]{f+Vector3.back*.5f,f+Vector3.forward*.5f},new[]{.34f,.34f},10,rubber,ZoneKit.Shape.Dim(rubber,.7f),1,1);
                    }
                    var stern=new Vector3(0,box.extents.y*.35f-.25f,-half*.85f);s.Slot(0).Beam(stern,stern+new Vector3(0,1.6f,-3.6f),.32f,.32f,trim);
                    // Laid at the origin, so as a child kept in place it sits on her root as it was laid.
                    body.transform.SetParent(root.transform,false);
                }
                else s.Slot(0).Box(Vector3.zero,new Vector3(4.6f,2.4f,10),Quaternion.identity,new Color(.26f,.42f,.4f),new Color(.3f,.48f,.46f),.4f);
                var fittings=ZoneKit.Tinted(ZoneKit.Toon("tug-fittings",null,m=>{m.SetColor("_BaseColor",Color.white);m.SetFloat("_RampSteps",3);m.SetFloat("_LightFloor",.4f);}));
                var hung=kit.Built("The tug Welcome Back, fenders and boom",s,fittings,fittings);
                hung.transform.SetParent(root.transform,false);hung.transform.localPosition=Vector3.zero;hung.transform.localRotation=Quaternion.identity;
                foreach(var r in root.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}
                var lamp=new GameObject("Her beacon",typeof(Light));lamp.transform.SetParent(root.transform,false);lamp.transform.localPosition=new Vector3(0,3,0);
                var l=lamp.GetComponent<Light>();l.type=LightType.Point;l.range=16;l.intensity=2.4f;l.color=new Color(.7f,1,.86f);l.shadows=LightShadows.None;
                return root;
            }
            /// <summary>A piece in a colour of its own, drawn in copies of its own materials with the colour multiplied in, saved beside the zone's scene (a property block's tint shows in
            /// play only, and the editor's stills would show the stock paint).</summary>
            internal static void Paint(string zone,GameObject piece,string name,Color tone)
            {
                foreach(var view in piece.GetComponentsInChildren<Renderer>())
                {
                    var mats=view.sharedMaterials;
                    for(int i=0;i<mats.Length;i++)
                    {
                        var source=mats[i];if(source==null)continue;
                        if(!painted.TryGetValue((source,zone+name),out var copy))
                        {
                            string path="Assets/_Project/Scenes/"+zone+"-"+source.name.Replace(' ','-')+"-"+name+".mat";
                            copy=AssetDatabase.LoadAssetAtPath<Material>(path);var fresh=new Material(source){name=source.name+"-"+name};
                            if(copy==null){copy=fresh;AssetDatabase.CreateAsset(copy,path);}else{copy.CopyPropertiesFromMaterial(fresh);copy.shader=fresh.shader;UnityEngine.Object.DestroyImmediate(fresh);}
                            if(copy.HasProperty("_BaseColor"))copy.SetColor("_BaseColor",source.GetColor("_BaseColor")*tone);
                            EditorUtility.SetDirty(copy);painted[(source,zone+name)]=copy;
                        }
                        mats[i]=copy;
                    }
                    view.sharedMaterials=mats;
                }
            }
        }

        // ================================================================ the zone
        internal static class Passage
        {
            // ---------------------------------------------------------------- the numbers (docs/maps/ReturningPassage.md reads these; so do the tests and the routes)
            internal const float Plane=ZoneKit.FlightPlane;
            /// <summary>The haze, tuned for the flight camera at its play distance and measured from the plane by <see cref="PlaneHaze"/> (D344), so any view drawn back keeps the convoy clear.</summary>
            internal const float HazeFrom=70,HazeTo=480;
            /// <summary>The passage's line at the plane, the way the convoy flows (north), and the road's half width at each of its points: 30 m to the far end, narrowing to 14 at the
            /// far mouth, opening to 44 past it. Bends at s 210, 492 and 807.</summary>
            internal static readonly Vector3[] Line={new(0,Plane,-40),new(0,Plane,170),new(-36,Plane,450),new(20,Plane,760),new(0,Plane,1050),new(0,Plane,1170),new(0,Plane,1240),new(0,Plane,1300),new(0,Plane,1345)};
            internal static readonly float[] Halves={30,30,30,30,30,30,14,44,44};
            internal const float Wide=30;
            /// <summary>Where along the line each place is.</summary>
            internal const float ArrivalS=40,IloS=170,BendOneS=210,BendTwoS=492.3f,BendThreeS=807.3f,TugS=760,TugFightS=752,TugReachS=800,LineS=769,TowS=785,TurnS=1060,FarEndS=1080,
                NarrowsS=1218,MouthS=1288,EndS=1393;
            /// <summary>The convoy comes in at <see cref="FlowFrom"/> and goes out at <see cref="FlowTo"/>, then round again.</summary>
            internal const float FlowFrom=-110,FlowTo=1480;
            /// <summary>The fight round the tug: its box, the three drones' wake points (along, side), all facing south away from her.</summary>
            internal static readonly Vector3 TugFightSize=new(2*Wide,8,44);
            internal static readonly (float along,float side)[] Drones={(742,-10),(738,0),(742,10)};
            /// <summary>The tug's body at the plane while she waits (cover): across, tall, long.</summary>
            internal static readonly Vector3 TugBody=new(5.6f,4f,10.6f);

            internal static float Length{get{float run=0;for(int i=1;i<Line.Length;i++){var d=Line[i]-Line[i-1];d.y=0;run+=d.magnitude;}return run;}}
            /// <summary>A place <paramref name="s"/> metres along the line and <paramref name="side"/> to starboard of it, at the plane.</summary>
            internal static Vector3 On(float s,float side=0)=>ConvoyFlow.At(Line,s,side);
            /// <summary>The way the line runs there, in degrees (0 north, 90 east).</summary>
            internal static float Heading(float s){ConvoyFlow.At(Line,s,0,out var a);return Mathf.Atan2(a.x,a.z)*Mathf.Rad2Deg;}
            internal static float Half(float s)
            {
                float run=0;for(int i=1;i<Line.Length;i++){var d=Line[i]-Line[i-1];d.y=0;float leg=d.magnitude;if(s<=run+leg||i==Line.Length-1)return Mathf.Lerp(Halves[i-1],Halves[i],Mathf.Clamp01((s-run)/leg));run+=leg;}
                return Wide;
            }
            internal static Vector3 Arrival=>On(ArrivalS);internal static Vector3 TurnAt=>On(TurnS);internal static Vector3 TugAt=>On(TugS);internal static Vector3 TugReachAt=>On(TugReachS);
            internal static Vector3 TowAt=>On(TowS);internal static Vector3 LineAt=>On(LineS);internal static Vector3 FarEnd=>On(FarEndS);internal static Vector3 Mouth=>On(MouthS);
            internal const string TurnSpawn="Turn",TugReachSpawn="TugReach",TowSpawn="Tow",TugFight="Passage_Tug";

            /// <summary>LookDev's stills (render with <c>-look-far 1000</c>, the game camera's far plane): the arrival and the run as it is flown, each bend, the districts, the middle of
            /// the run, the tug's place, the far end, the narrows, the mouth and the rendezvous, wide views. Built with <c>-passage-pose turned</c> the same views show it after the turn.</summary>
            internal static LookDev.View[] Views=>new[]
            {
                new LookDev.View("arrival",Arrival,true),new("young",On(52,8),true),new("mother",On(70,30),true,2.2f),new("ilo",On(IloS,-2),true),new("bend-1",On(BendOneS,6),true),new("run-260",On(260,-12),true),
                new("run-330",On(330,10),true),new("run-400",On(400,0),true),new("bend-2",On(BendTwoS,-8),true),new("run-560",On(560,14),true),new("run-640",On(640,-10),true),
                new("tug-fight",On(TugFightS,4),true),new("tug",On(TugS+14,-6),true),new("bend-3",On(BendThreeS,0),true),new("run-880",On(880,12),true),new("run-960",On(960,-14),true),
                new("far-end",FarEnd,true),new("turn",TurnAt+Vector3.left*6,true),new("narrows",On(1240,0),true),new("mouth",Mouth,true),new("beyond",On(1330,0),true),
                new("edge-east",On(300,26),true),new("edge-west",On(700,-26),true),
                new("run-wide",On(300,0),true,3),new("tug-wide",On(TugS,0),true,3),new("narrows-wide",On(1250,0),true,3.4f),new("overview",On(640,0),true,11),
            };

            // ---------------------------------------------------------------- what it is made of
            internal sealed class Stuff{public Material tissue,glow,pulse,plank,rope,soft,warm,cold,young,lantern,stream,road,current,pool;}
            static Color L(float r,float g,float b)=>new Color(r,g,b).linear;
            static readonly Color Gold=new(1f,.78f,.42f),Cyan=new(.5f,.95f,1f),Rose=new(1f,.55f,.55f);
            /// <summary>The road's lanterns' glass, times the glow's 1.15: amber, one channel over one (the capture's (1, .7, .32) drew cream under the bloom, D390).</summary>
            internal static readonly Color LanternGlass=new(1f,.5f,.14f);internal const float GlowGain=1.15f;
            static Stuff Materials(ZoneKit kit)
            {
                var s=new Stuff();
                // The Choir's inside: dark rose tissue cut by veins that carry light (the Gullet's family, deeper and older), painted near neutral and coloured in the vertices; the veins
                // are the emission, so the bore's floor glows from inside under the haze.
                // Veins in both directions (the first pass's ran one way and the floor read as stretched silk): cells of tissue, a vein along each cell's edge, dark between.
                var veins=ZoneKit.Texture("Passage/choir-tissue",512,(u,v)=>
                {
                    float a=1-Mathf.Abs(2*ZoneKit.Tile(u*7+v*2.1f,v*7-u*1.3f,7,71)-1),b=1-Mathf.Abs(2*ZoneKit.Tile(u*13-v*3.1f,v*13+u*2.2f,13,72)-1),cloud=ZoneKit.Cloud(u,v,5,73);
                    float vein=Mathf.Max(Mathf.Pow(a,26),.6f*Mathf.Pow(b,34));float tone=.42f+.3f*(cloud-.5f)+.5f*vein;
                    return new Color(tone,tone*.95f,tone,1);
                });
                s.tissue=ZoneKit.Tinted(ZoneKit.Toon("passage-tissue",veins,m=>{m.SetColor("_BaseColor",Color.white);m.SetFloat("_RampSteps",4);m.SetFloat("_LightFloor",.28f);
                    m.SetColor("_ShadowTint",new Color(.36f,.3f,.52f));m.SetColor("_RimColor",new Color(.6f,.35f,.5f));m.SetTexture("_EmissionMap",veins);m.SetColor("_EmissionColor",new Color(.1f,.04f,.1f));
                    m.SetFloat("_ReceiveShadows",0);}));
                // Lamps and beads: unlit, coloured in their vertices, a little over one so they bloom without going white, a little of the haze on them so the far ones sit back.
                s.glow=ZoneKit.Toon("passage-glow",null,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetVector("_GlowParams",new Vector4(GlowGain,.25f,0,0));});
                // A lantern's glass, which keeps the count (RhythmLamp sets its emission in play; in a still it is drawn at this).
                s.pulse=ZoneKit.Toon("passage-lantern-glass",null,m=>{m.SetColor("_BaseColor",new Color(1f,.86f,.6f));m.SetColor("_EmissionColor",new Color(2.4f,1.7f,.9f));m.SetFloat("_LightFloor",.6f);});
                // The Wick-folk's floats: tarred planks.
                var planks=ZoneKit.Texture("Passage/float-planks",128,(u,v)=>{float board=Mathf.Abs(Mathf.Repeat(u*8,1)-.5f)<.04f?-.25f:0;float grain=.8f+.15f*(ZoneKit.Tile(u*8,v*32,8,81)-.5f);return new Color(grain+board,grain*.92f+board,grain*.84f+board,1);});
                s.plank=ZoneKit.Tinted(ZoneKit.Toon("passage-planks",planks,m=>{m.SetColor("_BaseColor",Color.white);m.SetFloat("_RampSteps",3);m.SetFloat("_LightFloor",.36f);}));
                s.rope=ZoneKit.Tinted(ZoneKit.Toon("passage-rope",null,m=>{m.SetColor("_BaseColor",Color.white);m.SetFloat("_RampSteps",3);m.SetFloat("_LightFloor",.5f);}));
                // The falloff in the alpha (D344): with it in the colour, URP's particle shader mixed the fog's colour into a quad's corners and added it, a square in any far or wide view.
                var round=ZoneKit.Texture("Passage/soft-glow",128,(u,v)=>{float d=Vector2.Distance(new Vector2(u,v),new Vector2(.5f,.5f))*2;float a=Mathf.Pow(Mathf.Clamp01(1-d),2.2f);return new Color(1,1,1,a);},false,true);
                Material Soft(string asset,Color colour){var m=kit.Glow(asset,false,colour);m.SetTexture("_BaseMap",round);return m;}
                s.warm=Soft("Window glow",new Color(1.4f,1.02f,.6f,1));s.cold=Soft("Running glow",new Color(.5f,1.1f,1.3f,1));s.young=Soft("Young glow",new Color(.35f,1.2f,1.25f,1));
                s.lantern=Soft("Lantern glow",new Color(1.7f,1.15f,.55f,1));s.soft=Soft("Rib glow",new Color(1.1f,.55f,.6f,1));
                // The deep's streams of light: long soft streaks along the flow (u along the line, a repeat to 60 m), faded to nothing at their sides; scrolled north by the convoy's flow.
                // Soft and broad: the first pass's fine lines and rings read as a hologram's grid laid over everything.
                var streaks=ZoneKit.Texture("Passage/deep-streams",256,(u,v)=>
                {
                    float across=Mathf.Abs(v-.5f)*2,edge=Mathf.Pow(Mathf.Clamp01(1-across),1.8f);
                    float swell=ZoneKit.Cloud(u,v*.25f,3,93),band=ZoneKit.Smooth(.35f,.8f,ZoneKit.Cloud(u,v,2,94));
                    return new Color(1,1,1,Mathf.Clamp01((.18f+.5f*swell*band)*edge));
                },true,true);
                s.stream=kit.Glow("Stream",false,Color.white);s.stream.SetTexture("_BaseMap",streaks);
                // The road's own line down its middle: chevrons pointing the way the convoy flows, twelve to a 75 m repeat, a rail either side.
                // Small sharp chevrons, twenty-five to the 75 m repeat (one every 3 m), no rails: the first pass's were wide gold smears.
                var chevrons=ZoneKit.Texture("Passage/road-chevrons",512,(u,v)=>
                {
                    float across=Mathf.Abs(v-.5f)*2;float stripe=Mathf.Repeat(u*25+across*.3f,1),chevron=ZoneKit.Smooth(0,.03f,stripe)*(1-ZoneKit.Smooth(.11f,.15f,stripe));
                    return new Color(1,1,1,Mathf.Clamp01(chevron*(1-ZoneKit.Smooth(.75f,1,across))));
                },true,true);
                s.road=kit.Glow("Road",false,new Color(1.1f,.85f,.5f,1));s.road.SetTexture("_BaseMap",chevrons);
                // A rib's current (D390): the rib's own light running round its hoop, bright down its middle and gone at its edges, broken into pulses along it (u along the hoop, a repeat
                // to 40 m, scrolled by the convoy's flow as the streams are; v across).
                var pulses=ZoneKit.Texture("Passage/rib-current",256,(u,v)=>
                {
                    float across=Mathf.Abs(v-.5f)*2,edge=Mathf.Pow(Mathf.Clamp01(1-across),2.4f);
                    float pulse=ZoneKit.Smooth(.32f,.78f,ZoneKit.Cloud(u,v*.15f,4,95));
                    return new Color(1,1,1,Mathf.Clamp01(edge*(.12f+.88f*pulse)));
                },true,true);
                s.current=kit.Glow("Rib current",false,new Color(1.05f,.5f,.62f,.72f));s.current.SetTexture("_BaseMap",pulses);
                // A lantern's pool on its float, under half its colour so the float's planks show through it.
                s.pool=Soft("Lantern pool",new Color(1f,.6f,.24f,.45f));
                return s;
            }

            // ---------------------------------------------------------------- the build
            /// <summary>Builds ReturningPassage.</summary>
            internal static bool Build()
            {
                bool turned=DevArgs.Value("-passage-pose")=="turned";
                var kit=ZoneKit.Flight(PassageZone);
                var scene=WorldBuilder.Begin(kit.zone);
                var key=GameObject.Find("KeyLight");if(key!=null)key.transform.rotation=Quaternion.Euler(40,30,0);
                var stuff=Materials(kit);
                WorldBuilder.Spawn("Arrival",Arrival).transform.rotation=Quaternion.Euler(0,Heading(ArrivalS),0);
                WorldBuilder.Spawn(TurnSpawn,TurnAt).transform.rotation=Quaternion.Euler(0,Heading(TurnS)+180,0);
                WorldBuilder.Spawn(TugReachSpawn,TugReachAt).transform.rotation=Quaternion.Euler(0,Heading(TugReachS)+180,0);
                WorldBuilder.Spawn(TowSpawn,TowAt).transform.rotation=Quaternion.Euler(0,Heading(TowS),0);
                kit.DriftingSky();

                // ---- the bore: its floor and ribs far down, its streams of light, the road's line, the motes in the air
                var streams=Bore(kit,stuff);
                // ---- the road's edges: solid at the plane, the lantern chain along them, the count
                int lanterns=Edges(kit,stuff);
                // ---- the convoy, running
                var flow=Convoy(kit,stuff,streams,turned);
                // ---- the young alongside, Mother of Doors far off, Idra on the flank (from the turn)
                var (young,mother,idra)=Companions(kit,stuff,turned);
                // ---- the rendezvous past the mouth: Many Hands waiting in open space, Orrin's board aboard
                var rendezvous=Rendezvous(kit,stuff);

                // ---- the tug and her fight
                var tug=TugKit.WelcomeBack(kit,TugAt,Heading(TugS));
                var body=new GameObject("The tug's body",typeof(BoxCollider));body.isStatic=false;body.transform.SetParent(tug.transform,false);body.transform.localPosition=new Vector3(0,.2f,0);
                body.GetComponent<BoxCollider>().size=TugBody;
                var drawn=ZoneKit.Drawn(tug);Debug.Log("PASSAGE_TUG drawn="+drawn);
                var rope=new GameObject("Her tow line",typeof(LineRenderer)).GetComponent<LineRenderer>();rope.transform.SetParent(tug.transform,false);
                rope.sharedMaterial=stuff.rope;rope.widthMultiplier=.14f;rope.startColor=rope.endColor=new Color(.92f,.84f,.6f);rope.useWorldSpace=true;rope.positionCount=2;rope.numCapVertices=2;
                rope.shadowCastingMode=ShadowCastingMode.Off;rope.enabled=false;
                string drone=Foe(Enemies.TowDrone,"TowDrone");
                var fight=WorldBuilder.Encounter(TugFight,On(TugFightS),TugFightSize,drone,1);fight.transform.rotation=Quaternion.Euler(0,Heading(TugFightS),0);
                var first=fight.spawners[0];var spawners=new List<Spawner>{first};
                for(int i=1;i<Drones.Length;i++)spawners.Add(ZoneKit.Spawn(fight,drone,1,On(Drones[i].along,Drones[i].side),0));
                for(int i=0;i<Drones.Length;i++){var sp=spawners[i];sp.transform.position=On(Drones[i].along,Drones[i].side);sp.radius=0;sp.faceAsPlaced=true;sp.transform.rotation=Quaternion.Euler(0,Heading(Drones[i].along)+180,0);}
                fight.spawners=spawners.ToArray();fight.requiredFlag="end.passageRun";fight.checkpointSpawn=TugReachSpawn;
                var towing=tug.AddComponent<TugTow>();towing.lineFlag="end.lineOn";towing.fight=fight;towing.solid=body.GetComponent<BoxCollider>();towing.line=rope;
                towing.bow=new Vector3(0,.4f,TugBody.z*.5f);towing.length=11;towing.height=Plane;
                foreach(var t in tug.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=false;
                var line=kit.Discovery("Take the tug's line",new Vector3(LineAt.x,0,LineAt.z),"Take the tug's line","end.lineOn","TugLine",null,"Sela",null,0,WorldBuilder.FlightDockRange);
                line.requiredFlag="clear."+TugFight;line.saveSpawn=TowSpawn;line.situation="";AtPlane(line);
                var onLine=new GameObject("Bryn: the line is on",typeof(AfterTalk)).GetComponent<AfterTalk>();onLine.transform.position=LineAt;
                onLine.flag="end.lineOn";onLine.node="BrynOnTheLine";onLine.speaker="Sela";onLine.heardFlag="end.tugFetched";onLine.delay=1.2f;onLine.range=0;

                // ---- what is said
                var ilo=kit.Crossing("Ilo reads the first name",new Vector3(On(IloS).x,0,On(IloS).z),new Vector3(2*Wide+6,8,6),"end.namesBegun","IloFirstName","Ilo");ilo.situation="";
                ilo.transform.rotation=Quaternion.Euler(0,Heading(IloS),0);AtPlane(ilo);
                var far=kit.Crossing("The far end: Bryn falls behind",new Vector3(FarEnd.x,0,FarEnd.z),new Vector3(2*Wide+6,8,6),"end.farEnd","BrynFallsBehind","Sela");far.report="FarEnd";far.situation="";
                far.transform.rotation=Quaternion.Euler(0,Heading(FarEndS),0);AtPlane(far);
                var mouth=kit.Crossing("The far mouth: the rendezvous",new Vector3(Mouth.x,0,Mouth.z),new Vector3(2*Half(MouthS)+4,8,6),"end.mouthReached","OrrinRendezvous","Orrin","end.tugFetched");mouth.report="FarMouth";mouth.situation="";
                mouth.transform.rotation=Quaternion.Euler(0,Heading(MouthS),0);AtPlane(mouth);
                Calls(kit);
                // The turn (D286): once Bryn's call has closed, the party is held while the convoy closes to the lanterns, then Sela and Taren are the pair out and the zone reloads at Turn.
                var turn=new GameObject("The turn: Sela goes back for her",typeof(CutAway)).GetComponent<CutAway>();turn.transform.position=FarEnd;
                turn.flag="end.passageRun";turn.doneFlag="end.turnedBack";turn.hold=4;turn.pair=new[]{"Taren","Sela"};turn.lead="Sela";turn.scene=PassageZone;turn.spawn=TurnSpawn;
                // The cut: once Orrin's board says all through, SEVEN WEEKS ON, and the neighbourhood.
                var cut=new GameObject("The cut: seven weeks on",typeof(CutAway)).GetComponent<CutAway>();cut.transform.position=Mouth;
                cut.flag="end.rendezvous";cut.hold=2.5f;cut.plate="SEVEN WEEKS ON";cut.plateSeconds=3.5f;cut.scene=AfloatZone;cut.spawn="Arrival";

                // ---- before and after the turn
                Gate("From the turn: the tug and her line",true,new[]{tug,line.gameObject},"end.passageRun");
                Gate("From the turn: Idra on the flank",true,new[]{idra},"end.passageRun");
                // What starts unseen is saved switched off (the stills see the zone as it is met), unless the stills are of it turned.
                if(!turned)foreach(var g in new[]{tug,line.gameObject,idra})if(g!=null)g.SetActive(false);
                var haze=new GameObject("The haze, from the plane",typeof(PlaneHaze)).GetComponent<PlaneHaze>();haze.plane=Plane;haze.start=HazeFrom;haze.end=HazeTo;

                WorldBuilder.Save(scene,kit.zone);
                string P(Vector3 p)=>"("+p.x.ToString("0.0")+","+p.y.ToString("0")+","+p.z.ToString("0.0")+")";
                Debug.Log("PASSAGE_COORDS ReturningPassage arrival="+P(Arrival)+" ilo="+P(On(IloS))+" far-end="+P(FarEnd)+" turn="+P(TurnAt)+" tug="+P(TugAt)+" tug-reach="+P(TugReachAt)
                    +" line="+P(LineAt)+" tow="+P(TowAt)+" mouth="+P(Mouth)+" drones="+string.Join(" ",Drones.Select(d=>P(On(d.along,d.side))))+" length="+Length.ToString("0")
                    +" lanterns="+lanterns+" groups="+flow.groups.Length+" rendezvous="+(rendezvous!=null)+" young="+(young!=null)+" mother="+(mother!=null)+" pose="+(turned?"turned":"as met"));
                Debug.Log("RETURNING_PASSAGE_OK");
                return true;
            }
            static T AtPlane<T>(T point)where T:Component{var at=point.transform.position;point.transform.position=new Vector3(at.x,Plane-1,at.z);return point;}
            static void Gate(string name,bool showWhenSet,GameObject[] targets,string flag)
            {
                var gate=new GameObject(name,typeof(FlagGate)).GetComponent<FlagGate>();gate.flag=flag;gate.showWhenSet=showWhenSet;gate.targets=targets.Where(t=>t!=null).ToArray();
            }

            // ---------------------------------------------------------------- the bore
            /// <summary>A strip laid along the line: for each place along it (every <paramref name="step"/> metres from <paramref name="from"/> to <paramref name="to"/>) a row across it
            /// from <paramref name="across"/>, u along the line (a repeat to <paramref name="period"/> metres), v across. Saved beside the scene.</summary>
            static Mesh Loft(ZoneKit kit,string name,float from,float to,float step,int rows,Func<float,float,Vector3> across,Func<float,float,Color> tone,float period,float acrossRepeats=1)
            {
                var points=new List<Vector3>();var uv=new List<Vector2>();var colours=new List<Color>();var tris=new List<int>();int columns=0;
                for(float s=from;s<=to+.01f;s+=step)
                {
                    ConvoyFlow.At(Line,s,0,out var ahead);var side=Vector3.Cross(Vector3.up,ahead);var mid=On(s);mid.y=0;
                    for(int r=0;r<=rows;r++){float v=r/(float)rows;var local=across(s,v);points.Add(mid+side*local.x+Vector3.up*local.y);uv.Add(new Vector2((s-from)/period,v*acrossRepeats));colours.Add(tone(s,v));}
                    columns++;
                }
                for(int c=1;c<columns;c++)for(int r=0;r<rows;r++){int a=(c-1)*(rows+1)+r,b=c*(rows+1)+r;tris.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
                var mesh=new Mesh{name=name,indexFormat=points.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};
                mesh.SetVertices(points);mesh.SetUVs(0,uv);mesh.SetColors(colours);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                return ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+kit.zone+"-"+name.Replace(' ','-').Replace("'","")+".asset");
            }
            static GameObject Drawn(string name,Mesh mesh,Material material,bool shadows=false,Transform parent=null)
            {
                var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=mesh;var view=go.GetComponent<MeshRenderer>();view.sharedMaterial=material;
                view.shadowCastingMode=shadows?ShadowCastingMode.On:ShadowCastingMode.Off;view.receiveShadows=false;if(parent!=null)go.transform.SetParent(parent,true);go.isStatic=true;return go;
            }
            /// <summary>The Choir's bore under everything: its floor 150 m down, curving up at its sides; its ribs, a hoop of light under the road every 70 m (the rhythm a run is
            /// flown over); three streams of light far down flowing north; the road's own line of chevrons down its middle; motes in the air. The floor ends past the far mouth in a lip, and
            /// beyond it is the new sky. Returns what the convoy's flow scrolls.</summary>
            static Renderer[] Bore(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject("The bore");
                // The floor: a U across the bore, deepest under the road, its veins lit; darker toward its sides so the road's middle is where the light is.
                // Toward the far mouth the bore closes in (narrower, shallower: the narrows), and past the mouth's lip it falls away into the new sky.
                Vector3 Floor(float s,float v)
                {
                    float close=ZoneKit.Smooth(NarrowsS-120,MouthS,s),wide=Mathf.Lerp(260,110,close),deep=Mathf.Lerp(-178,-62,close);
                    float x=(v-.5f)*2*wide;float y=deep+(130-80*close)*Mathf.Pow(Mathf.Abs(x)/wide,2.2f);if(s>MouthS)y-=Mathf.Pow((s-MouthS)/9,2);return new Vector3(x,y,0);
                }
                // Dark plum, lighter only under the road's middle: the convoy is the subject, the bore is the night it runs through (the first pass's floor was bright magenta silk).
                Color FloorTone(float s,float v){float mid=1-Mathf.Abs(v-.5f)*2;float n=Mathf.PerlinNoise(s*.012f,v*7);return Color.Lerp(L(.13f,.08f,.16f),L(.36f,.22f,.34f),.25f+.5f*mid*mid*n);}
                var floor=Drawn("The bore's floor",Loft(kit,"The bore's floor",-260,MouthS+30,10,26,Floor,FloorTone,48,11),stuff.tissue,false,root.transform);
                // The ribs: hoops of the bore's own tissue under the road every 70 m, the bore's beat, each with its current of light running round it (D390). Drawn as unlit glow tubes
                // they were one flat pink band across the whole frame (nov23-whole s68/105); as plain tissue (the first pass) stripes on a carpet. The rib is the tissue now, lit and
                // shaded (a ridge with a sun side and a shade side, its veins lit), and over its crown a soft ribbon of its light, bright down its middle and gone at its edges, in
                // pulses that the convoy's flow carries round the hoop.
                var ribs=new ZoneKit.Shape();int hoops=0;
                var cur=new List<Vector3>();var curUv=new List<Vector2>();var curTris=new List<int>();
                for(float s=-200;s<MouthS;s+=70)
                {
                    ConvoyFlow.At(Line,s,0,out var ahead);var side=Vector3.Cross(Vector3.up,ahead);var mid=On(s);mid.y=0;var path=new List<Vector3>();var radii=new List<float>();
                    for(int k=0;k<=48;k++){float v=k/48f;var p=Floor(s,v);path.Add(mid+side*p.x+Vector3.up*(p.y+2.2f));radii.Add(1.2f+1.8f*Mathf.Sin(v*Mathf.PI));}
                    ribs.Slot(0).Tube(path,radii,10,L(.62f,.34f,.42f),L(.44f,.24f,.34f),1,.12f);hoops++;
                    float run=0;int first=cur.Count;
                    for(int k=0;k<path.Count;k++)
                    {
                        if(k>0)run+=Vector3.Distance(path[k],path[k-1]);float v=k/(float)(path.Count-1),wide=2.5f+4.5f*Mathf.Sin(v*Mathf.PI);
                        var crown=path[k]+Vector3.up*(radii[k]+.25f);
                        cur.Add(crown-ahead*wide);cur.Add(crown+ahead*wide);curUv.Add(new Vector2(run/40f,0));curUv.Add(new Vector2(run/40f,1));
                        if(k>0){int a=first+(k-1)*2;curTris.AddRange(new[]{a,a+1,a+2,a+1,a+3,a+2});}
                    }
                }
                var ribsDrawn=kit.Built("The bore's ribs",ribs,stuff.tissue);ribsDrawn.transform.SetParent(root.transform,true);ribsDrawn.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;ribsDrawn.isStatic=true;
                var currentMesh=new Mesh{name="The ribs' currents",indexFormat=cur.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};currentMesh.SetVertices(cur);currentMesh.SetUVs(0,curUv);
                currentMesh.SetColors(Enumerable.Repeat(Color.white,cur.Count).ToList());currentMesh.SetTriangles(curTris,0);currentMesh.RecalculateNormals();currentMesh.RecalculateBounds();
                var currents=Drawn("The ribs' currents",ZoneKit.SaveMesh(currentMesh,"Assets/_Project/Scenes/"+kit.zone+"-The-ribs-currents.asset"),stuff.current,false,root.transform);currents.isStatic=false;
                // Each rib's light, a soft glow along its bottom, so the hoops show through the haze.
                for(float s=-200;s<MouthS;s+=70){var at=On(s);at.y=Floor(s,.5f).y+.5f;Halo(stuff.soft,at,46,root.transform,Heading(s));}
                // The far mouth's lip: a great fold of tissue round the opening, under the plane, its edge lit.
                var lip=new ZoneKit.Shape();
                {
                    ConvoyFlow.At(Line,MouthS,0,out var ahead);var side=Vector3.Cross(Vector3.up,ahead);var mid=On(MouthS+8);
                    for(int k=0;k<36;k++)
                    {
                        float a=k/36f*Mathf.PI*2,b=(k+1)/36f*Mathf.PI*2;
                        Vector3 Ring(float t,float r)=>mid+side*Mathf.Cos(t)*r+Vector3.up*(Mathf.Sin(t)*r*.55f-r*.55f-6);
                        if(Mathf.Sin(a)>.25f)continue;
                        lip.Slot(0).Tube(new[]{Ring(a,24),Ring(b,24)},new[]{3.6f,3.6f},10,L(.62f,.36f,.4f),L(.42f,.24f,.32f),1,.2f,false);
                    }
                }
                var lipDrawn=kit.Built("The far mouth's lip",lip,stuff.tissue);lipDrawn.transform.SetParent(root.transform,true);lipDrawn.isStatic=true;
                // Three streams of light far down, flowing north: warm under the road, cold to starboard, rose to port.
                var streams=new List<Renderer>{currents.GetComponent<MeshRenderer>()};
                foreach(var (name,side,depth,half,tone) in new[]{("Stream under the road",-4f,-52f,12f,new Color(1f,.78f,.45f,.28f)),("Stream to starboard",36f,-82f,20f,new Color(.45f,.9f,1f,.3f)),("Stream to port",-48f,-108f,28f,new Color(.95f,.45f,.6f,.26f))})
                {
                    var mesh=Loft(kit,name,-200,MouthS+10,6,1,(s,v)=>new Vector3(side+(v-.5f)*2*half,depth,0),(s,v)=>tone,60);
                    var view=Drawn(name,mesh,stuff.stream,false,root.transform).GetComponent<MeshRenderer>();streams.Add(view);
                }
                // The road's line: the Wick-folk's lanterns down its middle, under the plane, two short and one long, the count's pulse running north along them toward the mouth.
                // From the road's middle the lanterns at its edges are out of the frame (it is 60 m across and the frame 40), and the first passes' chevrons down the middle read as
                // road paint: the way on is the lanterns', as Pell's count says.
                RoadLanterns(kit,stuff,root.transform);
                // Motes in the air over the road, so the open middle still moves.
                for(float s=0;s<MouthS+60;s+=120)
                {
                    var at=On(s+60);at.y=-2;var dust=kit.Drift("Motes over the road",at,new Vector3(70,6,120),stuff.warm,new Color(1f,.85f,.6f,.55f),260,.08f,.22f,.4f,14);
                    dust.transform.rotation=Quaternion.Euler(0,Heading(s+60),0);dust.transform.SetParent(root.transform,true);
                }
                Debug.Log("PASSAGE_BORE hoops="+hoops+" floor="+floor.GetComponent<MeshFilter>().sharedMesh.vertexCount);
                return streams.ToArray();
            }
            /// <summary>Lantern buoys down the road's middle, in threes every 22 m (two small, one long), floats of tarred plank with warm glass under the plane, a glow under each three;
            /// every third three keeps the count with a light, its pulse running north at 38 m/s, as the edges' do.</summary>
            static void RoadLanterns(ZoneKit kit,Stuff stuff,Transform parent)
            {
                var root=new GameObject("The road's lanterns");root.transform.SetParent(parent,true);int k=0;
                for(float from=ArrivalS-10;from<MouthS+20;from+=286)
                {
                    var floats=new ZoneKit.Shape();var glass=new ZoneKit.Shape();
                    for(float s=from;s<Mathf.Min(MouthS+20,from+286);s+=22,k++)
                    {
                        float yaw=Heading(s);var turn=Quaternion.Euler(0,yaw,0);
                        foreach(var (at,isLong) in new[]{(s,false),(s+3.2f,false),(s+7.4f,true)})
                        {
                            // Each a thing that floats (D390: nov23-whole s68/105's markers were flat brown discs under flat cream bars): a drum of tarred planks with an iron hoop
                            // at its head, and on it a lantern, a base plate, four corner bars round its glass and a cap, the glass amber under the bloom; its pool on the drum's head.
                            var p=On(at,(k%2==0?-1:1)*.8f);float r=isLong?1.3f:.95f;
                            floats.Slot(0).Tube(new[]{new Vector3(p.x,-2.6f,p.z),new Vector3(p.x,-1.9f,p.z)},new[]{r*.9f,r},14,L(.28f,.2f,.13f),L(.46f,.35f,.22f),3,.8f);
                            floats.Slot(1).Tube(new[]{new Vector3(p.x,-2.04f,p.z),new Vector3(p.x,-1.88f,p.z)},new[]{r+.05f,r+.05f},14,L(.2f,.2f,.22f),L(.32f,.32f,.34f),1,.5f);
                            var lamp=new Vector3(p.x,-1.5f,p.z);var size=isLong?new Vector3(.34f,.4f,1.3f):new Vector3(.34f,.4f,.34f);
                            floats.Slot(1).Box(lamp+Vector3.down*.26f,new Vector3(size.x+.22f,.08f,size.z+.22f),turn,L(.2f,.2f,.22f),L(.32f,.32f,.34f));
                            foreach(int cx in new[]{-1,1})foreach(int cz in new[]{-1,1})floats.Slot(1).Box(lamp+turn*new Vector3(cx*(size.x*.5f+.04f),0,cz*(size.z*.5f+.04f)),new Vector3(.07f,size.y+.06f,.07f),turn,L(.2f,.2f,.22f));
                            floats.Slot(1).Box(lamp+Vector3.up*.25f,new Vector3(size.x+.24f,.1f,size.z+.24f),turn,L(.22f,.22f,.24f),L(.36f,.36f,.38f));
                            glass.Box(lamp,size,turn,LanternGlass);
                            Halo(stuff.pool,new Vector3(p.x,-1.86f,p.z),r*2.6f,root.transform,yaw);
                        }
                        Halo(stuff.lantern,On(s+3.7f)+Vector3.up*(-3.1f-Plane),7,root.transform,yaw);
                        if(k%3==0)
                        {
                            var light=kit.Lamp(On(s+3.7f)+Vector3.up*(-.4f-Plane),15,2.2f,new Color(1f,.76f,.42f));light.transform.SetParent(root.transform,true);
                            var count=light.gameObject.AddComponent<RhythmLamp>();count.count="SSL";count.glow=light;count.colour=new Color(1,.74f,.4f);count.intensity=light.intensity;count.dim=.45f;count.offset=-s/38f;
                        }
                    }
                    var f=kit.Built("The road's lantern floats "+from.ToString("0"),floats,stuff.plank,stuff.rope);f.transform.SetParent(root.transform,true);f.isStatic=true;
                    // Not statically batched: its own mesh stays readable in play, where ReturningPassageTests reads each lens's colour (a batched one is the scene's combined mesh).
                    var g=kit.Built("The road's lantern glass "+from.ToString("0"),glass,stuff.glow);g.transform.SetParent(root.transform,true);g.isStatic=false;g.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                }
            }
            /// <summary>A soft glow lying flat at a point: a lamp seen from above.</summary>
            static GameObject Halo(Material glow,Vector3 at,float size,Transform parent,float yaw=0)
            {
                var halo=GameObject.CreatePrimitive(PrimitiveType.Quad);UnityEngine.Object.DestroyImmediate(halo.GetComponent<Collider>());halo.name="Glow";halo.transform.SetParent(parent,true);
                halo.transform.SetPositionAndRotation(at,Quaternion.Euler(90,yaw,0));halo.transform.localScale=Vector3.one*size;
                var view=halo.GetComponent<Renderer>();view.sharedMaterial=glow;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
                return halo;
            }

            // ---------------------------------------------------------------- the road's edges
            /// <summary>The road's two edges: solid at the plane (boxes along each side, every 20 m of the line, at the road's half width there; the line's two ends shut), and on them the
            /// Wick-folk's chain: a lantern tower on a float every 40 m each side, staggered, its top under the plane and its lamp keeping the count (two short, one long), the pulse running
            /// north along the chain toward the mouth; a great lantern at each bend and either side of the narrows; and along the edge at the plane the count in beads. Returns the towers.</summary>
            static int Edges(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject("The road's edges");var limits=new GameObject("Passage limits");limits.transform.SetParent(root.transform,true);
                void Wall(Vector3 a,Vector3 b,string name)
                {
                    var run=b-a;run.y=0;var wall=new GameObject(name,typeof(BoxCollider));wall.isStatic=true;wall.transform.SetParent(limits.transform,true);
                    wall.transform.SetPositionAndRotation(new Vector3((a.x+b.x)*.5f,Plane,(a.z+b.z)*.5f),Quaternion.LookRotation(run.normalized));wall.GetComponent<BoxCollider>().size=new Vector3(2,12,run.magnitude+.6f);
                }
                int k=0;
                // Every 20 m, and every 10 m where the road narrows and opens, so the chords keep to its line.
                for(float s=0;s<EndS;k++)
                {
                    float e=Mathf.Min(EndS,s+(s>=NarrowsS-20?10:20));
                    foreach(int side in new[]{-1,1})Wall(On(s,side*(Half(s)+1)),On(e,side*(Half(e)+1)),"Passage limit "+(side<0?"port ":"starboard ")+k);
                    s=e;
                }
                Wall(On(0,-Half(0)-2),On(0,Half(0)+2),"Passage limit astern");Wall(On(EndS,-Half(EndS)-2),On(EndS,Half(EndS)+2),"Passage limit ahead");
                // The count along each edge at the plane, just outside the wall: two short and one long, every 11 m, chunked so the far ones can be culled.
                int beads=0;
                for(float from=0;from<EndS;from+=260)
                {
                    var count=new ZoneKit.Shape();
                    for(float s=from+3;s<Mathf.Min(EndS,from+260);s+=11)foreach(int side in new[]{-1,1})
                    {
                        float yaw=Heading(s);var at=On(s,side*(Half(s)+2.2f));at.y=.75f;var ahead=Quaternion.Euler(0,yaw,0)*Vector3.forward;var turn=Quaternion.Euler(0,yaw,0);
                        count.Box(at,new Vector3(.5f,.3f,.5f),turn,Gold);count.Box(at+ahead*1.4f,new Vector3(.5f,.3f,.5f),turn,Gold);count.Box(at+ahead*3.6f,new Vector3(.5f,.3f,2f),turn,Gold);beads++;
                        // The edge itself drawn as a low rail of warm light at the plane, so where the solid is reads from any frame.
                        count.Box(at+ahead*5.5f+Vector3.down*.35f,new Vector3(.14f,.1f,11),turn,ZoneKit.Shape.Dim(Gold,.4f));
                    }
                    var lit=kit.Built("The Wick-folk's count "+from.ToString("0"),count,stuff.glow);lit.transform.SetParent(root.transform,true);lit.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;lit.isStatic=true;
                }
                // The lantern towers on their floats, staggered side to side; every other one keeps the count with a light, its pulse running north at 38 m/s.
                var floats=new ZoneKit.Shape();var glass=new ZoneKit.Shape();int towers=0;var chain=new GameObject("The lantern chain");chain.transform.SetParent(root.transform,true);
                var bends=new[]{BendOneS,BendTwoS,BendThreeS,NarrowsS,MouthS-6};
                for(float s=10;s<EndS-10;s+=20)
                {
                    int slot=(int)(s/20),side=slot%2==0?-1:1;bool great=bends.Any(b=>Mathf.Abs(b-s)<10);bool keeps=(slot/2)%2==0;
                    foreach(int each in great?new[]{-1,1}:new[]{side})
                    {
                        var at=On(s,each*(Half(s)+4.5f));float yaw=Heading(s)+(each<0?25:-25);
                        var tower=ConvoyKit.Hull(great?"GreatLantern":"LanternTower",at.x,at.z,great?-1.2f:-1.6f,yaw,great?2.2f:.8f,chain.transform);if(tower==null)continue;towers++;
                        var box=ZoneKit.Drawn(tower);
                        floats.Slot(0).Tube(new[]{new Vector3(at.x,box.min.y-1.4f,at.z),new Vector3(at.x,box.min.y+.3f,at.z)},new[]{great?5f:3.2f,great?5.3f:3.4f},12,L(.46f,.34f,.22f),L(.64f,.5f,.32f),1,.25f);
                        // Its glass, built with the rest (a primitive cube is what the placeholder check looks for); the count is kept by its light.
                        glass.Box(new Vector3(at.x,box.max.y-.9f,at.z),Vector3.one*(great?1.8f:1.1f),Quaternion.Euler(0,yaw,0),new Color(1f,.72f,.36f));
                        Halo(stuff.lantern,new Vector3(at.x,box.max.y-.3f,at.z),great?14:8,chain.transform);
                        // Past the mouth every tower is lit: the rendezvous is the lanterns' own.
                        if(great||keeps||s>MouthS)
                        {
                            var light=kit.Lamp(new Vector3(at.x,box.max.y+.4f,at.z),great?26:17,great?3.2f:2.4f,new Color(1f,.76f,.42f));light.transform.SetParent(chain.transform,true);
                            var count=light.gameObject.AddComponent<RhythmLamp>();count.count="SSL";count.glow=light;count.colour=new Color(1,.74f,.4f);
                            count.intensity=light.intensity;count.dim=.5f;count.offset=-s/38f;
                        }
                    }
                }
                var floated=kit.Built("The lantern floats",floats,stuff.plank);floated.transform.SetParent(chain.transform,true);floated.isStatic=true;
                var glazed=kit.Built("The lanterns' glass",glass,stuff.glow);glazed.transform.SetParent(chain.transform,true);glazed.isStatic=true;glazed.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                Debug.Log("PASSAGE_EDGES walls="+limits.transform.childCount+" beads="+beads+" towers="+towers);
                return towers;
            }

            // ---------------------------------------------------------------- the convoy
            /// <summary>A group of the convoy moored together, as it is laid: its hulls in its own frame (across, along: metres), each at its own scale and turn, its top under its group's.</summary>
            internal sealed class Group
            {
                public readonly string name;public readonly float top;public readonly (string key,float x,float z,float scale,float yaw,float down)[] hulls;public readonly bool lit;
                public Group(string name,float top,bool lit,params (string,float,float,float,float,float)[] hulls){this.name=name;this.top=top;this.lit=lit;this.hulls=hulls;}
                public float Length=>hulls.Max(h=>h.z)-hulls.Min(h=>h.z)+18;
            }
            /// <summary>Every kind of group, by where it is a place the player knows (manifest section 1's "who does what").</summary>
            internal static readonly Group[] Kinds=
            {
                // Sizes from the second pass's stills: a hauler at 1.6 or a docking pod at 1 filled the frame wherever it was, and Nacre's cutters at 1.5 in their stock dark paint
                // read as fighters three times a hero's size. Nacre's are painted the Nacre approach's green (Lay).
                new("Xylos: the seed barges",-3.4f,true,("SeedBarge",0,0,1.3f,0,0),("SeedBarge",0,23,1.3f,0,.4f)),
                new("Driftreed: the raft bay",-3f,true,("ReedRaft",-5.8f,0,1.4f,90,0),("ReedRaft",5.8f,0,1.4f,90,.5f),("ReedRaft",-5.8f,9,1.4f,90,.5f),("ReedRaft",5.8f,9,1.4f,90,0),("ReedRaft",0,18,1.4f,90,.3f)),
                new("Nacre: the navigators' cutters",-3f,true,("PatrolCutter",-3,0,.7f,0,0),("PatrolCutter",3,5,.7f,0,.3f),("PatrolCutter",0,10,.7f,0,0)),
                new("The Compact's cutters, helping",-2.6f,true,("CompactCutter",-3,0,1.2f,0,0),("CompactCutter",3,0,1.2f,0,0),("CompactCutter",0,6,1.2f,0,.2f)),
                new("Lastlight: a longhouse",-4.2f,true,("KeptLonghouse",0,0,1.35f,90,0),("RibArch",0,8,.95f,90,.6f)),
                new("Oda's hauler",-5f,true,("Hauler",0,0,1.2f,0,0),("Skiff",7,-5,1.1f,10,.4f)),
                new("Teo's carriers",-2.4f,true,("Skiff",-3,0,1.1f,0,0),("Skiff",3,6,1.1f,0,.2f)),
                new("Lanternwick: a lantern float",-2.4f,true,("LanternTower",-4,-3,.6f,20,0),("LanternTower",4,-3,.6f,-20,0),("LanternTower",0,4,.6f,0,.3f)),
                new("Rootmarket's seed house",-2.6f,true,("SeedBarge",0,0,1.35f,0,1.6f),("SeedHouse",0,1,.7f,0,0)),
                new("Many Hands' docking pods",-4f,true,("DockingPodOffice",-4.5f,0,.65f,0,0),("DockingPodRepair",4.5f,7,.65f,0,.4f)),
                new("Dace's outpost, lease ended",-2.8f,true,("Hauler",0,0,1.15f,0,2.2f),("OutpostHab",-2,-2,.9f,0,0),("OutpostHab",2,2.5f,.9f,30,.1f)),
                new("The apprentices' hauler",-3.2f,true,("Hauler",0,0,1.2f,0,1.8f),("HarrowgateHouse",0,1,.9f,0,0)),
                new("Driftreed: the weavers' hall",-2.6f,true,("ReedRaft",-6,0,1.4f,90,1.2f),("ReedRaft",0,0,1.4f,90,1.2f),("ReedRaft",6,0,1.4f,90,1.2f),("WeavingHall",0,0,.85f,0,0)),
            };
            /// <summary>The big hulls deep under the lane's edges and outside it: Tallow's station hull, the ring under tow, haulers in pairs.</summary>
            internal static readonly Group[] Deep=
            {
                // The Crawl's treads stand 23 m tall: they run here, under everything (the second pass had them under the road, reaching down through the deep hulls).
                new("Tallow's station hull",-27f,true,("TallowStationHull",0,0,.75f,0,0)),
                new("The Halo's ring, under tow",-24f,true,("RingSegmentA",0,0,2.2f,0,0),("Skiff",0,26,1.4f,0,-9)),
                new("Haulers in a pair",-24f,true,("Hauler",-7,0,1.6f,0,0),("Hauler",7,10,1.6f,0,.5f)),
                new("The Halo's ring, two segments",-26f,true,("RingSegmentB",-6,0,2.3f,10,0),("RingSegmentA",8,18,2.3f,-10,.5f)),
                new("The Crawl: two treads coupled",-24f,true,("CrawlTread",0,0,1.2f,0,0),("CrawlTread",0,25,1.2f,0,0)),
            };
            /// <summary>The convoy's lanes: where across the bore each runs, the pace it runs at, how far apart its groups lie (beyond their own length), how much deeper than its kinds'
            /// own tops it runs, and what it carries. The two lanes under the road keep one pace and are laid so no group's footprint meets another's, so nothing there ever runs into
            /// anything; the others run outside them or under them. The first pass had them at ±14 and three metres down: the road's middle was empty and what was in frame sat on the
            /// ships like a deck they had landed on.</summary>
            static readonly (string name,float side,float pace,float gap,float down,int kinds,int seed)[] Lanes=
            {
                ("port",-10,6f,6,5.2f,0,11),("starboard",10,6f,7,6.4f,0,23),("port, outside",-50,5.4f,16,6.5f,1,37),("starboard, outside",50,6.6f,18,7f,1,41),
                ("deep port",-44,5f,30,0,2,53),("deep starboard",46,5.2f,36,0,2,67),
            };
            /// <summary>The kinds a lane carries: under the road, every kind but the cutters (the Compact's and Nacre's: sharp-nosed and close, they read as an enemy's craft beside the
            /// heroes, as the fourth pass's arrival showed with Nacre's) and the docking pods (a wide disc that filled every frame it was in); outside it, every kind; deep, the big hulls.</summary>
            static Group[] KindsFor(int which)=>which==0?Kinds.Where(k=>!k.name.StartsWith("The Compact")&&!k.name.StartsWith("Many Hands' docking")&&!k.name.StartsWith("Nacre")).ToArray():which==1?Kinds:Deep;
            static readonly Dictionary<string,Mesh> lightMeshes=new();
            /// <summary>Running lights for a hull of this key, in its own unscaled frame (cached, one mesh a key, saved beside the scene): warm at one end, cold at the other, a warm window
            /// or two along its top.</summary>
            static Mesh LightsFor(ZoneKit kit,string key)
            {
                if(lightMeshes.TryGetValue(key,out var mesh))return mesh;
                var prefab=ZoneKit.Prefab("Environment",key)??ZoneKit.Prefab("Ships",key);if(prefab==null)return null;
                var probe=(GameObject)PrefabUtility.InstantiatePrefab(prefab);probe.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                var box=ZoneKit.Drawn(probe);var local=probe.transform.InverseTransformPoint(box.center);var size=probe.transform.InverseTransformVector(box.size);size=new Vector3(Mathf.Abs(size.x),Mathf.Abs(size.y),Mathf.Abs(size.z));
                UnityEngine.Object.DestroyImmediate(probe);
                var s=new ZoneKit.Shape();bool alongX=size.x>=size.z;var length=alongX?Vector3.right:Vector3.forward;var across=alongX?Vector3.forward:Vector3.right;
                // Small and coloured: the first pass's were a metre and more across and bloomed into white blocks.
                float half=(alongX?size.x:size.z)*.5f,wide=(alongX?size.z:size.x)*.5f,top=local.y+size.y*.5f+.08f,lamp=Mathf.Clamp(Mathf.Min(size.x,size.z)*.025f,.1f,.32f);
                var c=new Vector3(local.x,top,local.z);
                foreach(int sign in new[]{-1,1})
                {
                    s.Box(c+length*half*.86f+across*sign*wide*.5f,Vector3.one*lamp,Quaternion.identity,new Color(1f,.62f,.25f));
                    s.Box(c-length*half*.86f+across*sign*wide*.5f,Vector3.one*lamp,Quaternion.identity,new Color(.3f,.85f,1f));
                }
                mesh=ZoneKit.SaveMesh(s.Mesh("Running lights "+key),"Assets/_Project/Scenes/"+kit.zone+"-running-lights-"+key+".asset");lightMeshes[key]=mesh;return mesh;
            }
            /// <summary>One group of the convoy, laid at a place along the line: its root (what the flow moves), its hulls with their running lights, a glow over it, a light in some.</summary>
            static Transform Lay(ZoneKit kit,Stuff stuff,Group g,float s,float side,float pace,Transform under,int index,float deeper=0)
            {
                var root=new GameObject("Convoy: "+g.name).transform;root.SetParent(under,true);
                ConvoyFlow.At(Line,s,side,out var ahead);var at=On(s,side);at.y=Plane-3;root.SetPositionAndRotation(at,Quaternion.LookRotation(ahead));
                foreach(var (hullKey,x,z,scale,yaw,down) in g.hulls)
                {
                    var p=root.TransformPoint(new Vector3(x,0,z));
                    var hull=ConvoyKit.Hull(hullKey,p.x,p.z,g.top-down-deeper,root.eulerAngles.y+yaw,scale,root);if(hull==null)continue;
                    hull.name="Convoy: "+hullKey;
                    if(hullKey=="PatrolCutter")TugKit.Paint(kit.zone,hull,"nacre",new Color(.6f,1f,.8f));
                    var mesh=LightsFor(kit,hullKey);
                    if(mesh!=null){var lit=new GameObject("Running lights",typeof(MeshFilter),typeof(MeshRenderer));lit.transform.SetParent(hull.transform,false);lit.GetComponent<MeshFilter>().sharedMesh=mesh;
                        var lv=lit.GetComponent<MeshRenderer>();lv.sharedMaterial=stuff.glow;lv.shadowCastingMode=ShadowCastingMode.Off;lv.receiveShadows=false;}
                }
                // No glow laid over a group: seen from above the third pass's read as pale squares round every group. Its running lights and, in some, a lamp light it.
                if(index%4==0){var box=ZoneKit.Drawn(root.gameObject);var l=kit.Lamp(new Vector3(box.center.x,box.max.y+3,box.center.z),20,1.7f,new Color(1f,.8f,.55f));l.transform.SetParent(root,true);}
                foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=false;
                return root;
            }
            /// <summary>The convoy running the passage: every lane filled from where the convoy comes in to where it goes out, each lane's groups in their own order, the Halo's four
            /// districts among them in the lanes under the road, and the navigator with the pencil leading, under the road's middle near the far end. One <see cref="ConvoyFlow"/> moves them.</summary>
            static ConvoyFlow Convoy(ZoneKit kit,Stuff stuff,Renderer[] streams,bool turned)
            {
                var root=new GameObject("The convoy, running");var groups=new List<(Transform t,float s,float side,float pace)>();int index=0;
                // The Halo's districts, copied out of the Broken Ring as they are built there: placed first in the lanes under the road, each with its own length kept clear.
                var districts=ConvoyKit.CopyOut(HomeBuilder.RingZone,true,ConvoyKit.Districts.Select(d=>"District: "+d).ToArray());
                // What lies under the road (the two lanes there, one pace): every group's footprint on the plan, so none is laid where another already is, and so none ever meets
                // another as they flow.
                var footprints=new List<Rect>();
                Rect Print(Transform t){var b=ZoneKit.Drawn(t.gameObject);return Rect.MinMaxRect(b.min.x,b.min.z,b.max.x,b.max.z);}
                bool Clear(Rect r)=>!footprints.Any(f=>f.Overlaps(new Rect(r.x-1.5f,r.y-1.5f,r.width+3,r.height+3)));
                var at=new[]{("the market",0,300f),("the residential crescent",1,420f),("the repair quarter",0,640f),("the tug dock",1,880f)};
                foreach(var (name,lane,s) in at)
                {
                    if(!districts.TryGetValue("District: "+name,out var d))continue;
                    foreach(var lee in d.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Its lee").ToArray())UnityEngine.Object.DestroyImmediate(lee.gameObject);
                    var holder=new GameObject("Convoy: "+name).transform;holder.SetParent(root.transform,true);
                    float side=Lanes[lane].side;ConvoyFlow.At(Line,s,side,out var ahead);var p=On(s,side);p.y=Plane-3;holder.SetPositionAndRotation(p,Quaternion.LookRotation(ahead));
                    d.transform.rotation=holder.rotation;var box=ZoneKit.Drawn(d);
                    // Fitted to the lane: at most 24 m across, its roofs 8 m under the plane (its masts are its own, and stand no higher than 3.25 m over them).
                    float fit=Mathf.Min(1,24/Mathf.Max(1,Mathf.Min(box.size.x,box.size.z)));d.transform.localScale*=fit;box=ZoneKit.Drawn(d);
                    d.transform.position+=new Vector3(p.x-box.center.x,(Plane-7.5f)-box.max.y,p.z-box.center.z);d.transform.SetParent(holder,true);
                    foreach(var t in d.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=false;
                    footprints.Add(Print(holder));groups.Add((holder,s,side,Lanes[lane].pace));index++;
                }
                // The navigator with the pencil, leading: Nacre's green on a cutter of her own, under the road's middle, with her lamp.
                {
                    float s=1130;var g=new Group("The navigator with the pencil",-5f,true,("PatrolCutter",0,0,1.6f,0,0));var t=Lay(kit,stuff,g,s,1,6,root.transform,0);t.name="Convoy: the navigator with the pencil";
                    groups.Add((t,s,1,6));footprints.Add(Print(t));
                }
                for(int li=0;li<Lanes.Length;li++)
                {
                    var lane=Lanes[li];var rng=new System.Random(lane.seed);var kinds=KindsFor(lane.kinds);int next=rng.Next(kinds.Length);bool under=lane.kinds==0;
                    float s=FlowFrom+(float)rng.NextDouble()*lane.gap;int tries=0;
                    while(s<FlowTo-10&&tries<400)
                    {
                        var g=kinds[next];float length=g.Length;
                        float side=lane.side+(float)(rng.NextDouble()-.5)*(lane.kinds==2?10:4);
                        // Its root at its hindmost hull's place, nine metres in: the group then runs from s to s plus its length.
                        float rootAt=s+9-g.hulls.Min(h=>h.z);
                        var t=Lay(kit,stuff,g,rootAt,side,lane.pace,root.transform,index,lane.down);
                        if(under){var print=Print(t);if(!Clear(print)){UnityEngine.Object.DestroyImmediate(t.gameObject);s+=6;tries++;continue;}footprints.Add(print);}
                        index++;next=(next+1+rng.Next(2))%kinds.Length;groups.Add((t,rootAt,side,lane.pace));
                        s+=length+lane.gap*(.7f+.6f*(float)rng.NextDouble());
                    }
                }
                var flow=root.AddComponent<ConvoyFlow>();flow.path=Line;flow.halves=Halves;flow.from=FlowFrom;flow.to=FlowTo;flow.wide=Wide;flow.edgeIn=6;
                flow.groups=groups.Select(g=>g.t).ToArray();flow.along=groups.Select(g=>g.s).ToArray();flow.side=groups.Select(g=>g.side).ToArray();
                flow.height=groups.Select(g=>g.t.position.y).ToArray();flow.pace=groups.Select(g=>g.pace).ToArray();flow.turn=groups.Select(_=>0f).ToArray();
                flow.closeFlag="end.passageRun";flow.closeSeconds=8;flow.streams=streams;flow.streamRate=.1f;
                // Laid where the flow puts them (the narrows squeeze them; the turned stills see them closed to the lanterns).
                for(int i=0;i<flow.groups.Length;i++)
                {
                    float side=flow.Side(flow.side[i],flow.along[i],turned?1:0);var p=ConvoyFlow.At(Line,flow.along[i],side,out var ahead);p.y=flow.height[i];
                    flow.groups[i].SetPositionAndRotation(p,Quaternion.LookRotation(ahead));
                }
                var high=flow.groups.SelectMany(g=>g.GetComponentsInChildren<Renderer>()).Where(r=>!(r is ParticleSystemRenderer)&&r.bounds.max.y>Plane-2.5f).Select(r=>r.name+" "+r.bounds.max.y.ToString("0.0")).ToList();
                if(high.Count>0)Debug.LogWarning("PASSAGE_CONVOY_HIGH "+high.Count+": "+string.Join(", ",high.Take(8)));
                // The lanes under and outside the road run over the deep ones at another pace: say if anything of theirs reaches down to where the deep hulls' tops are.
                float deepTop=Deep.Max(d=>d.top);
                var low=flow.groups.Where(g=>!Deep.Any(d=>g.name=="Convoy: "+d.name)).SelectMany(g=>g.GetComponentsInChildren<Renderer>().Where(r=>!(r is ParticleSystemRenderer)&&r.bounds.min.y<deepTop+.5f).Select(r=>g.name+"/"+r.name+" "+r.bounds.min.y.ToString("0.0"))).Distinct().ToList();
                if(low.Count>0)Debug.LogWarning("PASSAGE_CONVOY_LOW "+low.Count+": "+string.Join(", ",low.Take(10)));
                Debug.Log("PASSAGE_CONVOY groups="+flow.groups.Length+" districts="+districts.Count);
                return flow;
            }

            // ---------------------------------------------------------------- the young, Mother of Doors, Idra
            /// <summary>An enemy's generated body (or a ship's) stood as scenery, at its taken-in size times <paramref name="scale"/>, its middle at a point, turned; under a parent.</summary>
            static GameObject Body(string folder,string id,Vector3 middle,float yaw,float scale,Transform parent)
            {
                var body=ConvoyKit.Body(folder,id,middle,Quaternion.Euler(0,yaw,0),scale);if(body==null)return null;
                body.transform.SetParent(parent,true);foreach(var r in body.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
                foreach(var sk in body.GetComponentsInChildren<SkinnedMeshRenderer>())sk.updateWhenOffscreen=true;
                return body;
            }
            /// <summary>The young (seven, each a head, a segment and a tail of the Cantor's, small, in their cold glow) keeping station to starboard of the lead under the plane, and from
            /// the turn on ahead to the mouth; Mother of Doors (her head, seven segments and her tail, large) far off to starboard and deep, ahead of the lead, and from the turn out of the
            /// mouth; Idra's cutter on the convoy's west flank from the far end on toward the narrows (shown from the turn).</summary>
            static (GameObject young,GameObject mother,GameObject idra) Companions(ZoneKit kit,Stuff stuff,bool turned)
            {
                // ---- the young
                // Deeper than the convoy's own tops and smaller than a hero's ship, each in its glow: the first pass's, three metres under the plane at the ships' own size, read as
                // more craft.
                var young=new GameObject("The young Choir, alongside");var ys=40f;young.transform.SetPositionAndRotation(On(ys,15)+Vector3.up*(-11-Plane),Quaternion.Euler(0,Heading(ys),0));
                for(int k=0;k<7;k++)
                {
                    var lane=new Vector3((k%3-1)*4.2f+(k%2)*1.5f,-(k%3)*1.6f,-k*7.5f);
                    for(int seg=0;seg<3;seg++)
                    {
                        string id=seg==0?"CantorHead":seg==1?"CantorBody":"CantorTail";
                        var p=young.transform.TransformPoint(lane+new Vector3(Mathf.Sin((k+seg)*1.3f)*1.4f,0,-seg*2.4f));
                        Body("Enemies",id,p,Heading(ys)+Mathf.Sin((k+seg)*1.3f)*20,seg==0?.62f:.66f,young.transform);
                    }
                    Halo(stuff.young,young.transform.TransformPoint(lane+new Vector3(0,-1.2f,-2.4f)),11,young.transform);
                }
                var yl=kit.Lamp(young.transform.TransformPoint(new Vector3(0,4,-20)),30,2.4f,new Color(.45f,.95f,1f));yl.transform.SetParent(young.transform,true);
                var swim=young.AddComponent<Alongside>();swim.path=Line;swim.ahead=8;swim.side=15;swim.depth=-11;swim.speed=17;swim.sway=.8f;swim.swayPeriod=6;
                swim.release="end.passageRun";swim.releaseTo=On(1330,22)+Vector3.up*(-10-Plane);swim.releaseSpeed=14;
                // ---- Mother of Doors
                // Far under the convoy and larger than anything in it, with the haze between: at 96 m down and the convoy's own scale (the second pass) she read as one more thing
                // flying among the hulls.
                var mother=new GameObject("Mother of Doors, far off");var ms=150f;mother.transform.SetPositionAndRotation(On(ms,60)+Vector3.up*(-148-Plane),Quaternion.Euler(0,Heading(ms),0));
                Body("Enemies","MotherOfDoors",mother.transform.TransformPoint(new Vector3(0,0,44)),Heading(ms),3.4f,mother.transform);
                for(int seg=0;seg<8;seg++){float z=16-seg*23;var p=mother.transform.TransformPoint(new Vector3(Mathf.Sin(seg*.8f)*13,-Mathf.Cos(seg*.6f)*4,z));Body("Enemies","MotherBody",p,Heading(ms)+Mathf.Cos(seg*.8f)*24,3.1f,mother.transform);}
                Body("Enemies","MotherTail",mother.transform.TransformPoint(new Vector3(Mathf.Sin(8*.8f)*13,-3,-182)),Heading(ms)+20,3.1f,mother.transform);
                for(int k=0;k<6;k++)Halo(stuff.young,mother.transform.TransformPoint(new Vector3(Mathf.Sin(k*1.1f)*10,-9,44-k*42)),62,mother.transform);
                // The haze between her and the convoy, in her own paint: dimmer and colder than anything near (the third pass's, at full colour 128 m down, read as near and small).
                foreach(var part in mother.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Drawn: ")).ToArray())TugKit.Paint(kit.zone,part.gameObject,"far",new Color(.52f,.56f,.78f));
                var ml=kit.Lamp(mother.transform.TransformPoint(new Vector3(0,22,0)),120,3f,new Color(.45f,.95f,1f));ml.transform.SetParent(mother.transform,true);
                var goes=mother.AddComponent<Alongside>();goes.path=Line;goes.ahead=140;goes.side=60;goes.depth=-148;goes.speed=24;goes.sway=4;goes.swayPeriod=18;
                goes.release="end.passageRun";goes.releaseTo=On(1560,40)+Vector3.up*(-150-Plane);goes.releaseSpeed=16;
                // ---- Idra's cutter on the flank (D286: she takes the convoy's flank and is seen holding it)
                var idra=new GameObject("Idra's cutter, on the flank");float iz=FarEndS+10;idra.transform.SetPositionAndRotation(On(iz,-24)+Vector3.up*(-2.4f-Plane),Quaternion.Euler(0,Heading(iz),0));
                var cutter=Body("Characters","IdraFlight",idra.transform.position,Heading(iz),1,idra.transform);
                if(cutter!=null){var b=ZoneKit.Drawn(cutter);cutter.transform.position+=Vector3.up*((Plane-2.2f)-b.max.y);}
                var il=kit.Lamp(idra.transform.position+Vector3.up*3,14,2,new Color(1f,.8f,.5f));il.transform.SetParent(idra.transform,true);
                var flank=idra.AddComponent<Alongside>();flank.path=Line;flank.side=-24;flank.depth=idra.transform.position.y;flank.pace=6;flank.stopAt=NarrowsS-6;flank.speed=10;flank.from="end.passageRun";
                if(turned){young.transform.position=swim.releaseTo;mother.transform.position=goes.releaseTo;}
                foreach(var root in new[]{young,mother,idra})foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=false;
                return (young,mother,idra);
            }

            // ---------------------------------------------------------------- the rendezvous
            /// <summary>Past the far mouth, in the open: Many Hands (its own hull, copied out of its scene) waiting with Orrin's board aboard, deep under the plane, the first hulls through
            /// gathering round it; the great lanterns at the mouth's lip.</summary>
            static GameObject Rendezvous(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject("The rendezvous");
                var hands=ConvoyKit.ManyHands(Vector3.zero,.3f,0);
                if(hands!=null)
                {
                    var box=ZoneKit.Drawn(hands);var goal=On(EndS-14,-40);goal.y=-24;
                    hands.transform.position+=new Vector3(goal.x-box.center.x,goal.y-box.max.y,goal.z-box.center.z);hands.transform.SetParent(root.transform,true);
                    var l=kit.Lamp(new Vector3(goal.x,goal.y+6,goal.z),60,2.4f,new Color(1f,.82f,.56f));l.transform.SetParent(root.transform,true);
                }
                foreach(var (hullKey,s,side,top,scale) in new[]{("PatrolCutter",1330f,26f,-6f,1.5f),("SeedBarge",1352f,-30f,-7f,1.5f),("Hauler",1376f,30f,-9f,1.6f),("ReedRaft",1340f,-18f,-4f,1.6f)})
                {
                    var p=On(s,side);var hull=ConvoyKit.Hull(hullKey,p.x,p.z,top,Heading(s)+side,scale,root.transform);
                    var mesh=LightsFor(kit,hullKey);if(hull!=null&&mesh!=null){var lit=new GameObject("Running lights",typeof(MeshFilter),typeof(MeshRenderer));lit.transform.SetParent(hull.transform,false);lit.GetComponent<MeshFilter>().sharedMesh=mesh;lit.GetComponent<MeshRenderer>().sharedMaterial=stuff.glow;}
                }
                return root;
            }

            // ---------------------------------------------------------------- what is said on the way
            /// <summary>The run's radio and the heroes' words, by place along the line (manifest section 7; docs/maps/ReturningPassage.md, "Radio and barks").</summary>
            static void Calls(ZoneKit kit)
            {
                var calls=new GameObject("The run's radio",typeof(PassageCalls)).GetComponent<PassageCalls>();calls.transform.position=Arrival;calls.path=Line;
                const string Run="end.passageRun",Tow="end.lineOn";
                PassageCalls.Call C(string who,string key,int index,float along,string needs="",string unless=Run,bool back=false)=>new PassageCalls.Call{speaker=who,key=key,index=index,along=along,needs=needs,unless=unless,back=back};
                calls.calls=new[]
                {
                    C("","passage",0,50),C("","youngchoir",0,120),
                    C("Pell","hail.count",0,190),C("Ilo","hail.names",0,245),C("Dorrow","hail.step",0,300),C("Ilo","hail.names",1,360),C("Mira","hail.manifest",0,415),
                    C("Pell","hail.count",1,470),C("Orrin","hail.board",0,530),C("Ilo","hail.names",2,585),C("Hal","hail.cradle",0,640),C("Neve","hail.rear",0,695),
                    C("Ilo","hail.names",3,750),C("Pell","hail.count",2,790),C("Orrin","hail.board",1,850),C("Dorrow","hail.step",1,905),C("Mira","hail.manifest",1,960),
                    C("Bryn","hail.lead",0,1015),
                    C("","wake",0,1040,"end.turnedBack",Tow,true),
                    C("Ilo","hail.names",4,820,Tow,"end.rendezvous"),C("Neve","hail.rear",1,900,Tow,"end.rendezvous"),C("Ilo","hail.names",5,980,Tow,"end.rendezvous"),C("Orrin","hail.board",2,1150,Tow,"end.rendezvous"),
                };
            }
        }
    }
}
