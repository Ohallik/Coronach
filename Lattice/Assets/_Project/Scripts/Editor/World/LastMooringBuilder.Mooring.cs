using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Last Mooring (space combat; brief, "LastMooring"): the approach to the last shared passage. Rule: arenas divided by anchor chains. Stillwater's chains are strung taut at the plane between
    /// white pylons that rise out of the Compact's anchor works far below; the whole convoy is moored on them, every hull a place the player has been (the Halo's districts, Tallow's hull, the seed
    /// barges, the reed rafts, the Crawl's treads, the ring's segments, Lastlight's longhouses, Nacre's cutters, Many Hands itself), with Lanternwick's towers at every gap; the Tollkeeper's carrier
    /// lies across the way at the north end with its cage closed, and the Continuance holds the passage's mouth beyond it. The party flies four chain lines north (two interceptor pairs, a gap held
    /// by crossed clamp guns), sees Many Hands' galley quarter struck and held (MaruSectionHeld), can dock at two maintenance islands (berths: a free cradle on one, a Compact pair holding the other),
    /// breaks the Tollkeeper (its fight's file) on its arm (a berth this file declares), and docks at the Continuance's boarding spine.
    /// <para>Map brief, every coordinate, the berths, the Tollkeeper's arena contract and the route writers' numbers: docs/maps/LastMooring.md. Every place another file needs is a constant of
    /// <see cref="Mooring"/>. The convoy's pieces every zone that shows the convoy draws alike are <see cref="ConvoyKit"/> (the passage's too).</para>
    /// </summary>
    public static partial class LastMooringBuilder
    {
        /// <summary>The zone's own fields on its ZoneDef, set last inside <c>Define</c>'s lambda for it.</summary>
        static void MooringDefinition(ZoneDef z){}

        [ZoneLook.Source] static void MooringLooks()
        {
            // A hard cold star from the south-west on the Compact's white (its chains, its pylons, the works far down), so the convoy's own light is the warm in every frame: the districts'
            // windows, the lantern towers at the gaps, the Wick-folk count along the chains. A slate fill keeps a hull's shaded side readable; a blue haze takes the works back, so the
            // convoy reads over them in layers; bloom on what is lit.
            ZoneLook.Register(MooringZone,new ZoneLook.Spec{sun=C(.93f,.95f,1f),sunIntensity=1.3f,sunPitch=42,shadowStrength=.82f,
                sky=C(.34f,.38f,.48f),equator=C(.28f,.3f,.36f),ground=C(.12f,.13f,.16f),
                fog=true,fogColor=C(.08f,.1f,.17f),fogStart=60,fogEnd=380,
                contrast=14,saturation=9,splitShadows=C(.4f,.44f,.58f),splitHighlights=C(.62f,.55f,.46f),
                bloomThreshold=.88f,bloomIntensity=.85f,vignette=.22f});
        }

        // ================================================================ the convoy's shared pieces
        /// <summary>The convoy as every zone of the chapter that shows it draws it (Last Mooring, the Returning Passage): each hull at the size it was taken in at (docs/art/intake.json), and the
        /// places the player has flown over copied out of their own scenes as they stand there (Many Hands' hull, the Halo's four districts), so nothing of a place the player knows is built twice
        /// and each reads as itself. Everything it stands is drawn only: no collider, no behaviour, nothing that moves a hero.</summary>
        internal static class ConvoyKit
        {
            /// <summary>The Halo's four districts by the names chapter 9 gave them (HomeBuilder.Ring.Districts): copied out of the Broken Ring as they are built there.</summary>
            internal static readonly string[] Districts={"the market","the repair quarter","the residential crescent","the tug dock"};
            /// <summary>A hull of the convoy, drawn only, at its taken-in size times <paramref name="scale"/>, turned, its drawn box's middle at (x, z) and its top at <paramref name="top"/>.</summary>
            internal static GameObject Hull(string key,float x,float z,float top,float yaw,float scale=1,Transform parent=null,float pitch=0,float roll=0)
            {
                var prefab=ZoneKit.Prefab("Environment",key)??ZoneKit.Prefab("Ships",key);
                if(prefab==null){Debug.LogWarning("MOORING_CONVOY_MISSING "+key);return null;}
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name="Convoy: "+key;
                foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
                go.transform.localScale*=scale;go.transform.rotation=Quaternion.Euler(pitch,yaw,roll);
                var box=ZoneKit.Drawn(go);go.transform.position+=new Vector3(x-box.center.x,top-box.max.y,z-box.center.z);
                if(parent!=null)go.transform.SetParent(parent,true);
                foreach(var r in go.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}
                return go;
            }
            /// <summary>A generated body (an enemy's, a boss's) stood as scenery: unpacked, every collider, behaviour and animator taken off, turned, its drawn middle at <paramref name="middle"/>.</summary>
            internal static GameObject Body(string folder,string id,Vector3 middle,Quaternion turned,float scale=1)
            {
                var prefab=ZoneKit.Prefab(folder,id);if(prefab==null){Debug.LogWarning("MOORING_CONVOY_MISSING "+folder+"/"+id);return null;}
                var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);PrefabUtility.UnpackPrefabInstance(body,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                foreach(var c in body.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
                foreach(var b in body.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null)UnityEngine.Object.DestroyImmediate(b);
                foreach(var a in body.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(a);
                body.name="Drawn: "+id;body.transform.localScale*=scale;body.transform.rotation=turned;
                var box=ModelGeometry.BoundsOf(body);body.transform.position+=middle-box.center;
                return body;
            }
            /// <summary>Copies of named objects out of another zone's built scene, as they stand there, into the open one: only what is drawn (and, with <paramref name="lights"/>, its lights)
            /// is kept; every collider, body, behaviour, sign and switched-off state comes off. A scene that isn't built, or a name it lacks, is said in the log and skipped.</summary>
            internal static Dictionary<string,GameObject> CopyOut(string zone,bool lights,params string[] names)
            {
                var found=new Dictionary<string,GameObject>();string path="Assets/_Project/Scenes/"+zone+".unity";
                if(!File.Exists(path)){Debug.LogWarning("MOORING_COPY_MISSING "+path);return found;}
                var active=SceneManager.GetActiveScene();var other=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    var all=other.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToList();
                    foreach(var name in names)
                    {
                        var source=all.FirstOrDefault(t=>t.name==name);if(source==null){Debug.LogWarning("MOORING_COPY_MISSING "+zone+": "+name);continue;}
                        var copy=UnityEngine.Object.Instantiate(source.gameObject);copy.name=name;SceneManager.MoveGameObjectToScene(copy,active);
                        Strip(copy,lights);found[name]=copy;
                    }
                }
                finally{EditorSceneManager.CloseScene(other,true);SceneManager.SetActiveScene(active);}
                return found;
            }
            /// <summary>Only what is drawn: switched-off states, signs (a word on a hull far under the plane is no sign), colliders, bodies, behaviours and sounds come off, in an order no
            /// component's requirement stops.</summary>
            internal static void Strip(GameObject copy,bool lights)
            {
                foreach(var tr in copy.GetComponentsInChildren<Transform>(true).Reverse())if(tr!=null&&tr!=copy.transform&&!tr.gameObject.activeSelf)UnityEngine.Object.DestroyImmediate(tr.gameObject);
                foreach(var text in copy.GetComponentsInChildren<TMPro.TMP_Text>(true))if(text!=null)UnityEngine.Object.DestroyImmediate(text.gameObject);
                foreach(var b in copy.GetComponentsInChildren<MonoBehaviour>(true).Reverse())if(b!=null)UnityEngine.Object.DestroyImmediate(b);
                foreach(var c in copy.GetComponentsInChildren<Collider>(true))if(c!=null)UnityEngine.Object.DestroyImmediate(c);
                foreach(var c in copy.GetComponentsInChildren<Rigidbody>(true))if(c!=null)UnityEngine.Object.DestroyImmediate(c);
                foreach(var c in copy.GetComponentsInChildren<NavMeshObstacle>(true))if(c!=null)UnityEngine.Object.DestroyImmediate(c);
                foreach(var c in copy.GetComponentsInChildren<AudioSource>(true))if(c!=null)UnityEngine.Object.DestroyImmediate(c);
                if(!lights)foreach(var c in copy.GetComponentsInChildren<Light>(true))if(c!=null)UnityEngine.Object.DestroyImmediate(c);
                foreach(var t in copy.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=false;
            }
            /// <summary>Many Hands, the convoy's hub: its own hull (chapter 5's, ManyHandsBuilder.Outside) copied out of its scene at <paramref name="scale"/>, its origin at <paramref name="at"/>.
            /// A piece with no uvs under a textured material paints one texel's colour (in the city its collision body carries the uvs): it goes, as HomeBuilder.Ring's distant copy found.</summary>
            internal static GameObject ManyHands(Vector3 at,float scale,float yaw)
            {
                var copies=CopyOut(LastMooringBuilder.Hull,false,"The ship");if(!copies.TryGetValue("The ship",out var ship))return null;
                int bare=0;
                foreach(var f in ship.GetComponentsInChildren<MeshFilter>(true))
                {
                    var r=f.GetComponent<MeshRenderer>();if(r==null||f.sharedMesh==null||f.sharedMesh.HasVertexAttribute(VertexAttribute.TexCoord0))continue;
                    if(!r.sharedMaterials.Any(m=>m!=null&&m.HasProperty("_BaseMap")&&m.GetTexture("_BaseMap")!=null))continue;
                    UnityEngine.Object.DestroyImmediate(r);UnityEngine.Object.DestroyImmediate(f);bare++;
                }
                var pivot=new GameObject("Many Hands, moored");pivot.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));
                ship.transform.SetParent(pivot.transform,false);ship.transform.localPosition=Vector3.zero;ship.transform.localRotation=Quaternion.identity;ship.transform.localScale=Vector3.one*scale;ship.name="Many Hands";
                Debug.Log("MOORING_MANY_HANDS renderers="+ship.GetComponentsInChildren<Renderer>().Length+" bare="+bare+" drawn="+ZoneKit.Drawn(ship));
                return pivot;
            }
        }

        // ================================================================ the zone
        internal static class Mooring
        {
            // ---------------------------------------------------------------- the numbers (docs/maps/LastMooring.md reads these; so do the tests and the routes)
            internal const float Plane=ZoneKit.FlightPlane;
            /// <summary>The outer chains (the zone's walls) at x ±<see cref="OuterX"/> from <see cref="SouthZ"/> to the gate line at <see cref="GateZ"/>; past the gate the way is x ±<see cref="BeyondX"/>
            /// to <see cref="NorthZ"/>. The gate's opening (the cage's) is x ±<see cref="GateHalf"/>.</summary>
            internal const float OuterX=58,SouthZ=-18,GateZ=470,NorthZ=548,BeyondX=44,GateHalf=40;
            /// <summary>Every shelf's floor (the islands, the arm): 3 m under the plane. The anchor works' top, far under everything.</summary>
            internal const float ShelfY=-2,WorksY=-135;
            /// <summary>A chain's solid width at the plane (as wide as its links are drawn: Stillwater's chain taken in at 30 m a piece is 14 m across its links, so it is laid at
            /// <see cref="ChainScale"/>), how far its drawn links stand over the plane, a pylon's radius at the plane.</summary>
            internal const float ChainScale=.38f,ChainWide=5f,ChainOver=1.3f,PylonRadius=2.4f;

            /// <summary>A chain line across the anchorage, with its one gap.</summary>
            internal sealed class Line{public readonly string id;public readonly float z,gapFrom,gapTo;public Line(string id,float z,float from,float to){this.id=id;this.z=z;gapFrom=from;gapTo=to;}public float GapMiddle=>(gapFrom+gapTo)*.5f;}
            internal static readonly Line[] Lines={new("A",60,-8,8),new("B",160,12,30),new("C",270,-30,-12),new("D",386,-8,8)};
            /// <summary>The Tollkeeper's arena's dividing chains (x, from z to z): the guns' lanes are between them (<see cref="GunLanes"/>, lane middles). They stop short of z 420: from there
            /// to the gate nothing stands over the plane inside x ±40 but the cage's braces (its contract: its fans and lanes are read on the plane there).</summary>
            internal static readonly float[] ArenaChainX={-18,18};internal const float ArenaChainFrom=396,ArenaChainTo=416;
            internal static readonly float[] GunLanes={-38,0,38};

            internal static readonly Vector3 Arrival=new(0,Plane,0),HomeWarp=new(-10,Plane,-4),Checkpoint=new(0,Plane,372),ContinuanceSpawn=new(10,Plane,512),DockAt=new(0,Plane,524);
            internal static readonly Vector3 Lane1=new(0,Plane,110),Lane2=new(0,Plane,326),LaneFightSize=new(2*OuterX-4,8,44);
            // The gap's guns hang on chain C's south face, the side a party comes from, their 2 m bodies centred 3 m south of its line: stood on the line itself, each body lay wholly
            // inside the chain's 5 m solid, which stops every shot, and nobody could break them (route 64 in the player, November 15).
            internal static readonly Vector3 GapCentre=new(-21,Plane,270),GapSize=new(44,8,36),GunWest=new(-35,Plane,270-ChainWide*.5f-.5f),GunEast=new(-7,Plane,270-ChainWide*.5f-.5f);
            /// <summary>The section's crossing: across the whole arena twelve metres north of chain B's gap, where Many Hands' galley quarter is under the frame.</summary>
            internal static readonly Vector3 Crossing=new(0,Plane,172),CrossingSize=new(2*OuterX,8,6),MatsHail=new(21,Plane,150);
            /// <summary>Many Hands' hull, copied at <see cref="ManyHandsScale"/>, its origin at <see cref="ManyHandsAt"/>; its galley quarter (the hull's own (86, -2)) at <see cref="Galley"/>.</summary>
            internal const float ManyHandsScale=.3f;
            internal static readonly Vector3 Galley=new(21,-12,186),ManyHandsAt=new(Galley.x-86*ManyHandsScale,Galley.y,Galley.z+2*ManyHandsScale);

            /// <summary>The Tollkeeper's door (its box over the arena at the plane, its mouth the whole south face), the carrier's middle T (north of the lane, under the plane, its top at
            /// <see cref="CarrierTop"/>), the cage's hinges at the gate's opening.</summary>
            internal static readonly Vector3 DoorCentre=new(0,Plane,428),DoorSize=new(110,8,72);
            internal const float CarrierZ=492,CarrierTop=-14;
            internal static readonly Vector3 HingeWest=new(-GateHalf,Plane,GateZ-2),HingeEast=new(GateHalf,Plane,GateZ-2);
            /// <summary>The carrier's middle, as built (the body taken in is 90 x 31.2 x 28.8: its top at <see cref="CarrierTop"/>): the boss's spawner stands here. The build says if the
            /// body it measures puts its middle anywhere else (<c>MOORING_CARRIER_MOVED</c>).</summary>
            internal static readonly Vector3 TollkeeperAt=new(0,-29.6f,CarrierZ);

            /// <summary>The Tollkeeper's arm: its deck 6 m across by 12 along (its root, which the fight drives through <c>berth.Drive</c>, at the deck's middle <see cref="ArmOut"/>; drawn in it is
            /// at <see cref="ArmIn"/>, under the carrier), the gate motor at its north end (<see cref="ArmMotor"/>, an empty facing south for the fight's motor), the berth on it.</summary>
            internal const string ArmBerth="Arm",ArmReturn="ArmReturn",ArmRoot="The Tollkeeper's arm",MotorSeat="Gate motor seat";
            internal static readonly Vector3 ArmOut=new(0,ShelfY,450),ArmIn=new(0,ShelfY-6,486);
            internal static readonly Bounds ArmBox=new(new Vector3(0,ShelfY+.6f,450),new Vector3(6,1.2f,12));
            internal static readonly Vector3 ArmPad=new(0,Plane,450),ArmLaunch=new(-1.6f,ShelfY,449.5f),ArmFoot=new(1.4f,ShelfY,445.4f),ArmFlight=new(10,Plane,440),ArmReturnAt=new(0,Plane,382),ArmMotor=new(0,ShelfY,454.6f);

            /// <summary>A maintenance island between the chains: a berth (wiring 6.1.1), its floor at <see cref="ShelfY"/>, axis-aligned, its foot spawn on its south edge, its launch on the same
            /// edge to the west, its pad over its middle, its flight place south of it and the spawn a save made on it names south of that.</summary>
            internal sealed class Island
            {
                public readonly int index;public readonly string name,berth,returnSpawn;public readonly Vector3 middle,size,pad,foot,launch,flight,back;
                public Island(int i,string name,Vector2 at,float wide,float deep,float side)
                {
                    index=i;this.name=name;berth="Island"+i;returnSpawn="Island"+i+"Pad";middle=new Vector3(at.x,ShelfY,at.y);size=new Vector3(wide,1.2f,deep);
                    pad=new Vector3(at.x,Plane,at.y);foot=new Vector3(at.x+1,ShelfY,at.y-deep*.5f+1.6f);launch=new Vector3(at.x-wide*.5f+2.2f,ShelfY,at.y-deep*.5f+1.6f);
                    flight=new Vector3(at.x,Plane,at.y-deep*.5f-6);back=new Vector3(at.x+side*8,Plane,at.y-deep*.5f-10);
                }
                public Bounds Box=>new(middle+Vector3.up*.6f,size);
                public Vector3 At(float x,float z,float y=ShelfY)=>new(middle.x+x,y,middle.z+z);
            }
            internal static readonly Island[] Islands={new(1,"The west maintenance island",new Vector2(-47,228),18,12,1),new(2,"The east maintenance island",new Vector2(44,246),20,14,-1)};
            /// <summary>Island 1's free cradle (on its north edge); island 2's fight (two Compact Wardens, optional) and its box, which starts 4 m in from the south edge the party lands on.</summary>
            internal static Vector3 Cradle=>Islands[0].At(3,4);
            internal static Vector3 IslandFightCentre=>Islands[1].At(0,2.6f,ShelfY+.7f);
            internal static Vector3 IslandFightSize=>new(Islands[1].size.x-1,1.6f,Islands[1].size.z-6);

            /// <summary>LookDev's stills: the arrival over the Halo's districts, each chain's gap as it is met, both pairs' arenas, the section and Many Hands, each island from the plane and
            /// on foot, the guns' gap, the queue at chain D, the checkpoint, the Tollkeeper's lanes and its arm (from the plane and on foot), the cage, the Continuance's dock, wide views.</summary>
            internal static LookDev.View[] Views=>new[]
            {
                new LookDev.View("arrival",Arrival,true),new("lee-west",new Vector3(-30,Plane,20),true),new("lee-east",new Vector3(32,Plane,24),true),new("gap-a",new Vector3(0,Plane,54),true),
                new("lane-1",Lane1,true),new("arena-1-west",new Vector3(-36,Plane,100),true),new("arena-1-east",new Vector3(36,Plane,128),true),new("gap-b",new Vector3(21,Plane,154),true),
                new("crossing",new Vector3(21,Plane,172),true),new("many-hands",new Vector3(-10,Plane,206),true),new("island-1",Islands[0].pad,true),new("island-1-foot",Islands[0].middle,false),
                new("island-2",Islands[1].pad,true),new("island-2-foot",Islands[1].middle,false),new("rafts",new Vector3(40,Plane,262),true),new("gap-c",new Vector3(-21,Plane,264),true),
                new("lane-2",Lane2,true),new("arena-3-west",new Vector3(-36,Plane,308),true),new("arena-3-east",new Vector3(34,Plane,340),true),new("queue",new Vector3(0,Plane,364),true),
                new("gate-checkpoint",Checkpoint,true),new("arena-west",new Vector3(-38,Plane,420),true),new("arena-mouth",new Vector3(0,Plane,404),true),new("arena-east",new Vector3(38,Plane,428),true),
                new("arm",ArmPad,true),new("arm-foot",ArmOut,false),new("cage",new Vector3(0,Plane,462),true),new("beyond",new Vector3(0,Plane,494),true),new("dock",DockAt,true),
                new("lee-wide",new Vector3(0,Plane,24),true,3),new("moorings-wide",new Vector3(0,Plane,200),true,3.4f),new("arena-wide",new Vector3(0,Plane,432),true,3.4f),new("overview",new Vector3(0,Plane,260),true,11),
            };

            // ---------------------------------------------------------------- what it is made of
            internal sealed class Stuff{public Material works,worksDark,teal,cable,glow,cold,warm,lantern,scorch,mats,vapour;}
            static Color L(float r,float g,float b)=>new Color(r,g,b).linear;
            static readonly Color White=L(.9f,.91f,.93f),Pale=L(.78f,.8f,.84f),Grey=L(.48f,.5f,.54f),Iron=L(.26f,.27f,.3f),Teal=L(.24f,.62f,.64f),Amber=L(.95f,.72f,.25f),Cyan=new(.55f,.95f,1f),Warm=new(1f,.8f,.48f);
            static Stuff Materials(ZoneKit kit)
            {
                var s=new Stuff();
                // The Compact's plate: white in courses with teal seams (Stillwater's own paint, as Sorrel's works), a dark grey under it.
                var plate=ZoneKit.Texture("Mooring/compact-plate",256,(u,v)=>{float n=.84f+.08f*(ZoneKit.Cloud(u,v,8,41)-.5f)+.04f*(ZoneKit.Tile(u*64,v*64,64,42)-.5f);
                    float seam=Mathf.Abs(Mathf.Repeat(u*4,1)-.5f)<.015f||Mathf.Abs(Mathf.Repeat(v*2,1)-.5f)<.02f?-.22f:0;return new Color(n+seam,n+seam,n*1.01f+seam,1);});
                s.works=ZoneKit.Tinted(ZoneKit.Toon("mooring-works",plate,m=>{m.SetColor("_BaseColor",new Color(.96f,.97f,.98f));m.SetFloat("_RampSteps",4);m.SetFloat("_LightFloor",.38f);m.SetColor("_ShadowTint",new Color(.42f,.46f,.6f));}));
                s.worksDark=ZoneKit.Tinted(ZoneKit.Toon("mooring-works-dark",plate,m=>{m.SetColor("_BaseColor",new Color(.42f,.44f,.48f));m.SetFloat("_RampSteps",3);m.SetFloat("_LightFloor",.3f);}));
                s.teal=ZoneKit.Tinted(ZoneKit.Toon("mooring-trim",null,m=>{m.SetColor("_BaseColor",Color.white);m.SetFloat("_RampSteps",3);m.SetFloat("_LightFloor",.42f);}));
                s.cable=ZoneKit.Tinted(ZoneKit.Toon("mooring-cable",null,m=>{m.SetColor("_BaseColor",Color.white);m.SetFloat("_RampSteps",3);m.SetFloat("_LightFloor",.36f);}));
                // Lamps: unlit, coloured in their vertices, bright enough to bloom (the ring's glow).
                s.glow=ZoneKit.Toon("mooring-glow",null,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetVector("_GlowParams",new Vector4(1.4f,0,0,0));});
                var soft=ZoneKit.Texture("Mooring/soft-glow",128,(u,v)=>{float d=Vector2.Distance(new Vector2(u,v),new Vector2(.5f,.5f))*2;float a=Mathf.Pow(Mathf.Clamp01(1-d),2.2f);return new Color(a,a,a,1);},false);
                Material Soft(string asset,Color colour){var m=kit.Glow(asset,false,colour);m.SetTexture("_BaseMap",soft);return m;}
                s.cold=Soft("Pylon glow",new Color(.5f,1.2f,1.4f,1));s.warm=Soft("Window glow",new Color(1.4f,1.05f,.6f,1));s.lantern=Soft("Lantern glow",new Color(1.6f,1.1f,.55f,1));
                s.vapour=Soft("Vapour",new Color(.85f,.9f,1f,1));
                s.scorch=ZoneKit.Tinted(ZoneKit.Toon("mooring-scorch",null,m=>{m.SetColor("_BaseColor",Color.white);m.SetFloat("_RampSteps",2);m.SetFloat("_LightFloor",.1f);}));
                // Driftreed's air-mats: reeds woven over and under, rough side out, in the weavers' green and straw.
                var weave=ZoneKit.Texture("Mooring/reed-mat",128,(u,v)=>{float over=Mathf.Repeat(Mathf.Floor(u*16)+Mathf.Floor(v*16),2);float reed=over<1?.5f+.5f*Mathf.Abs(Mathf.Sin(v*16*Mathf.PI)):.5f+.5f*Mathf.Abs(Mathf.Sin(u*16*Mathf.PI));
                    float n=.82f+.18f*reed+.06f*(ZoneKit.Cloud(u,v,4,51)-.5f);return new Color(.78f*n,.72f*n,.44f*n,1);});
                s.mats=ZoneKit.Tinted(ZoneKit.TwoSided(ZoneKit.Toon("mooring-mats",weave,m=>{m.SetColor("_BaseColor",Color.white);m.SetFloat("_RampSteps",3);m.SetFloat("_LightFloor",.4f);})));
                return s;
            }

            // ---------------------------------------------------------------- the chains: every run at the plane, its gap, the pylons at its ends
            /// <summary>Every chain run at the plane (from, to), named. The gate's own run (x ±<see cref="GateHalf"/> on the gate line) is not here: it is the cage's, solid only until the
            /// Tollkeeper's motor dies.</summary>
            internal static List<(string name,Vector2 from,Vector2 to)> Runs()
            {
                var runs=new List<(string,Vector2,Vector2)>
                {
                    ("Outer chain west",new(-OuterX,SouthZ),new(-OuterX,GateZ)),("Outer chain east",new(OuterX,SouthZ),new(OuterX,GateZ)),("South chain",new(-OuterX,SouthZ),new(OuterX,SouthZ)),
                    ("Gate flank west",new(-OuterX,GateZ),new(-GateHalf,GateZ)),("Gate flank east",new(GateHalf,GateZ),new(OuterX,GateZ)),
                    ("Beyond west",new(-BeyondX,GateZ),new(-BeyondX,NorthZ)),("Beyond east",new(BeyondX,GateZ),new(BeyondX,NorthZ)),("North chain",new(-BeyondX,NorthZ),new(BeyondX,NorthZ)),
                };
                foreach(var l in Lines){runs.Add(("Chain "+l.id+" west",new(-OuterX,l.z),new(l.gapFrom,l.z)));runs.Add(("Chain "+l.id+" east",new(l.gapTo,l.z),new(OuterX,l.z)));}
                foreach(var x in ArenaChainX)runs.Add(("Arena chain "+(x<0?"west":"east"),new(x,ArenaChainFrom),new(x,ArenaChainTo)));
                return runs;
            }
            /// <summary>Where the chains are held: every run's two ends. Where a run meets the outer chains (and the gate's corners) a pylon rises out of the works far below
            /// (<c>deep</c>); everywhere else (a gap's edge, the end of an arena chain) a clamp head grips the chain's cut end and hangs a short drum under it. The first build stood a pylon every
            /// 32 m along every run, and every frame was a forest of white columns converging on the works.</summary>
            internal static List<(Vector2 at,bool deep)> Pylons()
            {
                var at=new List<(Vector2,bool)>();
                void Add(Vector2 p){if(at.All(q=>Vector2.Distance(p,q.Item1)>4))at.Add((p,Mathf.Abs(Mathf.Abs(p.x)-OuterX)<1||Mathf.Abs(p.y-SouthZ)<1||Mathf.Abs(Mathf.Abs(p.x)-BeyondX)<1));}
                foreach(var (_,a,b) in Runs()){Add(a);Add(b);}
                return at;
            }
            /// <summary>The nearest point of any chain to a point of the plan, and how far.</summary>
            internal static Vector2 NearestChain(Vector2 p,out float distance)
            {
                distance=float.PositiveInfinity;var best=p;
                foreach(var (_,a,b) in Runs()){var d=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,d)/Mathf.Max(.0001f,d.sqrMagnitude));var q=a+d*t;float gap=Vector2.Distance(p,q);if(gap<distance){distance=gap;best=q;}}
                return best;
            }

            // ---------------------------------------------------------------- the build
            /// <summary>Builds LastMooring.</summary>
            internal static bool Build()
            {
                var kit=ZoneKit.Flight(MooringZone);
                var scene=WorldBuilder.Begin(kit.zone);
                var key=GameObject.Find("KeyLight");if(key!=null)key.transform.rotation=Quaternion.Euler(42,38,0);
                var stuff=Materials(kit);
                var arrival=WorldBuilder.Spawn("Arrival",Arrival);arrival.transform.rotation=Quaternion.identity;
                WorldBuilder.Spawn("Gate",Checkpoint).transform.rotation=Quaternion.identity;
                var back=WorldBuilder.Spawn(Continuance.MooringBack,ContinuanceSpawn);back.transform.rotation=Quaternion.Euler(0,180,0);
                kit.DriftingSky();

                // ---- far down: the anchor works, and the pylons that rise out of them to the chains
                Works(kit,stuff);
                int pylons=PylonsUp(kit,stuff);
                // ---- the chains at the plane: drawn, solid, and the Wick-folk's count along them
                int links=Chains(kit,stuff);

                // ---- the convoy, moored on them
                var moored=new List<GameObject>();
                var queue=TheConvoy(kit,stuff,moored);
                var manyHands=ConvoyKit.ManyHands(ManyHandsAt,ManyHandsScale,0);if(manyHands!=null)moored.Add(manyHands);
                MooringLines(kit,stuff,moored);
                Lanterns(kit,stuff);

                // ---- the section struck: Many Hands' galley quarter, its breach and the mats over it
                var (breach,venting,mats)=Section(kit,stuff,manyHands);

                // ---- the islands and the Tollkeeper's arm: berths on foot
                var decks=new List<(GameObject root,Vector3 foot,Vector3[] reach)>();
                foreach(var island in Islands)decks.Add(IslandWorks(kit,stuff,island));
                var arm=TheArm(kit,stuff);decks.Add(arm);
                BakeDecks(decks);

                // ---- the Tollkeeper: its carrier, its cage, the gate; the Continuance past it
                string tollkeeper=Foe("Tollkeeper",null);
                var (carrier,cageShut,cageOpen,gateWall)=TheGate(kit,stuff,tollkeeper!=null);
                var (dockShown,spine)=Beyond(kit,stuff);

                // ---- the fights
                string interceptor=Foe(Enemies.Interceptor,"Interceptor");
                var lane1=kit.FlightFight("Mooring_Lane1",Lane1,LaneFightSize,(interceptor,2));lane1.spawners[0].transform.position=Lane1+new Vector3(0,0,14);lane1.spawners[0].radius=4;
                var lane2=kit.FlightFight("Mooring_Lane2",Lane2,LaneFightSize,(interceptor,2));lane2.spawners[0].transform.position=Lane2+new Vector3(0,0,14);lane2.spawners[0].radius=4;
                string gun=Foe(Enemies.Gun,"FlakGate");
                var gap=WorldBuilder.Encounter("Mooring_Gap",GapCentre,GapSize,gun,1);var west=gap.spawners[0];west.transform.position=GunWest;west.radius=0;
                gap.spawners=new[]{west,ZoneKit.Spawn(gap,gun,1,GunEast,0)};gap.checkpointSpawn="";
                // Crossed: each turned toward the other, so behind one is in front of the other and there is no lee in the gap (the roster's own pair).
                gap.spawners[0].faceAsPlaced=true;gap.spawners[0].transform.rotation=Quaternion.Euler(0,90,0);gap.spawners[1].faceAsPlaced=true;gap.spawners[1].transform.rotation=Quaternion.Euler(0,270,0);
                string warden=Foe(Enemies.Warden,"CompactWarden");
                var held=WorldBuilder.Encounter("Mooring_Island",IslandFightCentre,IslandFightSize,warden,2);held.checkpointSpawn="";
                var post=held.spawners[0];post.transform.position=Islands[1].At(0,4.6f);post.radius=2.2f;post.faceAsPlaced=true;post.transform.rotation=Quaternion.Euler(0,180,0);
                var door=Door(tollkeeper);

                // ---- what is said
                // The section struck: crossing the arena past chain B's gap sets it (the lead's sectionhit bark as it is struck), and Maru answers from the hull a few seconds on, once the bark
                // has been read (an after-talk: the bark first, as the wiring's row asks).
                var struck=kit.Crossing("Many Hands is struck",new Vector3(Crossing.x,0,Crossing.z),CrossingSize,"end.sectionStruck","","Maru");struck.situation="sectionhit";AtPlane(struck);
                var maru=new GameObject("Maru: the section holds",typeof(AfterTalk)).GetComponent<AfterTalk>();maru.transform.position=Galley;
                maru.flag="end.sectionStruck";maru.node="MaruSectionHeld";maru.speaker="Maru";maru.heardFlag="end.sectionHeld";maru.delay=3.8f;maru.range=0;
                var mended=new GameObject("Maru on the radio: hail.mats",typeof(RadioHail)).GetComponent<RadioHail>();mended.transform.position=MatsHail;
                mended.speaker="Maru";mended.situations=new[]{"hail.mats"};mended.needs=new[]{"end.sectionHeld"};mended.heardFlag="end.matsHeard";mended.silencedBy="";mended.range=14;
                var gateTalk=new GameObject("Orrin: the gate is open",typeof(AfterTalk)).GetComponent<AfterTalk>();gateTalk.transform.position=TollkeeperAt;
                gateTalk.flag="bossdown.Tollkeeper";gateTalk.node="OrrinGateOpen";gateTalk.speaker="Orrin";gateTalk.heardFlag="end.gateOpen";gateTalk.delay=4;gateTalk.range=0;

                // ---- the ways out
                var home=WorldBuilder.Warp("Warp — The Hull of Many Hands",HomeWarp,Hull,"",MooringSpawn);home.lockedPrompt="";home.range=WorldBuilder.FlightDockRange;
                WorldBuilder.Label("Warp — The Hull of Many Hands",HomeWarp+new Vector3(0,-.7f,-5),2.2f);
                // Its fight holds the way home while it is under way (the warp is far south of the gate, past the fight's reach, and a ship flown back to it mid-fight left with
                // nothing said: QaPlanSystemsTests.NoWayOutAnswersInEveryLaterBossFight). Open before it and after it, as built.
                if(door!=null){var holds=new GameObject("Its fight holds the way home",typeof(FightHoldsTheWay)).GetComponent<FightHoldsTheWay>();holds.fight=door;holds.way=home;
                    holds.until="bossdown.Tollkeeper";holds.saying="Stop the Tollkeeper's gate motor first: the convoy is still under its guns";}

                // ---- before and after
                Gate("Struck: the breach in the galley quarter",true,new[]{breach},"end.sectionStruck");
                Gate("Held: the venting stops",false,new[]{venting},"end.sectionHeld");
                Gate("Held: the mats over the breach",true,new[]{mats},"end.sectionHeld");
                Gate("Until the motor dies: the gate",false,new[]{gateWall},"bossdown.Tollkeeper");
                Gate("Once the motor dies: the cage hangs open",true,new[]{cageOpen},"bossdown.Tollkeeper");
                if(tollkeeper!=null)Gate("Once the motor dies: the carrier, as it stays",true,new[]{carrier},"bossdown.Tollkeeper");
                Gate("Once the gate is open: the dock",true,dockShown,"end.gateOpen");
                foreach(var (hull,after,seconds,path) in queue)
                {
                    var leave=hull.AddComponent<LeavesAlong>();leave.flag="bossdown.Tollkeeper";leave.after=after;leave.seconds=seconds;leave.path=path;
                    foreach(var t in hull.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=false;
                }
                // What starts unseen is saved switched off; each gate shows it when its flag says (the stills see the zone as it is met).
                foreach(var g in new[]{breach,mats,cageOpen})if(g!=null)g.SetActive(false);

                WorldBuilder.Save(scene,kit.zone);
                Debug.Log("MOORING_COORDS LastMooring arrival="+Arrival+" home="+HomeWarp+" gate="+Checkpoint+" continuance="+ContinuanceSpawn+" dock="+DockAt+" crossing="+Crossing+" galley="+Galley
                    +" many-hands="+ManyHandsAt+" lanes="+Lane1+"/"+Lane2+" gap="+GapCentre+" guns="+GunWest+"/"+GunEast+" door="+DoorCentre+"x"+DoorSize+" tollkeeper="+TollkeeperAt
                    +" arm="+ArmOut+" pad="+ArmPad+" foot="+ArmFoot+" launch="+ArmLaunch+" motor="+ArmMotor+" flight="+ArmFlight
                    +" islands="+string.Join(" ",Islands.Select(i=>i.index+":pad"+i.pad+"/foot"+i.foot+"/launch"+i.launch+"/flight"+i.flight+"/back"+i.back))
                    +" pylons="+pylons+" links="+links+" door-built="+(door!=null)+" spine="+(spine!=null));
                Debug.Log("LAST_MOORING_OK");
                return true;
            }

            // ---------------------------------------------------------------- the anchor works, far down
            /// <summary>The floor of every frame: the Compact's anchor works about 135 m under the plane, dark plates on a 44 m grid at three levels, wells left open between them onto space,
            /// girders along the grid, machine houses on the plates and their cold lamps, fogged toward the haze. Drawn only. The first build's were white at 105 m and every frame was a lit
            /// white grid that drowned the convoy over it: the works are the night under the convoy, not its subject.</summary>
            static void Works(ZoneKit kit,Stuff stuff)
            {
                var s=new ZoneKit.Shape();var rng=new System.Random(1046);const float Cell=44,Gap=4;int plates=0;
                for(float x=-176;x<176;x+=Cell)for(float z=-70;z<800;z+=Cell)
                {
                    float cx=x+Cell*.5f,cz=z+Cell*.5f,roll=(float)rng.NextDouble(),tier=WorksY-(roll<.45f?0:roll<.8f?7:14);int seed=rng.Next();
                    if(roll>.72f)continue;
                    plates++;var r=new System.Random(seed);
                    s.Slot(1).Box(new Vector3(cx,tier-1.2f,cz),new Vector3(Cell-Gap,2.4f,Cell-Gap),Quaternion.identity,Iron,Grey,.12f);
                    // A teal seam across it and its edge picked out, so a plate reads as a deck from 150 m.
                    s.Slot(1).Box(new Vector3(cx,tier+.03f,cz),new Vector3(Cell-Gap-3,.08f,.9f),Quaternion.identity,Teal);
                    s.Slot(1).Box(new Vector3(cx,tier+.03f,cz),new Vector3(.9f,.08f,Cell-Gap-3),Quaternion.identity,Teal);
                    int houses=1+r.Next(4);
                    for(int k=0;k<houses;k++)
                    {
                        var size=new Vector3(5+(float)r.NextDouble()*12,3+(float)r.NextDouble()*9,5+(float)r.NextDouble()*12);
                        var at=new Vector3(cx+((float)r.NextDouble()-.5f)*(Cell-Gap-size.x),tier+size.y*.5f,cz+((float)r.NextDouble()-.5f)*(Cell-Gap-size.z));
                        s.Slot(1).Box(at,size,Quaternion.identity,k%2==0?Grey:Iron,Pale,.25f);
                        s.Slot(2).Box(at+new Vector3(0,size.y*.5f+.3f,0),new Vector3(1.4f,.5f,1.4f),Quaternion.identity,Cyan);
                        s.Slot(2).Box(at+new Vector3(0,0,-size.z*.5f-.05f),new Vector3(size.x*.7f,.5f,.1f),Quaternion.identity,Cyan);
                    }
                }
                // Girders every other grid line, dark: pale, on every line, they ruled every frame like squared paper.
                for(float x=-176;x<=176;x+=Cell*2)s.Slot(1).Beam(new Vector3(x,WorksY+1.4f,-70),new Vector3(x,WorksY+1.4f,800),2.2f,2.2f,Grey);
                for(float z=-70;z<=800;z+=Cell*2)s.Slot(1).Beam(new Vector3(-176,WorksY+1.4f,z),new Vector3(176,WorksY+1.4f,z),2.2f,2.2f,Grey);
                var built=kit.Built("The anchor works",s,stuff.works,stuff.worksDark,stuff.glow);built.isStatic=true;
                built.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                Debug.Log("MOORING_WORKS plates="+plates);
            }
            /// <summary>Where the chains are held (<see cref="Pylons"/>): out of the works a white pylon with teal bands and a drum at its foot, or under a cut end a short drum; an anchor
            /// clamp's head gripping the chain at the plane and a cold lamp on it; solid at the plane. Returns how many.</summary>
            static int PylonsUp(ZoneKit kit,Stuff stuff)
            {
                var s=new ZoneKit.Shape();var root=new GameObject("The pylons");int n=0;
                foreach(var (p,deep) in Pylons())
                {
                    // The cage's hinges stand at the gate flanks' ends: the fight draws them, and anything of the zone's there lay over its bars (only the solid stays).
                    bool hinge=Mathf.Abs(Mathf.Abs(p.x)-GateHalf)<1&&Mathf.Abs(p.y-GateZ)<1;
                    if(hinge){var post=new GameObject("Pylon (the cage's hinge)",typeof(BoxCollider));post.isStatic=true;post.transform.SetParent(root.transform,true);post.transform.position=new Vector3(p.x,Plane,p.y);post.GetComponent<BoxCollider>().size=new Vector3(PylonRadius*2,12,PylonRadius*2);n++;continue;}
                    // The shafts dark, so a pylon is a line in the frame and not a white column across it (the second build's gap posts were); its head white, where the chain is held.
                    float foot=deep?WorksY-2:Plane-8;
                    s.Slot(1).Tube(new[]{new Vector3(p.x,foot,p.y),new Vector3(p.x,Plane-2.4f,p.y)},new[]{deep?1.8f:.9f,1.8f},10,deep?Grey:Iron,deep?Pale:Grey,1,.2f);
                    for(float y=foot+10;y<Plane-6;y+=13)s.Slot(1).Tube(new[]{new Vector3(p.x,y,p.y),new Vector3(p.x,y+1.4f,p.y)},new[]{2.1f,2.1f},10,Teal,Teal,1,.25f);
                    if(deep)
                    {
                        s.Slot(1).Tube(new[]{new Vector3(p.x,WorksY-1,p.y),new Vector3(p.x,WorksY+5,p.y)},new[]{7f,6f},14,Grey,Pale,1,.15f);
                        s.Slot(0).Tube(new[]{new Vector3(p.x,Plane-3.4f,p.y),new Vector3(p.x,Plane+.5f,p.y)},new[]{PylonRadius,PylonRadius},10,White,White,1,.25f);
                        s.Slot(1).Tube(new[]{new Vector3(p.x,Plane-.2f,p.y),new Vector3(p.x,Plane+.45f,p.y)},new[]{PylonRadius+.18f,PylonRadius+.18f},10,Teal,Teal,1,.25f);
                        s.Slot(2).Box(new Vector3(p.x,Plane+.75f,p.y),new Vector3(.8f,.5f,.8f),Quaternion.identity,Cyan);
                        continue;
                    }
                    // A gap's edge (or an arena chain's cut end): the clamp head gripping the chain's end in dark iron, a teal band low on it and its rim lit cold, so the way through is
                    // marked by a ring of light and not by a white post (the look pass of November 14: the gap posts were the brightest, heaviest thing in every gap's frame). As wide as its
                    // solid, so what stops a ship is still what it sees.
                    var c=new Vector3(p.x,0,p.y);
                    s.Slot(1).Tube(new[]{c+Vector3.up*(Plane-3.4f),c+Vector3.up*(Plane+.3f)},new[]{PylonRadius*.9f,PylonRadius},14,Iron,Grey,1,.25f);
                    s.Slot(1).Tube(new[]{c+Vector3.up*(Plane-1.4f),c+Vector3.up*(Plane-.9f)},new[]{PylonRadius+.12f,PylonRadius+.12f},14,Teal,Teal,1,.25f);
                    for(int k=0;k<8;k++){float a=k*45*Mathf.Deg2Rad;s.Slot(1).Box(c+new Vector3(Mathf.Sin(a)*(PylonRadius+.04f),Plane-.4f,Mathf.Cos(a)*(PylonRadius+.04f)),new Vector3(.36f,1.1f,.36f),Quaternion.Euler(0,k*45,0),L(.38f,.4f,.44f));}
                    s.Slot(1).Disc(c+Vector3.up*(Plane+.31f),Vector3.up,PylonRadius,24,L(.3f,.31f,.35f),0,null);
                    s.Slot(2).Disc(c+Vector3.up*(Plane+.34f),Vector3.up,PylonRadius-.14f,24,Cyan,PylonRadius-.52f);
                    s.Slot(2).Box(c+Vector3.up*(Plane+.6f),new Vector3(.5f,.5f,.5f),Quaternion.identity,Cyan);
                    var solid=new GameObject("Pylon",typeof(BoxCollider));solid.isStatic=true;solid.transform.SetParent(root.transform,true);solid.transform.position=new Vector3(p.x,Plane,p.y);
                    solid.GetComponent<BoxCollider>().size=new Vector3(PylonRadius*2,12,PylonRadius*2);n++;
                }
                var built=kit.Built("The pylons, drawn",s,stuff.works,stuff.teal,stuff.glow);built.transform.SetParent(root.transform,true);built.isStatic=true;
                // The colliders stand under what is drawn, so the invisible-wall check sees a pylon over each.
                foreach(Transform t in root.transform.Cast<Transform>().ToArray())if(t!=built.transform)t.SetParent(built.transform,true);
                return n;
            }
            /// <summary>Every chain run drawn in Stillwater's own chain (<c>AnchorChain</c>, end to end at its taken-in size, its top <see cref="ChainOver"/> over the plane) and solid at the plane;
            /// along the top of each run the Wick-folk's count in warm lamps, two short and one long (Pell: "Two short, one long, at every turn"). Returns the pieces laid.</summary>
            static int Chains(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject("Stillwater's chains");var beads=new ZoneKit.Shape();int pieces=0;
                var prefab=ZoneKit.Prefab("Environment","AnchorChain");float native=30,thick=2.4f;bool alongX=false;
                if(prefab!=null)
                {
                    var probe=(GameObject)PrefabUtility.InstantiatePrefab(prefab);var b=ModelGeometry.BoundsOf(probe);alongX=b.size.x>=b.size.z;native=Mathf.Max(b.size.x,b.size.z);thick=b.size.y;
                    UnityEngine.Object.DestroyImmediate(probe);Debug.Log("MOORING_CHAIN native="+b.size);
                }
                var fallback=new ZoneKit.Shape();
                foreach(var (name,a,b) in Runs())
                {
                    var run=b-a;float length=run.magnitude;var dir=new Vector3(run.x,0,run.y)/length;float yaw=Mathf.Atan2(run.x,run.y)*Mathf.Rad2Deg;
                    var wall=new GameObject(name,typeof(BoxCollider));wall.isStatic=true;wall.transform.SetParent(root.transform,true);
                    wall.transform.SetPositionAndRotation(new Vector3((a.x+b.x)*.5f,Plane,(a.y+b.y)*.5f),Quaternion.Euler(0,yaw,0));wall.GetComponent<BoxCollider>().size=new Vector3(ChainWide,12,length+.4f);
                    int count=Mathf.Max(1,Mathf.CeilToInt(length/(native*ChainScale)));float each=length/count,scale=each/native;
                    for(int k=0;k<count;k++)
                    {
                        var mid=new Vector3(a.x,0,a.y)+dir*(each*(k+.5f));
                        if(prefab!=null)
                        {
                            var piece=(GameObject)PrefabUtility.InstantiatePrefab(prefab);piece.name="Chain";
                            foreach(var c in piece.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
                            piece.transform.localScale*=scale;piece.transform.rotation=Quaternion.Euler(k%2==0?0:180,alongX?yaw-90:yaw,0);
                            var box=ZoneKit.Drawn(piece);piece.transform.position+=new Vector3(mid.x-box.center.x,Plane+ChainOver-box.max.y,mid.z-box.center.z);
                            piece.transform.SetParent(wall.transform,true);piece.isStatic=true;foreach(var t in piece.GetComponentsInChildren<Transform>())t.gameObject.isStatic=true;
                        }
                        else fallback.Slot(0).Tube(new[]{mid-dir*each*.5f+Vector3.up*(Plane-.3f),mid+dir*each*.5f+Vector3.up*(Plane-.3f)},new[]{1.1f,1.1f},8,White,Pale,1,.25f);
                        pieces++;
                    }
                    // The count along its top: two short and one long, every 11 m.
                    for(float d=4;d<length-4;d+=11)
                    {
                        var at=new Vector3(a.x,Plane+ChainOver+.15f,a.y)+dir*d;
                        beads.Box(at,new Vector3(.45f,.3f,.45f),Quaternion.Euler(0,yaw,0),Warm);beads.Box(at+dir*1.3f,new Vector3(.45f,.3f,.45f),Quaternion.Euler(0,yaw,0),Warm);
                        beads.Box(at+dir*3.4f,new Vector3(.45f,.3f,1.8f),Quaternion.Euler(0,yaw,0),Warm);
                    }
                }
                if(!fallback.Empty){var drawn=kit.Built("Stillwater's chains, drawn",fallback,stuff.cable);drawn.transform.SetParent(root.transform,true);}
                var lit=kit.Built("The Wick-folk's count",beads,stuff.glow);lit.transform.SetParent(root.transform,true);lit.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                return pieces;
            }

            // ---------------------------------------------------------------- the convoy
            /// <summary>Every hull of the convoy, moored, by where it is a place the player knows. Returns the hulls queued at chain D to go through the gate first (the market district at their
            /// head, as Orrin's board has it), each with when it starts, how long it takes and its way through.</summary>
            static List<(GameObject hull,float after,float seconds,Vector3[] path)> TheConvoy(ZoneKit kit,Stuff stuff,List<GameObject> moored)
            {
                var root=new GameObject("The convoy, moored");var queue=new List<(GameObject,float,float,Vector3[])>();
                GameObject H(string key,float x,float z,float top,float yaw,float scale=1){var h=ConvoyKit.Hull(key,x,z,top,yaw,scale,root.transform);if(h!=null)moored.Add(h);return h;}
                // ---- the Halo's districts, three moored side by side under the arrival; the market is at the head of the queue at chain D
                var districts=ConvoyKit.CopyOut(HomeBuilder.RingZone,true,ConvoyKit.Districts.Select(d=>"District: "+d).ToArray());
                void Moor(string name,Vector3 at,float yaw){if(!districts.TryGetValue("District: "+name,out var d))return;d.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));d.transform.SetParent(root.transform,true);moored.Add(d);
                    foreach(var lee in d.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Its lee").ToArray())UnityEngine.Object.DestroyImmediate(lee.gameObject);}
                Moor("the repair quarter",new Vector3(-17,-9,10),0);Moor("the residential crescent",new Vector3(0,-10,16),0);Moor("the tug dock",new Vector3(17,-9,10),0);
                Moor("the market",new Vector3(-16,-8,372),0);
                // Every place is moored in a group of its own kind, near enough the plane to fill the frame it is under (tops 4 to 9 m down, the frame there 35 to 45 m across),
                // so that wherever a ship is in an arena some place the player knows is under it. The first build moored them one by one, and most frames were the works alone.
                // ---- the lee: Tallow's station hull along the west chain, an arc of the Halo's ring under tow on the east, the tugs that tow it
                H("TallowStationHull",-40,34,-7,0);
                foreach(var (x,z,yaw) in new[]{(38f,6f,24f),(47f,32f,4f),(42f,58f,-18f)})H(ConvoyRing(x),x,z,-6,yaw,2);
                H("Skiff",26,40,-5,200,1.4f);H("Skiff",-24,-4,-6,10,1.4f);
                // ---- arena one: the Crawl's treads coupled in a line along the west, a ring segment towed deep under the fight, haulers; Xylos's seed barges moored under chain B
                foreach(var z in new[]{80f,108f,136f})H("CrawlTread",-38,z,-6,0,1.5f);
                H("RingSegmentA",4,92,-18,60,2.4f);H("Skiff",14,76,-12,30,1.4f);
                H("Hauler",20,104,-9,75,1.6f);H("Hauler",38,84,-7,-20,1.6f);H("Hauler",-8,138,-10,-60,1.6f);
                foreach(var x in new[]{36f,46f,56f})H("SeedBarge",x,146,-5,0,1.5f);
                foreach(var x in new[]{-16f,-28f})H("SeedBarge",x,150,-6,0,1.5f);
                // ---- the moorings (arena two): Nacre's navigators in a line on the east, Teo's carriers between the hulls
                foreach(var z in new[]{176f,194f,212f})H("PatrolCutter",44,z,-6,0,1.5f);
                H("Skiff",30,226,-5,250,1.4f);H("Skiff",-30,254,-6,80,1.4f);H("Skiff",8,252,-7,140,1.4f);
                // ---- chain C: a seed barge either side of the guns' gap
                H("SeedBarge",-46,262,-6,90,1.5f);H("SeedBarge",4,262,-6,90,1.5f);
                // ---- arena three: Driftreed's raft bay lashed together on the west, Lastlight's longhouses under their rib arches on the east, haulers deep under the fight, the Crawl
                for(int i=0;i<4;i++)for(int j=0;j<2;j++)H("ReedRaft",-50+i*12.6f,288+j*9,-4.5f-((i+j)%2)*.6f,90,1.6f);
                for(int i=0;i<3;i++)H("ReedRaft",-46+i*12.6f,310,-4.5f-(i%2)*.6f,90,1.6f);
                // Lastlight's longhouses, each with one rib arch at its end (over the roof, from above, an arch read as a hump of ice).
                foreach(var (x,z) in new[]{(28f,292f),(44f,316f),(30f,340f)}){H("KeptLonghouse",x,z,-6,90,1.8f);H("RibArch",x+10,z,-6.5f,90,1.2f);}
                H("Hauler",-6,300,-16,30,1.8f);H("CrawlTread",-40,340,-6,0,1.5f);H("SeedBarge",-24,358,-6,0,1.5f);
                // Lanternwick's float under the second pair's fight: three of its towers on one raft of planks, lit.
                var wick=new Vector3(2,0,334);foreach(var (dx,dz) in new[]{(-5f,-3f),(5f,-3f),(0f,5f)})H("LanternTower",wick.x+dx,wick.z+dz,-3.5f,dx*9,1.1f);
                var deckRoot=new GameObject("Lanternwick's float");deckRoot.transform.SetParent(root.transform,true);var deck=new ZoneKit.Shape();
                deck.Disc(new Vector3(wick.x,-17.6f,wick.z),Vector3.up,10,18,L(.5f,.38f,.24f),0,L(.36f,.27f,.17f));
                var planks=kit.Built("Lanternwick's float, its planks",deck,stuff.cable);planks.transform.SetParent(deckRoot.transform,true);
                foreach(var (dx,dz) in new[]{(-5f,-3f),(5f,-3f),(0f,5f)}){var l=kit.Lamp(new Vector3(wick.x+dx,-5.5f,wick.z+dz),14,2.2f,new Color(1f,.76f,.42f));l.transform.SetParent(deckRoot.transform,true);}
                // ---- the queue at chain D: the market district, a seed barge and two rafts, going through the gate the moment it opens, under the party, and on past the Continuance
                var barge=H("SeedBarge",20,370,-6,0,1.5f);var raft1=H("ReedRaft",34,366,-5,0,1.6f);var raft2=H("ReedRaft",34,378,-5,0,1.6f);
                // Each at the height it was moored at (its own pivot's), over the carrier and the Continuance (both deeper than any of them reaches), and on out of the far plane.
                Vector3[] Through(GameObject h){var p=h.transform.position;return new[]{new Vector3(p.x*.5f,p.y,420),new Vector3(p.x*.15f,p.y,470),new Vector3(p.x*.15f,p.y,530),new Vector3(p.x*.3f,p.y-2,660),new Vector3(p.x*.3f,p.y-10,840)};}
                if(districts.TryGetValue("District: the market",out var market))queue.Add((market,1.5f,34,Through(market)));
                if(barge!=null)queue.Add((barge,5,36,Through(barge)));if(raft1!=null)queue.Add((raft1,8,38,Through(raft1)));if(raft2!=null)queue.Add((raft2,10,38,Through(raft2)));
                // What goes through is cast off already (no line to the chain) and carries no light of the convoy's static ones: they would stay behind it.
                foreach(var q in queue)moored.Remove(q.Item1);
                // Running lights on every hull: warm at the bow, cold at the stern.
                var lights=new ZoneKit.Shape();var rng=new System.Random(77);
                foreach(var hull in moored)
                {
                    if(hull.name.StartsWith("District"))continue;var box=ZoneKit.Drawn(hull);
                    for(int j=0;j<3;j++){var p=new Vector3(box.center.x+((float)rng.NextDouble()-.5f)*box.size.x*.8f,box.max.y+.2f,box.center.z+((float)rng.NextDouble()-.5f)*box.size.z*.8f);
                        lights.Box(p,Vector3.one*(.5f+Mathf.Min(box.size.x,box.size.z)*.02f),Quaternion.identity,j==0?Warm:Cyan);}
                }
                var lit=kit.Built("The convoy's running lights",lights,stuff.glow);lit.transform.SetParent(root.transform,true);lit.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                Debug.Log("MOORING_CONVOY hulls="+moored.Count+" districts="+districts.Count+" queue="+queue.Count);
                return queue;
            }
            /// <summary>The ring's two segment kinds in turn along an arc.</summary>
            static string ConvoyRing(float x)=>((int)x)%2==0?"RingSegmentA":"RingSegmentB";
            /// <summary>A mooring line from each hull's top up to the chain over it (or the nearest, within 40 m), so every hull is seen strung on the chains.</summary>
            static void MooringLines(ZoneKit kit,Stuff stuff,List<GameObject> moored)
            {
                var s=new ZoneKit.Shape();int lines=0;
                foreach(var hull in moored)
                {
                    if(hull==null)continue;var box=ZoneKit.Drawn(hull);if(box.max.y>Plane-2)continue;
                    foreach(float side in new[]{-.3f,.3f})
                    {
                        var from=new Vector3(box.center.x+side*box.size.x*.6f,box.max.y-.2f,box.center.z);var chain=NearestChain(new Vector2(from.x,from.z),out float far);if(far>40)continue;
                        var to=new Vector3(chain.x,Plane-.6f,chain.y);s.Slot(0).Rope(from,to,1.2f,.16f,L(.36f,.37f,.4f),5,10);lines++;
                    }
                }
                var built=kit.Built("Mooring lines",s,stuff.cable);built.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                Debug.Log("MOORING_LINES "+lines);
            }
            /// <summary>Lanternwick's towers at every gap: one each side, standing on a float under the plane, its lamp lit, so a way through a chain line is the lit place in the frame.</summary>
            static void Lanterns(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject("The lantern towers at the gaps");var floats=new ZoneKit.Shape();
                foreach(var l in Lines)foreach(float x in new[]{l.gapFrom-5,l.gapTo+5})
                {
                    var tower=ConvoyKit.Hull("LanternTower",x,l.z-6,-2.2f,x<l.GapMiddle?30:-30,1,root.transform);if(tower==null)continue;var box=ZoneKit.Drawn(tower);
                    floats.Slot(0).Tube(new[]{new Vector3(x,box.min.y-1.2f,l.z-6),new Vector3(x,box.min.y+.2f,l.z-6)},new[]{3.2f,3.4f},12,L(.46f,.36f,.24f),L(.62f,.5f,.32f),1,.25f);
                    var lamp=kit.Lamp(new Vector3(x,box.max.y-1.5f,l.z-6),18,2.6f,new Color(1f,.76f,.42f));lamp.transform.SetParent(root.transform,true);
                    Halo(stuff.lantern,new Vector3(x,box.max.y-.8f,l.z-6),7,root.transform);
                }
                var built=kit.Built("The lantern floats",floats,stuff.cable);built.transform.SetParent(root.transform,true);
            }
            /// <summary>A soft glow lying flat at a point: a lamp seen from above.</summary>
            static void Halo(Material glow,Vector3 at,float size,Transform parent)
            {
                var halo=GameObject.CreatePrimitive(PrimitiveType.Quad);UnityEngine.Object.DestroyImmediate(halo.GetComponent<Collider>());halo.name="Glow";halo.transform.SetParent(parent,true);
                halo.transform.SetPositionAndRotation(at,Quaternion.Euler(90,0,0));halo.transform.localScale=Vector3.one*size;
                var view=halo.GetComponent<Renderer>();view.sharedMaterial=glow;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
            }

            // ---------------------------------------------------------------- the section struck
            /// <summary>Many Hands' galley quarter struck (shown from <c>end.sectionStruck</c>): a scorched hole in its roof with the plates torn up round it and a fire in it; air venting out of it
            /// (hidden from <c>end.sectionHeld</c>); and Driftreed's mats laid over it rough side out with two weavers on them (shown from <c>end.sectionHeld</c>). At the roof's own height.</summary>
            static (GameObject breach,GameObject venting,GameObject mats) Section(ZoneKit kit,Stuff stuff,GameObject manyHands)
            {
                // The roof's height where the galley is: the highest drawn thing over that point of the plan.
                float roof=Galley.y;
                if(manyHands!=null){float best=float.NegativeInfinity;foreach(var r in manyHands.GetComponentsInChildren<Renderer>()){var b=r.bounds;if(b.size.y<6&&b.min.x<Galley.x&&b.max.x>Galley.x&&b.min.z<Galley.z&&b.max.z>Galley.z)best=Mathf.Max(best,b.max.y);}if(best>float.NegativeInfinity)roof=best;}
                var at=new Vector3(Galley.x,roof,Galley.z);Debug.Log("MOORING_GALLEY roof="+at);
                var breach=new GameObject("The galley quarter's breach");breach.transform.position=at;
                var s=new ZoneKit.Shape();var rng=new System.Random(186);
                s.Slot(0).Disc(at+Vector3.up*.08f,Vector3.up,3.4f,18,L(.08f,.07f,.07f),0,L(.2f,.16f,.14f));
                for(int k=0;k<9;k++){float a=k*40+rng.Next(-12,12);var dir=Quaternion.Euler(0,a,0)*Vector3.forward;
                    s.Slot(0).Box(at+dir*(3+(float)rng.NextDouble())+Vector3.up*.4f,new Vector3(1.6f,.12f,1.1f),Quaternion.Euler(-35-rng.Next(30),a,rng.Next(-20,20)),L(.4f,.42f,.48f));}
                s.Slot(1).Disc(at+Vector3.up*.12f,Vector3.up,1.8f,14,new Color(1f,.55f,.2f));
                var hole=kit.Built("The breach",s,stuff.scorch,stuff.glow);hole.transform.SetParent(breach.transform,true);
                var fire=kit.Lamp(at+Vector3.up*2,16,3.2f,new Color(1f,.55f,.25f));fire.transform.SetParent(breach.transform,true);
                var venting=new GameObject("The air going");venting.transform.SetParent(breach.transform,false);venting.transform.position=at;
                var vapour=kit.Drift("Venting air",at+Vector3.up*5,new Vector3(3,10,3),stuff.vapour,new Color(.9f,.95f,1f,.5f),120,.8f,2.6f,3.2f,3);vapour.transform.SetParent(venting.transform,true);
                var sparks=kit.Drift("Sparks",at+Vector3.up*2,new Vector3(4,4,4),stuff.lantern,new Color(1f,.7f,.3f,.9f),60,.12f,.3f,2.4f,1.4f);sparks.transform.SetParent(venting.transform,true);
                var mats=new GameObject("Driftreed's mats over the breach");mats.transform.position=at;var m=new ZoneKit.Shape();
                for(int k=0;k<4;k++){float a=k*47+15;var c=at+Quaternion.Euler(0,a,0)*Vector3.forward*1.2f+Vector3.up*(.16f+k*.03f);
                    m.Box(c,new Vector3(3.6f,.06f,2.8f),Quaternion.Euler(0,a,0),k%2==0?Color.white:L(.86f,.92f,.72f));}
                var laid=kit.Built("The mats",m,stuff.mats);laid.transform.SetParent(mats.transform,true);
                foreach(var (body,dx,dz,yaw) in new[]{("MaruBody",3.4f,.6f,250f),("Maru",-3f,-1.2f,80f)})
                {
                    var prefab=ZoneKit.Prefab("Characters",body);if(prefab==null)continue;
                    var weaver=(GameObject)PrefabUtility.InstantiatePrefab(prefab);PrefabUtility.UnpackPrefabInstance(weaver,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                    foreach(var c in weaver.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);foreach(var b in weaver.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null)UnityEngine.Object.DestroyImmediate(b);
                    foreach(var a in weaver.GetComponentsInChildren<Animator>())a.cullingMode=AnimatorCullingMode.CullCompletely;foreach(var sk in weaver.GetComponentsInChildren<SkinnedMeshRenderer>())sk.updateWhenOffscreen=true;
                    weaver.name="A weaver on the mats";weaver.transform.SetPositionAndRotation(at+new Vector3(dx,.2f,dz),Quaternion.Euler(0,yaw,0));weaver.transform.SetParent(mats.transform,true);break;
                }
                return (breach,venting,mats);
            }

            // ---------------------------------------------------------------- the islands and the arm (berths)
            /// <summary>One maintenance island: a Compact deck with a low rail round it, its pontoon under it and a tether to the chain, a work hut and a winch on its north edge, a lamp, the pad's
            /// ring; island 1's free cradle; its berth. Returns its root, its foot spawn and what must be walked to on it.</summary>
            static (GameObject root,Vector3 foot,Vector3[] reach) IslandWorks(ZoneKit kit,Stuff stuff,Island i)
            {
                var root=new GameObject("Island "+i.index+": "+i.name);float hx=i.size.x*.5f,hz=i.size.z*.5f,top=ShelfY;
                var s=new ZoneKit.Shape();
                s.Slot(0).Box(i.At(0,0,top-.3f),new Vector3(i.size.x,.6f,i.size.z),Quaternion.identity,White,White);
                for(float x=-hx+2;x<hx-1;x+=2.8f)s.Slot(1).Box(i.At(x,0,top+.01f),new Vector3(.08f,.02f,i.size.z-.4f),Quaternion.identity,Grey);
                s.Slot(1).Disc(new Vector3(i.pad.x,top+.03f,i.pad.z),Vector3.up,3.3f,28,Amber,2.8f,Amber);
                var corners=new[]{i.At(-hx,-hz,top),i.At(hx,-hz,top),i.At(hx,hz,top),i.At(-hx,hz,top)};
                for(int k=0;k<4;k++)
                {
                    var a=corners[k];var b=corners[(k+1)%4];s.Slot(1).Beam(a+Vector3.up*.5f,b+Vector3.up*.5f,.16f,.16f,Teal);
                    for(float t=0;t<=1.001f;t+=1/Mathf.Ceil(Vector3.Distance(a,b)/2.4f))s.Slot(1).Box(Vector3.Lerp(a,b,t)+Vector3.up*.25f,new Vector3(.16f,.5f,.16f),Quaternion.identity,Grey);
                }
                // The tool locker on the north edge and a winch drum beside it: low, so a ship flying over the island meets nothing (its hull reaches y -.37; the first build's 2.2 m
                // hut stood to y .2 and LastMooringZoneTests found a hull striking it over both islands).
                var hut=i.At(-hx+3.2f,hz-1.6f,top+.7f);s.Slot(0).Box(hut,new Vector3(4.4f,1.4f,2.4f),Quaternion.identity,White,Pale);
                s.Slot(2).Box(hut+new Vector3(0,.3f,-1.22f),new Vector3(2.6f,.7f,.05f),Quaternion.identity,Warm);
                var drum=i.At(hx-3,hz-1.2f,top+.6f);s.Slot(1).Tube(new[]{drum+Vector3.left*1.2f,drum+Vector3.right*1.2f},new[]{.55f,.55f},10,Iron,Iron,1,.3f);
                // Under it: its pontoon, and its tether up to the nearest chain.
                s.Slot(1).Box(i.At(0,0,top-2.6f),new Vector3(i.size.x-2,4,i.size.z-2),Quaternion.identity,Iron,Grey);
                var chain=NearestChain(new Vector2(i.middle.x,i.middle.z),out _);s.Slot(1).Rope(i.At(0,0,top-3),new Vector3(chain.x,Plane-.6f,chain.y),2,.22f,Grey,6,10);
                var built=kit.Built("Island "+i.index+" deck",s,stuff.works,stuff.teal,stuff.glow);built.transform.SetParent(root.transform,true);
                ZoneKit.Block(built,i.At(0,0,top-.3f),new Vector3(i.size.x,.6f,i.size.z),0,"Island floor");
                for(int k=0;k<4;k++)ZoneKit.BlockAlong(built,corners[k]+Vector3.up*.25f,corners[(k+1)%4]+Vector3.up*.25f,.3f,.5f,"Island rail");
                ZoneKit.Block(built,hut,new Vector3(4.4f,1.4f,2.4f),0,"The tool locker");ZoneKit.Block(built,drum,new Vector3(2.6f,1.2f,1.2f),0,"The winch");
                var lamp=kit.Lamp(i.At(0,hz-2,top+3.2f),13,1.8f,new Color(.7f,.92f,1f));lamp.transform.SetParent(root.transform,true);
                var reach=new List<Vector3>{i.launch,i.middle};
                if(i.index==1)
                {
                    // The free cradle: a console on the north edge, saving where a save on a shelf may stand (its berth's return spawn: rule 2).
                    var console=kit.Prop(ZoneKit.Kit("DeckConsole","LandingPad"),new Vector3(Cradle.x,top,Cradle.z),1.4f,180,true,false);console.transform.SetParent(root.transform,true);console.name="The cradle";
                    var bay=console.AddComponent<RepairBay>();bay.free=true;bay.saveSpawn=i.returnSpawn;bay.prompt="Cradle — repair and save";bay.range=2.4f;
                    reach.Add(Cradle+Vector3.back*1.6f);
                }
                // What a maintenance island is for: work lamps, a cable drum, pallets and crates by the edges, and on the first Hal's anvil beside the cradle (his people keep it).
                var props=new List<(string key,float x,float z,float h,float yaw)>{("DeckCrate",hx-2.2f,-hz+3.4f,1f,20),("DeckCrate",hx-3.4f,-hz+3f,.8f,70),("DeckCrate",-hx+6.2f,hz-1.4f,.8f,10),
                    ("DressWorkLamp",hx-1.2f,hz-1.2f,1.4f,200),("DressWorkLamp",-hx+1.2f,-hz+1.4f,1.4f,30),("DressCableDrum",hx-2f,0,1f,90),("DressCargoPallet",-hx+2.4f,0,.7f,0)};
                if(i.index==1)props.Add(("LatticeAnvil",6.2f,3.6f,1.1f,190));else props.Add(("DeckConsole",-2.6f,hz-1.4f,1.3f,180));
                foreach(var (k,x,z,h,yaw) in props){if(!ZoneKit.Landed(k))continue;var prop=kit.Prop(k,i.At(x,z),h,yaw,true,false);prop.transform.SetParent(root.transform,true);}
                // The edge the party is set down on, striped amber and dark (where a ship's pad is, the Compact paints a stripe).
                var stripes=new ZoneKit.Shape();for(float x=-hx+.6f;x<hx-.4f;x+=1.2f)stripes.Box(i.At(x+.3f,-hz+.5f,top+.02f),new Vector3(.6f,.03f,.7f),Quaternion.Euler(0,30,0),Amber);
                var striped=kit.Built("Island "+i.index+" edge",stripes,stuff.teal);striped.transform.SetParent(root.transform,true);
                // The deck as its crew keep it (the look pass of November 14: both islands were a plain white deck with a few crates, a Compact platform and not a place anyone works):
                // its plates in tones of their own between the seams, a dark steel walk inside the rail, the landing square under the pad's ring with its corner marks, tie-downs along
                // the walk, worn where the crew go from the locker to the winch; on the first, the hands who keep it at work. All flush or drawn only: nothing solid, nothing in the bake.
                var paint=new ZoneKit.Shape();var prng=new System.Random(700+i.index);
                var cols=new List<float>{-hx};for(float x=-hx+2;x<hx-1;x+=2.8f)cols.Add(x);cols.Add(hx);
                for(int a=0;a+1<cols.Count;a++)for(float z=-hz;z<hz-.01f;z+=2.8f)
                {
                    float roll=(float)prng.NextDouble();if(roll>.42f)continue;float z1=Mathf.Min(z+2.8f,hz);
                    paint.Box(i.At((cols[a]+cols[a+1])*.5f,(z+z1)*.5f,top+.005f),new Vector3(cols[a+1]-cols[a]-.1f,.01f,z1-z-.1f),Quaternion.identity,roll<.14f?L(.56f,.6f,.64f):roll<.28f?L(.66f,.74f,.76f):L(.76f,.78f,.82f));
                }
                for(float z=-hz+2.8f;z<hz-.1f;z+=2.8f)paint.Box(i.At(0,z,top+.011f),new Vector3(i.size.x-.4f,.02f,.08f),Quaternion.identity,Grey);
                var walk=L(.32f,.34f,.38f);
                paint.Box(i.At(0,hz-.75f,top+.009f),new Vector3(i.size.x-.3f,.018f,1.2f),Quaternion.identity,walk);
                foreach(int sx in new[]{-1,1})paint.Box(i.At(sx*(hx-.75f),-.6f,top+.009f),new Vector3(1.2f,.018f,i.size.z-1.5f),Quaternion.identity,walk);
                for(float x=-hx+1.8f;x<hx-1;x+=2.8f)paint.Disc(i.At(x,hz-.75f,top+.022f),Vector3.up,.26f,10,Iron,0,Pale);
                foreach(int sx in new[]{-1,1})for(float z=-hz+2.4f;z<hz-1.6f;z+=2.8f)paint.Disc(i.At(sx*(hx-.75f),z,top+.022f),Vector3.up,.26f,10,Iron,0,Pale);
                var land=new Vector3(i.pad.x,top+.013f,i.pad.z);paint.Box(land,new Vector3(8.4f,.022f,8.4f),Quaternion.identity,L(.2f,.28f,.32f));
                foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
                {
                    var corner=land+new Vector3(sx*3.7f,.013f,sz*3.7f);
                    paint.Box(corner-new Vector3(sx*.6f,0,0),new Vector3(1.4f,.02f,.2f),Quaternion.identity,White);paint.Box(corner-new Vector3(0,0,sz*.6f),new Vector3(.2f,.02f,1.4f),Quaternion.identity,White);
                }
                paint.Box(i.At(-hx+3.2f,hz-3.4f,top+.016f),new Vector3(3.4f,.012f,1.4f),Quaternion.identity,L(.6f,.6f,.58f));
                var painted=kit.Built("Island "+i.index+" deck, as kept",paint,stuff.teal);painted.transform.SetParent(root.transform,true);painted.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                if(i.index==1)
                {
                    // Bent over Hal's anvil and into the open locker: the Compact's coat and a hand's suit (Continuance's crews; drawn only, so a hull passing over meets nobody, and bent so
                    // no head comes within a metre and a half of the plane's hulls).
                    Continuance.Figure(kit,"A hand at the anvil","SurvivorNatural",i.At(7f,2.4f),-40,Continuance.Coats[0],"bend",0,root.transform);
                    Continuance.Figure(kit,"A hand at the open locker","PatrolBody",i.At(-4.6f,2.5f),0,Continuance.Coats[1],"bend",0,root.transform);
                }
                Berth.Declare(i.berth,i.name,ZoneKind.GroundCombat,i.Box,i.pad,i.launch,i.foot,i.flight,i.returnSpawn,i.back,inFight:false,refuseWhileAwake:true,parent:root.transform);
                return (root,i.foot,reach.ToArray());
            }
            /// <summary>The Tollkeeper's arm: its deck out in the lane (dark iron, amber edges, a low rail on three sides), the gate motor's housing on its north end with an empty in front of it
            /// for the fight's motor, the boom back north to the carrier, all under one root the fight drives (wiring 6.1.1: <c>berth.Drive</c>), and its berth. Nothing of it static.</summary>
            static (GameObject root,Vector3 foot,Vector3[] reach) TheArm(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject(ArmRoot);root.transform.position=ArmOut;float hx=ArmBox.size.x*.5f,hz=ArmBox.size.z*.5f,top=ShelfY;var o=ArmOut;
                var s=new ZoneKit.Shape();
                s.Slot(0).Box(new Vector3(o.x,top-.3f,o.z),new Vector3(hx*2,.6f,hz*2),Quaternion.identity,Iron,Grey);
                foreach(int side in new[]{-1,1})s.Slot(1).Box(new Vector3(o.x+side*(hx-.25f),top+.02f,o.z),new Vector3(.5f,.04f,hz*2-.2f),Quaternion.identity,Amber);
                s.Slot(1).Disc(new Vector3(ArmPad.x,top+.04f,ArmPad.z),Vector3.up,2.6f,24,Amber,2.2f,Amber);
                var rails=new[]{(new Vector3(o.x-hx,top,o.z-hz),new Vector3(o.x-hx,top,o.z+hz)),(new Vector3(o.x+hx,top,o.z-hz),new Vector3(o.x+hx,top,o.z+hz)),(new Vector3(o.x-hx,top,o.z-hz),new Vector3(o.x+hx,top,o.z-hz))};
                foreach(var (a,b) in rails)s.Slot(1).Beam(a+Vector3.up*.5f,b+Vector3.up*.5f,.16f,.16f,Grey);
                var housing=new Vector3(ArmMotor.x,top+.7f,o.z+hz-.6f);s.Slot(0).Box(housing,new Vector3(3.6f,1.4f,1.2f),Quaternion.identity,Grey,Iron);
                s.Slot(2).Box(housing+new Vector3(0,.2f,-.62f),new Vector3(2.4f,.5f,.05f),Quaternion.identity,new Color(1f,.5f,.25f));
                // The boom: back north under the carrier, the deck's two beams meeting in its root.
                foreach(int side in new[]{-1,1})s.Slot(0).Beam(new Vector3(o.x+side*2.4f,top-1,o.z+hz),new Vector3(o.x+side*.8f,top-7,o.z+hz+30),.9f,1.1f,Iron);
                var built=kit.Built("The arm",s,stuff.worksDark,stuff.teal,stuff.glow);built.transform.SetParent(root.transform,true);
                ZoneKit.Block(built,new Vector3(o.x,top-.3f,o.z),new Vector3(hx*2,.6f,hz*2),0,"Arm deck");
                foreach(var (a,b) in rails)ZoneKit.BlockAlong(built,a+Vector3.up*.25f,b+Vector3.up*.25f,.3f,.5f,"Arm rail");
                ZoneKit.Block(built,housing,new Vector3(3.6f,1.4f,1.2f),0,"The gate motor's housing");
                var seat=new GameObject(MotorSeat);seat.transform.SetParent(root.transform,true);seat.transform.SetPositionAndRotation(ArmMotor,Quaternion.Euler(0,180,0));
                var lamp=kit.Lamp(housing+new Vector3(0,2.2f,-2),10,1.8f,new Color(1f,.62f,.32f));lamp.transform.SetParent(root.transform,true);
                Berth.Declare(ArmBerth,"The Tollkeeper's arm",ZoneKind.GroundCombat,ArmBox,ArmPad,ArmLaunch,ArmFoot,ArmFlight,ArmReturn,ArmReturnAt,inFight:true,refuseWhileAwake:true,parent:root.transform);
                foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=false;
                return (root,ArmFoot,new[]{ArmLaunch,ArmMotor+Vector3.back*1.2f,ArmOut});
            }
            /// <summary>Every shelf's navigation in one bake from their own floors, rails and works (the plane has nothing a body stands on), each checked from its foot spawn to what must be
            /// walked to on it (wiring 6.1 rule 6).</summary>
            static void BakeDecks(List<(GameObject root,Vector3 foot,Vector3[] reach)> decks)
            {
                Physics.SyncTransforms();var sources=new List<NavMeshBuildSource>();var area=new Bounds(decks[0].foot,Vector3.one);
                foreach(var (root,foot,reach) in decks)
                {
                    area.Encapsulate(foot);foreach(var r in reach)area.Encapsulate(r);
                    foreach(var box in root.GetComponentsInChildren<BoxCollider>())
                        if(box.enabled&&!box.isTrigger&&box.GetComponentInParent<Berth>()==null)
                            sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=box.transform.localToWorldMatrix*Matrix4x4.Translate(box.center),size=box.size,area=0});
                }
                area.Expand(new Vector3(30,8,30));
                var settings=NavMesh.GetSettingsByID(0);settings.agentRadius=.55f;settings.agentHeight=2;settings.agentClimb=.3f;settings.agentSlope=35;settings.overrideVoxelSize=true;settings.voxelSize=.15f;
                var built=NavMeshBuilder.BuildNavMeshData(settings,sources,area,Vector3.zero,Quaternion.identity);
                if(built==null)throw new InvalidOperationException(MooringZone+" shelves' navigation bake failed");
                var instance=NavMesh.AddNavMeshData(built);int points=0;
                try
                {
                    foreach(var (root,foot,reach) in decks)
                    {
                        if(!NavMesh.SamplePosition(foot,out var start,1.5f,NavMesh.AllAreas))throw new InvalidOperationException(MooringZone+": "+root.name+" has no floor at its foot spawn "+foot);
                        foreach(var goal in reach)
                        {
                            var path=new NavMeshPath();
                            if(!NavMesh.SamplePosition(goal,out var sampled,1.5f,NavMesh.AllAreas)||!NavMesh.CalculatePath(start.position,sampled.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)
                                throw new InvalidOperationException(MooringZone+": "+root.name+" can't be walked from its foot spawn to "+goal);
                            points++;
                        }
                    }
                    Debug.Log("ZONE_ROUTE_BAKE_OK "+MooringZone+" points="+points);
                }
                finally{instance.Remove();}
                string file="Assets/_Project/Scenes/"+MooringZone+"-Navigation.asset";var previous=AssetDatabase.LoadAssetAtPath<NavMeshData>(file);
                if(previous==null){AssetDatabase.CreateAsset(built,file);previous=built;}else{EditorUtility.CopySerialized(built,previous);UnityEngine.Object.DestroyImmediate(built);EditorUtility.SetDirty(previous);}
                new GameObject("Walkable paths",typeof(GroundNavigation)).GetComponent<GroundNavigation>().data=previous;
            }

            // ---------------------------------------------------------------- the gate
            /// <summary>The Tollkeeper's carrier across the way north of the gate, under the plane (a drawn stand-in: its fight's body is the boss; with no boss defined the stand-in stands for it, and
            /// once its motor dies the stand-in is what stays); the cage closed across the gate's opening (a stand-in for the fight's own outriggers, only while no boss is defined) and the
            /// cage hanging open (from its fall); the gate's stop, solid until its fall. Compact cutters on the carrier's deck.</summary>
            static (GameObject carrier,GameObject shut,GameObject open,GameObject wall) TheGate(ZoneKit kit,Stuff stuff,bool fought)
            {
                var root=new GameObject("The Tollkeeper's gate");
                GameObject carrier=null;var turn=Quaternion.identity;
                var prefab=ZoneKit.Prefab("Enemies","Tollkeeper");
                if(prefab!=null)
                {
                    var probe=(GameObject)PrefabUtility.InstantiatePrefab(prefab);var b=ModelGeometry.BoundsOf(probe);UnityEngine.Object.DestroyImmediate(probe);
                    // Its length across the way (east to west).
                    turn=Quaternion.Euler(0,b.size.z>b.size.x?90:0,0);
                    carrier=ConvoyKit.Body("Enemies","Tollkeeper",new Vector3(0,0,CarrierZ),turn);
                    var drawn=ZoneKit.Drawn(carrier);carrier.transform.position+=Vector3.up*(CarrierTop-drawn.max.y);drawn=ZoneKit.Drawn(carrier);
                    if(Vector3.Distance(drawn.center,TollkeeperAt)>.5f)Debug.LogWarning("MOORING_CARRIER_MOVED its middle is "+drawn.center+", not "+TollkeeperAt+": correct Mooring.TollkeeperAt");
                    carrier.name=fought?"The Tollkeeper, as it stays (drawn after its fall)":"The Tollkeeper (drawn: no fight defined yet)";carrier.transform.SetParent(root.transform,true);
                    Debug.Log("MOORING_CARRIER drawn="+drawn+" native="+b.size);
                }
                // The cage: one outrigger hinged at each side of the gate's opening.
                GameObject Rigger(string name,Vector3 hinge,float yaw,float pitch,Transform under)
                {
                    var o=ZoneKit.Prefab("Enemies","TollkeeperOutrigger");if(o==null)return null;
                    var probe=(GameObject)PrefabUtility.InstantiatePrefab(o);var b=ModelGeometry.BoundsOf(probe);UnityEngine.Object.DestroyImmediate(probe);float length=Mathf.Max(b.size.x,b.size.z);
                    var along=Quaternion.Euler(pitch,yaw,0)*Vector3.forward;var body=ConvoyKit.Body("Enemies","TollkeeperOutrigger",hinge+along*length*.5f,Quaternion.Euler(pitch,yaw+(b.size.x>b.size.z?90:0),0));
                    if(body==null)return null;body.name=name;body.transform.SetParent(under,true);return body;
                }
                var shut=new GameObject("The cage, closed (drawn: no fight defined yet)");shut.transform.SetParent(root.transform,true);
                if(!fought){Rigger("West outrigger, closed",HingeWest,90,0,shut.transform);Rigger("East outrigger, closed",HingeEast,-90,0,shut.transform);}
                // Hanging open after its fall: with the fight defined its own outriggers swing to that pose and stay (its contract: two cages would swing one through the other), so the
                // zone draws them only for a zone with no fight.
                var open=new GameObject("The cage, hanging open");open.transform.SetParent(root.transform,true);
                if(!fought){Rigger("West outrigger, hanging open",HingeWest+Vector3.down*2,-12,24,open.transform);Rigger("East outrigger, hanging open",HingeEast+Vector3.down*2,12,24,open.transform);}
                // The fight's cage, stood at the gate while the boss is on show: the Tollkeeper engineer's call (docs/maps/LastMooring.md, "What the fight needs of the room"),
                // LastMooringBuilder.Tollkeeper.StandCage(root). Found by name, so this file builds before and after that one has it; until then the fight stands its cage as it wakes.
                if(fought)
                {
                    var stand=typeof(Tollkeeper).GetMethod("StandCage",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);
                    var takes=stand?.GetParameters();
                    if(takes!=null&&takes.Length==1){stand.Invoke(null,new object[]{takes[0].ParameterType==typeof(Transform)?root.transform:root});Debug.Log("MOORING_CAGE_STOOD");}
                    else Debug.LogWarning("MOORING_CAGE_UNSTOOD LastMooringBuilder.Tollkeeper.StandCage isn't written yet: the fight stands its cage as it wakes");
                }
                var wall=new GameObject("The gate (until the Tollkeeper's motor dies)",typeof(BoxCollider));wall.isStatic=true;wall.transform.SetParent(root.transform,true);
                wall.transform.position=new Vector3(0,Plane,GateZ);wall.GetComponent<BoxCollider>().size=new Vector3(GateHalf*2+.6f,12,ChainWide);
                // Compact cutters on the carrier's deck and alongside it.
                foreach(var (x,z,yaw) in new[]{(-30f,486f,180f),(-22f,490f,170f),(28f,484f,190f),(36f,500f,200f)})ConvoyKit.Hull("CompactCutter",x,z,CarrierTop+1.5f,yaw,2.2f,root.transform);
                // The carrier's own anchor chains, out from its two ends past the outer chains and down into the works, each gripped half way by an anchor clamp the size of a district:
                // the machine the convoy is moored under. Laid first under the side lanes, they lay over the cage's bars and braces in every still of its fight (the Tollkeeper
                // engineer's stills, toll-look-01): nothing of the zone's is drawn inside x -48..48 from z 420 to the gate above y -30.
                var links=ZoneKit.Prefab("Environment","AnchorChain");
                if(links!=null)foreach(int side in new[]{-1,1})
                {
                    var from=new Vector3(side*47,CarrierTop-8,CarrierZ);var to=new Vector3(side*118,WorksY+6,CarrierZ+16);var run=to-from;int count=5;
                    for(int k=0;k<count;k++)
                    {
                        var piece=(GameObject)PrefabUtility.InstantiatePrefab(links);piece.name="The carrier's anchor chain";foreach(var c in piece.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
                        piece.transform.localScale*=run.magnitude/count/30f;piece.transform.rotation=Quaternion.LookRotation(run.normalized)*Quaternion.Euler(0,0,k%2==0?0:90);
                        var mid=from+run*((k+.5f)/count);var box=ZoneKit.Drawn(piece);piece.transform.position+=mid-box.center;piece.transform.SetParent(root.transform,true);
                    }
                    ConvoyKit.Hull("AnchorClamp",from.x+run.x*.45f,from.z+run.z*.45f,from.y+run.y*.45f+12,side*20,4,root.transform);
                }
                foreach(var (x,z) in new[]{(-26f,404f),(26f,410f),(-53f,430f),(53f,444f)})ConvoyKit.Hull("CompactCutter",x,z,-6,180,3,root.transform);
                return (carrier,shut,open,wall);
            }
            /// <summary>The door: the Tollkeeper's encounter over its arena, once its definition exists (the Tollkeeper engineer). Its box's mouth is the whole south face, behind chain D's gap.</summary>
            static EncounterVolume Door(string id)
            {
                if(id==null)return null;
                var door=WorldBuilder.Encounter("Tollkeeper",DoorCentre,DoorSize,id,1);
                var spawner=door.spawners[0];spawner.transform.position=TollkeeperAt;spawner.transform.rotation=Quaternion.Euler(0,180,0);spawner.faceAsPlaced=true;spawner.radius=0;
                door.requiredFlag="end.sectionHeld";door.checkpointSpawn="Gate";
                var show=new GameObject("The Tollkeeper on show").transform;show.SetParent(door.transform,false);show.position=Checkpoint;door.previewPoint=show;door.previewRadius=Vector3.Distance(Checkpoint,TollkeeperAt)+2;
                return door;
            }
            /// <summary>Past the gate: the Continuance, bow south, under the plane (drawn), its boarding spine from its bow up to a platform under the plane at the dock, Idra's crews' lamps
            /// along it, the dock's pad and sign (shown from <c>end.gateOpen</c>), Compact cutters alongside. Returns what the gate shows, and the spine.</summary>
            static (GameObject[] shown,GameObject spine) Beyond(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject("The Continuance, at the passage's mouth");
                var hull=ConvoyKit.Hull("Continuance",0,0,-17,90,1,root.transform);float bow=528;
                if(hull!=null){var b=ZoneKit.Drawn(hull);hull.transform.position+=new Vector3(0,0,bow-b.min.z);b=ZoneKit.Drawn(hull);Debug.Log("MOORING_CONTINUANCE drawn="+b);}
                float deck=hull!=null?ZoneKit.Drawn(hull).max.y:-17;
                var s=new ZoneKit.Shape();var head=new Vector3(DockAt.x,ShelfY,DockAt.z-2);
                // The platform at the spine's head, under the pad; the spine down from it to the bow, its rails, the crews' lamps.
                s.Slot(0).Box(head+Vector3.down*.3f,new Vector3(9,.6f,8),Quaternion.identity,White,White);
                s.Slot(1).Disc(new Vector3(DockAt.x,ShelfY+.04f,DockAt.z),Vector3.up,3.3f,28,Amber,2.8f,Amber);
                var foot=new Vector3(0,deck+.2f,bow+16);var from=head+new Vector3(0,-.3f,4);
                s.Slot(0).Beam(from,foot,4.6f,.5f,Pale,White);
                foreach(int side in new[]{-1,1})s.Slot(1).Beam(from+new Vector3(side*2.3f,1,0),foot+new Vector3(side*2.3f,1,0),.12f,.12f,Teal);
                for(int k=0;k<=6;k++){var p=Vector3.Lerp(from,foot,k/6f);foreach(int side in new[]{-1,1})s.Slot(2).Box(p+new Vector3(side*2.5f,.5f,0),new Vector3(.4f,.3f,.4f),Quaternion.identity,Warm);}
                var spine=kit.Built("The boarding spine",s,stuff.works,stuff.teal,stuff.glow);spine.transform.SetParent(root.transform,true);
                var lamp=kit.Lamp(head+Vector3.up*3,14,2,new Color(1f,.82f,.55f));lamp.transform.SetParent(root.transform,true);
                foreach(var (x,z,yaw) in new[]{(-14f,540f,20f),(16f,546f,-15f),(-18f,600f,0f)})ConvoyKit.Hull("PatrolCutter",x,z,deck-1,yaw,1,root.transform);
                var pad=new GameObject("Dock — The Continuance",typeof(DockingPad));pad.transform.position=DockAt;
                var dock=pad.GetComponent<DockingPad>();dock.prompt="Dock — The Continuance";dock.range=WorldBuilder.FlightDockRange;dock.scene=ContinuanceZone;dock.spawn=Continuance.SpineSpawn;
                var sign=WorldBuilder.Label("Dock — The Continuance",DockAt+new Vector3(0,-.7f,-5),2.2f);
                return (new[]{pad,sign},spine);
            }

            // ---------------------------------------------------------------- small things
            /// <summary>A point or a crossing at the plane (the kit stands one on the ground, which in a flight zone is y 0).</summary>
            static T AtPlane<T>(T point)where T:Component{var at=point.transform.position;point.transform.position=new Vector3(at.x,Plane-1,at.z);return point;}
            static void Gate(string name,bool showWhenSet,GameObject[] targets,string flag)
            {
                var gate=new GameObject(name,typeof(FlagGate)).GetComponent<FlagGate>();gate.flag=flag;gate.showWhenSet=showWhenSet;gate.targets=targets.Where(t=>t!=null).ToArray();
            }
        }
    }
}
