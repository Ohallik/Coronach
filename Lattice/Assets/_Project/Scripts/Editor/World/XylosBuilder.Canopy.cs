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

namespace Lattice.EditorTools
{
    /// <summary>The Walking Canopy (dungeon; rule: cut an anchor and the island lurches, and the doors change). Map brief and evidence: docs/maps/WalkingCanopy.md.</summary>
    public static partial class XylosBuilder
    {
        // ---------------------------------------------------------------- the plan
        // A root island seen from inside, on two levels: the seed vault and the anchor chamber behind it stand at nought; the root hall is the level below; the flooding gallery
        // runs down its west side from seven metres under to the second anchor's platform, and the basin lies north of the hall, lower still. North is the back of every view,
        // so the high ground is there and whatever stands between the camera and a floor stays under the line of sight to it (D119, as Hushwell's rock does).
        const float HallLevel=-2.8f,VaultLevel=0,GalleryLow=-7,GalleryHigh=-3.6f,BasinRim=-5.2f,BasinDeep=-6,PlatformTop=-4.7f,BasinFull=-5,BasinEmpty=-6.15f,FloodRest=-7.08f,FloodTop=-3.76f;
        const float BasinRadius=17,LedgeRise=1.25f;
        static readonly Vector2 BasinCentre=new(24,52);
        /// <summary>The three rooted platforms (dry while the basin is full) and the three flood gates on its rim, by bearing from its centre: east is nought, north ninety.</summary>
        static readonly float[] PlatformBearings={30,150,270},GateBearings={90,200,340};
        static Vector2 OnBasin(float bearing,float radius)=>BasinCentre+new Vector2(Mathf.Cos(bearing*Mathf.Deg2Rad),Mathf.Sin(bearing*Mathf.Deg2Rad))*radius;
        /// <summary>Where the roots are drawn across the basin's passage behind the party for the fight. The Floodcrown is 8 m long on a capsule 2 m round, so its head or tail is 2 m
        /// out past where it stops: at 31.8 a charge run aground at the south rim stood it in the seal, the roots straight through its body (s21 262 and 266, November 20). Halfway
        /// down the passage's ramp nothing of it reaches them from the basin.</summary>
        internal const float BasinSealZ=29.6f;
        /// <summary>Where each of the gallery's dry ledges begins along it (z); each is a shelf on its east side, a ramp up from the south.</summary>
        static readonly float[] LedgeZ={21,30,39,47.4f};
        static float GalleryFloor(float z)=>Mathf.Lerp(GalleryLow,GalleryHigh,Mathf.Clamp01((z-15)/35));
        static float GalleryAt(Vector2 p)
        {
            float floor=GalleryFloor(p.y),shelf=0;
            foreach(float z in LedgeZ)
            {
                float along=p.y<z-4.4f?0:p.y<z-1.2f?(p.y-(z-4.4f))/3.2f:p.y<=z+2.6f?1:ZoneKit.Smooth(z+3.2f,z+2.6f,p.y);
                shelf=Mathf.Max(shelf,along);
            }
            // The low end is a wade over knuckles of root, a hand either side of the resting water: flat, the water lay on it as one rectangle with a rim, a pool and not a flooded hall.
            float knuckles=.44f*(Mathf.PerlinNoise(p.x*.36f+7,p.y*.36f+2)-.5f)+.16f*(Mathf.PerlinNoise(p.x*1.1f+3,p.y*1.1f+9)-.5f);
            // None where the ramp from the hall comes in or the first ledge begins: those meet the gallery at one height.
            return floor+knuckles*(1-ZoneKit.Smooth(12,16,p.y))*(1-ZoneKit.Smooth(-33.6f,-31.4f,p.x))+LedgeRise*shelf*ZoneKit.Smooth(-34.2f,-33.6f,p.x);
        }
        static float BasinAt(Vector2 p)
        {
            float r=Vector2.Distance(p,BasinCentre)/BasinRadius,floor=BasinDeep+(BasinRim-BasinDeep)*r*r;
            foreach(float bearing in PlatformBearings)floor=Mathf.Max(floor,PlatformTop-.5f*Mathf.Max(0,Vector2.Distance(p,OnBasin(bearing,9.5f))-2.4f));
            return floor;
        }
        static float RoundBox(Vector2 p,float x0,float x1,float z0,float z1,float round=1.2f)
        {
            var d=new Vector2(Mathf.Abs(p.x-(x0+x1)*.5f)-(x1-x0)*.5f+round,Mathf.Abs(p.y-(z0+z1)*.5f)-(z1-z0)*.5f+round);
            return Mathf.Min(Mathf.Max(d.x,d.y),0)+new Vector2(Mathf.Max(d.x,0),Mathf.Max(d.y,0)).magnitude-round;
        }
        /// <summary>Metres outside the nearest floor (negative on one), and that floor's height here. Each room meets its neighbour at one height, so there is no step where two overlap.</summary>
        static float CanopyFloor(Vector2 p,out float level)
        {
            float best=float.PositiveInfinity,at=HallLevel;
            void Room(float clear,float height){if(clear<best){best=clear;at=height;}}
            Room(RoundBox(p,-20,24,-3,23),HallLevel);
            Room(RoundBox(p,-31,-19.5f,6,14,.6f),Mathf.Lerp(HallLevel,GalleryLow,Mathf.Clamp01((-20-p.x)/9.6f)));
            Room(RoundBox(p,-43,-29.6f,1,63),GalleryAt(p));
            Room(RoundBox(p,-30.5f,-21.5f,53,59,.6f),Mathf.Lerp(GalleryHigh,VaultLevel,Mathf.Clamp01((p.x+29.6f)/7.6f)));
            Room(RoundBox(p,-22,2,40,84),VaultLevel);
            Room(RoundBox(p,-16,-8,22.5f,40.5f,.6f),Mathf.Lerp(HallLevel,VaultLevel,Mathf.Clamp01((p.y-24.5f)/9)));
            Room(RoundBox(p,18,26,22.5f,35.6f,.6f),Mathf.Lerp(HallLevel,BasinRim,Mathf.Clamp01((p.y-24)/9)));
            Room(Vector2.Distance(p,BasinCentre)-BasinRadius,BasinAt(p));
            level=at;return best;
        }
        static readonly Vector2 CanopyAway=new(Mathf.Sin(SightlineAudit.Yaw*Mathf.Deg2Rad),Mathf.Cos(SightlineAudit.Yaw*Mathf.Deg2Rad));
        static float CanopyHeight(Vector2 p)
        {
            float clear=CanopyFloor(p,out float level);if(clear<=0)return level;
            // Root mass. With a floor beyond it along the camera's line it stays under the sightline to a hero at that floor's edge, measured from the lower of the two levels, and
            // climbs away at a pitch too steep to stand on or be drawn as floor. With none beyond, it is the island's own wall.
            float allowed=float.PositiveInfinity;
            for(float d=.5f;d<=14;d+=.5f){if(CanopyFloor(p+CanopyAway*d,out float there)<0){allowed=Mathf.Min(level,there)+.6f+.82f*d;break;}}
            float top=Mathf.Min(allowed,3.3f+1.7f*Mathf.PerlinNoise((p.x+70)*.09f,(p.y+20)*.07f));
            return level+(top-level)*Mathf.SmoothStep(0,1,Mathf.Clamp01(clear/1.2f));
        }
        /// <summary>How far over a floor anything on the camera's side of it may be drawn, <paramref name="back"/> metres back from that floor along the camera's line. The camera
        /// stands 12.1 m over the hero and 15 m back, so its line to a hero's feet rises .81 a metre. At the floor's edge it is the kerb the ground always had (six tenths: under
        /// that the drawn ground would be floor nobody can walk on); from a metre back it stays under the line, by a metre and a half at the lens's end of it; and it never comes
        /// within five metres of the lens (the 7).</summary>
        static float SeenOver(float back)=>Mathf.Min(SeenMost,Mathf.Max(SeenKerb,-.4f+.72f*back));
        const float SeenKerb=.6f,SeenMost=7;
        /// <summary>The ground as it is drawn. On the camera's side of a floor the mass that bodies meet (<see cref="CanopyHeight"/>) climbs at the camera's own pitch, six tenths
        /// over the line to a hero's feet, and columns on it stood a metre over that line. At a floor's near edge that hid a hero to the chest; where the floor is the island's
        /// lowest, the basin, the crest and the columns on it stood two metres under the lens and filled half the frame with bark: the Floodcrown's fall and everything said after
        /// it were played with nobody in sight (replay nov13b, November 13). What is drawn there is a low kerb and a slope kept under the line of sight; bodies and the bake still
        /// meet the steeper wall, which nobody sees.</summary>
        static float CanopySeen(Vector2 p)
        {
            float stood=CanopyHeight(p),clear=CanopyFloor(p,out float level);if(clear<=0)return stood;
            float seen=stood,rise=Mathf.SmoothStep(0,1,Mathf.Clamp01(clear/1.2f));
            for(float d=.5f;d<=15;d+=.5f)
            {
                if(CanopyFloor(p+CanopyAway*d,out float there)>=0)continue;
                float top=Mathf.Min(level,there)+SeenOver(d);
                seen=Mathf.Min(stood,level+(top-level)*rise);break;
            }
            // And the lens, wherever it hangs: never under the kerb of the floor this stands beside.
            float lens=LensCap(p);
            return float.IsPositiveInfinity(lens)?seen:Mathf.Min(seen,Mathf.Max(lens,level+SeenKerb*rise));
        }
        /// <summary>The lens itself. With a hero on a floor the camera hangs 12.1 m over that floor and 15 m back along its line, and whatever stands within six metres of that
        /// place on the plan is in front of the glass, on the camera's line or beside it. Nothing there is drawn within five metres of the lens: no higher than that floor and
        /// <see cref="SeenMost"/>. The basin's west wall is on nobody's line to a floor, so the line's rule let it stand: ten metres of it, two from the lens of anyone on the
        /// basin's west rim. Unbounded where no camera ever hangs.</summary>
        static float LensCap(Vector2 p)
        {
            float cap=float.PositiveInfinity;var under=p+CanopyAway*LensBack;
            for(float x=-LensNear;x<=LensNear;x+=1.5f)for(float z=-LensNear;z<=LensNear;z+=1.5f)
            {
                if(x*x+z*z>LensNear*LensNear)continue;
                if(CanopyFloor(under+new Vector2(x,z),out float level)<0)cap=Mathf.Min(cap,level+SeenMost);
            }
            return cap;
        }
        const float LensBack=15,LensNear=6;
        /// <summary>How high something standing at a plan point may reach before it stands between the camera and a hero on a floor beyond it along the camera's line (D119):
        /// unbounded with no floor there. It is the drawn ground's own rule (<see cref="SeenOver"/>) and no higher: a metre over the line to the feet, as it was, let a row of
        /// columns stand in front of the basin.</summary>
        static float Sightline(Vector2 p,float reach=15)
        {
            for(float d=.5f;d<=reach;d+=.5f)if(CanopyFloor(p+CanopyAway*d,out float there)<0)return there+SeenOver(d)+.3f;
            return float.PositiveInfinity;
        }
        /// <summary>Each place its own ground: brown woven root in the hall, wet green in the gallery, paler and drier in the vault, silt in the basin. Kept darker than what stands
        /// on it: a floor brighter than the party is a board, not ground.</summary>
        static Color CanopyTone(Vector2 p)
        {
            CanopyFloor(p,out float level);float wear=.9f+.2f*Mathf.PerlinNoise(p.x*.17f+3,p.y*.17f+9);
            Color hall=new(.62f,.52f,.38f),gallery=new(.38f,.52f,.43f),vault=new(.72f,.65f,.46f),basin=new(.42f,.5f,.44f);
            var tone=Vector2.Distance(p,BasinCentre)<BasinRadius+2||p.x>17&&p.y>24&&p.y<37?Color.Lerp(hall,basin,ZoneKit.Smooth(24,31,p.y)):p.x<-19.5f&&p.y<64?Color.Lerp(hall,gallery,ZoneKit.Smooth(-19.5f,-27,p.x)):
                p.y>22.5f?Color.Lerp(hall,vault,ZoneKit.Smooth(25,35,p.y)):hall;
            // The gallery's low end and the basin's middle are where water stands longest.
            if(p.x<-29)tone=Color.Lerp(tone,new Color(.27f,.4f,.36f),ZoneKit.Smooth(GalleryLow+1.4f,GalleryLow,level));
            return new Color(tone.r*wear,tone.g*wear,tone.b*wear,1);
        }
        [ZoneLook.Source] static void CanopyLooks()
        {
            // Inside the island, and never murky: daylight comes down through the roots as a weak green-gold key that still casts, the fill is the green of leaves from every side,
            // and the seed lanterns are the warm thing in each room. Lifted a little overall, since most of every frame is floor in shade.
            ZoneLook.Register("WalkingCanopy",new ZoneLook.Spec{sun=new Color(.9f,.96f,.7f),sunIntensity=.62f,sunPitch=62,shadowStrength=.5f,
                sky=new Color(.3f,.42f,.34f),equator=new Color(.36f,.42f,.33f),ground=new Color(.2f,.23f,.17f),
                exposure=.3f,contrast=10,saturation=6,splitShadows=new Color(.43f,.51f,.52f),splitHighlights=new Color(.57f,.54f,.44f),
                bloomThreshold=.85f,bloomIntensity=.8f,vignette=.26f});
        }

