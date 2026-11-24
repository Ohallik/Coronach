using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Sorrel's second sky (space combat, with berths on foot; brief, "SorrelSecondSky"): chapter 1's moon seen from its own sky. The haul road's valley between the two ridge spines with Dace's
    /// empty outpost in it, the drill mast, the two apertures the drill cut in the crater's rim, and the crater over the nursery with Stillwater's clamp standing in it, chained out to four anchor
    /// clamps on their shelves; Mother of Doors coiled round the nursery under it (her body is her fight's). After her release, the lane east to Last Mooring and the young's own road north.
    /// Map brief, the rule, every coordinate and the contracts: docs/maps/SorrelSecondSky.md. Every place another file needs is a constant of <see cref="Sorrel"/>.
    /// <para>The rule is "fly the ridge tops": every lane is at the crests' height and the moon is drawn 35 to 85 m under the ships, so the floor fills every frame and the edges are the rock
    /// a player can see. Nothing stands above the plane on the camera's side of the arena (wiring 6.2, "Camera"). The moon's surface has no collider (nothing walks on it); the stops are boxes
    /// at the plane on the crest lines, with teeth of rock standing through the plane outside them.</para>
    /// </summary>
    public static partial class LastMooringBuilder
    {
        /// <summary>The zone's own fields on its ZoneDef, set last inside <c>Define</c>'s lambda for it.</summary>
        static void SorrelDefinition(ZoneDef z){}

        /// <summary>Sorrel's look probe for its haze (marker SORREL_HAZE_STILLS_OK, <c>-look-run name</c>, written to Builds/quality/look/name): the views a player's camera stood at in the
        /// whole-game capture and the wide ones, each drawn twice, with the zone's fog as the lens alone has it (<c>-lens</c>) and as <see cref="PlaneHaze"/> sets it for that camera
        /// (<c>-plane</c>). LookDev draws a whole run in one editor frame, which a haze set as a camera begins to draw doesn't reach; this sets it for each camera before it draws.</summary>
        public static void SorrelHazeStills()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-look-run")??throw new ArgumentException("-look-run <name> is required");
            string folder=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../../Builds/quality/look",run));
            if(System.IO.Directory.Exists(folder))throw new InvalidOperationException("Preserve earlier look run: "+folder);
            System.IO.Directory.CreateDirectory(folder);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+SorrelZone+".unity");
            foreach(var text in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))text.gameObject.SetActive(false);
            var def=AssetDatabase.FindAssets("t:ZoneDef").Select(g=>AssetDatabase.LoadAssetAtPath<ZoneDef>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(d=>d.id==SorrelZone);if(def!=null)RenderSettings.skybox=def.skybox;
            var haze=UnityEngine.Object.FindFirstObjectByType<PlaneHaze>()??throw new InvalidOperationException("SORREL_HAZE no PlaneHaze in the scene");
            var profile=AssetDatabase.LoadAssetAtPath<CameraProfile>("Assets/_Project/Resources/Definitions/CameraProfile/Flight.asset");
            var camera=new GameObject("Haze camera",typeof(Camera),typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData)).GetComponent<Camera>();
            camera.clearFlags=RenderSettings.skybox!=null?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;camera.nearClipPlane=.3f;camera.farClipPlane=1600;camera.fieldOfView=profile.fov;
            var data=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.antialiasing=UnityEngine.Rendering.Universal.AntialiasingMode.None;
            var rotation=Quaternion.Euler(profile.pitch,profile.yaw,0);int count=0;
            foreach(var view in Sorrel.Views.Where(v=>v.name.EndsWith("as-played")||v.name.EndsWith("pulling-out")||v.name.EndsWith("-wide")))
            {
                camera.transform.SetPositionAndRotation(view.point-rotation*Vector3.forward*profile.distance*Mathf.Max(1,view.zoom),rotation);
                foreach(bool plane in new[]{false,true})
                {
                    haze.enabled=false;if(plane){haze.enabled=true;haze.ApplyFor(camera);}
                    var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=4};camera.targetTexture=rt;camera.Render();if(plane)haze.ApplyFor(camera);camera.Render();
                    var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(folder,SorrelZone+"-"+view.name+(plane?"-plane":"-lens")+".png"),image.EncodeToPNG());
                    RenderTexture.active=prior;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);count++;
                    Debug.Log($"SORREL_HAZE {view.name} {(plane?"plane":"lens")} fog {RenderSettings.fogStartDistance:0} to {RenderSettings.fogEndDistance:0}");
                }
            }
            UnityEngine.Object.DestroyImmediate(camera.gameObject);
            Debug.Log($"SORREL_HAZE_STILLS_OK count={count} folder={folder}");
        });

        [ZoneLook.Source] static void SorrelLooks()
        {
            // Sorrel's long afternoon, cooled: the same warm key on the same ochre (chapter 1's sun, a little lower and further round, so each spine lays its shadow across the valley),
            // a cooler sky fill than chapter 1's because something stands between the moon and its sky now, a dust haze that takes the crater's depths back, and the clamp's cyan works and the
            // young's glow what blooms.
            ZoneLook.Register(SorrelZone,new ZoneLook.Spec{sun=C(1f,.9f,.76f),sunIntensity=1.5f,sunPitch=38,shadowStrength=.8f,
                sky=C(.4f,.47f,.62f),equator=C(.44f,.38f,.36f),ground=C(.3f,.21f,.16f),
                fog=true,fogColor=C(.62f,.5f,.5f),fogStart=Sorrel.HazeFrom,fogEnd=Sorrel.HazeTo,
                contrast=14,saturation=8,splitShadows=C(.4f,.45f,.6f),splitHighlights=C(.6f,.53f,.44f),
                bloomThreshold=.9f,bloomIntensity=.7f,vignette=.22f});
        }

        internal static class Sorrel
        {
            // ---------------------------------------------------------------- the numbers (docs/maps/SorrelSecondSky.md reads these; so do the tests and the routes)
            internal const float Plane=ZoneKit.FlightPlane;
            /// <summary>The moon's floor in the haul road's valley and the basins round it; the crater's floor; every shelf's floor, 3 m under the plane: Idra's hull reaches 2.17 m round a
            /// middle .8 over it (to y -.37), and a shelf's works stand 1.25 m and its rail half a metre over its floor, so no ship meets a shelf it flies over.</summary>
            internal const float FloorY=-26,CraterFloor=-86,ShelfY=-2f;
            /// <summary>The valley: the lane between the spines, <see cref="ValleyHalf"/> either side of x 0 from <see cref="CanyonSouth"/> to <see cref="FunnelFrom"/>, narrowing from there to
            /// <see cref="Half"/> at <see cref="FunnelTo"/> and running on at that to the crater's rim. Eighty metres wide, so the valley's floor fills the frame and its slopes stand at the sides:
            /// at thirty (the first build) every frame of it was two cliff faces and a strip of floor.</summary>
            internal const float ValleyHalf=40,Half=15,CanyonSouth=-14,FunnelFrom=190,FunnelTo=232,RimLine=242;
            internal static float HalfAt(float z)=>Mathf.Lerp(ValleyHalf,Half,Mathf.InverseLerp(FunnelFrom,FunnelTo,z));
            /// <summary>The two apertures in the rim (x from <see cref="ApertureInner"/> to <see cref="Half"/> either side), with the pillar between them.</summary>
            internal const float ApertureInner=4;
            /// <summary>The crater: its middle <see cref="Centre"/>, flyable to <see cref="CraterR"/>; the chain line across it at <see cref="ChainZ"/> (solid until <c>end.motherFreed</c>).</summary>
            internal static readonly Vector2 Centre=new(0,330);
            internal const float CraterR=74,ChainZ=352;
            /// <summary>Mother of Doors' fight is the crater's inner ring (her contract in the map doc): the seats on <see cref="SeatR"/>, the shelves' middles on <see cref="ShelfR"/>, her coil
            /// round the nursery on r 16 just under the plane (nothing of the zone's from r <see cref="CoilIn"/> to <see cref="CoilOut"/> between y -9 and the plane), the anchor clamps on
            /// <see cref="AnchorR"/>; the nursery's crown inside r <see cref="CoilOut"/> is under y -9.</summary>
            internal const float SeatR=23,ShelfR=27,AnchorR=17,CoilIn=11,CoilOut=21;
            /// <summary>The lane east to Last Mooring (its middle at z <see cref="EastZ"/>, <see cref="EastHalf"/> either side, to x <see cref="EastEnd"/>).</summary>
            internal const float EastZ=322,EastHalf=16,EastEnd=168;
            /// <summary>The young's road: three legs 10 m wide (x 0 north, x 22 south, x 44 north) joined by two turns, the first at its north end, the second at its south.</summary>
            internal const float RoadHalf=6,RoadSouth=416,RoadTurn1=480,RoadNorth=494,RoadTurn2=430,RoadEnd=524;
            internal static readonly float[] RoadLegs={0,22,44};

            /// <summary><see cref="Checkpoint"/> is where a defeat at her door returns (spawn <c>Clamps</c>): in the east aperture, short of Dace's crossing, 57 m from her door's middle.
            /// It stood at the valley's head (<see cref="ValleyHead"/>), 70 m from it and facing the pillar between the apertures, so every retry was a 70 m flight back round the pillar
            /// (QaStoryWorldTests.EveryBossDoorIsACheckpoint: under 60). Here a straight flight north clears the pillar into her crater, both ships arrive between the aperture's walls
            /// (Taren's and Idra's hulls 4.15 m apart, a quarter of a metre clear of each wall whichever leads), and neither touches the crossing's box (from z 248) or hers (from z 256).
            /// Where she is seen from on the way in stays the valley's head.</summary>
            internal static readonly Vector3 Arrival=new(0,Plane,0),HomeWarp=new(-10,Plane,-4),Checkpoint=new(7.45f,Plane,245),ValleyHead=new(0,Plane,232),Crossing=new(0,Plane,250),
                MotherAt=new(Centre.x,Plane,Centre.y),MotherExit=new(0,Plane,400),Road=new(-2.2f,Plane,410),RoadStart=new(0,Plane,418),RoadFarEnd=new(44,Plane,514),
                WarpOn=new(EastEnd-10,Plane,EastZ),Lane1=new(0,Plane,95),Lane2=new(0,Plane,175),Clamp=new(0,CraterFloor,356),Drill=new(-6,FloorY,212),Bore=new(6,FloorY,222);
            internal static readonly Vector3 DoorCentre=new(0,Plane,302),DoorSize=new(148,8,92),LaneFightSize=new(2*ValleyHalf,8,28),GunsSize=new(26,8,26);
            internal static readonly Vector3[] Cover={new(0,Plane,298),new(-21.6f,Plane,306.4f),new(21.6f,Plane,306.4f)};
            internal const float CoverRadius=3f,ClampHeight=72;
            /// <summary>The dust haze's linear fog as the flight camera has it at its play distance (ZoneLook's numbers). A view drawn back has it moved out by however much further it
            /// stands (<see cref="PlaneHaze"/>): from the valley's head her door frames the party and her from three hundred metres, and these numbers alone covered the crater in it.</summary>
            internal const float HazeFrom=70,HazeTo=330;

            /// <summary>A shelf beside one of the four anchor clamps, turned to face the crater's middle: its works on the edge toward her (the seat a metre in from it, on r 23), its floor 14 m
            /// across by 10 deep under the plane, the foot spawn on the edge away from her, the pad over its middle, the launch on its floor to one side, and the spawn a save made on it names
            /// (in flight, outward past her lanes). Its berth's frame (<see cref="Turn"/>) has +z toward the middle.</summary>
            internal sealed class Shelf
            {
                public readonly int index;public readonly string name,sign,flag,guns,berth,returnSpawn,needs;public readonly float bearing;
                public readonly Vector3 seat,middle,foot,pad,launch,flight,back,anchor,works;public readonly Vector2 outward,across;
                public const float Wide=14,Deep=10;
                public Shelf(int i,string name,string sign,float bearing)
                {
                    index=i;this.name=name;this.sign=sign;this.bearing=bearing;flag="end.clamp"+i;guns="Sorrel_Clamp"+i;berth="Clamp"+i;returnSpawn="Shelf"+i;needs=i==1?"end.sorrelSeen":"end.clamp"+(i-1);
                    float a=bearing*Mathf.Deg2Rad;outward=new Vector2(Mathf.Sin(a),Mathf.Cos(a));across=new Vector2(outward.y,-outward.x);
                    Vector3 R(float r,float side,float y){var p=Centre+outward*r+across*side;return new Vector3(p.x,y,p.y);}
                    seat=R(SeatR,0,ShelfY);middle=R(ShelfR,0,ShelfY);foot=R(ShelfR+3.5f,0,ShelfY);launch=R(ShelfR+2.5f,4.5f,ShelfY);works=R(SeatR-.9f,0,ShelfY);
                    pad=R(ShelfR,0,Plane);flight=R(ShelfR+3,0,Plane);back=R(40,0,Plane);anchor=R(AnchorR,0,Plane);
                }
                /// <summary>The shelf's frame: +z toward the crater's middle, +x along its inner edge.</summary>
                public Quaternion Turn=>Quaternion.Euler(0,bearing+180,0);
                /// <summary>A point of the shelf by its own frame (x along, z toward her), at a height.</summary>
                public Vector3 At(float x,float z,float y)=>new Vector3(middle.x,y,middle.z)+Turn*new Vector3(x,0,z);
                /// <summary>The berth's box: the floor to 1.2 m over it, in the shelf's frame.</summary>
                public Bounds Box=>new(middle+Vector3.up*.6f,new Vector3(Wide,1.2f,Deep));
            }
            internal static readonly Shelf[] Shelves={new(1,"The west clamp's works","WEST CLAMP",240),new(2,"The south-west clamp's works","SOUTH-WEST CLAMP",205),new(3,"The south-east clamp's works","SOUTH-EAST CLAMP",155),new(4,"The east clamp's works","EAST CLAMP",120)};

            /// <summary>LookDev's stills: where a player arrives and stops, the canyon as flown, both fights, the apertures and the crossing, each shelf's pad from the plane and one on foot, the
            /// crater and the clamp, the way on, the young's road, and wide views of the canyon, the crater and the road.</summary>
            internal static LookDev.View[] Views=>new[]
            {
                new LookDev.View("arrival",Arrival,true),new("canyon-40",new Vector3(0,Plane,40),true),new("lane-1",Lane1,true),new("beacons",new Vector3(6,Plane,130),true),new("lane-2",Lane2,true),
                new("drill",new Vector3(-2,Plane,200),true),new("apertures",new Vector3(0,Plane,228),true),new("crossing",new Vector3(-9,Plane,250),true),new("arena-mouth",new Vector3(9,Plane,266),true),
                new("shelf-1",Shelves[0].pad,true),new("shelf-2",Shelves[1].pad,true),new("shelf-3",Shelves[2].pad,true),new("shelf-4",Shelves[3].pad,true),
                new("crater",new Vector3(0,Plane,300),true),new("ring-west",new Vector3(-30,Plane,318),true),new("clamp",new Vector3(0,Plane,345),true),new("east-lane",new Vector3(100,Plane,EastZ),true),new("warp-on",WarpOn+Vector3.back*4,true),
                new("road-start",RoadStart,true),new("road-turn",new Vector3(11,Plane,487),true),new("road-back",new Vector3(22,Plane,452),true),new("road-end",RoadFarEnd+Vector3.back*4,true),
                new("shelf-2-foot",Shelves[1].middle,false),new("shelf-4-foot",Shelves[3].middle,false),
                new("canyon-wide",new Vector3(0,Plane,120),true,3),new("crater-wide",new Vector3(0,Plane,300),true,3.2f),new("road-wide",new Vector3(22,Plane,468),true,2.6f),
                // Where the player's camera stood in the whole-game capture (November 20): the lane fight as flown (s62/121), the view drawn back from the valley's head to show her
                // (s63/010: its axis met the plane at (-0.6, 282.5), 314 m back), and half way out to it as the second lane's fight ended (s62/153: (-0.6, 257), 263 m).
                new("lane-1-as-played",new Vector3(0,Plane,83),true,1.29f),new("door-as-played",new Vector3(-.6f,Plane,282.5f),true,12.07f),new("door-pulling-out",new Vector3(-.6f,Plane,257.2f),true,10.1f),
            };

            // ---------------------------------------------------------------- where the air is
            /// <summary>Signed distance on the plan into the air a ship flies in (positive inside), and which part of it: 0 the canyon, 1 an aperture, 2 the crater, 3 the lane east, 4 the road.</summary>
            internal static float Air(Vector2 p,out int part)
            {
                float best=float.NegativeInfinity;int which=0;
                void Take(float d,int k){if(d>best){best=d;which=k;}}
                {
                    // The valley, its sides slanting in along the funnel: the distance in from them along x, which is near enough for the ground's shape (the walls are laid on the true lines).
                    float h=HalfAt(p.y),slant=p.y>FunnelFrom&&p.y<FunnelTo?Mathf.Cos(Mathf.Atan2(ValleyHalf-Half,FunnelTo-FunnelFrom)):1;
                    float side=(h-Mathf.Abs(p.x))*slant,ends=Mathf.Min(p.y-CanyonSouth,RimLine-p.y);
                    Take(side>0&&ends>0?Mathf.Min(side,ends):-new Vector2(Mathf.Max(0,-side),Mathf.Max(0,-ends)).magnitude,0);
                }
                Take(Box(p,-Half,-ApertureInner,RimLine-2,RimLine+20),1);Take(Box(p,ApertureInner,Half,RimLine-2,RimLine+20),1);
                Take(CraterR-Vector2.Distance(p,Centre),2);
                Take(Box(p,60,EastEnd,EastZ-EastHalf,EastZ+EastHalf),3);
                foreach(var (x0,x1,z0,z1) in RoadParts)Take(Box(p,x0,x1,z0,z1),4);
                part=which;return best;
            }
            /// <summary>The young's road as boxes on the plan: three legs and the two turns joining them.</summary>
            internal static readonly (float x0,float x1,float z0,float z1)[] RoadParts=
            {
                (-RoadHalf,RoadHalf,CraterR+Centre.y-8,RoadNorth),(-RoadHalf,RoadLegs[1]+RoadHalf,RoadTurn1,RoadNorth),(RoadLegs[1]-RoadHalf,RoadLegs[1]+RoadHalf,RoadSouth,RoadNorth),
                (RoadLegs[1]-RoadHalf,RoadLegs[2]+RoadHalf,RoadSouth,RoadTurn2),(RoadLegs[2]-RoadHalf,RoadLegs[2]+RoadHalf,RoadSouth,RoadEnd),
            };
            static float Box(Vector2 p,float x0,float x1,float z0,float z1)
            {
                float dx=Mathf.Max(x0-p.x,p.x-x1),dz=Mathf.Max(z0-p.y,p.y-z1);
                if(dx<=0&&dz<=0)return Mathf.Min(-dx,-dz);
                return -new Vector2(Mathf.Max(dx,0),Mathf.Max(dz,0)).magnitude;
            }
            internal static bool InAir(Vector2 p,float margin=0)=>Air(p,out _)>margin;
            /// <summary>The camera's side of the arena: here no rock stands above the plane (wiring 6.2, "Camera"; 10, "Nothing drawn stands between the fixed camera and a fight").</summary>
            internal static bool Low(Vector2 p)=>p.y>RimLine-24&&p.y<Centre.y+6&&Vector2.Distance(p,Centre)<CraterR+30;

            // ---------------------------------------------------------------- the moon
            static float Noise(Vector2 p,float metres,float seed)=>Mathf.PerlinNoise(p.x/metres+seed*13.1f,p.y/metres+seed*7.7f);
            /// <summary>The moon's floor away from the lanes: the basins at <see cref="FloorY"/> with a little relief, and the lower ridges of chapter 1's country running north beside the spines.</summary>
            static float Country(Vector2 p)
            {
                float y=FloorY+3*(Noise(p,60,1)-.5f)+1.2f*(Noise(p,14,2)-.5f);
                // Outer spines, lower than the lane's, crested 8 to 14 m under the plane: the skyline a ship sees past the valley's own.
                foreach(var (x,wave,crest,wide) in new[]{(-88f,9f,-9f,16f),(94f,7f,-11f,14f),(160f,12f,-14f,22f)})
                {
                    float off=Mathf.Abs(p.x-(x+wave*Mathf.Sin(p.y/53+x)));float r=Mathf.Clamp01(1-off/wide);r=r*r*(3-2*r);
                    float top=crest+5*(Noise(p,24,x)-.5f);y=Mathf.Max(y,Mathf.Lerp(FloorY,top,r));
                }
                // The crater's thrown-out apron, a raised ring round its rim.
                float rim=Vector2.Distance(p,Centre)-CraterR;if(rim>0)y=Mathf.Max(y,Mathf.Lerp(-6,FloorY,ZoneKit.Smooth(6,70,rim))+2*(Noise(p,10,9)-.5f));
                return y;
            }
            /// <summary>The crater by distance from its middle: the nursery's crown under her head, the ledge her coil lies on (both under y -9, her contract), the crown's cliff down into the moat,
            /// the moat round it where Stillwater's clamp stands, and the outer wall in terraces up to the rim at the plane.</summary>
            static float Bowl(float r,Vector2 p)
            {
                float n=1.6f*(Noise(p,9,4)-.5f);
                // On the south half the crown's ledge runs on under her fight's ring, so the shelves and the cover stand on rock 10 m under them, not on spires out of a pit; on the north it
                // drops at her coil's edge into the moat where Stillwater's clamp stands.
                var d=p-Centre;float bearing=Mathf.Abs(Mathf.DeltaAngle(Mathf.Atan2(d.x,d.y)*Mathf.Rad2Deg,180));
                float north=ZoneKit.Smooth(70,105,bearing),edge=Mathf.Lerp(37,CoilOut,north),cliff=edge+9,moat=Mathf.Lerp(-30,CraterFloor,north);
                if(r>=CraterR)return Plane;
                float shelf=Mathf.Lerp(-18,-36,north),under=Mathf.Lerp(-22,-30,north);
                if(r>64)return Mathf.Lerp(under+n,Plane,Strata(Mathf.InverseLerp(64,CraterR,r)));
                if(r>58)return Mathf.Lerp(shelf,under,ZoneKit.Smooth(58,64,r))+n;
                if(r>50)return Mathf.Lerp(moat+n,shelf,Strata(Mathf.InverseLerp(50,58,r)));
                if(r>cliff)return moat+n;
                if(r>edge)return Mathf.Lerp(moat,-11.5f,Strata(Mathf.InverseLerp(cliff,edge,r)));
                if(r>CoilIn)return -11.5f+.5f*n;
                return -10.6f+.4f*n-.8f*ZoneKit.Smooth(6,0,r);
            }
            /// <summary>A wall's rise with a ledge or two in it, so a cliff reads as strata from above and not as a smooth funnel.</summary>
            static float Strata(float t){t=Mathf.Clamp01(t);float steps=3,s=t*steps,f=s-Mathf.Floor(s);return Mathf.Clamp01((Mathf.Floor(s)+ZoneKit.Smooth(.25f,.85f,f))/steps);}
            /// <summary>The ground under a point of the plan.</summary>
            internal static float Height(Vector2 p)
            {
                float air=Air(p,out int part);
                if(air>0)
                {
                    if(part==2)return Bowl(Vector2.Distance(p,Centre),p);
                    // The apertures and the lane east are cut shallow, with their walls short and stepped (the look pass of November 14: at -11 and -12 under walls 4 and 10 m across,
                    // their frames were strata from edge to edge and a strip of floor). Drawn ground only: the moon has no collider, and the air is Air's.
                    float floor=part switch{1=>-7f,3=>-8.6f,4=>-58f,_=>FloorY+1.2f*(Noise(p,9,3)-.5f)};
                    // The valley climbs north: gently from the second fight to a bench where the drill stands over its bore, and on up to the apertures' floor, the haul road with it
                    // (the cut the drill made). From the checkpoint the valley's head is ground a ship looks down on, its walls half as tall, not a wall 27 m high with two slots in it.
                    if(part==0)floor=Mathf.Lerp(Mathf.Lerp(floor,-12f,ZoneKit.Smooth(150,212,p.y)),-7f,ZoneKit.Smooth(224,241,p.y));
                    // The haul road is laid flat on the valley's floor (Floor paints it): sunk 0.6 m on a 2.5 m grid, its sides were facets the sun lit as pale squares down the middle
                    // (whole-game capture, November 20, s62/121).
                    float run=part==4?2.5f:part==1?1.8f:part==0?Mathf.Lerp(16,7,ZoneKit.Smooth(196,226,p.y)):part==3?4:10;
                    return Mathf.Lerp(Plane,floor,Strata(air/run));
                }
                // Outside the air: the crest at the plane on the line, a little over it just outside, and down the far side to the country; on the camera's side of the arena never over the plane.
                float d=-air,low=Low(p)?.8f:2.5f+1.5f*(Noise(p,7,6)-.5f);
                float crest=Mathf.Lerp(Plane-.2f,low,ZoneKit.Smooth(0,3,d));
                return Mathf.Max(Country(p),Mathf.Lerp(crest,Country(p),ZoneKit.Smooth(4,20,d)));
            }
            /// <summary>The haul road's line down the valley.</summary>
            internal static float HaulX(float z)=>2.2f*Mathf.Sin(z/37f)-1;

            // ---------------------------------------------------------------- the build
            internal sealed class Stuff{public Material ground,rock,crag,works,worksDark,cable,lamp,young,glow,warm,shaft,spill;}
            static Color L(float r,float g,float b)=>new Color(r,g,b).linear;

            /// <summary>Builds SorrelSecondSky.</summary>
            internal static bool Build()
            {
                var kit=ZoneKit.Ground(SorrelZone,Height,500);kit.fadeTall=false;
                var scene=WorldBuilder.Begin(kit.zone);
                var key=GameObject.Find("KeyLight");if(key!=null)key.transform.rotation=Quaternion.Euler(38,62,0);
                var stuff=Materials(kit);
                var arrival=WorldBuilder.Spawn("Arrival",Arrival);arrival.transform.rotation=Quaternion.identity;
                WorldBuilder.Spawn("Clamps",Checkpoint);WorldBuilder.Spawn("Road",Road);
                kit.DriftingSky();
                var keep=Keep();

                // ---- the moon under the lanes
                var land=kit.Terrain("The moon",-230,250,-60,650,2.5f,stuff.ground,stuff.rock,Tone,L(1f,.94f,.9f),L(.86f,.76f,.7f));
                foreach(var c in land.root.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
                Teeth(kit,stuff);
                Outpost(kit,stuff,keep);DrillSite(kit,stuff);Beacons(kit,stuff);Ground(kit);
                var glow=kit.Glow("Dust",false,new Color(1.2f,1.05f,.95f,1));
                kit.Drift("Dust in the valley",new Vector3(0,-14,110),new Vector3(44,30,240),glow,new Color(1,.88f,.78f,.22f),360,.08f,.2f,.15f,22);
                kit.Drift("Dust over the crater",new Vector3(0,-30,330),new Vector3(140,50,150),glow,new Color(.8f,.95f,1f,.2f),420,.1f,.26f,.12f,24);

                // ---- the crater: the clamp, its chains, the anchor clamps and their shelves, the nursery
                var (taut,slack)=TheClamp(kit,stuff);
                var shelfRoots=new List<GameObject>();foreach(var s in Shelves)shelfRoots.Add(ShelfWorks(kit,stuff,s));
                BakeShelves(shelfRoots);
                // The fourth shelf hangs off its anchor clamp's arm and swings (Mother's strain drives it through its berth): nothing of it is static.
                foreach(var t in shelfRoots[3].GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=false;
                Nursery(kit,stuff,out var young);
                var cover=CoverRocks(kit,stuff);

                // ---- the walls, and the chains that close the way on until she goes
                var walls=Walls(out var chainWalls);

                // ---- the fights
                string interceptor=Foe("StillwaterInterceptor","Interceptor");
                kit.FlightFight("Sorrel_Lane1",Lane1,LaneFightSize,(interceptor,2));
                kit.FlightFight("Sorrel_Lane2",Lane2,LaneFightSize,(interceptor,2));
                string gun=Foe("ClampGun","FlakGate");
                var gunFights=new List<EncounterVolume>();
                foreach(var s in Shelves)
                {
                    // Its guns stand on the anchor clamp's body either side, at the plane; the clamps are taken in order, so each one's guns wake only once the clamp before is open.
                    var fight=WorldBuilder.Encounter(s.guns,new Vector3(s.seat.x,Plane,s.seat.z),GunsSize,gun,1);var side=new Vector3(s.across.x,0,s.across.y)*4.5f;
                    var first=fight.spawners[0];first.transform.position=s.anchor-side;first.radius=0;
                    fight.spawners=new[]{first,ZoneKit.Spawn(fight,gun,1,s.anchor+side,0)};fight.requiredFlag=s.needs;fight.checkpointSpawn="";
                    // Crossed: each gun turns 35 degrees toward the other (across is the shelf's right as it faces out), so their curtains meet over the shelf and neither has a lee.
                    for(int g=0;g<fight.spawners.Length;g++){var sp=fight.spawners[g];sp.faceAsPlaced=true;sp.transform.rotation=Quaternion.Euler(0,s.bearing+(g==0?35:-35),0);}
                    gunFights.Add(fight);
                }
                var door=Door();

                // ---- what is said, and the arrow's points
                AtPlane(kit.Crossing("Dace over his ridge",new Vector3(Crossing.x,0,Crossing.z),new Vector3(2*Half,8,4),"end.sorrelSeen","SurvivorSecondSky","Survivor"));
                for(int i=0;i<Shelves.Length;i++)
                {
                    var s=Shelves[i];
                    var wanted=kit.Discovery(s.name+" is shut",new Vector3(s.pad.x,0,s.pad.z),"Take the clamp's weight",s.flag,"Clamp",null,"Sela",null,0,WorldBuilder.FlightDockRange);AtPlane(wanted);
                    wanted.previewEncounter=gunFights[i];wanted.requiredFlag=s.needs;wanted.transform.SetParent(shelfRoots[i].transform,true);
                }
                var goes=new GameObject("Dace: she goes",typeof(AfterTalk)).GetComponent<AfterTalk>();goes.transform.position=MotherAt;
                goes.flag="bossdown.MotherOfDoors";goes.node="SurvivorMotherGoes";goes.speaker="Survivor";goes.heardFlag="end.motherFreed";goes.delay=6;goes.range=0;
                // Nothing barks for eight seconds after his line (wiring 2); the young go up her road after her from three seconds in, and are gone by twelve (her contract).
                var quiet=new GameObject("Quiet after Dace's line",typeof(QuietAfter)).GetComponent<QuietAfter>();quiet.flag="end.motherFreed";quiet.seconds=8;
                var follow=young.AddComponent<LeavesAlong>();follow.flag="bossdown.MotherOfDoors";follow.after=3;follow.seconds=9;
                follow.path=new[]{young.transform.position+Vector3.up*8,new Vector3(MotherExit.x,-12,MotherExit.z-40),MotherExit+Vector3.down*6,MotherExit+new Vector3(0,-4,60)};
                var exit=new GameObject("Her way out",typeof(BossExit)).GetComponent<BossExit>();exit.transform.position=MotherExit;exit.transform.rotation=Quaternion.identity;exit.boss="MotherOfDoors";
                Sight("young",new Vector3(Centre.x,Plane,Centre.y-20),40,"sorrel.youngSeen","end.sorrelSeen");

                // ---- the ways out
                var home=WorldBuilder.Warp("Warp — The Hull of Many Hands",HomeWarp,Hull,"",SorrelSpawn);home.lockedPrompt="";home.range=WorldBuilder.FlightDockRange;
                WorldBuilder.Label("Warp — The Hull of Many Hands",HomeWarp+new Vector3(0,-.7f,-5),2.2f);
                // Her fight holds the way home while it is under way (the warp is 330 m down the valley, past her fight's reach, and a ship flown back to it mid-fight left with
                // nothing said: QaPlanSystemsTests.NoWayOutAnswersInEveryLaterBossFight). Open before it and after it, as built.
                if(door!=null){var holds=new GameObject("Her fight holds the way home",typeof(FightHoldsTheWay)).GetComponent<FightHoldsTheWay>();holds.fight=door;holds.way=home;
                    holds.until="bossdown.MotherOfDoors";holds.saying="Free Mother of Doors first: she is still chained in the crater";}
                var on=WorldBuilder.Warp("Warp — Last Mooring",WarpOn,MooringZone,"","Arrival");on.lockedPrompt="";on.range=WorldBuilder.FlightDockRange;
                WorldBuilder.Label("Warp — Last Mooring",WarpOn+new Vector3(0,-.7f,-5),2.2f);
                var onSigns=scene.GetRootGameObjects().Where(g=>g.name=="Sign_Warp — Last Mooring").ToArray();

                // ---- the haze, measured from the plane wherever the view stands
                var haze=new GameObject("The dust haze, from the plane",typeof(PlaneHaze)).GetComponent<PlaneHaze>();haze.plane=Plane;haze.start=HazeFrom;haze.end=HazeTo;
                var flightView=AssetDatabase.LoadAssetAtPath<CameraProfile>("Assets/_Project/Resources/Definitions/CameraProfile/Flight.asset");haze.lens=flightView!=null?flightView.distance:26;

                // ---- the young's road (LastSurvey): open from end.motherFreed, saved at its start, its far end the quest's point
                var road=YoungsRoad(kit,stuff);
                var begun=kit.Crossing("The young's road begins",new Vector3(RoadStart.x,0,RoadStart.z),new Vector3(2*RoadHalf,8,3),"end.roadBegun","","Sela","end.motherFreed");begun.saveSpawn="Road";AtPlane(begun);
                var far=kit.Discovery("The young's road, its far end",new Vector3(RoadFarEnd.x,0,RoadFarEnd.z),"Mark the road's far end","end.surveyFlown","NurseryRoad",null,"Sela","LastSurvey",0,WorldBuilder.FlightDockRange);
                far.requiredFlag="end.motherFreed";AtPlane(far);

                // ---- before and after: the chains close the crater's north and the east notch until she has gone; the warp on and the road's marks are drawn from then
                Gate("Until she is freed: Stillwater's chains",false,chainWalls.Concat(taut).ToArray(),"end.motherFreed");
                Gate("Once she is freed: the way on and the young's road",true,new[]{on.gameObject,road}.Concat(onSigns).Concat(slack).Where(g=>g!=null).ToArray(),"end.motherFreed");

                // ---- last, once everything stands: the moon's floor laid as chapter 1 laid it
                Floor(land);

                WorldBuilder.Save(scene,kit.zone);
                Debug.Log("MOORING_COORDS SorrelSecondSky arrival="+Arrival+" home="+HomeWarp+" clamps="+Checkpoint+" valley-head="+ValleyHead+" crossing="+Crossing+" mother="+MotherAt+" exit="+MotherExit+" warp-on="+WarpOn+" road="+Road+" road-end="+RoadFarEnd
                    +" shelves="+string.Join(" ",Shelves.Select(s=>s.index+":seat"+s.seat+"/foot"+s.foot+"/pad"+s.pad+"/back"+s.back))+" cover="+string.Join(" ",Cover.Select(c=>c.ToString()))
                    +" walls="+walls+" door="+(door!=null)+" coverRocks="+cover);
                Debug.Log("SORREL_SECOND_SKY_OK");
                return true;
            }

            /// <summary>What must stay readable over the backdrop, each with the room it needs: the ways, the pads, the crossing.</summary>
            static List<(Vector2 at,float clear)> Keep()
            {
                Vector2 F(Vector3 p)=>new(p.x,p.z);
                var keep=new List<(Vector2,float)>{(F(Arrival),5),(F(HomeWarp),6),(F(WarpOn),7),(F(RoadFarEnd),6)};
                foreach(var s in Shelves)keep.Add((F(s.pad),6));
                return keep;
            }

            static Stuff Materials(ZoneKit kit)
            {
                var s=new Stuff();
                var groundMap=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Textures/sorrel-ground.png");
                var rockMap=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Textures/sorrel-rock.png");
                // Chapter 1's own ground and strata, so the moon is the moon: its ochre dust and its red bands, toned in the vertices for the scrub, the clay and the haul road.
                s.ground=ZoneKit.Tinted(ZoneKit.Toon("sorrel-second-ground",groundMap,m=>{m.SetColor("_BaseColor",new Color(1,.97f,.94f));m.SetFloat("_RampSteps",4);m.SetFloat("_LightFloor",.32f);m.SetColor("_ShadowTint",new Color(.46f,.42f,.6f));}));
                s.rock=ZoneKit.Tinted(ZoneKit.Toon("sorrel-second-rock",rockMap,m=>{m.SetColor("_BaseColor",new Color(1,.95f,.92f));m.SetFloat("_RampSteps",4);m.SetFloat("_LightFloor",.3f);m.SetColor("_ShadowTint",new Color(.42f,.38f,.56f));}));
                s.crag=ZoneKit.Tinted(ZoneKit.Toon("sorrel-second-crag",rockMap,m=>{m.SetFloat("_RampSteps",4);m.SetFloat("_LightFloor",.32f);m.SetColor("_ShadowTint",new Color(.44f,.38f,.56f));m.SetColor("_RimColor",new Color(.7f,.55f,.5f));}));
                // The Compact's works: white plate in courses with teal seams (Stillwater's own paint), and a dark grey under it.
                var plate=ZoneKit.Texture("Sorrel/second-plate",256,(u,v)=>{float n=.84f+.08f*(ZoneKit.Cloud(u,v,8,31)-.5f)+.04f*(ZoneKit.Tile(u*64,v*64,64,32)-.5f);
                    float seam=Mathf.Abs(Mathf.Repeat(u*4,1)-.5f)<.015f||Mathf.Abs(Mathf.Repeat(v*2,1)-.5f)<.02f?-.22f:0;return new Color(n+seam,n+seam,n*1.01f+seam,1);});
                s.works=ZoneKit.Tinted(ZoneKit.Toon("sorrel-second-works",plate,m=>{m.SetColor("_BaseColor",new Color(.96f,.97f,.98f));m.SetFloat("_RampSteps",4);m.SetFloat("_LightFloor",.4f);}));
                s.worksDark=ZoneKit.Tinted(ZoneKit.Toon("sorrel-second-works-dark",plate,m=>{m.SetColor("_BaseColor",new Color(.38f,.4f,.43f));m.SetFloat("_RampSteps",3);}));
                s.cable=ZoneKit.Tinted(ZoneKit.Toon("sorrel-second-cable",plate,m=>{m.SetColor("_BaseColor",new Color(.92f,.93f,.95f));m.SetFloat("_RampSteps",3);m.SetFloat("_LightFloor",.42f);}));
                s.lamp=ZoneKit.Toon("sorrel-second-lamp",null,m=>{m.SetColor("_BaseColor",new Color(.5f,.95f,1f));ZoneKit.With(m,"_EMISSION","_Emission");m.SetColor("_EmissionColor",new Color(.9f,2.2f,2.4f));m.SetFloat("_LightFloor",1);});
                s.shaft=ZoneKit.Toon("sorrel-second-shaft",null,m=>{m.SetColor("_BaseColor",new Color(.05f,.05f,.07f));m.SetFloat("_RampSteps",2);m.SetFloat("_LightFloor",0);});
                // The falloff is in the alpha, the colour white. URP's particle shader mixes its fog toward the fog's colour even when it adds: with the falloff in the colour and an alpha of
                // one, a quad's black corners took the fog's pink and added it, so every glow in the crater was a pale pink square, opaque from far back (whole-game capture, November 20,
                // s62/150 to s63/010). Faded by its alpha, a corner adds nothing however fogged it is.
                var soft=ZoneKit.Texture("Sorrel/soft-glow",128,(u,v)=>{float d=Vector2.Distance(new Vector2(u,v),new Vector2(.5f,.5f))*2;float a=Mathf.Pow(Mathf.Clamp01(1-d),2.2f);return new Color(1,1,1,a);},false,true);
                Material Soft(string asset,Color colour){var m=kit.Glow(asset,false,colour);m.SetTexture("_BaseMap",soft);return m;}
                s.young=Soft("Young glow",new Color(.35f,1.2f,1.25f,1));s.glow=Soft("Works glow",new Color(.5f,1.3f,1.5f,1));s.warm=Soft("Window glow",new Color(1.4f,1.05f,.65f,1));
                // The young's light round a bore's mouth, a little of it (D382): the full glow laid over a mouth was a blown-out pool with no source.
                s.spill=Soft("Young spill",new Color(.13f,.42f,.44f,1));
                return s;
            }
            /// <summary>The floor's tone: Sorrel's ochre, darker clay in broad patches, olive scrub where it is sheltered (as chapter 1 dressed its margins), and the crater's floor and walls
            /// going cold and dark toward the nursery. Written linear: a vertex colour isn't converted. The haul road and what lies on the floor are <see cref="Floor"/>'s.</summary>
            static Color Tone(Vector2 p)
            {
                float clay=ZoneKit.Smooth(.55f,.7f,Noise(p,22,11)),scrub=ZoneKit.Smooth(.58f,.68f,Noise(p,11,12))*ZoneKit.Smooth(.35f,.55f,Noise(p,40,13));
                var c=Color.Lerp(new Color(1,.98f,.95f),new Color(.82f,.66f,.58f),clay*.7f);
                c=Color.Lerp(c,new Color(.5f,.56f,.28f),scrub*.85f);
                // The haul road isn't toned here: painted in the vertices of a 2.5 m grid it came out as a chain of pale squares down the valley's middle (whole-game capture, November 20,
                // s62/121). It is the ground kit's graded aggregate and ruts (Floor).
                // Deep ground goes cold and dark (the moat, the shafts); the nursery's crown under her is the pale grey of Hushwell's chambers.
                float h=Height(p),r=Vector2.Distance(p,Centre);
                c=Color.Lerp(c,new Color(.5f,.48f,.56f),ZoneKit.Smooth(-24,-80,h)*.6f);
                if(r<CraterR&&h>-14&&h<-8)c=Color.Lerp(c,new Color(.8f,.78f,.8f),.5f);
                return new Color(c.r,c.g,c.b).linear;
            }

            // ---------------------------------------------------------------- the walls
            /// <summary>Every stop a hull meets: boxes at the plane on the crest lines, each a little longer than its piece so neighbours overlap, none running past a bend into a lane. The chain
            /// line across the crater and the east notch's chain stand until <c>end.motherFreed</c> (returned, so a gate can take them away). Returns the number of boxes.</summary>
            static int Walls(out List<GameObject> chains)
            {
                var root=new GameObject("Sorrel's limits");int count=0;var held=new List<GameObject>();
                GameObject Line(Vector2 a,Vector2 b,string name,Transform under=null)
                {
                    var run=b-a;var wall=new GameObject(name,typeof(BoxCollider));wall.isStatic=true;wall.transform.SetParent(under??root.transform,true);
                    wall.transform.SetPositionAndRotation(new Vector3((a.x+b.x)*.5f,Plane,(a.y+b.y)*.5f),Quaternion.Euler(0,Mathf.Atan2(run.x,run.y)*Mathf.Rad2Deg,0));
                    wall.GetComponent<BoxCollider>().size=new Vector3(2,12,run.magnitude+.5f);count++;return wall;
                }
                Vector2 On(float bearing,float r){float a=bearing*Mathf.Deg2Rad;return Centre+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*r;}
                float Bearing(Vector2 p){var d=p-Centre;return Mathf.Repeat(Mathf.Atan2(d.x,d.y)*Mathf.Rad2Deg,360);}
                float CircleZ(float x,bool north)=>Centre.y+(north?1:-1)*Mathf.Sqrt(CraterR*CraterR-x*x);
                // The canyon and the apertures' outer sides run straight up to the crater's circle; the south end behind the arrival.
                foreach(int side in new[]{-1,1})
                {
                    string name=side<0?"Valley west":"Valley east";
                    Line(new(side*ValleyHalf,CanyonSouth),new(side*ValleyHalf,FunnelFrom),name);Line(new(side*ValleyHalf,FunnelFrom),new(side*Half,FunnelTo),name+", the funnel");
                    Line(new(side*Half,FunnelTo),new(side*Half,CircleZ(Half,false)),name+", the aperture");
                }
                Line(new(-ValleyHalf,CanyonSouth),new(ValleyHalf,CanyonSouth),"Valley south end");
                // The pillar between the apertures.
                float pillarTop=CircleZ(ApertureInner,false);
                Line(new(-ApertureInner,RimLine),new(-ApertureInner,pillarTop),"Pillar west");Line(new(ApertureInner,RimLine),new(ApertureInner,pillarTop),"Pillar east");
                Line(new(-ApertureInner,RimLine),new(ApertureInner,RimLine),"Pillar south");Line(new(-ApertureInner,pillarTop),new(ApertureInner,pillarTop),"Pillar north");
                // The crater's rim, round from the west aperture to the east one, broken at the east notch and the road's mouth.
                float eastFrom=Bearing(new Vector2(Mathf.Sqrt(CraterR*CraterR-Mathf.Pow(EastZ+EastHalf-Centre.y,2)),EastZ+EastHalf)),eastTo=Bearing(new Vector2(Mathf.Sqrt(CraterR*CraterR-Mathf.Pow(EastZ-EastHalf-Centre.y,2)),EastZ-EastHalf));
                float roadHalf=Mathf.Asin(RoadHalf/CraterR)*Mathf.Rad2Deg,westAperture=Bearing(new Vector2(-Half,CircleZ(Half,false)))-360,eastAperture=Bearing(new Vector2(Half,CircleZ(Half,false)));
                void Arc(float from,float to,string name){for(float t=from;t<to-.01f;t+=4){float u=Mathf.Min(t+4,to);Line(On(t,CraterR),On(u,CraterR),name);}}
                Arc(westAperture,-roadHalf,"Crater rim north-west");Arc(roadHalf,eastFrom,"Crater rim north-east");Arc(eastTo,eastAperture,"Crater rim south-east");
                // The lane east to the warp on.
                Line(new(Mathf.Sqrt(CraterR*CraterR-Mathf.Pow(EastZ-EastHalf-Centre.y,2))-1,EastZ-EastHalf),new(EastEnd,EastZ-EastHalf),"East lane south");
                Line(new(Mathf.Sqrt(CraterR*CraterR-Mathf.Pow(EastZ+EastHalf-Centre.y,2))-1,EastZ+EastHalf),new(EastEnd,EastZ+EastHalf),"East lane north");
                Line(new(EastEnd,EastZ-EastHalf),new(EastEnd,EastZ+EastHalf),"East lane end");
                // The young's road: each leg's sides and each turn's ends.
                float mouth=CircleZ(RoadHalf,true)-.5f,b=RoadLegs[1],c=RoadLegs[2];
                Line(new(-RoadHalf,mouth),new(-RoadHalf,RoadNorth),"Road A west");Line(new(RoadHalf,mouth),new(RoadHalf,RoadTurn1),"Road A east");
                Line(new(-RoadHalf,RoadNorth),new(b+RoadHalf,RoadNorth),"Road turn one north");Line(new(RoadHalf,RoadTurn1),new(b-RoadHalf,RoadTurn1),"Road turn one south");
                Line(new(b-RoadHalf,RoadSouth),new(b-RoadHalf,RoadTurn1),"Road B west");Line(new(b+RoadHalf,RoadTurn2),new(b+RoadHalf,RoadNorth),"Road B east");
                Line(new(b-RoadHalf,RoadSouth),new(c+RoadHalf,RoadSouth),"Road turn two south");Line(new(b+RoadHalf,RoadTurn2),new(c-RoadHalf,RoadTurn2),"Road turn two north");
                Line(new(c-RoadHalf,RoadTurn2),new(c-RoadHalf,RoadEnd),"Road C west");Line(new(c+RoadHalf,RoadSouth),new(c+RoadHalf,RoadEnd),"Road C east");
                Line(new(c-RoadHalf,RoadEnd),new(c+RoadHalf,RoadEnd),"Road's far end");
                // Stillwater's chains: across the crater on the chain line (the road's mouth is behind it), and across the east notch.
                var chainRoot=new GameObject("Stillwater's chains (stops)");chainRoot.transform.SetParent(root.transform,true);
                float chainX=Mathf.Sqrt(CraterR*CraterR-Mathf.Pow(ChainZ-Centre.y,2));
                Line(new(-chainX-1,ChainZ),new(chainX+1,ChainZ),"Chain line",chainRoot.transform);
                float notchX=Mathf.Sqrt(CraterR*CraterR-Mathf.Pow(EastHalf,2))-1;Line(new(notchX,EastZ-EastHalf-1),new(notchX,EastZ+EastHalf+1),"East notch chain",chainRoot.transform);
                held.Add(chainRoot);chains=held;return count;
            }

            // ---------------------------------------------------------------- rock
            static Color Red(float by)=>ZoneKit.Shape.Dim(L(.74f,.42f,.3f),by);
            /// <summary>Teeth of red rock along every stop, standing through the plane just outside it: the edge a player sees. None on the camera's side of the arena; on the young's road low,
            /// because its walls are marked and close.</summary>
            static void Teeth(ZoneKit kit,Stuff stuff)
            {
                var shape=new ZoneKit.Shape();var rng=new System.Random(1004);int made=0,kept=0;
                for(float x=-205;x<235;x+=3.1f)for(float z=-55;z<645;z+=3.1f)
                {
                    var p=new Vector2(x+((float)rng.NextDouble()-.5f)*2.4f,z+((float)rng.NextDouble()-.5f)*2.4f);float air=Air(p,out int part);int seed=rng.Next();float roll=(float)rng.NextDouble();
                    // A band 2 to 6 m outside the air's edge, about one place in three.
                    if(air>-2||air<-6||roll>.36f)continue;
                    if(Low(p)){kept++;continue;}
                    float r=Vector2.Distance(p,Centre);bool road=p.y>CraterR+Centre.y-6&&p.x>-RoadHalf-8&&p.x<RoadLegs[2]+RoadHalf+8;
                    float tall=road?2.6f+2.2f*(float)new System.Random(seed).NextDouble():4+7*(float)new System.Random(seed).NextDouble();
                    var foot=new Vector3(p.x,Height(p)-1.5f,p.y);float wide=road?1.4f:1.8f+1.6f*(float)new System.Random(seed+1).NextDouble();
                    var top=Red(1.25f+.2f*roll);var under=Red(.45f);
                    if(roll<.12f)HarrowgateBuilder.Crag(shape,foot+Vector3.up*wide*.6f,new Vector3(wide*1.5f,wide*1.1f,wide*1.4f),Quaternion.Euler(0,seed%360,0),seed,.24f,top,under,8,4);
                    else HarrowgateBuilder.Fang(shape,foot,(Vector3.up+new Vector3((float)new System.Random(seed+2).NextDouble()-.5f,0,(float)new System.Random(seed+3).NextDouble()-.5f)*.35f).normalized,
                        Plane-foot.y+tall-1,wide,roll<.24f?.55f:1,5+seed%3,seed,top,under,roll>.3f);
                    made++;_=r;
                }
                var built=kit.Built("Teeth of the ridges",shape,stuff.crag);built.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.On;
                Debug.Log("SORREL_TEETH made="+made+" low="+kept);
            }

            // ---------------------------------------------------------------- the ground
            /// <summary>What lies on the moon's floor, as chapter 1 dressed it: boulders and fallen ridge rock, strata breaking through, and Sorrel's cyan ore in clusters, thicker at the
            /// spines' feet and the crown's ledge, none on the haul road or the outpost's ground, nothing taller than 4 m and all of it far under the plane. Seeded, dropped not moved.</summary>
            static void Ground(ZoneKit kit)
            {
                var root=new GameObject("The moon's floor, dressed");var rng=new System.Random(812);int made=0;
                var kinds=new[]{("DressBoulders",2.4f),("DressBoulders",3.2f),("RidgeRock1",3.4f),("RidgeRock2",3.6f),("RidgeRock3",3.2f),("DressStrataOutcrop",2.6f),("CrystalClusterA",2.2f),("CrystalClusterB",2.6f),("DressStrataLedge",2.4f)};
                for(int k=0;k<1500;k++)
                {
                    var p=new Vector2(-150+(float)rng.NextDouble()*320,-20+(float)rng.NextDouble()*620);var (key,size)=kinds[rng.Next(kinds.Length)];float scale=size*(.7f+.6f*(float)rng.NextDouble()),yaw=(float)rng.NextDouble()*360,roll=(float)rng.NextDouble();
                    float h=Height(p),air=Air(p,out int part);
                    // Where it may lie: level ground under -8, off the haul road and the outpost, and in the crater only on the crown's ledge and the moat's floor.
                    if(h>-8||!ZoneKit.Landed(key)||Vector2.Distance(p,Centre)<CoilOut+3)continue;
                    if(part==0&&air>0&&(Mathf.Abs(p.x-HaulX(p.y))<5||p.y>16&&p.y<72&&Mathf.Abs(p.x)<20))continue;
                    var n=new Vector2(Height(p+Vector2.right)-Height(p-Vector2.right),Height(p+Vector2.up)-Height(p-Vector2.up));if(n.magnitude>1.4f)continue;
                    float near=part==0&&air>0?ZoneKit.Smooth(30,12,air):1;if(roll>.35f+.5f*near)continue;
                    var piece=kit.Prop(key,new Vector3(p.x,-.2f,p.y),scale,yaw,false,true);piece.transform.SetParent(root.transform,true);
                    foreach(var r in piece.GetComponentsInChildren<Renderer>())r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;made++;
                }
                Debug.Log("SORREL_GROUND made="+made);
            }
            /// <summary>The haul road's line down the valley, from the arrival to the apertures.</summary>
            static Vector2[] HaulRoad(){var line=new List<Vector2>();for(float z=CanyonSouth+4;z<=RimLine-4;z+=4)line.Add(new Vector2(HaulX(z),z));return line.ToArray();}
            /// <summary>The moon's floor as chapter 1 laid Sorrel's basin (GroundStaging's recipe), at the flight camera's distance: the haul road graded aggregate, paler and greyer than
            /// the ochre it crosses, with its two ruts; rust rock breaking through the flats in patches, pale dust drifted at the walls' feet and in broad banks, cracked hardpan and loose
            /// stone between, and the outpost's yard graded like the road. Every layer and mark half again the chapter 1 size, read from 50 to 80 m. On the floor only, under every lane and
            /// far under the plane: nothing of it stands. The canyon was plain ochre with the haul road's vertex tone in pale squares down it (whole-game capture, November 20, s62/121).</summary>
            static void Floor(ZoneKit.Land land)
            {
                // The moon's floor mesh carries the crests and the apertures' floors too, up at the plane: layers stop at y -8 and fade where the ground steepens (opened as a whole
                // landform), or the dust lay pale along every crest top.
                var kit=GroundKit.Open(SorrelZone,land.FloorFilter,29,-8,true);
                var haul=HaulRoad();float Road(Vector2 p)=>GroundKit.Distance(p,haul);
                bool Valley(Vector2 p)=>Air(p,out int part)>0&&part==0;
                // Marks only where a frame sees them: the lanes' floors and the country just past their crests. Strewn over the whole moon they were 2,700 pieces in 1,460 batches.
                float Seen(Vector2 p)=>GroundKit.Smooth(-36,-12,Air(p,out _));
                kit.Look("Sorrel",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Textures/sorrel-rock.png"));
                // The dust a shade of the ochre it lies on: chapter 1's pale dust read as white paper laid along the walls from fifty metres.
                Color aggregate=new(.86f,.74f,.68f),rust=new(.8f,.62f,.54f),dust=new(1f,.8f,.6f,.85f);
                kit.Layers(null,1.6f,new GroundKit.Layer("gravel",aggregate,4.6f),new GroundKit.Layer("scree",rust,5.4f),new GroundKit.Layer("sand",dust,9.5f));
                kit.PaintPath(0,haul,2.8f,1,.3f,.9f);
                // The outpost's yard, graded like the road that runs through it.
                kit.Paint(0,p=>{var q=new Vector2((p.x-1)/14,(p.y-48)/24);return (1-GroundKit.Smooth(.75f,1,q.magnitude+(kit.Noise(p,6,51)-.5f)*.4f))*(1-.4f*GroundKit.Smooth(.45f,.62f,kit.Noise(p,5,53)));});
                kit.PaintPatches(1,10,.4f,p=>GroundKit.Smooth(4.5f,7.5f,Road(p)));
                kit.PaintFoot(2,4.5f,.5f).PaintPatches(2,16,.2f,p=>GroundKit.Smooth(6,10,Road(p)),5);
                kit.Decals(new[]{"CracksA","CracksB"},2.4f,p=>Seen(p)*GroundKit.Smooth(4,6,Road(p))*GroundKit.Smooth(2,4,kit.Edge(p)),3.8f,7f,new Color(.78f,.64f,.6f,.9f));
                kit.Decals(new[]{"PebblesA","PebblesB"},3f,p=>Seen(p)*(.3f+.7f*(1-GroundKit.Smooth(2,7,kit.Edge(p)))),1.8f,3.4f,new Color(1,.82f,.68f));
                kit.Decals(new[]{"Flagstones","Slabs"},1.4f,p=>Seen(p)*GroundKit.Smooth(5,8,Road(p))*GroundKit.Smooth(.5f,.66f,kit.Noise(p,16,31)),2.6f,4.6f,new Color(.96f,.72f,.56f));
                kit.Decals(new[]{"SandDrift"},1.2f,p=>Seen(p)*(1-GroundKit.Smooth(3,9,kit.Edge(p))),4.5f,8f,new Color(dust.r,dust.g,dust.b,.9f),false);
                kit.Decals(new[]{"Stain"},1.4f,p=>Valley(p)?GroundKit.Smooth(.5f,.66f,kit.Noise(p,20,61)):0,3.5f,6.5f,new Color(.5f,.4f,.38f,.7f),false);
                // The haul road's ruts, worn by the outpost's ore trucks, and their tracks where the road crosses the yard; scorch round the landing pad.
                foreach(float side in new[]{-1.15f,1.15f})kit.Strip("Ruts",haul,side,1.3f,new Color(.56f,.44f,.4f,.85f));
                foreach(var (dx,dz) in new[]{(5.2f,1.4f),(-5f,-1.8f),(1.6f,5.2f),(-2f,-5.4f)})kit.Decal("Scorch",new Vector2(3+dx,30+dz),5.4f,dx*50,new Color(1,.95f,.92f,.85f),false);
                kit.Finish();
            }

            // ---------------------------------------------------------------- the valley
            /// <summary>Dace's outpost on the haul road, empty: its two habs facing in across the court, the landing pad, the parked rover, the comms mast and the stores, fitted by height a
            /// little over their chapter 1 sizes so they read from 35 m up. Lights out but one.</summary>
            static void Outpost(ZoneKit kit,Stuff stuff,List<(Vector2 at,float clear)> keep)
            {
                var root=new GameObject("Dace's outpost, empty");
                GameObject Stand(string key,float x,float z,float size,float yaw)
                {
                    if(!ZoneKit.Landed(key))return null;var p=new Vector2(x,z);
                    var piece=kit.Prop(key,new Vector3(x,0,z),size,yaw,false,true);piece.transform.SetParent(root.transform,true);return piece;
                }
                Stand("OutpostHab",-7,48,7.5f,90);Stand("OutpostHab",8,56,7.5f,-90);Stand("LandingPad",3,30,1.2f,0);Stand("DressRover",-6,34,2.6f,30);
                Stand("DressCommsMast",11,42,9,0);Stand("HushwellCrates",-3,62,2.4f,15);Stand("DeckCrate",6,36,1.6f,40);Stand("DeckCrate",7.2f,37.6f,1.3f,10);
                Stand("DressSurveyBeacon",-9,66,3,0);
                var lamp=kit.Lamp(new Vector3(8,3.5f,52),12,2.4f,new Color(1,.78f,.5f));lamp.transform.SetParent(root.transform,true);
            }
            /// <summary>The drill mast at the valley's head, where chapter 1 left it, and the bore beside it: the mouth the drill broke into Hushwell by, black, its scaffold over it and a cold
            /// glow far down.</summary>
            static void DrillSite(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject("The drill site");
                if(ZoneKit.Landed("OutpostDrill")){var drill=kit.Prop("OutpostDrill",new Vector3(Drill.x,0,Drill.z),20,30,false,true);drill.transform.SetParent(root.transform,true);}
                var hole=new ZoneKit.Shape().Disc(new Vector3(Bore.x,Height(new Vector2(Bore.x,Bore.z))+.15f,Bore.z),Vector3.up,4.2f,20,Color.white);
                var mouth=kit.Built("The bore",hole,stuff.shaft);mouth.transform.SetParent(root.transform,true);
                if(ZoneKit.Landed("HushwellScaffold")){var frame=kit.Prop("HushwellScaffold",new Vector3(Bore.x,0,Bore.z),7,0,false,true);frame.transform.SetParent(root.transform,true);}
                var deep=Lit(kit,new Vector3(Bore.x,Height(new Vector2(Bore.x,Bore.z))-.5f,Bore.z),10,2,new Color(.4f,.95f,1f));deep.transform.SetParent(root.transform,true);
            }
            /// <summary>Dace's evacuation beacons in the east branch ("Mind the east branch. I planted beacons there once."), still standing, dark.</summary>
            static void Beacons(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject("Dace's beacons in the east branch");
                foreach(var (x,z) in new[]{(31f,104f),(36f,128f),(29f,150f)})
                {
                    if(!ZoneKit.Landed("PulseBeacon"))break;var b=kit.Prop("PulseBeacon",new Vector3(x,0,z),5,(x*7)%360,false,true);b.transform.SetParent(root.transform,true);
                }
            }

            // ---------------------------------------------------------------- the crater
            /// <summary>Stillwater's clamp: the Compact's white machine seventy metres tall standing in the crater on the nursery's floor, its jaws toward the arena, its top under the plane;
            /// a chain from its jaws to each anchor clamp, and a chain across the crater on the chain line and across the east notch. Returns what stands until she goes (the taut chains)
            /// and what is drawn after (the chains slack, the clamp leaning open).</summary>
            static (List<GameObject> taut,List<GameObject> slack) TheClamp(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject("Stillwater's clamp");var taut=new List<GameObject>();var slack=new List<GameObject>();
                GameObject body=null;
                if(ZoneKit.Landed("StillwaterClamp"))
                {
                    body=kit.Loose("StillwaterClamp",Clamp,ClampHeight,Quaternion.Euler(0,180,0));var box=ZoneKit.Drawn(body);body.transform.position+=new Vector3(Clamp.x-box.center.x,Clamp.y-box.min.y,Clamp.z-box.center.z);
                    body.transform.SetParent(root.transform,true);Debug.Log("SORREL_CLAMP drawn="+ZoneKit.Drawn(body));
                }
                var jaws=new Vector3(Clamp.x,-26,Clamp.z-12);
                // Its work lamps, cold, round the jaws: what makes it a machine that is running.
                foreach(var (dx,dz) in new[]{(-14f,-6f),(14f,-6f),(0f,-16f),(-10,8f),(10,8f)})
                {
                    var l=Lit(kit,new Vector3(Clamp.x+dx,-16,Clamp.z+dz),26,3.2f,new Color(.55f,.92f,1f));l.transform.SetParent(root.transform,true);
                }
                var cables=new ZoneKit.Shape();var hanging=new ZoneKit.Shape();var white=L(.92f,.93f,.95f);var teal=L(.3f,.7f,.72f);
                void Cable(ZoneKit.Shape into,Vector3 a,Vector3 b,float sag,float radius)
                {
                    var path=new List<Vector3>();var radii=new List<float>();
                    for(int k=0;k<=16;k++){float t=k/16f;path.Add(Vector3.Lerp(a,b,t)+Vector3.down*sag*4*t*(1-t));radii.Add(radius);}
                    into.Slot(0).Tube(path,radii,8,white,ZoneKit.Shape.Dim(white,.85f),1,.25f);
                    // Bands of teal every few metres, so a chain reads as links from above.
                    float length=Vector3.Distance(a,b);for(float d=2.5f;d<length-1;d+=3.2f){float t=d/length;var at=Vector3.Lerp(a,b,t)+Vector3.down*sag*4*t*(1-t);
                        into.Slot(0).Tube(new[]{at-(b-a).normalized*.35f,at+(b-a).normalized*.35f},new[]{radius*1.25f,radius*1.25f},8,teal,teal,1,.25f);}
                }
                foreach(var s in Shelves){Cable(cables,jaws,s.anchor+Vector3.down*4.5f,1.5f,.85f);Cable(hanging,jaws+Vector3.down*6,s.anchor+Vector3.down*20,16,.85f);}
                float chainX=Mathf.Sqrt(CraterR*CraterR-Mathf.Pow(ChainZ-Centre.y,2));
                var line=new ZoneKit.Shape();Cable(line,new Vector3(-chainX-2,.2f,ChainZ),new Vector3(chainX+2,.2f,ChainZ),1.2f,.75f);
                float notchX=Mathf.Sqrt(CraterR*CraterR-EastHalf*EastHalf)-1;Cable(line,new Vector3(notchX,.2f,EastZ-EastHalf-2),new Vector3(notchX,.2f,EastZ+EastHalf+2),.8f,.6f);
                var tautChains=kit.Built("Stillwater's chains, taut",cables,stuff.cable);tautChains.transform.SetParent(root.transform,true);taut.Add(tautChains);
                var lineChains=kit.Built("Stillwater's chains across the ways on",line,stuff.cable);lineChains.transform.SetParent(root.transform,true);taut.Add(lineChains);
                var slackChains=kit.Built("Stillwater's chains, slack",hanging,stuff.cable);slackChains.transform.SetParent(root.transform,true);slack.Add(slackChains);
                // The chain line's lamps: a cold bead every few metres along it, so the stop is a thing in the frame.
                var beads=new ZoneKit.Shape();for(float x=-chainX;x<=chainX;x+=4.5f){float t=(x+chainX)/(2*chainX);beads.Box(new Vector3(x,.2f-1.2f*4*t*(1-t)+.85f,ChainZ),new Vector3(.5f,.3f,.5f),Quaternion.identity,Color.white);}
                var lit=kit.Built("The chain line's lamps",beads,stuff.lamp);lit.transform.SetParent(lineChains.transform,true);
                return (taut,slack);
            }

            /// <summary>One shelf, turned to face her: a deck of the Compact's plate, a low rail round it, the pad's ring, its floor sign; the works on its inner edge (a winch housing, solid, 3 m
            /// wide, between the seat and her) and an empty at the seat facing her for the hold (Mother's <c>StandClamp</c> stands it); the anchor clamp on her coil inside it, top at the
            /// plane, chained to Stillwater's clamp; a buttress of the crown's rock under it (the fourth: hung off its anchor clamp's arm, under one root at the crater's middle that her
            /// strain swings); its berth (wiring 6.1.1). Nothing of it inside r 21 between y -9 and the plane but the anchor clamp, and 3 m clear either side of the seat.</summary>
            static GameObject ShelfWorks(ZoneKit kit,Stuff stuff,Shelf s)
            {
                bool swings=s.index==4;var root=new GameObject("Shelf "+s.index+": "+s.name);if(swings)root.transform.position=new Vector3(Centre.x,ShelfY,Centre.y);
                var frame=new GameObject("Shelf "+s.index+" frame");frame.transform.SetParent(root.transform,true);frame.transform.SetPositionAndRotation(s.middle,s.Turn);
                float top=ShelfY,hx=Shelf.Wide/2,hz=Shelf.Deep/2;var turn=s.Turn;float yaw=s.bearing+180;
                var deck=new ZoneKit.Shape();var white=L(.9f,.91f,.93f);var grey=L(.5f,.52f,.55f);var teal=L(.25f,.62f,.64f);var amber=L(.95f,.72f,.25f);
                deck.Slot(0).Box(s.At(0,0,top-.3f),new Vector3(Shelf.Wide,.6f,Shelf.Deep),turn,white,white);
                for(float x=-hx+2;x<hx-1;x+=2.8f)deck.Slot(0).Box(s.At(x,0,top+.01f),new Vector3(.08f,.02f,Shelf.Deep-.4f),turn,grey);
                deck.Slot(0).Disc(new Vector3(s.pad.x,top+.03f,s.pad.z),Vector3.up,3.3f,28,amber,2.8f,amber);
                var corners=new[]{s.At(-hx,-hz,top),s.At(hx,-hz,top),s.At(hx,hz,top),s.At(-hx,hz,top)};
                for(int k=0;k<4;k++)
                {
                    var a=corners[k];var b=corners[(k+1)%4];deck.Slot(1).Beam(a+Vector3.up*.5f,b+Vector3.up*.5f,.16f,.16f,teal);
                    for(float t=0;t<=1.001f;t+=1/Mathf.Ceil(Vector3.Distance(a,b)/2.4f))deck.Slot(1).Box(Vector3.Lerp(a,b,t)+Vector3.up*.25f,new Vector3(.16f,.5f,.16f),Quaternion.identity,grey);
                }
                // The works: a winch housing on the inner edge, its drum toward the seat, its cable down to the anchor clamp.
                var works=s.At(0,hz-.5f,top+.625f);
                deck.Slot(0).Box(works,new Vector3(3.2f,1.25f,.9f),turn,white,L(.8f,.82f,.85f));
                deck.Slot(1).Box(works+turn*new Vector3(0,.3f,-.5f),new Vector3(2.2f,.7f,.2f),turn,teal);
                deck.Slot(1).Tube(new[]{works+turn*new Vector3(-1.2f,-.1f,-.6f),works+turn*new Vector3(1.2f,-.1f,-.6f)},new[]{.42f,.42f},10,grey,grey,1,.3f);
                var built=kit.Built("Shelf "+s.index+" deck",deck,stuff.works,stuff.worksDark);built.transform.SetParent(root.transform,true);
                ZoneKit.Block(built,s.At(0,0,top-.3f),new Vector3(Shelf.Wide,.6f,Shelf.Deep),yaw,"Shelf floor");
                for(int k=0;k<4;k++)ZoneKit.BlockAlong(built,corners[k]+Vector3.up*.25f,corners[(k+1)%4]+Vector3.up*.25f,.3f,.5f,"Shelf rail");
                ZoneKit.Block(built,works,new Vector3(3.2f,1.25f,.9f),yaw,"The works");
                // Under it: the crown's rock built out as a buttress (kept outside r 21.5), or for the fourth its anchor clamp's arm.
                var under=new ZoneKit.Shape();int seed=40+s.index*17;
                // Legs down to the crown's ledge (10 m under the deck), each standing in a footing of its rock; the fourth's arm back to its anchor clamp instead.
                foreach(var (x,z) in new[]{(-5.5f,-3.8f),(5.5f,-3.8f),(-5.5f,2.4f),(5.5f,2.4f)})
                {
                    var from=s.At(x,z,top-.6f);
                    if(swings){under.Slot(0).Beam(from,new Vector3(s.anchor.x,-3.4f,s.anchor.z)+turn*new Vector3(x*.3f,0,0),.42f,.42f,grey);continue;}
                    var to=s.At(x*.82f,z*.82f,0);to.y=Height(new Vector2(to.x,to.z));
                    under.Slot(1).Beam(from,to+Vector3.up*.6f,.5f,.5f,grey);
                    HarrowgateBuilder.Crag(under,to+Vector3.up*.4f,new Vector3(1.6f,1.1f,1.5f),Quaternion.Euler(0,seed*13,0),seed++,.22f,Red(1.1f),Red(.45f),7,4);
                }
                var rock=kit.Built("Shelf "+s.index+(swings?" arm":" buttress"),under,swings?stuff.worksDark:stuff.crag,stuff.worksDark);rock.transform.SetParent(root.transform,true);
                rock.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.On;
                // The anchor clamp on her coil, its top at the plane.
                if(ZoneKit.Landed("AnchorClamp"))
                {
                    var clamp=kit.Loose("AnchorClamp",s.anchor,6.5f,Quaternion.Euler(0,s.bearing,0));var box=ZoneKit.Drawn(clamp);clamp.transform.position+=new Vector3(0,Plane-.1f-box.max.y,0);clamp.transform.SetParent(root.transform,true);
                }
                var seat=new GameObject("Clamp seat "+s.index);seat.transform.SetParent(root.transform,true);seat.transform.SetPositionAndRotation(s.seat,turn);
                Mother.StandClamp(seat.transform,s.index);
                var lamp=Lit(kit,s.At(0,hz-1.5f,top+2.4f),9,1.6f,new Color(.6f,.92f,1f));lamp.transform.SetParent(root.transform,true);
                WorldBuilder.Label(s.sign,s.At(0,-hz+1.6f,top+.1f),1.8f).transform.SetParent(root.transform,true);
                if(ZoneKit.Landed("DeckConsole")){var launch=kit.Prop("DeckConsole",new Vector3(s.launch.x,0,s.launch.z),1.1f,yaw,false,false);launch.transform.position+=Vector3.up*(top-ZoneKit.Drawn(launch).min.y);launch.transform.SetParent(root.transform,true);}
                Berth.Declare(s.berth,s.name,ZoneKind.GroundCombat,s.Box,s.pad,s.launch,s.foot,s.flight,s.returnSpawn,s.back,inFight:true,refuseWhileAwake:true,parent:frame.transform);
                // The fourth swings on her strain round her middle (the root stands there), from its bearing out to 92 and back.
                if(swings)Mother.StandStrain(root.transform);
                return root;
            }
            /// <summary>The fight's shelves' navigation, all four in one bake from their own floors and rails, each checked from its foot spawn to its seat, its launch and its middle: the
            /// moon has no collider and the plane none a body stands on, so nothing else is read.</summary>
            static void BakeShelves(List<GameObject> roots)
            {
                Physics.SyncTransforms();var sources=new List<NavMeshBuildSource>();
                foreach(var root in roots)foreach(var box in root.GetComponentsInChildren<BoxCollider>())
                    if(box.enabled&&!box.isTrigger&&box.GetComponentInParent<Berth>()==null)
                        sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=box.transform.localToWorldMatrix*Matrix4x4.Translate(box.center),size=box.size,area=0});
                var settings=NavMesh.GetSettingsByID(0);settings.agentRadius=.55f;settings.agentHeight=2;settings.agentClimb=.3f;settings.agentSlope=35;settings.overrideVoxelSize=true;settings.voxelSize=.15f;
                var built=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(new Vector3(Centre.x,ShelfY,Centre.y),new Vector3(90,10,90)),Vector3.zero,Quaternion.identity);
                if(built==null)throw new InvalidOperationException(SorrelZone+" shelves' navigation bake failed");
                var instance=NavMesh.AddNavMeshData(built);int points=0;
                try
                {
                    foreach(var s in Shelves)
                    {
                        if(!NavMesh.SamplePosition(s.foot,out var start,1.5f,NavMesh.AllAreas))throw new InvalidOperationException(SorrelZone+": shelf "+s.index+" has no floor at its foot spawn "+s.foot);
                        foreach(var goal in new[]{s.seat,s.launch,s.middle})
                        {
                            var path=new NavMeshPath();
                            if(!NavMesh.SamplePosition(goal,out var sampled,1.5f,NavMesh.AllAreas)||!NavMesh.CalculatePath(start.position,sampled.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)
                                throw new InvalidOperationException(SorrelZone+": shelf "+s.index+" can't be walked from its foot spawn to "+goal);
                            points++;
                        }
                    }
                    Debug.Log("ZONE_ROUTE_BAKE_OK "+SorrelZone+" points="+points);
                }
                finally{instance.Remove();}
                string file="Assets/_Project/Scenes/"+SorrelZone+"-Navigation.asset";var previous=AssetDatabase.LoadAssetAtPath<NavMeshData>(file);
                if(previous==null){AssetDatabase.CreateAsset(built,file);previous=built;}else{EditorUtility.CopySerialized(built,previous);UnityEngine.Object.DestroyImmediate(built);EditorUtility.SetDirty(previous);}
                new GameObject("Walkable paths",typeof(GroundNavigation)).GetComponent<GroundNavigation>().data=previous;
            }
            /// <summary>The nursery: the bore's mouths on the crown under her (black, the young's glow coming up out of them) and in the moat's floor; the young moving in them (the Cantor's
            /// segments small, in their own cold glow, circling about her at their own pace, under one root that leaves up her road after her); hatched cradles on the moat's floor.</summary>
            static void Nursery(ZoneKit kit,Stuff stuff,out GameObject young)
            {
                var root=new GameObject("The nursery");young=new GameObject("The young in the voids");young.transform.SetParent(root.transform,true);young.transform.position=new Vector3(Centre.x,-9.5f,Centre.y);
                var voids=new ZoneKit.Shape();var rng=new System.Random(77);var places=new List<Vector3>();var mouthsWide=new List<float>();
                for(int k=0;k<11;k++)
                {
                    // Five on the crown, six in the moat.
                    bool crown=k<5;float a=k*(crown?72:60)+rng.Next(-14,14),r=crown?3+(float)rng.NextDouble()*5:33+(float)rng.NextDouble()*6;
                    var p=Centre+new Vector2(Mathf.Sin(a*Mathf.Deg2Rad),Mathf.Cos(a*Mathf.Deg2Rad))*r;
                    var at=new Vector3(p.x,Height(p)+.15f,p.y);places.Add(at);float wide=crown?1.6f+(float)rng.NextDouble():2.4f+(float)rng.NextDouble()*1.6f;mouthsWide.Add(wide);voids.Disc(at,Vector3.up,wide,18,Color.white);
                }
                var mouths=kit.Built("The drilled voids",voids,stuff.shaft);mouths.transform.SetParent(root.transform,true);mouths.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                // Round each mouth only a little of that light, on its rim and the floor a few metres out, dim enough that the mouth stays dark round its throat. It was a quad of
                // the young's full glow 9 to 15 m across laid over the mouth and lit from 3 m over it by a 30 m lamp: from the east clamp's pad a flat, blown-out cyan pool with no
                // source in it (nov22b-whole s63/164, D382). The lamps now stand just over the lip, low and short, so what they light is the rim and the floor round it.
                var spill=stuff.spill;
                for(int k=0;k<places.Count;k++)
                {
                    var at=places[k];
                    var halo=GameObject.CreatePrimitive(PrimitiveType.Quad);UnityEngine.Object.DestroyImmediate(halo.GetComponent<Collider>());halo.name="Glow from the void";halo.transform.SetParent(root.transform,true);
                    halo.transform.SetPositionAndRotation(at+Vector3.up*.3f,Quaternion.Euler(90,0,0));halo.transform.localScale=Vector3.one*mouthsWide[k]*4.4f;
                    var view=halo.GetComponent<Renderer>();view.sharedMaterial=spill;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
                    // The source: the young's own glow, small, down the throat inside the dark mouth.
                    var throat=GameObject.CreatePrimitive(PrimitiveType.Quad);UnityEngine.Object.DestroyImmediate(throat.GetComponent<Collider>());throat.name="The young's light down the bore";throat.transform.SetParent(root.transform,true);
                    throat.transform.SetPositionAndRotation(at+Vector3.up*.32f,Quaternion.Euler(90,0,0));throat.transform.localScale=Vector3.one*mouthsWide[k]*1.3f;
                    var core=throat.GetComponent<Renderer>();core.sharedMaterial=stuff.young;core.shadowCastingMode=ShadowCastingMode.Off;core.receiveShadows=false;
                    if(k==0||k%3==1){var l=Lit(kit,at+Vector3.up*1.2f,k<5?10:14,k<5?1.4f:1.8f,new Color(.4f,.95f,1f));l.transform.SetParent(root.transform,true);}
                }
                // The young: a head and two segments each, curled, circling about her at their own pace; three on the crown under her, four in the moat.
                for(int k=0;k<7;k++)
                {
                    bool crown=k<3;var pivot=new GameObject("A young road, circling",typeof(Tumble));pivot.transform.SetParent(young.transform,true);
                    pivot.transform.position=new Vector3(Centre.x,crown?-9.5f:CraterFloor+2,Centre.y);
                    var turn=pivot.GetComponent<Tumble>();turn.axis=Vector3.up;turn.rate=(k%2==0?1:-1)*(5+k*1.3f);
                    float a=k*51+13,r=crown?4.5f+k*1.6f:34+(k-3)*1.8f;
                    for(int seg=0;seg<3;seg++)
                    {
                        // The young's own grade of the Cantor's segments (tools/c17_intake.py): pale like their mother, never the Cantor's violet.
                        string id=seg==0?"YoungHead":seg==1?"YoungBody":"YoungTail";float along=a-seg*(crown?22:8)*(k%2==0?1:-1);
                        var p=Centre+new Vector2(Mathf.Sin(along*Mathf.Deg2Rad),Mathf.Cos(along*Mathf.Deg2Rad))*r;
                        var body=Drawn(id,new Vector3(p.x,pivot.transform.position.y+.6f+.25f*seg,p.y),seg==0?3.4f:2.8f,along+(k%2==0?90:-90));
                        if(body!=null)body.transform.SetParent(pivot.transform,true);
                    }
                }
                if(ZoneKit.Landed("HushwellCradleHatched"))foreach(var a in new[]{20f,150f,250f,320f})
                {
                    var p=Centre+new Vector2(Mathf.Sin(a*Mathf.Deg2Rad),Mathf.Cos(a*Mathf.Deg2Rad))*36;
                    var cradle=kit.Prop("HushwellCradleHatched",new Vector3(p.x,0,p.y),3.6f,a,false,true);cradle.transform.SetParent(root.transform,true);
                }
            }
            /// <summary>An enemy's generated body stood as scenery (no brain, no collider), fitted to a height: the young in the bore.</summary>
            static GameObject Drawn(string id,Vector3 middle,float height,float yaw)
            {
                var prefab=ZoneKit.Prefab("Enemies",id);if(prefab==null)return null;
                var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);PrefabUtility.UnpackPrefabInstance(body,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                foreach(var c in body.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
                foreach(var b in body.GetComponentsInChildren<MonoBehaviour>(true))if(b!=null)UnityEngine.Object.DestroyImmediate(b);
                foreach(var a in body.GetComponentsInChildren<Animator>(true))UnityEngine.Object.DestroyImmediate(a);
                body.transform.rotation=Quaternion.Euler(0,yaw,0);var box=ModelGeometry.BoundsOf(body);body.transform.localScale*=height/Mathf.Max(.01f,box.size.y);
                box=ModelGeometry.BoundsOf(body);body.transform.position+=middle-box.center;
                foreach(var r in body.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
                return body;
            }
            /// <summary>The three rocks her pressure wave stops at, solid at the plane, each on its own spire out of the crater, drawn to a little over the plane.</summary>
            static int CoverRocks(ZoneKit kit,Stuff stuff)
            {
                var shape=new ZoneKit.Shape();int made=0;var root=new GameObject("Cover in the crater");
                // Sorrel's own sandstone, the ridges' generated rock (the look pass of November 14: code-built crags in a dark red read as faceted lumps beside the ochre ground): a broken
                // ledge of it for each head, its top at 2.5, as wide as its solid and a little more; a column of the same under it down to the crown's ledge, each piece turned its own
                // way. Every piece's mesh is named "Cover ..." (what the camera's-side check leaves to the cover). The crags stay as the fallback where the kit isn't landed.
                string[] heads={"RidgeRock3","RidgeRock2","RidgeRock2"},columns={"RidgeRock3","RidgeRock2"};
                GameObject Rock(string key,Vector3 top,float wide,float yaw)
                {
                    var piece=kit.Prop(key,new Vector3(top.x,0,top.z),4,yaw,false,false);var b=ZoneKit.Drawn(piece);
                    piece.transform.localScale*=wide/Mathf.Max(.01f,Mathf.Max(b.size.x,b.size.z));b=ZoneKit.Drawn(piece);
                    piece.transform.position+=new Vector3(top.x-b.center.x,top.y-b.max.y,top.z-b.center.z);piece.transform.SetParent(root.transform,true);
                    foreach(var f in piece.GetComponentsInChildren<MeshFilter>(true))f.gameObject.name="Cover rock ("+key+")";
                    foreach(var r in piece.GetComponentsInChildren<Renderer>(true))r.shadowCastingMode=ShadowCastingMode.On;
                    return piece;
                }
                foreach(var at in Cover)
                {
                    int seed=300+made*11;var foot=Height(new Vector2(at.x,at.z));
                    if(ZoneKit.Landed(heads[made])&&ZoneKit.Landed(columns[0])&&ZoneKit.Landed(columns[1]))
                    {
                        var head=Rock(heads[made],new Vector3(at.x,Plane+1.5f,at.z),CoverRadius*2.15f,seed%360);float under=ZoneKit.Drawn(head).min.y+.5f;int k=0;
                        while(under>foot+.5f){var piece=Rock(columns[k%2],new Vector3(at.x,under,at.z),5.2f-.4f*(k%2),(seed+k*97)%360);under=ZoneKit.Drawn(piece).min.y+.6f;k++;if(k>8)break;}
                    }
                    else
                    {
                        for(float y=foot;y<-3;y+=5)HarrowgateBuilder.Crag(shape,new Vector3(at.x,y+2.5f,at.z),new Vector3(4.2f,3.2f,3.8f),Quaternion.Euler(0,y*13+seed,0),seed+(int)y,.2f,Red(1.1f),Red(.4f),8,5);
                        HarrowgateBuilder.Crag(shape,new Vector3(at.x,Plane-.4f,at.z),new Vector3(CoverRadius,2.1f,CoverRadius*.92f),Quaternion.Euler(0,seed,0),seed,.18f,Red(1.3f),Red(.5f),10,6);
                    }
                    var solid=new GameObject("Cover rock body",typeof(SphereCollider));solid.isStatic=true;solid.transform.SetParent(root.transform,true);solid.transform.position=at;solid.GetComponent<SphereCollider>().radius=CoverRadius;
                    made++;
                }
                if(!shape.Empty){var built=kit.Built("Cover rocks",shape,stuff.crag);built.transform.SetParent(root.transform,true);built.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.On;}
                return made;
            }
            /// <summary>The door: Mother of Doors' encounter over the four clamps and the lane between them, once her definition exists (Mother engineer). Its box is open along the whole of its
            /// south side, where the apertures come in.</summary>
            static EncounterVolume Door()
            {
                string id=Foe("MotherOfDoors",null);if(id==null)return null;
                var door=WorldBuilder.Encounter("MotherOfDoors",DoorCentre,DoorSize,id,1);
                var spawner=door.spawners[0];spawner.transform.position=MotherAt;spawner.transform.rotation=Quaternion.Euler(0,180,0);spawner.faceAsPlaced=true;spawner.radius=0;
                door.requiredFlag="end.sorrelSeen";door.checkpointSpawn="Clamps";
                var show=new GameObject("Mother of Doors on show").transform;show.SetParent(door.transform,false);show.position=ValleyHead;door.previewPoint=show;door.previewRadius=Vector3.Distance(ValleyHead,MotherAt)+2;
                return door;
            }
            /// <summary>The young's road through the drilled voids, drawn once she has gone: a line of cold lamps along both sides of every leg and turn at the plane, so a wall is a mark, and
            /// the young's glow down in the cut.</summary>
            static GameObject YoungsRoad(ZoneKit kit,Stuff stuff)
            {
                var root=new GameObject("The young's road");var beads=new ZoneKit.Shape();int count=0;
                foreach(var wall in UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
                {
                    if(!wall.name.StartsWith("Road"))continue;var t=wall.transform;float half=wall.size.z*.5f-.25f;
                    var a=t.position-t.forward*half;var b=t.position+t.forward*half;
                    for(float d=0;d<=half*2;d+=2.5f){var at=Vector3.Lerp(a,b,d/(half*2));beads.Box(new Vector3(at.x,.25f,at.z),new Vector3(.45f,.22f,.45f),Quaternion.identity,Color.white);count++;}
                }
                var lit=kit.Built("The young's road, marked",beads,stuff.lamp);lit.transform.SetParent(root.transform,true);lit.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                foreach(var x in RoadLegs)foreach(float z in new[]{440f,480f,510f})
                {
                    var halo=GameObject.CreatePrimitive(PrimitiveType.Quad);UnityEngine.Object.DestroyImmediate(halo.GetComponent<Collider>());halo.name="The young's glow in the cut";halo.transform.SetParent(root.transform,true);
                    halo.transform.SetPositionAndRotation(new Vector3(x,-56,z),Quaternion.Euler(90,0,0));halo.transform.localScale=new Vector3(9,14,1);
                    var view=halo.GetComponent<Renderer>();view.sharedMaterial=stuff.young;view.shadowCastingMode=ShadowCastingMode.Off;
                }
                Debug.Log("SORREL_ROAD lamps="+count);
                return root;
            }
            /// <summary>A point or a crossing a ship meets, at the plane: the kit stands one on the ground under it, which here is the moon, metres under the ships (the first build's clamp
            /// points stood 12 m down and its road's 58, and the shared reach check found no ship could press them).</summary>
            static T AtPlane<T>(T point)where T:Component{var at=point.transform.position;point.transform.position=new Vector3(at.x,Plane-1,at.z);return point;}
            /// <summary>A point light where it is written, not lifted onto the ground as the kit's lamps are.</summary>
            static Light Lit(ZoneKit kit,Vector3 world,float range,float intensity,Color colour){var l=kit.Lamp(world,range,intensity,colour);l.transform.position=world;return l;}
            static void Gate(string name,bool showWhenSet,GameObject[] targets,string flag)
            {
                var gate=new GameObject(name,typeof(FlagGate)).GetComponent<FlagGate>();gate.flag=flag;gate.showWhenSet=showWhenSet;gate.targets=targets;
            }
            static FirstSight Sight(string situation,Vector3 at,float range,string once,string needs)
            {
                var go=new GameObject("First sight: "+situation,typeof(FirstSight));go.transform.position=at;
                var sight=go.GetComponent<FirstSight>();sight.situation=situation;sight.range=range;sight.onceFlag=once;sight.requiredFlag=needs;return sight;
            }
        }
    }
}
