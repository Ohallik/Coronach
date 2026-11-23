using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Lattice.EditorTools
{
    /// <summary>The Broken Ring (space combat; rule: keep beside a district, its lit water is calm and its bulk is cover, and the danger runs down the marked channel between them).
    /// Map brief, the rule, coordinates and both combat engineers' contracts: docs/maps/BrokenRing.md. The districts fly in <see cref="SectionFlight"/>, the formation's door flag is
    /// <see cref="RingFormation"/>'s, the channel is a <see cref="WreckTrack"/>; everything here lays them from the named numbers of <see cref="Ring"/>, which the map brief, the tests
    /// and a route writer read too. The space-zone engineer's file.</summary>
    public static partial class HomeBuilder
    {
        /// <summary>The launch on the Decks cuts straight in with Bryn's radio: a hero's "new ground" line over it would break the moment.</summary>
        static void RingDefinition(ZoneDef z){z.quietArrival=true;}
        /// <summary>A report on the built Ring's cover (Ring.CoverReport); changes nothing.</summary>
        public static void RingCoverReport()=>BatchTools.Run(()=>Ring.CoverReport());
        [ZoneLook.Source] static void RingLooks()
        {
            // Home's own space, cooler than chapter 1's: a white key off a far star so the districts' cream plate and their family colours read true, a slate fill, and the windows,
            // the engines and the channel's lamps what blooms. The warm is the districts' own light, never the grade's.
            ZoneLook.Register(RingZone,new ZoneLook.Spec{sun=C(1f,.97f,.94f),sunIntensity=1.3f,shadowStrength=.75f,
                sky=C(.36f,.38f,.46f),equator=C(.3f,.31f,.36f),ground=C(.12f,.12f,.15f),
                contrast=14,saturation=8,splitShadows=C(.42f,.44f,.56f),splitHighlights=C(.6f,.54f,.46f),
                bloomThreshold=.85f,bloomIntensity=.85f,vignette=.22f});
        }

        internal static class Ring
        {
            // ---------------------------------------------------------------- the zone's numbers (docs/maps/BrokenRing.md)
            internal const float Plane=ZoneKit.FlightPlane;
            /// <summary>The Cutter's centre C, the fight's bound round it, its lanes' ring.</summary>
            internal static readonly Vector2 Centre=new(0,280);
            internal const float Bound=60,LaneRadius=30,Reach=45;
            /// <summary>The approach: walled at x ±<see cref="WallX"/> from <see cref="SouthZ"/> to where the fight's circle meets it; the exit lane north, x ±<see cref="ExitHalf"/> to <see cref="ExitEnd"/>.</summary>
            internal const float WallX=46,SouthZ=-18,ExitHalf=14,ExitEnd=382;
            /// <summary>The districts: 12 m across, 17 long, on columns at x ±<see cref="ColumnX"/> in the approach, bending out to their stations at x ±<see cref="StationX"/>.</summary>
            internal const float Wide=12,Long=17,ColumnX=30,StationX=42,BendFrom=230,BendTo=262;
            /// <summary>The channel down the middle, its banks at ±<see cref="BankX"/>, and the track its debris rides.</summary>
            internal const float ChannelHalf=6,BankX=6.5f,ChannelFrom=-26,ChannelTo=216,ChannelSpeed=9,ChannelReach=3,ChannelStrike=.1f,ChannelThrow=6,ChannelPhase=1.11f;
            internal const int ChannelPieces=5;
            internal static readonly Vector3 Arrival=new(-15,Plane,2),Checkpoint=new(0,Plane,240),HomeWarp=new(0,Plane,368),Tug=new(-64,Plane,280),TugAway=new(-130,Plane,280),
                Pair1=new(-16,Plane,118),Pair2=new(16,Plane,188),Cover1=new(-20,Plane,134),Cover2=new(20,Plane,172);
            internal static Vector3 C=>new(Centre.x,Plane,Centre.y);
            /// <summary>Cutter lane k (0 to 7): r 30 round C, k x 45 degrees clockwise from north.</summary>
            internal static Vector3 Lane(int k){float a=k*45*Mathf.Deg2Rad;return new Vector3(Centre.x+Mathf.Sin(a)*LaneRadius,Plane,Centre.y+Mathf.Cos(a)*LaneRadius);}
            /// <summary>What is left of the ring: chapter 1's Halo ring (r 62), far under the approach's south end, its sockets empty where the districts were.</summary>
            internal static readonly Vector3 RingCentre=new(0,-73,46);internal const float RingRadius=62;
            internal static Vector3 OnRing(float bearing,float lift=0){float a=bearing*Mathf.Deg2Rad;return RingCentre+new Vector3(Mathf.Sin(a)*RingRadius,lift,Mathf.Cos(a)*RingRadius);}

            /// <summary>One district: its id and names, its side (-1 west, +1 east), its station's z, its socket on the ring (bearing round the ring's centre), the lead's z it lifts at
            /// (or launched at load at <see cref="startZ"/>), Orrin's line for it (by its place in his pool), and each hero's <c>section</c> line for it.</summary>
            internal sealed class District
            {
                public string id,name,label;public int side;public float stationZ,socket,liftAt,startZ;public bool launched;public int orrinLine=-1;
                public (string hero,int line)[] section=Array.Empty<(string,int)>();
                public float Column=>side*ColumnX;public float StationXAt=>side*StationX;public int Inner=>-side;
                public Vector3 Station=>new(StationXAt,Plane,stationZ);
            }
            internal static readonly District[] Districts=
            {
                new District{id="market",name="the market",label="MARKET",side=-1,stationZ=296,socket=209,launched=true,startZ=10,section=new[]{("Sela",0)}},
                new District{id="repair",name="the repair quarter",label="REPAIR QUARTER",side=1,stationZ=296,socket=151,liftAt=10,orrinLine=1,section=new[]{("Taren",0),("Idra",0)}},
                new District{id="crescent",name="the residential crescent",label="CRESCENT",side=-1,stationZ=264,socket=230,liftAt=70,orrinLine=2,section=new[]{("Taren",1),("Idra",1),("Sela",1)}},
                new District{id="dock",name="the tug dock",label="TUG DOCK",side=1,stationZ=264,socket=130,liftAt=130,orrinLine=3,section=new[]{("Taren",2),("Idra",2),("Sela",2)}},
            };
            internal static District Of(string id)=>Districts.Single(d=>d.id==id);
            /// <summary>Where a district's root goes, from its socket (or the market's place at load) to its station: out of the ring and up to the plane on its column, north on it, and
            /// out to its station in a smooth bend. The root's y is the plane at the plane.</summary>
            internal static Vector3[] PathOf(District d)
            {
                var p=new List<Vector3>();float x=d.Column;
                if(d.launched)p.Add(new Vector3(x,Plane,d.startZ));
                else
                {
                    var s=OnRing(d.socket,3);p.Add(s);
                    // Out of its socket and up: steep at first, levelling as it comes to the plane on its column.
                    p.Add(new Vector3(Mathf.Lerp(s.x,x,.45f),-46,s.z+22));p.Add(new Vector3(Mathf.Lerp(s.x,x,.85f),-24,s.z+44));p.Add(new Vector3(x,-9,s.z+60));
                    p.Add(new Vector3(x,-2.5f,s.z+68));p.Add(new Vector3(x,Plane,s.z+76));
                }
                p.Add(new Vector3(x,Plane,BendFrom));
                for(int k=1;k<8;k++){float t=k/8f;p.Add(new Vector3(Mathf.Lerp(x,d.StationXAt,ZoneKit.Smooth(0,1,t)),Plane,Mathf.Lerp(BendFrom,BendTo,t)));}
                p.Add(new Vector3(d.StationXAt,Plane,BendTo));
                if(d.stationZ>BendTo+.5f)p.Add(d.Station);else p[p.Count-1]=d.Station;
                return p.ToArray();
            }
            /// <summary>Its way from its station once the Cutter is towed off: north out of the fight and down to the convoy, far under the way home.</summary>
            internal static Vector3[] ExitOf(District d)
            {
                float x=d.StationXAt,far=d.stationZ>270?0:1;
                return new[]{new Vector3(x,Plane,334),new Vector3(x+d.side*(5+far*6),-14-far*4,378+far*6),new Vector3(x+d.side*(16+far*14),-82-far*8,454+far*10)};
            }
            /// <summary>For stills only (<c>-ring-pose stills</c>, never saved as the zone): each district where a player meets it at the plane, the market and the repair quarter flying,
            /// the crescent and the tug dock on station. In play every district is put where its flags say at load, wherever the scene left it.</summary>
            internal static readonly Dictionary<string,Vector3> StillsPose=new(){["market"]=new(-ColumnX,Plane,44),["repair"]=new(ColumnX,Plane,78),["crescent"]=new(-StationX,Plane,264),["dock"]=new(StationX,Plane,264)};

            /// <summary>LookDev's stills: the arrival in the market's lee, the channel as it is crossed, the ring below, each district as it is flown beside (in a stills pose), both pairs
            /// and their cover, the checkpoint, the formation from inside the fight, the tug, the Cutter's first lane, the way home, and wide views.</summary>
            internal static LookDev.View[] Views=>new[]
            {
                new LookDev.View("arrival",Arrival,true),new("arrival-channel",new Vector3(-4,Plane,10),true),new("ring-below",new Vector3(0,Plane,26),true,2.6f),
                new("market",new Vector3(-17,Plane,44),true),new("market-front",new Vector3(-30,Plane,30),true),new("repair",new Vector3(17,Plane,78),true),new("repair-front",new Vector3(30,Plane,64),true),
                new("channel",new Vector3(0,Plane,60),true),new("lee-east",new Vector3(15,Plane,96),true),new("pair1",Pair1,true),new("cover1",Cover1+new Vector3(4,0,-4),true),
                new("pair2",Pair2,true),new("cover2",Cover2+new Vector3(-4,0,-4),true),new("channel-end",new Vector3(0,Plane,206),true),new("checkpoint",Checkpoint,true),
                new("crescent",new Vector3(-26,Plane,264),true),new("crescent-front",new Vector3(-42,Plane,250),true),new("dock",new Vector3(26,Plane,264),true),new("dock-front",new Vector3(42,Plane,250),true),
                new("fight-west",new Vector3(-20,Plane,280),true),new("fight-east",new Vector3(20,Plane,280),true),new("tug",new Vector3(-48,Plane,280),true),new("tug-boom",Tug+new Vector3(-2,0,0),true),new("lane-0",Lane(0),true),
                new("exit",new Vector3(0,Plane,352),true),new("warp",HomeWarp+Vector3.back*4,true),
                new("approach-wide",new Vector3(0,Plane,70),true,3.6f),new("fight-wide",C,true,3.6f),new("overview",new Vector3(0,Plane,190),true,11),
            };

            // ---------------------------------------------------------------- what it is made of
            internal sealed class Stuff{public Material steel,soot,brass,paint,glow,pool,flame,flare,cloth,rope,bank,halo;public readonly Dictionary<string,Material> picto=new();}
            static readonly Dictionary<(Material,string),Material> painted=new();
            /// <summary>The Halo's families' colours: the trim of a cabin's window and its door, a stall's cloth, a tug's stripe. Never a danger's orange, red or purple.</summary>
            internal static readonly Color[] Families={new(.24f,.66f,.62f),new(.9f,.72f,.28f),new(.94f,.9f,.8f),new(.42f,.62f,.9f),new(.44f,.74f,.4f),new(.86f,.76f,.56f),new(.56f,.62f,.82f)};
            static readonly Color Warm=new(1f,.82f,.52f),Cool=new(.76f,.9f,1f),Cream=new(.9f,.86f,.76f),Soot=new(.16f,.17f,.19f),Brass=new(.74f,.58f,.3f),Iron=new(.3f,.31f,.34f),Chalk=new(.96f,.96f,.92f),Line=new(.98f,.8f,.18f);
            static Stuff Materials(ZoneKit kit)
            {
                var s=new Stuff();
                var plate=ZoneKit.Texture("Ring/ring-plate",256,(u,v)=>{float n=.8f+.1f*(ZoneKit.Cloud(u,v,8,3)-.5f)+.05f*(ZoneKit.Tile(u*64,v*64,64,5)-.5f);
                    float seam=Mathf.Abs(Mathf.Repeat(u*4,1)-.5f)<.012f||Mathf.Abs(Mathf.Repeat(v*4,1)-.5f)<.012f?-.2f:0;return new Color(n+seam,n+seam,n*1.02f+seam,1);});
                s.steel=ZoneKit.Tinted(ZoneKit.Toon("ring-steel",plate,m=>{m.SetColor("_BaseColor",Color.white);m.SetFloat("_RampSteps",4);m.SetColor("_RimColor",new Color(.22f,.24f,.28f));}));
                s.soot=ZoneKit.Tinted(ZoneKit.Toon("ring-soot",plate,m=>{m.SetColor("_BaseColor",new Color(.7f,.7f,.72f));m.SetFloat("_RampSteps",3);}));
                s.brass=ZoneKit.Tinted(ZoneKit.Toon("ring-brass",null,m=>{m.SetColor("_BaseColor",Color.white);m.SetFloat("_RampSteps",4);m.SetColor("_RimColor",new Color(.6f,.5f,.3f));}));
                // Paint on plate: the promenade's yellow line, chalk, family colours, lock nine's number. Lit a little from within so it reads under the key's shadow side.
                s.paint=ZoneKit.Tinted(ZoneKit.Toon("ring-paint",null,m=>{m.SetColor("_BaseColor",Color.white);m.SetFloat("_RampSteps",3);m.SetFloat("_LightFloor",.5f);}));
                // Windows, lamps and the cut edges' work lights: unlit, coloured in their vertices, bright enough to bloom a little.
                s.glow=ZoneKit.Toon("ring-glow",null,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetVector("_GlowParams",new Vector4(1.2f,0,0,0));});
                // The bank lamps of the channel: the same unlit glow, dimmer, so the banks are a boundary and the amber down the middle the thing that's lit.
                s.bank=ZoneKit.Toon("ring-bank",null,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetVector("_GlowParams",new Vector4(.8f,0,0,0));});
                // A district's lee: the warm light of its windows lying on the plane beside it, a soft-edged sheet added to what is under it and drawn before any danger marking.
                // One soft even glow over the lee, fading out to its sides and ends: drawn in fourteen bands along its length, it read in the stills as a blurred ladder.
                var pool=ZoneKit.Texture("Ring/ring-lee-pool-2",128,(u,v)=>{float dv=Mathf.Min(v,1-v);float t=ZoneKit.Smooth(0,.22f,dv)*Mathf.Pow(1-Mathf.Abs(u-.5f)*2,1.6f);
                    return new Color(t,t,t,1);},false);
                s.pool=ZoneKit.Toon("ring-lee-pool",pool,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetVector("_GlowParams",new Vector4(1,0,0,0));m.SetFloat("_SrcBlend",(float)BlendMode.One);m.SetFloat("_DstBlend",(float)BlendMode.One);
                    m.SetFloat("_ZWrite",0);m.SetOverrideTag("RenderType","Transparent");m.renderQueue=(int)RenderQueue.Transparent-30;m.SetFloat("_ReceiveShadows",0);m.SetColor("_BaseColor",new Color(.34f,.27f,.17f));});
                s.cloth=ZoneKit.TwoSided(ZoneKit.Tinted(ZoneKit.Toon("ring-cloth",null,m=>{m.SetColor("_BaseColor",Color.white);m.SetFloat("_LightFloor",.35f);})));
                s.rope=ZoneKit.Tinted(ZoneKit.Toon("ring-rope",null,m=>m.SetColor("_BaseColor",Color.white)));
                // The Crawl's engines burn warm white (never a danger's orange): a cone of light added to what is behind it, hot at the nozzle and gone by its point, and a soft flare at
                // each nozzle. Drawn solid (unlit glow, written over what was behind it) each cone was a flat white funnel from the flight camera, three under every district's stern
                // as it lifted (whole-game capture, November 20, s61/058, 071, 083, 102, 112).
                s.flame=ZoneKit.Toon("ring-flame",null,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetVector("_GlowParams",new Vector4(1.3f,0,0,0));
                    m.SetFloat("_SrcBlend",(float)BlendMode.One);m.SetFloat("_DstBlend",(float)BlendMode.One);m.SetFloat("_ZWrite",0);m.SetOverrideTag("RenderType","Transparent");
                    m.renderQueue=(int)RenderQueue.Transparent-20;m.SetFloat("_ReceiveShadows",0);m.SetColor("_BaseColor",new Color(.62f,.56f,.46f));});
                s.flare=kit.Glow("Engine flare",false,new Color(1.6f,1.45f,1.1f,1));
                s.halo=kit.Glow("Buoy halo",false,new Color(1.1f,1.25f,1.4f,1));
                // The Halo's own roof paint from chapter 1 (RoomDressing): the trade on each roof, read from the flight camera.
                foreach(string kind in new[]{"PictoMarket","PictoRepair","PictoCabins","PictoDock"}){var m=ZoneKit.WorldMaterial("dress-detail-"+kind);if(m!=null)s.picto[kind]=m;}
                return s;
            }
            /// <summary>A piece in a colour of its own, drawn in copies of its own materials with the colour multiplied in (saved beside the scene): a tint in a property block shows in play
            /// only, and the editor's stills would show it in its stock paint.</summary>
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
                            string path="Assets/_Project/Scenes/"+RingZone+"-"+source.name.Replace(' ','-')+"-"+name+".mat";
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
            static void Unshadowed(GameObject go,bool casts=false){foreach(var r in go.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=casts?ShadowCastingMode.On:ShadowCastingMode.Off;r.receiveShadows=casts;}}
            /// <summary>A station kit piece fitted to a box (WorldBuilder.Piece: the deck and wall modules tile themselves at their own proportions), drawn only, under a parent.</summary>
            static GameObject Piece(Transform parent,string key,Vector3 centre,Vector3 size)
            {
                if(!ZoneKit.Landed(key))return null;var go=WorldBuilder.Piece(key,centre,size,"Rock",false);go.transform.SetParent(parent,true);return go;
            }
            /// <summary>A kit prop standing at a point (its height <paramref name="tall"/>), turned, drawn only, under a parent.</summary>
            static GameObject Stand(ZoneKit kit,Transform parent,string key,Vector3 feet,float tall,float yaw)
            {
                // Nothing of a district but a mast stands more than 3.25 m over the plane (built about the origin, the plane is y 0), so it never hides a ship or a marking
                // beside it: the repair quarter's fuel tank stood 4.1 m up on its roof.
                if(key!="DressCommsMast")tall=Mathf.Min(tall,RoofMost-feet.y);if(tall<.2f||!ZoneKit.Landed(key))return null;
                var go=kit.Prop(key,feet,tall,yaw,false,false);go.transform.SetParent(parent,true);return go;
            }
            const float RoofMost=3.25f;
            /// <summary>A flat picture lying on a surface (a roof's trade, a sign's), the chapter 1 decal on a quad of its own.</summary>
            static void Picture(Stuff stuff,Transform parent,string kind,Vector3 at,float size,Vector3? facing=null)
            {
                if(!stuff.picto.TryGetValue(kind,out var look))return;
                var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());quad.name="Painted "+kind;
                quad.transform.SetParent(parent,true);quad.transform.position=at;quad.transform.rotation=facing is Vector3 f?Quaternion.LookRotation(-f):Quaternion.Euler(90,0,0);quad.transform.localScale=Vector3.one*size;
                var r=quad.GetComponent<Renderer>();r.sharedMaterial=look;r.shadowCastingMode=ShadowCastingMode.Off;
            }
            /// <summary>Someone aboard, standing on an open deck: a body of the Halo's own people, still, its animation culled out of sight.</summary>
            static void Aboard(Transform parent,string body,Vector3 feet,float yaw)
            {
                var prefab=ZoneKit.Prefab("Characters",body);if(prefab==null)return;
                var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                foreach(var c in go.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
                foreach(var b in go.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null)UnityEngine.Object.DestroyImmediate(b);
                foreach(var a in go.GetComponentsInChildren<Animator>())a.cullingMode=AnimatorCullingMode.CullCompletely;
                // Its bounds follow the pose it stands in: the import's own box stood a body on the repair quarter's deck four metres over the plane, and that box is what the camera
                // culls by and every measure reads.
                foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;
                go.name="Aboard: "+body;go.transform.SetPositionAndRotation(feet,Quaternion.Euler(0,yaw,0));go.transform.SetParent(parent,true);
            }
            /// <summary>A lit pane on a face that looks <paramref name="outward"/>, with a dark frame round it (the frame in the family's colour where it has one).</summary>
            static void Window(ZoneKit.Shape s,Vector3 centre,Vector3 along,float wide,float tall,Vector3 outward,Color light,Color frame)
            {
                var x=along*wide*.5f;var y=Vector3.up*tall*.5f;var fx=along*(wide*.5f+.14f);var fy=Vector3.up*(tall*.5f+.14f);
                s.Slot(1).Quad(centre-fx-fy,centre+fx-fy,centre+fx+fy,centre-fx+fy,outward,frame);
                var c=centre+outward*.02f;s.Slot(2).Quad(c-x-y,c+x-y,c+x+y,c-x+y,outward,light);
            }

            // ---------------------------------------------------------------- the zone
            internal static void Build()
            {
                painted.Clear();
                var kit=ZoneKit.Flight(RingZone);
                var scene=WorldBuilder.Begin(kit.zone);
                var stuff=Materials(kit);
                Validate();
                var arrival=WorldBuilder.Spawn("Arrival",Arrival);arrival.transform.rotation=Quaternion.identity;
                var ring=WorldBuilder.Spawn("Ring",Checkpoint);ring.transform.rotation=Quaternion.identity;
                kit.DriftingSky();
                var keep=Keep();

                // ---- the backdrop: what is left of the ring, wreckage below, Many Hands turning, the convoy's lights, Vorun and Sorrel
                RingLeft(kit,stuff);
                Below(kit,stuff,keep);
                Hub();
                Convoy(kit,stuff);
                // Vorun at a third of the size it was: at 300 it filled a quarter of every frame, the arrival's and the fight's, and the districts were the small things in them. Each
                // stands where the flight camera sees it in the open band down a side of the frame the whole way north (Vorun the right's, from 370 px down to 810 at the exit, Sorrel the
                // left's): at (260, -470, 820) and (-170, -330, 640) they rode up under the objective panel and the party's, where no still showed more than an edge of either
                // (whole-game capture, November 20, s60/250, s61/050). Far enough down that the camera's own travel moves them little.
                WorldArt.Planet("Vorun",new Vector3(399,-907,986),208);WorldArt.Planet("Sorrel",new Vector3(-303,-692,760),58);
                // ---- what is left of the ring's own spans, broken off and adrift under the lanes: the broken ring is what the party flies through
                Adrift(kit,stuff,keep);
                var motes=kit.Glow("Motes",false,new Color(1.2f,1.15f,1.05f,1));
                for(float z=0;z<380;z+=95)kit.Drift("Drifting grit",new Vector3(0,-3,z+40),new Vector3(WallX*2+20,12,100),motes,new Color(.92f,.92f,1,.22f),300,.05f,.16f,.1f,20);

                // ---- the walls, the lamp-buoy line on them and the wreckage drifting beyond them
                int walls=Walls(kit,stuff);

                // ---- the channel down the middle
                var channel=Channel(kit,stuff);

                // ---- the four districts
                var sections=Districts.Select(d=>Section(kit,stuff,d)).ToArray();
                string pose=DevArgs.Value("-ring-pose");
                if(pose=="stills")foreach(var section in sections)section.transform.position=StillsPose[section.districtId];
                var formation=new GameObject("The formation",typeof(RingFormation)).GetComponent<RingFormation>();formation.centre=C;formation.reach=Reach;formation.need=2;formation.formedFlag="ring.formed";

                // ---- cover on the way, the two pairs, and the first sight of them
                CoverWreck(kit,stuff,"Cover wreck 1",Cover1,20,new System.Random(1341));CoverWreck(kit,stuff,"Cover wreck 2",Cover2,-25,new System.Random(1720));
                string interceptor=Foe("Interceptor","ClaimDrone");
                EncounterVolume Pair(string id,Vector3 at){var fight=kit.FlightFight(id,at,new Vector3(40,8,40),(interceptor,2));fight.spawners[0].transform.position=at;fight.spawners[0].radius=4;return fight;}
                var pair1=Pair("Ring_Pair1",Pair1);Pair("Ring_Pair2",Pair2);
                var sight=new GameObject("First sight: interceptor",typeof(FirstSight)).GetComponent<FirstSight>();sight.transform.position=Pair1;sight.situation="interceptor";sight.encounter=pair1;sight.onceFlag="ring.interceptorSeen";sight.requiredFlag="";

                // ---- the Cutter's fight: its lanes, its door, Bryn's tug
                var lanes=new GameObject("The Cutter's lanes");
                for(int k=0;k<8;k++){var lane=new GameObject("Cutter lane "+k,typeof(CutterLane));lane.transform.SetParent(lanes.transform,true);lane.transform.position=Lane(k);lane.GetComponent<CutterLane>().index=k;}
                var cutter=WorldBuilder.Encounter("DebrisCutter",C,new Vector3(100,8,100),Foe("DebrisCutter","DraskSkiff"),1);
                var post=cutter.spawners[0];post.transform.position=Lane(0);post.transform.rotation=Quaternion.Euler(0,180,0);post.faceAsPlaced=true;post.radius=0;
                cutter.requiredFlag="ring.formed";cutter.checkpointSpawn="Ring";
                TugWaiting(kit);

                // ---- what is said: Bryn at the launch, Bryn after the fall
                var hail=new GameObject("Bryn on the radio: hail.ring",typeof(RadioHail)).GetComponent<RadioHail>();hail.transform.position=Arrival;
                hail.speaker="Bryn";hail.situations=new[]{"hail.ring","hail.ring"};hail.needs=Array.Empty<string>();hail.heardFlag="ring.brynHailed";hail.silencedBy="";hail.range=40;
                var tow=new GameObject("Bryn tows the Cutter",typeof(AfterTalk)).GetComponent<AfterTalk>();tow.transform.position=C;
                tow.flag="bossdown.DebrisCutter";tow.node="BrynTowsCutter";tow.speaker="Bryn";tow.heardFlag="home.cutterDisabled";tow.delay=2.5f;tow.range=0;

                // ---- the way home, once she has it in tow
                var home=WorldBuilder.Warp("Warp — The Hull of Many Hands",HomeWarp,Hull,"home.cutterDisabled","Ring");home.range=WorldBuilder.FlightDockRange;
                home.lockedPrompt="Hold the Ring first: the Debris Cutter is still hunting the districts";

                Unmoving(sections);
                WorldBuilder.Save(scene,kit.zone);
                Debug.Log("HOME_COORDS BrokenRing arrival="+Arrival+" checkpoint="+Checkpoint+" C="+C+" warp="+HomeWarp+" tug="+Tug+" away="+TugAway+" pair1="+Pair1+" pair2="+Pair2+" cover1="+Cover1+" cover2="+Cover2
                    +" lanes="+string.Join(" ",Enumerable.Range(0,8).Select(k=>Lane(k).ToString("0.0")))+" stations="+string.Join(" ",Districts.Select(d=>d.id+d.Station.ToString("0.0")))
                    +" paths="+string.Join(" ",sections.Select(s=>s.districtId+"="+s.Total.ToString("0")+"m"))+$" channel traverse={channel.Traverse:0.0}s length={channel.Total:0}m walls={walls} pose={pose??"play"}");
                Debug.Log("BROKEN_RING_OK");
                _=formation;
            }

            /// <summary>The contracts' numbers, checked on the layout before anything is built, each fault said by name: the Cutter's lanes keep 8 m of open space from every district at its
            /// station (swaying), stations are within reach of C and 14 m apart, the checkpoint and the tug are clear of every district's way, each pair is 75 m from C and 70 from the other
            /// with a district's column within 20 m, the channel's marking 12 m off its centre, and the channel never under a district.</summary>
            static void Validate()
            {
                var faults=new List<string>();var c=Centre;
                Rect Foot(Vector3 at)=>new(at.x-Wide*.5f,at.z-Long*.5f,Wide,Long);
                foreach(var d in Districts)
                {
                    var foot=Foot(d.Station);foot.xMin-=1;foot.xMax+=1;foot.yMin-=.6f;foot.yMax+=.6f;
                    for(int k=0;k<8;k++){var l=Lane(k);float gap=SectionFlight.From(foot,new Vector2(l.x,l.z));if(gap<8)faults.Add($"{d.id}'s station is {gap:0.0} m from Cutter lane {k}");}
                    if(Vector2.Distance(new Vector2(d.Station.x,d.Station.z),c)>Reach)faults.Add($"{d.id}'s station is {Vector2.Distance(new Vector2(d.Station.x,d.Station.z),c):0.0} m from C");
                    foreach(var o in Districts)if(o!=d&&SectionFlight.Gap(Foot(d.Station),Foot(o.Station))<14.5f)faults.Add($"{d.id} and {o.id} hold stations {SectionFlight.Gap(Foot(d.Station),Foot(o.Station)):0.0} m apart");
                    var path=PathOf(d);
                    for(int i=1;i<path.Length;i++)for(float t=0;t<=1;t+=.05f)
                    {
                        var p=Vector3.Lerp(path[i-1],path[i],t);if(p.y<Plane-1)continue;var f=Foot(p);
                        if(SectionFlight.From(f,new Vector2(Checkpoint.x,Checkpoint.z))<6)faults.Add($"{d.id}'s way passes {SectionFlight.From(f,new Vector2(Checkpoint.x,Checkpoint.z)):0.0} m from the checkpoint");
                        if(SectionFlight.From(f,new Vector2(Tug.x,Tug.z))<8)faults.Add($"{d.id}'s way passes {SectionFlight.From(f,new Vector2(Tug.x,Tug.z)):0.0} m from Bryn's tug");
                        if(p.z<ChannelTo&&f.xMin<BankX+1&&f.xMax>-BankX-1)faults.Add($"{d.id}'s way crosses the channel at z {p.z:0}");
                        if(Mathf.Abs(p.x)+Wide*.5f>WallX-1&&p.z<BendFrom)faults.Add($"{d.id}'s way meets the approach's wall at z {p.z:0}");
                    }
                }
                foreach(var (pair,name) in new[]{(Pair1,"Ring_Pair1"),(Pair2,"Ring_Pair2")})
                {
                    var at=new Vector2(pair.x,pair.z);
                    if(Vector2.Distance(at,c)<75)faults.Add($"{name} is {Vector2.Distance(at,c):0} m from C");
                    if(Mathf.Abs(pair.x)-ChannelReach-.5f<12)faults.Add($"{name}'s centre is {Mathf.Abs(pair.x)-ChannelReach-.5f:0.0} m from the channel's marking");
                    float column=Mathf.Abs(Mathf.Abs(pair.x)-(ColumnX-Wide*.5f));if(column>20||column<8)faults.Add($"{name} is {column:0.0} m from its column's district");
                }
                if(Vector3.Distance(Pair1,Pair2)<70)faults.Add($"the pairs are {Vector3.Distance(Pair1,Pair2):0} m apart");
                if(faults.Count>0)throw new InvalidOperationException("BrokenRing's layout breaks a contract:\n"+string.Join("\n",faults));
            }

            /// <summary>What must stay readable on screen, each with the room it needs: the arrival, the channel, the pairs and their cover, the checkpoint, C and the Cutter's lanes,
            /// the districts' stations, the tug and the way home.</summary>
            static List<(Vector2 at,float clear)> Keep()
            {
                Vector2 F(Vector3 p)=>new(p.x,p.z);
                var keep=new List<(Vector2,float)>{(F(Arrival),8),(F(Pair1),12),(F(Pair2),12),(F(Cover1),6),(F(Cover2),6),(F(Checkpoint),8),(Centre,14),(F(Tug),8),(F(HomeWarp),8)};
                for(float z=ChannelFrom+16;z<=ChannelTo-16;z+=12)keep.Add((new Vector2(0,z),ChannelHalf+1));
                for(int k=0;k<8;k++)keep.Add((F(Lane(k)),6));
                foreach(var d in Districts)keep.Add((F(d.Station),10));
                return keep;
            }

            // ---------------------------------------------------------------- the walls
            /// <summary>Every solid line a hull meets: the approach's two sides and its south end, the fight's circle (open to the approach and to the exit lane), and the exit lane's sides
            /// and end. Short boxes along each, a little longer than their piece so neighbours overlap; none runs on past a bend (chapter 6's corridor ran a wall 72 m into a current).
            /// On every one a lamp-buoy line, and beyond each the ring's wreckage drifting, so the stop is something seen. Returns the number of boxes.</summary>
            static int Walls(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject("Ring limits");int count=0;var lines=new List<List<Vector2>>();
                void Line(IReadOnlyList<Vector2> line,string name)
                {
                    lines.Add(line.ToList());
                    for(int i=1;i<line.Count;i++)
                    {
                        var a=line[i-1];var b=line[i];var run=b-a;if(run.sqrMagnitude<.01f)continue;
                        var wall=new GameObject(name+" "+i,typeof(BoxCollider));wall.isStatic=true;wall.transform.SetParent(root.transform,true);
                        wall.transform.SetPositionAndRotation(new Vector3((a.x+b.x)*.5f,Plane,(a.y+b.y)*.5f),Quaternion.Euler(0,Mathf.Atan2(run.x,run.y)*Mathf.Rad2Deg,0));
                        wall.GetComponent<BoxCollider>().size=new Vector3(2,12,run.magnitude+.5f);count++;
                    }
                }
                List<Vector2> Arc(float from,float to,float step=3){var pts=new List<Vector2>();float dir=Mathf.Sign(to-from);for(float t=from;dir*(to-t)>0;t+=dir*step)pts.Add(OnCircle(t));pts.Add(OnCircle(to));return pts;}
                float meet=Mathf.Atan2(WallX,-(float)Math.Sqrt(Bound*Bound-WallX*WallX))*Mathf.Rad2Deg,gap=Mathf.Asin(ExitHalf/Bound)*Mathf.Rad2Deg;
                float meetZ=Centre.y-Mathf.Sqrt(Bound*Bound-WallX*WallX),exitZ=Centre.y+Mathf.Sqrt(Bound*Bound-ExitHalf*ExitHalf);
                Line(new[]{new Vector2(-WallX,SouthZ),new Vector2(WallX,SouthZ)},"South limit");
                Line(new[]{new Vector2(-WallX,SouthZ),new Vector2(-WallX,meetZ)},"West limit");Line(new[]{new Vector2(WallX,SouthZ),new Vector2(WallX,meetZ)},"East limit");
                Line(Arc(meet,gap),"Fight's bound east");Line(Arc(360-meet,360-gap),"Fight's bound west");
                Line(new[]{new Vector2(-ExitHalf,exitZ),new Vector2(-ExitHalf,ExitEnd)},"Exit limit west");Line(new[]{new Vector2(ExitHalf,exitZ),new Vector2(ExitHalf,ExitEnd)},"Exit limit east");
                Line(new[]{new Vector2(-ExitHalf,ExitEnd),new Vector2(ExitHalf,ExitEnd)},"Exit limit north");
                // The lamp-buoys: a small dark float with a cold lamp and a halo the flight camera reads, every ten metres along every limit.
                var buoys=new ZoneKit.Shape();var halos=new List<Vector3>();
                foreach(var line in lines)
                {
                    float carry=0;
                    for(int i=1;i<line.Count;i++)
                    {
                        var a=line[i-1];var b=line[i];float len=Vector2.Distance(a,b);
                        for(float t=carry;t<len;t+=10){var p=Vector2.Lerp(a,b,t/len);var at=new Vector3(p.x,Plane-1.2f,p.y);
                            buoys.Slot(0).Box(at,new Vector3(1.1f,1.1f,1.1f),Quaternion.Euler(0,(p.x*7+p.y*3)%90,0),Iron,new Color(.42f,.44f,.48f));
                            buoys.Slot(1).Box(at+Vector3.up*.75f,new Vector3(.46f,.4f,.46f),Quaternion.identity,new Color(.82f,.92f,1f));halos.Add(at+Vector3.up*1.1f);}
                        carry=Mathf.Repeat(carry-len,10);
                    }
                }
                var built=kit.Built("Lamp buoys",buoys,stuff.steel,stuff.bank);built.transform.SetParent(root.transform,true);Unshadowed(built);
                foreach(var h in halos)
                {
                    var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());quad.name="Buoy halo";quad.transform.SetParent(root.transform,true);
                    quad.transform.SetPositionAndRotation(h,Quaternion.Euler(ZoneKit.FlightPitch,0,0));quad.transform.localScale=Vector3.one*2.4f;var r=quad.GetComponent<Renderer>();r.sharedMaterial=stuff.halo;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
                }
                // Beyond the line: the ring's wreckage, at the plane and a little under it, tumbling slowly. Drawn only: the wall is what a hull meets.
                var rng=new System.Random(4646);var drift=new GameObject("Wreckage beyond the limits");var exits=Districts.Select(d=>new[]{d.Station}.Concat(ExitOf(d)).Select(e=>new Vector2(e.x,e.z)).ToList()).ToList();string[] keys={"DeckFloor","DeckWall","DeckCrate","DressCargoPallet","RingSegmentA","RingSegmentB","DressFuelTank","MusterCrate","DressPipeRun"};
                foreach(var line in lines)
                    for(int i=1;i<line.Count;i++)
                    {
                        var a=line[i-1];var b=line[i];float len=Vector2.Distance(a,b);var along=(b-a)/Mathf.Max(.01f,len);var outward=Outward(a,b);
                        for(float t=rng.Next(4);t<len;t+=6+(float)rng.NextDouble()*6)
                        {
                            var p=a+along*t+outward*(4+(float)rng.NextDouble()*10);string key=keys[rng.Next(keys.Length)];float size=key.StartsWith("Ring")?6+(float)rng.NextDouble()*4:key.StartsWith("Deck")&&key!="DeckCrate"?3+(float)rng.NextDouble()*2:1.6f+(float)rng.NextDouble()*1.6f;
                            var turned=Quaternion.Euler(rng.Next(360),rng.Next(360),rng.Next(360));float sink=(float)rng.NextDouble()*3;if(!ZoneKit.Landed(key))continue;
                            // Dropped, not moved, off the tug's berth and the districts' ways out to the convoy.
                            if(Vector2.Distance(p,new Vector2(Tug.x,Tug.z))<14||exits.Any(e=>ZoneKit.FromPath(p,e)<Wide+size))continue;
                            // The deck's floor and wall modules at their own proportions, as Below lays them: fitted by height alone, a wall module stood on end was a panel stretched
                            // fifteen times across its texture.
                            GameObject piece;var at=new Vector3(p.x,Plane-sink,p.y);
                            if(key=="DeckFloor"||key=="DeckWall"){piece=WorldBuilder.Piece(key,at,key=="DeckFloor"?new Vector3(size,.4f,size*.9f):new Vector3(size,size*.8f,.5f),"Rock",false);piece.transform.rotation=turned;}
                            else piece=kit.Loose(key,at,size,turned);
                            piece.transform.SetParent(drift.transform,true);Unshadowed(piece,true);
                            if(rng.Next(3)==0){var tumble=piece.AddComponent<Tumble>();tumble.axis=new Vector3((float)rng.NextDouble()-.5f,1,(float)rng.NextDouble()-.5f).normalized;tumble.rate=2+(float)rng.NextDouble()*4;}
                        }
                    }
                return count;
            }
            /// <summary>A point of the fight's bound at a bearing (degrees clockwise from north) round C.</summary>
            static Vector2 OnCircle(float bearing){float a=bearing*Mathf.Deg2Rad;return Centre+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*Bound;}
            /// <summary>Which way is out of the zone across a limit from a to b: away from the middle of the open water near it.</summary>
            static Vector2 Outward(Vector2 a,Vector2 b)
            {
                var along=(b-a).normalized;var n=new Vector2(along.y,-along.x);var mid=(a+b)*.5f;
                var inside=mid.y>Centre.y+Bound*.9f?new Vector2(0,mid.y-4):Vector2.Distance(mid,Centre)<Bound+1?Centre:new Vector2(0,mid.y);
                return Vector2.Dot(n,inside-mid)>0?-n:n;
            }

            // ---------------------------------------------------------------- the channel
            /// <summary>The marked channel down the middle: a straight open track the ring's debris rides outward at 9 m/s, coming up out of the field over its first 16 m and going down over
            /// its last; its lamps down the middle (amber ahead of each piece, a cold chase elsewhere: Nacre's language) on a cable just under the plane, and its two banks of pale lamps.</summary>
            static WreckTrack Channel(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject("The channel");var track=root.AddComponent<WreckTrack>();track.trackName="The channel";
                var path=new List<Vector3>();for(float z=ChannelFrom;z<=ChannelTo;z+=2)path.Add(new Vector3(0,Plane,z));track.path=path.ToArray();track.closed=false;
                track.centre=new Vector2(0,-1000);track.speed=ChannelSpeed;track.refRadius=100;track.steep=0;track.minSpeed=ChannelSpeed;track.maxSpeed=ChannelSpeed;
                track.pieceRadius=ChannelReach;track.lead=2.5f;track.plane=Plane;track.sink=8;track.sinkLength=16;track.strike=ChannelStrike;track.throwClear=ChannelThrow;
                track.markRange=55;track.markStep=6;track.markWidth=ChannelReach*2+1;track.phase=ChannelPhase;
                // The pieces: the ring's own plate and fascia, a crate, a cabin's door, a ring segment's end, each turned and tumbling on its "Body".
                var rng=new System.Random(216);string[] keys={"DeckFloor","RingSegmentA","DeckWall","DeckCrate","DeckDoorway"};var pieces=new List<Transform>();
                for(int i=0;i<ChannelPieces;i++)
                {
                    var piece=new GameObject("Debris "+(i+1)).transform;piece.SetParent(root.transform,true);var body=new GameObject("Body").transform;body.SetParent(piece,false);
                    string key=keys[i%keys.Length];
                    if(ZoneKit.Landed(key))
                    {
                        var size=key=="DeckFloor"?new Vector3(4,.5f,3.6f):key=="DeckWall"?new Vector3(4.2f,3.2f,.5f):key=="DeckCrate"?Vector3.one*2.2f:key=="DeckDoorway"?new Vector3(3.4f,3.2f,.6f):Vector3.one*5;
                        var drawn=key.StartsWith("Ring")?kit.Loose(key,Vector3.zero,5.2f,Quaternion.identity):WorldBuilder.Piece(key,Vector3.zero,size,"Rock",false);
                        drawn.transform.rotation=Quaternion.Euler(rng.Next(-40,40),rng.Next(360),rng.Next(-50,50));drawn.transform.position-=ZoneKit.Drawn(drawn).center;
                        drawn.transform.SetParent(body,false);Unshadowed(drawn,true);
                    }
                    pieces.Add(piece);
                }
                track.pieces=pieces.ToArray();track.Pose(0);
                // The middle's lamps, written as they stand the moment the zone opens (play writes its own each frame).
                float from=track.sinkLength,to=track.Total-track.sinkLength,spacing=3;int count=Mathf.Max(1,Mathf.FloorToInt((to-from)/spacing)+1);var beads=new ZoneKit.Shape();
                for(int k=0;k<count;k++){float a=from+k*spacing;beads.Box(track.PointAt(a)+Vector3.down*.55f,new Vector3(.5f,.22f,.9f),Quaternion.identity,Color.white);}
                var mesh=beads.Mesh("channel lamps");var uv=new Vector2[mesh.vertexCount];for(int v=0;v<uv.Length;v++)uv[v]=new Vector2((v/24+.5f)/count,.5f);mesh.uv=uv;
                mesh=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+kit.zone+"-channel-lamps.asset");
                var strip=new GameObject("The channel's lamps",typeof(MeshFilter),typeof(MeshRenderer));strip.transform.SetParent(root.transform,true);strip.GetComponent<MeshFilter>().sharedMesh=mesh;
                track.lampStrip=strip.GetComponent<MeshRenderer>();track.lampFrom=from;track.lampSpacing=spacing;track.lampCount=count;
                string texPath="Assets/_Project/Art/Generated/Ring/ring-channel-lamps.asset";var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if(texture==null||texture.width!=count){if(texture!=null)AssetDatabase.DeleteAsset(texPath);System.IO.Directory.CreateDirectory("Assets/_Project/Art/Generated/Ring");
                    texture=new Texture2D(count,1,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};AssetDatabase.CreateAsset(texture,texPath);}
                var pixels=new Color32[count];for(int k=0;k<count;k++)pixels[k]=track.Lamp(k,0);texture.SetPixels32(pixels);texture.Apply(false);EditorUtility.SetDirty(texture);
                track.lampStrip.sharedMaterial=ZoneKit.Toon("ring-channel-lamps",texture,m=>{m.SetColor("_BaseColor",Color.white);m.SetTexture("_EmissionMap",texture);m.SetColor("_EmissionColor",new Color(1.6f,1.6f,1.6f));m.SetFloat("_LightFloor",1);});
                Unshadowed(strip);
                // The cable under the lamps, and the banks: a pale lamp every three metres along each side, on a cable of its own.
                var cable=new ZoneKit.Shape();var line=new List<Vector3>();var radii=new List<float>();for(float a=from;a<=to;a+=3){line.Add(track.PointAt(a)+Vector3.down*1.05f);radii.Add(.26f);}
                cable.Slot(0).Tube(line,radii,6,new Color(.32f,.34f,.38f),new Color(.32f,.34f,.38f),1,.3f,false);
                var bank=new ZoneKit.Shape();
                foreach(int side in new[]{-1,1})
                {
                    var run=new List<Vector3>();var rr=new List<float>();
                    for(float z=ChannelFrom+track.sinkLength;z<=ChannelTo-track.sinkLength;z+=3){var at=new Vector3(side*BankX,Plane-.6f,z);bank.Slot(0).Box(at,new Vector3(.34f,.2f,.5f),Quaternion.identity,new Color(.62f,.74f,.86f));run.Add(at+Vector3.down*.4f);rr.Add(.16f);}
                    cable.Slot(0).Tube(run,rr,5,new Color(.3f,.32f,.36f),new Color(.3f,.32f,.36f),1,.3f,false);
                }
                var cables=kit.Built("Channel cables",cable,stuff.steel);cables.transform.SetParent(root.transform,true);Unshadowed(cables);
                var banks=kit.Built("Channel banks",bank,stuff.bank);banks.transform.SetParent(root.transform,true);Unshadowed(banks);
                return track;
            }

            // ---------------------------------------------------------------- a district
            /// <summary>One district: its deck and the kit's walls and roof, what it is known by, its cut stern chalked where it parted with the Crawl's engines under it, its lee's light on
            /// the plane, its name, its cover, its <c>RingDistrict</c> and its <see cref="SectionFlight"/>. Built about the origin heading north, then put at the start of its way.</summary>
            static SectionFlight Section(ZoneKit kit,Stuff stuff,District d)
            {
                var root=new GameObject("District: "+d.name);var t=root.transform;float hw=Wide*.5f,hl=Long*.5f;
                var s=new ZoneKit.Shape();var rng=new System.Random(700+Array.IndexOf(Districts,d)*37);
                // ---- the deck: the kit's floor plate over the hull's fascias, as chapter 1 built the Halo
                Piece(t,"DeckFloor",new Vector3(0,-.6f,0),new Vector3(Wide,.5f,Long));
                foreach(int side in new[]{-1,1}){Piece(t,"DeckWall",new Vector3(side*(hw-.28f),-2.1f,-hl*.5f),new Vector3(.55f,2.5f,hl));Piece(t,"DeckWall",new Vector3(side*(hw-.28f),-2.1f,hl*.5f),new Vector3(.55f,2.5f,hl));}
                Piece(t,"DeckWall",new Vector3(0,-2.1f,hl-.28f),new Vector3(Wide,2.5f,.55f));
                s.Slot(0).Box(new Vector3(0,-3.5f,0),new Vector3(Wide-.4f,.4f,Long-.4f),Quaternion.identity,Iron);
                // ---- the cut stern: a raw section where it parted, a chalk line along its edge with the joint's ticks, torn collar plates, and the Crawl's engines bolted under it
                Stern(s,d,hw,hl,rng);
                var flames=Engines(kit,stuff,t,s,hw,hl,out var engineLight);
                // ---- what it is known by
                switch(d.id)
                {
                    case "market":Market(kit,stuff,t,s,d,hw,hl,rng);break;
                    case "repair":Repair(kit,stuff,t,s,d,hw,hl,rng);break;
                    case "crescent":Crescent(kit,stuff,t,s,d,hw,hl,rng);break;
                    default:Dock(kit,stuff,t,s,d,hw,hl,rng);break;
                }
                var built=kit.Built("District "+d.id,s,stuff.steel,stuff.paint,stuff.glow,stuff.brass,stuff.soot);built.transform.SetParent(t,true);Unshadowed(built,true);
                // ---- its lee: its windows' light lying on the plane along its inner side, the length of it and a little more
                var lee=GameObject.CreatePrimitive(PrimitiveType.Quad);UnityEngine.Object.DestroyImmediate(lee.GetComponent<Collider>());lee.name="Its lee";lee.transform.SetParent(t,true);
                lee.transform.SetPositionAndRotation(new Vector3(d.Inner*(hw+6.2f),-.7f,0),Quaternion.Euler(90,0,0));lee.transform.localScale=new Vector3(12,Long+3,1);
                var leeView=lee.GetComponent<Renderer>();leeView.sharedMaterial=stuff.pool;leeView.shadowCastingMode=ShadowCastingMode.Off;leeView.receiveShadows=false;
                // The light itself, over the deck by its windows: what lights the stalls, the bay, the court and the cradles.
                var deckLight=new GameObject("Its windows' light",typeof(Light));deckLight.transform.SetParent(t,true);deckLight.transform.position=new Vector3(d.Inner*2,2.6f,-2.5f);
                var dl=deckLight.GetComponent<Light>();dl.type=LightType.Point;dl.range=13;dl.intensity=1.5f;dl.color=Warm;dl.shadows=LightShadows.None;
                // ---- its name, on the plane by its stern on the inner side, against its hull: 5.5 m out it read as a word floating in empty space (s60/250)
                WorldBuilder.Label(d.label,new Vector3(d.Inner*(hw+1.5f),Plane-1,-hl-1.5f),3).transform.SetParent(t,true);
                // ---- its cover: solid from 2 m under the plane to 4 m over (contract) in the shape it is drawn there, kinematic on its moving root
                var box=FitCover(t,"district-"+d.id,"Its cover",out var foot);
                var body=root.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;body.interpolation=RigidbodyInterpolation.None;
                var district=root.AddComponent<RingDistrict>();district.id=d.id;district.displayName=d.name;
                var flight=root.AddComponent<SectionFlight>();flight.districtId=d.id;flight.displayName=d.name;flight.path=PathOf(d);flight.exit=ExitOf(d);flight.launched=d.launched;flight.liftAt=d.liftAt;
                flight.cruise=3.8f;flight.easy=2.2f;flight.climb=3.6f;flight.ahead=30;flight.spacing=14;flight.clearance=.8f;flight.plane=Plane;flight.body=box;flight.footCentre=foot.center;flight.footSize=foot.size;
                flight.stationFlag="ring.formed";flight.releaseFlag="home.cutterDisabled";
                if(d.orrinLine>=0){flight.liftSpeaker="Orrin";flight.liftSituation="hail.section";flight.liftLine=d.orrinLine;}
                if(d.id=="repair"){flight.planeSpeaker="Hal";flight.planeSituation="hail.repair";flight.planeLine=0;}
                flight.barkHeroes=d.section.Select(x=>x.hero).ToArray();flight.barkLines=d.section.Select(x=>x.line).ToArray();flight.barkRange=12;
                flight.flames=flames;flight.engineLight=engineLight;flight.engineIntensity=2.4f;
                root.transform.position=flight.path[0];
                return flight;
            }
            /// <summary>Where a district parted from the station: its stern face raw (a dark section with the deck's layers showing), a chalk line along the deck's edge with the joint's
            /// ticks across it every metre and a half, and torn collar plates standing proud.</summary>
            static void Stern(ZoneKit.Shape s,District d,float hw,float hl,System.Random rng)
            {
                float z=-hl;
                s.Slot(4).Quad(new Vector3(-hw,-.35f,z-.02f),new Vector3(hw,-.35f,z-.02f),new Vector3(hw,-3.5f,z-.02f),new Vector3(-hw,-3.5f,z-.02f),Vector3.back,new Color(.3f,.3f,.33f));
                for(int k=0;k<5;k++){float y=-.9f-k*.6f;s.Slot(4).Beam(new Vector3(-hw,y,z-.08f),new Vector3(hw,y,z-.08f),.08f,.12f,new Color(.2f,.2f,.22f));}
                // The chalk: a line along the edge, and the joint's ticks across it.
                s.Slot(1).Quad(new Vector3(-hw,-.32f,z+.06f),new Vector3(hw,-.32f,z+.06f),new Vector3(hw,-.32f,z+.22f),new Vector3(-hw,-.32f,z+.22f),Vector3.up,Chalk);
                for(float x=-hw+.6f;x<hw;x+=1.5f)s.Slot(1).Quad(new Vector3(x-.06f,-.31f,z+.1f),new Vector3(x+.06f,-.31f,z+.1f),new Vector3(x+.06f,-.31f,z+.85f),new Vector3(x-.06f,-.31f,z+.85f),Vector3.up,Chalk);
                // Torn collar plates where the joint let go: the Halo's own grey, standing proud of the cut and bent a little.
                for(float x=-hw+1;x<hw-.5f;x+=2.2f+(float)rng.NextDouble()){float lean=((float)rng.NextDouble()-.5f)*30;s.Slot(0).Box(new Vector3(x,-1.4f,z-.25f),new Vector3(1.4f,1.6f,.18f),Quaternion.Euler(lean,0,(float)rng.NextDouble()*10),new Color(.5f,.52f,.56f));}
            }
            /// <summary>The Crawl's rig under the stern: an iron cradle with brass collars bolted across the cut, three nozzles out of it, and their flames (each a holder at the nozzle,
            /// so a burn scales from it), a soft flare at each mouth and one light.</summary>
            static Transform[] Engines(ZoneKit kit,Stuff stuff,Transform t,ZoneKit.Shape s,float hw,float hl,out Light light)
            {
                float z=-hl-.4f,y=-2.2f;var flames=new List<Transform>();
                s.Slot(4).Beam(new Vector3(-hw+.8f,y+.9f,z+.2f),new Vector3(hw-.8f,y+.9f,z+.2f),.5f,.5f,Soot);s.Slot(4).Beam(new Vector3(-hw+.8f,y-.9f,z+.2f),new Vector3(hw-.8f,y-.9f,z+.2f),.5f,.5f,Soot);
                foreach(float x in new[]{-hw+2.4f,0,hw-2.4f})
                {
                    var mouth=new Vector3(x,y,z-1.8f);
                    s.Slot(4).Tube(new[]{new Vector3(x,y,z+.6f),new Vector3(x,y,z-.6f),mouth},new[]{.62f,.8f,.95f},12,Iron,Iron,1,.3f);
                    s.Slot(3).Tube(new[]{mouth+Vector3.forward*.15f,mouth},new[]{1.04f,1.04f},14,Brass,Brass,1,.3f);
                    s.Slot(3).Beam(new Vector3(x,y+.9f,z+.2f),new Vector3(x,y,z+.2f),.3f,.3f,Brass);
                    var holder=new GameObject("Engine flame").transform;holder.SetParent(t,true);holder.position=mouth;
                    // Hot at the nozzle and black (adding nothing) at its point, so the cone fades into the dark rather than ending in an edge.
                    var cone=new ZoneKit.Shape().Slot(0).Tube(new[]{Vector3.zero,new Vector3(0,0,-1.6f),new Vector3(0,-.2f,-3.6f)},new[]{.82f,.55f,.05f},12,new Color(1f,.94f,.8f),new Color(0,0,0),1,.3f,false);
                    var flame=kit.Built("Flame "+t.name.Replace("District: ","")+" "+flames.Count,cone,stuff.flame);flame.transform.SetParent(holder,false);flame.transform.localPosition=Vector3.zero;Unshadowed(flame);
                    var flare=GameObject.CreatePrimitive(PrimitiveType.Quad);UnityEngine.Object.DestroyImmediate(flare.GetComponent<Collider>());flare.name="Engine flare";flare.transform.SetParent(holder,false);
                    flare.transform.localPosition=new Vector3(0,.3f,-.8f);flare.transform.rotation=Quaternion.Euler(ZoneKit.FlightPitch,0,0);flare.transform.localScale=Vector3.one*3.4f;
                    var fr=flare.GetComponent<Renderer>();fr.sharedMaterial=stuff.flare;fr.shadowCastingMode=ShadowCastingMode.Off;fr.receiveShadows=false;
                    flames.Add(holder);
                }
                var go=new GameObject("The engines' light",typeof(Light));go.transform.SetParent(t,true);go.transform.position=new Vector3(0,y+.5f,z-3.5f);
                light=go.GetComponent<Light>();light.type=LightType.Point;light.range=12;light.intensity=2.4f;light.color=new Color(1f,.9f,.72f);light.shadows=LightShadows.None;
                return flames.ToArray();
            }
            /// <summary>A hull block on a district's deck, of the kit's wall modules and its floor plate for a roof, from z <paramref name="from"/> to <paramref name="to"/>, so tall.
            /// Returns the roof's plate, which <see cref="Roof"/> paints over.</summary>
            static GameObject Block(Transform t,float hw,float from,float to,float tall)
            {
                float mid=(from+to)*.5f,depth=to-from,y=-.35f+tall*.5f;
                Piece(t,"DeckWall",new Vector3(0,y,from+.25f),new Vector3(hw*2,tall,.5f));Piece(t,"DeckWall",new Vector3(0,y,to-.25f),new Vector3(hw*2,tall,.5f));
                foreach(int side in new[]{-1,1})Piece(t,"DeckWall",new Vector3(side*(hw-.25f),y,mid),new Vector3(.5f,tall,depth-1));
                return Piece(t,"DeckFloor",new Vector3(0,-.35f+tall+.2f,mid),new Vector3(hw*2,.4f,depth));
            }


            /// <summary>What makes a district its own from the flight camera, thirty metres up (the lead, November 14: flown as a convoy they read as grey blocks, the kit's dark deck plate
            /// on every roof and deck). Its open deck lit as a street is at night: a light every metre and a half along both its edges and a lamp on each corner with its halo. All of it
            /// inside the district's footprint and under <see cref="RoofMost"/> over the plane; its cover is the outline of what is drawn at the plane, which none of it moves.</summary>
            static void Street(ZoneKit kit,Transform t,ZoneKit.Shape s,float hw,float hl,float deckTo)
            {
                for(float z=-hl+.7f;z<deckTo-.3f;z+=1.5f)foreach(int side in new[]{-1,1})s.Slot(2).Box(new Vector3(side*(hw-.18f),-.27f,z),new Vector3(.14f,.1f,.32f),Quaternion.identity,Warm);
                foreach(int side in new[]{-1,1})foreach(float z in new[]{-hl+.7f,deckTo-.6f})
                {
                    var foot=new Vector3(side*(hw-.45f),-.35f,z);s.Slot(4).Post(foot,2.45f,.06f,.05f,6,Soot,Soot);
                    var head=foot+Vector3.up*2.6f;s.Slot(2).Box(head,new Vector3(.26f,.2f,.26f),Quaternion.identity,Warm);
                    // Its halo stands over the band the cover is read from (a hand over 1.5 m above the plane), so the district's solid shape is what it was.
                    var halo=GameObject.CreatePrimitive(PrimitiveType.Quad);UnityEngine.Object.DestroyImmediate(halo.GetComponent<Collider>());halo.name="Street lamp halo";halo.transform.SetParent(t,false);
                    halo.transform.localPosition=head;halo.transform.rotation=Quaternion.Euler(ZoneKit.FlightPitch,0,0);halo.transform.localScale=Vector3.one*1.8f;
                    var r=halo.GetComponent<Renderer>();r.sharedMaterial=StreetHalo(kit);r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
                }
            }
            static Material streetHalo;
            static Material StreetHalo(ZoneKit kit)=>streetHalo!=null&&AssetDatabase.Contains(streetHalo)&&AssetDatabase.GetAssetPath(streetHalo).Contains(kit.zone)?streetHalo:streetHalo=kit.Glow("Street halo",false,new Color(1.5f,1.2f,.8f,1));
            /// <summary>A roof laid a hand over a hull block's dark kit plate (its top read off the piece itself), whole or in stripes along the district: what the flight camera sees of a
            /// district first.</summary>
            static float Roof(ZoneKit.Shape s,GameObject roof,int slot,float hw,float from,float to,Color[] stripes,float stripe)
            {
                float y=Mathf.Min(RoofMost-.06f,(roof!=null?ZoneKit.Drawn(roof).max.y:3)+.03f);
                int k=0;for(float x=-hw+.12f;x<hw-.13f;x+=stripe,k++){float x1=Mathf.Min(hw-.12f,x+stripe);s.Slot(slot).Quad(new Vector3(x,y,from+.12f),new Vector3(x,y,to-.12f),new Vector3(x1,y,to-.12f),new Vector3(x1,y,from+.12f),Vector3.up,stripes[k%stripes.Length]);}
                return y;
            }
            /// <summary>A line of lights slung between two points, a bulb every sixty centimetres in the colours given in turn: a market's strings over its stalls.</summary>
            static void Lights(ZoneKit.Shape s,Vector3 from,Vector3 to,float sag,Color[] bulbs)
            {
                s.Slot(4).Rope(from,to,sag,.025f,Soot,4,10);float length=Vector3.Distance(from,to);int n=Mathf.Max(2,Mathf.RoundToInt(length/.6f));
                for(int k=1;k<n;k++){float f=k/(float)n;var p=Vector3.Lerp(from,to,f)+Vector3.down*(sag*4*f*(1-f)+.08f);s.Slot(2).Box(p,Vector3.one*.13f,Quaternion.identity,bulbs[k%bulbs.Length]);}
            }

            /// <summary>The market: Mira's pitch nine and the promenade. Three stalls under striped awnings along the open deck, Jun's pears a crate past the line, the promenade's yellow
            /// line run along the stern's cut (its other half stayed with the office side: DecksLeaving), and the market hull behind with its shop windows lit and its trade on the roof.</summary>
            static void Market(ZoneKit kit,Stuff stuff,Transform t,ZoneKit.Shape s,District d,float hw,float hl,System.Random rng)
            {
                float z=-hl;
                // The yellow line, half of it, along the cut, just inside the chalk.
                s.Slot(1).Quad(new Vector3(-hw,-.31f,z+.24f),new Vector3(hw,-.31f,z+.24f),new Vector3(hw,-.31f,z+.62f),new Vector3(-hw,-.31f,z+.62f),Vector3.up,Line);
                // The promenade's runner up the deck to the hull's door, worn paler.
                s.Slot(1).Quad(new Vector3(d.Inner*3.6f-.9f,-.33f,z+.7f),new Vector3(d.Inner*3.6f+.9f,-.33f,z+.7f),new Vector3(d.Inner*3.6f+.9f,-.33f,1.2f),new Vector3(d.Inner*3.6f-.9f,-.33f,1.2f),Vector3.up,new Color(.5f,.54f,.58f));
                // The stalls, facing the camera across the promenade.
                foreach(var x in new[]{-3.9f,-.2f})Stand(kit,t,"MarketAwning",new Vector3(x,-.35f,-3.4f),2.4f,180);
                Stand(kit,t,"DressMarketStall",new Vector3(3.3f,-.35f,-4.6f),1.8f,180);Stand(kit,t,"DressMarketStall",new Vector3(-4.2f,-.35f,-6.4f),1.6f,200);
                foreach(var (x,zz) in new[]{(1.6f,-6.9f),(2.6f,-6.6f),(4.5f,-7.4f)}){var crate=Stand(kit,t,"DeckCrate",new Vector3(x,-.35f,zz),.75f,rng.Next(360));if(crate!=null&&x>4)Paint(crate,"pears",new Color(.82f,.9f,.5f));}
                Aboard(t,"Mira",new Vector3(.9f,-.35f,-5.6f),190);Aboard(t,"JunBody",new Vector3(4.2f,-.35f,-6.6f),200);
                // The market hull: its shop windows across its front, warm and cool, and its door on the promenade side.
                var roof=Block(t,hw,.4f,hl,3);
                foreach(var x in new[]{-4.4f,-1.6f,1.2f})Window(s,new Vector3(x*-d.Inner,1.2f,.12f),Vector3.right,2f,1.3f,Vector3.back,x<0?Warm:new Color(1f,.9f,.68f),Cream);
                Window(s,new Vector3(d.Inner*4.2f,.85f,.12f),Vector3.right,1.4f,2.2f,Vector3.back,Warm,Families[1]);
                // The market hall's roof is canvas, striped cream and the families' teal and gold: what says "market" from thirty metres up (the kit's dark plate said nothing).
                float top=Roof(s,roof,1,hw,.4f,hl,new[]{Cream,Families[0],Cream,Families[1]},1.1f);
                Picture(stuff,t,"PictoMarket",new Vector3(-1.4f*d.Inner,top+.02f,4.4f),3.4f);
                // Strings of lights over the stalls, from the hall's front to the lamps at the stern's corners and once across; a striped awning over the stalls on its outer side.
                var bulbs=new[]{Warm,Families[0],Warm,Families[1],Cream};
                foreach(int side in new[]{-1,1})Lights(s,new Vector3(side*(hw-.5f),2.3f,.25f),new Vector3(-side*(hw-.6f),2.2f,-hl+.75f),.35f,bulbs);
                Lights(s,new Vector3(-hw+.5f,2.15f,-3.4f),new Vector3(hw-.5f,2.15f,-3.4f),.25f,bulbs);
                for(float az=-hl+.9f;az<-1.4f;az+=.8f)
                {
                    var tone=Mathf.RoundToInt((az+hl)/.8f)%2==0?Cream:Families[0];float x0=-d.Inner*(hw-.15f),x1=-d.Inner*(hw-2.3f);
                    s.Slot(1).Quad(new Vector3(x0,2.35f,az),new Vector3(x0,2.35f,az+.8f),new Vector3(x1,1.95f,az+.8f),new Vector3(x1,1.95f,az),Vector3.up,tone);
                }
                Street(kit,t,s,hw,hl,.4f);
                Stand(kit,t,"DressSolarArray",new Vector3(3.6f*d.Inner,2.85f,3.2f),1.1f,90);Stand(kit,t,"DressSolarArray",new Vector3(3.6f*d.Inner,2.85f,6.2f),1.1f,90);
                Stand(kit,t,"DressCommsMast",new Vector3(-4.5f*d.Inner,2.85f,7.4f),3.6f,0);
            }
            /// <summary>The repair quarter: Hal's shop front. The workshop's wide doorway lit across the hull's front with its sign plate over it (the repair mark from chapter 1's roof), the
            /// receiving bay's crates and drum, the anvil out on the deck, and a gantry crane over the bay.</summary>
            static void Repair(ZoneKit kit,Stuff stuff,Transform t,ZoneKit.Shape s,District d,float hw,float hl,System.Random rng)
            {
                var roof=Block(t,hw,.4f,hl,3);
                // The shop front: the doorway (the kit's), its opening lit behind it, a sign plate lit over it with the repair mark on it, a window either side.
                var door=Piece(t,"DeckDoorway",new Vector3(d.Inner*1.2f,1.2f,.1f),new Vector3(3.6f,2.9f,.5f));
                Window(s,new Vector3(d.Inner*1.2f,.85f,.37f),Vector3.right,2.6f,2.2f,Vector3.back,Warm,Iron);
                s.Slot(2).Quad(new Vector3(d.Inner*1.2f-1.9f,2.25f,.06f),new Vector3(d.Inner*1.2f+1.9f,2.25f,.06f),new Vector3(d.Inner*1.2f+1.9f,2.85f,.06f),new Vector3(d.Inner*1.2f-1.9f,2.85f,.06f),Vector3.back,new Color(1f,.94f,.78f));
                Picture(stuff,t,"PictoRepair",new Vector3(d.Inner*1.2f,2.55f,.02f),.62f,Vector3.back);
                Window(s,new Vector3(-d.Inner*3.6f,1.3f,.12f),Vector3.right,2.2f,1.2f,Vector3.back,new Color(.86f,.95f,1f),Cream);Window(s,new Vector3(d.Inner*4.5f,1.3f,.12f),Vector3.right,1.2f,1.2f,Vector3.back,Warm,Cream);
                // A workshop's roof, sooted from its forge, a row of skylights along its front lit from the bench below, and the station's yellow along its edge.
                float top=Roof(s,roof,4,hw,.4f,hl,new[]{new Color(.3f,.3f,.33f)},12);
                foreach(float x in new[]{-4.1f,-1.3f,1.5f,4.3f})s.Slot(2).Quad(new Vector3(x-1.05f,top+.02f,1.1f),new Vector3(x-1.05f,top+.02f,2.4f),new Vector3(x+1.05f,top+.02f,2.4f),new Vector3(x+1.05f,top+.02f,1.1f),Vector3.up,Warm);
                s.Slot(1).Quad(new Vector3(-hw+.15f,top+.02f,.55f),new Vector3(-hw+.15f,top+.02f,.8f),new Vector3(hw-.15f,top+.02f,.8f),new Vector3(hw-.15f,top+.02f,.55f),Vector3.up,Line);
                Picture(stuff,t,"PictoRepair",new Vector3(-1.6f*d.Inner,top+.02f,4.6f),3.4f);
                Street(kit,t,s,hw,hl,.4f);
                Stand(kit,t,"DressFuelTank",new Vector3(3.8f*d.Inner,2.85f,6.4f),1.2f,0);Stand(kit,t,"DressPipeRun",new Vector3(1f*d.Inner,2.85f,6.8f),.6f,90);
                // The receiving bay: crates, a cable drum, the anvil out where the bench used to be wheeled, work lamps.
                Stand(kit,t,"LatticeAnvil",new Vector3(-d.Inner*2.6f,-.35f,-3.6f),1.2f,170);
                foreach(var (x,zz,h) in new[]{(-4.6f,-6.6f,1f),(-3.4f,-6.9f,.8f),(-4.4f,-5.4f,.8f),(4.4f,-6.8f,1f)})Stand(kit,t,"DeckCrate",new Vector3(x*d.Inner,-.35f,zz),h,rng.Next(360));
                Stand(kit,t,"DressCableDrum",new Vector3(d.Inner*4.6f,-.35f,-4.2f),.9f,30);Stand(kit,t,"DressWorkLamp",new Vector3(d.Inner*4.8f,-.35f,-1.6f),1.2f,200);Stand(kit,t,"DressWorkLamp",new Vector3(-d.Inner*5f,-.35f,-1.4f),1.2f,160);
                Aboard(t,"Hal",new Vector3(d.Inner*1.2f,-.35f,-.9f),180);Aboard(t,"TamBody",new Vector3(-d.Inner*1.6f,-.35f,-4.6f),150);
                // Hal's gantry crane over the bay: two posts and a beam in the Halo's cream with a dark band, its hook down over the anvil.
                foreach(int side in new[]{-1,1}){s.Slot(0).Beam(new Vector3(side*(hw-.5f),-.35f,-2.2f),new Vector3(side*(hw-.5f),4.4f,-2.2f),.34f,.34f,Cream);s.Slot(4).Beam(new Vector3(side*(hw-.5f),3.6f,-2.2f),new Vector3(side*(hw-.5f),3.9f,-2.2f),.4f,.4f,Soot);}
                s.Slot(0).Beam(new Vector3(-hw+.3f,4.55f,-2.2f),new Vector3(hw-.3f,4.55f,-2.2f),.36f,.4f,Cream);
                var hook=new Vector3(-d.Inner*2.6f,2.4f,-2.2f);s.Slot(4).Beam(hook,new Vector3(hook.x,4.35f,hook.z),.05f,.05f,Soot);s.Slot(3).Box(hook,new Vector3(.4f,.5f,.3f),Quaternion.identity,Brass);
                _=door;
            }
            /// <summary>The residential crescent: rows of cabins in an arc round a court open to the stern, three terraces stepping up away from the camera so every row's windows are seen,
            /// each window lit and framed in its family's colour with the family's door beside it, and laundry lines strung across the court.</summary>
            static void Crescent(ZoneKit kit,Stuff stuff,Transform t,ZoneKit.Shape s,District d,float hw,float hl,System.Random rng)
            {
                var centre=new Vector3(0,0,-hl-4);int family=0;
                foreach(var (radius,tall,cabins,spread) in new[]{(9.2f,1.3f,3,26f),(12.4f,2.1f,4,26f),(15.6f,3f,4,21f)})
                {
                    for(int k=0;k<cabins;k++)
                    {
                        float a=Mathf.Lerp(-spread,spread,cabins==1?.5f:k/(float)(cabins-1));var dir=Quaternion.Euler(0,a,0)*Vector3.forward;var face=centre+dir*radius;face.y=0;
                        var faceAt=new Vector3(face.x,-.35f+tall*.5f,face.z);var yaw=a+180;var across=Quaternion.Euler(0,a,0)*Vector3.right;float w=radius*2*Mathf.Sin(spread*Mathf.Deg2Rad)/Mathf.Max(1,cabins-1)*.98f;
                        if(cabins==1)w=4;w=Mathf.Min(w,4.2f);
                        var front=Piece(t,"DeckWall",faceAt+dir*.25f,new Vector3(w,tall,.5f));if(front!=null)front.transform.RotateAround(faceAt+dir*.25f,Vector3.up,a);
                        // The cabin's roof: a slab of dark plate back to the next row, so each terrace reads as a step.
                        // Roofed in the family's own colour (the station's dark plate on all of them read as one grey block from the flight camera).
                        s.Slot(0).Box(new Vector3(face.x,-.35f+tall+.12f,face.z)+dir*1.6f,new Vector3(w+.1f,.24f,3.2f),Quaternion.Euler(0,a,0),new Color(.36f,.4f,.46f),ZoneKit.Shape.Dim(Families[family%Families.Length],.86f));
                        var colour=Families[family++%Families.Length];var outward=-dir;
                        Window(s,faceAt+outward*.04f+across*(-w*.18f)+Vector3.up*(tall>1.5f?.15f:0),across,w*.42f,Mathf.Min(.9f,tall*.5f),outward,rng.NextDouble()<.8?Warm:Cool,colour);
                        s.Slot(1).Quad(faceAt+outward*.06f+across*(w*.24f)+Vector3.down*(tall*.5f-.02f),faceAt+outward*.06f+across*(w*.42f)+Vector3.down*(tall*.5f-.02f),
                            faceAt+outward*.06f+across*(w*.42f)+Vector3.down*(tall*.5f-1.15f*Mathf.Min(1,tall/1.3f)),faceAt+outward*.06f+across*(w*.24f)+Vector3.down*(tall*.5f-1.15f*Mathf.Min(1,tall/1.3f)),outward,colour);
                    }
                }
                Picture(stuff,t,"PictoCabins",new Vector3(0,2.88f,hl-2.6f),2.4f);
                // The back of the arc, behind the top terrace, closed to the deck's edge.
                Piece(t,"DeckWall",new Vector3(0,1.15f,hl-.28f),new Vector3(Wide,3,.5f));
                // Laundry across the court: two lines from the front row's ends to posts at the stern's corners, a family's washing on each.
                foreach(int side in new[]{-1,1})
                {
                    var post=new Vector3(side*(hw-.6f),-.35f,-hl+1.2f);s.Slot(4).Beam(post,post+Vector3.up*2.2f,.12f,.12f,Soot);
                    var from=post+Vector3.up*2.1f;var to=new Vector3(-side*2.2f,1.2f,-3.8f);s.Slot(4).Rope(from,to,.5f,.03f,new Color(.82f,.8f,.74f));
                    for(int k=1;k<6;k++)
                    {
                        float f=k/6f;var p=Vector3.Lerp(from,to,f)+Vector3.down*(.5f*4*f*(1-f));var colour=Families[(k+side+3)%Families.Length];float w=.45f+.25f*(float)rng.NextDouble(),h=.55f+.3f*(float)rng.NextDouble();
                        var acrossLine=(to-from).normalized;acrossLine.y=0;acrossLine.Normalize();
                        s.Slot(1).Quad(p-acrossLine*w*.5f,p+acrossLine*w*.5f,p+acrossLine*w*.5f+Vector3.down*h,p-acrossLine*w*.5f+Vector3.down*h,Vector3.back,colour);
                    }
                }
                // The court is the crescent's garden: grass between the rows, planters at its corners, and a tree the families have kept alive since the ring was built.
                s.Slot(1).Quad(new Vector3(-4.6f,-.33f,-8f),new Vector3(-4.6f,-.33f,-4.6f),new Vector3(4.6f,-.33f,-4.6f),new Vector3(4.6f,-.33f,-8f),Vector3.up,new Color(.34f,.56f,.3f));
                foreach(var (px,pz) in new[]{(-4.1f,-7.5f),(-3.4f,-5f),(3.3f,-5f)})
                {
                    s.Slot(3).Box(new Vector3(px,-.1f,pz),new Vector3(1.1f,.5f,.6f),Quaternion.identity,new Color(.52f,.4f,.3f),new Color(.6f,.46f,.34f));
                    for(int k=0;k<3;k++)s.Slot(1).Lump(new Vector3(px-.32f+k*.32f,.3f,pz),new Vector3(.26f,.24f,.24f),k+(int)(px*10),.25f,new Color(.42f,.68f,.36f),new Color(.24f,.4f,.22f),8,5);
                }
                var trunk=new Vector3(3.9f,-.35f,-7.6f);s.Slot(4).Tube(new[]{trunk,trunk+new Vector3(.05f,1.7f,0)},new[]{.15f,.1f},8,new Color(.42f,.32f,.24f),new Color(.5f,.38f,.28f));
                s.Slot(1).Lump(trunk+new Vector3(0,2.1f,0),new Vector3(1f,.72f,1f),71,.22f,new Color(.44f,.7f,.38f),new Color(.24f,.42f,.22f),10,6).Lump(trunk+new Vector3(-.6f,1.7f,.4f),new Vector3(.6f,.45f,.6f),72,.22f,new Color(.4f,.66f,.34f),new Color(.22f,.38f,.2f),8,5);
                Street(kit,t,s,hw,hl,-4.2f);
                Aboard(t,"OdaBody",new Vector3(-1.2f,-.35f,-6.2f),160);Aboard(t,"IloBody",new Vector3(2.4f,-.35f,-5.4f),200);
            }
            /// <summary>The tug dock: two cradles on the open deck, one with its tug still in it, lock nine's door in the dock office's front with its number painted large on the deck,
            /// the office's windows lit and the dock's mark on its roof.</summary>
            static void Dock(ZoneKit kit,Stuff stuff,Transform t,ZoneKit.Shape s,District d,float hw,float hl,System.Random rng)
            {
                var roof=Block(t,hw,3.4f,hl,2.6f);
                Piece(t,"DeckDoorway",new Vector3(d.Inner*3,1.05f,3.15f),new Vector3(3.2f,2.6f,.5f));Window(s,new Vector3(d.Inner*3,.75f,3.36f),Vector3.right,2.2f,1.9f,Vector3.back,Cool,Iron);
                foreach(var x in new[]{-.4f,-3.6f})Window(s,new Vector3(x*d.Inner,1.1f,3.34f),Vector3.right,1.8f,1f,Vector3.back,Warm,Cream);
                // The dock office's roof is a landing pad in the dock's blue-grey, ringed in chalk round its mark and lit along its edges.
                float top=Roof(s,roof,1,hw,3.4f,hl,new[]{new Color(.4f,.5f,.62f)},12);
                s.Slot(1).Disc(new Vector3(-1.6f*d.Inner,top+.02f,6.2f),Vector3.up,2.15f,32,Chalk,1.9f);
                for(float x=-hw+.4f;x<hw;x+=1.3f)foreach(float z in new[]{3.6f,hl-.25f})s.Slot(2).Box(new Vector3(x,top+.05f,z),new Vector3(.16f,.08f,.16f),Quaternion.identity,Cool);
                Picture(stuff,t,"PictoDock",new Vector3(-1.6f*d.Inner,top+.03f,6.2f),3f);
                Street(kit,t,s,hw,hl,3.4f);Stand(kit,t,"DressCommsMast",new Vector3(4.4f*d.Inner,2.45f,7.6f),3.4f,0);
                // Lock nine's number, painted large on the deck before its door: a seven-segment nine in the dock's cream.
                var o=new Vector3(d.Inner*3,-.31f,.2f);float w=1.5f,h=2.6f,b=.34f;
                void Seg(Vector3 a,Vector3 c)=>s.Slot(1).Quad(new Vector3(a.x,o.y,a.z),new Vector3(c.x,o.y,a.z),new Vector3(c.x,o.y,c.z),new Vector3(a.x,o.y,c.z),Vector3.up,Chalk);
                Seg(o+new Vector3(-w*.5f,0,h*.5f-b),o+new Vector3(w*.5f,0,h*.5f));Seg(o+new Vector3(-w*.5f,0,-b*.5f),o+new Vector3(w*.5f,0,b*.5f));Seg(o+new Vector3(-w*.5f,0,-h*.5f),o+new Vector3(w*.5f,0,-h*.5f+b));
                Seg(o+new Vector3(-w*.5f,0,0),o+new Vector3(-w*.5f+b,0,h*.5f));Seg(o+new Vector3(w*.5f-b,0,-h*.5f),o+new Vector3(w*.5f,0,h*.5f));
                // The cradles: a pair of ribbed arms each side of a keel rail, in the dock's grey-blue, one holding its tug.
                foreach(var (x,full) in new[]{(-d.Inner*2.9f,true),(d.Inner*2.2f,false)})
                {
                    var c=new Vector3(x,-.35f,-4);s.Slot(0).Box(c+Vector3.up*.15f,new Vector3(.5f,.3f,6),Quaternion.identity,new Color(.42f,.5f,.6f));
                    foreach(float zz in new[]{-2.2f,0,2.2f})foreach(int side in new[]{-1,1})
                    {
                        var foot=c+new Vector3(side*.4f,.25f,zz);var path=new List<Vector3>();var radii=new List<float>();
                        for(int k=0;k<=6;k++){float a=k/6f*80*Mathf.Deg2Rad;path.Add(foot+new Vector3(side*Mathf.Sin(a)*1.4f,1.4f-Mathf.Cos(a)*1.4f,0));radii.Add(.14f);}
                        s.Slot(0).Tube(path,radii,6,new Color(.46f,.56f,.68f),new Color(.6f,.68f,.78f),1,.3f);
                    }
                    s.Slot(2).Box(c+new Vector3(0,.45f,-3.2f),new Vector3(.4f,.2f,.2f),Quaternion.identity,Cool);
                    // The tug in its cradle, fitted to it (5.6 m long, 2.3 tall): fitted by height alone it stood out past the deck and over the roofs.
                    if(full&&ZoneKit.Landed("Skiff")){var tug=kit.Prop("Skiff",c+Vector3.up*.6f,4.6f,0,false,false);var fit=ZoneKit.Drawn(tug);tug.transform.localScale*=Mathf.Min(1,Mathf.Min(5.6f/Mathf.Max(.1f,Mathf.Max(fit.size.x,fit.size.z)),2.3f/Mathf.Max(.1f,fit.size.y)));var at=c+Vector3.up*1.2f;tug.transform.position+=at-ZoneKit.Drawn(tug).center;Paint(tug,"dock-tug",new Color(.72f,.92f,.84f));tug.transform.SetParent(t,true);}
                }
                Aboard(t,"SurvivorNatural",new Vector3(d.Inner*4.8f,-.35f,-6.8f),200);Aboard(t,"Neve",new Vector3(d.Inner*.2f,-.35f,-1.2f),170);
            }

            /// <summary>No district may be static: each flies, and a static-batched mesh doesn't move in a player.</summary>
            static void Unmoving(IEnumerable<SectionFlight> sections)
            {
                foreach(var section in sections)foreach(var t in section.GetComponentsInChildren<Transform>(true))GameObjectUtility.SetStaticEditorFlags(t.gameObject,0);
            }

            // ---------------------------------------------------------------- cover on the way
            /// <summary>A piece of the ring's deck torn away whole, adrift at the plane: a floor plate with its fascia hanging under it and its broken beams, solid (a tractor's line and a strike
            /// stop at it), the cover a pair's contract asks for within 20 m of its centre.</summary>
            static void CoverWreck(ZoneKit kit,Stuff stuff,string name,Vector3 at,float yaw,System.Random rng)
            {
                var root=new GameObject(name);root.transform.position=at;var turn=Quaternion.Euler(0,yaw,0);
                var plate=Piece(root.transform,"DeckFloor",at+Vector3.down*.2f,new Vector3(7,.5f,5));if(plate!=null){plate.transform.rotation=turn*Quaternion.Euler(-6,0,4);}
                // Hung from the plate's north edge: stood unturned and then turned about the wreck once (it was turned twice, and hung a metre and a half off the plate's edge).
                var fascia=Piece(root.transform,"DeckWall",at+new Vector3(0,-1.8f,2.2f),new Vector3(6.6f,3.2f,.5f));if(fascia!=null)fascia.transform.RotateAround(at,Vector3.up,yaw);
                var s=new ZoneKit.Shape();
                for(int k=0;k<4;k++){var a=at+turn*new Vector3(-3+k*2,-.6f,-2.4f);s.Slot(0).Beam(a,a+turn*new Vector3((float)rng.NextDouble()-.5f,-.4f-(float)rng.NextDouble(),-1.4f-(float)rng.NextDouble()*1.2f),.3f,.3f,new Color(.44f,.46f,.5f));}
                s.Slot(1).Quad(at+turn*new Vector3(-3.4f,.08f,-2.3f),at+turn*new Vector3(3.4f,.08f,-2.3f),at+turn*new Vector3(3.4f,.08f,-2.1f),at+turn*new Vector3(-3.4f,.08f,-2.1f),Vector3.up,Chalk);
                var built=kit.Built(name,s,stuff.steel,stuff.paint);built.transform.SetParent(root.transform,true);Unshadowed(built,true);
                var solid=FitCover(root.transform,name.ToLowerInvariant().Replace(' ','-'),name+" body",out _);solid.gameObject.isStatic=true;
            }

            /// <summary>What a hull flying at the plane meets of a piece: what is drawn of it from <see cref="CoverLow"/> under the plane to <see cref="CoverHigh"/> over it.</summary>
            internal const float CoverLow=-1,CoverHigh=1.5f;
            /// <summary>Cover in flight is the shape it looks (D272). A box over a barge's pointed bow stopped shots in clear space at Xylos; here the district's box stood over its cut stern
            /// and its cabins' ends stood out of it, and a wreck's box stood four metres over a plate a hand thick. Each piece is solid as one convex shape: the outline on the plan of what
            /// is drawn of it at the plane (its vertices between <see cref="CoverLow"/> and <see cref="CoverHigh"/>, the furthest each way round, as <see cref="XylosBuilder.FitHull"/>
            /// takes a barge's), stood from 2 m under the plane to 4 m over it (the Cutter's contract), on a child of <paramref name="root"/> called <paramref name="child"/>. Its lee's
            /// light, its name on the plane, the engines' flames and flares are light, not iron, and are left out. Returns the collider and, in <paramref name="foot"/>, the shape's box about
            /// the root (what a district is spaced by).</summary>
            internal static MeshCollider FitCover(Transform root,string asset,string child,out Bounds foot)
            {
                var points=new List<Vector2>();var toRoot=root.worldToLocalMatrix;
                foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
                {
                    var view=filter.GetComponent<MeshRenderer>();if(filter.sharedMesh==null||view==null||!view.enabled||NotIron(filter.transform,root))continue;
                    // Every triangle cut to the band, not only the corners inside it: a hull's long faces cross the band with their corners above and below it (the tug dock's tug
                    // stood out past a cover taken from corners alone).
                    var toWorld=filter.transform.localToWorldMatrix;var drawn=filter.sharedMesh;var v=drawn.vertices;var tri=drawn.triangles;var at=new Vector3[v.Length];
                    for(int i=0;i<v.Length;i++)at[i]=toRoot.MultiplyPoint3x4(toWorld.MultiplyPoint3x4(v[i]));
                    for(int i=0;i+2<tri.Length;i+=3)
                    {
                        var poly=Slab(Slab(new List<Vector3>{at[tri[i]],at[tri[i+1]],at[tri[i+2]]},CoverLow,true),CoverHigh,false);
                        foreach(var p in poly)points.Add(new Vector2(p.x,p.z));
                    }
                }
                if(points.Count<3)throw new InvalidOperationException(root.name+" draws nothing at the plane to be solid as");
                const int Sides=48;var ring=new List<Vector2>();
                for(int k=0;k<Sides;k++)
                {
                    float a=k*Mathf.PI*2/Sides;var u=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var far=points[0];float most=float.NegativeInfinity;
                    foreach(var p in points){float d=Vector2.Dot(p,u);if(d>most){most=d;far=p;}}
                    if(ring.Count==0||(ring[ring.Count-1]-far).sqrMagnitude>1e-5f)ring.Add(far);
                }
                if(ring.Count>1&&(ring[0]-ring[ring.Count-1]).sqrMagnitude<1e-5f)ring.RemoveAt(ring.Count-1);
                int n=ring.Count;if(n<3)throw new InvalidOperationException(root.name+"'s outline at the plane has "+n+" corners");
                var corners=new Vector3[n*2];var faces=new List<int>();
                for(int i=0;i<n;i++)
                {
                    corners[i]=new Vector3(ring[i].x,-2,ring[i].y);corners[n+i]=new Vector3(ring[i].x,4,ring[i].y);int next=(i+1)%n;
                    faces.AddRange(new[]{i,n+i,next,next,n+i,n+next});
                    if(i>0&&i<n-1){faces.AddRange(new[]{0,i,i+1});faces.AddRange(new[]{n,n+i+1,n+i});}
                }
                string path="Assets/_Project/Scenes/"+RingZone+"-cover-"+asset+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(mesh==null){mesh=new Mesh{name=RingZone+" cover "+asset};AssetDatabase.CreateAsset(mesh,path);}
                mesh.Clear();mesh.vertices=corners;mesh.triangles=faces.ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
                var go=new GameObject(child,typeof(MeshCollider));go.transform.SetParent(root,false);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;
                var solid=go.GetComponent<MeshCollider>();solid.sharedMesh=mesh;solid.convex=true;foot=mesh.bounds;
                return solid;
            }
            /// <summary>Reads the built scene and says, for each district and wreck, how far each drawn piece at the plane reaches past its cover's outline (marker RING_COVER_REPORT_OK).
            /// Changes nothing.</summary>
            internal static void CoverReport()
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+RingZone+".unity");
                var roots=UnityEngine.Object.FindObjectsByType<SectionFlight>(FindObjectsSortMode.None).Select(s=>s.transform).Concat(SceneManager.GetActiveScene().GetRootGameObjects().Where(g=>g.name.StartsWith("Cover wreck")).Select(g=>g.transform));
                foreach(var root in roots)
                {
                    var cover=root.GetComponentsInChildren<MeshCollider>().FirstOrDefault(c=>c.convex);if(cover==null){Debug.Log("RING_COVER "+root.name+" no fitted cover");continue;}
                    var shape=cover.sharedMesh.vertices.Select(v=>new Vector2(v.x,v.z)).ToList();var toRoot=root.worldToLocalMatrix;
                    float Support(Vector2 u)=>shape.Max(p=>Vector2.Dot(p,u));
                    foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
                    {
                        var view=filter.GetComponent<MeshRenderer>();if(filter.sharedMesh==null||view==null||!view.enabled||NotIron(filter.transform,root))continue;
                        var toWorld=filter.transform.localToWorldMatrix;var drawn=filter.sharedMesh;var v=drawn.vertices;var tri=drawn.triangles;var pts=new List<Vector2>();
                        for(int i=0;i+2<tri.Length;i+=3)foreach(var p in Slab(Slab(new List<Vector3>{toRoot.MultiplyPoint3x4(toWorld.MultiplyPoint3x4(v[tri[i]])),toRoot.MultiplyPoint3x4(toWorld.MultiplyPoint3x4(v[tri[i+1]])),toRoot.MultiplyPoint3x4(toWorld.MultiplyPoint3x4(v[tri[i+2]]))},CoverLow,true),CoverHigh,false))pts.Add(new Vector2(p.x,p.z));
                        if(pts.Count==0)continue;float worst=0;int at=0;
                        for(int deg=0;deg<360;deg+=10){var u=new Vector2(Mathf.Sin(deg*Mathf.Deg2Rad),Mathf.Cos(deg*Mathf.Deg2Rad));float over=pts.Max(p=>Vector2.Dot(p,u))-Support(u);if(over>worst){worst=over;at=deg;}}
                        if(worst>.3f)Debug.Log($"RING_COVER {root.name}: {filter.name} under {filter.transform.parent?.name} reaches {worst:0.00} m past the cover at {at} degrees (vertices {v.Length}, readable {drawn.isReadable})");
                    }
                    Debug.Log($"RING_COVER {root.name} cover corners {shape.Count/2} box {cover.sharedMesh.bounds.size}");
                    // The test's own measure (BrokenRingTests.EveryPieceOfCoverIsTheShapeItIsDrawnAtThePlane): probe colliders on what is drawn, parallel rays at six heights.
                    var probes=new List<GameObject>();
                    foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
                    {
                        var view=filter.GetComponent<MeshRenderer>();if(filter.sharedMesh==null||view==null||!view.enabled||NotIron(filter.transform,root))continue;
                        var probe=new GameObject("Cover probe "+filter.name+" of "+filter.transform.parent?.name){layer=31};probe.transform.SetParent(filter.transform,false);probe.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;probes.Add(probe);
                    }
                    Physics.SyncTransforms();var middle=new Vector2(root.position.x,root.position.z);
                    for(int deg=0;deg<360;deg+=10)
                    {
                        var u=new Vector2(Mathf.Sin(deg*Mathf.Deg2Rad),Mathf.Cos(deg*Mathf.Deg2Rad));var across=new Vector2(-u.y,u.x);float most=float.NegativeInfinity;string who="";
                        foreach(float h in new[]{-.9f,-.5f,0f,.5f,1f,1.4f})for(float s=-14;s<=14;s+=.25f)
                        {
                            var from=middle+u*20+across*s;
                            if(Physics.Raycast(new Vector3(from.x,root.position.y+h,from.y),new Vector3(-u.x,0,-u.y),out var hit,40,1<<31,QueryTriggerInteraction.Collide)){float d=Vector2.Dot(new Vector2(hit.point.x,hit.point.z)-middle,u);if(d>most){most=d;who=hit.collider.name+" at h "+h;}}
                        }
                        float over=most-Support(u);if(over>.3f)Debug.Log($"RING_COVER_RAY {root.name} {deg}: drawn {most:0.00}, cover {Support(u):0.00}, by {who}");
                    }
                    foreach(var p in probes)UnityEngine.Object.DestroyImmediate(p);
                }
                Debug.Log("RING_COVER_REPORT_OK");
            }
            /// <summary>A polygon cut to one side of a level plane: what of it lies over (<paramref name="keepAbove"/>) or under the height <paramref name="y"/>.</summary>
            static List<Vector3> Slab(List<Vector3> poly,float y,bool keepAbove)
            {
                var kept=new List<Vector3>();
                for(int i=0;i<poly.Count;i++)
                {
                    var a=poly[i];var b=poly[(i+1)%poly.Count];bool inA=keepAbove?a.y>=y:a.y<=y,inB=keepAbove?b.y>=y:b.y<=y;
                    if(inA)kept.Add(a);if(inA!=inB)kept.Add(Vector3.Lerp(a,b,(y-a.y)/(b.y-a.y)));
                }
                return kept;
            }
            static bool NotIron(Transform t,Transform root)
            {
                for(;t!=null&&t!=root;t=t.parent)
                {
                    string n=t.name;
                    if(n=="Its lee"||n=="Engine flare"||n=="Engine flame"||n.StartsWith("Flame ")||n.StartsWith("Sign_")||n=="Buoy halo"||t.GetComponent<TMPro.TMP_Text>()!=null)return true;
                }
                return false;
            }

            // ---------------------------------------------------------------- Bryn's tug
            /// <summary>The tug Welcome Back waiting off the fight's west side, nose to C, as Nacre's paint has her (green with cream trim, fenders round her, her tow boom off the stern), with
            /// the contract's <see cref="TowedOff"/>. Drawn only: she comes in through the limit once the Cutter is down.</summary>
            static void TugWaiting(ZoneKit kit)
            {
                var root=new GameObject("The tug Welcome Back");root.transform.SetPositionAndRotation(Tug,Quaternion.LookRotation(new Vector3(C.x-Tug.x,0,C.z-Tug.z)));
                if(ZoneKit.Landed("PatrolCutter"))
                {
                    var body=kit.Prop("PatrolCutter",Tug,9,root.transform.eulerAngles.y,false,false);body.transform.position+=Tug+Vector3.down*.4f-ZoneKit.Drawn(body).center;Paint(body,"tug",new Color(.5f,.9f,.78f));
                    body.transform.SetParent(root.transform,true);Unshadowed(body,true);
                }
                var s=new ZoneKit.Shape();var turn=root.transform.rotation;Color trim=new(.9f,.84f,.62f),soot=new(.18f,.19f,.2f);var c=Tug+Vector3.down*.4f;
                for(int k=0;k<8;k++){var f=c+turn*new Vector3((k%2==0?-1:1)*2.2f,.2f,-3.4f+(k/2)*2.2f);s.Slot(0).Tube(new[]{f+Vector3.up*.35f,f+Vector3.down*.35f},new[]{.36f,.36f},10,soot,soot,1,1);}
                // Her tow boom off the stern: a dark steel arm with cream bands, its stay down to the stern bollard and its hook block at the tip. A plain cream beam three metres long
                // read as a plank laid through whatever stood beside her (whole-game capture, November 20, s61/086 to 095).
                var heel=c+turn*new Vector3(0,1.1f,-3.1f);var tip=c+turn*new Vector3(0,2.5f,-5.3f);
                s.Slot(0).Beam(heel,tip,.2f,.2f,soot).Beam(c+turn*new Vector3(0,.7f,-3.6f),tip,.06f,.06f,soot);
                foreach(float t in new[]{.3f,.62f}){var band=Vector3.Lerp(heel,tip,t);s.Slot(0).Box(band,new Vector3(.26f,.26f,.16f),Quaternion.LookRotation(tip-heel),trim);}
                s.Slot(0).Box(tip+Vector3.down*.32f,new Vector3(.3f,.36f,.22f),turn,new Color(.3f,.31f,.34f)).Box(tip+Vector3.down*.56f,new Vector3(.08f,.18f,.18f),turn,trim);
                var built=kit.Built("The tug Welcome Back",s,ZoneKit.WorldMaterial("ring-steel"));built.transform.SetParent(root.transform,true);Unshadowed(built,true);
                var lamp=new GameObject("Her beacon",typeof(Light));lamp.transform.SetParent(root.transform,true);lamp.transform.position=Tug+Vector3.up*3;var l=lamp.GetComponent<Light>();l.type=LightType.Point;l.range=14;l.intensity=2;l.color=new Color(.7f,1,.86f);l.shadows=LightShadows.None;
                var away=new GameObject("Where she tows it").transform;away.position=TugAway;
                var towed=root.AddComponent<TowedOff>();towed.boss="DebrisCutter";towed.away=away;
                WorldBuilder.Label("WELCOME BACK",Tug+new Vector3(6,-1,-7),2.6f).transform.SetParent(root.transform,true);
            }

            // ---------------------------------------------------------------- the backdrop
            /// <summary>What is left of the ring: chapter 1's ring (r 62, the same deck plate over inner and outer fascia) 74 m under the approach's south end, its four sockets empty where the
            /// districts lifted out (the three still to go wait in theirs at load), each socket's edges chalked and its collar plates torn, and a few plates come loose drifting over it.</summary>
            static void RingLeft(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject("What is left of the ring");const int Spans=72;const float Width=5,Fascia=3.5f;float half=Mathf.Tan(Mathf.PI/Spans);
                var sockets=Districts.Select(d=>d.socket).ToArray();var s=new ZoneKit.Shape();
                for(int i=0;i<Spans;i++)
                {
                    float bearing=i*360f/Spans;if(sockets.Any(b=>Mathf.Abs(Mathf.DeltaAngle(b,bearing))<7))continue;
                    var mid=OnRing(bearing);var rot=Quaternion.Euler(0,bearing,0);
                    var deck=Piece(root.transform,"DeckFloor",mid+Vector3.up*(-.25f+(i%2)*.02f),new Vector3(2*(RingRadius+Width*.5f)*half+.08f,.5f,Width));if(deck!=null)deck.transform.rotation=rot;
                    foreach(int side in new[]{-1,1}){var face=Piece(root.transform,"DeckWall",mid+Quaternion.Euler(0,bearing,0)*Vector3.forward*side*(Width*.5f-.28f)+Vector3.up*(-.5f-Fascia*.5f),new Vector3(2*(RingRadius+side*Width*.5f)*half+.06f,Fascia,.55f));if(face!=null)face.transform.rotation=rot;}
                }
                foreach(float b in sockets)foreach(int side in new[]{-1,1})
                {
                    float edge=b+side*7;var e=OnRing(edge);var along=Quaternion.Euler(0,edge,0)*Vector3.right;var radial=Quaternion.Euler(0,edge,0)*Vector3.forward;
                    s.Slot(1).Quad(e-radial*Width*.5f+along*side*-.1f+Vector3.up*.02f,e+radial*Width*.5f+along*side*-.1f+Vector3.up*.02f,e+radial*Width*.5f+along*side*-.5f+Vector3.up*.02f,e-radial*Width*.5f+along*side*-.5f+Vector3.up*.02f,Vector3.up,Chalk);
                    for(int k=0;k<3;k++)s.Slot(0).Box(e+radial*(k-1)*1.6f+along*side*.4f+Vector3.down*1.2f,new Vector3(.2f,2,1.3f),Quaternion.Euler(0,edge,side*(10+k*8)),new Color(.5f,.52f,.56f));
                }
                var built=kit.Built("The ring's torn sockets",s,stuff.steel,stuff.paint);built.transform.SetParent(root.transform,true);Unshadowed(built,true);
                var rng=new System.Random(62);
                for(int k=0;k<14;k++)
                {
                    float bearing=150+(float)rng.NextDouble()*100;var at=OnRing(bearing,6+(float)rng.NextDouble()*18)+new Vector3(((float)rng.NextDouble()-.5f)*14,0,((float)rng.NextDouble()-.5f)*14);
                    string key=k%3==0?"DeckWall":"DeckFloor";var piece=WorldBuilder.Piece(key,at,key=="DeckWall"?new Vector3(4,3.5f,.55f):new Vector3(4.2f,.5f,5),"Rock",false);
                    piece.transform.rotation=Quaternion.Euler(rng.Next(360),rng.Next(360),rng.Next(360));piece.transform.SetParent(root.transform,true);
                    var tumble=piece.AddComponent<Tumble>();tumble.axis=new Vector3((float)rng.NextDouble()-.5f,1,(float)rng.NextDouble()-.5f).normalized;tumble.rate=1.5f+(float)rng.NextDouble()*3;
                }
                // The ring's own lights, a few still on: the station's work lamps on its inner fascia, cold.
                foreach(float b in new[]{176f,196,240,108})
                {var lamp=new GameObject("Ring work lamp",typeof(Light));lamp.transform.SetParent(root.transform,true);lamp.transform.position=OnRing(b,4);var l=lamp.GetComponent<Light>();l.type=LightType.Point;l.range=26;l.intensity=2.6f;l.color=new Color(.74f,.86f,1f);l.shadows=LightShadows.None;}
            }
            /// <summary>Spans of the ring itself, broken off where the districts tore out of it and adrift under the lanes and beyond the limits: each a run of the ring's own deck and both
            /// its fascias on the ring's own curve, tilted the way it came away, torn at its ends, with a gap where a span is gone and a pane or two still lit, a few turning slowly. The
            /// Ring was a starfield with crates in it (whole-game capture, November 20, s60/250, s61/050): this is the wreck the party flies through, the biggest thing in most frames and
            /// never at the plane (8 m and more under it in the lanes, a little less past the limits), each span kept off what must be read by where it shows.</summary>
            static void Adrift(ZoneKit kit,Stuff stuff,List<(Vector2 at,float clear)> keep)
            {
                var root=new GameObject("The ring's spans adrift");var rng=new System.Random(6262);const float Width=5,Fascia=3.5f,Step=5;int made=0,dropped=0;
                // Where each arc's middle lies, how deep, which way it runs, how many spans, how it is tilted, and whether it turns.
                var arcs=new (float x,float z,float y,float yaw,int spans,float pitch,float roll,bool turns)[]
                {
                    (-25,34,-11,24,7,8,-14,false),(26,62,-13,-35,6,-10,12,true),(-37,98,-15,70,8,6,18,false),(36,128,-11,-80,6,12,-10,false),
                    (-21,166,-13,-20,7,-8,-16,true),(24,206,-16,40,7,10,14,false),(-35,214,-10,-60,5,-14,8,false),
                    (-61,64,-5,10,8,4,-20,false),(63,152,-6,-15,8,-6,16,true),(-63,236,-6,30,7,8,12,false),(66,300,-5,-40,8,-10,-12,false),
                    // North of the fight, clear of the districts' ways down to the convoy (ExitOf), and two far under the fight itself.
                    (-66,330,-12,55,7,12,10,true),(64,350,-14,-65,6,-8,-18,false),(30,252,-21,15,6,6,-8,false),(-28,302,-22,-25,6,-10,10,false),
                }.ToList();
                // And a run of them down both sides of the whole approach and round the fight, one every twenty-odd metres each side, so a frame of the flight has one in it: the
                // fifteen above alone left most of the lane's frames empty sky.
                var strewn=new System.Random(6363);
                for(float z=12;z<370;z+=22)foreach(int s in new[]{-1,1})
                {
                    float x=s*(15+(float)strewn.NextDouble()*20),at=z+(s>0?11:0)+((float)strewn.NextDouble()-.5f)*8;
                    // Tilted less than the fifteen, and deeper: the districts fly the columns at x 30 over them, their engines four metres under the plane.
                    arcs.Add((x,at,-9.5f-(float)strewn.NextDouble()*5,(float)strewn.NextDouble()*360,4+strewn.Next(4),((float)strewn.NextDouble()-.5f)*14,((float)strewn.NextDouble()-.5f)*18,strewn.NextDouble()<.25));
                }
                foreach(var arc in arcs)
                {
                    // The group stands at the arc's middle, so one that turns turns about it.
                    var at=new Vector3(arc.x,Plane+arc.y,arc.z);var turn=Quaternion.Euler(arc.pitch,arc.yaw,arc.roll);
                    var group=new GameObject("A span of the ring, adrift").transform;group.position=at;group.SetParent(root.transform,true);
                    var lit=new ZoneKit.Shape();int gap=1+rng.Next(Mathf.Max(1,arc.spans-2));int kept=0;bool firstKept=false,lastKept=false,panes=false;
                    for(int k=0;k<arc.spans;k++)
                    {
                        if(k==gap&&arc.spans>5)continue;
                        // On the ring's own curve, about the arc's middle: its centre the ring's radius behind it.
                        float bearing=(k-(arc.spans-1)*.5f)*Step;var local=new Vector3(Mathf.Sin(bearing*Mathf.Deg2Rad)*RingRadius,0,-RingRadius+Mathf.Cos(bearing*Mathf.Deg2Rad)*RingRadius);
                        var world=at+turn*local;var spin=turn*Quaternion.Euler(0,bearing,0);
                        if(!ZoneKit.ClearOnScreen(world,3.2f,keep)){dropped++;continue;}
                        float half=Mathf.Tan(Step*.5f*Mathf.Deg2Rad);
                        var deck=Piece(group,"DeckFloor",world+spin*Vector3.up*(-.25f+(k%2)*.02f),new Vector3(2*(RingRadius+Width*.5f)*half+.08f,.5f,Width));if(deck!=null)deck.transform.rotation=spin;
                        foreach(int side in new[]{-1,1})
                        {
                            var face=Piece(group,"DeckWall",world+spin*(Vector3.forward*side*(Width*.5f-.28f)+Vector3.up*(-.5f-Fascia*.5f)),new Vector3(2*(RingRadius+side*Width*.5f)*half+.06f,Fascia,.55f));
                            if(face!=null)face.transform.rotation=spin;
                        }
                        // A pane still lit on the outer fascia, now and then.
                        if(rng.NextDouble()<.4){var centre=world+spin*(Vector3.forward*(Width*.5f+.02f)+Vector3.up*(-1.6f));Window(lit,centre,spin*Vector3.right,1.1f,.7f,spin*Vector3.forward,rng.NextDouble()<.3?Cool:Warm,Soot);panes=true;}
                        kept++;made++;if(k==0)firstKept=true;if(k==arc.spans-1)lastKept=true;
                    }
                    // Its torn ends: collar plates bent proud where the span parted (only where its end span is there to have parted).
                    foreach(int end in new[]{-1,1})
                    {
                        if(end<0&&!firstKept||end>0&&!lastKept)continue;
                        float bearing=end*((arc.spans-1)*.5f+.5f)*Step;var edge=at+turn*new Vector3(Mathf.Sin(bearing*Mathf.Deg2Rad)*RingRadius,0,-RingRadius+Mathf.Cos(bearing*Mathf.Deg2Rad)*RingRadius);
                        var spin=turn*Quaternion.Euler(0,bearing,0);
                        for(int j=0;j<3;j++)lit.Slot(1).Box(edge+spin*(Vector3.forward*(j-1)*1.6f+Vector3.down*1.2f),new Vector3(.2f,2,1.3f),spin*Quaternion.Euler(0,0,end*(12+j*9)),new Color(.5f,.52f,.56f));
                    }
                    if(kept==0){UnityEngine.Object.DestroyImmediate(group.gameObject);continue;}
                    // Slot 1 the torn plates and the panes' frames (the ring's steel), slot 2 the panes (lit).
                    // A material for each slot the shape has: an arc with no pane lit has none for the glow.
                    if(lit.Vertices>0){var drawn=panes?kit.Built("A span of the ring, adrift "+made,lit,stuff.steel,stuff.steel,stuff.glow):kit.Built("A span of the ring, adrift "+made,lit,stuff.steel,stuff.steel);drawn.transform.SetParent(group,true);}
                    Unshadowed(group.gameObject);
                    if(arc.turns){var tumble=group.gameObject.AddComponent<Tumble>();tumble.axis=(turn*Vector3.up+new Vector3(.2f,0,.1f)).normalized;tumble.rate=.35f+(float)rng.NextDouble()*.4f;}
                    else foreach(var t in group.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=true;
                }
                Debug.Log($"RING_ADRIFT_SPANS made={made} dropped={dropped}");
            }
            /// <summary>Wreckage 6 to 40 m under the plane over the whole zone, the ring's plate and fascia, crates and pallets, kept clear of what must be read by where it shows on screen.</summary>
            static void Below(ZoneKit kit,Stuff stuff,List<(Vector2 at,float clear)> keep)
            {
                var rng=new System.Random(4411);var root=new GameObject("Wreckage below");int made=0;
                string[] keys={"DeckFloor","DeckWall","DeckCrate","DressCargoPallet","RingSegmentA","RingSegmentB","DressFuelTank","DressCableDrum","DeckDoorway","DressPipeRun"};
                for(int k=0;k<360;k++)
                {
                    var at=new Vector3(-70+(float)rng.NextDouble()*140,Plane-6-(float)rng.NextDouble()*(float)rng.NextDouble()*34,-30+(float)rng.NextDouble()*420);string key=keys[rng.Next(keys.Length)];
                    float size=key.StartsWith("Ring")?5+(float)rng.NextDouble()*6:key=="DeckFloor"||key=="DeckWall"||key=="DeckDoorway"?2.5f+(float)rng.NextDouble()*2:1.2f+(float)rng.NextDouble()*1.6f;
                    var turned=Quaternion.Euler(rng.Next(360),rng.Next(360),rng.Next(360));
                    if(!ZoneKit.ClearOnScreen(at,size*1.2f,keep)||!ZoneKit.Landed(key))continue;
                    GameObject piece;
                    if(key=="DeckFloor"||key=="DeckWall"||key=="DeckDoorway"){var sz=key=="DeckFloor"?new Vector3(size,.4f,size*.9f):new Vector3(size,size*.8f,.5f);piece=WorldBuilder.Piece(key,at,sz,"Rock",false);piece.transform.rotation=turned;}
                    else piece=kit.Loose(key,at,size,turned);
                    piece.transform.SetParent(root.transform,true);piece.isStatic=true;foreach(var t in piece.GetComponentsInChildren<Transform>())t.gameObject.isStatic=true;Unshadowed(piece);made++;
                }
                Debug.Log("RING_BELOW made="+made);
                // Under the fight, pieces of homes adrift: torn deck and cabin fronts with a window still lit or a lamp still burning, so a frame of the fight has the Halo in it and
                // not only the Cutter (the lead, November 14). Kept clear of what must be read, by where each shows.
                var homes=new GameObject("Homes adrift under the fight");var lit=new ZoneKit.Shape();int adrift=0;
                for(int k=0;k<160&&adrift<34;k++)
                {
                    var at=new Vector3(-62+(float)rng.NextDouble()*124,Plane-10-(float)rng.NextDouble()*22,214+(float)rng.NextDouble()*130);bool wall=rng.NextDouble()<.5;float size=2.4f+(float)rng.NextDouble()*2;
                    var turned=Quaternion.Euler(rng.Next(-35,35),rng.Next(360),rng.Next(-35,35));
                    if(!ZoneKit.ClearOnScreen(at,size*1.3f,keep)||!ZoneKit.Landed(wall?"DeckWall":"DeckFloor"))continue;
                    var piece=WorldBuilder.Piece(wall?"DeckWall":"DeckFloor",at,wall?new Vector3(size,size*.8f,.5f):new Vector3(size,.4f,size*.9f),"Rock",false);piece.transform.rotation=turned;
                    piece.transform.SetParent(homes.transform,true);foreach(var t in piece.GetComponentsInChildren<Transform>())t.gameObject.isStatic=true;Unshadowed(piece);
                    var face=turned*(wall?Vector3.back:Vector3.up);var light=at+face*(wall?.3f:.25f);
                    lit.Slot(0).Quad(light+turned*new Vector3(-.5f,-.35f,0),light+turned*new Vector3(.5f,-.35f,0),light+turned*new Vector3(.5f,.35f,0),light+turned*new Vector3(-.5f,.35f,0),face,adrift%3==0?Cool:Warm);
                    adrift++;
                }
                if(lit.Vertices>0){var windows=kit.Built("Homes adrift, lit",lit,stuff.glow);windows.transform.SetParent(homes.transform,true);Unshadowed(windows);}
                Debug.Log("RING_ADRIFT "+adrift);
            }
            /// <summary>Many Hands, the convoy's hub, far down and ahead past the way home, turning slowly to take the districts in: its own hull (chapter 5's, built by ManyHandsBuilder) copied
            /// out of its scene at a third of its size, everything but what is drawn taken off it.</summary>
            static void Hub()
            {
                const string Path="Assets/_Project/Scenes/ManyHandsHull.unity";
                if(!System.IO.File.Exists(Path)){Debug.LogWarning("RING_HUB_MISSING "+Path);return;}
                var active=SceneManager.GetActiveScene();var hull=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);
                try
                {
                    var ship=hull.GetRootGameObjects().FirstOrDefault(g=>g.name=="The ship");if(ship==null){Debug.LogWarning("RING_HUB_MISSING The ship");return;}
                    var copy=UnityEngine.Object.Instantiate(ship);copy.name="Many Hands, turning";SceneManager.MoveGameObjectToScene(copy,active);
                    // Only what is drawn, as it stands in the city's own stills: switched-off states, colliders, lights, words and every behaviour come off.
                    foreach(var tr in copy.GetComponentsInChildren<Transform>(true).Reverse())if(tr!=null&&tr!=copy.transform&&!tr.gameObject.activeSelf)UnityEngine.Object.DestroyImmediate(tr.gameObject);
                    foreach(var c in copy.GetComponentsInChildren<Component>(true).Reverse())
                        if(c!=null&&!(c is Transform)&&!(c is MeshFilter)&&!(c is MeshRenderer))UnityEngine.Object.DestroyImmediate(c);
                    // A piece with no uvs under a textured material paints one texel's colour: in the city its collision body carries the uvs, and here nothing does. A hundred and
                    // fifty metres down at a third of its size it is a rim nobody can see: it goes.
                    int bare=0;
                    foreach(var f in copy.GetComponentsInChildren<MeshFilter>(true))
                    {
                        var r=f.GetComponent<MeshRenderer>();if(r==null||f.sharedMesh==null||f.sharedMesh.HasVertexAttribute(VertexAttribute.TexCoord0))continue;
                        if(!r.sharedMaterials.Any(m=>m!=null&&m.HasProperty("_BaseMap")&&m.GetTexture("_BaseMap")!=null))continue;
                        UnityEngine.Object.DestroyImmediate(r);UnityEngine.Object.DestroyImmediate(f);bare++;
                    }
                    int drawn=0;foreach(var r in copy.GetComponentsInChildren<MeshRenderer>()){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;drawn++;}
                    if(bare>0)Debug.Log("RING_HUB_BARE "+bare);
                    var pivot=new GameObject("Many Hands (the hub), turning");pivot.transform.position=new Vector3(30,-150,600);
                    copy.transform.SetParent(pivot.transform,true);copy.transform.localScale=Vector3.one*.36f;copy.transform.localPosition=Vector3.zero;copy.transform.localRotation=Quaternion.Euler(0,-30,0);
                    var turning=pivot.AddComponent<Tumble>();turning.axis=Vector3.up;turning.rate=1.2f;
                    foreach(var tr in pivot.GetComponentsInChildren<Transform>(true))tr.gameObject.isStatic=false;
                    Debug.Log("RING_HUB renderers="+drawn);
                }
                finally{EditorSceneManager.CloseScene(hull,true);SceneManager.SetActiveScene(active);}
            }
            /// <summary>The convoy's lights waiting further out, beyond Many Hands: hulls of every kind the convoy flies, each with its running lights, in a loose crowd far down.</summary>
            static void Convoy(ZoneKit kit,Stuff stuff)
            {
                var rng=new System.Random(910);var root=new GameObject("The convoy, waiting");string[] keys={"Hauler","Skiff","PatrolCutter","SeedBarge","TallowStationHull","DockingPodOffice"};var lights=new ZoneKit.Shape();
                for(int k=0;k<30;k++)
                {
                    var at=new Vector3(-180+(float)rng.NextDouble()*360,-110-(float)rng.NextDouble()*120,520+(float)rng.NextDouble()*240);string key=keys[rng.Next(keys.Length)];
                    float size=key=="TallowStationHull"?26:key=="SeedBarge"?16:key=="Hauler"?14:8;if(!ZoneKit.Landed(key))continue;
                    var hull=kit.Loose(key,at,size,Quaternion.Euler(rng.Next(-8,8),rng.Next(-40,40),rng.Next(-6,6)));hull.transform.SetParent(root.transform,true);Paint(hull,"convoy",new Color(.55f,.57f,.62f));Unshadowed(hull);
                    for(int j=0;j<3;j++){var p=at+new Vector3(((float)rng.NextDouble()-.5f)*size*.8f,size*.2f,((float)rng.NextDouble()-.5f)*size);lights.Slot(0).Box(p,Vector3.one*(.9f+size*.04f),Quaternion.identity,j==0?new Color(1f,.86f,.6f):new Color(.78f,.92f,1f));}
                }
                var built=kit.Built("The convoy's lights",lights,stuff.glow);built.transform.SetParent(root.transform,true);Unshadowed(built);
            }
        }
    }
}