        // ---------------------------------------------------------------- what the island is made of
        internal static Material CanopyGround,CanopyRoot,CanopyWall,CanopyWater;
        static void CanopyMaterials()
        {
            var weave=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Xylos/xylos-weave.png");var bark=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Xylos/xylos-bark.png");
            // The floor is root woven flat: one repeat of the weave every eight metres, so each strand is as thick as a leg. Faint (.46): at .85 the weave was a grid of large
            // square tiles under every room.
            CanopyGround=ZoneKit.Toon("xylos-canopy-floor",weave,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_BaseColor",new Color(1,.95f,.86f));m.SetFloat("_BaseMapStrength",.46f);m.SetColor("_ShadowTint",new Color(.5f,.58f,.56f));m.SetFloat("_LightFloor",.2f);});
            CanopyRoot=ZoneKit.Toon("xylos-canopy-root",bark,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.66f,.57f,.43f));m.SetColor("_ShadowTint",new Color(.5f,.58f,.56f));m.SetFloat("_LightFloor",.3f);});
            // The island's walls are the same bark at a finer grain: at the landform's own mapping one ridge was a metre across, and a hall's walls read as broad smooth bands of
            // brown (look_density counted them as the plainest thing in five frames).
            CanopyWall=ZoneKit.Toon("xylos-canopy-wall",bark,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.7f,.6f,.45f));m.SetColor("_ShadowTint",new Color(.5f,.58f,.56f));m.SetFloat("_LightFloor",.3f);m.mainTextureScale=new Vector2(2.6f,2.2f);});
            // Still water under a roof: a fine grain and hardly a glint (broad glints lay on it like slabs).
            // The glint all but gone under a roof: thresholded noise at any grain lies on dark water as pale blots, and at .05 the basin was grey camouflage under the whole fight.
            // Jade lit from above, not near black: the Floodcrown is a deep blue body, and on a deep of (.05, .22, .22) it all but vanished against its own basin (s21 262 and 266,
            // November 20). The water is the brightest broad thing in the basin now, a step under the pads, and the boss stands out on it as a dark shape.
            CanopyWater=WaterKit.Look("xylos-canopy-water",new Color(.3f,.46f,.36f,.5f),new Color(.17f,.37f,.31f,.92f),new Color(.8f,.94f,.86f,1),.6f,3.2f,.018f);
            // A wet edge, not a rim: the house foam was a pale mint band round every puddle and round the basin, the brightest thing in a dark frame, and the shallows were mint too.
            // Here the edge is thin, darker than the water and broken up, with no lap line, and shallow water is dark.
            CanopyWater.SetVector("_FoamParams",new Vector4(.035f,.9f,.03f,0));CanopyWater.SetColor("_FoamColor",new Color(.15f,.21f,.17f,1));EditorUtility.SetDirty(CanopyWater);
        }
        /// <summary>A root door: thick roots drawn across a way through, solid and cut out of the paths while it stands. A door set is these, switched by FlagGates on an anchor's flag.
        /// It is a Membrane as well, shut: a seal like a boss's, so whatever opens every seal to read the island's baked floor opens the doors too.</summary>
        static GameObject RootDoor(ZoneKit kit,string name,Vector3 from,Vector3 to,float tall,int seed)
        {
            var rng=new System.Random(seed);var shape=new ZoneKit.Shape();var run=to-from;float length=run.magnitude;var along=run/length;var across=Vector3.Cross(Vector3.up,along);
            int roots=Mathf.Max(4,Mathf.RoundToInt(length/1.1f));
            for(int i=0;i<roots;i++)
            {
                float t=(i+.5f)/roots,lean=((float)rng.NextDouble()-.5f)*1.6f,thick=.2f+.14f*(float)rng.NextDouble();var foot=from+along*(t*length)+Vector3.down*.3f;
                shape.Slot(0).Tube(new[]{foot,foot+Vector3.up*tall*.5f+along*lean*.5f+across*((float)rng.NextDouble()-.5f)*.5f,foot+Vector3.up*(tall+.4f)+along*lean},new[]{thick*1.3f,thick,thick*.8f},6,BarkDark,BarkPale,1,.3f);
            }
            // Two roots wound across the uprights, so it reads as tied shut and not as a row of posts.
            foreach(float high in new[]{.34f,.72f})
                shape.Slot(0).Tube(new[]{from+Vector3.up*tall*high,from+along*length*.5f+Vector3.up*tall*(high+.08f)+across*.3f,to+Vector3.up*tall*(high-.05f)},new[]{.16f,.2f,.15f},6,BarkDark,BarkPale,1,.3f);
            var door=kit.Built(name,shape,CanopyRoot);door.AddComponent<Membrane>();
            var solid=new GameObject("Root door wall",typeof(BoxCollider),typeof(NavMeshObstacle));solid.transform.SetParent(door.transform,true);
            solid.transform.SetPositionAndRotation((from+to)*.5f+Vector3.up*tall*.5f,Quaternion.LookRotation(across));
            var size=new Vector3(length+1,tall+.6f,1.3f);solid.GetComponent<BoxCollider>().size=size;
            var carve=solid.GetComponent<NavMeshObstacle>();carve.shape=NavMeshObstacleShape.Box;carve.size=size;carve.carving=true;carve.carveOnlyStationary=false;
            return door;
        }
        /// <summary>A seed lantern on a root post, and its light. The pool it lays is the ground kit's.</summary>
        static void SeedLamp(ZoneKit kit,ZoneKit.Shape shape,float x,float z,float tall=2.3f,float range=11,float intensity=4.2f)
        {
            var foot=kit.On(x,z);
            shape.Slot(0).Tube(new[]{foot+Vector3.down*.2f,foot+Vector3.up*tall*.6f+Vector3.right*.12f,foot+Vector3.up*tall},new[]{.13f,.1f,.07f},6,BarkDark,BarkPale,1,.3f);
            shape.Slot(1).Lump(foot+Vector3.up*(tall+.24f),new Vector3(.24f,.32f,.24f),(int)(x*7+z),.08f,Color.white,Color.white,8,6);
            kit.Lamp(new Vector3(x,tall+.3f,z),range,intensity,new Color(1,.8f,.48f));
        }
        /// <summary>An anchor root: taut from the floor up out of sight, with a pale knot at the height a blade reaches. Cut, it is a stump and a frayed end drawn up. The point a hero
        /// cuts it at is a discovery; the anchor is what answers the flag (the lurch), and the doors it names are where dust comes off.</summary>
        static ShiftAnchor AnchorRoot(ZoneKit kit,int number,Vector2 at,Vector2 standAt,string bark,params GameObject[] doors)
        {
            var foot=kit.On(at.x,at.y);string flag="xylos.anchor"+number;
            var taut=new ZoneKit.Shape();
            taut.Slot(0).Tube(new[]{foot+Vector3.down*.4f,foot+Vector3.up*1.1f+Vector3.right*.1f,foot+Vector3.up*3.4f,foot+Vector3.up*7.5f+Vector3.forward*.8f},new[]{.52f,.34f,.3f,.26f},8,BarkDark,BarkPale,1,.25f);
            foreach(int side in new[]{-1,1})taut.Slot(0).Tube(new[]{foot+new Vector3(side*1.3f,-.3f,.5f),foot+new Vector3(side*.6f,.5f,.2f),foot+Vector3.up*1.2f},new[]{.14f,.2f,.2f},6,BarkDark,BarkPale,1,.3f);
            taut.Slot(1).Lump(foot+Vector3.up*1.25f,new Vector3(.46f,.3f,.46f),number*3,.2f,Color.white,Color.white,9,6);
            var whole=kit.Built("Anchor root "+number+" taut",taut.Slot(1),CanopyRoot,Lantern);
            var cut=new ZoneKit.Shape();
            cut.Slot(0).Tube(new[]{foot+Vector3.down*.4f,foot+Vector3.up*.55f+Vector3.right*.06f,foot+Vector3.up*1.05f},new[]{.52f,.38f,.34f},8,BarkDark,BarkPale,1,.25f);
            cut.Slot(0).Disc(foot+Vector3.up*1.06f,Vector3.up,.34f,10,new Color(1.2f,1.1f,.8f));
            cut.Slot(0).Tube(new[]{foot+Vector3.up*4.3f+Vector3.right*.5f,foot+Vector3.up*5.6f+Vector3.right*.2f,foot+Vector3.up*7.5f+Vector3.forward*.8f},new[]{.2f,.28f,.26f},8,BarkPale,BarkDark,1,.25f);
            for(int k=0;k<5;k++)cut.Slot(0).Tube(new[]{foot+Vector3.up*4.35f+Vector3.right*.5f,foot+Vector3.up*(3.5f+.12f*k)+new Vector3(.5f+Mathf.Cos(k*1.3f)*.4f,0,Mathf.Sin(k*1.3f)*.4f)},new[]{.07f,.02f},4,BarkPale,BarkPale,1,1,false);
            var severed=kit.Built("Anchor root "+number+" cut",cut,CanopyRoot);
            var root=new GameObject("Anchor root "+number);root.transform.position=foot;whole.transform.SetParent(root.transform,true);severed.transform.SetParent(root.transform,true);
            ZoneKit.Block(root,foot+Vector3.up*1.2f,new Vector3(1,2.4f,1),0,"Anchor root foot");
            var work=kit.Discovery("Anchor root "+number+" cut point",new Vector3(standAt.x,0,standAt.y),"Cut the anchor root",flag,"AnchorRoot",null,"Taren",null,0,3.2f);work.situation=bark??"";
            var anchor=root.AddComponent<ShiftAnchor>();anchor.flag=flag;anchor.whole=whole;anchor.severed=severed;anchor.point=work;anchor.answers=doors.Select(d=>d.transform).ToArray();severed.SetActive(false);
            kit.Lamp(new Vector3(at.x,1.9f,at.y-.6f),7,2.6f,new Color(1,.86f,.56f));
            return anchor;
        }
        /// <summary>One of the basin's flood gates: a frame of root with a panel of woven reed across the rim's notch and a seed lantern on its head; wrecked, the panel is out and the
        /// frame hangs. A FloodGate with integrity and a hurtbox: the Floodcrown's moves decide when it can be hit.</summary>
        static FloodGate BasinGate(ZoneKit kit,string name,float bearing,int index)
        {
            var at=OnBasin(bearing,BasinRadius-.6f);var foot=kit.On(at.x,at.y);var outward=new Vector3(Mathf.Cos(bearing*Mathf.Deg2Rad),0,Mathf.Sin(bearing*Mathf.Deg2Rad));var across=Vector3.Cross(Vector3.up,outward);
            var gate=new GameObject("Flood gate "+name);gate.transform.position=foot;
            var standing=new ZoneKit.Shape();
            foreach(int side in new[]{-1,1})standing.Slot(0).Tube(new[]{foot+across*side*1.9f+Vector3.down*.3f,foot+across*side*1.8f+Vector3.up*2,foot+across*side*1.2f+Vector3.up*3.5f},new[]{.34f,.28f,.22f},7,BarkDark,BarkPale,1,.3f);
            standing.Slot(0).Tube(new[]{foot+across*-1.3f+Vector3.up*3.45f,foot+Vector3.up*3.8f,foot+across*1.3f+Vector3.up*3.45f},new[]{.22f,.26f,.22f},7,BarkDark,BarkPale,1,.3f);
            Vector3 a=foot+across*-1.7f+outward*.15f,b=foot+across*1.7f+outward*.15f;
            // Both faces: one-sided, the west gate's panel faced away from the fixed camera and the gate read as an empty frame.
            standing.Slot(1).Quad(a,b,b+Vector3.up*3,a+Vector3.up*3,-outward,Color.white,.4f).Quad(a,b,b+Vector3.up*3,a+Vector3.up*3,outward,new Color(.82f,.82f,.82f),.4f);
            standing.Slot(2).Lump(foot+Vector3.up*4.2f,new Vector3(.34f,.44f,.34f),(int)bearing,.08f,Color.white,Color.white,8,6);
            var whole=kit.Built("Flood gate "+name+" standing",standing.Slot(2),CanopyRoot,Weave,Lantern);whole.transform.SetParent(gate.transform,true);
            var broken=new ZoneKit.Shape();
            foreach(int side in new[]{-1,1})broken.Slot(0).Tube(new[]{foot+across*side*1.9f+Vector3.down*.3f,foot+across*side*(2.1f+.3f*side)+Vector3.up*1.6f,foot+across*side*2.6f+Vector3.up*2.5f+outward*.6f},new[]{.34f,.26f,.16f},7,BarkDark,BarkPale,1,.3f);
            broken.Slot(1).Quad(a+outward*1.2f,a+across*1.5f+outward*1.4f,a+across*1.4f+outward*2.6f+Vector3.up*.3f,a+outward*2.4f+Vector3.up*.2f,Vector3.up,new Color(.8f,.8f,.8f),.4f);
            var wreck=kit.Built("Flood gate "+name+" wrecked",broken.Slot(1),CanopyRoot,Weave);wreck.transform.SetParent(gate.transform,true);wreck.SetActive(false);
            var health=gate.AddComponent<Health>();health.id="FloodGate";health.maximum=health.integrity=2400;health.breakThreshold=100000;health.weakness=DamageType.Kinetic;health.resistance=DamageType.Pulse;
            var body=gate.AddComponent<SphereCollider>();body.center=Vector3.up*1.6f-gate.transform.InverseTransformDirection(outward)*.2f;body.radius=1.7f;gate.AddComponent<Hurtbox>().owner=health;
            var flood=gate.AddComponent<FloodGate>();flood.whole=whole;flood.wrecked=wreck;flood.defeatedFlag="xylos.gate."+name;flood.settledBy="Floodcrown";flood.index=index;
            return flood;
        }

        // ---------------------------------------------------------------- the zone
        static void Canopy()
        {
            RiverMaterials();CanopyMaterials();   // the river's are the region's floating leaf too (PadLeaf, PadFlower)
            var kit=ZoneKit.Ground("WalkingCanopy",CanopyHeight,.5f);bool greybox=DevArgs.Has("-xylos-greybox");
            var bed=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Ambience/AMB_RootHall.ogg");
            if(bed!=null){var def=ZoneKit.Find<ZoneDef>("WalkingCanopy");def.ambientAudio=bed;def.ambientVolume=.22f;EditorUtility.SetDirty(def);}
            var scene=WorldBuilder.Begin(kit.zone);
            kit.Terrain("Root island",-58,50,-16,94,.5f,CanopyGround,CanopyWall,CanopyTone,new Color(.36f,.4f,.34f),new Color(.86f,.84f,.72f),CanopySeen,p=>-CanopyFloor(p,out _));

            // ---- ways in and out
            WorldBuilder.Spawn("Arrival",kit.On(2,2));WorldBuilder.Spawn("Basin",kit.On(19.5f,18.5f));
            // The way out stands against the south wall east of the arrival: on the camera's side and to its left it would stand across whoever came in.
            kit.Way(KitArch(kit,"Mooring roots",new Vector3(11.5f,0,-1.4f),4.6f,0),"Mooring roots — The Braided River","BraidedRiver","Canopy",null,null,4.4f,new Vector2(11.5f,1.4f));
            kit.Sign("ROOT HALL",2,5.6f,1.8f);
            kit.Crossing("Look round the root hall",new Vector3(2,0,7.6f),new Vector3(44,5,3),"xylos.canopyEntered","SelaCanopyMooring","Sela");

            // ---- the door sets. Each door is a wall of root with its own hole in the paths; a FlagGate on an anchor's flag takes it away or draws it across. The stair's foot is the
            // one that shuts: open until the first anchor, shut until the second, so it hangs under a holder the second anchor removes.
            var galleryDoor=RootDoor(kit,"Gallery door",kit.On(-20.2f,6.2f),kit.On(-20.2f,13.8f),3.4f,11);
            var stairDoor=RootDoor(kit,"Stair foot door",kit.On(-15.8f,23.6f),kit.On(-8.2f,23.6f),3.4f,12);
            var stairHolder=new GameObject("Stair foot (shut between the first anchor and the second)");stairDoor.transform.SetParent(stairHolder.transform,true);
            var vaultFront=RootDoor(kit,"Vault door",kit.On(-15.8f,39.6f),kit.On(-8.2f,39.6f),3,13);
            var vaultBack=RootDoor(kit,"Vault back door",kit.On(-22.2f,53.2f),kit.On(-22.2f,58.8f),3,14);
            var basinDoor=RootDoor(kit,"Basin door",kit.On(18.2f,23.6f),kit.On(25.8f,23.6f),3.6f,15);
            // Each doorway in the hall's back and west walls stands under an arch of root, so it reads as a way through whether its door is drawn or not.
            KitArch(kit,"Stair arch",new Vector3(-12,0,24.2f),5.4f,0);KitArch(kit,"Basin arch",new Vector3(22,0,24.2f),5.4f,0);KitArch(kit,"Gallery arch",new Vector3(-20.8f,0,10),5.4f,90);
            var gates=new GameObject("Door sets");
            void Gate(string flag,bool shownWhenSet,GameObject target){var gate=gates.AddComponent<FlagGate>();gate.flag=flag;gate.showWhenSet=shownWhenSet;gate.targets=new[]{target};}
            Gate("xylos.anchor1",false,galleryDoor);Gate("xylos.anchor1",true,stairDoor);Gate("xylos.anchor2",false,stairHolder);
            Gate("xylos.anchor2",false,vaultFront);Gate("xylos.anchor2",false,vaultBack);Gate("xylos.anchor3",false,basinDoor);
            stairDoor.SetActive(false);
            // The three anchors: A in the hall, in plain sight of the way in; B on its platform at the gallery's head; C in the chamber behind the vault.
            AnchorRoot(kit,1,new Vector2(9,15.5f),new Vector2(8.6f,13),null,galleryDoor,stairDoor);
            AnchorRoot(kit,2,new Vector2(-37,60),new Vector2(-36.4f,57.6f),"anchor",vaultBack,vaultFront);
            AnchorRoot(kit,3,new Vector2(-11,79),new Vector2(-10.6f,76.4f),"anchor",basinDoor);
            // What the party makes of the first lurch, once it has settled and nothing is on them.
            var settled=new GameObject("The first lurch settles",typeof(AfterTalk)).GetComponent<AfterTalk>();settled.flag="xylos.anchor1";settled.node="SelaCanopyAnchor";settled.speaker="Taren";settled.heardFlag="xylos.anchorHeard";settled.delay=2.4f;

            // ---- the flooding gallery. Dry until someone walks into it with the first anchor cut; then the water climbs from its low end behind the party until the second is.
            var gallerySheet=WaterKit.Sheet(kit,"Gallery water",-56,-18,-10,66,.4f,FloodRest,CanopyWater,p=>p.x<-19.4f,FloodTop,"Canopy");
            var galleryWater=WaterKit.Volume(gallerySheet,FloodRest,(new Vector2(-37,28),new Vector2(38,78),0));
            // At rest the low end holds standing water in the dips between its knuckles of root: seen, splashed through, and not yet a wade. (A film over the whole of it, with
            // the foam's lap line lying across it everywhere, was one pale slab.) The sheets are drawn fine, so a waterline that moves across a bowl or up a hall is a curve.
            galleryWater.wadeFrom=.22f;
            var flood=new GameObject("Gallery flood",typeof(BoxCollider),typeof(RisingWater));flood.transform.position=new Vector3(-36.5f,GalleryLow+1.5f,14);
            var starts=flood.GetComponent<BoxCollider>();starts.isTrigger=true;starts.size=new Vector3(14,4,6);
            var rising=flood.GetComponent<RisingWater>();rising.water=galleryWater;rising.startFlag="xylos.anchor1";rising.stopFlag="xylos.anchor2";rising.restLevel=FloodRest;rising.topLevel=FloodTop;rising.rate=.14f;
            rising.caughtDepth=.75f;rising.damageShare=.12f;rising.fallBack=1.3f;rising.grace=2.5f;rising.drainSeconds=3.5f;rising.width=9;rising.lead=3.5f;rising.profileStep=.5f;
            var low=new GameObject("Gallery low end").transform;low.SetParent(flood.transform,true);low.position=new Vector3(-38.5f,GalleryLow,2);
            var high=new GameObject("Gallery head").transform;high.SetParent(flood.transform,true);high.position=new Vector3(-38.5f,GalleryHigh,51);
            rising.low=low;rising.high=high;rising.profile=Enumerable.Range(0,99).Select(i=>GalleryFloor(2+i*.5f)).ToArray();
            var ledges=new List<Transform>();
            foreach(float z in LedgeZ)
            {
                var ledge=new GameObject("Dry ledge "+(ledges.Count+1)).transform;ledge.SetParent(flood.transform,true);
                ledge.SetPositionAndRotation(kit.On(-31.9f,z+.2f),Quaternion.Euler(0,-90,0));ledges.Add(ledge);
            }
            rising.ledges=ledges.ToArray();
            kit.Sign("FLOODING GALLERY",-36.5f,5.4f,1.8f);
            // Duckweed on the gallery's resting water: it belongs to the water and rides up the hall with it.
            if(!greybox)Rafts(kit,"Gallery duckweed",gallerySheet.transform,FloodRest,-42.5f,-31,1.5f,15.6f,80,88,.12f,.34f,p=>CanopyFloor(p,out float under)<-.3f?FloodRest-under:-1,.05f);

            // ---- the seed vault: the breather. A chest, the racks, and the slot in its east wall that looks down into the basin.
            kit.Sign("ROOT LADDER",-12,27.4f,1.6f);kit.Sign("SEED VAULT",-10,43.4f,1.8f);
            Stash(kit,"xylos.canopy.vault","Seed vault chest",new Vector3(-17.5f,0,57.5f),40,160,ProgressionBuilder.AnyPart(3,1),ProgressionBuilder.Drop("AmberSap",3,1),ProgressionBuilder.Drop("RepairGel",2,1));
            Stash(kit,"xylos.canopy.porch","Keepers' basket",new Vector3(-9.6f,0,37.4f),-20,80,ProgressionBuilder.Drop("Rootfibre",3,1),ProgressionBuilder.Drop("ChargeCell",1,1));
            Stash(kit,"xylos.canopy.gallery","Drowned basket",new Vector3(-41.4f,0,3.6f),30,110,ProgressionBuilder.AnyPart(3,GearSlot.Frame,1));
            var slot=kit.Discovery("Vault window slot",new Vector3(.2f,0,52),"Look down through the slot","xylos.vaultSeen","",  "SelaSeedVault","Sela",null,0,3);

            // ---- the basin. Its door is in the hall's north wall; the passage down is the checkpoint, and the talk in it is where a lost fight starts again.
            var cradle=kit.Prop("DeckConsole",new Vector3(13.2f,0,20.4f),1.4f,180).AddComponent<RepairBay>();cradle.free=true;cradle.saveSpawn="Basin";cradle.prompt="Root cradle — repair and save";
            ChapterOneStaging.CradleBay("Root cradle",cradle.gameObject,kit.On(13.2f,20.4f),kit.Y(13.2f,20.4f)+.02f,false,true);
            kit.Sign("THE BASIN",22,27.2f,1.8f);
            var confront=kit.Crossing("Look at what holds the basin",new Vector3(22,0,BasinSealZ-2.5f),new Vector3(9,5,3),"xylos.basinSeen","SelaFloodcrownBasin","Taren");confront.saveSpawn="Basin";
            // 8.8 wide: the width Hushwell's seals are, so the one material the two share is left exactly as it was.
            var seal=WorldBuilder.Membrane("Basin seal",kit.On(22,BasinSealZ,1.7f),8.8f,true,3.6f);
            foreach(var filter in seal.GetComponentsInChildren<MeshFilter>())UnityEngine.Object.DestroyImmediate(filter);
            foreach(var view in seal.GetComponentsInChildren<Renderer>())UnityEngine.Object.DestroyImmediate(view);
            var sealBox=seal.GetComponent<BoxCollider>();var sealCarve=seal.gameObject.AddComponent<NavMeshObstacle>();sealCarve.shape=NavMeshObstacleShape.Box;sealCarve.center=sealBox.center;sealCarve.size=sealBox.size;sealCarve.carving=true;sealCarve.carveOnlyStationary=false;sealCarve.enabled=false;
            // The seal is roots too: drawn across behind the party as the fight starts, gone when it ends.
            var sealShape=new ZoneKit.Shape();var sealRng=new System.Random(77);
            for(int i=0;i<8;i++){var foot=kit.On(18.2f+i*1.08f,BasinSealZ)+Vector3.down*.3f;float lean=((float)sealRng.NextDouble()-.5f)*1.4f;sealShape.Slot(0).Tube(new[]{foot,foot+Vector3.up*1.8f+Vector3.right*lean*.5f,foot+Vector3.up*3.8f+Vector3.right*lean},new[]{.26f,.2f,.15f},6,BarkDark,BarkPale,1,.3f);}
            var sealRoots=kit.Built("Basin seal roots",sealShape,CanopyRoot);sealRoots.transform.SetParent(seal.transform,true);sealRoots.GetComponent<Renderer>().enabled=false;
            var duel=WorldBuilder.Encounter("Floodcrown",new Vector3(BasinCentre.x,BasinRim+1.5f,BasinCentre.y),new Vector3(36,8,34),"Floodcrown",1,new[]{seal});
            var wades=duel.spawners[0];wades.transform.SetPositionAndRotation(kit.On(14.5f,53),Quaternion.Euler(0,100,0));wades.faceAsPlaced=true;
            duel.requiredFlag="xylos.basinSeen";duel.checkpointSpawn="Basin";
            var window=new GameObject("Floodcrown preview (vault window)");window.transform.position=kit.On(.2f,52);duel.previewPoint=window.transform;duel.previewRadius=5;
            confront.previewEncounter=duel;slot.previewEncounter=duel;
            // The water: full while three gates stand, down a third with each that breaks, and whatever is above it is dry floor from then on.
            var basinSheet=WaterKit.Sheet(kit,"Basin water",5,43,33,71,.35f,BasinFull,CanopyWater,p=>Vector2.Distance(p,BasinCentre)<BasinRadius+.8f,float.NaN,"Canopy");
            var basinWater=WaterKit.Volume(basinSheet,BasinFull,(BasinCentre,new Vector2(36,36),0));
            var basin=new GameObject("Basin flood",typeof(FloodBasin)).GetComponent<FloodBasin>();basin.water=basinWater;basin.fullLevel=BasinFull;basin.emptyLevel=BasinEmpty;basin.seconds=2.6f;
            BasinGate(kit,"north",GateBearings[0],0);BasinGate(kit,"west",GateBearings[1],1);BasinGate(kit,"east",GateBearings[2],2);
            // For stills only (never the zone): the Floodcrown where it wakes, and the water at a share of full (-xylos-basin 0.67 is one gate down).
            if(DevArgs.Has("-xylos-stills"))StillBoss("Floodcrown",wades.transform.position,100);
            if(float.TryParse(DevArgs.Value("-xylos-basin"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float share))StillLevel(basinWater,Mathf.Lerp(BasinEmpty,BasinFull,share));
            // Pads on the basin, thick where it is deepest: they go down with the water a third at a time and are left on the bed.
            if(!greybox)Rafts(kit,"Basin pads",basinSheet.transform,BasinFull,8,40,36,68,150,89,.18f,.46f,p=>Vector2.Distance(p,BasinCentre)<BasinRadius-.6f?BasinFull-BasinAt(p):-1,.14f);
            // Daylight down through the roots onto the middle of the basin: what the fight is lit by, and what the boss's dark back is seen against.
            kit.Lamp(new Vector3(BasinCentre.x-2,BasinRim+11,BasinCentre.y-3),24,1.7f,new Color(.92f,1,.78f)).name="Daylight over the basin";
            var drainWay=new GameObject("The Floodcrown's way out",typeof(BossExit)).GetComponent<BossExit>();drainWay.transform.position=kit.On(BasinCentre.x+14.6f,BasinCentre.y+6);drainWay.boss="Floodcrown";drainWay.sinks=true;
            var after=new GameObject("The Floodcrown goes out with the water",typeof(AfterTalk)).GetComponent<AfterTalk>();after.flag="bossdown.Floodcrown";after.node="SelaFloodcrownGone";after.speaker="Sela";after.heardFlag="xylos.floodcrownGone";after.delay=3;

            // ---- fights. Flies and a caster in the hall; flies, then casters, in the rising water; a warden on the second anchor's platform and two in the last chamber; and, once
            // the island has lurched twice, something has come up into the hall by the foot of the ladder.
            EncounterVolume Fight(string id,Vector2 at,Vector2 size,params (string enemy,int count,float dx,float dz)[] packs)
            {
                var encounter=WorldBuilder.Encounter(id,kit.On(at.x,at.y,1.5f),new Vector3(size.x,6,size.y),packs[0].enemy,packs[0].count);
                var spawners=new List<Spawner>{encounter.spawners[0]};spawners[0].transform.position=kit.On(at.x+packs[0].dx,at.y+packs[0].dz);
                for(int i=1;i<packs.Length;i++)spawners.Add(ZoneKit.Spawn(encounter,packs[i].enemy,packs[i].count,kit.On(at.x+packs[i].dx,at.y+packs[i].dz),2.4f));
                encounter.spawners=spawners.ToArray();return encounter;
            }
            Fight("Canopy_0",new Vector2(0,14),new Vector2(30,12),("MireflySwarm",2,-3,3),("Sporecaster",1,7,5));
            Fight("Canopy_1",new Vector2(-37.5f,25),new Vector2(11,10),("MireflySwarm",3,0,3));
            Fight("Canopy_2",new Vector2(-37.5f,40),new Vector2(11,10),("Sporecaster",2,-1,4),("MireflySwarm",2,2,1));
            Fight("Canopy_3",new Vector2(-36.5f,57),new Vector2(12,10),("Rootwarden",1,-1,3));
            Fight("Canopy_4",new Vector2(-11,74),new Vector2(20,16),("Rootwarden",2,-3,4),("MireflySwarm",2,5,2));
            Fight("Canopy_Return",new Vector2(-6,14),new Vector2(18,14),("Sporecaster",2,-4,5),("Rootwarden",1,3,3)).requiredFlag="xylos.anchor2";

            if(!greybox)CanopyDress(kit);

            WorldBuilder.Boundary("Canopy south limit",new Vector3(-4,2,-15),new Vector3(112,24,2));WorldBuilder.Boundary("Canopy north limit",new Vector3(-4,2,93),new Vector3(112,24,2));
            WorldBuilder.Boundary("Canopy west limit",new Vector3(-57,2,39),new Vector3(2,24,112));WorldBuilder.Boundary("Canopy east limit",new Vector3(49,2,39),new Vector3(2,24,112));

            // ---- navigation, with every door open: the door sets are play's business, and each cuts its own hole.
            var doors=new[]{galleryDoor,stairDoor,vaultFront,vaultBack,basinDoor};var were=doors.Select(d=>d.activeSelf).ToArray();
            foreach(var door in doors)door.SetActive(false);
            var reach=new List<Vector2>{new(2,7.6f),new(8.6f,13),new(11.5f,1),new(13.2f,18.8f),new(19.5f,18.5f),new(-25,10),new(-36.5f,8),new(-41.4f,5),new(-37.5f,25),new(-37.5f,40),new(-36.4f,57.6f),new(-26,56),
                new(-17.5f,56),new(-1.4f,52),new(-10.6f,76.4f),new(-12,38),new(-9.6f,35.8f),new(-12,26),new(22,28.6f),new(22,34),BasinCentre,OnBasin(30,9.5f),OnBasin(150,9.5f),OnBasin(270,9.5f),
                OnBasin(90,14),OnBasin(200,14),OnBasin(340,14)};
            foreach(float z in LedgeZ)reach.Add(new Vector2(-31.9f,z+.2f));
            kit.BakeNavigation(new Bounds(new Vector3(-4,-2,39),new Vector3(112,22,112)),new Vector2(2,2),reach);
            for(int i=0;i<doors.Length;i++)doors[i].SetActive(were[i]);
            if(!greybox)CanopyGround2(kit);
            WalkersKeepClear(kit.zone);
            WorldBuilder.Save(scene,kit.zone);Debug.Log("WALKING_CANOPY_OK");
        }

        // ---------------------------------------------------------------- dressing
        /// <summary>What says "inside a root island": columns of root from floor to ceiling along every back wall, roots hanging from above, seed lanterns on posts, light coming
        /// down in a few soft shafts. The vault has its racks and its window, the gallery its ledges' edges, the basin its drain. All at the back and the edges: nothing stands
        /// on the camera's side of a floor.</summary>
        static void CanopyDress(ZoneKit kit)
        {
            var rng=new System.Random(73);float R()=>(float)rng.NextDouble();
            // Root columns wherever rock stands behind a floor (its north and east sides): found by walking the floors' edges.
            var columns=new ZoneKit.Shape();int stood=0;
            for(float x=-56;x<48;x+=1.5f)for(float z=-14;z<92;z+=1.5f)
            {
                var p=new Vector2(x+R()*.9f,z+R()*.9f);float clear=CanopyFloor(p,out float level);
                if(clear<.5f||clear>2.2f||R()>.45f)continue;
                // Only where the floor is toward the camera from here, or to the west of here: a back wall or an east one, the two the fixed camera faces.
                if(CanopyFloor(p-CanopyAway*2.4f,out _)>0&&CanopyFloor(p+Vector2.left*2.4f,out _)>0)continue;
                float foot=CanopySeen(p),tall=4.5f+R()*3,girth=.3f+R()*.4f,lean=(R()-.5f)*1.2f;var at=new Vector3(p.x,foot-.5f,p.y);
                // Never between the camera and a floor: with one beyond it along the camera's line, a column stops under the sightline to a hero standing there, or isn't stood.
                float clearTop=Mathf.Min(Sightline(p),LensCap(p)+.3f);if(at.y+tall>clearTop){tall=clearTop-at.y;if(tall<2.4f)continue;}
                columns.Slot(0).Tube(new[]{at,at+Vector3.up*tall*.4f+Vector3.right*lean*.3f,at+Vector3.up*tall+Vector3.right*lean},new[]{girth*1.4f,girth,girth*.8f},7,BarkDark,BarkPale,1,.25f);
                if(R()<.8f)Creepers(columns,1,at+Vector3.up*(tall*.9f),1.6f,1.2f+R()*1.6f,3,(int)(x*31+z));
                stood++;
            }
            kit.Built("Root columns",columns.Slot(1),CanopyRoot,Vine);
            Debug.Log("CANOPY_COLUMNS "+stood);
            // The walls the camera faces, ribbed: roots running down each face from the island's mass above to the floor's edge, creeper hung from the top between them. A face of
            // bark with nothing on it was the plainest thing in most frames; each runs up the face as far as four metres (the basin's east wall is eight). Kept off the doors,
            // the gates, the drain and the window, which have to read as what they are.
            var ribs=new ZoneKit.Shape();var rb=new System.Random(317);float B()=>(float)rb.NextDouble();int ribbed=0;
            var keepOff=new List<(Vector2 at,float r)>{(new(-20.2f,10),5),(new(-12,23.6f),4.6f),(new(-12,39.6f),4.6f),(new(-22.2f,56),4),(new(22,23.6f),4.6f),(new(1.5f,52),4),(new(39.6f,58.4f),3.9f)};
            foreach(float bearing in GateBearings)keepOff.Add((OnBasin(bearing,BasinRadius-.6f),2.5f));
            for(float x=-56;x<48;x+=1.25f)for(float z=-14;z<92;z+=1.25f)
            {
                var p=new Vector2(x+B()*.8f,z+B()*.8f);float clear=CanopyFloor(p,out float level),take=B();
                if(clear<.1f||clear>.7f||take>.5f||keepOff.Any(k=>Vector2.Distance(k.at,p)<k.r))continue;
                if(CanopyFloor(p-CanopyAway*1.6f,out _)>0&&CanopyFloor(p+Vector2.left*1.6f,out _)>0)continue;
                var grade=new Vector2(CanopyFloor(p+Vector2.right*.3f,out _)-CanopyFloor(p+Vector2.left*.3f,out _),CanopyFloor(p+Vector2.up*.3f,out _)-CanopyFloor(p+Vector2.down*.3f,out _));
                if(grade.sqrMagnitude<.0001f)continue;grade.Normalize();
                float thick=.13f+.2f*B(),reach=1.6f+2.6f*B(),phase=B()*6;var side=new Vector2(-grade.y,grade.x);var path=new Vector3[6];var radii=new float[6];
                for(int i=0;i<6;i++){float t=i/5f;var q=p+grade*(-.25f+reach*t)+side*Mathf.Sin(t*3+phase)*.35f;path[5-i]=new Vector3(q.x,CanopySeen(q)+thick*.45f,q.y);radii[5-i]=thick*Mathf.Lerp(1.25f,.8f,t);}
                ribs.Slot(0).Tube(path,radii,6,BarkDark,BarkPale,1,.3f);ribbed++;
                if(B()<.45f)Creepers(ribs,1,path[1]-new Vector3(grade.x,0,grade.y)*.2f,.9f,1.2f+1.6f*B(),2,(int)(x*17+z*3));
            }
            // The basin's east and north-east wall is eight metres of face behind the fight, seen from every gate: ribbed all the way up, between the east gate and the drain.
            for(float bearing=-62;bearing<84;bearing+=4.6f+2*B())
            {
                if(Mathf.Abs(Mathf.DeltaAngle(bearing,GateBearings[2]))<9||Mathf.Abs(Mathf.DeltaAngle(bearing,22.3f))<13||Mathf.Abs(Mathf.DeltaAngle(bearing,GateBearings[0]))<9)continue;
                var foot=OnBasin(bearing,BasinRadius+.25f);var outward=(foot-BasinCentre).normalized;float thick=.16f+.2f*B(),reach=3+3*B(),phase=B()*6;var side=new Vector2(-outward.y,outward.x);
                var path=new Vector3[7];var radii=new float[7];
                for(int i=0;i<7;i++){float t=i/6f;var q=foot+outward*(reach*t)+side*Mathf.Sin(t*3+phase)*.4f;path[6-i]=new Vector3(q.x,CanopySeen(q)+thick*.45f,q.y);radii[6-i]=thick*Mathf.Lerp(1.25f,.75f,t);}
                ribs.Slot(0).Tube(path,radii,6,BarkDark,BarkPale,1,.3f);ribbed++;
                if(B()<.6f)Creepers(ribs,1,path[1]-new Vector3(outward.x,0,outward.y)*.2f,1.1f,1.6f+2.2f*B(),3,(int)(bearing*13));
            }
            kit.Built("Wall ribs",ribs.Slot(1),CanopyRoot,Vine);Debug.Log("CANOPY_WALL_RIBS "+ribbed);
            // The island's mass is root too: thick roots lie along the top of every wall, following the floor's edge, so what stands round a room is woven and not a block.
            var mass=new ZoneKit.Shape();int laid=0;
            for(float x=-56;x<48;x+=1.7f)for(float z=-14;z<92;z+=1.7f)
            {
                var p=new Vector2(x+R()*1.3f,z+R()*1.3f);float clear=CanopyFloor(p,out _),girth=.28f+R()*.34f,length=4+R()*4.5f,wave=R()*6;
                if(clear<1||clear>10||R()>.7f)continue;
                // The camera's side of a floor stays as low as the ground was made: nothing is laid on it.
                if(!float.IsPositiveInfinity(Sightline(p,6)))continue;
                var grade=new Vector2(CanopyFloor(p+Vector2.right*.5f,out _)-CanopyFloor(p+Vector2.left*.5f,out _),CanopyFloor(p+Vector2.up*.5f,out _)-CanopyFloor(p+Vector2.down*.5f,out _));
                if(grade.sqrMagnitude<.0001f)continue;grade.Normalize();var along=new Vector2(-grade.y,grade.x);
                var path=new Vector3[6];var radii=new float[6];bool lies=true;
                for(int i=0;i<6;i++)
                {
                    float t=i/5f-.5f;var q=p+along*t*length+grade*Mathf.Sin(t*5+wave)*.5f;
                    if(CanopyFloor(q,out _)<.7f){lies=false;break;}
                    path[i]=new Vector3(q.x,CanopySeen(q)+girth*.35f,q.y);radii[i]=girth*Mathf.Lerp(1.15f,.7f,Mathf.Abs(t)*2);
                }
                if(!lies)continue;
                mass.Slot(0).Tube(path,radii,6,BarkDark,BarkPale,1,.3f);laid++;
            }
            kit.Built("Root mass",mass,CanopyRoot);
            Debug.Log("CANOPY_ROOT_MASS "+laid);
            // Roots run out over the floor from each anchor, half buried: every line in a room leads to the thing that holds the island there.
            var runners=new ZoneKit.Shape();
            void Runners(Vector2 at,int count,float shortest,float longest,int seed)
            {
                var rr=new System.Random(seed);
                for(int k=0;k<count;k++)
                {
                    float bearing=(k+(float)rr.NextDouble()*.6f)*Mathf.PI*2/count,length=Mathf.Lerp(shortest,longest,(float)rr.NextDouble()),wander=((float)rr.NextDouble()-.5f)*1.4f;
                    var path=new List<Vector3>();var radii=new List<float>();
                    for(int i=0;i<=8;i++)
                    {
                        float t=i/8f,turn=bearing+wander*t*t+Mathf.Sin(t*5+k)*.12f;var q=at+new Vector2(Mathf.Cos(turn),Mathf.Sin(turn))*(.5f+length*t);
                        if(CanopyFloor(q,out float level)>-.4f)break;
                        path.Add(new Vector3(q.x,level-.13f+.05f*(1-t),q.y));radii.Add(Mathf.Lerp(.3f,.1f,t));
                    }
                    if(path.Count>=3)runners.Slot(0).Tube(path.ToArray(),radii.ToArray(),6,BarkDark,BarkPale,1,.3f);
                }
            }
            // The island's mooring roots run in through the way the party came, across the arrival, as each anchor's run out from it.
            Runners(new Vector2(9,15.5f),9,7,15,301);Runners(new Vector2(11.5f,.4f),7,4,9,304);Runners(new Vector2(-37,60),6,3,6,302);Runners(new Vector2(-11,79),8,5,11,303);
            kit.Built("Anchor runners",runners,CanopyRoot);
            // The gallery's east wall, behind its ledges, is the face every frame of the gallery looks at: roots run over its lip and down in front of its face into the water, so it is a wall
            // of root and not a band of bark (look_density measured it as the plainest thing in the ledge's frame).
            var face=new ZoneKit.Shape();var fw=new System.Random(313);float W()=>(float)fw.NextDouble();
            for(float z=16.5f;z<53;z+=1.1f+W()*1.1f)
            {
                float zz=z+W()*.6f,floor=GalleryFloor(zz);var path=new Vector3[8];var radii=new float[8];float phase=W()*6,thick=.26f+.3f*W(),drop=W()<.35f?.6f:1;
                for(int i=0;i<8;i++)
                {
                    float t=i/7f;var q=new Vector2(Mathf.Lerp(-27.8f,-29.55f-thick-.2f*W(),ZoneKit.Smooth(0,.22f,t)),zz+Mathf.Sin(t*4+phase)*.6f);
                    float top=CanopySeen(new Vector2(-27.8f,zz));path[i]=new Vector3(q.x,Mathf.Lerp(top+.2f,floor+(drop<1?2.4f:-.1f),t*drop),q.y);radii[i]=thick*Mathf.Lerp(1.1f,.7f,t);
                }
                face.Slot(0).Tube(path,radii,6,BarkDark,BarkPale,1,.3f);
                // Creeper down the face between them: green lines on the brown, which is what the eye reads as a wall that grows.
                if(W()<.8f)Creepers(face,1,path[2]+Vector3.left*(thick+.15f),1.4f,1.8f+2.6f*W(),4,(int)(zz*37));
            }
            kit.Built("Gallery east wall roots",face.Slot(1),CanopyRoot,Vine);
            // The gallery's low end: thick roots along the feet of its west and south walls and out into the water, so the resting water meets root and not a straight wall foot
            // (with straight feet all round it read as a pool). Drawn only, at the walls, where nobody fights.
            // Each its own piece: one mesh round them all was a box over the whole low end, and the ground kit keeps its cover off whatever a box stands over. They lie on the floor,
            // not in it: let in by a third of their girth, more than half of each one's box was under the ground (QaPlanWorldTests.NothingFloatsOrSinksIn).
            var fr=new System.Random(311);float F()=>(float)fr.NextDouble();int footRoots=0;
            void Foot(Vector2 from,Vector2 to,float thick)
            {
                var feet=new ZoneKit.Shape();var path=new Vector3[9];var radii=new float[9];var side=new Vector2(-(to-from).y,(to-from).x).normalized;float phase=F()*6;
                for(int i=0;i<9;i++){float t=i/8f;var q=Vector2.Lerp(from,to,t)+side*Mathf.Sin(t*5+phase)*.5f;float r=thick*Mathf.Lerp(.7f,1.1f,Mathf.Sin(t*Mathf.PI));CanopyFloor(q,out float at);path[i]=new Vector3(q.x,at+r*.2f,q.y);radii[i]=r;}
                kit.Built("Gallery wall root "+(++footRoots),feet.Slot(0).Tube(path,radii,7,BarkDark,BarkPale,1,.3f),CanopyRoot);
            }
            for(float z=1.5f;z<20;z+=3.2f+F()*1.6f)Foot(new Vector2(-42.4f+F()*.4f,z),new Vector2(-39.6f-F()*2.2f,z+2.6f+F()*2),.32f+.16f*F());
            for(float x=-42;x<-31.5f;x+=3+F()*1.6f)Foot(new Vector2(x,1.7f+F()*.3f),new Vector2(x+1.2f+F()*1.5f,4+F()*2.2f),.3f+.14f*F());
            Foot(new Vector2(-42.6f,2),new Vector2(-42.2f,19),.42f);Foot(new Vector2(-42,1.8f),new Vector2(-31.6f,2.1f),.38f);
            // Two trunks of root at the back of the hall, floor to roof: cover for whoever fights there, each its own piece so the view opens a hole in the one a hero is behind.
            int trunk=0;
            foreach(var (x,z,girth) in new[]{(-12.6f,18.8f,.8f),(-1.6f,19.6f,.95f)})
            {
                var column=new ZoneKit.Shape();var foot=kit.On(x,z);
                column.Slot(0).Tube(new[]{foot+Vector3.down*.4f,foot+Vector3.up*1.2f+Vector3.right*.1f,foot+Vector3.up*4.4f+Vector3.left*.15f,foot+Vector3.up*9},new[]{girth*1.35f,girth,girth*.9f,girth*1.1f},9,BarkDark,BarkPale,2,.25f);
                for(int k=0;k<5;k++)
                {
                    var away=Quaternion.Euler(0,k*72+x*9,0)*Vector3.forward;
                    column.Slot(0).Tube(new[]{foot+away*girth*2.6f+Vector3.down*.3f,foot+away*girth*1.5f+Vector3.up*.5f,foot+away*girth*.7f+Vector3.up*1.9f},new[]{girth*.2f,girth*.3f,girth*.26f},5,BarkDark,BarkPale,1,.25f);
                }
                Creepers(column,1,foot+Vector3.up*5.2f,girth*2.4f,1.8f,5,(int)(x*13));
                var body=kit.Built("Hall trunk "+(++trunk),column.Slot(1),CanopyRoot,Vine);ZoneKit.Block(body,foot+Vector3.up*3,new Vector3(girth*1.9f,6,girth*1.9f),0,"Hall trunk");ZoneKit.Fadeable(body);
            }
            // Seed lanterns: on posts at the back of each room and along the gallery's east wall, one at each ledge.
            var lamps=new ZoneKit.Shape();
            foreach(var (x,z) in new[]{(-17f,20.5f),(21f,20.6f),(-2f,21f),(-18f,1f),(-27f,7.4f),(-15.3f,31f),(-19.5f,59.5f),(-2.6f,45f),(-19.5f,81f),(-2.6f,81f),(-41f,60.5f),(25.3f,29.5f)})SeedLamp(kit,lamps,x,z);
            foreach(float z in LedgeZ)SeedLamp(kit,lamps,-30.6f,z+2,1.9f,8,3.2f);
            foreach(float bearing in PlatformBearings){var at=OnBasin(bearing,9.5f);SeedLamp(kit,lamps,at.x+.3f,at.y+1.6f,1.6f,9,3.4f);}
            kit.Built("Seed lanterns",lamps.Slot(1),CanopyRoot,Lantern);
            // Each ledge is edged in pale root, and a pale plate lies on it: the party's own colours (cream), nothing a danger wears.
            var ledgeMarks=new ZoneKit.Shape();
            foreach(float z in LedgeZ)
            {
                float y=GalleryFloor(z+.7f)+LedgeRise+.03f;
                ledgeMarks.Slot(0).Disc(new Vector3(-31.9f,y,z+.7f),Vector3.up,1.5f,18,new Color(1.3f,1.24f,1),1.15f,new Color(1.3f,1.24f,1));
                ledgeMarks.Slot(1).Tube(new[]{new Vector3(-33.7f,y-.1f,z-1.2f),new Vector3(-33.8f,y,z+.7f),new Vector3(-33.7f,y-.1f,z+2.6f)},new[]{.14f,.17f,.14f},6,new Color(1.3f,1.2f,.96f),new Color(1.3f,1.2f,.96f));
            }
            kit.Built("Ledge marks",ledgeMarks.Slot(1),Cloth,CanopyRoot);
            // The vault's racks: three rows of shelves of seed trays along its west and north sides, pale with a few seeds that still glow. And the window: a low sill with a slot.
            var racks=new ZoneKit.Shape();
            void Rack(Vector3 at,float yaw,int seed)
            {
                var turn=Quaternion.Euler(0,yaw,0);
                foreach(int side in new[]{-1,1})racks.Slot(0).Box(at+turn*new Vector3(side*1.5f,1.1f,0),new Vector3(.14f,2.2f,.7f),turn,new Color(.8f,.72f,.56f));
                for(int shelf=0;shelf<3;shelf++)
                {
                    racks.Slot(0).Box(at+turn*new Vector3(0,.45f+shelf*.72f,0),new Vector3(3,.08f,.7f),turn,new Color(.9f,.82f,.64f),new Color(1,.94f,.76f));
                    for(int k=0;k<4;k++)racks.Slot(k==(seed+shelf)%4?1:2).Lump(at+turn*new Vector3(-1.1f+k*.74f,.62f+shelf*.72f,0),new Vector3(.26f,.13f,.24f),seed+shelf*4+k,.2f,new Color(1,.94f,.7f),new Color(.8f,.72f,.5f),7,4);
                }
            }
            for(int k=0;k<4;k++)Rack(kit.On(-20.6f,43.5f+k*4.2f),90,k);
            // The last row stands with gaps of nearly three metres between: at a metre and a half a walk through one brushed a rack (the chapter 3 routes, XylosWorldTests).
            for(int k=0;k<3;k++)Rack(kit.On(-17.2f+k*6,60.9f),0,10+k);
            var sill=kit.On(1.5f,52);
            racks.Slot(0).Box(sill+new Vector3(0,.5f,-2.6f),new Vector3(.5f,1,2.4f),Quaternion.identity,BarkDark,BarkPale).Box(sill+new Vector3(0,.5f,2.6f),new Vector3(.5f,1,2.4f),Quaternion.identity,BarkDark,BarkPale)
                .Beam(sill+new Vector3(0,1.5f,-3.8f),sill+new Vector3(0,1.5f,3.8f),.4f,.3f,BarkDark,BarkPale);
            var vaultSet=kit.Built("Vault racks and window",racks.Slot(2),Plank,Lantern,Husk);
            for(int k=0;k<4;k++)ZoneKit.Block(vaultSet,kit.On(-20.6f,43.5f+k*4.2f,1.1f),new Vector3(.8f,2.2f,3.1f),0,"Rack");
            for(int k=0;k<3;k++)ZoneKit.Block(vaultSet,kit.On(-17.2f+k*6,60.9f,1.1f),new Vector3(3.1f,2.2f,.8f),0,"Rack");
            // The ladder: root rungs across the stair, let down when the second anchor goes.
            var rungs=new ZoneKit.Shape();
            for(float z=24.5f;z<34;z+=.85f){float y=Mathf.Lerp(HallLevel,VaultLevel,Mathf.Clamp01((z-24.5f)/9))+.05f;rungs.Slot(0).Tube(new[]{new Vector3(-15.4f,y,z),new Vector3(-12,y+.05f,z+.1f),new Vector3(-8.6f,y,z)},new[]{.09f,.11f,.09f},5,BarkPale,BarkPale);}
            foreach(float x in new[]{-15.5f,-8.5f})rungs.Slot(0).Tube(new[]{new Vector3(x,HallLevel+.2f,24),new Vector3(x,(HallLevel+VaultLevel)*.5f+.25f,29),new Vector3(x,VaultLevel+.2f,34.2f)},new[]{.13f,.13f,.13f},6,BarkDark,BarkPale);
            var ladder=kit.Built("Root ladder",rungs,CanopyRoot);
            var drops=new GameObject("The root ladder drops",typeof(FlagGate)).GetComponent<FlagGate>();drops.flag="xylos.anchor2";drops.targets=new[]{ladder};ladder.SetActive(false);
            // The basin's drain, in its east wall north of the east gate: a low dark mouth under an arch of root, where the Floodcrown goes when the water does.
            var drain=new ZoneKit.Shape();var mouth=new Vector3(BasinCentre.x+15.6f,BasinRim,BasinCentre.y+6.4f);
            // Wide and high enough for what has to leave by it: the Floodcrown is eight metres long and stands 2.6.
            RootSpan(drain,mouth+new Vector3(.4f,0,-3.6f),mouth+new Vector3(.4f,0,3.6f),3.6f,.4f,91);
            drain.Slot(1).Quad(mouth+new Vector3(1,-.2f,-3.2f),mouth+new Vector3(1,-.2f,3.2f),mouth+new Vector3(1,3.2f,3.2f),mouth+new Vector3(1,3.2f,-3.2f),Vector3.left,new Color(.03f,.04f,.04f));
            kit.Built("Basin drain",drain.Slot(1),CanopyRoot,Cloth);
            // Light through the roots, and what drifts in it.
            var shafts=new GameObject("Light through the roots");var beam=kit.Glow("Shafts",true,new Color(1,1,.84f,1));
            foreach(var (x,z) in new[]{(4f,17f),(-10f,12f),(-36f,34f),(-12f,52f),(-8f,74f),(24f,58f),(31f,48f),(16f,46f)})
                ZoneKit.Strand(shafts.transform,kit.On(x,z)+Vector3.up*.05f,kit.On(x,z)+new Vector3(-1.6f,9,2.2f),beam,3,new Color(1,1,.84f,.09f),new Color(1,1,.84f,0));
            // And daylight through gaps in the walls' tops, slanting in onto the floor at their feet: a wall with light coming through it is a wall of roots and not of rock.
            foreach(var (x,z) in new[]{(-6f,21.4f),(14.5f,21.6f),(-8f,82.4f),(-17f,81.6f),(24f,67.4f),(33.5f,62f),(-41.6f,42f),(-41.4f,26f),(0.6f,64f)})
                ZoneKit.Strand(shafts.transform,kit.On(x,z)+Vector3.up*.05f,kit.On(x,z)+new Vector3(-.8f,7.5f,3.2f),beam,1.6f,new Color(1,.98f,.8f,.12f),new Color(1,1,.86f,0));
            var motes=kit.Glow("Motes",false,new Color(1.5f,1.4f,.8f,1));
            kit.Drift("Seed dust",new Vector3(-4,-1,39),new Vector3(100,7,100),motes,new Color(1,.92f,.6f,.3f),900,.04f,.13f,.16f,16);
            kit.Drift("Fireflies",new Vector3(-4,-2,39),new Vector3(100,4,100),motes,new Color(.8f,1,.5f,.6f),160,.06f,.1f,.5f,9);
        }

        /// <summary>The island's floor: leaf litter drifted against the back walls, damp where the gallery and the basin hold water, gravel on the basin's bed; fungus and ferns at the
        /// walls' feet, a pool of light under every seed lantern.</summary>
        static void CanopyGround2(ZoneKit kit)
        {
            var ground=GroundKit.Open(kit.zone,GroundKit.FindFloor(),67,.4f);
            float Smooth(float a,float b,float x)=>GroundKit.Smooth(a,b,x);
            bool InBasin(Vector2 p)=>Vector2.Distance(p,BasinCentre)<BasinRadius+1;bool InGallery(Vector2 p)=>p.x<-29;
            Vector2[] hallWay={new(2,2),new(2,12),new(20,20),new(22,34)},westWay={new(-19,10),new(-36,10),new(-38.5f,50),new(-36.4f,57.6f),new(-22,56)},upWay={new(-12,23),new(-12,40),new(-12,60),new(-10.6f,76.4f)};
            ground.ClearLane(hallWay,1.6f).ClearLane(westWay,1.6f).ClearLane(upWay,1.6f).Look("River",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Xylos/xylos-bark.png"));
            ground.Layers(new Vector4(.05f,.34f,.4f,.32f),1.2f,new GroundKit.Layer("litter",new Color(.76f,.68f,.42f),3.4f),new GroundKit.Layer("damp",new Color(.8f,.98f,.86f),4.2f),new GroundKit.Layer("gravel",new Color(.72f,.76f,.74f),3.2f));
            // Litter in smaller, more frequent drifts: in broad ones the weave between them read as a floor of big square tiles.
            ground.PaintFoot(0,2.8f,.5f,1).PaintPatches(0,4.2f,.55f,p=>InBasin(p)?0:InGallery(p)?.5f:1,2);
            ground.Paint(1,p=>InGallery(p)?Smooth(GalleryHigh+.6f,GalleryLow+.8f,GalleryFloor(p.y))*(.3f+.7f*Smooth(.36f,.56f,ground.Noise(p,5,23))):InBasin(p)?Smooth(6,12,Vector2.Distance(p,BasinCentre))*.5f:0).PaintPatches(1,3.6f,.34f,null,5);   // damp in small drifts everywhere, so no stretch of bare weave is wide enough to read as a grid
            ground.Paint(2,p=>InBasin(p)?1-Smooth(7,13,Vector2.Distance(p,BasinCentre)):0);
            ground.Decals(new[]{"Litter"},7,p=>InBasin(p)?0:.4f+.6f*(1-Smooth(1,4,ground.Edge(p))),1.1f,2.2f,new Color(.7f,.64f,.38f,.9f),false);
            ground.Decals(new[]{"SandDrift"},4,p=>InBasin(p)?0:Smooth(1.5f,3,ground.Edge(p)),2,3.4f,new Color(.8f,.74f,.52f,.6f),false);
            ground.Decals(new[]{"Lichen"},9,p=>Smooth(.44f,.6f,ground.Noise(p,11,3)),1.1f,2.6f,new Color(.44f,.68f,.42f,.85f),false);
            ground.Decals(new[]{"Stain"},1.4f,p=>InGallery(p)||InBasin(p)?1:.2f,2.4f,4.2f,new Color(.3f,.4f,.36f,.7f),false);
            ground.Decals(new[]{"PebblesA","PebblesB"},6,p=>InBasin(p)?1:InGallery(p)?.6f:.15f,1,1.9f,new Color(.7f,.7f,.62f));
            ground.LampPools(2.8f,new Color(1,.78f,.44f,.5f));
            float Foot(Vector2 p)=>1-Smooth(.8f,3.2f,ground.Edge(p));
            ground.Cover(new[]{"FungusA","FungusB"},22,p=>Mathf.Max(Foot(p),.3f*Smooth(.56f,.66f,ground.Noise(p,5,17)))*(InBasin(p)?0:1),.12f,.3f,new Color(.95f,.84f,.64f),3);
            ground.Cover(new[]{"Fern"},16,p=>Foot(p)*(InBasin(p)||InGallery(p)?0:1),.26f,.5f,new Color(.48f,.78f,.4f),2);
            ground.Cover(new[]{"Sprouts","TuftC"},20,p=>(InBasin(p)?0:1)*Smooth(.48f,.62f,ground.Noise(p,6,9)),.12f,.26f,new Color(.56f,.82f,.44f),3);
            ground.Cover(new[]{"Reeds"},20,p=>InBasin(p)?Smooth(13,15.5f,Vector2.Distance(p,BasinCentre)):InGallery(p)?Foot(p)*.6f:0,.3f,.56f,new Color(.58f,.76f,.44f),3);
            ground.Rocks(5,p=>InBasin(p)?.8f:Foot(p)*.4f,.08f,.26f,new Color(.66f,.68f,.6f));
            // Daylight comes down through the roots overhead in broken pieces, and they sway: the one thing in here that moves when nothing else does.
            // Light and soft-edged: the shade is laid on the water as well as the floor, and on the basin's dark water a crisp one lay as pale and dark camouflage.
            ground.MovingShade("dapple",9,Vector2.zero,.18f,.5f,.1f,new Vector2(.3f,.2f),new Vector2(5,8));
            ground.Finish();
        }
    }
}
