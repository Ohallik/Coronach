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
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Splines;

namespace Lattice.EditorTools
{
    /// <summary>
    /// The anchor chamber (zone StillwaterChamber, the final arenas; brief, "StillwaterChamber"): two arenas joined by a lift, the open frame gantry on the Continuance's moving exterior and the
    /// compact circular control well inside the anchor chamber, three clear cover anchors surviving every phase in each. Meret's second and third phases, her yielding, the crews coming in,
    /// Ishen's sequence, the last collar (one hold on Idra's call), the anchor letting go, Ilo taking her into custody, and the launch into the passage.
    /// <para>Map brief, every coordinate, both fights' contracts and the anchor: docs/maps/StillwaterChamber.md. Every place another file needs is a constant of <see cref="Chamber"/>.
    /// The gantry is built (a deck on a frame nine metres over the hull's back, rails all round, the lift's cage at its north edge and its rail running down over the back into a hatch);
    /// everything from the hatch on is one heightfield as the Continuance's decks are (the shaft, the landing at its foot, the well, the collar platform beyond it, the crews' two
    /// hatches), the hull's back round it all at <see cref="Chamber.HullBack"/>, drawn under the camera's real line on its side of every floor. North of the collar the bow is open:
    /// the chain hangs down the pit at <see cref="Chamber.A"/>, and through the opening the road lies far below with the convoy waiting at its mouth.</para>
    /// <para>Rebuild with <c>-mooring-zones StillwaterChamber</c>; for stills of the anchor let go (never saved as the zone: rebuild without it) add <c>-chamber-released</c>.</para>
    /// </summary>
    public static partial class LastMooringBuilder
    {
        /// <summary>The zone's own fields on its ZoneDef: its own air (tools/stillwater_ambience.py: a deep still well under the anchor's held weight, steel taking the load far down,
        /// water at the foot of the drop; not the ship's corridors, which it shared until November 14) and the release's cue.</summary>
        static void ChamberDefinition(ZoneDef z)
        {
            var bed=AssetDatabase.LoadAssetAtPath<AudioClip>(Chamber.Bed);if(bed!=null){z.ambientAudio=bed;z.ambientVolume=Chamber.BedLevel;}
            z.cueMusicIds=new[]{Chamber.ReleaseMusic};
        }

        /// <summary>The chamber's light, held: a cold high key off the Compact's white (the gantry is outside, under the convoy's sky; the well is lit by its own cold fixtures), cool fill,
        /// navy haze far off. The anchor's release warms the key at run time (AnchorRelease); the road's light comes up from below.</summary>
        [ZoneLook.Source] static void ChamberLooks()
        {
            ZoneLook.Register(ChamberZone,new ZoneLook.Spec{sun=C(.93f,.95f,1f),sunIntensity=.88f,sunPitch=56,shadowStrength=.76f,
                sky=C(.33f,.36f,.42f),equator=C(.28f,.29f,.32f),ground=C(.11f,.12f,.14f),
                fog=true,fogColor=C(.06f,.08f,.13f),fogStart=60,fogEnd=260,
                exposure=0,contrast=19,saturation=14,splitShadows=C(.42f,.46f,.54f),splitHighlights=C(.62f,.58f,.5f),
                bloomThreshold=.95f,bloomIntensity=.6f,vignette=.26f});
        }

        [SetsFlag("end.sequenceRead")][SetsFlag("end.collarTaken")][SetsFlag("end.custody")][SetsFlag("end.gearHeard")]
        internal static class Chamber
        {
            // ---------------------------------------------------------------- the numbers (docs/maps/StillwaterChamber.md reads these; so do the tests and the fights)
            /// <summary>Levels: the gantry's deck, the hull's back under it, the well and the collar, the pit and the bow's drop.</summary>
            internal const float Deck=0,HullBack=-9,Well=-16,Void=-70;
            internal const string GantrySpawn="Gantry",WellSpawn="Well",GantryDoor="MeretGantry",WellDoor="MeretWell",ReleaseMusic=DepartureTheme;
            internal const string LiftWords="Ride down — The Control Well",LiftLocked="The lift won't run while her frame is still up",LaunchWords="Launch — The Returning Passage";
            /// <summary>The zone's bed and the ZoneDef volume tools/stillwater_ambience.py measured for it.</summary>
            internal const string Bed="Assets/_Project/Audio/Ambience/AMB_StillwaterChamber.ogg";internal const float BedLevel=.337f;
            /// <summary>The gantry: its deck's bounds (x, z), the gangway and the landing the party docks on, her spawn, the three cover anchors, the lift's cage and her way out.</summary>
            internal const float DeckX=19,DeckZ0=-17,DeckZ1=17,SealZ=-17.4f,SealWidth=8;
            /// <summary>Her door's checkpoint (where the Continuance's dock sets the party down and a fall on the gantry retries) is on the gangway, nearer her than the zone's first arrival on the landing.</summary>
            internal static readonly Vector3 ArrivalAt=new(0,Deck,-30.5f),GantryAt=new(0,Deck,-21),MeretGantryAt=new(0,Deck,0),LiftTop=new(0,Deck,12.5f),GantryExit=new(0,Deck,12.5f);
            internal static readonly Vector3[] GantryCover={new(-10,Deck,.5f),new(-7.4f,Deck,-6.8f),new(10.4f,Deck,1.6f)};
            internal static readonly Vector3 GantryDoorCentre=new(0,Deck+2.5f,1.3f),GantryDoorSize=new(38,6,31.4f);
            /// <summary>The lift: its rail from the cage at the top, off the deck's north edge, down over the hull's back into the hatch, to the landing at the well's door.</summary>
            internal const float RampHead=16.5f,RampFoot=44.2f,HatchZ=32.1f,RampHalf=3.6f;
            internal static float RampY(float z)=>Mathf.Lerp(Deck,Well,Mathf.InverseLerp(RampHead,RampFoot,z));
            internal static readonly Vector3[] LiftPath={LiftTop,new(0,Deck,RampHead),new(0,Well,RampFoot),new(0,Well,46.4f)};
            internal static readonly Vector3 LiftFoot=new(0,Well,46.4f);
            /// <summary>The well: its middle W, its radius, the door's seal on the south rim, the crossing just inside it, the fight's box, the cover anchors, her spawn.</summary>
            internal static readonly Vector3 W=new(0,Well,63),WellAt=new(0,Well,47.6f),WellCrossing=new(0,Well,52.8f),WellDoorCentre=new(0,Well+2.5f,64.6f),WellDoorSize=new(25,6,20.8f);
            internal const float WellRadius=12,WellSealZ=51.2f,WellSealWidth=4.4f,NeckZ=74.6f,NeckWidth=7.2f;
            internal static readonly float[] WellCoverBearings={30,150,270};internal const float WellCoverR=6.5f;
            internal static Vector3 WellCover(int i)=>W+new Vector3(Mathf.Sin(WellCoverBearings[i]*Mathf.Deg2Rad),0,Mathf.Cos(WellCoverBearings[i]*Mathf.Deg2Rad))*WellCoverR;
            /// <summary>The anchor: the chain's middle A (its pit), the collar platform's radius, the four clamps (bearings from A and how far out), the last collar's seat, the braces.</summary>
            internal static readonly Vector3 A=new(0,Well,83.5f),CollarSeat=new(0,Well,78.4f);
            internal const float CollarRadius=9.5f,PitRadius=3,KerbRadius=3.9f,ClampR=5.8f;
            internal static readonly float[] ClampBearings={225,135,315,45};
            internal static Vector3 Clamp(int i)=>A+new Vector3(Mathf.Sin(ClampBearings[i]*Mathf.Deg2Rad),0,Mathf.Cos(ClampBearings[i]*Mathf.Deg2Rad))*ClampR;
            internal static Vector3 SmithPost(int i)=>A+new Vector3(Mathf.Sin(ClampBearings[i]*Mathf.Deg2Rad),0,Mathf.Cos(ClampBearings[i]*Mathf.Deg2Rad))*(ClampR+2.9f);
            internal static readonly Vector3[] Braces={new(-7.6f,Well,83.5f),new(7.6f,Well,83.5f)};
            /// <summary>The crews' two hatches off the collar platform, east and west; where Ishen stands to read; Meret's post on a later load; Ilo's and the civilian crews' places at the door.</summary>
            internal static readonly Vector3[] Hatches={new(-11,Well,83.5f),new(11,Well,83.5f)};
            internal static readonly Vector3 IshenReads=new(-2.9f,Well,76.4f),MeretPost=new(-3.2f,Well,66.5f),IloAt=new(1.2f,Well,54.2f),LaunchPad=new(5.6f,Well,47.6f);
            internal static readonly Vector3[] CiviliansAt={new(-1.8f,Well,53.4f),new(3.6f,Well,53.2f)};
            internal static readonly Vector3 WellCradle=new(-5.6f,Well,47.4f),GantryCradle=new(-5.8f,Deck,-29.5f);
            /// <summary>The road far below the bow, and the convoy at its mouth.</summary>
            internal static readonly Vector3 RoadMouth=new(0,-52,150);internal const float RoadRadius=24,RoadRing=12;internal const int RoadRings=16;
            /// <summary>What the view draws back to take in as the anchor lets go: the well's north half, the collar, the clamps, the bow and the road's near rings.</summary>
            internal static readonly Bounds ReleaseShows=new(new Vector3(0,-14,82),new Vector3(26,10,34));

            // ---------------------------------------------------------------- the plan (heightfield)
            internal enum Room{None,Shaft,Foot,Well,Collar,HatchW,HatchE}
            static float RoundBox(Vector2 p,float x0,float x1,float z0,float z1,float round)
            {
                var d=new Vector2(Mathf.Abs(p.x-(x0+x1)*.5f)-(x1-x0)*.5f+round,Mathf.Abs(p.y-(z0+z1)*.5f)-(z1-z0)*.5f+round);
                return Mathf.Min(Mathf.Max(d.x,d.y),0)+new Vector2(Mathf.Max(d.x,0),Mathf.Max(d.y,0)).magnitude-round;
            }
            static Vector2 P(Vector3 v)=>new(v.x,v.z);
            /// <summary>Metres outside the nearest heightfield floor (negative on one), and which. The gantry and the upper rail are built, not here.</summary>
            internal static float Nearest(Vector2 p,out Room room)
            {
                float best=float.PositiveInfinity;var found=Room.None;
                void Try(float c,Room r){if(c<best){best=c;found=r;}}
                Try(RoundBox(p,-RampHalf,RampHalf,HatchZ-1.2f,RampFoot+.4f,.1f),Room.Shaft);
                Try(RoundBox(p,-8,8,RampFoot,WellSealZ+.6f,.4f),Room.Foot);
                Try(Vector2.Distance(p,P(W))-WellRadius,Room.Well);
                Try(Mathf.Max(Vector2.Distance(p,P(A))-CollarRadius,PitRadius-Vector2.Distance(p,P(A))),Room.Collar);
                Try(RoundBox(p,-12.6f,-8.4f,81.4f,85.6f,.2f),Room.HatchW);Try(RoundBox(p,8.4f,12.6f,81.4f,85.6f,.2f),Room.HatchE);
                room=found;return best;
            }
            internal static float Level(Vector2 p,Room room)=>room==Room.Shaft?Mathf.Min(HullBack,RampY(Mathf.Max(p.y,HatchZ))):Well;
            internal static float FloorAt(Vector2 p,out float level){float c=Nearest(p,out var r);level=Level(p,r);return c;}
            internal static bool InPit(Vector2 p)=>Vector2.Distance(p,P(A))<PitRadius;
            /// <summary>Beyond the collar's north rail the bow is open: nothing under the opening but the drop.</summary>
            internal static bool Bow(Vector2 p)=>p.y>A.z+2&&Mathf.Abs(p.x)<9.4f&&Vector2.Distance(p,P(A))>CollarRadius-.01f||p.y>A.z+CollarRadius+.6f&&Mathf.Abs(p.x)<12;
            static readonly Vector2 Away=new(Mathf.Sin(SightlineAudit.Yaw*Mathf.Deg2Rad),Mathf.Cos(SightlineAudit.Yaw*Mathf.Deg2Rad));
            /// <summary>The ground bodies meet. A floor is its room's level; the pit and the bow drop away; elsewhere the chamber's plating up to the hull's back, which on the camera's side of a
            /// floor stays under the sightline to a hero at that floor's edge (the Waystation's rule, D119), and the hull's back itself out to the flanks.</summary>
            internal static float Height(Vector2 p)
            {
                float clear=FloorAt(p,out float level);if(clear<=0)return level;
                if(InPit(p)||Bow(p))return Void;
                float allowed=float.PositiveInfinity;
                for(float d=.5f;d<=16;d+=.5f){if(FloorAt(p+Away*d,out float there)<0){allowed=Mathf.Min(level,there)+.6f+.82f*d;break;}}
                float top=Mathf.Min(allowed,HullBack);
                return level+(top-level)*Mathf.SmoothStep(0,1,Mathf.Clamp01(clear/.9f));
            }
            static float SeenOver(float back)=>Mathf.Min(7,Mathf.Max(.6f,-.4f+.72f*back));
            /// <summary>The plating as drawn: a low kerb, then a slope under the line of sight (the Works' rule, D278).</summary>
            internal static float Seen(Vector2 p)
            {
                float stood=Height(p),clear=FloorAt(p,out float level);if(clear<=0||InPit(p)||Bow(p))return stood;
                float rise=Mathf.SmoothStep(0,1,Mathf.Clamp01(clear/.9f));
                for(float d=.5f;d<=16;d+=.5f)
                {
                    if(FloorAt(p+Away*d,out float there)>=0)continue;
                    return Mathf.Min(stood,level+(Mathf.Min(level,there)+SeenOver(d)-level)*rise);
                }
                return stood;
            }
            static Color Tone(Vector2 p)
            {
                float clear=Nearest(p,out var room);float mottle=.94f+.1f*Mathf.PerlinNoise(p.x*.13f+3,p.y*.13f+7);
                // The well's floor a deep slate-teal (the look pass of November 21: its plain pale tile, s67/099 to 120, gave a woman in a white coat nothing to stand out against), the
                // collar's a shade deeper, the landing's plating a mid grey.
                var tone=room switch{Room.Well=>new Color(.3f,.38f,.42f),Room.Collar=>new Color(.28f,.35f,.39f),Room.Shaft or Room.Foot=>new Color(.54f,.58f,.62f),_=>new Color(.7f,.74f,.76f)};
                if(clear>.2f)tone=new Color(.86f,.88f,.9f);
                // The well's ring of gold at its rim and the walk from the door: a ship that paints where its people go.
                float r=Vector2.Distance(p,P(W));if(room==Room.Well&&Mathf.Abs(r-(WellRadius-.7f))<.14f)tone=new Color(.82f,.68f,.36f);
                if(room==Room.Foot&&Mathf.Abs(p.x)<1.4f)tone=Color.Lerp(tone,new Color(.62f,.72f,.74f),.7f);
                tone=Color.Lerp(tone,tone*.74f,ZoneKit.Smooth(-1.4f,0,clear));
                return new Color(tone.r*mottle,tone.g*mottle,tone.b*mottle,1);
            }
            static Vector3 W3(float x,float y,float z)=>new(x,y,z);

