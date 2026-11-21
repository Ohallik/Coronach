using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>Rootmarket (town). Map brief and evidence: docs/maps/Rootmarket.md.</summary>
    public static partial class XylosBuilder
    {
        // ---------------------------------------------------------------- the ground: root platforms in river water
        internal static readonly Vector2 Pool=new(0,44);
        internal const float PoolA=20,PoolB=14,RingWide=11,WaterLevel=-.75f,RiverBed=-2.4f,RimTall=.5f,RimWide=1.1f;
        /// <summary>The platforms that hang off the ring round the pool, each an oval: the landing, the seed house's forecourt, Binna's kitchen, the press, the mooring, Senn's island and the ferry stage.</summary>
        static readonly (Vector2 c,float a,float b)[] Lobes={(new(0,20),13,9),(new(0,74),17,12.5f),(new(26,65),12,10.5f),(new(37,43),11,10.5f),(new(26,23),10.5f,10),(new(-46,13),8,7),(new(-30,44),7.5f,7.5f)};
        static readonly Vector2 CausewayFrom=new(46,43),CausewayTo=new(61,43);const float CausewayHalf=3.6f;
        /// <summary>The upper dry walkway: a root ridge that leaves the ring's north-west, climbs, crosses behind the seed house and comes down into the kitchen. Far enough behind the seed
        /// house (Shadowed) that someone walking it is never hidden by the roof.</summary>
        internal static readonly Vector2[] Walkway={new(-24.5f,56.5f),new(-30,68),new(-25,85),new(-11,97.5f),new(0,101),new(11,97.5f),new(25,88),new(33.5f,78),new(32,70.5f)};
        const float WalkHalf=2.9f,WalkTop=2.4f,WalkRamp=11;
        /// <summary>Where a rim is left open for something built to cross: the two ends of the raft crossing, and the two ends of the rope walk to Senn's island. Each runs from the
        /// platform's edge in through the whole depth of its rim, as wide as what crosses: a round gap at the edge alone closed to a body's width at the rim's inner face.</summary>
        static readonly (Vector2 from,Vector2 to,float half)[] Openings={(new(-19.6f,44),new(-22,44),1.5f),(new(19.6f,44),new(22,44),1.5f),(new(-24.3f,27.8f),new(-22.5f,29.05f),1.15f),(new(-38.9f,17.6f),new(-40.7f,16.35f),1.15f)};
        /// <summary>Metres from an oval's edge, negative inside. Exact along its axes and close everywhere near the edge, which is all a rim needs.</summary>
        static float Oval(Vector2 p,Vector2 c,float a,float b)
        {
            var q=p-c;float e=Mathf.Sqrt(q.x*q.x/(a*a)+q.y*q.y/(b*b));if(e<1e-4f)return -Mathf.Min(a,b);
            return (e-1)*e/Mathf.Sqrt(q.x*q.x/(a*a*a*a)+q.y*q.y/(b*b*b*b));
        }
        /// <summary>Metres from the edge of the low platforms (the ring and what hangs off it), negative on them.</summary>
        internal static float LowLand(Vector2 p)
        {
            float pool=Oval(p,Pool,PoolA,PoolB);
            float d=Mathf.Min(Mathf.Max(-pool,Oval(p,Pool,PoolA+RingWide,PoolB+RingWide)),ZoneKit.Segment(p,CausewayFrom,CausewayTo)-CausewayHalf);
            foreach(var (c,a,b) in Lobes)d=Mathf.Min(d,Oval(p,c,a,b));
            return Mathf.Max(d,-pool);
        }
        static float WalkLength{get{float total=0;for(int i=1;i<Walkway.Length;i++)total+=Vector2.Distance(Walkway[i-1],Walkway[i]);return total;}}
        /// <summary>Metres from the upper walkway's middle line, and how far along it the nearest point is.</summary>
        static float FromWalk(Vector2 p,out float along)
        {
            float best=float.PositiveInfinity,run=0;along=0;
            for(int i=1;i<Walkway.Length;i++)
            {
                Vector2 a=Walkway[i-1],d=Walkway[i]-a;float length=d.magnitude,t=Mathf.Clamp01(Vector2.Dot(p-a,d)/(length*length)),distance=Vector2.Distance(p,a+d*t);
                if(distance<best){best=distance;along=run+t*length;}
                run+=length;
            }
            return best;
        }
        static float WalkHeight(float along)=>WalkTop*ZoneKit.Smooth(0,WalkRamp,along)*ZoneKit.Smooth(0,WalkRamp,WalkLength-along);
        static float Opening(Vector2 p)
        {
            float open=0;foreach(var (from,to,half) in Openings)open=Mathf.Max(open,1-ZoneKit.Smooth(half,half+.25f,ZoneKit.Segment(p,from,to)));
            return open;
        }
        /// <summary>The town's ground. Platforms of packed earth held in woven root, standing in the river: each has a knee-high root rim along every edge, steep on both faces, so the
        /// edge of the floor is something a body meets and nobody walks off into the water (no invisible wall stands anywhere). The rim is left out only where a deck crosses.
        /// Behind it all, north and east, the bank the jungle stands on; south and west stay open water, because that is the fixed camera's side (D119).</summary>
        static float RootHeight(Vector2 p)=>RootGround(p,true);
        /// <summary>The same ground as it is drawn: without the rims. A kerb half a metre high and two cells wide is nothing a heightfield can draw. It came out as a saw of
        /// triangles, bark against pale earth at full contrast (a rise of half a metre gets no wall-foot shade), and finer cells only made the teeth smaller. The rims are drawn
        /// as roots laid along the same lines (<see cref="RootKerbs"/>); bodies and the bake still meet the rim that is in the collider.</summary>
        static float RootDrawn(Vector2 p)=>RootGround(p,false);
        static float RootGround(Vector2 p,bool rimmed)
        {
            float low=LowLand(p),walk=FromWalk(p,out float along)-WalkHalf,top=WalkHeight(along),kerb=rimmed?1:0;
            float relief=.05f*(Mathf.PerlinNoise(p.x*.11f+7,p.y*.11f+3)-.5f),lumpy=.8f+.4f*Mathf.PerlinNoise(p.x*.35f,p.y*.35f);
            // No low rim where the ridge's ramp comes down onto a platform, or it would lie across the ramp's foot.
            float rim=kerb*RimTall*lumpy*ZoneKit.Smooth(-RimWide,-RimWide+.35f,low)*(1-Opening(p))*ZoneKit.Smooth(-.2f,.5f,walk);
            float lowY=low<0?relief+rim:Mathf.Lerp(relief+rim,RiverBed,ZoneKit.Smooth(0,.6f,low));
            // The ridge keeps a rim of its own wherever it stands over water or well above the platform beside it; at its two feet it opens onto the floor.
            float ridgeRim=kerb*RimTall*.9f*lumpy*ZoneKit.Smooth(-.9f,-.55f,walk)*Mathf.Max(ZoneKit.Smooth(-1.2f,-.4f,low),ZoneKit.Smooth(.5f,1.2f,top));
            float ridgeY=walk<0?top+relief+ridgeRim:Mathf.Lerp(top+relief+ridgeRim,RiverBed,ZoneKit.Smooth(0,.55f,walk));
            // The river leaves east through a gap in the bank, which is why the gate is there.
            float gap=1-ZoneKit.Smooth(5,9,Mathf.Abs(p.y-43));
            float bank=ZoneKit.Smooth(104.5f,109,p.y)*(11+4*Mathf.PerlinNoise(p.x*.05f,2))+ZoneKit.Smooth(66.5f,71,p.x)*(10+4*Mathf.PerlinNoise(3,p.y*.05f))*(1-gap);
            // Low enough that what stands on it is in the frame: the fixed camera never sees higher than itself, so a tall bank is a wall of bark and nothing else.
            return Mathf.Max(Mathf.Max(lowY,ridgeY),RiverBed+bank*.56f);
        }
        /// <summary>The floor's own colour, before the ground kit lays anything on it: moss-green earth, paler where feet go (round the ring, across each forecourt), dark and damp toward
        /// every rim, and the rims' own tops the colour of root.</summary>
        static Color RootTone(Vector2 p)
        {
            float low=LowLand(p),walk=FromWalk(p,out _)-WalkHalf,edge=Mathf.Min(-low,walk<0?-walk:99);
            float ring=1-ZoneKit.Smooth(1.6f,3.4f,Mathf.Abs(Oval(p,Pool,PoolA+5.5f,PoolB+5.5f)));
            float worn=ring;foreach(var (at,reach) in Worn)worn=Mathf.Max(worn,1-ZoneKit.Smooth(reach*.55f,reach,Vector2.Distance(p,at)));
            if(walk<-.6f)worn=Mathf.Max(worn,.8f);
            float mottle=.9f+.2f*Mathf.PerlinNoise(p.x*.19f+3,p.y*.19f+7),patch=Mathf.PerlinNoise(p.x*.06f+11,p.y*.06f+5);
            var moss=Color.Lerp(new Color(.5f,.66f,.36f),new Color(.66f,.68f,.38f),ZoneKit.Smooth(.35f,.7f,patch));
            var tone=Color.Lerp(moss,new Color(.92f,.8f,.58f),worn*.85f);
            tone=Color.Lerp(tone,new Color(.34f,.46f,.34f),(1-ZoneKit.Smooth(1.2f,3.2f,edge))*.75f);
            // A rim's crest is root, not earth.
            if(edge<RimWide-.3f&&Opening(p)<.5f)tone=new Color(.3f,.25f,.19f);
            return new Color(tone.r*mottle,tone.g*mottle,tone.b*mottle,1);
        }
        /// <summary>Where the ground is trodden pale: each forecourt and doorstep, with how far the wear reaches.</summary>
        static readonly (Vector2 at,float reach)[] Worn={(new(0,21),9),(new(0,70),8),(new(25,64),7.5f),(new(37,44),7.5f),(new(28,21.5f),7),(new(-28,44),6),(new(-45.5f,13.5f),5),(new(54,43),5)};

        // ---------------------------------------------------------------- who and what stands where (plan coordinates; routes and tests read these)
        internal static readonly Vector2 RootArrival=new(0,17),PadAt=new(0,24.2f),RuvaAtMooring=new(28.5f,21),RuvaAtSeedHouse=new(5.5f,68.5f),RopeAt=new(32.4f,18.8f),TeoAt=new(-8.5f,24.6f),JaskAt=new(-25,47.6f),PoleAt=new(-22.3f,46.4f),
            BinnaAt=new(24.6f,68),HaddaAt=new(35.4f,46.6f),PressAt=new(41,48.6f),CradleAt=new(41.6f,40.4f),SennAt=new(-46.4f,13.6f),GateAt=new(60.2f,43),RiverSpawn=new(54,43),NoticeAt=new(-2.7f,72.3f),DoorAt=new(2.5f,72.3f),
            SeedHouseAt=new(0,78.2f),CounterAt=new(36.3f,49.9f),WalkCacheAt=new(0,102.3f),IslandCacheAt=new(-51.2f,11),IslandMoored=new(38.6f,13.2f),IslandAdrift=new(50,4.5f);
        static readonly Vector2[] RootPeople={RuvaAtMooring,RuvaAtSeedHouse,TeoAt,JaskAt,BinnaAt,HaddaAt,SennAt};
        /// <summary>The floor signs, for whatever is scattered to keep off: their spots and half their widths.</summary>
        static readonly (Vector2 at,float half)[] RootSigns={(new(5.2f,21.6f),2.6f),(new(-25.4f,52.4f),2.4f),(new(-7.4f,66.4f),2.4f),(new(20.4f,59.4f),3.2f),(new(32.6f,37.8f),2.2f),(new(23.6f,25.6f),1.8f),(new(-43.4f,9.4f),2.8f),(new(51.5f,41.2f),2.2f),(new(56.6f,45f),3.4f)};
        /// <summary>What someone with no name wears or carries. All of them are the one grower's body, so who is who is told by outline: a hat's cone, a pannier on the back, a yoke
        /// across the shoulders with a basket at each end, a pole in the hand, a tray on the head, a coil of line over a shoulder; by build (each stands a little taller or shorter
        /// than the next); and by the dye of the cloth.</summary>
        enum Look{Bare,Hat,Pannier,Yoke,Pole,Tray,HatPannier,HatPole,Coil}
        /// <summary>Which of the chapter's two unnamed Okhari bodies someone wears, by the stand-in listed for them: the grower (green and rust work clothes) for those who stood in as
        /// Neve or Orrin, the plain body (sand-coloured wrap, teal shawl, a red sash) for the rest. Two outfits under the hats, panniers, yokes and poles, and no two neighbours in one.</summary>
        static string Townsbody(string standIn)=>standIn is "Neve" or "Orrin"||ZoneKit.Prefab("Characters","OkhariBody")==null?Grower(standIn):"OkhariBody";
        /// <summary>People with no names, standing: the stand-in until the grower's body is staged, where, facing, dye, look and build. No two who stand together share a look.
        /// Off every route between the named, off the signs, none between the camera and anyone with a name.</summary>
        static readonly (string body,float x,float z,float yaw,Color tint,Look look,float build)[] RootFolk=
        {
            // two cooks behind Binna's counter
            ("Mira",29.8f,71.9f,200,new(.92f,.86f,.72f),Look.Bare,1.08f),("Neve",36.2f,65.2f,250,new(.62f,.8f,.66f),Look.Hat,.94f),
            // the ferry queue, in a line up the ring's inner side toward the stage head, each looking at the back ahead: a pannier, a yoke, a tray. East of the way from Teo to Jask
            // (two of them stood on it, half a metre and a metre from its line) and west of the walk down to the rope walk.
            ("Hal",-24f,41.6f,50,new(.66f,.78f,.56f),Look.Pannier,1f),("Orrin",-24.2f,39.8f,15,new(.88f,.74f,.5f),Look.Yoke,1.07f),("SurvivorNatural",-24.1f,38f,5,new(.56f,.72f,.72f),Look.Tray,.9f),
            // two arguing over a coil of rope, short of the mooring: the short one has it over a shoulder already
            ("Hal",18,30.2f,60,new(.86f,.68f,.5f),Look.Coil,.9f),("Orrin",19.6f,31,240,new(.58f,.74f,.6f),Look.Hat,1.1f),
            // seed-house hands either side of the door, and one at the press, by its south-east corner and off the way from Hadda to the river gate (he stood a metre from its line)
            ("Neve",-8.2f,75.2f,120,new(.74f,.78f,.5f),Look.Tray,1f),("SurvivorNatural",8.6f,76,230,new(.58f,.76f,.68f),Look.HatPannier,.95f),("Mira",44.6f,46.6f,290,new(.86f,.72f,.52f),Look.Pole,1.04f),
            // an angler on the west rim, back to the town
            ("Orrin",-33.4f,40,250,new(.6f,.7f,.52f),Look.HatPole,1f),
        };
        /// <summary>Where nothing is put down: the ways and what stands about the named, the signs, the townsfolk, the places to stand. What the builder places by hand is held to the
        /// ways by <see cref="NothingStandsOnTheWays"/>, which reads the finished scene, so a coordinate typed a metre out can't put a lantern post across a walk again.</summary>
        static bool RootKeepsOff(Vector2 p)
        {
            if(FromWays(p)<WayClear+.7f)return true;
            foreach(var person in RootPeople)if(ZoneKit.InSightOf(p,person,2.6f,4.5f,1.6f))return true;
            if(ZoneKit.OnASign(p,RootSigns))return true;
            foreach(var folk in RootFolk)if(Vector2.Distance(p,new Vector2(folk.x,folk.z))<2.2f)return true;
            foreach(var spot in new[]{RootArrival,PadAt,RopeAt,PoleAt,PressAt,CradleAt,GateAt,RiverSpawn,NoticeAt,DoorAt,WalkCacheAt,IslandCacheAt,CounterAt})if(Vector2.Distance(p,spot)<3)return true;
            return Vector2.Distance(p,SeedHouseAt)<7.5f;
        }
        /// <summary>The ways feet go between the town's places: nothing is scattered within reach of these, and the ground kit keeps them clear.</summary>
        static readonly Vector2[][] RootWays=
        {
            new[]{RootArrival,new Vector2(13,25.5f),new Vector2(20,26.5f),RuvaAtMooring,RopeAt},new[]{RootArrival,TeoAt,new Vector2(-19,27),new Vector2(-27,38),JaskAt,new Vector2(-26,52),new Vector2(-17,62),new Vector2(-6,68),RuvaAtSeedHouse},
            new[]{RuvaAtSeedHouse,new Vector2(11,63.5f),new Vector2(17.5f,62.5f),BinnaAt,new Vector2(23.5f,58),new Vector2(27.5f,50),new Vector2(28.5f,45),HaddaAt,new Vector2(45,43),GateAt},new[]{HaddaAt,new Vector2(29,40),new Vector2(25,32),RuvaAtMooring},new[]{new Vector2(-20,44),new Vector2(20,44)},
            // Into the rope walk by its mouth: a corner cut short of it ran across the end of its south rail.
            new[]{new Vector2(-19,27),new Vector2(-21.5f,28.6f),new Vector2(-23.4f,28.4f),new Vector2(-39.2f,17.4f),SennAt},
        };
        /// <summary>How near a way's line the edge of anything solid may come: the bake's agent is .55 m round and its cells .15, so nearer than this a straight walk along the way is cut.
        /// Someone standing with no name keeps <see cref="FolkClear"/> from it (Lanternwick's rule, 1.6 m, with room to spare).</summary>
        const float WayClear=.9f,FolkClear=2;
        /// <summary>Refuses the finished town if anything solid the builder stood in it, by hand or by any helper, cuts a way: its footprint (a box's own turned rectangle, otherwise
        /// its bounds) within <see cref="WayClear"/> of a way's line, a townsperson within <see cref="FolkClear"/>, or a walker's loop within 1.6 m. Decks are floor and the named
        /// stand at the ways' ends, so neither counts; the landform is the only thing wider than forty metres.</summary>
        static void NothingStandsOnTheWays()
        {
            Physics.SyncTransforms();var faults=new List<string>();
            float Outside(Vector2 p,Vector2 centre,float yaw,Vector2 half)
            {
                var d=p-centre;float r=yaw*Mathf.Deg2Rad,x=d.x*Mathf.Cos(r)-d.y*Mathf.Sin(r),z=d.x*Mathf.Sin(r)+d.y*Mathf.Cos(r);
                float ox=Mathf.Abs(x)-half.x,oz=Mathf.Abs(z)-half.y;return new Vector2(Mathf.Max(ox,0),Mathf.Max(oz,0)).magnitude+Mathf.Min(Mathf.Max(ox,oz),0);
            }
            (float gap,Vector2 from,Vector2 to) Nearest(Vector2 centre,float yaw,Vector2 half)
            {
                var best=(float.PositiveInfinity,Vector2.zero,Vector2.zero);
                foreach(var way in RootWays)for(int i=1;i<way.Length;i++)
                {
                    int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(way[i-1],way[i])/.1f));
                    for(int k=0;k<=steps;k++){float gap=Outside(Vector2.Lerp(way[i-1],way[i],k/(float)steps),centre,yaw,half);if(gap<best.Item1)best=(gap,way[i-1],way[i]);}
                }
                return best;
            }
            foreach(var solid in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if(!solid.enabled||solid.isTrigger||solid.GetComponentInParent<Npc>()!=null)continue;
                var bounds=solid.bounds;if(bounds.size.x>40||bounds.size.z>40)continue;
                var at=new Vector2(bounds.center.x,bounds.center.z);if(bounds.max.y<Mathf.Max(0,RootDrawn(at))+.32f)continue;
                bool folk=solid.name=="Townsperson",walker=solid.GetComponent<WalkLoop>()!=null;if(walker)continue;
                float yaw=0;var half=new Vector2(bounds.extents.x,bounds.extents.z);
                if(solid is BoxCollider box&&Mathf.Abs(solid.transform.eulerAngles.x)<.5f&&Mathf.Abs(Mathf.DeltaAngle(solid.transform.eulerAngles.z,0))<.5f)
                {
                    var c=solid.transform.TransformPoint(box.center);at=new Vector2(c.x,c.z);yaw=solid.transform.eulerAngles.y;
                    var size=Vector3.Scale(box.size,solid.transform.lossyScale);half=new Vector2(Mathf.Abs(size.x),Mathf.Abs(size.z))*.5f;
                }
                if(folk){half=Vector2.zero;yaw=0;}
                var (gap,from,to)=Nearest(at,yaw,half);float need=folk?FolkClear:WayClear;
                if(gap<need)faults.Add($"{(solid.transform.parent!=null?solid.transform.parent.name+"/":"")}{solid.name} at ({at.x:0.0}, {at.y:0.0}) is {gap:0.00} m from the way from {from} to {to} (needs {need})");
            }
            foreach(var walk in UnityEngine.Object.FindObjectsByType<WalkLoop>(FindObjectsSortMode.None))foreach(var p in walk.path)
            {
                var (gap,from,to)=Nearest(new Vector2(p.x,p.z),0,Vector2.zero);
                if(gap<1.6f)faults.Add($"{walk.name}'s loop at ({p.x:0.0}, {p.z:0.0}) is {gap:0.00} m from the way from {from} to {to}");
            }
            if(faults.Count>0)throw new InvalidOperationException("Rootmarket: things stand on the town's ways:\n"+string.Join("\n",faults.Distinct()));
            Debug.Log("XYLOS_WAYS_CLEAR Rootmarket");
        }
        /// <summary>Refuses a thing that stands on the floor but was put on a rim or in the water: the platforms are ovals, and a number a metre out is a grower up to the knees in a root.</summary>
        static void OnTheFloor(string what,params Vector2[] spots)
        {
            foreach(var at in spots)
            {
                float low=LowLand(at),walk=FromWalk(at,out _)-WalkHalf,inside=Mathf.Max(-low,-walk);
                if(inside<RimWide+.15f&&Opening(at)<.5f)throw new InvalidOperationException($"Rootmarket: {what} at {at} is {inside:0.0} m from the floor's edge: on a rim or in the water");
            }
        }
        static float FromWays(Vector2 p){float d=float.PositiveInfinity;foreach(var way in RootWays)d=Mathf.Min(d,ZoneKit.FromPath(p,way));return d;}

        static void Rootmarket()
        {
            var kit=ZoneKit.Ground("Rootmarket",RootHeight,3.2f);
            var scene=WorldBuilder.Begin(kit.zone);
            // Half-metre cells: every wall's top edge is in view somewhere. The walls are woven root, dark with damp from the waterline up: here the floor is at a wall's top and
            // not its foot, so it is the top of the wall that has to sit in shade where the two textures meet. Drawn without the rims (RootDrawn); the roots that are the rims
            // lie along the same edge and cover it.
            var land=kit.Terrain("Root platforms",-62,76,-8,114,.5f,Loam,RootWall,RootTone,new Color(.7f,.68f,.62f),new Color(.58f,.57f,.52f),RootDrawn);
            RootKerbs(kit);
            var water=kit.Water("River water",-62,76,-8,114,1,WaterLevel,Water,5);
            // The licensed water can take this sheet's place on a machine that has it (D131): same mesh, the resource named for the zone.
            var patch=water.AddComponent<BiomePatch>();patch.resourceKey="Rootmarket-river";patch.palette="River";patch.water=true;
            WorldBuilder.Spawn("Arrival",kit.On(RootArrival.x,RootArrival.y));WorldBuilder.Spawn("River",kit.On(RiverSpawn.x,RiverSpawn.y));WorldBuilder.Spawn("Press",kit.On(34,41.2f));

            OnTheFloor("someone with a name",RootPeople);OnTheFloor("a floor sign",RootSigns.Select(sign=>sign.at).ToArray());OnTheFloor("a townsperson",RootFolk.Select(f=>new Vector2(f.x,f.z)).ToArray());
            OnTheFloor("a place to stand",RootArrival,PadAt,RopeAt,PoleAt,PressAt,CradleAt,RiverSpawn,NoticeAt,DoorAt,WalkCacheAt,IslandCacheAt,CounterAt);
            foreach(var way in RootWays)OnTheFloor("a way's corner",way.Where(p=>Mathf.Abs(p.y-44)>.1f||Mathf.Abs(p.x)>21).ToArray());

            // ---- the landing: a pad on the pool's south edge, and the way back up
            // A round stage of single boards on a ring of piles, a fender of line round its rim, a ring of pale paint, a lantern post at each quarter and a windsock at its back:
            // what a town that lands ships on a pool builds, where a station would bolt down a plate.
            var padAt=kit.On(PadAt.x,PadAt.y);var stage=new ZoneKit.Shape();Color board=new(.86f,.72f,.5f),pale=new(1,.96f,.82f),pile=new(.6f,.48f,.34f),line=new(.95f,.86f,.62f);var boards=new System.Random(17);
            for(float z=-2.52f;z<2.6f;z+=.36f)
            {
                // Each board cut to the round, its own tone, slipped a finger along.
                float half=Mathf.Sqrt(Mathf.Max(.05f,2.7f*2.7f-z*z)),shade=.84f+.24f*(float)boards.NextDouble(),slip=((float)boards.NextDouble()-.5f)*.12f;
                Board(stage.Slot(0),padAt+new Vector3(slip,.07f,z),half*2,.33f,.08f,90,ZoneKit.Shape.Dim(board,.6f),ZoneKit.Shape.Dim(board,shade));
            }
            for(int k=0;k<12;k++)
            {
                // Piles round the rim, every third one left tall to make a ship's line fast to, with a cap.
                float a=k*Mathf.PI*2/12+.26f;var q=padAt+new Vector3(Mathf.Cos(a)*2.8f,-.3f,Mathf.Sin(a)*2.8f);bool tall=k%3==0;
                stage.Slot(0).Post(q,tall?.95f:.4f,.15f,.13f,7,ZoneKit.Shape.Dim(pile,.8f),pile);
                if(tall)stage.Slot(0).Lump(q+Vector3.up*.97f,new Vector3(.17f,.09f,.17f),k,.1f,board,pile,7,4);
            }
            var fender=new Vector3[25];for(int k=0;k<25;k++){float a=k*Mathf.PI*2/24;fender[k]=padAt+new Vector3(Mathf.Cos(a)*2.74f,.03f+.03f*Mathf.Sin(a*6),Mathf.Sin(a)*2.74f);}
            stage.Slot(0).Tube(fender,new[]{.09f},6,line,ZoneKit.Shape.Dim(line,.8f),1,2,false);
            stage.Slot(1).Disc(padAt+Vector3.up*.118f,Vector3.up,2.1f,28,pale,1.82f).Disc(padAt+Vector3.up*.118f,Vector3.up,.5f,14,pale);
            for(int k=0;k<4;k++)
            {
                float a=k*Mathf.PI*.5f;var spoke=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                // A bar of the same paint at each quarter, pointing in: which way is the pad's middle, from any side.
                stage.Slot(1).Box(padAt+spoke*1.3f+Vector3.up*.118f,new Vector3(.7f,.004f,.16f),Quaternion.LookRotation(Vector3.Cross(Vector3.up,spoke)),pale);
                a+=.785f;var q=padAt+new Vector3(Mathf.Cos(a)*2.98f,0,Mathf.Sin(a)*2.98f);var lean=new Vector3(-Mathf.Cos(a)*.34f,0,-Mathf.Sin(a)*.34f);
                stage.Slot(0).Tube(new[]{q,q+Vector3.up*1.5f,q+lean+Vector3.up*1.9f},new[]{.1f,.08f,.05f},6,pile,board).Slot(2).Lump(q+lean+Vector3.up*1.66f,new Vector3(.15f,.2f,.15f),k,.05f,Color.white,Color.white,7,4);
            }
            // The windsock, north of the pad where it hides nothing: a mast and a sleeve of dyed cloth.
            var mast=padAt+new Vector3(1.3f,0,3.25f);
            stage.Slot(0).Tube(new[]{mast,mast+Vector3.up*3.3f},new[]{.07f,.045f},6,pile,board).Slot(1).Tube(new[]{mast+Vector3.up*3.18f,mast+new Vector3(.75f,3.05f,.1f),mast+new Vector3(1.45f,2.8f,.16f)},new[]{.19f,.15f,.07f},8,new Color(.92f,.42f,.26f),new Color(1,.9f,.74f),1,1,false);
            var dock=kit.Built("Landing stage",stage.Slot(2),Plank,Cloth,Lantern);ZoneKit.Block(dock,padAt+Vector3.up*.02f,new Vector3(3.8f,.16f,3.8f),0,"Landing stage deck");ZoneKit.Block(dock,padAt+Vector3.up*.02f,new Vector3(3.8f,.16f,3.8f),45,"Landing stage deck");
            ZoneKit.Block(dock,mast+Vector3.up*.8f,new Vector3(.3f,1.6f,.3f),0,"Windsock mast");
            var point=new GameObject("Landing pad",typeof(DockingPad));point.transform.position=padAt;dock.transform.SetParent(point.transform,true);
            var pad=point.GetComponent<DockingPad>();pad.prompt="Launch — Xylos Nearspace";pad.range=4;pad.scene="XylosNearspace";pad.spawn="Rootmarket";
            kit.Sign("LANDING POOL",RootSigns[0].at.x,RootSigns[0].at.y);

            Timber(kit);
            SeedHouse(kit);
            Kitchen(kit);
            Press(kit);
            Jungle(kit);

            // ---- the river gate: east, past the press, where the bank opens
            var gate=RootArch(kit,"River gate",new Vector3(GateAt.x,0,GateAt.y),6.4f,90);
            // Its name on the causeway's north side, four metres short of the arch and beside the walk to it: the floor sign RIVER GATE is nine metres back, and a way out with nothing
            // written within seven metres of it read as an unlabelled column (QaStoryWorldTests.EveryChapterTwoExitIsSigned).
            kit.Way(gate,"River gate — The Braided River","BraidedRiver","Arrival","xylos.termsHeard","Ruva Okh hasn't said what she wants yet: find her at the mooring",4.6f,RootSigns[8].at);
            kit.Sign("RIVER GATE",RootSigns[7].at.x,RootSigns[7].at.y);

            // ---- people
            var ruva=kit.Person("Ruva",new Vector3(RuvaAtMooring.x,0,RuvaAtMooring.y),128,"",false,"xylos.voteHeard","RuvaAfterVote","RuvaTerms");
            Tiers(ruva);
            var ruvaHome=kit.Person("Ruva",new Vector3(RuvaAtSeedHouse.x,0,RuvaAtSeedHouse.y),205,"",false,"xylos.voteHeard","RuvaAfterVote","RuvaTerms");
            Tiers(ruvaHome);
            // At the mooring until the party has been out on the river; at the seed house from then on, where the crews come home to and the vote is held. Decided as the town loads.
            Where("Ruva is at the mooring",ruva.gameObject,null,null,null,"xylos.archSeen",false);
            Where("Ruva is at the seed house",ruvaHome.gameObject,"xylos.archSeen",null,null,null,false);
            var binna=kit.Person("Binna",new Vector3(BinnaAt.x,0,BinnaAt.y),215,"xylos.voteHeard");
            var hadda=kit.Person("Hadda",new Vector3(HaddaAt.x,0,HaddaAt.y),250,"xylos.voteHeard",true,"chapter3.complete","HaddaAir");
            hadda.finalCompletionFlag="hadda.airHeard";hadda.finalRepeatNode="HaddaPost";
            var senn=kit.Person("Senn",new Vector3(SennAt.x,0,SennAt.y),78,"xylos.voteHeard");senn.postNode="SennAfterVote";
            // Jask poles the town ferry until the party first goes out on the river; from then he's at the mid-river stage, and after the vote he is nowhere.
            var jask=kit.Person("Jask",new Vector3(JaskAt.x,0,JaskAt.y),95,"",false);
            Where("Jask is at the town ferry",jask.gameObject,null,null,null,"xylos.archSeen",false);
            // Teo comes down once the terms are heard, stands by Marrow-3 from the Seedthane's fall until the vote, and is back after it. The pool is out of sight of the mooring, so he can arrive at once.
            var teo=kit.Person("Teo",new Vector3(TeoAt.x,0,TeoAt.y),70,"xylos.voteHeard");
            Where("Teo is by the landing pool",teo.gameObject,"xylos.termsHeard","bossdown.Seedthane","xylos.voteHeard",null,true);
            foreach(var folk in RootFolk)Dress(kit,kit.Townsperson(Townsbody(folk.body),new Vector3(folk.x,0,folk.z),folk.yaw,folk.tint),folk.look,folk.build);
            // A child after a seed pod, round the seed house's west flank; someone carrying between the kitchen's back and the seed house. Both off every way between the named.
            var child=kit.Walker(Townsbody("Neve"),new[]{new Vector2(-11.5f,77),new Vector2(-8.8f,81.4f),new Vector2(-11.4f,80.6f),new Vector2(-13.2f,78.6f)},2.3f,.5f,new Color(.86f,.9f,.56f),"Neve");
            child.name="A child after a pod";
            var podBody=kit.Built("Runaway seed pod",new ZoneKit.Shape().Lump(Vector3.zero,new Vector3(.2f,.2f,.26f),3,.1f,new Color(1,.8f,.4f),new Color(.8f,.5f,.2f),7,4),Cloth);
            podBody.transform.SetParent(child.transform,false);podBody.transform.localPosition=new Vector3(0,.22f,1.9f);Dress(kit,child.gameObject,Look.Bare,.72f);
            var carrier=kit.Walker(Townsbody("Hal"),new[]{new Vector2(11.6f,77),new Vector2(13,73.6f),new Vector2(14.6f,75.6f),new Vector2(13,79.4f)},1.1f,3.2f,new Color(.7f,.82f,.6f),"Hal");
            carrier.name="A grower carrying trays";Dress(kit,carrier.gameObject,Look.Tray,1.04f);

            // ---- the arrival: a root island on its lines, a crew a back short, and the conversation that starts on the way to them
            Arrival(kit,ruva,binna);

            // ---- things to read, find and open
            // The notice is nailed to the door's west leaf; the doorway's own look is on its east side, far enough along that the two prompts never share a spot.
            kit.Discovery("The Compact notice",new Vector3(NoticeAt.x,0,NoticeAt.y),"Read the Compact notice","xylos.noticeRead","","SelaCompactNotice","Sela",null,0,2.4f);
            kit.Discovery("The seed house doorway",new Vector3(DoorAt.x,0,DoorAt.y),"Look into the seed house","xylos.seedHouseSeen","","SelaSeedHouse","Sela",null,0,2.4f).requiredFlag="xylos.termsHeard";
            // After the vote Jask is gone and his pole isn't: against the stage post, a seed tag tied to it.
            var pole=kit.Discovery("Jask's pole",new Vector3(PoleAt.x,0,PoleAt.y),"Look at Jask's pole","jask.poleFound","JaskPole","SelaJaskPole","Sela","Questions",1,2.6f);pole.requiredFlag="xylos.voteHeard";
            var poleBody=kit.Built("Jask's pole body",new ZoneKit.Shape().Slot(0).Tube(new[]{kit.On(PoleAt.x+.3f,PoleAt.y+.2f),kit.On(PoleAt.x+.9f,PoleAt.y+.5f,3.6f)},new[]{.05f,.04f},5,new Color(.86f,.74f,.5f),new Color(.9f,.8f,.56f))
                .Slot(1).Box(kit.On(PoleAt.x+.62f,PoleAt.y+.36f,1.9f),new Vector3(.22f,.3f,.03f),Quaternion.Euler(0,30,8),new Color(1,.92f,.7f)),Plank,Cloth);
            pole.visual=poleBody;poleBody.SetActive(false);
            // Abri's call, on the way back to the pad once the vote is heard: nobody is standing there, and it ends the chapter.
            kit.Crossing("Keeper Abri calls about dinner",new Vector3(0,0,22),new Vector3(34,5,12),"chapter3.complete","AbriDinnerCall","Keeper","xylos.voteHeard");
            Hoard(kit,"xylos.walkway","Growers' cache",new Vector3(WalkCacheAt.x,0,WalkCacheAt.y),20,140,ProgressionBuilder.Drop("AmberSap",3,1),ProgressionBuilder.Drop("Rootfibre",3,1),ProgressionBuilder.AnyPart(3,GearSlot.Module,1));
            Hoard(kit,"xylos.sennIsland","Old mooring box",new Vector3(IslandCacheAt.x,0,IslandCacheAt.y),-60,80,ProgressionBuilder.Drop("Rootfibre",2,1),ProgressionBuilder.Drop("RepairGel",1,1));

            // ---- what grows and lies about
            var rng=new System.Random(47);
            kit.Scatter(rng,46,()=>new Vector2((float)rng.NextDouble()*120-58,(float)rng.NextDouble()*104+6),p=>{float d=LowLand(p);return d<-1.9f&&d>-5.2f&&FromWays(p)>3.2f;},RootKeepsOff,
                ("DressFanShrub",.9f,false),("DressSucculent",.7f,false),("DressSucculent",.9f,false),("DressFanShrub",1.1f,false),("DressSucculent",.6f,false),("DressFanShrub",.7f,false));
            Growth(kit);

            NothingStandsOnTheWays();
            kit.BakeNavigation(new Bounds(new Vector3(7,3,53),new Vector3(150,24,132)),RootArrival,new[]{PadAt+new Vector2(0,-3.4f),RuvaAtMooring+new Vector2(-1.6f,0),RopeAt+new Vector2(-1.4f,.4f),TeoAt+new Vector2(1.6f,0),
                JaskAt+new Vector2(1.6f,0),PoleAt+new Vector2(-1,-.8f),RuvaAtSeedHouse+new Vector2(0,-1.6f),NoticeAt+new Vector2(0,-1.2f),DoorAt+new Vector2(0,-1.2f),BinnaAt+new Vector2(-1.4f,-1.2f),HaddaAt+new Vector2(-1.6f,0),
                PressAt+new Vector2(-2,-1.6f),CradleAt+new Vector2(-2,.6f),GateAt+new Vector2(-3.4f,0),RiverSpawn,SennAt+new Vector2(1.6f,0),WalkCacheAt+new Vector2(0,-1.2f),IslandCacheAt+new Vector2(1.6f,.4f),new Vector2(0,44),new Vector2(-31.5f,22.4f)});
            Ground(kit,land);
            // Pollen in the light over the whole town, and midges low over the pool.
            var motes=kit.Glow("Pollen",false,new Color(1.5f,1.4f,.8f,1));
            kit.Drift("Pollen",new Vector3(4,3.4f,52),new Vector3(120,6,110),motes,new Color(1,.96f,.6f,.5f),520,.05f,.16f,.22f,14);
            var midges=kit.Drift("Midges over the pool",new Vector3(Pool.x,WaterLevel+1.1f,Pool.y),new Vector3(PoolA*1.7f,1.6f,PoolB*1.7f),motes,new Color(.9f,1,.8f,.7f),90,.03f,.07f,1.1f,5);
            var flit=midges.noise;flit.enabled=true;flit.strength=1.4f;flit.frequency=.9f;flit.scrollSpeed=.6f;
            NoBrokenMeshes(kit.zone);
            WorldBuilder.Save(scene,kit.zone);
            Debug.Log("XYLOS_COORDS Rootmarket arrival="+RootArrival+" pad="+PadAt+" ruva-mooring="+RuvaAtMooring+" rope="+RopeAt+" crossing=(14,24) teo="+TeoAt+" jask="+JaskAt+" pole="+PoleAt+" ruva-seedhouse="+RuvaAtSeedHouse
                +" notice="+NoticeAt+" door="+DoorAt+" binna="+BinnaAt+" hadda="+HaddaAt+" press="+PressAt+" cradle="+CradleAt+" gate="+GateAt+" river-spawn="+RiverSpawn+" senn="+SennAt+" walk-cache="+WalkCacheAt+" island-cache="+IslandCacheAt);
            Debug.Log("ROOTMARKET_OK");
        }
        /// <summary>Ruva's words by where the story is (manifest section 2): the terms the first time she can be spoken to, her repeat, the vote and the crews' thanks as beats, and after
        /// the vote her last word once, then its echo.</summary>
        static void Tiers(Npc ruva){ruva.repeatNode="RuvaRepeat";ruva.finalCompletionFlag="ruva.lineHeld";ruva.finalRepeatNode="RuvaAfterVoteRepeat";}
        static void Where(string name,GameObject who,string from,string awayFrom,string backAt,string goneAt,bool live)
        {
            var where=new GameObject(name,typeof(Whereabouts)).GetComponent<Whereabouts>();where.from=from??"";where.awayFrom=awayFrom??"";where.backAt=backAt??"";where.goneAt=goneAt??"";where.live=live;where.targets=new[]{who};
        }
        /// <summary>A box of something worth the walk: a lidded seed crate with a lantern on it, and the cache it is.</summary>
        static void Hoard(ZoneKit kit,string id,string label,Vector3 at,float yaw,int scrip,params LootEntry[] contents)
        {
            var foot=kit.On(at);var turn=Quaternion.Euler(0,yaw,0);
            var shape=new ZoneKit.Shape().Slot(0).Box(foot+Vector3.up*.34f,new Vector3(1.15f,.68f,.8f),turn,new Color(.82f,.68f,.46f),new Color(.95f,.82f,.56f)).Box(foot+Vector3.up*.74f,new Vector3(1.25f,.12f,.9f),turn,new Color(.7f,.56f,.36f),new Color(.9f,.76f,.5f))
                .Slot(1).Lump(foot+Vector3.up*1f,Vector3.one*.17f,5,.05f,Color.white,Color.white,7,4);
            var box=kit.Built("Cache "+label,shape,Plank,Lantern);ZoneKit.Block(box,foot+Vector3.up*.4f,new Vector3(1.15f,.8f,.8f),yaw);
            // The cache's own place is where the box stands: a prompt measures from there.
            var point=new GameObject(label);point.transform.position=foot;box.transform.SetParent(point.transform,true);
            LootCache.On(point,id,label,scrip,contents).range=2.6f;
        }

        // ---------------------------------------------------------------- the arrival scene
        /// <summary>The root island on its lines (Binna's spice garden, the one bush the storm left on it), a crew of three heaving, Ruva calling the pulls, and the rope a visitor is
        /// handed. The conversation starts on the way from the pad; the island seats on the third pull.</summary>
        static void Arrival(ZoneKit kit,Npc ruva,Npc binna)
        {
            // The island, built about its own middle so it can be hauled.
            var rng=new System.Random(63);var shape=new ZoneKit.Shape();
            shape.Slot(0).Lump(new Vector3(0,-.75f,0),new Vector3(5.2f,1.05f,4.6f),5,.12f,Color.white,new Color(.6f,.56f,.52f),16,6);
            for(int k=0;k<9;k++)
            {
                // Roots woven round its edge and trailing in the water.
                float a=k*Mathf.PI*2/9+.2f;var from=new Vector3(Mathf.Cos(a)*4.4f,.34f,Mathf.Sin(a)*3.9f);var to=new Vector3(Mathf.Cos(a+.6f)*5.4f,-1.2f,Mathf.Sin(a+.6f)*4.9f);
                shape.Tube(new[]{from,Vector3.Lerp(from,to,.5f)+Vector3.up*.3f,to},new[]{.3f,.24f,.1f},6,Color.white,new Color(.7f,.66f,.6f),1,.4f);
            }
            shape.Slot(1).Disc(new Vector3(0,.3f,0),Vector3.up,4.1f,18,new Color(.6f,.62f,.4f),0,new Color(.4f,.46f,.32f),.2f);
            // The bush: fronds, and the red pods that bite.
            shape.Slot(2).Fern(new Vector3(.6f,.3f,-.4f),1.5f,9,rng,new Color(.3f,.5f,.26f),new Color(.56f,.8f,.36f)).Fern(new Vector3(-1.8f,.3f,1.2f),.7f,6,rng,new Color(.3f,.5f,.26f),new Color(.5f,.74f,.36f));
            for(int k=0;k<8;k++)shape.Slot(3).Lump(new Vector3(.6f+Mathf.Cos(k*.8f)*.5f,.75f+.12f*(k%3),-.4f+Mathf.Sin(k*.8f)*.5f),new Vector3(.09f,.2f,.09f),k,.05f,new Color(.95f,.3f,.22f),new Color(.7f,.16f,.14f),6,4);
            // Cleats the lines are made fast to, on the side that faces the mooring.
            var cleatAt=new[]{new Vector3(-3.6f,.3f,2.2f),new Vector3(-4.1f,.3f,.6f),new Vector3(-2.6f,.3f,3.3f),new Vector3(-3.9f,.3f,-1)};
            foreach(var c in cleatAt)shape.Slot(0).Post(c,.6f,.14f,.11f,6,new Color(.8f,.74f,.66f),Color.white);
            var island=kit.Built("Binna's spice garden",shape.Slot(3),Bark,Loam,Leaf,Cloth);
            Vector3 moored=new(IslandMoored.x,WaterLevel+.5f,IslandMoored.y),adrift=new(IslandAdrift.x,WaterLevel+.5f,IslandAdrift.y);island.transform.position=moored;

            // The crew: three on lines, stepping back with each heave and forward for the next, facing what they haul, until she's seated.
            var hands=new List<Transform>();var cleats=new List<Transform>();var lines=new List<LineRenderer>();var ropes=new GameObject("Mooring lines");
            Transform Cleat(Vector3 local){var cleat=new GameObject("Line cleat").transform;cleat.SetParent(island.transform,false);cleat.localPosition=local+Vector3.up*.55f;return cleat;}
            LineRenderer Line(){var line=new GameObject("Mooring line",typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.SetParent(ropes.transform,false);line.useWorldSpace=true;line.positionCount=9;
                line.startWidth=line.endWidth=.09f;line.sharedMaterial=Rope;line.shadowCastingMode=ShadowCastingMode.Off;return line;}
            var toIsland=(IslandAdrift-RuvaAtMooring).normalized;int who=0;
            foreach(var (x,z,body,tint,look,build) in new[]{(29.6f,16.6f,"Hal",new Color(.7f,.8f,.5f),Look.Hat,1f),(27.2f,15.2f,"Orrin",new Color(.56f,.72f,.7f),Look.Bare,1.1f),(24.8f,16.6f,"Mira",new Color(.86f,.7f,.5f),Look.Coil,.9f)})
            {
                var stand=new Vector2(x,z);
                var hauler=kit.Walker(Townsbody(body),new[]{stand,stand-toIsland*1.1f},.9f,.9f,tint,body);hauler.thereAndBack=true;hauler.faces=island.transform;hauler.stopFlag="xylos.ropeHauled";hauler.name="A grower on the line";
                var grip=new GameObject("Line in hand").transform;grip.SetParent(hauler.transform,false);grip.localPosition=new Vector3(0,.95f,.35f);
                Dress(kit,hauler.gameObject,look,build);
                hands.Add(grip);cleats.Add(Cleat(cleatAt[who++]));lines.Add(Line());
            }
            // The line that's short a back: made fast to the mooring post, its tail lying where a visitor can take it up.
            var postFoot=kit.On(RopeAt.x+1.3f,RopeAt.y-.9f);
            var mooring=new GameObject("Mooring posts");Color post=new(.8f,.74f,.66f),hemp=new(.95f,.86f,.62f);
            void Coil(ZoneKit.Shape into,float cx,float cz,float r)
            {
                for(int turn=0;turn<3;turn++)
                {
                    var coil=new Vector3[13];var thick=new float[13];for(int k=0;k<13;k++){float a=k*Mathf.PI*2/12;coil[k]=kit.On(cx+Mathf.Cos(a)*(r-turn*.1f),cz+Mathf.Sin(a)*(r-turn*.1f),.06f+turn*.09f);thick[k]=.05f;}
                    into.Tube(coil,thick,5,hemp,hemp,1,1,false);
                }
            }
            // Each post and each coil its own small mesh: together they span the whole mooring, and the ground kit keeps its details off the box round a mesh.
            var first=new ZoneKit.Shape().Post(postFoot,1.5f,.3f,.26f,8,post,Color.white);Coil(first,RopeAt.x+.2f,RopeAt.y+.9f,.5f);
            kit.Built("Mooring post",first,Plank).transform.SetParent(mooring.transform,true);
            var second=new ZoneKit.Shape().Post(kit.On(RopeAt.x-2.4f,RopeAt.y-3.4f),1.3f,.26f,.22f,8,post,Color.white);Coil(second,RopeAt.x-2.9f,RopeAt.y-2.2f,.4f);
            kit.Built("Second mooring post",second,Plank).transform.SetParent(mooring.transform,true);
            var argued=new ZoneKit.Shape();Coil(argued,18.8f,30.7f,.45f);kit.Built("Coil argued over",argued,Plank).transform.SetParent(mooring.transform,true);
            ZoneKit.Block(mooring,postFoot+Vector3.up*.7f,new Vector3(.6f,1.4f,.6f));ZoneKit.Block(mooring,kit.On(RopeAt.x-2.4f,RopeAt.y-3.4f,.6f),new Vector3(.5f,1.2f,.5f));
            var made=new GameObject("Line made fast").transform;made.position=postFoot+Vector3.up*1.2f;
            hands.Add(made);cleats.Add(Cleat(cleatAt[3]));lines.Add(Line());
            kit.Sign("MOORING",RootSigns[5].at.x,RootSigns[5].at.y);

            for(int i=0;i<lines.Count;i++)for(int k=0;k<9;k++){float t=k/8f;lines[i].SetPosition(k,Vector3.Lerp(cleats[i].position,hands[i].position,t)+Vector3.down*.5f*4*t*(1-t)*Mathf.Min(1,Vector3.Distance(cleats[i].position,hands[i].position)*.2f));}
            var rope=new GameObject("The mooring line",typeof(MooringHaul)).GetComponent<MooringHaul>();rope.transform.position=kit.On(RopeAt.x,RopeAt.y);
            rope.prompt="Haul the mooring line";rope.range=2.8f;rope.island=island.transform;rope.adrift=adrift;rope.moored=moored;rope.lines=lines.ToArray();rope.cleats=cleats.ToArray();rope.hands=hands.ToArray();
            rope.held=new[]{ruva,binna};rope.holdNodes=new[]{"RuvaRopeHold","BinnaRopeHold"};
            // Across the whole width of the way from the pad to the mooring: the talk can't be walked round, and it is over before the rope is in reach.
            kit.Crossing("Ruva puts a visitor on the rope",new Vector3(14,0,24),new Vector3(3,5,22),"xylos.arrivalSeen","RuvaArrival","Ruva");
        }

        // ---------------------------------------------------------------- what the town is built of
        /// <summary>Decks, rails and posts: the raft crossing over the pool (the ferry's rafts lashed end to end since the storm took the upper walk's east foot), the rope walk out to
        /// Senn's island, the ferry stage, lantern posts and what is stacked about. Drawn as one mesh a kind; every deck stands on a box the bake reads, every rail on one a body stops at.</summary>
        static void Timber(ZoneKit kit)
        {
            var rng=new System.Random(29);Color wood=new(.86f,.72f,.5f),dark=new(.6f,.48f,.34f),rope=new(.95f,.86f,.62f);
            // ---- the raft crossing
            var rafts=new ZoneKit.Shape();const float RaftZ=44;
            for(int raft=0;raft<5;raft++)
            {
                float x0=-19.9f+raft*8f,x1=x0+7.7f;
                for(int log=0;log<4;log++)
                {
                    float z=RaftZ-1.5f+log;rafts.Slot(0).Tube(new[]{new Vector3(x0-.2f,-.34f,z),new Vector3(x1+.2f,-.34f,z)},new[]{.3f,.3f},7,Color.white,new Color(.86f,.84f,.8f),1,.3f);
                    // Lashed at each end and the middle: a band of line round every log.
                    foreach(float x in new[]{x0+.35f,(x0+x1)*.5f,x1-.35f})rafts.Slot(1).Tube(new[]{new Vector3(x-.07f,-.34f,z),new Vector3(x+.07f,-.34f,z)},new[]{.325f,.325f},7,rope,ZoneKit.Shape.Dim(rope,.85f),3,2,false);
                }
                Boards(rafts.Slot(1),new Vector3(x0,.02f,RaftZ),new Vector3(x1,.02f,RaftZ),3.3f,.09f,wood,rng);
                foreach(int side in new[]{-1,1})
                {
                    // The cross-pieces the deck is pinned to stand proud of it at both sides, and a pod float rides the water off each side, slung from them.
                    foreach(float x in new[]{x0+.35f,x1-.35f})rafts.Slot(1).Beam(new Vector3(x,-.06f,RaftZ+side*1.6f),new Vector3(x,-.06f,RaftZ+side*2.02f),.2f,.12f,dark,wood);
                    var buoy=new Vector3((x0+x1)*.5f+side*1.4f,WaterLevel+.04f,RaftZ+side*2.36f);
                    rafts.Slot(2).Lump(buoy,new Vector3(.3f,.26f,.46f),raft*2+side+2,.08f,new Color(.98f,.84f,.4f),new Color(.7f,.5f,.2f),8,5,.25f,Quaternion.Euler(0,90+raft*20,0));
                    rafts.Slot(1).Rope(buoy+Vector3.up*.2f,new Vector3(buoy.x,-.06f,RaftZ+side*1.9f),.08f,.02f,rope,4,4);
                }
            }
            for(float x=-19.4f;x<=19.5f;x+=4.85f)foreach(int side in new[]{-1,1})rafts.Slot(1).Post(new Vector3(x,-.2f,RaftZ+side*1.6f),1.3f,.09f,.07f,6,dark,wood);
            foreach(int side in new[]{-1,1})foreach(float high in new[]{1f,.6f})for(float x=-19.4f;x<19.4f;x+=4.85f)rafts.Slot(1).Rope(new Vector3(x,high,RaftZ+side*1.6f),new Vector3(x+4.85f,high,RaftZ+side*1.6f),.12f,.035f,rope);
            int stake=0;
            for(float x=-15.4f;x<=15.5f;x+=7.7f)
            {
                // Stakes driven into the pool's bed along the far side, the rafts made fast to them: taller than the rails, each with a strip of dyed cloth, every other one a seed
                // lantern. All on the north side, so none stands between the camera and anyone crossing.
                var foot=new Vector3(x,WaterLevel-1.2f,RaftZ+2.2f);float tall=3.5f+.3f*(stake%3);var head=foot+Vector3.up*tall;
                rafts.Slot(1).Tube(new[]{foot,foot+Vector3.up*tall*.5f+new Vector3(.05f,0,.04f),head},new[]{.12f,.1f,.08f},7,dark,wood,1,.5f);
                rafts.Slot(1).Rope(head+Vector3.down*.7f,new Vector3(x+1.6f,0,RaftZ+1.72f),.25f,.022f,rope,4,6).Rope(head+Vector3.down*.7f,new Vector3(x-1.6f,0,RaftZ+1.72f),.25f,.022f,rope,4,6);
                var dye=stake%3==0?new Color(.9f,.4f,.26f):stake%3==1?new Color(.96f,.82f,.4f):new Color(.4f,.7f,.66f);
                rafts.Slot(2).Triangle(head+Vector3.down*.1f,head+Vector3.down*.5f,head+new Vector3(.9f,-.34f,.1f),Vector3.back,dye);
                if(stake%2==0)rafts.Slot(3).Lump(head+Vector3.up*.2f,new Vector3(.15f,.2f,.15f),stake,.05f,Color.white,Color.white,8,5);
                stake++;
            }
            var crossing=kit.Built("Raft crossing",rafts.Slot(3),Bark,Plank,Cloth,Lantern);
            ZoneKit.Block(crossing,new Vector3(0,-.24f,RaftZ),new Vector3(40.8f,.5f,3.3f),0,"Raft deck");
            foreach(int side in new[]{-1,1})ZoneKit.BlockAlong(crossing,new Vector3(-19.5f,.55f,RaftZ+side*1.62f),new Vector3(19.5f,.55f,RaftZ+side*1.62f),.16f,1.1f,"Raft rail");

            // ---- the rope walk to Senn's island: boards on two slung lines
            var walk=new ZoneKit.Shape();Vector3 a=new(-23.4f,.02f,28.4f),b=new(-39.8f,.02f,17);var run=(b-a).normalized;var across=Vector3.Cross(Vector3.up,run);
            Boards(walk,a,b,2.2f,.08f,wood,rng,.5f,.07f);
            foreach(int side in new[]{-1,1})
            {
                walk.Post(a+across*side*1.16f+Vector3.down*.3f,1.5f,.1f,.08f,6,dark,wood).Post(b+across*side*1.16f+Vector3.down*.3f,1.5f,.1f,.08f,6,dark,wood);
                walk.Rope(a+across*side*1.16f+Vector3.up*1.1f,b+across*side*1.16f+Vector3.up*1.1f,.32f,.04f,rope).Rope(a+across*side*1.16f+Vector3.up*.55f,b+across*side*1.16f+Vector3.up*.55f,.26f,.035f,rope);
                walk.Rope(a+across*side*1.05f+Vector3.down*.08f,b+across*side*1.05f+Vector3.down*.08f,.02f,.06f,dark);
            }
            var ropeWalk=kit.Built("Rope walk",walk,Plank);
            ZoneKit.BlockAlong(ropeWalk,a-run*.5f+Vector3.down*.26f,b+run*.5f+Vector3.down*.26f,2.2f,.5f,"Rope walk deck");
            foreach(int side in new[]{-1,1})ZoneKit.BlockAlong(ropeWalk,a+across*side*1.16f+Vector3.up*.55f,b+across*side*1.16f+Vector3.up*.55f,.14f,1.1f,"Rope walk rail");
            kit.Sign("SENN'S ISLAND",RootSigns[6].at.x,RootSigns[6].at.y);
            // What's moored along the rope walk: poles with a line of floats slung between them, a canoe tied off at each. Open water either side of a walk is the emptiest frame in town.
            var moorings=new ZoneKit.Shape();var south=-across;
            for(int k=0;k<5;k++)
            {
                var foot=Vector3.Lerp(a,b,(k+.5f)/5)+south*(3.4f+.8f*(k%2))+Vector3.up*(WaterLevel-.6f);
                moorings.Slot(0).Post(foot,2.3f+.3f*(k%2),.11f,.09f,6,dark,wood);
                if(k>0){var before=Vector3.Lerp(a,b,(k-.5f)/5)+south*(3.4f+.8f*((k-1)%2))+Vector3.up*(WaterLevel+1.2f);moorings.Slot(0).Rope(before,foot+Vector3.up*1.8f,.5f,.035f,rope);
                    for(int f=1;f<4;f++)moorings.Slot(1).Lump(Vector3.Lerp(before,foot+Vector3.up*1.8f,f/4f)+Vector3.down*(.5f*4*(f/4f)*(1-f/4f)+.12f),new Vector3(.16f,.2f,.16f),k*4+f,.1f,new Color(.96f,.8f,.34f),new Color(.7f,.5f,.2f),6,4);}
            }
            foreach(var (along,off,yaw) in new[]{(.3f,5.6f,40f),(.72f,5.2f,-30f)})
            {
                var at=Vector3.Lerp(a,b,along)+south*off;at.y=WaterLevel+.05f;
                moorings.Slot(2).Lump(at,new Vector3(.62f,.42f,2.3f),(int)(along*10),.05f,Color.white,new Color(.6f,.54f,.46f),10,5,.25f,Quaternion.Euler(0,yaw,0));
            }
            kit.Built("Rope walk moorings",moorings.Slot(2),Plank,Cloth,Husk);

            // ---- the ferry stage: a head of boards at the crossing's west end, its post, a raft tied up beside it and a canoe
            var stagePosts=new ZoneKit.Shape().Post(kit.On(PoleAt.x+.4f,PoleAt.y+.6f),2.1f,.16f,.13f,7,dark,wood).Post(kit.On(-22.2f,41.4f),1.2f,.14f,.12f,7,dark,wood);
            kit.Built("Ferry stage posts",stagePosts,Plank);
            if(ZoneKit.Landed("SeedBarge"))
            {
                // The ferry itself, tied up at the stage: a seed pod of a boat like the barges overhead, a third their length, riding a hand deep.
                var boat=kit.Prop("SeedBarge",new Vector3(-14.5f,WaterLevel,37f),1.7f,98,false,false);boat.name="Ferry boat";
                var hull=ZoneKit.Drawn(boat);boat.transform.localScale*=6.4f/Mathf.Max(.1f,Mathf.Max(hull.size.x,hull.size.z));hull=ZoneKit.Drawn(boat);
                boat.transform.position+=new Vector3(-14.5f-hull.center.x,WaterLevel-.36f-hull.min.y,37f-hull.center.z);
                // Its stake stands in the pool beside it, so the line crosses water and nobody's path.
                var staked=new Vector3(-17.9f,WaterLevel-1.1f,39.4f);
                kit.Built("Ferry mooring",new ZoneKit.Shape().Tube(new[]{staked,staked+Vector3.up*2.5f},new[]{.11f,.08f},7,dark,wood).Rope(staked+Vector3.up*2.1f,new Vector3(-16.9f,WaterLevel+.75f,37.9f),.3f,.03f,rope),Plank);
            }
            else
            {
                var ferry=new ZoneKit.Shape();
                for(int log=0;log<4;log++)ferry.Slot(0).Tube(new[]{new Vector3(-17.6f,WaterLevel+.12f,36.2f+log*.8f),new Vector3(-11.4f,WaterLevel+.12f,35.4f+log*.8f)},new[]{.28f,.28f},7,Color.white,new Color(.86f,.84f,.8f),1,.3f);
                Boards(ferry.Slot(1),new Vector3(-17.3f,WaterLevel+.46f,37.4f),new Vector3(-11.7f,WaterLevel+.46f,36.6f),2.6f,.08f,wood,rng);
                ferry.Slot(1).Rope(new Vector3(-19.4f,WaterLevel+.9f,39.4f),new Vector3(-17.4f,WaterLevel+.5f,38.2f),.2f,.035f,rope);
                kit.Built("Ferry raft",ferry,Bark,Plank);
            }
            // A canoe: half a pod, as everything that floats here is.
            var canoe=new ZoneKit.Shape().Slot(0).Lump(new Vector3(-9.4f,WaterLevel+.05f,52.6f),new Vector3(.62f,.42f,2.3f),4,.05f,Color.white,new Color(.6f,.54f,.46f),10,5,.25f,Quaternion.Euler(0,64,0))
                .Slot(1).Box(new Vector3(-9.4f,WaterLevel+.34f,52.6f),new Vector3(.8f,.05f,.2f),Quaternion.Euler(0,64,0),wood);
            kit.Built("Canoe",canoe,Husk,Plank);
            kit.Sign("FERRY STAGE",RootSigns[1].at.x,RootSigns[1].at.y);

            // ---- seed lanterns on posts, where people stand of an evening, beside the ways and never on them (four stood within half a metre of a way's line and cut the walk along
            // it; the one south of the kitchen stood a metre off the vote route's walk past Binna and cut that). A few carry a real light; the rest get a painted pool from the ground kit.
            var lanterns=new ZoneKit.Shape();var solid=new GameObject("Lantern posts");int lit=0;
            foreach(var (x,z) in new[]{(4.6f,27.2f),(-5.2f,14.6f),(16.4f,28.6f),(-21.4f,50.6f),(-6.4f,64.2f),(10.6f,66.6f),(16.4f,60.8f),(31.2f,50.2f),(30.6f,36.4f),(46.6f,45),(-41f,12.6f),(27.8f,28.6f)})
            {
                var foot=kit.On(x,z);
                lanterns.Slot(0).Tube(new[]{foot,foot+Vector3.up*1.9f,foot+new Vector3(.34f,2.25f,0)},new[]{.09f,.07f,.05f},6,dark,wood).Slot(1).Lump(foot+new Vector3(.34f,1.98f,0),new Vector3(.2f,.27f,.2f),7,.06f,Color.white,Color.white,8,5);
                ZoneKit.Block(solid,foot+Vector3.up*.9f,new Vector3(.3f,1.8f,.3f),0,"Lantern post");
                if(lit++%2==0)kit.Lamp(new Vector3(x+.34f,1.9f,z),6.5f,.9f,new Color(1,.8f,.5f));
            }
            kit.Built("Seed lanterns",lanterns,Plank,Lantern).transform.SetParent(solid.transform,true);

            // ---- what's stacked about: trays of seedlings by the seed house and the landing, baskets, crates of Teo's
            var stacks=new ZoneKit.Shape();var stackBlocks=new GameObject("Stacks");
            void Trays(float x,float z,float yaw,int tall)
            {
                var turn=Quaternion.Euler(0,yaw,0);var foot=kit.On(x,z);
                for(int k=0;k<tall;k++)
                {
                    stacks.Slot(0).Box(foot+Vector3.up*(.13f+k*.3f),new Vector3(1.3f,.22f,.9f),turn*Quaternion.Euler(0,k*7-6,0),dark,new Color(.5f,.42f,.3f));
                    if(k==tall-1)for(int s=0;s<6;s++)stacks.Slot(1).Lump(foot+turn*new Vector3(-.4f+s%3*.4f,.3f+k*.3f,-.2f+s/3*.4f),new Vector3(.12f,.16f,.12f),s,.1f,new Color(.6f,.86f,.4f),new Color(.36f,.56f,.28f),6,4);
                }
                ZoneKit.Block(stackBlocks,foot+Vector3.up*(tall*.15f),new Vector3(1.3f,tall*.3f,.9f),yaw,"Tray stack");
            }
            void Basket(float x,float z,float size,Color pods)
            {
                var foot=kit.On(x,z);stacks.Slot(0).Tube(new[]{foot,foot+Vector3.up*size*.7f},new[]{size*.42f,size*.5f},9,new Color(.8f,.68f,.44f),new Color(.95f,.82f,.56f),2,1,false);
                stacks.Slot(1).Lump(foot+Vector3.up*size*.66f,new Vector3(size*.44f,size*.2f,size*.44f),(int)(x*7+z),.25f,pods,ZoneKit.Shape.Dim(pods,.6f),8,4);
            }
            Trays(-9.8f,26.8f,20,3);Trays(-11.2f,28.2f,-30,2);Trays(-10.6f,22.6f,70,1);Trays(-6.6f,71.2f,10,3);Trays(-9.6f,71.8f,-20,2);Trays(9.6f,71.4f,30,3);Trays(11.6f,70.4f,80,2);Trays(31.5f,26.5f,40,2);Trays(40.6f,36.2f,-10,2);
            Basket(-7.2f,27.6f,.7f,new Color(1,.92f,.74f));Basket(8.4f,69.4f,.8f,new Color(1,.92f,.74f));Basket(-12.6f,68.4f,.7f,new Color(.95f,.8f,.4f));Basket(30.6f,25.4f,.7f,new Color(.9f,.34f,.24f));Basket(-44.2f,16.8f,.6f,new Color(.95f,.8f,.4f));
            kit.Built("Trays and baskets",stacks,Plank,Cloth).transform.SetParent(stackBlocks.transform,true);

            // Senn's own place: a hut at the back of his island (the growers' round hall at a hut's size, greyer for its years), a seat, and the post he carved and never carried out.
            var hutFoot=kit.On(-49.4f,16.2f);GameObject sennHut;
            if(ZoneKit.Landed("SeedHouse"))
            {
                // Its door (+z on the model) turned to where he sits.
                sennHut=kit.Prop("SeedHouse",new Vector3(-49.4f,0,16.2f),3.5f,130,false);sennHut.name="Hut on the island";sennHut.AddComponent<PropTint>().tint=new Color(.84f,.82f,.76f);
            }
            else
            {
                var hut=new ZoneKit.Shape();
                hut.Slot(0).Tube(new[]{hutFoot,hutFoot+Vector3.up*1.5f},new[]{1.7f,1.55f},10,new Color(.8f,.7f,.52f),new Color(.9f,.8f,.6f),3,.5f,false).Slot(1).Tube(new[]{hutFoot+Vector3.up*1.35f,hutFoot+Vector3.up*3.1f},new[]{2.3f,.1f},12,new Color(.62f,.74f,.42f),new Color(.8f,.82f,.5f),4,.6f,false);
                sennHut=kit.Built("Hut on the island",hut,Weave,Thatch);
            }
            ZoneKit.Block(sennHut,hutFoot+Vector3.up*.8f,new Vector3(2.9f,1.6f,2.9f),0,"Hut");
            kit.Built("Seat on the island",new ZoneKit.Shape().Box(kit.On(SennAt.x-1.2f,SennAt.y-.2f,.24f),new Vector3(.9f,.48f,.5f),Quaternion.Euler(0,70,0),dark,wood),Plank);
            var carved=new ZoneKit.Shape().Tube(new[]{kit.On(-44.6f,10.6f),kit.On(-44.6f,10.6f,1.7f)},new[]{.17f,.13f},7,new Color(.9f,.84f,.7f),Color.white,1,.5f).Lump(kit.On(-44.6f,10.6f,1.78f),new Vector3(.2f,.14f,.2f),2,.1f,Color.white,new Color(.8f,.74f,.6f),7,4);
            ZoneKit.Block(kit.Built("Carved mooring post",carved,Plank),kit.On(-44.6f,10.6f,.85f),new Vector3(.4f,1.7f,.4f),0,"Carved post");
        }
        /// <summary>A root arch on its feet: two buttress roots that rise and fuse overhead, tall enough to walk under. The fade is told where the way through is. Stands in for the
        /// generated RootArch until it is taken in, and takes its name and collision from here either way.</summary>
        static GameObject RootArch(ZoneKit kit,string name,Vector3 at,float tall,float yaw)
        {
            if(ZoneKit.Landed("RootArch"))
            {
                // The generated arch: wider than it is tall, its way through along its own z. Its feet are where its own box says, not where the Ancelane arch's would be.
                var made=kit.Prop("RootArch",at,tall,yaw,false);made.name=name;var across=Quaternion.Euler(0,yaw,0)*Vector3.right;
                var box=ZoneKit.Drawn(made);float span=Mathf.Abs(Vector3.Dot(box.size,new Vector3(Mathf.Abs(across.x),0,Mathf.Abs(across.z))))*.5f;
                var through=made.GetComponent<SceneryOcclusion>();if(through!=null)through.opening=new Bounds(new Vector3(0,-tall*.12f,0),new Vector3(span*1.3f,tall*.72f,3));
                foreach(int side in new[]{-1,1})ZoneKit.Block(made,kit.On(at)+across*side*(span-.9f)+Vector3.up*1.2f,new Vector3(1.6f,2.4f,1.8f),yaw,"Arch foot");
                return made;
            }
            var foot=kit.On(at);var turn=Quaternion.Euler(0,yaw,0);var shape=new ZoneKit.Shape();float half=tall*.38f;
            foreach(int side in new[]{-1,1})
            {
                var path=new List<Vector3>();var thick=new List<float>();
                for(int k=0;k<=8;k++){float t=k/8f,a=t*Mathf.PI*.5f;path.Add(foot+turn*new Vector3(side*half*(1.25f-.25f*t)*Mathf.Cos(a)+side*half*.05f,tall*Mathf.Sin(a)*(.92f+.08f*t),side*.35f*Mathf.Sin(t*3)));thick.Add(Mathf.Lerp(.95f,.5f,t));}
                shape.Slot(0).Tube(path,thick,8,new Color(.8f,.76f,.7f),Color.white,1,.3f);
                // A splayed foot and a second, thinner root beside it.
                shape.Tube(new[]{foot+turn*new Vector3(side*half*2f,-.2f,.9f),foot+turn*new Vector3(side*half*1.5f,.7f,.4f),foot+turn*new Vector3(side*half*1.2f,2f,.1f)},new[]{.2f,.4f,.3f},6,new Color(.74f,.7f,.64f),Color.white,1,.3f);
            }
            for(int k=0;k<4;k++)shape.Slot(1).Frond(foot+turn*new Vector3((k-1.5f)*.7f,tall*.94f,0),turn*(k%2==0?Vector3.forward:Vector3.back),1.6f,.34f,-.6f,.5f,new Color(.3f,.5f,.26f),new Color(.56f,.8f,.36f));
            var arch=kit.Built(name,shape.Slot(1),Bark,Leaf);
            ZoneKit.Fadeable(arch);var fade=arch.GetComponent<SceneryOcclusion>();if(fade!=null)fade.opening=new Bounds(foot+Vector3.up*tall*.4f,turn*new Vector3(half*1.5f,tall*.8f,2.4f));
            foreach(int side in new[]{-1,1})ZoneKit.Block(arch,foot+turn*new Vector3(side*half*1.3f,1.2f,0),new Vector3(1.5f,2.4f,1.6f),yaw,"Arch foot");
            return arch;
        }
        /// <summary>The seed house, at the back where the tallest thing belongs: a round hall woven of root, wider at its foot, under a steep thatch, its door to the pool with the Compact's
        /// notice nailed to one leaf and a seed lantern over it. The generated SeedHouse takes its place when taken in.</summary>
        static void SeedHouse(ZoneKit kit)
        {
            var foot=kit.On(SeedHouseAt.x,SeedHouseAt.y);GameObject house;
            bool generated=ZoneKit.Landed("SeedHouse");float wide=8.6f,front=foot.z-4.75f;
            if(generated)
            {
                // The generated hall, its door (+z on the model) turned to the pool, stood so its front is where the two things to read at its door expect it.
                house=kit.Prop("SeedHouse",new Vector3(SeedHouseAt.x,0,SeedHouseAt.y),10.8f,180,false);var box=ZoneKit.Drawn(house);
                house.transform.position+=Vector3.forward*(front-box.min.z);box=ZoneKit.Drawn(house);wide=Mathf.Min(box.size.x,box.size.z)*.86f;foot=new Vector3(box.center.x,foot.y,box.center.z);
            }
            else
            {
                var shape=new ZoneKit.Shape();
                shape.Slot(0).Tube(new[]{foot+Vector3.down*.2f,foot+Vector3.up*1.4f,foot+Vector3.up*4.2f,foot+Vector3.up*6.2f},new[]{5.3f,4.7f,4.2f,3.9f},20,new Color(.8f,.76f,.7f),Color.white,6,.22f,false);
                // The thatch: a wide eave, a steep cone, and a second skirt under it so the eave has thickness.
                shape.Slot(1).Tube(new[]{foot+Vector3.up*5.5f,foot+Vector3.up*7.4f,foot+Vector3.up*10.6f},new[]{5.6f,3.3f,.12f},22,new Color(.56f,.72f,.4f),new Color(.86f,.86f,.5f),7,.5f,false)
                    .Tube(new[]{foot+Vector3.up*5.9f,foot+Vector3.up*5.4f},new[]{4,5.55f},22,new Color(.3f,.4f,.26f),new Color(.4f,.52f,.3f),7,.5f,false);
                // The doorway, dark inside, with its two leaves stood open and the root beam the lantern hangs from.
                shape.Slot(2).Box(foot+new Vector3(0,1.5f,-4.72f),new Vector3(2.4f,3f,.5f),Quaternion.identity,new Color(.1f,.09f,.08f));
                foreach(int side in new[]{-1,1})shape.Slot(3).Box(foot+new Vector3(side*1.75f,1.45f,-5.25f),new Vector3(1.15f,2.9f,.1f),Quaternion.Euler(0,side*-38,0),new Color(.8f,.66f,.46f),null,.6f);
                shape.Slot(0).Tube(new[]{foot+new Vector3(-1.7f,0,-4.9f),foot+new Vector3(-1.6f,3.2f,-5),foot+new Vector3(0,3.75f,-5.5f),foot+new Vector3(1.6f,3.2f,-5),foot+new Vector3(1.7f,0,-4.9f)},new[]{.3f,.26f,.24f,.26f,.3f},7,new Color(.7f,.64f,.56f),new Color(.86f,.8f,.72f),1,.4f);
                foreach(int side in new[]{-1,1})shape.Slot(2).Disc(foot+Quaternion.Euler(0,side*52,0)*new Vector3(0,3.6f,-4.42f),Quaternion.Euler(0,side*52,0)*Vector3.back,.5f,12,new Color(.12f,.1f,.09f));
                house=kit.Built("Seed house",shape.Slot(3),Weave,Thatch,Cloth,Plank);
            }
            house.name="Seed house";ZoneKit.Fadeable(house);
            // Round, near enough: three boxes turned on each other.
            ZoneKit.Block(house,foot+Vector3.up*3,new Vector3(wide,6,wide*.74f),0,"Seed house wall");ZoneKit.Block(house,foot+Vector3.up*3,new Vector3(wide*.74f,6,wide),0,"Seed house wall");ZoneKit.Block(house,foot+Vector3.up*3,new Vector3(wide*.88f,6,wide*.88f),45,"Seed house wall");
            foot=kit.On(SeedHouseAt.x,SeedHouseAt.y);
            // The seed lantern over the door, and the notice on the west leaf with the ferry times nailed across its bottom half.
            var extras=new ZoneKit.Shape().Slot(0).Lump(foot+new Vector3(0,3.05f,-5.6f),new Vector3(.34f,.44f,.34f),3,.06f,Color.white,Color.white,9,6)
                .Slot(1).Box(foot+new Vector3(-2.1f,1.75f,-5.56f),new Vector3(.7f,.9f,.04f),Quaternion.Euler(0,38,0),new Color(.93f,.92f,.86f)).Box(foot+new Vector3(-2.06f,1.5f,-5.6f),new Vector3(.5f,.34f,.04f),Quaternion.Euler(0,38,6),new Color(.95f,.84f,.56f))
                .Slot(2).Tube(new[]{foot+new Vector3(0,3.7f,-5.5f),foot+new Vector3(0,3.4f,-5.6f)},new[]{.03f,.03f},4,Color.white,Color.white);
            if(!generated)kit.Built("Seed house door fittings",extras.Slot(2),Lantern,Cloth,Plank);
            if(generated)
            {
                // The model has its own door and lantern: what's left to add is the notice, on a board stood against the wall beside the door, where its prompt is.
                var boardAt=kit.On(NoticeAt.x,front+.15f);
                var board=new ZoneKit.Shape().Slot(0).Post(boardAt,1.9f,.07f,.06f,6,new Color(.6f,.48f,.34f),new Color(.86f,.72f,.5f)).Box(boardAt+Vector3.up*1.5f+Vector3.back*.06f,new Vector3(.95f,1.05f,.06f),Quaternion.identity,new Color(.7f,.56f,.38f))
                    .Slot(1).Box(boardAt+new Vector3(0,1.58f,-.1f),new Vector3(.7f,.8f,.03f),Quaternion.Euler(0,0,2),new Color(.93f,.92f,.86f)).Box(boardAt+new Vector3(.04f,1.3f,-.12f),new Vector3(.5f,.32f,.03f),Quaternion.Euler(0,0,-5),new Color(.95f,.84f,.56f));
                kit.Built("Compact notice board",board,Plank,Cloth);
            }
            kit.Lamp(new Vector3(SeedHouseAt.x,2.7f,SeedHouseAt.y-6.2f),9,1.5f,new Color(1,.8f,.48f));
            kit.Sign("SEED HOUSE",RootSigns[2].at.x,RootSigns[2].at.y);
        }
        /// <summary>A stall under a leaf awning: a counter of boards on posts, four poles, and a roof of big leaves pitched to the back. The generated MarketAwning takes its place when taken in.
        /// Each piece of the kitchen is its own small mesh: the ground kit keeps its details off the box round whatever stands, and one mesh for the whole kitchen left a bare rectangle the size of it.</summary>
        static void Stall(ZoneKit kit,string name,GameObject blocks,Vector3 at,float yaw)
        {
            if(ZoneKit.Landed("MarketAwning")){var made=kit.Prop("MarketAwning",at,2.9f,yaw);made.name=name;made.transform.SetParent(blocks.transform,true);return;}
            var shape=new ZoneKit.Shape();var foot=kit.On(at);var turn=Quaternion.Euler(0,yaw,0);Color wood=new(.86f,.72f,.5f),dark=new(.6f,.48f,.34f);
            shape.Slot(0).Box(foot+turn*new Vector3(0,.5f,0),new Vector3(3.2f,.9f,1),turn,dark,wood).Box(foot+turn*new Vector3(0,.99f,0),new Vector3(3.5f,.08f,1.25f),turn,wood);
            foreach(var (x,z,tall) in new[]{(-1.7f,-.75f,2.2f),(1.7f,-.75f,2.2f),(-1.7f,1.1f,2.9f),(1.7f,1.1f,2.9f)})shape.Slot(0).Post(foot+turn*new Vector3(x,0,z),tall,.08f,.07f,6,dark,wood);
            // The awning: five great leaves side by side, each from the high back pole forward and down over the counter.
            for(int k=0;k<5;k++)shape.Slot(1).Frond(foot+turn*new Vector3(-1.7f+k*.85f,2.95f,1.25f),turn*Vector3.back,2.9f,1.15f,-.45f,.35f,new Color(.34f,.52f,.28f),new Color(.62f+.1f*(k%2),.82f,.36f),5);
            // On the counter: bowls and pods.
            for(int k=0;k<4;k++)shape.Slot(2).Lump(foot+turn*new Vector3(-1.2f+k*.8f,1.14f,.05f*(k%2)),new Vector3(.2f,.13f,.2f),k+(int)at.x,.2f,k%2==0?new Color(.92f,.32f,.22f):new Color(.96f,.8f,.34f),new Color(.5f,.3f,.2f),7,4);
            kit.Built(name,shape,Plank,Leaf,Cloth).transform.SetParent(blocks.transform,true);
            ZoneKit.Block(blocks,foot+turn*new Vector3(0,.55f,0),new Vector3(3.4f,1.1f,1.2f),yaw,"Stall counter");
        }
        /// <summary>Binna's kitchen, north-east: her stall and a second counter under their awnings, a cooking pit with pots on it, and tables anyone may sit at.</summary>
        static void Kitchen(ZoneKit kit)
        {
            var blocks=new GameObject("Kitchen");Color wood=new(.86f,.72f,.5f),dark=new(.6f,.48f,.34f);
            Stall(kit,"Kitchen stall",blocks,new Vector3(27.6f,0,71.4f),180);Stall(kit,"Kitchen counter",blocks,new Vector3(34.4f,0,64.2f),250);
            // The cooking pit: a ring of stones, embers, two pots on a pole.
            var pit=kit.On(21.6f,71.6f);var fire=new ZoneKit.Shape();
            for(int k=0;k<9;k++)fire.Slot(1).Lump(pit+new Vector3(Mathf.Cos(k*.7f)*.9f,.1f,Mathf.Sin(k*.7f)*.9f),new Vector3(.26f,.2f,.24f),k,.3f,new Color(.42f,.42f,.4f),new Color(.24f,.24f,.25f),6,4);
            fire.Slot(1).Disc(pit+Vector3.up*.05f,Vector3.up,.72f,10,new Color(.14f,.12f,.11f));
            for(int k=0;k<5;k++)fire.Slot(2).Lump(pit+new Vector3(Mathf.Cos(k*1.3f)*.3f,.1f,Mathf.Sin(k*1.3f)*.3f),new Vector3(.12f,.07f,.1f),k,.3f,Color.white,Color.white,5,3);
            fire.Slot(0).Post(pit+new Vector3(-1.1f,0,0),1.5f,.06f,.05f,5,dark,wood).Post(pit+new Vector3(1.1f,0,0),1.5f,.06f,.05f,5,dark,wood).Beam(pit+new Vector3(-1.2f,1.45f,0),pit+new Vector3(1.2f,1.45f,0),.07f,.07f,dark);
            foreach(float x in new[]{-.4f,.45f})fire.Slot(1).Lump(pit+new Vector3(x,.95f,0),new Vector3(.36f,.3f,.36f),7,.04f,new Color(.3f,.3f,.33f),new Color(.16f,.16f,.18f),9,5);
            kit.Built("Cooking pit",fire,Plank,Cloth,Lantern).transform.SetParent(blocks.transform,true);
            ZoneKit.Block(blocks,pit+Vector3.up*.5f,new Vector3(2.6f,1,1.9f),0,"Cooking pit");
            kit.Lamp(new Vector3(21.6f,.7f,71.6f),4,.8f,new Color(1,.6f,.3f));
            // Tables: a round of boards on a stump, stools round it, a bowl or two left out.
            int number=0;
            // The first stood across the way from the seed house to Binna's counter; it is south of it now, by the ring's edge.
            foreach(var (x,z,r) in new[]{(20.2f,60.2f,1.05f),(27.8f,61.6f,1.2f),(30.8f,58.6f,.95f),(18.6f,67.6f,.9f)})
            {
                var t=kit.On(x,z);var table=new ZoneKit.Shape();
                table.Slot(0).Post(t,.7f,.3f,.24f,8,dark,wood).Disc(t+Vector3.up*.74f,Vector3.up,r,14,wood,0,new Color(.74f,.6f,.4f),.5f).Tube(new[]{t+Vector3.up*.66f,t+Vector3.up*.74f},new[]{r,r},14,dark,dark,3,1,false);
                for(int k=0;k<4;k++){float a=k*Mathf.PI*.5f+x;table.Slot(0).Post(t+new Vector3(Mathf.Cos(a)*(r+.55f),0,Mathf.Sin(a)*(r+.55f)),.42f,.2f,.18f,7,dark,wood);}
                table.Slot(1).Lump(t+new Vector3(.2f,.82f,-.1f),new Vector3(.18f,.09f,.18f),(int)x,.1f,new Color(.95f,.9f,.78f),new Color(.6f,.5f,.4f),7,4).Lump(t+new Vector3(-.3f,.82f,.25f),new Vector3(.14f,.08f,.14f),(int)z,.1f,new Color(.9f,.4f,.26f),new Color(.6f,.3f,.2f),7,4);
                kit.Built("Kitchen table "+(++number),table,Plank,Cloth).transform.SetParent(blocks.transform,true);
                ZoneKit.Block(blocks,t+Vector3.up*.4f,new Vector3(r*1.6f,.8f,r*1.6f),0,"Kitchen table");
            }
            kit.Sign("BINNA'S KITCHEN",RootSigns[3].at.x,RootSigns[3].at.y);
        }
        /// <summary>The mender's press, east: a screw press taller than a door (the fabrication bench), the cradle a Shaper is mended in (repair and save), and Hadda's counter.
        /// Both are built here, so they are built as carpentry: whole logs, posts with carved heads, joints bound with line, a thread on the screw, a sling that is woven.</summary>
        static void Press(ZoneKit kit)
        {
            var shape=new ZoneKit.Shape();Color wood=new(.86f,.72f,.5f),dark=new(.6f,.48f,.34f),hemp=new(.95f,.86f,.62f),iron=new(.5f,.5f,.54f);
            var foot=kit.On(PressAt.x,PressAt.y);var turn=Quaternion.Euler(0,-35,0);
            Vector3 P(float x,float y,float z)=>foot+turn*new Vector3(x,y,z);
            // The bed: two sills of whole log under a slab, the pulp on it and the plate that comes down on the pulp.
            foreach(int side in new[]{-1,1})shape.Slot(0).Tube(new[]{P(-1.45f,.22f,side*.52f),P(1.45f,.22f,side*.52f)},new[]{.24f,.24f},8,dark,ZoneKit.Shape.Dim(dark,.85f),2,.4f);
            shape.Slot(0).Box(P(0,.52f,0),new Vector3(2.5f,.2f,1.4f),turn,dark,wood).Box(P(0,.68f,0),new Vector3(1.75f,.14f,1.1f),turn,ZoneKit.Shape.Dim(wood,.8f),wood);
            shape.Slot(1).Box(P(0,.84f,0),new Vector3(1.4f,.18f,.85f),turn,new Color(.82f,.8f,.56f),new Color(.9f,.88f,.62f));
            shape.Slot(0).Box(P(0,1.02f,0),new Vector3(1.55f,.16f,.98f),turn,dark,wood);
            foreach(int side in new[]{-1,1})
            {
                // The frame: two posts as thick as a thigh with carved heads, a knee under the head beam, and every joint bound with line.
                shape.Slot(0).Post(P(side*1.05f,.5f,0),2.75f,.19f,.16f,8,dark,wood).Lump(P(side*1.05f,3.34f,0),new Vector3(.2f,.16f,.2f),side+5,.12f,wood,dark,8,5);
                shape.Slot(0).Beam(P(side*1.0f,2.2f,0),P(side*.42f,2.86f,0),.12f,.12f,dark,wood).Box(P(side*1.54f,3f,0),new Vector3(.1f,.46f,.5f),turn,ZoneKit.Shape.Dim(dark,.8f));
                foreach(float high in new[]{.72f,2.74f,3.2f})shape.Slot(0).Tube(new[]{P(side*1.05f,high,0),P(side*1.05f,high+.1f,0)},new[]{.215f,.215f},8,hemp,ZoneKit.Shape.Dim(hemp,.85f),3,2,false);
            }
            shape.Slot(0).Beam(P(-1.5f,3f,0),P(1.5f,3f,0),.42f,.38f,dark,wood);
            // The screw: a shaft down through the beam with its thread standing proud, a collar, and the bar that turns it.
            shape.Slot(0).Post(P(0,1.1f,0),2.5f,.09f,.09f,8,iron,new Color(.7f,.7f,.74f),2f);
            var thread=new List<Vector3>();for(int k=0;k<=56;k++){float a=k*.7854f;thread.Add(P(Mathf.Cos(a)*.11f,1.2f+k*.03f,Mathf.Sin(a)*.11f));}
            shape.Slot(0).Tube(thread,new[]{.028f},4,new Color(.74f,.74f,.78f),new Color(.6f,.6f,.64f),1,2,false);
            shape.Slot(0).Tube(new[]{P(-.95f,3.52f,.2f),P(.95f,3.52f,-.2f)},new[]{.05f,.05f},6,wood,wood).Lump(P(-.95f,3.52f,.2f),Vector3.one*.09f,1,.1f,wood,dark,6,4).Lump(P(.95f,3.52f,-.2f),Vector3.one*.09f,2,.1f,wood,dark,6,4)
                .Tube(new[]{P(0,3.4f,0),P(0,3.62f,0)},new[]{.13f,.11f},8,iron,iron);
            // What runs out of the pulp: a spout at the front corner and a tub under it.
            shape.Slot(0).Box(P(-.6f,.6f,-.78f),new Vector3(.16f,.06f,.4f),turn*Quaternion.Euler(18,0,0),dark,wood).Tube(new[]{P(-.6f,0,-1.1f),P(-.6f,.34f,-1.1f)},new[]{.2f,.25f},9,dark,wood,2,1,false);
            shape.Slot(1).Disc(P(-.6f,.28f,-1.1f),Vector3.up,.23f,9,new Color(.7f,.74f,.46f));
            // A seed lantern hung from the beam's end.
            shape.Slot(0).Tube(new[]{P(-1.42f,2.8f,-.2f),P(-1.42f,2.46f,-.2f)},new[]{.012f,.012f},4,hemp,hemp,1,2,false).Slot(2).Lump(P(-1.42f,2.32f,-.2f),new Vector3(.14f,.19f,.14f),3,.05f,Color.white,Color.white,8,5);
            // Pressed sheets drying on a rack behind it: two posts, three rails, the sheets hung over the rails.
            foreach(int side in new[]{-1,1})shape.Slot(0).Post(P(2.4f+side*.66f,0,.3f),2.5f,.06f,.05f,6,dark,wood);
            for(int k=0;k<3;k++)
            {
                float high=1f+k*.62f;shape.Slot(0).Beam(P(1.7f,high,.3f),P(3.1f,high,.3f),.05f,.05f,wood);
                for(int sheet=0;sheet<2;sheet++)shape.Slot(1).Box(P(2.05f+sheet*.68f+.08f*(k%2),high-.2f,.3f),new Vector3(.52f,.44f,.03f),turn*Quaternion.Euler(0,0,(k+sheet)%2==0?3:-4),ZoneKit.Shape.Dim(new Color(.92f,.88f,.62f),.9f+.1f*sheet));
            }
            var press=kit.Built("The press",shape.Slot(2),Plank,Cloth,Lantern);ZoneKit.Fadeable(press);
            ZoneKit.Block(press,foot+Vector3.up*1.2f,new Vector3(2.8f,2.4f,1.6f),-35,"Press bed");ZoneKit.Block(press,foot+turn*new Vector3(2.4f,1.1f,.3f)+Vector3.zero,new Vector3(1.5f,2.2f,1.1f),-35,"Drying rack");
            var bench=new GameObject("The press bench",typeof(Bench)).GetComponent<Bench>();bench.transform.position=foot+turn*new Vector3(0,0,-1.3f);bench.prompt="Fabricate — The Press";bench.range=2.6f;bench.transform.SetParent(press.transform,true);
            // Hadda's counter, behind where she stands: a stall like the kitchen's, facing the pool. Until the stall is taken in she has the press and the cradle to stand by.
            if(ZoneKit.Landed("MarketAwning"))kit.Prop("MarketAwning",new Vector3(CounterAt.x,0,CounterAt.y),2.9f,205).name="Hadda's counter";

            // The cradle: an A-frame of bound poles with a woven sling, where a Shaper lies to be mended; a mat under it, a capped lantern over it, the mender's gourds hung at its end.
            var cradleAt=kit.On(CradleAt.x,CradleAt.y);var frame=new ZoneKit.Shape();var across=Quaternion.Euler(0,30,0);
            Vector3 C(float x,float y,float z)=>cradleAt+across*new Vector3(x,y,z);
            frame.Slot(3).Disc(C(0,.035f,0),Vector3.up,1.75f,18,new Color(.9f,.84f,.62f),0,new Color(.66f,.58f,.4f),.9f);
            foreach(int end in new[]{-1,1})
            {
                foreach(int leg in new[]{-1,1})
                {
                    frame.Slot(0).Tube(new[]{C(end*1.36f,0,leg*.74f),C(end*1.27f,1.1f,leg*.34f),C(end*1.2f,2.16f,-leg*.07f)},new[]{.1f,.085f,.065f},7,dark,wood,1,.6f);
                    // The sling's lines, from its spreader up to where the legs cross.
                    frame.Slot(0).Tube(new[]{C(end*1.02f,.86f,leg*.44f),C(end*1.14f,2f,0)},new[]{.014f,.014f},4,hemp,hemp,1,2,false);
                }
                frame.Slot(0).Tube(new[]{C(end*1.2f,1.96f,0),C(end*1.2f,2.1f,0)},new[]{.13f,.13f},7,hemp,ZoneKit.Shape.Dim(hemp,.85f),3,2,false).Beam(C(end*1.31f,.5f,-.56f),C(end*1.31f,.5f,.56f),.07f,.07f,dark,wood)
                    .Tube(new[]{C(end*1.02f,.86f,-.46f),C(end*1.02f,.86f,.46f)},new[]{.035f,.035f},5,wood,wood);
            }
            frame.Slot(0).Tube(new[]{C(-1.5f,2.04f,0),C(1.5f,2.04f,0)},new[]{.07f,.07f},7,wood,dark,1,.6f).Lump(C(-1.5f,2.04f,0),Vector3.one*.1f,1,.1f,wood,dark,6,4).Lump(C(1.5f,2.04f,0),Vector3.one*.1f,2,.1f,wood,dark,6,4);
            for(int k=0;k<8;k++)
            {
                // The sling: woven, sagging between its spreaders.
                float t0=k/8f,t1=(k+1)/8f,y0=.86f-.26f*4*t0*(1-t0),y1=.86f-.26f*4*t1*(1-t1);
                frame.Slot(3).Quad(C(-1.02f+2.04f*t0,y0,-.46f),C(-1.02f+2.04f*t0,y0,.46f),C(-1.02f+2.04f*t1,y1,.46f),C(-1.02f+2.04f*t1,y1,-.46f),Vector3.up,new Color(.78f,.9f,.74f),1.2f)
                    .Quad(C(-1.02f+2.04f*t0,y0-.02f,-.46f),C(-1.02f+2.04f*t0,y0-.02f,.46f),C(-1.02f+2.04f*t1,y1-.02f,.46f),C(-1.02f+2.04f*t1,y1-.02f,-.46f),Vector3.down,new Color(.5f,.56f,.46f),1.2f);
            }
            frame.Slot(1).Lump(C(-.72f,.74f,0),new Vector3(.2f,.09f,.3f),5,.1f,new Color(.9f,.86f,.7f),new Color(.6f,.56f,.46f),8,4,.25f,across);
            frame.Slot(0).Tube(new[]{C(0,2f,0),C(0,1.86f,0)},new[]{.012f,.012f},4,hemp,hemp,1,2,false).Tube(new[]{C(0,1.8f,0),C(0,1.92f,0)},new[]{.2f,.02f},8,dark,wood,1,1,false);
            frame.Slot(2).Lump(C(0,1.68f,0),new Vector3(.16f,.2f,.16f),3,.05f,Color.white,Color.white,8,5);
            for(int k=0;k<3;k++)
            {
                frame.Slot(0).Tube(new[]{C(1.24f,1.5f-.02f*k,-.2f+k*.2f),C(1.3f,1.2f-.05f*k,-.2f+k*.2f)},new[]{.01f,.01f},4,hemp,hemp,1,2,false);
                frame.Slot(1).Lump(C(1.3f,1.1f-.05f*k,-.2f+k*.2f),new Vector3(.09f,.12f,.09f),k,.1f,k==1?new Color(.6f,.8f,.5f):new Color(.95f,.78f,.4f),new Color(.6f,.46f,.26f),7,5);
            }
            var cradle=kit.Built("Mender's cradle",frame.Slot(3),Plank,Cloth,Lantern,Weave);ZoneKit.Block(cradle,cradleAt+Vector3.up*.5f,new Vector3(2.6f,1,1.2f),30,"Cradle");
            var repair=new GameObject("Mender's cradle point",typeof(RepairBay)).GetComponent<RepairBay>();repair.transform.position=cradleAt+across*new Vector3(0,0,-1.1f);repair.transform.SetParent(cradle.transform,true);
            repair.prompt="Mender's cradle — repair and save";repair.saveSpawn="Press";repair.range=2.6f;
            kit.Lamp(new Vector3(CradleAt.x,1.7f,CradleAt.y),5,.7f,new Color(1,.8f,.5f));
            kit.Sign("THE PRESS",RootSigns[4].at.x,RootSigns[4].at.y);
        }

        // ---------------------------------------------------------------- the jungle: at the back and the edges, never over the floor
        /// <summary>Trunks and buttress roots along the north and east bank, their crowns behind everything a foot can reach, so nothing ever hangs between the camera and the party
        /// (the brief's rule: the jungle reads through trunks at the back and edges). Low knees of root and reeds stand in the water west and south, the camera's side.</summary>
        static void Jungle(ZoneKit kit)
        {
            var rng=new System.Random(101);var back=new ZoneKit.Shape();var crowns=new ZoneKit.Shape();
            void Tree(float x,float z,float tall,float girth,bool crowned)
            {
                var foot=kit.On(x,z)+Vector3.down*.6f;var path=new List<Vector3>();var thick=new List<float>();float lean=((float)rng.NextDouble()-.5f)*2.4f,twist=(float)rng.NextDouble()*6;
                for(int k=0;k<=6;k++){float t=k/6f;path.Add(foot+new Vector3(Mathf.Sin(t*2+twist)*.5f+lean*t,tall*t,Mathf.Cos(t*2.6f+twist)*.4f));thick.Add(girth*Mathf.Lerp(1.25f,.62f,Mathf.Sqrt(t)));}
                back.Tube(path,thick,10,new Color(.64f,.6f,.56f),new Color(.96f,.94f,.9f),3,.14f);
                int roots=4+rng.Next(3);
                for(int r=0;r<roots;r++)
                {
                    // A buttress: off the trunk at head height, out and down to the ground.
                    float a=r*Mathf.PI*2/roots+twist,reach=girth*(2.6f+1.6f*(float)rng.NextDouble()),high=2.2f+2.4f*(float)rng.NextDouble();var out2=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                    var end=foot+out2*reach;end.y=kit.Y(end.x,end.z)-.3f;
                    back.Tube(new[]{foot+Vector3.up*high+out2*girth*.5f,foot+Vector3.up*high*.45f+out2*reach*.5f,end},new[]{girth*.42f,girth*.3f,girth*.14f},7,new Color(.7f,.66f,.6f),new Color(.9f,.88f,.84f),1,.2f);
                }
                if(!crowned)return;
                // The crown: fronds in tiers from the trunk's head and from two boughs off it. Never a lump: from above a lump of leaf is a green boulder on a pole.
                var head=path[path.Count-1];Crown(crowns.Slot(0),head,girth*(3.4f+1.2f*(float)rng.NextDouble()),rng,16);
                for(int c=0;c<2;c++)
                {
                    var bough=head+new Vector3(((float)rng.NextDouble()-.5f)*girth*5,-.6f-(float)rng.NextDouble()*1.8f,((float)rng.NextDouble()-.3f)*girth*4);
                    back.Tube(new[]{path[path.Count-2],Vector3.Lerp(path[path.Count-2],bough,.6f)+Vector3.up*.5f,bough},new[]{girth*.4f,girth*.3f,girth*.16f},6,new Color(.7f,.66f,.6f),new Color(.9f,.88f,.84f),1,.2f);
                    Crown(crowns.Slot(0),bough,girth*(2.6f+1.2f*(float)rng.NextDouble()),rng,12);
                }
            }
            // The north bank, behind the upper walk, and the east bank either side of the river's gap. Each crown stands north or east of every floor, so it hides only more jungle.
            foreach(var (x,z,tall,girth) in new[]{(-52f,110f,17f,1.3f),(-38,112,20,1.5f),(-24,109.5f,16,1.2f),(-12,112.5f,21,1.6f),(2,110,18,1.4f),(15,112.5f,22,1.6f),(28,110,17,1.3f),(41,112,20,1.5f),(55,109.5f,18,1.4f),
                (71.5f,98,19,1.5f),(73,84,17,1.3f),(71,70,20,1.5f),(72.5f,57,16,1.2f),(72.5f,29,18,1.4f),(71,14,16,1.3f),(73,2,19,1.4f)})Tree(x,z,tall,girth,true);
            // Dead snags the storm left standing in the water: no crown, so they may stand nearer.
            foreach(var (x,z,tall,girth) in new[]{(-56f,60f,5.5f,.5f),(-50,86,7,.6f),(-40,96,6,.55f),(48,98,7,.6f),(60,84,6,.5f),(62,20,5,.5f)})Tree(x,z,tall,girth,false);
            // Smaller trees standing in the water where a crown at eight metres can hide nothing walked: in the lagoon behind the seed house, well south of the upper walk
            // (ZoneKit.Shadowed), and off the kitchen's and the press's east side, which the camera only ever looks toward.
            foreach(var (x,z,tall,girth) in new[]{(-13f,89.5f,6.5f,.7f),(6,90.5f,6.5f,.75f),(-2,92,6,.65f),(44,62,7.5f,.8f),(51,55,7,.75f),(47,75,8,.85f),(55,68,7.5f,.8f),(52,30,7,.75f),(58,12,7.5f,.8f),(-44,84,7,.75f),(-52,72,6.5f,.7f)})Tree(x,z,tall,girth,true);
            // The understorey along the bank's lip, which is what a jungle looks like from above when its canopy is out of the frame: tree-ferns' heads sat low, and clumps of
            // broad leaf between them. Two outlines mixed, both of them leaves.
            void Thicket(Vector3 at)
            {
                if(rng.Next(3)>0){Crown(crowns.Slot(0),at+Vector3.up*(.4f+.8f*(float)rng.NextDouble()),2.2f+1.4f*(float)rng.NextDouble(),rng,11);return;}
                int blades=5+rng.Next(4);float yaw=(float)rng.NextDouble()*360;
                for(int k=0;k<blades;k++)
                {
                    float reach=1.5f+1.3f*(float)rng.NextDouble();
                    crowns.Slot(1).Frond(at,Quaternion.Euler(0,yaw+k*360f/blades,0)*Vector3.forward,reach,reach*.52f,reach*1.1f,reach*.75f,new Color(.22f,.4f,.22f),new Color(.44f+.16f*(float)rng.NextDouble(),.76f,.34f),5);
                }
            }
            for(float x=-60;x<66;x+=2.6f)Thicket(kit.On(x+(float)rng.NextDouble()*2,107.2f+(float)rng.NextDouble()*2.4f,.3f));
            for(float z=-6;z<106;z+=2.6f){if(Mathf.Abs(z-43)<8)continue;Thicket(kit.On(69.6f+(float)rng.NextDouble()*2.2f,z+(float)rng.NextDouble()*2,.3f));}
            var trunks=kit.Built("Jungle trunks",back,Bark);var leaves=kit.Built("Jungle crowns",crowns.Slot(1),CanopyLeaf,Leaf);leaves.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;trunks.isStatic=true;

            // Knees of root and reeds in the open water: low, so the camera's side stays open, and thick enough that no stretch of water is bare.
            var knees=new ZoneKit.Shape();var reeds=new ZoneKit.Shape();var pads=new ZoneKit.Shape();
            for(int k=0;k<1100;k++)
            {
                var p=new Vector2((float)rng.NextDouble()*134-60,(float)rng.NextDouble()*118-6);float size=.5f+(float)rng.NextDouble()*.9f,turn=(float)rng.NextDouble()*6.28f;int kind=rng.Next(5);bool far=rng.Next(4)!=0;
                if(RootHeight(p)>WaterLevel-.5f)continue;
                // Thick within a frame's reach of where anyone walks, thin beyond: that water is only ever at the edge of a view.
                float out2=Mathf.Min(LowLand(p),FromWalk(p,out _)-WalkHalf);if(out2>13&&far)continue;
                var at=new Vector3(p.x,WaterLevel,p.y);
                // Away from the decks and the island's way in, where they'd stand through something.
                if(Mathf.Abs(p.y-44)<3&&Mathf.Abs(p.x)<22||ZoneKit.Segment(p,IslandMoored,IslandAdrift)<8||ZoneKit.Segment(p,new Vector2(-23.4f,28.4f),new Vector2(-39.8f,17))<2.4f)continue;
                bool inPool=Oval(p,Pool,PoolA,PoolB)<0;
                if(kind==0&&!inPool)knees.Tube(new[]{at+Vector3.down*.8f,at+new Vector3(Mathf.Cos(turn)*.3f,size*.9f,Mathf.Sin(turn)*.3f),at+new Vector3(Mathf.Cos(turn)*size*1.4f,-.6f,Mathf.Sin(turn)*size*1.4f)},new[]{.22f,.18f,.1f},6,new Color(.62f,.58f,.52f),Color.white,1,.4f);
                else if(kind==1&&!inPool)for(int r=0;r<5;r++)reeds.Frond(at+new Vector3(Mathf.Cos(r*1.3f)*.2f,-.1f,Mathf.Sin(r*1.3f)*.2f),new Vector3(Mathf.Cos(turn+r*1.26f),0,Mathf.Sin(turn+r*1.26f)),.5f,.1f,1.2f+size,.2f,new Color(.3f,.5f,.3f),new Color(.66f,.84f,.42f),3);
                else
                {
                    // Lily pads, in rafts of two or three, one in five with a flower.
                    for(int r=0;r<1+rng.Next(3);r++)
                    {
                        var c=at+new Vector3(Mathf.Cos(turn+r*2)*size*.9f,.03f+.004f*r,Mathf.Sin(turn+r*2)*size*.9f);float green=.5f+.3f*(float)rng.NextDouble();
                        Pad(pads.Slot(0),c,size*(.5f+.2f*r),turn+r*2,new Color(.36f,green,.3f));
                        if(rng.Next(5)==0)pads.Slot(1).Lump(c+Vector3.up*.1f,new Vector3(.16f,.12f,.16f),k,.2f,new Color(1,.86f,.9f),new Color(.9f,.5f,.66f),7,4);
                    }
                }
            }
            // The lagoons either side of the upper walk fill every frame taken from it, and under the licensed water (plainer than ours) the one east of it measured .29 of a frame
            // with nothing in it. A second, thicker scatter of pads and reeds within ten metres of the walk, on its own draws so the first is unchanged.
            var lagoon=new System.Random(131);
            for(int k=0;k<900;k++)
            {
                var p=new Vector2((float)lagoon.NextDouble()*110-55,(float)lagoon.NextDouble()*58+52);float size=.5f+(float)lagoon.NextDouble()*.9f,turn=(float)lagoon.NextDouble()*6.28f;
                bool reed=lagoon.Next(4)==0;int count=1+lagoon.Next(3);float green=.5f+.3f*(float)lagoon.NextDouble();bool flower=lagoon.Next(5)==0;
                float off=FromWalk(p,out _)-WalkHalf;
                if(RootHeight(p)>WaterLevel-.5f||off<.9f||off>10||LowLand(p)<1.4f)continue;
                var at=new Vector3(p.x,WaterLevel,p.y);
                if(reed){for(int r=0;r<5;r++)reeds.Frond(at+new Vector3(Mathf.Cos(r*1.3f)*.2f,-.1f,Mathf.Sin(r*1.3f)*.2f),new Vector3(Mathf.Cos(turn+r*1.26f),0,Mathf.Sin(turn+r*1.26f)),.5f,.1f,1.2f+size,.2f,new Color(.3f,.5f,.3f),new Color(.66f,.84f,.42f),3);continue;}
                for(int r=0;r<count;r++)
                {
                    var c=at+new Vector3(Mathf.Cos(turn+r*2)*size*.9f,.03f+.004f*r,Mathf.Sin(turn+r*2)*size*.9f);
                    Pad(pads.Slot(0),c,size*(.5f+.2f*r),turn+r*2,new Color(.36f,green-.04f*r,.3f));
                    if(flower&&r==0)pads.Slot(1).Lump(c+Vector3.up*.1f,new Vector3(.16f,.12f,.16f),k+2000,.2f,new Color(1,.86f,.9f),new Color(.9f,.5f,.66f),7,4);
                }
            }
            kit.Built("Root knees",knees,Bark);kit.Built("Reeds",reeds,Leaf).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            kit.Built("Lily pads",pads.Slot(1),Cloth,Cloth).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        /// <summary>The rims as they are seen. Along every edge that has a rim in the collider (and nowhere else: not across an opening, not at a ramp's foot) three roots lie
        /// together: two as thick as a thigh twisted round each other on the floor's edge, and a thinner one over the wall's top. They follow the platforms' own outlines, traced
        /// from the same functions the ground is made of, so they turn with every lobe and dive into the earth where a rim ends. Round, bark-skinned, lit and shadowed as roots.</summary>
        static void RootKerbs(ZoneKit kit)
        {
            var shape=new ZoneKit.Shape();var rng=new System.Random(307);int runs=0;float laid=0;const float Step=.6f;
            void Trace(Func<Vector2,float> field,bool ridge)
            {
                // The line .55 m in from the edge: the middle of the rim.
                foreach(var chain in ZoneKit.Contours(field,.55f,-62,76,-8,114,.5f))
                {
                    var line=Even(chain,Step);bool loop=Vector2.Distance(chain[0],chain[chain.Count-1])<.01f;
                    // Only where there is a rim to stand for; and the low platforms' line stops where the ridge stands over it.
                    var rim=line.Select(p=>RootHeight(p)-RootDrawn(p)>.2f&&(ridge||FromWalk(p,out _)>WalkHalf)).ToList();
                    if(loop&&rim.Contains(false)){int gap=rim.IndexOf(false);line=line.Skip(gap).Concat(line.Take(gap)).ToList();rim=rim.Skip(gap).Concat(rim.Take(gap)).ToList();loop=false;}
                    else if(loop)line.Add(line[0]);
                    var run=new List<(Vector2 at,Vector2 inward)>();
                    void Lay(bool closed){if(run.Count>=4){Kerb(shape,run,rng,closed);runs++;laid+=run.Count*Step;}run=new List<(Vector2 at,Vector2 inward)>();}
                    for(int i=0;i<line.Count;i++)
                    {
                        if(i<rim.Count&&!rim[i]){Lay(false);continue;}
                        var p=line[i];run.Add((p,new Vector2(field(p+Vector2.right*.3f)-field(p-Vector2.right*.3f),field(p+Vector2.up*.3f)-field(p-Vector2.up*.3f)).normalized));
                    }
                    Lay(loop);
                }
            }
            Trace(p=>-LowLand(p),false);Trace(p=>WalkHalf-FromWalk(p,out _),true);
            kit.Built("Root rims",shape,Bark).isStatic=true;
            Debug.Log($"XYLOS_ROOT_RIMS runs={runs} metres={laid:0} points={shape.Vertices}");
        }
        /// <summary>One board, its grain along its length: a box's top lays the plank texture's rows along its local x, so a board turned the other way showed three seams across
        /// itself and every deck in town read as a floor of square tiles. Length on the box's z, and the texture at a board's width to a row.</summary>
        static ZoneKit.Shape Board(ZoneKit.Shape into,Vector3 centre,float length,float wide,float thick,float yaw,Color tone,Color top)=>into.Box(centre,new Vector3(wide,thick,length),Quaternion.Euler(0,yaw,0),tone,top,.44f);
        /// <summary><see cref="ZoneKit.Shape.Deck"/>'s boards (the same draws, so nothing else moves) with their grain run along each board.</summary>
        static ZoneKit.Shape Boards(ZoneKit.Shape into,Vector3 from,Vector3 to,float wide,float thick,Color tone,System.Random rng,float board=.42f,float gap=.04f)
        {
            var run=to-from;float length=run.magnitude;if(length<.01f)return into;var along=run/length;
            var turn=Quaternion.LookRotation(along,Vector3.up);int count=Mathf.Max(1,Mathf.RoundToInt(length/board));float each=length/count;float yaw=turn.eulerAngles.y+90;
            for(int i=0;i<count;i++)
            {
                float shade=.86f+.2f*(float)rng.NextDouble(),over=(float)rng.NextDouble()*.14f;
                var centre=from+along*((i+.5f)*each)+Vector3.down*thick*.5f+turn*Vector3.right*((float)rng.NextDouble()-.5f)*.08f;
                Board(into,centre,wide+over,each-gap,thick,yaw,ZoneKit.Shape.Dim(tone,.6f),ZoneKit.Shape.Dim(tone,shade));
            }
            return into;
        }
        /// <summary>A lily pad: round, with the slit every lily pad has from its rim to its middle, its sixteen wedges alternately a shade lighter so it reads as veined. A nine-sided disc of
        /// one green read as a flat octagon in every frame with water in it.</summary>
        static ZoneKit.Shape Pad(ZoneKit.Shape into,Vector3 centre,float radius,float slit,Color tone)
        {
            const int Wedges=16;const float Gap=.38f;float each=(Mathf.PI*2-Gap)/Wedges;var middle=centre+Vector3.up*.012f;
            for(int k=0;k<Wedges;k++)
            {
                float a0=slit+Gap*.5f+k*each,a1=a0+each;
                into.Triangle(middle,centre+new Vector3(Mathf.Cos(a1),0,Mathf.Sin(a1))*radius,centre+new Vector3(Mathf.Cos(a0),0,Mathf.Sin(a0))*radius,Vector3.up,k%2==0?tone:ZoneKit.Shape.Dim(tone,.9f));
            }
            return into;
        }
        /// <summary>A line's points again at even steps along it.</summary>
        static List<Vector2> Even(IReadOnlyList<Vector2> chain,float step)
        {
            var even=new List<Vector2>{chain[0]};float carried=0;
            for(int i=1;i<chain.Count;i++)
            {
                float leg=Vector2.Distance(chain[i-1],chain[i]);if(leg<1e-5f)continue;
                float at=step-carried;
                while(at<=leg){even.Add(Vector2.Lerp(chain[i-1],chain[i],at/leg));at+=step;}
                carried=leg-(at-step);
            }
            return even;
        }
        static void Kerb(ZoneKit.Shape shape,List<(Vector2 at,Vector2 inward)> run,System.Random rng,bool closed)
        {
            int n=run.Count;var along=new float[n];for(int i=1;i<n;i++)along[i]=along[i-1]+Vector2.Distance(run[i].at,run[i-1].at);
            // A closed rim's twist has to come round to where it began.
            float length=along[n-1],wave=5.2f,start=closed?0:(float)rng.NextDouble()*wave;if(closed)wave=length/Mathf.Max(1,Mathf.Round(length/wave));
            var strands=new[]{new List<Vector3>(),new List<Vector3>(),new List<Vector3>()};var girths=new[]{new List<float>(),new List<float>(),new List<float>()};
            for(int i=0;i<n;i++)
            {
                var (p,inward)=run[i];float turn=(along[i]+start)/wave*Mathf.PI*2,floor=RootDrawn(p),swell=.86f+.3f*Mathf.PerlinNoise(p.x*.31f+3,p.y*.31f+9);
                // At a run's two ends the roots go down into the earth.
                float up=closed?1:ZoneKit.Smooth(0,1.5f,Mathf.Min(along[i],length-along[i]));
                Vector3 At(float off,float high)=>new(p.x+inward.x*off,floor+high*up-.3f*(1-up),p.y+inward.y*off);
                strands[0].Add(At(.2f*Mathf.Sin(turn),.2f+.15f*Mathf.Cos(turn)));girths[0].Add(.33f*swell*Mathf.Lerp(.45f,1,up));
                strands[1].Add(At(-.2f*Mathf.Sin(turn),.2f-.15f*Mathf.Cos(turn)));girths[1].Add(.31f*swell*Mathf.Lerp(.45f,1,up));
                // The third lies outside the edge, over the top of the wall.
                strands[2].Add(At(-.56f+.08f*Mathf.Sin(turn*2+1),-.08f+.08f*Mathf.Sin(turn*3)));girths[2].Add(.23f*swell*Mathf.Lerp(.5f,1,up));
            }
            Color[] tones={new(.94f,.9f,.84f),new(.8f,.74f,.68f),new(.68f,.64f,.6f)};
            for(int k=0;k<3;k++)shape.Tube(strands[k],girths[k],7,tones[k],ZoneKit.Shape.Dim(tones[k],.9f),1,.22f,!closed);
        }
        /// <summary>A crown as fronds: a rosette in three tiers from one head, the outer drooping below it and the inner standing, each frond its own length and green. Drawn with
        /// the cut-out frond, from both sides, and moving.</summary>
        static void Crown(ZoneKit.Shape into,Vector3 head,float reach,System.Random rng,int fronds)
        {
            float turn=(float)rng.NextDouble()*360;Color stem=new(.2f,.36f,.2f);
            for(int tier=0;tier<3;tier++)
            {
                int count=tier==0?fronds:tier==1?fronds*2/3:fronds/3;float length=reach*(tier==0?1:tier==1?.76f:.5f);
                for(int i=0;i<count;i++)
                {
                    float yaw=turn+tier*23+i*360f/count+((float)rng.NextDouble()-.5f)*16,own=length*(.8f+.34f*(float)rng.NextDouble()),green=(float)rng.NextDouble();
                    float rise=own*(tier==0?.22f:tier==1?.55f:.95f),droop=own*(tier==0?.7f:tier==1?.6f:.5f);
                    into.Frond(head+Vector3.up*tier*.2f,Quaternion.Euler(0,yaw,0)*Vector3.forward,own,own*.34f,rise,droop,stem,new Color(.42f+.2f*green,.74f+.08f*green,.3f+.08f*green),6);
                }
            }
        }
        static Vector3[] Ring(Vector3 centre,float radius){var ring=new Vector3[13];for(int k=0;k<13;k++){float a=k*Mathf.PI*2/12;ring[k]=centre+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);}return ring;}
        /// <summary>One of the things a townsperson wears or carries, built about the point it is worn at (the crown of the head, the small of the back, the shoulders, the ground by
        /// a foot). Every copy shares the one saved mesh.</summary>
        static GameObject Carried(ZoneKit kit,string what)
        {
            var shape=new ZoneKit.Shape();Color wood=new(.86f,.72f,.5f),dark=new(.6f,.48f,.34f),straw=new(.98f,.9f,.6f),hemp=new(.95f,.86f,.62f),withy=new(.8f,.68f,.46f),pale=new(.96f,.84f,.58f);Material[] of;
            switch(what)
            {
                case "Leaf hat":
                    // A wide shallow cone of dried leaf, dark underneath: its brim is wider than the shoulders, which is what reads from above.
                    shape.Tube(new[]{new Vector3(0,-.05f,0),new Vector3(0,.08f,0),new Vector3(0,.32f,0)},new[]{.58f,.36f,.02f},12,new Color(.84f,.76f,.46f),straw,3,.9f,false).Disc(new Vector3(0,-.05f,0),Vector3.down,.58f,12,new Color(.5f,.45f,.3f));
                    of=new[]{Thatch};break;
                case "Pannier":
                    // A tall basket for the back, heaped.
                    shape.Slot(0).Tube(new[]{Vector3.zero,new Vector3(0,.34f,0),new Vector3(0,.64f,0)},new[]{.18f,.25f,.27f},10,withy,pale,2,2).Tube(Ring(new Vector3(0,.64f,0),.27f),new[]{.03f},5,dark,dark,1,2,false);
                    for(int k=0;k<6;k++)shape.Slot(1).Lump(new Vector3(Mathf.Cos(k*1.1f)*.13f,.68f+.05f*(k%3),Mathf.Sin(k*1.1f)*.13f),new Vector3(.1f,.09f,.1f),k,.1f,k%2==0?new Color(.95f,.36f,.24f):new Color(.98f,.82f,.36f),new Color(.6f,.3f,.2f),6,4);
                    of=new[]{Weave,Cloth};break;
                case "Yoke":
                    // A carrying pole across the shoulders, bowed by the basket at each end.
                    shape.Slot(0).Tube(new[]{new Vector3(-1.02f,-.07f,0),new Vector3(-.5f,0,0),new Vector3(0,.02f,0),new Vector3(.5f,0,0),new Vector3(1.02f,-.07f,0)},new[]{.03f,.036f,.04f,.036f,.03f},6,wood,dark,1,2);
                    foreach(int side in new[]{-1,1})
                    {
                        var hung=new Vector3(side*.98f,-.6f,0);
                        foreach(int cord in new[]{-1,1})shape.Slot(0).Tube(new[]{new Vector3(side*.98f,-.07f,0),hung+new Vector3(0,.22f,cord*.15f)},new[]{.012f,.012f},4,hemp,hemp,1,2,false);
                        shape.Slot(1).Tube(new[]{hung+Vector3.down*.06f,hung+Vector3.up*.22f},new[]{.15f,.19f},9,withy,pale,2,2);
                        shape.Slot(2).Lump(hung+Vector3.up*.24f,new Vector3(.17f,.1f,.17f),side+3,.2f,side<0?new Color(.6f,.86f,.4f):new Color(.98f,.82f,.36f),new Color(.4f,.5f,.3f),7,4);
                    }
                    of=new[]{Plank,Weave,Cloth};break;
                case "Pole":
                    // A pole twice a grower's height, hooked at its head, a gourd hung from the hook: a ferry pole, a fruit hook or a rod, as its owner's place says.
                    shape.Slot(0).Tube(new[]{Vector3.zero,new Vector3(.03f,1.3f,0),new Vector3(.02f,2.5f,0),new Vector3(.16f,2.66f,0),new Vector3(.26f,2.52f,0)},new[]{.04f,.035f,.03f,.026f,.02f},6,dark,wood,1,1.5f)
                        .Tube(new[]{new Vector3(.25f,2.52f,0),new Vector3(.25f,2.26f,0)},new[]{.01f,.01f},4,hemp,hemp,1,2,false);
                    shape.Slot(1).Lump(new Vector3(.25f,2.16f,0),new Vector3(.1f,.13f,.1f),4,.1f,new Color(.98f,.82f,.4f),new Color(.7f,.5f,.24f),7,5);
                    of=new[]{Plank,Cloth};break;
                case "Tray":
                    // A seedling tray carried on the head.
                    shape.Slot(0).Box(new Vector3(0,.04f,0),new Vector3(.98f,.08f,.62f),Quaternion.identity,dark,new Color(.42f,.34f,.26f));
                    for(int k=0;k<8;k++)shape.Slot(1).Lump(new Vector3(-.36f+k%4*.24f,.13f,-.15f+k/4*.3f),new Vector3(.09f,.12f,.09f),k,.15f,new Color(.6f,.88f,.4f),new Color(.34f,.54f,.26f),6,4);
                    of=new[]{Plank,Cloth};break;
                default:
                    // A coil of line worn over one shoulder and under the other arm.
                    for(int turn=0;turn<3;turn++)
                    {
                        var loop=new Vector3[13];for(int k=0;k<13;k++){float a=k*Mathf.PI*2/12;loop[k]=Quaternion.Euler(0,0,50)*new Vector3(Mathf.Cos(a)*(.4f+turn*.02f),(turn-1)*.05f,Mathf.Sin(a)*.33f);}
                        shape.Tube(loop,new[]{.035f},5,hemp,ZoneKit.Shape.Dim(hemp,.8f),1,2,false);
                    }
                    of=new[]{Plank};break;
            }
            var made=kit.Built("Carried "+what.ToLowerInvariant(),shape.Slot(of.Length-1),of);made.name=what;return made;
        }
        static bool folkMeasured;
        /// <summary>Gives someone their look and their build. What is worn goes on the bone that carries it (the head, the chest), placed by the body's own measure, so it nods and
        /// breathes with them; a pole stands on the ground beside them. Build last: it scales what they carry with them.</summary>
        static void Dress(ZoneKit kit,GameObject person,Look look,float build)
        {
            var model=person.transform.GetChild(0);var rig=model.GetComponentInChildren<Animator>();var parts=model.GetComponentsInChildren<Transform>();
            Transform Bone(HumanBodyBones bone,params string[] named)
            {
                var found=rig!=null&&rig.isHuman?rig.GetBoneTransform(bone):null;
                foreach(string name in named){if(found!=null)break;found=parts.FirstOrDefault(t=>t.name.ToLowerInvariant().Contains(name));}
                return found;
            }
            Transform head=Bone(HumanBodyBones.Head,"head"),chest=Bone(HumanBodyBones.Chest,"spine1","spine01","spine_01","spine"),arm=Bone(HumanBodyBones.LeftUpperArm,"leftarm","upperarm","arm");
            float feet=person.transform.position.y,crown=ZoneKit.Drawn(model.gameObject).max.y-feet,shoulder=arm!=null?arm.position.y-feet:crown*.6f;
            if(!folkMeasured){folkMeasured=true;Debug.Log($"XYLOS_FOLK crown={crown:0.00} shoulder={shoulder:0.00} head={(head!=null?head.name:"none")} chest={(chest!=null?chest.name:"none")} arm={(arm!=null?arm.name:"none")} human={rig!=null&&rig.isHuman}");}
            void Wear(string what,Transform on,Vector3 local)
            {
                var thing=Carried(kit,what);thing.transform.SetPositionAndRotation(person.transform.TransformPoint(local),person.transform.rotation);thing.transform.SetParent(on!=null?on:person.transform,true);
            }
            bool hat=look is Look.Hat or Look.HatPannier or Look.HatPole,pannier=look is Look.Pannier or Look.HatPannier,pole=look is Look.Pole or Look.HatPole;
            if(hat)Wear("Leaf hat",head,new Vector3(0,crown-.03f,0));
            if(pannier)Wear("Pannier",chest,new Vector3(0,shoulder-.56f,-.4f));
            if(pole)Wear("Pole",null,new Vector3(.42f,0,.12f));
            if(look==Look.Yoke)Wear("Yoke",chest,new Vector3(0,shoulder+.08f,-.05f));
            if(look==Look.Tray)Wear("Tray",head,new Vector3(0,crown+.03f,0));
            if(look==Look.Coil)Wear("Coil",chest,new Vector3(0,shoulder-.16f,0));
            person.transform.localScale=Vector3.one*build;
        }
        /// <summary>Ferns and low growth on the platforms themselves, as built leaves: bigger against the rims at the back and edges, small in the open, none where feet go.</summary>
        static void Growth(ZoneKit kit)
        {
            var rng=new System.Random(211);
            foreach(var (name,xMin,xMax) in new[]{("west",-62f,-12f),("middle",-12f,22f),("east",22f,70f)})
            {
                var shape=new ZoneKit.Shape();
                for(int k=0;k<220;k++)
                {
                    var p=new Vector2(xMin+(float)rng.NextDouble()*(xMax-xMin),(float)rng.NextDouble()*112-2);float roll=(float)rng.NextDouble(),hue=(float)rng.NextDouble();int leaves=6+rng.Next(4);
                    float low=LowLand(p),walk=FromWalk(p,out _)-WalkHalf,inside=Mathf.Max(-low,walk<0?-walk:-99);
                    if(inside<RimWide+.1f||FromWays(p)<2.4f||RootKeepsOff(p))continue;
                    // Against a rim it may stand to the knee; out in the open it keeps under the ground kit's limit (D131).
                    bool edge=inside<3.2f;if(!edge&&roll>.4f)continue;
                    float size=edge?.55f+.5f*roll:.3f+.22f*roll;
                    shape.Fern(kit.On(p.x,p.y,-.02f),size,leaves,rng,new Color(.24f,.42f,.24f),new Color(.44f+.2f*hue,.76f,.34f+.1f*hue));
                }
                kit.Built("Ferns "+name,shape,Leaf).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            }
        }
        /// <summary>The floor's dressing, last, once everything stands on it: the ground kit's layers (trodden earth along the ways, leaf litter, damp toward the rims), its flat details and
        /// low cover, and the canopy's shade swaying over all of it. Without the kit's textures (python tools/ground_textures.py) the floor keeps its own vertex tones and says so.</summary>
        static void Ground(ZoneKit kit,ZoneKit.Land land)
        {
            if(!File.Exists(GroundKit.Art+"ground-litter.png")||!File.Exists(GroundKit.Art+"ground-decals.png")||!File.Exists(GroundKit.Art+"ground-cover.png")||!File.Exists(GroundKit.Art+"ground-shade-dapple.png"))
            {Debug.LogWarning("XYLOS_GROUND_KIT_PENDING Rootmarket: the ground kit's textures aren't painted yet, so the floor has its vertex tones only");return;}
            var ground=GroundKit.Open(kit.zone,land.FloorFilter,31,3.2f);
            foreach(var way in RootWays)ground.ClearLane(way,1.5f);
            float Inside(Vector2 p){float low=LowLand(p),walk=FromWalk(p,out _)-WalkHalf;return Mathf.Max(-low,walk<0?-walk:-99);}
            float Open(Vector2 p)=>ZoneKit.Smooth(RimWide+.2f,RimWide+1.2f,Inside(p));
            ground.Layers(new Vector4(.05f,.34f,.4f,.28f),1.2f,new GroundKit.Layer("sand",new Color(.86f,.74f,.52f,1),3.2f),new GroundKit.Layer("litter",new Color(.74f,.66f,.36f,1),2.6f),new GroundKit.Layer("damp",new Color(.3f,.42f,.3f,.92f),3.4f));
            // Trodden earth where feet go: the ways between the town's places, worn through in patches, and each doorstep.
            foreach(var way in RootWays)ground.PaintPath(0,way,1.7f,1,.35f,.7f,way.Length);
            foreach(var (at,reach) in Worn)ground.PaintAround(0,at,reach*.62f,1,(int)at.x);
            // Litter drifts off the ways, thickest toward the back; damp ground along every rim.
            ground.PaintPatches(1,5.5f,.42f,p=>Open(p)*ZoneKit.Smooth(1.6f,3.4f,FromWays(p)),3);
            float OnTheRidge(Vector2 p)=>FromWalk(p,out float along)<WalkHalf&&WalkHeight(along)>.4f?1:0;
            ground.Paint(2,p=>(1-ZoneKit.Smooth(RimWide+.2f,RimWide+1.5f,Inside(p)+(ground.Noise(p,3.5f,5)-.5f)*1.2f))*ZoneKit.Smooth(.2f,RimWide,Inside(p))*(1-.8f*OnTheRidge(p)));
            ground.Paint(0,p=>OnTheRidge(p)*ZoneKit.Smooth(RimWide,RimWide+.5f,Inside(p))*(.55f+.45f*ground.Noise(p,4,9)));
            ground.Shade(p=>.1f*(1-ZoneKit.Smooth(RimWide,RimWide+1.4f,Inside(p)))*(1-OnTheRidge(p))-.06f*(1-ZoneKit.Smooth(.4f,1.6f,FromWays(p))));
            Color leaf=new(.7f,.66f,.36f,1),stone=new(.62f,.6f,.52f,1);
            ground.Decals(new[]{"Litter","Lichen"},9,p=>Open(p)*ZoneKit.Smooth(1.2f,3,FromWays(p)),.9f,2.2f,leaf,false,.12f,1);
            ground.Decals(new[]{"PebblesA","PebblesB"},5,p=>Open(p)*ZoneKit.Smooth(.8f,2.4f,FromWays(p)),.7f,1.5f,stone,true,.1f,2);
            ground.Decals(new[]{"Stain","SandDrift"},4,p=>Open(p),1.4f,3.2f,new Color(.4f,.46f,.34f,.7f),false,.1f,3);
            foreach(var way in RootWays)ground.Strip("Tracks",way,.2f,1.1f,new Color(.5f,.42f,.3f,.55f));
            Color fern=new(.5f,.8f,.38f,1);
            ground.Cover(new[]{"Fern","Sprouts","TuftB"},7,p=>Open(p)*ZoneKit.Smooth(2,3.6f,FromWays(p))*(RootKeepsOff(p)?0:1),.22f,.5f,fern,3,.16f,1);
            ground.Cover(new[]{"Flowers","FungusA","FungusB","Twigs"},4,p=>Open(p)*ZoneKit.Smooth(1.8f,3.2f,FromWays(p))*(RootKeepsOff(p)?0:1),.14f,.32f,new Color(.95f,.9f,.7f,1),2,.14f,2);
            ground.Cover(new[]{"Reeds","TuftA","TuftC"},6,p=>(1-ZoneKit.Smooth(RimWide+.4f,RimWide+1.8f,Inside(p)))*ZoneKit.Smooth(RimWide,RimWide+.3f,Inside(p))*(RootKeepsOff(p)?0:1),.26f,.56f,new Color(.56f,.8f,.42f,1),3,.14f,3);
            ground.Look("Xylos",null).LampPools(2.4f,new Color(1,.78f,.45f,.5f));
            // The canopy nobody can see is over all of it: its shade lies on the ground in patches a few strides across, and sways.
            ground.MovingShade("dapple",9,Vector2.zero,.3f,.46f,.03f,new Vector2(.45f,.28f));
            ground.Finish();
        }
    }
}