            // ---------------------------------------------------------------- what it is made of (the Continuance's own plating, in the chamber's own materials)
            static Material FloorMat,KeelMat,DeckMat,WhiteMat,TealMat,GoldMat,DarkMat,HullMat,LampTeal,LampWhite,LampWarm,BraceMat,RoadMat,CoolMat,VoidMat;
            static readonly Color White=new(.95f,.96f,.97f),Pale=new(.84f,.87f,.89f),Teal=new(.18f,.56f,.58f),TealDeep=new(.1f,.34f,.38f),Gold=new(.86f,.7f,.38f),
                Iron=new(.3f,.32f,.35f),IronPale=new(.62f,.65f,.68f),Orange=new(.95f,.46f,.14f);
            static Texture2D Art(string name)=>AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Continuance/continuance-"+name+".png");
            static void Materials()
            {
                FloorMat=ZoneKit.Toon("chamber-floor",Art("deck"),m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_BaseColor",Color.white);m.SetFloat("_BaseMapStrength",.9f);m.SetColor("_ShadowTint",new Color(.5f,.56f,.7f));m.SetFloat("_LightFloor",.24f);});
                KeelMat=ZoneKit.Toon("chamber-keel",Art("keel"),m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.84f,.87f,.9f));m.SetColor("_ShadowTint",new Color(.46f,.52f,.66f));m.SetFloat("_LightFloor",.24f);});
                DeckMat=ZoneKit.Toon("chamber-deck",Art("deck"),m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetFloat("_BaseMapStrength",.85f);m.SetColor("_ShadowTint",new Color(.52f,.58f,.72f));m.SetFloat("_LightFloor",.3f);});
                WhiteMat=ZoneKit.Toon("chamber-white",Art("plate"),m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.98f,.98f,.99f));m.SetColor("_ShadowTint",new Color(.54f,.6f,.72f));m.SetFloat("_LightFloor",.32f);});
                TealMat=ZoneKit.Toon("chamber-teal",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_ShadowTint",new Color(.4f,.5f,.6f));m.SetFloat("_LightFloor",.3f);});
                GoldMat=ZoneKit.Toon("chamber-gold",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_ShadowTint",new Color(.5f,.46f,.5f));m.SetColor("_RimColor",new Color(.8f,.66f,.4f));m.SetFloat("_LightFloor",.32f);});
                DarkMat=ZoneKit.Toon("chamber-dark",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_ShadowTint",new Color(.42f,.46f,.58f));m.SetFloat("_LightFloor",.22f);});
                HullMat=ZoneKit.Toon("chamber-hull",Art("hull"),m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_ShadowTint",new Color(.5f,.56f,.7f));m.SetFloat("_LightFloor",.3f);});
                BraceMat=ZoneKit.Toon("chamber-brace",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",Color.white);m.SetColor("_ShadowTint",new Color(.55f,.42f,.4f));m.SetFloat("_LightFloor",.3f);});
                LampTeal=ZoneKit.Toon("chamber-lamp-teal",null,m=>{m.SetColor("_BaseColor",new Color(.6f,.96f,1));m.SetColor("_EmissionColor",new Color(.26f,1.4f,1.5f));});
                LampWhite=ZoneKit.Toon("chamber-lamp-white",null,m=>{m.SetColor("_BaseColor",new Color(.96f,.98f,1));m.SetColor("_EmissionColor",new Color(1.5f,1.55f,1.65f));});
                // Every material a fading piece is made of carries the fade's variant from its paint, so no later paint of it can take it away (D359: these drew their pieces solid while the rest faded).
                foreach(var fadeMat in new[]{LampTeal,LampWhite}){ZoneKit.With(fadeMat,"_OCCFADE_ON","_OccFadeToggle");fadeMat.SetFloat("_OccFade",0);}
                LampWarm=ZoneKit.Toon("chamber-lamp-warm",null,m=>{m.SetColor("_BaseColor",new Color(1,.92f,.78f));m.SetColor("_EmissionColor",new Color(1.6f,1.2f,.75f));});
                CoolMat=ZoneKit.Toon("chamber-coolant",null,m=>{m.SetColor("_BaseColor",new Color(.34f,.78f,.86f));m.SetColor("_EmissionColor",new Color(.08f,.62f,.78f));});
                // The road: one emissive the release drives (by property block, so an editor still shows it held unless built released).
                RoadMat=ZoneKit.Toon("chamber-road",null,m=>{m.SetColor("_BaseColor",new Color(.3f,.4f,.52f));m.SetColor("_EmissionColor",released?RoadFree:new Color(.12f,.2f,.32f));m.SetFloat("_LightFloor",.4f);});
                VoidMat=ZoneKit.Toon("chamber-void",null,m=>{m.SetColor("_BaseColor",new Color(.03f,.05f,.08f));m.SetColor("_EmissionColor",Color.black);m.SetFloat("_LightFloor",0);});
                AssetDatabase.SaveAssets();
            }
            static bool released;
            /// <summary>The road's light once the anchor has let go (AnchorRelease.roadFree).</summary>
            static readonly Color RoadFree=new(1.55f,1.22f,.8f);

            // ---------------------------------------------------------------- pieces
            static GameObject Made(ZoneKit kit,string name,ZoneKit.Shape shape,params Material[] materials){shape.Slot(materials.Length-1);shape.Slot(0);return kit.Built(name,shape,materials);}
            static Light PointLight(Vector3 at,float range,float intensity,Color colour,string name="Lamp light")
            {var go=new GameObject(name,typeof(Light));go.transform.position=at;var l=go.GetComponent<Light>();l.type=LightType.Point;l.color=colour;l.range=range;l.intensity=intensity;l.shadows=LightShadows.None;return l;}
            static void Gate(string name,string flag,bool showWhenSet,params GameObject[] targets)
            {var gate=new GameObject(name,typeof(FlagGate)).GetComponent<FlagGate>();gate.flag=flag;gate.showWhenSet=showWhenSet;gate.targets=targets.Where(t=>t!=null).ToArray();}
            /// <summary>A generated model stood as scenery (no brain, no collider), fitted to a height or a length, its feet at a point of the world.</summary>
            static GameObject Model(string folder,string id,Vector3 foot,float size,float yaw,bool byLength=false,string name=null)
            {
                var prefab=ZoneKit.Prefab(folder,id);if(prefab==null){Debug.LogWarning("CHAMBER_NO_MODEL "+folder+"/"+id);return null;}
                var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);PrefabUtility.UnpackPrefabInstance(body,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                foreach(var c in body.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
                foreach(var b in body.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null)UnityEngine.Object.DestroyImmediate(b);
                foreach(var a in body.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(a);
                body.transform.rotation=Quaternion.Euler(0,yaw,0);var box=ModelGeometry.BoundsOf(body);
                float span=byLength?Mathf.Max(box.size.x,box.size.z):box.size.y;body.transform.localScale*=size/Mathf.Max(.01f,span);
                box=ModelGeometry.BoundsOf(body);body.transform.position+=foot-new Vector3(box.center.x,box.min.y,box.center.z);body.name=name??id;
                return body;
            }
            /// <summary>Somebody with no lines, standing or walking: a body (their own, or a crew's tinted apart) in a capsule (it walks: a Guide or LeavesAfter moves it).</summary>
            static GameObject Person(string name,string folder,string id,Vector3 at,float yaw,Color tint,float scale=1,string fallback="Neve")
            {
                var prefab=ZoneKit.Prefab(folder,id)??ZoneKit.Prefab("Characters",fallback);if(prefab==null)return null;
                var person=new GameObject(name);person.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));
                var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab);model.transform.SetParent(person.transform,false);model.name="Generated civilian";model.transform.localScale*=scale;
                if(folder=="Enemies"){foreach(var c in model.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);foreach(var b in model.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null)UnityEngine.Object.DestroyImmediate(b);}
                foreach(var rig in model.GetComponentsInChildren<Animator>()){rig.cullingMode=AnimatorCullingMode.CullCompletely;PrefabUtility.RecordPrefabInstancePropertyModifications(rig);}
                var cap=person.AddComponent<CapsuleCollider>();cap.height=1.9f;cap.radius=.35f;cap.center=Vector3.up*.95f;
                if(tint!=Color.white)person.AddComponent<PropTint>().tint=tint;
                return person;
            }
            static Guide Walks(GameObject person,string flag,Vector3[] path,Vector3 faces)
            {
                if(person==null)return null;var guide=person.AddComponent<Guide>();
                guide.legs=new[]{new Guide.Leg{flag="",path=new[]{person.transform.position}},new Guide.Leg{flag=flag,path=path}};
                guide.faces=new[]{person.transform.position+person.transform.forward,faces};guide.speed=2.6f;guide.patience=4;return guide;
            }
            static RepairBay Cradle(ZoneKit kit,string name,Vector3 at,float yaw,string spawn)
            {
                var bay=kit.Prop("DeckConsole",new Vector3(at.x,0,at.z),1.4f,yaw).AddComponent<RepairBay>();bay.free=true;bay.saveSpawn=spawn;bay.prompt=name+" — repair and save";
                // The gantry's is on built decking, not the ground under it: stood on the deck's top.
                var box=ModelGeometry.BoundsOf(bay.gameObject);bay.transform.position+=Vector3.up*(at.y+.01f-box.min.y);
                bay.gameObject.AddComponent<PropTint>().tint=new Color(.96f,1,1);
                ChapterOneStaging.CradleBay(name,bay.gameObject,at,at.y+.02f,false,false);return bay;
            }
            /// <summary>A seal across a door, shut through a fight: two Compact door leaves meeting in the middle in a teal-lined frame, drawn only while shut, with a carving obstacle.</summary>
            static Membrane Seal(ZoneKit kit,string name,Vector3 foot,float width,float yaw=0,float tall=2.6f)
            {
                var seal=WorldBuilder.Membrane(name,foot+Vector3.up*tall*.5f,width,true,tall);seal.transform.rotation=Quaternion.Euler(0,yaw,0);
                foreach(var filter in seal.GetComponentsInChildren<MeshFilter>())UnityEngine.Object.DestroyImmediate(filter);
                foreach(var view in seal.GetComponentsInChildren<Renderer>())UnityEngine.Object.DestroyImmediate(view);
                var box=seal.GetComponent<BoxCollider>();var carve=seal.gameObject.AddComponent<NavMeshObstacle>();carve.shape=NavMeshObstacleShape.Box;carve.center=box.center;carve.size=box.size;carve.carving=true;carve.carveOnlyStationary=false;carve.enabled=false;
                var s=new ZoneKit.Shape();var lit=new ZoneKit.Shape();var turn=Quaternion.Euler(0,yaw,0);
                foreach(int side in new[]{-1,1})
                {
                    s.Slot(0).Box(foot+turn*Vector3.right*side*width*.25f+Vector3.up*tall*.5f,new Vector3(width*.5f-.06f,tall,.22f),turn,Pale,White);
                    // A blast door's leaves, as the Continuance's are (the look pass of November 23: s67/117's door was a plain tiled wall with two teal bars).
                    Continuance.SealLeaf(s,lit,foot,turn,side,width,tall);
                }
                var drawn=Made(kit,name+" leaves",s,WhiteMat,TealMat);drawn.transform.SetParent(seal.transform,true);
                var lamps=Made(kit,name+" lamps",lit,LampTeal,LampWarm);lamps.transform.SetParent(seal.transform,true);lamps.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                seal.SetOpen(true);return seal;
            }
            /// <summary>A cover anchor: a mooring bitt of the Compact's, a drum with a cap and a gold band, solid and nothing a blow can break (CombatCover reads its box).</summary>
            static GameObject Bitt(ZoneKit kit,string name,Vector3 foot,float wide,float tall)
            {
                // The look pass of November 23 (s67/117: a bare grey cylinder with a yellow cap): a capstan of the Compact's on a bolted iron foot, ribbed, a hawser wound on its
                // waist, a heavy iron head with its gold ring and a teal-lit crown where the cap was flat gold. Its solid box is as it was.
                var s=new ZoneKit.Shape();var lit=new ZoneKit.Shape();float r=wide*.5f;var iron=new Color(.3f,.33f,.37f);var ironTop=new Color(.5f,.54f,.58f);
                s.Slot(0).Box(foot+Vector3.up*.12f,new Vector3(wide+.5f,.24f,wide+.5f),Quaternion.identity,IronPale,Pale);
                s.Slot(3).Post(foot+Vector3.up*.24f,.14f,r*1.16f,r*1.1f,16,iron,ironTop);
                for(int k=0;k<8;k++){float a=(k*45+22.5f)*Mathf.Deg2Rad;s.Slot(3).Box(foot+new Vector3(Mathf.Sin(a)*r*1.04f,.4f,Mathf.Cos(a)*r*1.04f),new Vector3(.1f,.06f,.1f),Quaternion.Euler(0,k*45,0),ironTop);}
                s.Slot(0).Post(foot+Vector3.up*.38f,tall-.74f,r*.9f,r*.8f,16,Pale,White);
                for(int k=0;k<8;k++){float a=k*45*Mathf.Deg2Rad;var d=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));s.Slot(3).Box(foot+d*r*.86f+Vector3.up*(.38f+(tall-.74f)*.5f),new Vector3(.12f,tall-.8f,.1f),Quaternion.LookRotation(d),new Color(.56f,.6f,.64f),ironTop);}
                // The hawser: two and a half turns on its waist, dark.
                var turns=new List<Vector3>();var radii=new List<float>();for(int k=0;k<=40;k++){float t=k/40f,a=t*Mathf.PI*5;turns.Add(foot+new Vector3(Mathf.Sin(a)*r*.98f,.62f+t*.42f,Mathf.Cos(a)*r*.98f));radii.Add(.07f);}
                s.Slot(3).Tube(turns,radii,6,new Color(.2f,.24f,.27f),new Color(.2f,.24f,.27f),4,.5f);
                s.Slot(1).Post(foot+Vector3.up*(tall*.62f),.16f,r*.93f,r*.93f,16,Teal,Teal);
                s.Slot(3).Post(foot+Vector3.up*(tall-.4f),.4f,r*1.12f,r*1.18f,16,iron,ironTop);
                s.Slot(2).Post(foot+Vector3.up*(tall-.24f),.07f,r*1.2f,r*1.2f,16,Gold,Gold);
                s.Slot(3).Disc(foot+Vector3.up*(tall+.005f),Vector3.up,r*1.18f,16,new Color(.36f,.4f,.44f),r*.56f,ironTop);
                lit.Slot(0).Disc(foot+Vector3.up*(tall+.012f),Vector3.up,r*.56f,16,Color.white,r*.38f);
                s.Slot(3).Disc(foot+Vector3.up*(tall+.008f),Vector3.up,r*.38f,16,new Color(.14f,.17f,.2f));
                var bitt=Made(kit,name,s,WhiteMat,TealMat,GoldMat,DarkMat);bitt.isStatic=true;
                var crown=Made(kit,name+" crown",lit,LampTeal);crown.transform.SetParent(bitt.transform,true);crown.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                ZoneKit.Block(bitt,foot+Vector3.up*tall*.5f,new Vector3(wide,tall,wide),0,name+" (solid)");
                return bitt;
            }
            /// <summary>Rails along a run of deck: posts every two metres, a gold handrail and a teal mid rail, solid to the bodies.</summary>
            static void Rail(ZoneKit kit,ZoneKit.Shape rails,GameObject owner,Vector3 a,Vector3 b)
            {
                float length=Vector3.Distance(a,b);int posts=Mathf.Max(1,Mathf.RoundToInt(length/2));
                for(int i=0;i<=posts;i++)rails.Slot(0).Post(Vector3.Lerp(a,b,i/(float)posts),1.1f,.05f,.04f,6,IronPale,Pale);
                rails.Slot(1).Beam(a+Vector3.up*1.08f,b+Vector3.up*1.08f,.08f,.08f,Gold);rails.Slot(2).Beam(a+Vector3.up*.55f,b+Vector3.up*.55f,.05f,.05f,Teal);
                var run=b-a;var mid=(a+b)*.5f;var box=ZoneKit.Block(owner,mid+Vector3.up*.55f,new Vector3(.3f,1.7f,length+.3f),Mathf.Atan2(run.x,run.z)*Mathf.Rad2Deg,"Rail");
                box.transform.rotation=Quaternion.LookRotation(run.normalized,Vector3.up);
            }

            // ---------------------------------------------------------------- the zone
            /// <summary>Builds StillwaterChamber.</summary>
            internal static bool Build()
            {
                released=DevArgs.Has("-chamber-released");
                Materials();
                var kit=ZoneKit.Ground(ChamberZone,Height,1.2f);
                var scene=WorldBuilder.Begin(ChamberZone);
                var key=GameObject.Find("KeyLight");if(key!=null)key.transform.rotation=Quaternion.Euler(56,214,0);
                var land=kit.Terrain("Anchor chamber",-46,46,-40,100,.5f,FloorMat,KeelMat,Tone,new Color(.94f,.95f,.97f),new Color(.78f,.8f,.84f),Seen,p=>Bow(p)||InPit(p)?-1:-FloorAt(p,out _));

                // ---- the gantry, the lift, the hull's back
                var spawnGantry=WorldBuilder.Spawn(GantrySpawn,GantryAt);spawnGantry.transform.rotation=Quaternion.identity;
                var arrival=WorldBuilder.Spawn("Arrival",ArrivalAt);arrival.transform.rotation=Quaternion.identity;
                var well=WorldBuilder.Spawn(WellSpawn,WellAt);well.transform.rotation=Quaternion.identity;
                var gantry=Gantry(kit,out var gantrySeal);
                HullOutside(kit);
                var lift=Lift(kit);
                Cradle(kit,"Gantry cradle",GantryCradle,90,GantrySpawn);
                WorldBuilder.Label("FRAME GANTRY",W3(-4.2f,Deck+.1f,-14.6f),1.9f);

                // ---- phase two's door (docs/maps/StillwaterChamber.md, "Boss arena contract: the gantry")
                var door=WorldBuilder.Encounter(GantryDoor,GantryDoorCentre,GantryDoorSize,Foe("MeretGantry","CompactWarden"),1,new[]{gantrySeal});
                var post=door.spawners[0];post.transform.SetPositionAndRotation(MeretGantryAt,Quaternion.Euler(0,180,0));post.faceAsPlaced=true;post.radius=0;
                door.requiredFlag="end.meretShip";door.checkpointSpawn=GantrySpawn;
                var show=new GameObject("Her frame on show (the dock)").transform;show.SetParent(door.transform,false);show.position=GantryAt;door.previewPoint=show;door.previewRadius=Vector3.Distance(GantryAt,MeretGantryAt)+4;
                var exit=new GameObject("Her way down (the lift)",typeof(BossExit)).GetComponent<BossExit>();exit.transform.position=GantryExit;exit.boss=GantryDoor;exit.sinks=true;

                // ---- the well: its door, the crossing at the lift's foot, phase three's door
                var anchor=WellRoom(kit,out var wellSeal,out var neckSeal);
                Cradle(kit,"Well cradle",WellCradle,90,WellSpawn);
                // On the walk before the door, clear of the cradle's bollards (the look pass of November 21: at (-3.6, 49.2) the east bollard stood in its middle, "CONTR WELL", s67/099).
                kit.Sign("CONTROL WELL",0,50,1.6f);
                var duel=WorldBuilder.Encounter(WellDoor,WellDoorCentre,WellDoorSize,Foe("MeretWell","CompactWarden"),1,new[]{wellSeal,neckSeal});
                var wellPost=duel.spawners[0];wellPost.transform.SetPositionAndRotation(W,Quaternion.Euler(0,180,0));wellPost.faceAsPlaced=true;wellPost.radius=0;
                duel.requiredFlag="end.wellEntered";duel.checkpointSpawn=WellSpawn;
                var wellShow=new GameObject("Meret on show (the door)").transform;wellShow.SetParent(duel.transform,false);wellShow.position=W3(0,Well,WellSealZ);duel.previewPoint=wellShow;duel.previewRadius=16;
                var meets=kit.Crossing("Meret in the control well",W3(WellCrossing.x,0,WellCrossing.z),new Vector3(9,5,2.2f),"end.wellEntered","MeretWell","Meret","bossdown.MeretGantry");
                meets.saveSpawn=WellSpawn;meets.previewEncounter=duel;

                // ---- after her: she stands; the crews; the sequence; the collar; the anchor; Ilo
                After(kit,anchor);
                // The way on: once Ilo has her, the party's ships are at the landing.
                var pad=kit.Prop("LandingPad",new Vector3(LaunchPad.x,0,LaunchPad.z),4.2f,0);pad.name=LaunchWords;
                var launch=pad.AddComponent<DockingPad>();launch.prompt=LaunchWords;launch.range=3.6f;launch.scene=PassageZone;launch.spawn="Arrival";
                var launchSign=WorldBuilder.Label(LaunchWords,kit.On(LaunchPad.x,LaunchPad.z-3.4f,.3f),2.2f);
                Gate("Once Ilo has her: the launch",CustodyFlag,true,pad,launchSign);

                // ---- limits: rails round the gantry, the chamber's plating; boxes so nobody finds a way up a wall or off the world
                WorldBuilder.Boundary("South limit",new Vector3(0,0,-39),new Vector3(92,60,2));WorldBuilder.Boundary("North limit",new Vector3(0,-20,99),new Vector3(92,90,2));
                WorldBuilder.Boundary("West limit",new Vector3(-45,-10,30),new Vector3(2,80,140));WorldBuilder.Boundary("East limit",new Vector3(45,-10,30),new Vector3(2,80,140));
                WorldBuilder.Boundary("The bow's rail (beyond it)",new Vector3(0,-14,A.z+CollarRadius+1.2f),new Vector3(26,6,1));

                // ---- navigation: every seal open (what is shut at first is play's business)
                var reach=new List<Vector3>{GantryAt,ArrivalAt,ArrivalAt+Vector3.right*2,GantryCradle+new Vector3(1.6f,0,0),W3(0,0,-12),W3(-14,0,-12),W3(14,0,-12),W3(-14,0,12),W3(14,0,12),MeretGantryAt+Vector3.back*3,
                    LiftTop+Vector3.back*2,W3(0,RampY(24),24),W3(0,RampY(38),38),LiftFoot,WellAt,WellCradle+new Vector3(1.6f,0,0),LaunchPad+Vector3.left*1.6f,WellCrossing,W+Vector3.back*3,W+Vector3.left*9,W+Vector3.right*9,W+Vector3.forward*9,
                    CollarSeat,CollarSeat+Vector3.left*2,IshenReads,Braces[0]+Vector3.right*1.2f,Braces[1]+Vector3.left*1.2f,Hatches[0],Hatches[1]};
                foreach(var p in GantryCover)reach.Add(p+Vector3.back*1.6f);for(int i=0;i<3;i++)reach.Add(WellCover(i)+(W-WellCover(i)).normalized*1.6f);
                for(int i=0;i<4;i++)reach.Add(SmithPost(i));
                kit.BakeNavigation(new Bounds(new Vector3(0,-30,30),new Vector3(94,110,142)),ArrivalAt,reach);
                if(released)Debug.Log("CHAMBER_STILL_RELEASED: never save this build as the zone; rebuild without -chamber-released");
                Ground(kit,land);
                WorldBuilder.Save(scene,kit.zone);
                Debug.Log("MOORING_COORDS StillwaterChamber gantry="+GantryAt+" meret="+MeretGantryAt+" lift="+LiftTop+"->"+LiftFoot+" well="+W+" r="+WellRadius+" seal z="+WellSealZ+" crossing="+WellCrossing+" A="+A+" seat="+CollarSeat+" pad="+LaunchPad);
                Debug.Log("CHAMBER_OK");
                return true;
            }
            const string CustodyFlag="end.custody";

            // ---------------------------------------------------------------- the frame gantry
            /// <summary>The deck on its frame over the hull's back: Compact plate in two-metre squares, a teal kerb and gold line round it, rails on every edge but the gangway's mouth and the
            /// lift's; the coolant runs along both sides with a sluice head every six metres (the flood's source), grated drains across it, the frame's two legs and its beam at the
            /// north edge with the maintenance arms parked on it; the gangway and the landing the party docked at, a gold docking ring painted on it. Floods on the legs, lamps at the kerb.</summary>
            static GameObject Gantry(ZoneKit kit,out Membrane seal)
            {
                var root=new GameObject("The frame gantry");var s=new ZoneKit.Shape();var trim=new ZoneKit.Shape();var glow=new ZoneKit.Shape();var cool=new ZoneKit.Shape();var frame=new ZoneKit.Shape();
                const float thick=.5f;
                void Plate(float x0,float x1,float z0,float z1){s.Slot(0).Box(W3((x0+x1)*.5f,Deck-thick*.5f,(z0+z1)*.5f),new Vector3(x1-x0,thick,z1-z0),Quaternion.identity,IronPale,new Color(.82f,.85f,.87f),.5f);}
                Plate(-DeckX,DeckX,DeckZ0,DeckZ1);Plate(-4,4,-22,DeckZ0+.01f);Plate(-8,8,-34,-21.9f);
                // The kerb's teal band and its gold line, a hand inside each edge.
                foreach(var (a,b) in new[]{(W3(-DeckX+.4f,0,DeckZ0+.4f),W3(-4.4f,0,DeckZ0+.4f)),(W3(4.4f,0,DeckZ0+.4f),W3(DeckX-.4f,0,DeckZ0+.4f)),(W3(-DeckX+.4f,0,DeckZ0+.4f),W3(-DeckX+.4f,0,DeckZ1-.4f)),
                    (W3(DeckX-.4f,0,DeckZ0+.4f),W3(DeckX-.4f,0,DeckZ1-.4f)),(W3(-DeckX+.4f,0,DeckZ1-.4f),W3(-RampHalf-.4f,0,DeckZ1-.4f)),(W3(RampHalf+.4f,0,DeckZ1-.4f),W3(DeckX-.4f,0,DeckZ1-.4f))})
                {trim.Slot(0).Beam(a+Vector3.up*.012f,b+Vector3.up*.012f,.5f,.02f,Teal);trim.Slot(1).Beam(a+Vector3.up*.014f+(a.x<0&&Mathf.Abs(a.x-b.x)<.1f?Vector3.right:a.x>0&&Mathf.Abs(a.x-b.x)<.1f?Vector3.left:a.z<0?Vector3.forward:Vector3.back)*.45f,b+Vector3.up*.014f+(a.x<0&&Mathf.Abs(a.x-b.x)<.1f?Vector3.right:a.x>0&&Mathf.Abs(a.x-b.x)<.1f?Vector3.left:a.z<0?Vector3.forward:Vector3.back)*.45f,.1f,.02f,Gold);}
                // Her working deck painted as a yard that builds to drawings marks its floor (the look pass of November 14: one pale blue-grey sheet): the four bays either side of the
                // walk in the Compact's deep teal-grey, each plate a little its own, a layout grid in fine pale lines over them; under the drains, the walk, her ring and the kerb.
                {
                    var bays=new ZoneKit.Shape();
                    foreach(int sx in new[]{-1,1})foreach(var (z0,z1) in new[]{(DeckZ0+1.3f,-.6f),(.6f,DeckZ1-1.3f)})
                    {
                        float x0=sx<0?-DeckX+1.3f:1.7f,x1=sx<0?-1.7f:DeckX-1.3f;
                        for(float x=x0;x<x1-.01f;x+=2)for(float z=z0;z<z1-.01f;z+=2)
                        {float xe=Mathf.Min(x+2,x1),ze=Mathf.Min(z+2,z1);float own=Mathf.Repeat(Mathf.Sin(x*12.9898f+z*78.233f)*43758.5453f,1);
                            bays.Slot(0).Box(W3((x+xe)*.5f,Deck+.004f,(z+ze)*.5f),new Vector3(xe-x-.05f,.008f,ze-z-.05f),Quaternion.identity,Color.Lerp(new Color(.2f,.27f,.31f),new Color(.28f,.35f,.39f),own));}
                        for(float x=x0;x<=x1+.01f;x+=4)bays.Slot(1).Box(W3(x,Deck+.009f,(z0+z1)*.5f),new Vector3(.04f,.006f,z1-z0),Quaternion.identity,new Color(.62f,.74f,.78f));
                        for(float z=z0;z<=z1+.01f;z+=4)bays.Slot(1).Box(W3((x0+x1)*.5f,Deck+.009f,z),new Vector3(x1-x0,.006f,.04f),Quaternion.identity,new Color(.62f,.74f,.78f));
                    }
                    Made(kit,"The gantry's bays",bays,TealMat,TealMat).transform.SetParent(root.transform,true);
                }
                // Grated drains across the deck every nine metres (the floods run off into them), and the walk from the gangway to the lift painted pale.
                for(float z=-13.5f;z<16;z+=9)s.Slot(1).Box(W3(0,Deck+.006f,z),new Vector3(DeckX*2-2,.012f,.9f),Quaternion.identity,new Color(.26f,.3f,.33f));
                trim.Slot(0).Beam(W3(0,Deck+.01f,DeckZ0+.6f),W3(0,Deck+.01f,-4.6f),2.2f,.015f,new Color(.7f,.76f,.78f));trim.Slot(0).Beam(W3(0,Deck+.01f,4.6f),W3(0,Deck+.01f,LiftTop.z-2.8f),2.2f,.015f,new Color(.7f,.76f,.78f));
                trim.Slot(1).Disc(W3(0,Deck+.016f,0),Vector3.up,4.4f,48,Gold,3.9f,Gold);trim.Slot(0).Disc(W3(0,Deck+.015f,0),Vector3.up,3.9f,48,TealDeep);trim.Slot(0).Disc(W3(0,Deck+.017f,0),Vector3.up,1.6f,32,Teal,1.2f,Teal);
                for(int k=0;k<12;k++){float a=k*30*Mathf.Deg2Rad;var d=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));var c=W3(0,Deck+.018f,0);trim.Slot(1).Triangle(c+d*4.5f+Vector3.Cross(d,Vector3.up)*.4f,c+d*4.5f-Vector3.Cross(d,Vector3.up)*.4f,c+d*(k%3==0?6.2f:5.2f),Vector3.up,Gold);}
                // Teal and white chevrons at the lift's mouth, pointing the way down.
                for(int k=0;k<4;k++){var c=W3(0,Deck+.016f,8.2f+k*1.1f);trim.Slot(0).Triangle(c+W3(-1.6f,0,-.5f),c+W3(1.6f,0,-.5f),c+W3(0,0,.5f),Vector3.up,k%2==0?Teal:new Color(.9f,.93f,.95f));}
                // The docking ring on the landing, and the dock's two clamp arms reaching out over the edge where the ships came in.
                trim.Slot(1).Disc(W3(0,Deck+.015f,-28),Vector3.up,4.4f,32,Gold,3.9f,Gold);trim.Slot(0).Disc(W3(0,Deck+.014f,-28),Vector3.up,3.9f,32,new Color(.74f,.8f,.82f));
                foreach(int side in new[]{-1,1}){frame.Slot(0).Box(W3(side*7.4f,Deck+.6f,-33),new Vector3(1.2f,1.2f,1.6f),Quaternion.identity,Pale,White);frame.Slot(0).Beam(W3(side*7.4f,Deck+1,-33),W3(side*10.6f,Deck+.4f,-37),.6f,.5f,Pale,White);frame.Slot(1).Box(W3(side*10.8f,Deck+.4f,-37.2f),new Vector3(.9f,.5f,.9f),Quaternion.identity,Teal);}
                // The coolant: a pipe along each side of the deck at the rail's foot, a sluice head every six metres, lit cyan.
                foreach(int side in new[]{-1,1})
                {
                    cool.Slot(0).Tube(new[]{W3(side*(DeckX-.9f),Deck+.32f,DeckZ0+1.5f),W3(side*(DeckX-.9f),Deck+.32f,DeckZ1-1.5f)},new[]{.26f,.26f},10,IronPale,IronPale,1,.25f);
                    for(float z=DeckZ0+3;z<DeckZ1-2;z+=6)
                    {
                        cool.Slot(0).Box(W3(side*(DeckX-.9f),Deck+.42f,z),new Vector3(.9f,.84f,.9f),Quaternion.identity,Pale,White);
                        glow.Slot(0).Box(W3(side*(DeckX-1.37f),Deck+.5f,z),new Vector3(.04f,.36f,.5f),Quaternion.identity,Color.white);
                    }
                }
                // The frame: two legs at the deck's north corners, a beam over, the maintenance arms parked on it, floods on the legs. All of it north of the fight, on the camera's far side.
                foreach(int side in new[]{-1,1})
                {
                    var leg=W3(side*(DeckX+.4f),HullBack,DeckZ1-1);
                    frame.Slot(0).Box(leg+Vector3.up*11.5f,new Vector3(1.4f,23,1.4f),Quaternion.identity,Pale,White);frame.Slot(1).Box(leg+Vector3.up*(9-HullBack)+Vector3.back*.72f,new Vector3(1.2f,.4f,.06f),Quaternion.identity,Teal);
                    frame.Slot(0).Box(leg+W3(-side*1.2f,9-HullBack+3.4f,0),new Vector3(1,.6f,1.2f),Quaternion.Euler(0,0,side*-30),IronPale,Pale);
                    frame.Slot(3).Box(leg+W3(-side*1.6f,9-HullBack+3.2f,-.4f),new Vector3(.6f,.3f,.2f),Quaternion.Euler(20,0,0),Color.white);
                }
                frame.Slot(0).Box(W3(0,HullBack+22.6f,DeckZ1-1),new Vector3(DeckX*2+3,1.6f,1.8f),Quaternion.identity,Pale,White);frame.Slot(2).Box(W3(0,HullBack+22.6f,DeckZ1-1.92f),new Vector3(DeckX*2+1,.3f,.06f),Quaternion.identity,Gold);
                var arms=new GameObject("The parked arms");
                foreach(var (x,folder,id) in new[]{(-11f,"Environment","SaintArm"),(11f,"Enemies","WeldArm")})
                {
                    var arm=Model(folder,id,W3(x,Deck+6.5f,DeckZ1+1.2f),5,x<0?160:200,true,"Parked "+id);if(arm==null)continue;arm.transform.SetParent(arms.transform,true);arm.AddComponent<PropTint>().tint=new Color(.94f,.97f,1);
                    // Hung from the frame's beam on a trolley and two falls, a hook block over it (the look pass of November 14: parked in the air with nothing holding them, the arms
                    // read as debris floating over the deck).
                    var top=ZoneKit.Drawn(arm).max.y;var beam=HullBack+22.6f;var trolley=W3(x,beam-1.1f,DeckZ1+.3f);
                    frame.Slot(0).Box(trolley,new Vector3(2.4f,.6f,1.6f),Quaternion.identity,IronPale,Pale);frame.Slot(1).Box(trolley+W3(0,.32f,-.82f),new Vector3(2.2f,.12f,.04f),Quaternion.identity,Teal);
                    foreach(int k in new[]{-1,1})frame.Slot(0).Beam(trolley+W3(k*.7f,-.3f,.6f),W3(x+k*.5f,top+1.1f,DeckZ1+1.2f),.07f,.07f,new Color(.2f,.22f,.25f));
                    frame.Slot(0).Box(W3(x,top+.8f,DeckZ1+1.2f),new Vector3(1.3f,.7f,.8f),Quaternion.identity,IronPale,Pale);frame.Slot(2).Box(W3(x,top+.8f,DeckZ1+.78f),new Vector3(1.1f,.12f,.04f),Quaternion.identity,Gold);
                }
                var deck=Made(kit,"The gantry's deck",s,DeckMat,DarkMat);deck.transform.SetParent(root.transform,true);deck.isStatic=true;
                ZoneKit.Block(deck,W3(0,Deck-thick*.5f,(DeckZ0+DeckZ1)*.5f),new Vector3(DeckX*2,thick,DeckZ1-DeckZ0),0,"Deck (walked)");
                ZoneKit.Block(deck,W3(0,Deck-thick*.5f,-19.5f),new Vector3(8,thick,5.2f),0,"Gangway (walked)");ZoneKit.Block(deck,W3(0,Deck-thick*.5f,-28),new Vector3(16,thick,12.2f),0,"Landing (walked)");
                Made(kit,"The gantry's trim",trim,TealMat,GoldMat).transform.SetParent(root.transform,true);
                var lit=Made(kit,"The gantry's lamps",glow,LampWhite);lit.transform.SetParent(root.transform,true);lit.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                var pipes=Made(kit,"The gantry's coolant",cool,WhiteMat);pipes.transform.SetParent(root.transform,true);
                foreach(int side in new[]{-1,1})ZoneKit.Block(pipes,W3(side*(DeckX-.9f),Deck+.42f,0),new Vector3(.9f,.84f,DeckZ1-DeckZ0-3),0,"Coolant run");
                var legs=Made(kit,"The gantry's frame",frame,WhiteMat,TealMat,GoldMat,LampWhite);legs.isStatic=true;
                foreach(int side in new[]{-1,1})ZoneKit.Block(legs,W3(side*(DeckX+.4f),HullBack+11.5f,DeckZ1-1),new Vector3(1.4f,23,1.4f),0,"Frame leg");
                // Rails: every edge but the gangway's mouth (the seal) and the lift's head.
                var rails=new ZoneKit.Shape();var railOwner=new GameObject("The gantry's rails");railOwner.transform.SetParent(root.transform,true);
                var runs=new List<(Vector3,Vector3)>{(W3(-DeckX,0,DeckZ0),W3(-4,0,DeckZ0)),(W3(4,0,DeckZ0),W3(DeckX,0,DeckZ0)),(W3(-DeckX,0,DeckZ0),W3(-DeckX,0,DeckZ1)),(W3(DeckX,0,DeckZ0),W3(DeckX,0,DeckZ1)),
                    (W3(-DeckX,0,DeckZ1),W3(-RampHalf,0,DeckZ1)),(W3(RampHalf,0,DeckZ1),W3(DeckX,0,DeckZ1)),(W3(-4,0,-22),W3(-4,0,DeckZ0)),(W3(4,0,-22),W3(4,0,DeckZ0)),
                    (W3(-8,0,-34),W3(8,0,-34)),(W3(-8,0,-34),W3(-8,0,-22)),(W3(8,0,-34),W3(8,0,-22)),(W3(-8,0,-22),W3(-4,0,-22)),(W3(4,0,-22),W3(8,0,-22))};
                foreach(var (a,b) in runs)Rail(kit,rails,railOwner,a,b);
                var railing=Made(kit,"The gantry's rails, drawn",rails,WhiteMat,GoldMat,TealMat);railing.transform.SetParent(railOwner.transform,true);
                foreach(var t in railOwner.transform.Cast<Transform>().ToArray())if(t.name=="Rail")t.SetParent(railing.transform,true);
                foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.isStatic=true;
                // The cover anchors: three bitts that survive every phase.
                for(int i=0;i<GantryCover.Length;i++)Bitt(kit,"Gantry cover anchor "+(i+1),GantryCover[i],1.5f,2.5f).transform.SetParent(root.transform,true);
                // The seal across the gangway's mouth.
                seal=Seal(kit,"Gantry seal",W3(0,Deck,SealZ),SealWidth);
                // Light: floods on the frame's legs over the deck, cold, and the kerb's running lights.
                // The frame's own work lights warm (the look pass of November 14: every light aboard was the same cold blue, and the zone read as one tone); the landing's stays cold.
                PointLight(W3(-15,Deck+8,14),26,1.6f,new Color(1f,.86f,.66f),"Gantry flood");PointLight(W3(15,Deck+8,14),26,1.6f,new Color(1f,.86f,.66f),"Gantry flood");
                PointLight(W3(0,Deck+6,-26),16,1.1f,new Color(.9f,.95f,1),"Landing flood");
                return root;
            }

            /// <summary>The hull's back round the gantry and the chamber, nine metres under the deck: its plates (the heightfield's floor there, tinted the hull's white), teal bands across it and
            /// rows of lit skylights; past its flanks the convoy goes by on two loops (the moving exterior: the ship is under way, and what moves is what passes it, never a floor).</summary>
            static void HullOutside(ZoneKit kit)
            {
                var root=new GameObject("The hull's back");var trim=new ZoneKit.Shape();var lamps=new ZoneKit.Shape();var flank=new ZoneKit.Shape();
                bool Open(Vector2 p)=>Nearest(p,out _)<1.2f||Bow(p)||InPit(p)||Mathf.Abs(p.x)<DeckX+1.5f&&p.y>-36&&p.y<DeckZ1+1||Mathf.Abs(p.x)<RampHalf+1.2f&&p.y<HatchZ+1||Seen(p)<HullBack-.02f;
                // The back itself in plates four metres square, each its own white, one in five the Compact's pale teal-grey, laid on the heightfield's flat top: the landform there draws as rock.
                var plates=new ZoneKit.Shape();
                for(float x=-46;x<46;x+=4)for(float z=-40;z<100;z+=4)
                {
                    bool flat=true;foreach(var (dx,dz) in new[]{(0f,0f),(4f,0f),(0f,4f),(4f,4f),(2f,2f)})if(Seen(new Vector2(x+dx,z+dz))<HullBack-.02f||Bow(new Vector2(x+dx,z+dz))){flat=false;break;}
                    if(!flat)continue;int ix=Mathf.FloorToInt((x+400)/4),iz=Mathf.FloorToInt((z+400)/4);float own=Mathf.Repeat(Mathf.Sin(ix*12.9898f+iz*78.233f)*43758.5453f,1);
                    var tone=own<.18f?new Color(.66f,.76f,.78f):Color.Lerp(new Color(.8f,.82f,.85f),new Color(.95f,.95f,.96f),own);
                    plates.Slot(0).Quad(W3(x,HullBack+.03f,z),W3(x+4,HullBack+.03f,z),W3(x+4,HullBack+.03f,z+4),W3(x,HullBack+.03f,z+4),Vector3.up,tone,1/8f);
                }
                if(!plates.Empty){var back=Made(kit,"The hull's plates",plates,HullMat);back.transform.SetParent(root.transform,true);back.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;}
                foreach(float z in new[]{-30f,-6f,24f,40f,58f,76f})for(float x=-44;x<44;x+=4){var c=new Vector2(x+2,z);if(Open(c))continue;trim.Slot(0).Box(W3(c.x,HullBack+.05f,z),new Vector3(4,.04f,2.4f),Quaternion.identity,Teal);trim.Slot(1).Box(W3(c.x,HullBack+.055f,z+1.6f),new Vector3(4,.04f,.2f),Quaternion.identity,Gold);}
                for(float x=-40;x<=40;x+=6)for(float z=-36;z<96;z+=3.4f){var c=new Vector2(x,z);if(Open(c)||Mathf.Abs(x)<26&&z>40)continue;lamps.Slot(0).Box(W3(x,HullBack+.06f,z),new Vector3(1.2f,.04f,1.6f),Quaternion.identity,Color.white);}
                // The flanks curving down past the hull's edge, lit windows in two rows.
                foreach(int side in new[]{-1,1})for(float z=-40;z<100;z+=8)
                {
                    float x0=side*46,x1=side*51;
                    flank.Slot(0).Quad(W3(x0,HullBack,z),W3(x1,HullBack-8,z),W3(x1,HullBack-8,z+8),W3(x0,HullBack,z+8),Vector3.right*side+Vector3.up*.6f,Pale,1/8f);
                    flank.Slot(0).Quad(W3(x1,HullBack-8,z),W3(x1+1,HullBack-30,z),W3(x1+1,HullBack-30,z+8),W3(x1,HullBack-8,z+8),Vector3.right*side,ZoneKit.Shape.Dim(Pale,.85f),1/8f);
                    for(float wz=z+1;wz<z+7.5f;wz+=2.6f)lamps.Slot(0).Box(W3(side*48.6f,HullBack-4.2f,wz),new Vector3(.5f,.4f,1.3f),Quaternion.identity,Color.white);
                }
                Made(kit,"The hull's bands",trim,TealMat,GoldMat).transform.SetParent(root.transform,true);
                var sky=Made(kit,"The hull's skylights",lamps,LampWarm);sky.transform.SetParent(root.transform,true);sky.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                Made(kit,"The hull's flanks",flank,HullMat).transform.SetParent(root.transform,true);
                // The superstructure stepping up to the north-east, lit: the Continuance's own (as the flight berth sees it), well clear of every floor.
                var sup=new ZoneKit.Shape();var supLit=new ZoneKit.Shape();
                foreach(var (x0,x1,y0,y1,z0,z1) in new[]{(22f,44f,HullBack,HullBack+10,40f,98f),(26f,42f,HullBack+10,HullBack+17,48f,96f),(30f,40f,HullBack+17,HullBack+23,56f,92f)})
                {
                    sup.Slot(0).Box(W3((x0+x1)*.5f,(y0+y1)*.5f,(z0+z1)*.5f),new Vector3(x1-x0,y1-y0,z1-z0),Quaternion.identity,Pale,White);
                    sup.Slot(1).Box(W3(x0-.03f,y1-1.2f,(z0+z1)*.5f),new Vector3(.06f,.5f,z1-z0-1),Quaternion.identity,Teal);
                    for(float z=z0+1.4f;z<z1-1;z+=1.8f)supLit.Slot(0).Box(W3(x0-.04f,(y0+y1)*.5f,z),new Vector3(.06f,.7f,1.1f),Quaternion.identity,Color.white);
                }
                var superstructure=Made(kit,"The superstructure",sup,WhiteMat,TealMat);superstructure.transform.SetParent(root.transform,true);
                Made(kit,"The superstructure, lit",supLit,LampWarm).transform.SetParent(superstructure.transform,true);
                foreach(var r in root.GetComponentsInChildren<Renderer>())r.receiveShadows=false;
                // The convoy going by on two loops beyond the flanks, under the deck's level.
                var traffic=new GameObject("The convoy going by");string[] ships={"Hauler","Skiff","PatrolCutter","CompactCutter","Skiff"};float[] lengths={22,9,12,6,9};
                for(int laneIndex=0;laneIndex<2;laneIndex++)
                {
                    var go=new GameObject("Convoy loop "+laneIndex,typeof(SplineContainer),typeof(TrafficLane));go.transform.SetParent(traffic.transform,true);var spline=new Spline();
                    float rx=78+laneIndex*20,rz=130+laneIndex*20,y=-26-laneIndex*10;
                    for(int i=0;i<24;i++){float a=i*Mathf.PI/12;spline.Add(new BezierKnot(new float3(Mathf.Sin(a)*rx,y,30+Mathf.Cos(a)*rz)));}
                    spline.Closed=true;spline.SetTangentMode(TangentMode.AutoSmooth);go.GetComponent<SplineContainer>().Spline=spline;
                    var lane=go.GetComponent<TrafficLane>();lane.speed=4+laneIndex*1.5f;
                    for(int j=0;j<4;j++)
                    {
                        int k=(j+laneIndex*2)%ships.Length;string id=ships[k];if(!ZoneKit.Landed(id))continue;
                        var ship=WorldBuilder.Piece(id,Vector3.zero,new Vector3(lengths[k]*.4f,lengths[k]*.18f,lengths[k]),"Rock",false);ship.isStatic=false;ship.transform.SetParent(go.transform,true);
                        var mover=ship.AddComponent<TrafficShip>();mover.lane=lane;mover.phase=j/4f+laneIndex*.11f;ship.transform.SetPositionAndRotation(lane.Position(mover.phase),Quaternion.LookRotation(lane.Tangent(mover.phase)));
                        foreach(var view in ship.GetComponentsInChildren<Renderer>())view.shadowCastingMode=ShadowCastingMode.Off;
                    }
                }
            }

            // ---------------------------------------------------------------- the lift
            /// <summary>The lift: its cage on the deck's north edge (her way down: her BossExit stands in it), its rail and the walked way beside it running down over the hull's back on a
            /// lattice to the hatch, then down the shaft cut into the hull to the landing at the well's door; guide lamps down both sides; a gate at the deck's edge shut until it may run.</summary>
            static ChamberLift Lift(ZoneKit kit)
            {
                var root=new GameObject("The lift");var s=new ZoneKit.Shape();var trim=new ZoneKit.Shape();var glow=new ZoneKit.Shape();
                float angle=Mathf.Atan2(Deck-HullBack,HatchZ-RampHead)*Mathf.Rad2Deg;var slope=Quaternion.Euler(angle,0,0);
                // The upper run, built over the hull's back on two lattice girders and three trestles.
                float length=Mathf.Sqrt((HatchZ-RampHead)*(HatchZ-RampHead)+(Deck-HullBack)*(Deck-HullBack));
                var mid=W3(0,(Deck+HullBack)*.5f-.2f,(RampHead+HatchZ)*.5f);
                s.Slot(0).Box(mid,new Vector3(RampHalf*2,.4f,length+.2f),slope,IronPale,new Color(.8f,.83f,.85f));
                foreach(int side in new[]{-1,1})s.Slot(0).Box(mid+Vector3.right*side*(RampHalf-.2f)+slope*Vector3.down*.5f,new Vector3(.4f,.8f,length),slope,Pale,White);
                foreach(float z in new[]{20f,25f,29.5f}){float y=RampY(z);foreach(int side in new[]{-1,1})s.Slot(0).Box(W3(side*(RampHalf-.6f),(y+HullBack)*.5f-.3f,z),new Vector3(.5f,y-HullBack-.4f,.5f),Quaternion.identity,IronPale,Pale);}
                // The rail down the middle all the way to the foot, and guide lamps down both sides.
                for(float z=RampHead;z<RampFoot;z+=2.4f){float z1=Mathf.Min(z+2.4f,RampFoot);trim.Slot(0).Beam(W3(0,RampY(z)+.04f,z),W3(0,RampY(z1)+.04f,z1),.5f,.08f,Teal);foreach(int side in new[]{-1,1})glow.Slot(0).Box(W3(side*(RampHalf-.35f),RampY(z)+.05f,z+.6f),new Vector3(.12f,.05f,.5f),slope,Color.white);}
                var built=Made(kit,"The lift's upper run",s,WhiteMat);built.transform.SetParent(root.transform,true);built.isStatic=true;
                var walked=ZoneKit.Block(built,mid,new Vector3(RampHalf*2,.4f,length+.2f),0,"Lift run (walked)");walked.transform.rotation=slope;
                Made(kit,"The lift's rail",trim,TealMat).transform.SetParent(root.transform,true);
                var lamps=Made(kit,"The lift's guide lamps",glow,LampTeal);lamps.transform.SetParent(root.transform,true);lamps.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                // Rails on the upper run (nobody steps off onto the hull's back).
                var rails=new ZoneKit.Shape();var owner=new GameObject("The lift's rails");owner.transform.SetParent(root.transform,true);
                foreach(int side in new[]{-1,1})Rail(kit,rails,owner,W3(side*RampHalf,RampY(RampHead),RampHead),W3(side*RampHalf,RampY(HatchZ+.6f),HatchZ+.6f));
                var railing=Made(kit,"The lift's rails, drawn",rails,WhiteMat,GoldMat,TealMat);railing.transform.SetParent(owner.transform,true);
                foreach(var t in owner.GetComponentsInChildren<Transform>().ToArray())if(t.name=="Rail")t.SetParent(railing.transform,true);
                // The hatch's collar where the run goes into the hull, lit.
                var hatch=new ZoneKit.Shape();foreach(int side in new[]{-1,1})hatch.Slot(0).Box(W3(side*(RampHalf+.5f),HullBack+.6f,HatchZ),new Vector3(1,1.2f,1.4f),Quaternion.identity,Pale,White);
                hatch.Slot(1).Box(W3(0,HullBack+.08f,HatchZ-.9f),new Vector3(RampHalf*2+2,.16f,.4f),Quaternion.identity,Teal);
                Made(kit,"The hatch's collar",hatch,WhiteMat,TealMat).transform.SetParent(root.transform,true);
                foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.isStatic=true;
                // The cage: a floor, four posts, a roof frame, its lamp; drawn only, moved by the lift.
                var car=new ZoneKit.Shape();
                // A built cage, not a bare frame (the look pass of November 21, s67/099): a tread floor with gold edges at its two open ends and the rail's shoes under it, grilles of
                // bars down both long sides with a mid rail, corner posts with gussets, a lattice roof the camera sees through, two strip lamps under it, and a control post at a corner
                // with its panel lit teal. Open at both ends (the deck's side at the top, the well's door at the foot).
                car.Slot(0).Box(Vector3.up*.06f,new Vector3(5,.12f,4.2f),Quaternion.identity,new Color(.4f,.44f,.48f),new Color(.6f,.64f,.68f));
                for(float x=-2.2f;x<2.21f;x+=.4f)car.Slot(0).Box(W3(x,.125f,0),new Vector3(.04f,.01f,3.8f),Quaternion.identity,new Color(.34f,.38f,.42f));
                foreach(int end in new[]{-1,1}){car.Slot(2).Box(W3(0,.13f,end*1.98f),new Vector3(4.8f,.02f,.2f),Quaternion.identity,Gold);for(float x=-2.1f;x<2.11f;x+=.6f)car.Slot(0).Box(W3(x+.15f,.142f,end*1.98f),new Vector3(.22f,.012f,.2f),Quaternion.Euler(0,30,0),new Color(.16f,.18f,.2f));}
                foreach(int end in new[]{-1,1})car.Slot(0).Box(W3(0,-.18f,end*1.4f),new Vector3(.9f,.36f,.7f),Quaternion.identity,Iron,IronPale);
                foreach(var (x,z) in new[]{(-2.4f,-2f),(2.4f,-2f),(-2.4f,2f),(2.4f,2f)}){car.Slot(1).Box(W3(x,1.55f,z),new Vector3(.18f,3.1f,.18f),Quaternion.identity,Pale,White);car.Slot(1).Beam(W3(x,2.6f,z),W3(x,3.05f,z-Mathf.Sign(z)*.5f),.1f,.1f,Pale,White);}
                foreach(int side in new[]{-1,1})
                {
                    float x=side*2.4f;
                    for(float z=-1.8f;z<1.81f;z+=.3f)car.Slot(1).Box(W3(x,1.5f,z),new Vector3(.05f,2.9f,.05f),Quaternion.identity,IronPale,Pale);
                    foreach(float y in new[]{.12f,1.1f,2.95f})car.Slot(1).Box(W3(x,y,0),new Vector3(.12f,.1f,4.1f),Quaternion.identity,Pale,White);
                    car.Slot(2).Box(W3(x-side*.07f,1.1f,0),new Vector3(.02f,.06f,3.9f),Quaternion.identity,Teal);
                    car.Slot(3).Box(W3(x-side*.25f,2.98f,0),new Vector3(.1f,.06f,3.4f),Quaternion.identity,Color.white);
                }
                foreach(var (a,b) in new[]{(W3(-2.4f,3.1f,-2),W3(2.4f,3.1f,-2)),(W3(-2.4f,3.1f,2),W3(2.4f,3.1f,2)),(W3(-2.4f,3.1f,-2),W3(-2.4f,3.1f,2)),(W3(2.4f,3.1f,-2),W3(2.4f,3.1f,2))})car.Slot(1).Beam(a,b,.16f,.16f,Pale,White);
                for(float x=-1.6f;x<1.61f;x+=.8f)car.Slot(1).Beam(W3(x,3.12f,-2),W3(x,3.12f,2),.06f,.06f,IronPale,Pale);
                for(float z=-1.2f;z<1.21f;z+=.8f)car.Slot(1).Beam(W3(-2.4f,3.13f,z),W3(2.4f,3.13f,z),.06f,.06f,IronPale,Pale);
                car.Slot(2).Box(W3(0,3.15f,2),new Vector3(4.8f,.12f,.08f),Quaternion.identity,Teal);car.Slot(2).Box(W3(0,3.15f,-2),new Vector3(4.8f,.12f,.08f),Quaternion.identity,Teal);
                car.Slot(1).Box(W3(2.05f,.65f,1.65f),new Vector3(.36f,1.3f,.3f),Quaternion.identity,Pale,White);car.Slot(2).Box(W3(2.05f,1.08f,1.49f),new Vector3(.26f,.3f,.02f),Quaternion.Euler(-15,0,0),new Color(.08f,.16f,.18f));
                car.Slot(3).Box(W3(2.05f,1.12f,1.48f),new Vector3(.18f,.08f,.012f),Quaternion.Euler(-15,0,0),Color.white);
                var cage=Made(kit,"The lift's cage",car,DarkMat,WhiteMat,TealMat,LampTeal);
                // Its mesh is built about its own floor centre: stood at the top.
                var holder=new GameObject("The lift's cage (moves)");holder.transform.position=LiftTop;cage.transform.SetParent(holder.transform,false);
                PointLight(LiftTop+Vector3.up*2.8f,7,1.1f,new Color(.8f,.95f,1),"Cage lamp").transform.SetParent(holder.transform,true);
                // The prompt on the cage, the gate at the run's head.
                var promptAt=new GameObject(LiftWords);promptAt.transform.position=LiftTop;
                var lift=promptAt.AddComponent<ChamberLift>();lift.prompt=LiftWords;lift.range=3;lift.requiredFlag="bossdown.MeretGantry";lift.lockedPrompt=LiftLocked;lift.spawn=WellSpawn;
                lift.path=LiftPath;lift.seconds=8;lift.car=holder.transform;
                lift.gate=Seal(kit,"The lift's gate",W3(0,Deck,DeckZ1+.3f),RampHalf*2,0,2.4f);
                WorldBuilder.Label(LiftWords,LiftTop+W3(-4.2f,.3f,-2.6f),2.2f);
                // Down the shaft: lamps on its walls, and the cage's rest at the foot.
                for(float z=HatchZ+3;z<RampFoot;z+=4)foreach(int side in new[]{-1,1})PointLight(W3(side*(RampHalf-.6f),RampY(z)+2.2f,z),7,.8f,new Color(.7f,.92f,1),"Shaft lamp");
                return lift;
            }

            // ---------------------------------------------------------------- the control well and the anchor
            /// <summary>The control well: its door and seal on the south rim off the landing, the gold ring and the Compact's mark painted on its floor, consoles round its north, east and
            /// west walls (it is a control room: the screens lit teal), three cover anchors; the neck north to the collar platform (sealed through the fight); the collar round the pit, the
            /// chain hanging down it, the four clamps on the chain, Idra's people's braces, the crews' two hatches; the bow open beyond, the road below. Returns the anchor.</summary>
            static AnchorRelease WellRoom(ZoneKit kit,out Membrane seal,out Membrane neck)
            {
                var root=new GameObject("The control well");var s=new ZoneKit.Shape();var trim=new ZoneKit.Shape();var glow=new ZoneKit.Shape();
                // The door's frame on the south rim, the landing's rail where it drops to nothing (none: the shaft's walls hold it), the floor's painted mark.
                foreach(int side in new[]{-1,1}){s.Slot(0).Box(W3(side*(WellSealWidth*.5f+.5f),Well+.5f,WellSealZ),new Vector3(1,1,1.2f),Quaternion.identity,Pale,White);trim.Slot(0).Box(W3(side*(WellSealWidth*.5f+.5f),Well+.7f,WellSealZ-.62f),new Vector3(.5f,.3f,.04f),Quaternion.identity,Teal);}
                trim.Slot(1).Disc(W+Vector3.up*.012f,Vector3.up,2.6f,40,Gold,2.3f,Gold);trim.Slot(0).Disc(W+Vector3.up*.011f,Vector3.up,2.3f,40,TealDeep);
                for(int k=0;k<8;k++){float a=k*45*Mathf.Deg2Rad;var d=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));trim.Slot(1).Triangle(W+d*2.7f+Vector3.up*.013f+Vector3.Cross(d,Vector3.up)*.35f,W+d*2.7f+Vector3.up*.013f-Vector3.Cross(d,Vector3.up)*.35f,W+d*(k%2==0?4.2f:3.5f)+Vector3.up*.013f,Vector3.up,Gold);}
                // The quiet room at the machine's middle (the look pass of November 14: the well read as a plain round floor): its floor in rings, dark inlays at five and eight and a half
                // metres, gold spokes between them, the walk from the door to the neck; a line of cold light round its wall's foot, broken at the door and the neck.
                foreach(var (inner,outer) in new[]{(5f,5.5f),(8.4f,8.9f)})trim.Slot(0).Disc(W+Vector3.up*.009f,Vector3.up,outer,56,TealDeep,inner,TealDeep);
                trim.Slot(0).Disc(W+Vector3.up*.008f,Vector3.up,11.2f,64,new Color(.5f,.56f,.6f),9.4f,new Color(.44f,.5f,.54f));
                for(int k=0;k<16;k++){float a=(k*22.5f+11.25f)*Mathf.Deg2Rad;var d=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));trim.Slot(1).Beam(W+d*5.6f+Vector3.up*.012f,W+d*8.3f+Vector3.up*.012f,.08f,.012f,Gold);}
                for(int k=0;k<72;k++)
                {
                    float b=k*5;if(Mathf.Abs(Mathf.DeltaAngle(b,180))<14||Mathf.Abs(Mathf.DeltaAngle(b,0))<20)continue;float a=b*Mathf.Deg2Rad;var d=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
                    glow.Slot(0).Box(W+d*(WellRadius+.25f)+Vector3.up*.3f,new Vector3(.7f,.1f,.08f),Quaternion.LookRotation(d),Color.white);
                }
                // Consoles round the well's north half, behind the rim: a ring of desks with lit teal screens.
                foreach(float bearing in new[]{-70f,-40f,40f,70f})
                {
                    var at=W+new Vector3(Mathf.Sin(bearing*Mathf.Deg2Rad),0,Mathf.Cos(bearing*Mathf.Deg2Rad))*(WellRadius+1.2f);float yaw=bearing+180;var turn=Quaternion.Euler(0,yaw,0);
                    s.Slot(0).Box(at+Vector3.up*.5f,new Vector3(2.6f,1,1),turn,Pale,White);glow.Slot(0).Box(at+Vector3.up*1.08f+turn*Vector3.forward*.1f,new Vector3(2.2f,.06f,.7f),turn*Quaternion.Euler(-24,0,0),Color.white);
                }
                // The collar: the ring round the pit (the last collar), its gold and teal bands, a kerb round it, and the lamps that show the anchor's cycle.
                var collar=new ZoneKit.Shape();var collarLamps=new ZoneKit.Shape();
                var ring=new List<Vector3>();var radii=new List<float>();for(int k=0;k<=48;k++){float a=k*7.5f*Mathf.Deg2Rad;ring.Add(A+new Vector3(Mathf.Sin(a)*(PitRadius+.4f),.55f,Mathf.Cos(a)*(PitRadius+.4f)));radii.Add(.55f);}
                collar.Slot(0).Tube(ring,radii,12,Pale,White,8,.25f,false);
                var band=ring.Select(p=>p+Vector3.up*.18f).ToList();collar.Slot(1).Tube(band,radii.Select(r=>r*1.04f).ToList(),12,Gold,Gold,8,.25f,false);
                for(int k=0;k<16;k++){float a=(k*22.5f+11.25f)*Mathf.Deg2Rad;var d=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));collar.Slot(2).Box(A+d*(PitRadius+.4f)+Vector3.up*.55f,new Vector3(.5f,1.2f,.5f),Quaternion.LookRotation(d),Teal);collarLamps.Slot(0).Box(A+d*(PitRadius+.98f)+Vector3.up*.75f,new Vector3(.3f,.3f,.08f),Quaternion.LookRotation(d),Color.white);}
                var collarBody=Made(kit,"The collar",collar,WhiteMat,GoldMat,TealMat);collarBody.transform.SetParent(root.transform,true);collarBody.isStatic=true;
                for(int k=0;k<12;k++){float a=k*30*Mathf.Deg2Rad;var d=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));ZoneKit.Block(collarBody,A+d*(PitRadius+.45f)+Vector3.up*.5f,new Vector3(1.9f,1.4f,1.1f),k*30,"Collar kerb");}
                var collarLit=Made(kit,"The collar's lamps",collarLamps,LampTeal);collarLit.transform.SetParent(root.transform,true);collarLit.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                // The pit's lip lit cold from inside, and the bow's open frame north of the collar: two pillars and a lintel, a rail across between them.
                var bow=new ZoneKit.Shape();
                foreach(int side in new[]{-1,1}){bow.Slot(0).Box(W3(side*11,Well+5,A.z+CollarRadius+.8f),new Vector3(2,14,2),Quaternion.identity,Pale,White);bow.Slot(1).Box(W3(side*11,Well+6,A.z+CollarRadius-.22f),new Vector3(1.6f,.4f,.06f),Quaternion.identity,Teal);}
                bow.Slot(0).Box(W3(0,Well+12.4f,A.z+CollarRadius+.8f),new Vector3(24,1.6f,2),Quaternion.identity,Pale,White);
                var bowBody=Made(kit,"The bow's frame",bow,WhiteMat,TealMat);bowBody.transform.SetParent(root.transform,true);bowBody.isStatic=true;
                foreach(int side in new[]{-1,1})ZoneKit.Block(bowBody,W3(side*11,Well+5,A.z+CollarRadius+.8f),new Vector3(2,14,2),0,"Bow pillar");
                var bowRails=new ZoneKit.Shape();var bowOwner=new GameObject("The bow's rail");bowOwner.transform.SetParent(root.transform,true);
                var arc=new List<Vector3>();for(float b=-58;b<=58.01f;b+=14.5f){float a=b*Mathf.Deg2Rad;arc.Add(A+new Vector3(Mathf.Sin(a),0,Mathf.Cos(a))*(CollarRadius-.15f));}
                for(int k=1;k<arc.Count;k++)Rail(kit,bowRails,bowOwner,arc[k-1],arc[k]);
                var bowDrawn=Made(kit,"The bow's rail, drawn",bowRails,WhiteMat,GoldMat,TealMat);bowDrawn.transform.SetParent(bowOwner.transform,true);
                foreach(var t in bowOwner.GetComponentsInChildren<Transform>().ToArray())if(t.name=="Rail")t.SetParent(bowDrawn.transform,true);
                var walls=Made(kit,"The well's fittings",s,WhiteMat);walls.transform.SetParent(root.transform,true);walls.isStatic=true;
                foreach(int side in new[]{-1,1})ZoneKit.Block(walls,W3(side*(WellSealWidth*.5f+.5f),Well+.5f,WellSealZ),new Vector3(1,1,1.2f),0,"Door post");
                foreach(float bearing in new[]{-70f,-40f,40f,70f}){var at=W+new Vector3(Mathf.Sin(bearing*Mathf.Deg2Rad),0,Mathf.Cos(bearing*Mathf.Deg2Rad))*(WellRadius+1.2f);ZoneKit.Block(walls,at+Vector3.up*.5f,new Vector3(2.6f,1,1),bearing+180,"Console");}
                Made(kit,"The well's paint",trim,TealMat,GoldMat).transform.SetParent(root.transform,true);
                var screens=Made(kit,"The well's screens",glow,LampTeal);screens.transform.SetParent(root.transform,true);screens.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.isStatic=true;
                for(int i=0;i<3;i++)Bitt(kit,"Well cover anchor "+(i+1),WellCover(i),1.2f,2.2f).transform.SetParent(root.transform,true);
                // The seals: the door, and the neck to the collar (both the well fight's).
                seal=Seal(kit,"Well seal",W3(0,Well,WellSealZ),WellSealWidth+.4f);
                neck=Seal(kit,"Collar neck seal",W3(0,Well,NeckZ),NeckWidth);
                // Idra's people's braces: orange jacks from the platform up to the collar's ring, a power pack each.
                var brace=new ZoneKit.Shape();var black=new Color(.12f,.12f,.13f);
                foreach(var b in Braces)
                {
                    var toward=(A-b).normalized;var top=A-toward*(PitRadius+.9f)+Vector3.up*1.1f;
                    foreach(float dz in new[]{-1.2f,1.2f}){var foot=b+Vector3.Cross(Vector3.up,toward)*dz;brace.Slot(0).Tube(new[]{foot+Vector3.up*.1f,Vector3.Lerp(foot,top,.6f)},new[]{.16f,.16f},10,Orange,Orange,1,.3f);brace.Slot(1).Tube(new[]{Vector3.Lerp(foot,top,.55f),top},new[]{.1f,.1f},8,IronPale,IronPale,1,.3f);brace.Slot(0).Box(foot+Vector3.up*.05f,new Vector3(.5f,.1f,.5f),Quaternion.identity,black,Orange);}
                    brace.Slot(0).Box(b-toward*.8f+Vector3.up*.3f,new Vector3(.6f,.6f,.5f),Quaternion.LookRotation(toward),Orange,new Color(1,.6f,.3f));
                }
                var braces=Made(kit,"Idra's people's braces",brace,BraceMat,DarkMat);braces.transform.SetParent(root.transform,true);
                foreach(var b in Braces)ZoneKit.Block(braces,b-(A-b).normalized*.8f+Vector3.up*.3f,new Vector3(.6f,.6f,.5f),0,"Power pack");
                // The crews' hatches: a door frame at the back of each alcove.
                var hatch=new ZoneKit.Shape();foreach(var h in Hatches){float side=Mathf.Sign(h.x);hatch.Slot(0).Box(W3(side*12.8f,Well+1.6f,h.z),new Vector3(.4f,3.2f,3.4f),Quaternion.identity,Pale,White);hatch.Slot(1).Box(W3(side*12.55f,Well+1.5f,h.z),new Vector3(.06f,2.6f,1.9f),Quaternion.identity,TealDeep);}
                Made(kit,"The crews' hatches",hatch,WhiteMat,TealMat).transform.SetParent(root.transform,true);

                // ---- the anchor itself
                var anchor=new GameObject("Stillwater's last anchor",typeof(AnchorRelease)).GetComponent<AnchorRelease>();anchor.transform.position=A;
                anchor.music=ReleaseMusic;anchor.collar=A+Vector3.up*.6f;anchor.shows=ReleaseShows;anchor.farthest=50;
                // As it lets go the view takes in the seat, the four clamps, the chain's head over them and the bow with the road through it, and not the well behind the party.
                anchor.opening=new Bounds(A+new Vector3(0,5.5f,2.5f),new Vector3(22,13,20));
                // The chain: links hanging down the pit from just over the collar, far into the dark (drawn only).
                var chainRoot=new GameObject("The anchor chain");chainRoot.transform.position=A;
                const float segment=18;
                for(int i=0;i<8;i++)
                {
                    var piece=Model("Environment","AnchorChain",A+Vector3.down*(i*segment-7f+segment),segment,i%2==0?0:90,true,"Chain "+(i+1));
                    if(piece==null)break;
                    // Stood on end: its length down the pit.
                    var c=ZoneKit.Drawn(piece).center;piece.transform.RotateAround(c,Vector3.right,90);var now=ZoneKit.Drawn(piece);piece.transform.position+=new Vector3(A.x-now.center.x,A.y+7f-i*segment-now.max.y,A.z-now.center.z);
                    piece.transform.SetParent(chainRoot.transform,true);foreach(var r in piece.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
                    piece.AddComponent<PropTint>().tint=new Color(.62f,.66f,.72f);
                }
                // The chain's head, what the four clamps hold (the look pass of November 14: the chain's top stood no higher than the clamps and read as a stone in the collar): a great
                // shackle in the chain's dark steel standing on the chain's top link over the clamps' heads, banded in the Compact's gold, its pin through the top link, the clamps' teal on its
                // cheeks. It is the chain's (under its root), so it goes down the pit with it, and it is narrower than the pit.
                {
                    var head=new ZoneKit.Shape();var steel=new Color(.56f,.6f,.66f);var lit=new Color(.78f,.82f,.86f);
                    var loop=new List<Vector3>();var thick=new List<float>();
                    for(int k=0;k<=32;k++){float a=k*Mathf.PI*2/32;loop.Add(A+new Vector3(Mathf.Sin(a)*2.4f,10.8f+Mathf.Cos(a)*3.9f*(Mathf.Cos(a)>0?1:.86f),0));thick.Add(.78f);}
                    head.Slot(0).Tube(loop,thick,14,steel,lit,10,.25f,false);
                    head.Slot(0).Tube(new[]{A+new Vector3(-2.9f,7.4f,0),A+new Vector3(2.9f,7.4f,0)},new[]{.42f,.42f},14,steel,lit,1,.25f);
                    foreach(int side in new[]{-1,1})
                    {
                        head.Slot(0).Tube(new[]{A+new Vector3(side*2.9f,7.4f,-.5f),A+new Vector3(side*2.9f,7.4f,.5f)},new[]{.7f,.7f},14,steel,lit,1,.25f);
                        head.Slot(1).Box(A+new Vector3(side*2.4f,10.8f,0),new Vector3(.24f,1.8f,1.7f),Quaternion.identity,Gold);
                        head.Slot(2).Box(A+new Vector3(side*2.4f,9.2f,0),new Vector3(.08f,1f,1.7f),Quaternion.identity,new Color(.4f,.95f,1f));
                    }
                    head.Slot(1).Box(A+new Vector3(0,14.75f,0),new Vector3(2f,.56f,1.7f),Quaternion.identity,Gold);
                    var shackle=Made(kit,"The chain's head",head,DarkMat,GoldMat,LampTeal);shackle.transform.SetParent(chainRoot.transform.childCount>0?chainRoot.transform.GetChild(0):chainRoot.transform,true);shackle.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.On;
                }
                anchor.chain=chainRoot.transform;anchor.link=segment/3;anchor.chainRun=8*segment+36;
                // The four clamps on the chain, each its base and its jaw on a pivot at the hinge, the jaw's free end toward the chain.
                var jaws=new List<Transform>();var axes=new List<Vector3>();var lamps=new List<Renderer>();var lampOf=new List<int>();var clampLights=new List<Light>();var vents=new List<Vector3>();
                var clamps=new GameObject("The four clamps");
                for(int i=0;i<4;i++)
                {
                    var at=Clamp(i);var toA=(A-at);toA.y=0;toA.Normalize();float facing=Mathf.Atan2(toA.x,toA.z)*Mathf.Rad2Deg;
                    var body=kit.Prop(ZoneKit.Kit("AnchorClamp","BridleClamp"),new Vector3(at.x,0,at.z),6,facing+OrielBuilder.Choir.ClampYaw,false);body.name="Clamp "+(i+1);
                    var jaw=SplitJaw(body,toA,out var hinge,out var axis,out bool toward);
                    if(jaw!=null&&!toward)
                    {
                        // The jaw reached away from the chain: the clamp the other way round.
                        UnityEngine.Object.DestroyImmediate(body);body=kit.Prop(ZoneKit.Kit("AnchorClamp","BridleClamp"),new Vector3(at.x,0,at.z),6,facing+OrielBuilder.Choir.ClampYaw+180,false);body.name="Clamp "+(i+1);
                        jaw=SplitJaw(body,toA,out hinge,out axis,out toward);
                    }
                    body.transform.SetParent(clamps.transform,true);body.AddComponent<PropTint>().tint=new Color(1.02f,1.06f,1.12f);
                    ZoneKit.Block(body,at+Vector3.up*2.4f,new Vector3(2.6f,4.8f,2.2f),facing,"Clamp body");ZoneKit.Fadeable(body);
                    if(jaw!=null){var pivot=new GameObject("Clamp "+(i+1)+" hinge").transform;pivot.position=hinge;pivot.SetParent(body.transform,true);jaw.SetParent(pivot,true);jaws.Add(pivot);axes.Add(axis);}
                    // Its lamps: two on its pillar's face toward the platform, hard white while it holds.
                    var lampShape=new ZoneKit.Shape();var outward=-toA;foreach(int k in new[]{-1,1})lampShape.Slot(0).Box(at+outward*1.15f+Vector3.Cross(Vector3.up,outward)*k*.6f+Vector3.up*3.1f,new Vector3(.36f,.5f,.1f),Quaternion.LookRotation(outward),Color.white);
                    // And a lit seam up each side of its pillar, so a clamp reads as a machine and not as carved stone, holding or let go (the look pass: thrown back, they read as stone arches).
                    var seams=new ZoneKit.Shape();foreach(int k in new[]{-1,1})seams.Slot(0).Box(at+outward*1.1f+Vector3.Cross(Vector3.up,outward)*k*1.05f+Vector3.up*1.5f,new Vector3(.12f,2.4f,.08f),Quaternion.LookRotation(outward),Color.white);
                    var seamed=Made(kit,"Clamp "+(i+1)+" seams",seams,LampTeal);seamed.transform.SetParent(body.transform,true);seamed.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                    var lit=Made(kit,"Clamp "+(i+1)+" lamps",lampShape,LampWhite);lit.transform.SetParent(body.transform,true);lit.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;lamps.Add(lit.GetComponent<Renderer>());lampOf.Add(i);
                    var l=PointLight(at+outward*1.8f+Vector3.up*3.2f,7,1.6f,new Color(.82f,.92f,1),"Clamp "+(i+1)+" light");l.transform.SetParent(body.transform,true);clampLights.Add(l);
                    vents.Add(at+toA*1.6f+Vector3.up*3.4f);
                }
                anchor.jaws=jaws.ToArray();anchor.axes=axes.ToArray();anchor.clampLamps=lamps.ToArray();anchor.lampClamp=lampOf.ToArray();anchor.clampLights=clampLights.ToArray();anchor.vents=vents.ToArray();
                anchor.clampAt=new[]{1.1f,2.3f,3.5f,4.7f};
                // The held light: cold lamps over the well and the collar; the freed light: the road's warm from under the bay and through the bow.
                var heldLights=new List<Light>{PointLight(W+W3(-6,7,-2),18,1.5f,new Color(.74f,.88f,1),"Held light"),PointLight(W+W3(6,7,2),18,1.5f,new Color(.74f,.88f,1),"Held light"),PointLight(A+W3(0,8,-2),18,1.8f,new Color(.7f,.86f,1),"Held light")};
                var freedLights=new List<Light>{PointLight(A+W3(0,-4,0),26,3.2f,new Color(1,.78f,.48f),"Freed light (the pit)"),PointLight(A+W3(0,2,12),34,2.6f,new Color(1,.82f,.56f),"Freed light (the bow)"),PointLight(W+W3(0,6,6),22,1.2f,new Color(1,.86f,.66f),"Freed light (the well)")};
                anchor.held=heldLights.ToArray();anchor.freed=freedLights.ToArray();
                anchor.key=GameObject.Find("KeyLight")?.GetComponent<Light>();
                // The road far below the bow: rings of its light, running away north, and its glowing floor.
                var road=new GameObject("The road");var rings=new List<Transform>();var glowing=new List<Renderer>();
                for(int k=0;k<RoadRings;k++)
                {
                    var shape=new ZoneKit.Shape();var path=new List<Vector3>();var rr=new List<float>();float wob=1+.08f*Mathf.Sin(k*1.7f);
                    // A fine line of light with lamps strung along it (the look pass of November 21: solid tubes nearly a metre thick read as flat yellow coils hung in space, s67/210).
                    for(int j=0;j<=36;j++){float a=j*10*Mathf.Deg2Rad;path.Add(new Vector3(Mathf.Sin(a)*RoadRadius*wob,Mathf.Cos(a)*RoadRadius*.62f,0));rr.Add(.26f);}
                    shape.Slot(0).Tube(path,rr,6,Color.white,Color.white,6,.1f,false);
                    for(int j=0;j<18;j++){float a=(j*20+(k%2)*10)*Mathf.Deg2Rad;shape.Slot(0).Lump(new Vector3(Mathf.Sin(a)*RoadRadius*wob,Mathf.Cos(a)*RoadRadius*.62f,0),Vector3.one*.62f,j+k*31,.02f,Color.white,Color.white,8,5);}
                    var go=kit.Built("Road ring "+(k+1),shape,RoadMat);var holder=new GameObject("Road ring "+(k+1)+" (flows)");holder.transform.position=RoadMouth+Vector3.forward*(k*RoadRing);go.transform.SetParent(holder.transform,false);
                    holder.transform.SetParent(road.transform,true);rings.Add(holder.transform);glowing.Add(go.GetComponent<Renderer>());go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                }
                anchor.road=rings.ToArray();anchor.roadGlow=glowing.ToArray();anchor.roadSpan=RoadRings*RoadRing;anchor.roadFlow=Vector3.forward;anchor.roadSpeed=7;
                // Woken, a warm white-gold, not the saturated yellow it was.
                anchor.roadFree=RoadFree;
                // The convoy at the road's mouth, waiting; once the anchor lets go they begin to move into it.
                var convoy=new GameObject("The convoy at the road's mouth");var westward=new GameObject("The convoy's west line");westward.transform.SetParent(convoy.transform,true);var eastward=new GameObject("The convoy's east line");eastward.transform.SetParent(convoy.transform,true);
                foreach(var (id,at,length,yaw,after) in new[]{("Hauler",W3(-34,-40,128),26f,20f,8f),("Skiff",W3(-20,-34,118),9f,10f,9.5f),("SeedBarge",W3(26,-44,132),14f,-15f,10.5f),("PatrolCutter",W3(14,-36,120),12f,-10f,11.5f),
                    ("TallowStationHull",W3(42,-58,142),40f,-30f,13f),("CompactCutter",W3(-6,-30,112),6f,0f,12f)})
                {
                    if(!ZoneKit.Landed(id))continue;
                    var ship=WorldBuilder.Piece(id,Vector3.zero,new Vector3(length*.4f,length*.18f,length),"Rock",false);ship.isStatic=false;ship.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));ship.transform.SetParent((at.x<0?westward:eastward).transform,true);
                    foreach(var view in ship.GetComponentsInChildren<Renderer>())view.shadowCastingMode=ShadowCastingMode.Off;
                    var goes=ship.AddComponent<LeavesAlong>();goes.flag="end.collarTaken";goes.after=after;goes.seconds=36;
                    goes.path=new[]{at+Vector3.forward*18,new Vector3(at.x*.4f,RoadMouth.y+4,RoadMouth.z+30),new Vector3(0,RoadMouth.y,RoadMouth.z+150)};
                    if(released)ship.SetActive(false);
                }
                // A still of the anchor let go (-chamber-released): the jaws thrown back, the chain gone, the road warm, the key warm. Never saved as the zone.
                if(released)
                {
                    for(int i=0;i<jaws.Count;i++)jaws[i].localRotation=Quaternion.AngleAxis(anchor.thrownBack,jaws[i].parent!=null?jaws[i].parent.InverseTransformDirection(axes[i]):axes[i])*jaws[i].localRotation;
                    chainRoot.SetActive(false);foreach(var l in heldLights)l.intensity=0;foreach(var l in clampLights)l.intensity=0;
                    if(anchor.key!=null){anchor.key.color=anchor.keyFree;anchor.key.intensity=anchor.keyFreeIntensity;}
                }
                return anchor;
            }
            /// <summary>Cuts a placed clamp into its base and its jaw (the cut kit pieces the Glass Choir made: the AnchorClamp is the Bridle clamp's mesh in the Compact's white) and finds the
            /// hinge where the jaw meets the pillar, the axis the jaw opens about (its free end rising for a positive angle), and whether its free end reaches toward the chain.</summary>
            static Transform SplitJaw(GameObject body,Vector3 toChain,out Vector3 hinge,out Vector3 axis,out bool toward)
            {
                hinge=body.transform.position;axis=Vector3.right;toward=true;
                var whole=body.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh!=null).OrderByDescending(f=>f.sharedMesh.vertexCount).FirstOrDefault();
                Mesh Part(string part)=>AssetDatabase.LoadAllAssetsAtPath(OrielBuilder.Choir.ClampParts+part+".fbx").OfType<Mesh>().FirstOrDefault();
                Mesh baseMesh=Part("base"),jawMesh=Part("jaw");
                if(whole==null||baseMesh==null||jawMesh==null){Debug.LogWarning("CHAMBER_CLAMP_PARTS_MISSING");return null;}
                var drawn=whole.GetComponent<MeshRenderer>();var was=drawn.bounds;var from=whole.transform;
                var cutBox=baseMesh.bounds;cutBox.Encapsulate(jawMesh.bounds);float unit=whole.sharedMesh.bounds.size.magnitude/Mathf.Max(1e-6f,cutBox.size.magnitude);
                GameObject Piece(string name,Mesh mesh)
                {
                    var piece=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));piece.transform.SetParent(from.parent,false);
                    piece.transform.localPosition=from.localPosition;piece.transform.localRotation=from.localRotation;piece.transform.localScale=from.localScale*unit;
                    piece.GetComponent<MeshFilter>().sharedMesh=mesh;var r=piece.GetComponent<MeshRenderer>();r.sharedMaterials=drawn.sharedMaterials;r.shadowCastingMode=drawn.shadowCastingMode;return piece;
                }
                var stand=Piece("Clamp base",baseMesh);var jaw=Piece("Clamp jaw",jawMesh);
                var now=stand.GetComponent<MeshRenderer>().bounds;now.Encapsulate(jaw.GetComponent<MeshRenderer>().bounds);
                if(Vector3.Distance(now.min,was.min)>.25f||Vector3.Distance(now.max,was.max)>.25f)
                {Debug.LogWarning("CHAMBER_CLAMP_MISFIT was="+was+" cut="+now+" unit="+unit);UnityEngine.Object.DestroyImmediate(stand);UnityEngine.Object.DestroyImmediate(jaw);return null;}
                drawn.enabled=false;
                Vector3 Middle(IReadOnlyCollection<Vector3> points){var sum=Vector3.zero;foreach(var p in points)sum+=p;return sum/Mathf.Max(1,points.Count);}
                var jawPoints=jawMesh.vertices.Select(v=>jaw.transform.TransformPoint(v)).ToArray();var basePoints=baseMesh.vertices.Select(v=>stand.transform.TransformPoint(v)).ToArray();
                float cut=jawPoints.Min(v=>v.y);var pillar=Middle(basePoints.Where(v=>v.y>cut+.4f).ToArray());var free=Middle(jawPoints);
                var across=new Vector3(pillar.x-free.x,0,pillar.z-free.z).normalized;float reach=jawPoints.Max(v=>Vector3.Dot(v,across));
                hinge=Middle(jawPoints.Where(v=>Vector3.Dot(v,across)>reach-.12f).ToArray());axis=Vector3.Cross(Vector3.up,across).normalized;
                if((Quaternion.AngleAxis(10,axis)*(free-hinge)).y<free.y-hinge.y)axis=-axis;
                toward=Vector3.Dot(-across,toChain)>0;
                return jaw.transform;
            }

            // ---------------------------------------------------------------- after her: the people, the talks, the hold
            static void After(ZoneKit kit,AnchorRelease anchor)
            {
                // Her yield, standing: her person in her emergency frame, hidden until the fighter's body hands over; Ilo walks her off once she has her.
                var meret=Person("Meret, yielded","Enemies","MeretWell",MeretPost,20,Color.white,1,"MeretBody");
                if(meret!=null)
                {
                    var stands=new GameObject("Meret yields standing",typeof(YieldsStanding)).GetComponent<YieldsStanding>();stands.body=meret;stands.post=MeretPost;stands.postYaw=20;
                    var off=meret.AddComponent<LeavesAfter>();off.flag=CustodyFlag;off.speed=1.7f;off.patience=6;off.path=new[]{W3(-.8f,Well,56),W3(-.6f,Well,WellSealZ-.6f),W3(-.6f,Well,RampFoot+.6f),W3(-.6f,RampY(38),38)};
                    meret.SetActive(false);
                }
                var yields=new GameObject("Meret yields",typeof(AfterTalk)).GetComponent<AfterTalk>();yields.transform.position=W;yields.flag="bossdown.MeretWell";yields.node="MeretYields";yields.speaker="Meret";yields.heardFlag="end.crewsOrdered";yields.delay=2.4f;
                // The crews: Tovra's smiths to the four clamps from the hatches, Idra's people to the braces, Ishen with the page from the door. On show (out of the view, in the hatches and on the
                // landing) from her yield; they walk in once she has told her crews.
                var crews=new List<GameObject>();
                // Four smiths, four bodies of Harrowgate's (the look pass of November 21: the third wore Tovra's own body and apron, a clone of her across the collar in s67/203 and 238).
                (string name,string id,Color tint,float scale)[] smiths={("Tovra, at the first clamp","TovraBody",Color.white,1),("Brekk, at the second clamp","BrekkBody",Color.white,1),("A smith, at the third clamp","OdrunBody",new Color(.86f,.84f,.9f),.97f),("A smith, at the fourth clamp","BrekkBody",new Color(.9f,.84f,.8f),1.02f)};
                for(int i=0;i<4;i++)
                {
                    var hatch=Hatches[Clamp(i).x<0?0:1];var at=hatch+W3(0,0,i<2?-.9f:.9f);var postAt=SmithPost(i);
                    var smith=Person(smiths[i].name,"Characters",smiths[i].id,at,Clamp(i).x<0?90:-90,smiths[i].tint,smiths[i].scale);
                    Walks(smith,"end.crewsOrdered",new[]{W3(Mathf.Sign(at.x)*8.6f,Well,at.z),W3((postAt.x+Mathf.Sign(at.x)*8.6f)*.5f,Well,(postAt.z+at.z)*.5f),postAt},Clamp(i));crews.Add(smith);
                }
                for(int i=0;i<2;i++)
                {
                    var hatch=Hatches[i];var at=hatch+W3(0,0,i==0?.9f:-.9f);var postAt=Braces[i]+(A-Braces[i]).normalized*-.2f+W3(0,0,1.8f);
                    var hand=Person(i==0?"A rescue hand, at the west brace":"A rescue hand, at the east brace","Characters","IdraBraced",at,i==0?90:-90,i==0?new Color(.82f,.84f,.9f):new Color(.9f,.86f,.84f));
                    Walks(hand,"end.crewsOrdered",new[]{W3(Mathf.Sign(at.x)*8.4f,Well,at.z+.6f),postAt},A);crews.Add(hand);
                }
                var ishen=Person("Ishen, with her page","Characters","IshenBody",W3(-2.2f,Well,RampFoot+.6f),0,Color.white);
                Walks(ishen,"end.crewsOrdered",new[]{W3(-1.6f,Well,WellSealZ+1.6f),W3(-4.5f,Well,60),W3(-4.2f,Well,71),IshenReads},A);crews.Add(ishen);
                Gate("From her yield: the crews, ready to come in","bossdown.MeretWell",true,crews.ToArray());foreach(var c in crews)if(c!=null)c.SetActive(false);
                // Tovra on the radio as the smiths fit the gear (three in turn), then the sequence once they stand.
                var gear=new GameObject("Tovra's smiths fit the gear",typeof(RadioHail)).GetComponent<RadioHail>();gear.transform.position=A;
                gear.speaker="Tovra";gear.situations=new[]{"hail.gear","hail.gear","hail.gear"};gear.needs=new[]{"end.crewsOrdered"};gear.heardFlag="end.gearHeard";gear.silencedBy="end.sequenceRead";gear.range=60;
                var sequence=new GameObject("Ishen reads the sequence",typeof(AfterTalk)).GetComponent<AfterTalk>();sequence.transform.position=A;
                sequence.flag="end.crewsOrdered";sequence.node="IshenAnchorSequence";sequence.speaker="Tovra";sequence.heardFlag="end.sequenceRead";sequence.delay=13.5f;
                // The last collar: its seat south of the ring, facing the chain; the arrow's point beside it (never offered: its range is nothing).
                var seat=new GameObject("The last collar's seat");seat.transform.SetPositionAndRotation(CollarSeat,Quaternion.identity);
                var collarHold=seat.AddComponent<LastCollar>();collarHold.anchor=anchor;collarHold.flag="end.collarTaken";collarHold.requiredFlag="end.sequenceRead";collarHold.report="LastCollar";
                var collarLamps=GameObject.Find("The collar's lamps");if(collarLamps!=null)collarHold.lamp=new[]{collarLamps.GetComponent<Renderer>()};
                collarHold.lampLight=PointLight(CollarSeat+W3(0,1.6f,1.6f),6,.4f,new Color(.55f,.9f,1),"The collar's light");
                var arrow=kit.Discovery("The last collar (step nine's arrow)",W3(CollarSeat.x,0,CollarSeat.z),"Take the last collar","end.collarTaken","LastCollar",null,"Taren",null,0,0);arrow.requiredFlag="end.sequenceRead";
                kit.Sign("THE LAST COLLAR",-4.4f,76.2f,1.3f);
                // Custody: Ilo and two of the civilian crews come in at the door once it has opened, Meret goes with them, and they leave by the shaft together.
                var ilo=Person("Ilo, with the list","Characters","IloBody",W3(1.2f,Well,RampFoot+1.2f),0,Color.white);
                Walks(ilo,"end.anchorReleased",new[]{W3(1.2f,Well,WellSealZ-.4f),IloAt},MeretPost);
                if(ilo!=null){var go=ilo.AddComponent<LeavesAfter>();go.flag=CustodyFlag;go.speed=1.7f;go.patience=6;go.path=new[]{W3(.8f,Well,WellSealZ-.6f),W3(.8f,Well,RampFoot+.6f),W3(.8f,RampY(38),38)};}
                var civilians=new List<GameObject>{ilo};
                for(int i=0;i<CiviliansAt.Length;i++)
                {
                    var c=Person(i==0?"A civilian crew, at the door":"Another, at the door","Characters",i==0?"Mira":"Orrin",W3(i==0?-1.8f:3.4f,Well,RampFoot+1+i),0,i==0?new Color(.86f,.9f,.96f):new Color(.94f,.9f,.86f),i==0?.96f:1.02f);
                    Walks(c,"end.anchorReleased",new[]{W3(CiviliansAt[i].x,Well,WellSealZ-.4f),CiviliansAt[i]},MeretPost);
                    if(c!=null){var go=c.AddComponent<LeavesAfter>();go.flag=CustodyFlag;go.speed=1.7f;go.patience=6;go.path=new[]{W3(CiviliansAt[i].x,Well,WellSealZ-.6f),W3(CiviliansAt[i].x,Well,RampFoot+.6f),W3(CiviliansAt[i].x,RampY(38),38)};}
                    civilians.Add(c);
                }
                Gate("From her yield: Ilo and the civilian crews, waiting on the landing","bossdown.MeretWell",true,civilians.ToArray());foreach(var c in civilians)if(c!=null)c.SetActive(false);
                var custody=new GameObject("Ilo takes her into custody",typeof(AfterTalk)).GetComponent<AfterTalk>();custody.transform.position=IloAt;
                custody.flag="end.anchorReleased";custody.node="IloCustody";custody.speaker="Ilo";custody.heardFlag=CustodyFlag;custody.delay=5.5f;
            }

            // ---------------------------------------------------------------- the floor
            internal static readonly Vector2[][] Ways={new Vector2[]{new(0,RampFoot),new(0,WellSealZ),new(0,63),new(0,CollarSeat.z)}};
            /// <summary>The chamber's floors: polish where feet go, dust at the wall feet, scuffs and a few stains round the collar where work is done, a pool under every lamp.</summary>
            static void Ground(ZoneKit kit,ZoneKit.Land land)
            {
                var ground=GroundKit.Open(kit.zone,land.FloorFilter,97,1.1f,false,v=>{var plan=new Vector2(v.x,v.z);return FloorAt(plan,out float level)<=.15f&&Mathf.Abs(v.y-level)<.3f;});
                float Smooth(float a,float b,float x)=>GroundKit.Smooth(a,b,x);
                bool Collar(Vector2 p){Nearest(p,out var r);return r==Room.Collar;}
                foreach(var way in Ways)ground.ClearLane(way,1.2f);
                ground.Look("Canyon",Art("keel"));
                ground.Layers(new Vector4(0,.06f,.12f,0),1.1f,new GroundKit.Layer("damp",new Color(.88f,.92f,.95f),4),new GroundKit.Layer("veins",new Color(.7f,.74f,.78f),3),new GroundKit.Layer("litter",new Color(.62f,.66f,.7f,.8f),2.4f));
                ground.Paint(0,p=>.35f*Smooth(.58f,.74f,ground.Noise(p,6,3)));
                ground.Paint(2,p=>(Collar(p)?.5f:.15f)*Smooth(.62f,.76f,ground.Noise(p,3,19)));
                ground.Shade(p=>.14f*Smooth(1.6f,.3f,ground.Edge(p)));
                ground.Decals(new[]{"SurveyLine"},3,p=>.5f,1.6f,3.2f,new Color(.92f,.9f,.8f,.6f),false);
                ground.Decals(new[]{"Tracks"},2.2f,p=>Collar(p)?.9f:.3f,.8f,1.6f,new Color(.3f,.32f,.34f,.5f),false);
                ground.Decals(new[]{"Stain"},1.6f,p=>Collar(p)?.7f:.15f,1.2f,2.6f,new Color(.26f,.28f,.3f,.5f),false);
                ground.Decals(new[]{"Litter"},1.4f,p=>Collar(p)?.5f:.1f,.6f,1.2f,new Color(.6f,.62f,.64f,.7f),false);
                ground.LampPools(2.8f,new Color(.86f,.94f,1,.26f));
                ground.Finish();
            }

            // ---------------------------------------------------------------- stills
            /// <summary>LookDev's views: the dock and the deck as the fight uses it (her spawn, each cover anchor, the lift), drawn back; the run down; the landing and the door; the well from its
            /// middle, its sides and its north rim; the collar's seat; the anchor drawn back as the release frames it; wide views of both rooms.</summary>
            internal static readonly LookDev.View[] Views=
            {
                new("arrival",ArrivalAt),new("gangway",GantryAt),new("gantry-south",W3(0,Deck,-11)),new("gantry",MeretGantryAt),new("gantry-west",W3(-11,Deck,-2)),new("gantry-east",W3(11,Deck,0)),new("gantry-north",W3(0,Deck,9)),
                new("gantry-wide",W3(0,Deck,0),false,2.4f),new("run",W3(0,RampY(26),26)),new("shaft",W3(0,RampY(39),39)),new("foot",LiftFoot+Vector3.forward*1.2f),new("well-door",WellCrossing),
                new("well",W),new("well-west",W+W3(-7,0,-1)),new("well-east",W+W3(7,0,1)),new("well-north",W+W3(0,0,9)),new("collar",CollarSeat),new("hatch",Hatches[1]+W3(-3,0,0)),
                new("anchor-wide",W3(0,Well,78),false,2.4f),new("overview-well",W3(0,Well,68),false,3.4f),new("overview-gantry",W3(0,Deck,2),false,3.4f),
            };
        }
    }
}
