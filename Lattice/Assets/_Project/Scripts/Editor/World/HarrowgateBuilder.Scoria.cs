using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>The Scoria Flows (wild, combat; rule: the crust tells you). Map brief, coordinates and contracts: docs/maps/ScoriaFlows.md. Everything of the zone's own is in
    /// <see cref="Scoria"/>, so nothing here can collide with the other Harrowgate files' members. <c>-scoria-greybox</c> leaves out dressing and the ground kit (the blockout).
    /// The Picket's landing arrives at spawn <c>Arrival</c>; the launch back arrives at <see cref="HarrowgateBuilder.FromScoriaSpawn"/>.</summary>
    public static partial class HarrowgateBuilder
    {
        /// <summary>What the Flows set on their own definition after HarrowgateBuilder.Define's common fields. Nothing yet: the camp keeps the zone's track.</summary>
        static void ScoriaDefinition(ZoneDef z){}
        static void BuildScoria()=>Scoria.Build();
        [ZoneLook.Source] static void ScoriaLooks()
        {
            // Ash-filtered daylight over basalt: a warm sun, a cooler fill so dark crust reads as stone and not as one flat brown. The bloom's threshold sits over the open melt's own
            // light, so a marking laid on it isn't drowned in its glow (the melt is the brightest thing in the valley and still under it).
            ZoneLook.Register(ScoriaZone,new ZoneLook.Spec{sun=C(1f,.86f,.72f),sunIntensity=1.25f,sunPitch=42,shadowStrength=.8f,
                sky=C(.44f,.42f,.46f),equator=C(.4f,.34f,.3f),ground=C(.22f,.14f,.1f),
                fog=true,fogColor=C(.5f,.4f,.36f),fogStart=60,fogEnd=220,
                contrast=12,saturation=4,splitShadows=C(.44f,.45f,.56f),splitHighlights=C(.6f,.5f,.4f),
                bloomThreshold=1.05f,bloomIntensity=.5f,vignette=.22f});
        }

        internal static class Scoria
        {
            // ---------------------------------------------------------------- the plan
            // x runs east and z north; the fixed camera looks north-north-east, so every flow is crossed going north and read from its south bank. The valley runs south to north from the
            // landing to the Foundry gate; the camp is up a side valley east of the junction. Floors are basalt shelves at about nought; a flow's plates lie in a bed a third of a metre down.
            internal const float Bed=-.32f,BankRun=1.4f,Terrace=.9f;
            /// <summary>The walkable valley as chains of discs (x, z, radius): a point is in it inside the hull of two neighbouring discs.</summary>
            static readonly (float x,float z,float r)[][] Valley=
            {
                new[]{(0f,7f,13f),(1f,24f,11f),(4f,40f,13f),(2f,56f,12f),(0f,72f,11f),(0f,88f,13f),(-1f,101f,12f)},
                new[]{(4f,32f,6f),(15f,31f,8f)},
                new[]{(-8f,116f,19f),(-6f,134f,20f),(2f,150f,13f),(2f,160f,10f),(0f,178f,17f),(-1f,196f,13f),(4f,210f,10f),(1f,224f,10f),(0f,240f,12f),(0f,253f,9f)},
                new[]{(8f,104f,5f),(15f,106f,5f),(26f,108f,8f),(31f,113f,4.5f)},
                new[]{(-6f,194f,8f),(-20f,195f,6f)},
                new[]{(4f,210f,6f),(16f,209f,6f),(28f,208f,7f),(42f,206f,12f),(56f,205f,9f),(66f,205f,5f)},new[]{(42f,206f,10f),(49f,213f,7.5f),(58f,209f,6f)},
            };
            /// <summary>Plan points a player stands at, named once: the scatter's keep-off, the navigation's must-reach list, LookDev's views, the map brief and a route all want them.</summary>
            internal static readonly Vector2 Arrival=new(2,9),Pad=new(-8,5),LandingCradle=new(9,4),BrekkStart=new(5,16),BrekkTongue=new(7,38.5f),BrekkCauseway=new(-5,64),
                Causeway=new(0,84),CrossingFight=new(0,93),KettleOne=new(6.5f,98),HollowCache=new(32f,114.5f),HollowFight=new(26,108),
                IslandA=new(-5,121),IslandB=new(4,136.5f),GlassIsland=new(-22,141),GlassCache=new(-22.5f,142.5f),DeltaFight=new(-4,128),NorthShelf=new(2,160),
                BatteryFight=new(0,184),KettleTwo=new(-12.5f,198.5f),SledCache=new(-21,196.5f),Junction=new(4,210),CampSpawn=new(39,204),CampCradle=new(35.5f,200.5f),
                CampBench=new(41.5f,198.5f),BrekkStall=new(49.5f,200.6f),TovraAt=new(46,210.5f),ForgeAt=new(56,211.5f),PikeHead=new(54,208.4f),HarrowgateSpawn=new(61,205),
                HarrowgateRoad=new(66.5f,205),GateFight=new(0,243),KettleThree=new(8.5f,248),FoundrySpawn=new(0,251),FoundryGate=new(0,259);
            /// <summary>Basalt that stands out of a flow and never cycles: the delta's two stepping islands and the glass island (x, z, radius).</summary>
            internal static readonly Vector3[] Islands={new(IslandA.x,IslandA.y,3.6f),new(IslandB.x,IslandB.y,3.4f),new(GlassIsland.x,GlassIsland.y,3.4f)};

            static float Chain(Vector2 p,(float x,float z,float r)[] chain)
            {
                float d=float.PositiveInfinity;
                for(int i=1;i<chain.Length;i++)
                {
                    Vector2 a=new(chain[i-1].x,chain[i-1].z),ab=new Vector2(chain[i].x,chain[i].z)-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);
                    d=Mathf.Min(d,Vector2.Distance(p,a+ab*t)-Mathf.Lerp(chain[i-1].r,chain[i].r,t));
                }
                return d;
            }
            /// <summary>Metres outside the valley's floor (negative inside it). An edge wanders, so no wall is an arc.</summary>
            internal static float Open(Vector2 p)
            {
                float d=float.PositiveInfinity;foreach(var chain in Valley)d=Mathf.Min(d,Chain(p,chain));
                return d+(Mathf.PerlinNoise(p.x*.07f+11,p.y*.07f+3)-.5f)*3f+(Mathf.PerlinNoise(p.x*.21f+5,p.y*.21f+9)-.5f)*1.1f;
            }

            // ---------------------------------------------------------------- the flows: plates, laid by field
            internal sealed class Plate{public string field,name;public Vector2[] corners;public float offset,dark,seam,bright;public Vector2 centre;}
            static List<Plate> plates;static Dictionary<(int,int),List<Plate>> buckets;
            internal static List<Plate> Plates{get{if(plates==null)Lay();return plates;}}
            /// <summary>Every flow's plates, from the same numbers every run. A channel is cut into quads along a line (a tongue, a wave down it); a field is a grid of quads clipped to the
            /// valley (the delta, the battery's floor). Clocks are laid as the brief has them: a wave from the east down each tongue, a tide from the north-east across the delta, and the
            /// battery's plates each on their own.</summary>
            static void Lay()
            {
                plates=new List<Plate>();var rng=new System.Random(808);float R()=>(float)rng.NextDouble();
                void Channel(string field,Vector2 from,Vector2 to,float half,float row,float dark,float seam,float bright,float wave,float jitter=.25f)
                {
                    float length=Vector2.Distance(from,to);int rows=Mathf.Max(1,Mathf.RoundToInt(length/row));var along=(to-from)/length;var side=new Vector2(-along.y,along.x);
                    var cuts=new Vector2[rows+1];var skew=new float[rows+1];
                    for(int i=0;i<=rows;i++){cuts[i]=from+along*(length*i/rows);skew[i]=i==0||i==rows?0:(R()-.5f)*1.6f;}
                    for(int i=0;i<rows;i++)
                    {
                        var a=cuts[i]+side*half+along*skew[i];var b=cuts[i]-side*half-along*skew[i];var c=cuts[i+1]-side*half-along*skew[i+1];var d=cuts[i+1]+side*half+along*skew[i+1];
                        Add(field,i,new[]{a,b,c,d},-i*wave+(R()-.5f)*jitter,dark,seam,bright);
                    }
                }
                void Grid(string field,float x0,float z0,float w,float h,int nx,int nz,Func<Vector2,bool> keep,Func<Vector2,(float offset,float dark,float seam,float bright)> clock)
                {
                    var corner=new Vector2[nx+1,nz+1];
                    for(int i=0;i<=nx;i++)for(int j=0;j<=nz;j++)corner[i,j]=new Vector2(x0+i*w+(R()-.5f)*w*.36f,z0+j*h+(R()-.5f)*h*.36f);
                    int n=0;
                    for(int j=0;j<nz;j++)for(int i=0;i<nx;i++)
                    {
                        var quad=new[]{corner[i,j],corner[i+1,j],corner[i+1,j+1],corner[i,j+1]};var c=(quad[0]+quad[1]+quad[2]+quad[3])*.25f;
                        if(!keep(c)){R();continue;}
                        var (offset,dark,seam,bright)=clock(c);Add(field,n++,quad,offset,dark,seam,bright);
                    }
                }
                void Add(string field,int i,Vector2[] corners,float offset,float dark,float seam,float bright)
                {
                    // Convex and wound one way, so the field's inside test and the bed agree.
                    float area=0;for(int k=0;k<corners.Length;k++){var a=corners[k];var b=corners[(k+1)%corners.Length];area+=a.x*b.y-b.x*a.y;}
                    if(area<0)Array.Reverse(corners);
                    var c=Vector2.zero;foreach(var p in corners)c+=p;c/=corners.Length;
                    plates.Add(new Plate{field=field,name=field+"-"+i,corners=corners,offset=offset,dark=dark,seam=seam,bright=bright,centre=c});
                }
                // F1, the lesson: a short tongue beside the path, out of the east wall; a wave down it every eleven seconds.
                Channel("F1",new Vector2(25,31.5f),new Vector2(8.5f,31),3.3f,5.4f,5,2.5f,3.5f,1.4f);
                // F2, the first crossing: from under the east wall to under the west one, the bright band walking west past whoever waits.
                Channel("F2",new Vector2(17,47.5f),new Vector2(-16,46.5f),3.1f,6.3f,6,2.5f,3.5f,1.5f);
                // F3, under the garrison's causeway: the crust either side of it is the flank, each plate on its own slower clock.
                Channel("F3E",new Vector2(23,84),new Vector2(2.7f,84),4,6.6f,7,2.5f,3,-3.1f,2);Channel("F3W",new Vector2(-2.7f,84),new Vector2(-21,84),4,6.1f,7,2.5f,3,2.3f,2);
                // F4, the narrow tongue to the cinder hollow, on a faster clock; F5, the hollow's pan, where the crawlers are.
                Channel("F4",new Vector2(11.5f,113),new Vector2(11.5f,97),2.4f,5.3f,3.6f,1.8f,3,1.1f);
                Channel("F5",new Vector2(19.5f,103.5f),new Vector2(29,109.5f),3.4f,5.6f,8,2.5f,3,4);
                // F6, the delta: a tide from the north-east, a band of open melt crossing it every thirteen seconds; round the glass island a faster clock of its own.
                Grid("F6",-29,106,6,6,8,8,c=>Open(c)<-1.2f&&c.y<151.5f&&!(c.x>8&&c.y<117),c=>
                {
                    if(Vector2.Distance(c,GlassIsland)<9.5f)return ((float)(rng.NextDouble()*7.2),3.2f,1.6f,2.4f);
                    float s=Vector2.Dot(c-new Vector2(18,152),new Vector2(-.6f,-.8f));return (-s/5.2f+(R()-.5f)*.5f,7,2.5f,3.5f);
                });
                // F7, the battery's floor: each plate on its own clock, so the ground under a fight changes from moment to moment.
                Grid("F7",-17,168,6,6,6,3,c=>Open(c)<-1,c=>((float)(rng.NextDouble()*12),6+2*R(),2.5f,3));
                // F9, the last tongue before the gate: F2's wave again.
                Channel("F9",new Vector2(15,222.5f),new Vector2(-14,221.5f),3,5.9f,5.5f,2.5f,3.5f,1.4f);
                buckets=new Dictionary<(int,int),List<Plate>>();
                foreach(var plate in plates)
                {
                    float xMin=plate.corners.Min(c=>c.x)-BankRun-1,xMax=plate.corners.Max(c=>c.x)+BankRun+1,zMin=plate.corners.Min(c=>c.y)-BankRun-1,zMax=plate.corners.Max(c=>c.y)+BankRun+1;
                    for(int bx=Mathf.FloorToInt(xMin/8);bx<=Mathf.FloorToInt(xMax/8);bx++)for(int bz=Mathf.FloorToInt(zMin/8);bz<=Mathf.FloorToInt(zMax/8);bz++)
                    {if(!buckets.TryGetValue((bx,bz),out var list))buckets[(bx,bz)]=list=new List<Plate>();list.Add(plate);}
                }
            }
            /// <summary>Metres outside a convex plate (negative inside it).</summary>
            static float Outside(Vector2 p,Vector2[] poly)
            {
                float worst=float.NegativeInfinity;float edge=float.PositiveInfinity;bool inside=true;
                for(int i=0;i<poly.Length;i++)
                {
                    var a=poly[i];var b=poly[(i+1)%poly.Length];var ab=b-a;var n=new Vector2(ab.y,-ab.x).normalized;float s=Vector2.Dot(p-a,n);
                    worst=Mathf.Max(worst,s);if(s>0)inside=false;edge=Mathf.Min(edge,ZoneKit.Segment(p,a,b));
                }
                return inside?worst:edge;
            }
            /// <summary>Metres outside the nearest plate of any flow (negative inside one); far is anything over a few metres.</summary>
            internal static float Flow(Vector2 p)
            {
                if(plates==null)Lay();float d=99;
                if(buckets.TryGetValue((Mathf.FloorToInt(p.x/8),Mathf.FloorToInt(p.y/8)),out var list))foreach(var plate in list)d=Mathf.Min(d,Outside(p,plate.corners));
                return d;
            }
            static float IslandRise(Vector2 p,float under){float h=under;foreach(var island in Islands){float d=Vector2.Distance(p,new Vector2(island.x,island.y));h=Mathf.Max(h,Mathf.Lerp(0,Bed,ZoneKit.Smooth(island.z,island.z+1.3f,d)));}return h;}
            /// <summary>Metres in from the nearest island's rim (0 or less off every island).</summary>
            static float IslandEdge(Vector2 p){float most=0;foreach(var island in Islands)most=Mathf.Max(most,island.z-Vector2.Distance(p,new Vector2(island.x,island.y)));return most;}
            static bool OnIsland(Vector2 p,float margin=0){foreach(var island in Islands)if(Vector2.Distance(p,new Vector2(island.x,island.y))<island.z+margin)return true;return false;}
            /// <summary>The shelf's own height: basalt a hand uneven, and the battery's terrace (with its ramp in the middle) at the head of its field.</summary>
            static float Shelf(Vector2 p)
            {
                float h=.12f*(Mathf.PerlinNoise(p.x*.09f+7,p.y*.09f+2)-.5f)+.05f*(Mathf.PerlinNoise(p.x*.4f+3,p.y*.4f+1)-.5f);
                if(p.y>186&&p.y<206&&p.x>-28&&p.x<18)
                {
                    float front=Mathf.Abs(p.x)<3.2f?ZoneKit.Smooth(187.6f,191.8f,p.y):ZoneKit.Smooth(191.2f,191.7f,p.y);
                    h+=Terrace*front*(1-ZoneKit.Smooth(201,204,p.y));
                }
                return h;
            }
            static readonly Vector2 Away=new(Mathf.Sin(SightlineAudit.Yaw*Mathf.Deg2Rad),Mathf.Cos(SightlineAudit.Yaw*Mathf.Deg2Rad));
            /// <summary>The floor under a point of the valley: shelf, or a flow's bed with a bank up from it, the islands standing out of the beds.</summary>
            static float Floor(Vector2 p)
            {
                float shelf=Shelf(p),flow=Flow(p);float h=flow>=BankRun?shelf:Mathf.Lerp(Bed,shelf,ZoneKit.Smooth(0,BankRun,flow));
                return IslandRise(p,h);
            }
            /// <summary>The ground. Outside the valley the scoria hills: on the camera's side of a floor they stay under the sightline to a hero at its edge (measured from that floor, as the
            /// Waystation's masonry does), elsewhere they stand five to eight metres and the flows come out from under them.</summary>
            internal static float Height(Vector2 p)
            {
                float clear=Open(p);float level=Floor(p);if(clear<=0)return level;
                float allowed=float.PositiveInfinity;
                for(float d=.5f;d<=14;d+=.5f){var q=p+Away*d;if(Open(q)<0){allowed=Floor(q)+.6f+.82f*d;break;}}
                float rough=Mathf.PerlinNoise((p.x+70)*.11f,(p.y+20)*.09f);
                float top=Mathf.Min(allowed,level+3.6f+3.4f*rough);
                return level+(top-level)*Mathf.SmoothStep(0,1,Mathf.Clamp01(clear/1.6f));
            }
            /// <summary>Whether a body could stand here: the valley's floor, off every plate's bed.</summary>
            internal static bool Walkable(Vector2 p)=>Open(p)<-.4f;
            static bool OnPlate(Vector2 p,float margin=0)=>Flow(p)<margin&&!OnIsland(p,-.4f);

            /// <summary>Basalt grey-brown with paler ash where feet and wind leave it, darker toward a bank's foot; a flow's bed near black (it is under the crust anyway).</summary>
            static Color Tone(Vector2 p)
            {
                float flow=Flow(p),mottle=.9f+.2f*Mathf.PerlinNoise(p.x*.19f+3,p.y*.19f+7),ash=ZoneKit.Smooth(.45f,.72f,Mathf.PerlinNoise(p.x*.05f+2,p.y*.05f+40));
                var tone=Color.Lerp(new Color(.36f,.33f,.31f),new Color(.56f,.52f,.48f),ash*.8f);
                tone=Color.Lerp(tone,new Color(.2f,.18f,.17f),ZoneKit.Smooth(BankRun+1.4f,0,flow));
                if(Open(p)>0)tone=Color.Lerp(tone,new Color(.4f,.3f,.26f),ZoneKit.Smooth(0,2,Open(p)));
                return new Color(tone.r*mottle,tone.g*mottle,tone.b*mottle,1);
            }

            // ---------------------------------------------------------------- what it is made of
            internal static Material Basalt,Hills,Crust,Melt,Iron,Setts,Canvas,Coals;
            static readonly Color IronDark=new(.42f,.44f,.47f),IronPale=new(.7f,.72f,.74f),Rust=new(.62f,.32f,.2f),Hazard=new(.95f,.74f,.16f);
            static void Materials()
            {
                // Basalt: dark grey stone full of small gas holes (vesicles), with fine pale ash lying in its hollows. The ground kit's layers lie over it.
                var basalt=ZoneKit.Texture("Vaskar/scoria-basalt",512,(u,v)=>
                {
                    float big=ZoneKit.Cloud(u,v,5,3),fine=ZoneKit.Cloud(u,v,24,9),hole=ZoneKit.Tile(u*70,v*70,70,17),ash=ZoneKit.Cloud(u,v,9,21);
                    float tone=.58f+.12f*(big-.5f)+.14f*(fine-.5f)-(hole<.07f?.22f:hole<.12f?.08f:0)+.05f*ZoneKit.Smooth(.55f,.75f,ash);
                    return new Color(tone*1.02f,tone,tone*.96f,1);
                });
                // The hills: red-brown scoria in rough courses, pitted, laid along each wall so the bands stay level.
                var scoria=ZoneKit.Texture("Vaskar/scoria-wall",512,(u,v)=>
                {
                    float band=Mathf.PerlinNoise(v*14,ZoneKit.Cloud(u,v,3,4)*2),pit=ZoneKit.Tile(u*60,v*60,60,11),grain=ZoneKit.Cloud(u,v,20,8);
                    float tone=.56f+.16f*(band-.5f)+.12f*(grain-.5f)-(pit<.1f?.18f:0);
                    return new Color(tone*1.12f,tone*.82f,tone*.7f,1);
                });
                // Crust: near-black ropy skin (the folds of a flow that crusted while moving) with a glassy sheen in streaks and fine cracks.
                var crust=ZoneKit.Texture("Vaskar/scoria-crust",512,(u,v)=>
                {
                    float rope=Mathf.Abs(Mathf.Sin((u*9+ZoneKit.Cloud(u,v,4,6)*2.2f)*Mathf.PI*2)),glass=ZoneKit.Cloud(u,v,7,13),crack=Mathf.Abs(ZoneKit.Cloud(u,v,11,31)-.5f);
                    float speck=ZoneKit.Tile(u*80,v*80,80,29),fine=Mathf.Abs(ZoneKit.Cloud(u,v,23,47)-.5f);
                    float tone=.52f+.1f*(rope-.5f)+.12f*(ZoneKit.Cloud(u,v,5,3)-.5f)+.16f*ZoneKit.Smooth(.62f,.8f,glass)-(crack<.022f?.3f:0)-(fine<.018f?.22f:0)+(speck>.94f?.16f:0);
                    return new Color(tone,tone*.97f,tone*.96f,1);
                });
                // The melt: slow swirls of pale and hot, never one flat sheet; the same picture is its light.
                var melt=ZoneKit.Texture("Vaskar/scoria-melt",512,(u,v)=>
                {
                    // Molten rock from above: rafts of dark cooling skin with the glow in the cracks between them, brightest at a crack's middle. Mostly dark, so a marking's bright
                    // rim wins by its light. (Whitened so a marking would win, it read as marble: the lead's look pass.) The picture is its own light, scaled by the plate's colour.
                    float warp=ZoneKit.Cloud(u,v,3,2)*.3f,one=Mathf.Abs(ZoneKit.Cloud(u+warp,v,7,5)-.5f),two=Mathf.Abs(ZoneKit.Cloud(u,v+warp,12,17)-.5f)*1.25f;
                    // Thin cracks: at twice this width the glow covered half the plate (look pass 07). Red at a crack's edge, yellow at its middle.
                    float crack=Mathf.Min(one,two),glow=ZoneKit.Smooth(.042f,.006f,crack),skin=.08f+.06f*ZoneKit.Cloud(u,v,9,23);
                    float lit=Mathf.Clamp01(skin+(1-skin)*glow);return new Color(lit,lit*(.42f+.55f*glow),lit*(.18f+.62f*glow),1);
                });
                // Set basalt for the causeway: squared blocks, each its own tone, a dark joint between.
                var setts=ZoneKit.Texture("Vaskar/scoria-setts",256,(u,v)=>
                {
                    float row=v*4;int course=Mathf.FloorToInt(row);float up=row-course,across=u*3+(course%2)*.5f;int block=Mathf.FloorToInt(across);float along=across-block;
                    float own=ZoneKit.Tile(block*2.3f+course*7.1f,course*1.3f,45,11);bool joint=up<.08f||along<.04f;
                    float tone=joint?.26f:.5f+.16f*(own-.5f)+.08f*(ZoneKit.Cloud(u,v,12,3)-.5f);
                    return new Color(tone*1.02f,tone,tone*.97f,1);
                });
                // The Muster's iron: gunmetal plate with rust bleeding down from rivets.
                var iron=ZoneKit.Texture("Vaskar/scoria-iron",256,(u,v)=>
                {
                    float streak=ZoneKit.Cloud(u*4,v*.6f,4,9),rivet=ZoneKit.Tile(u*16,v*16,16,5);float rust=ZoneKit.Smooth(.55f,.75f,streak);
                    float tone=.6f+.1f*(ZoneKit.Cloud(u,v,8,2)-.5f)-(rivet>.94f?.14f:0);
                    return Color.Lerp(new Color(tone,tone*1.02f,tone*1.06f),new Color(tone*1.1f,tone*.62f,tone*.42f),rust*.7f);
                });
                var canvas=ZoneKit.Texture("Vaskar/scoria-canvas",256,(u,v)=>
                {
                    float weave=(Mathf.Sin(u*Mathf.PI*160)*Mathf.Sin(v*Mathf.PI*160))*.04f,stain=ZoneKit.Cloud(u,v,5,3);
                    float tone=.66f+weave+.12f*(stain-.5f);return new Color(tone*1.04f,tone,tone*.9f,1);
                });
                Basalt=ZoneKit.Toon("scoria-floor",basalt,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_BaseColor",Color.white);m.SetFloat("_BaseMapStrength",.9f);m.SetColor("_ShadowTint",new Color(.44f,.45f,.58f));m.SetFloat("_LightFloor",.2f);});
                Hills=ZoneKit.Toon("scoria-wall",scoria,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.82f,.62f,.54f));m.SetColor("_ShadowTint",new Color(.44f,.42f,.56f));m.SetFloat("_LightFloor",.24f);});
                // The slabs carry their joins' light in their vertex alpha (the toon shader's emission is scaled by it), so a seam lights and the slab beside it doesn't. CrustField sets
                // the colour per plate; the material's own is none, so an unposed plate is dark crust.
                Crust=ZoneKit.Toon("scoria-crust",crust,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",3);m.SetColor("_BaseColor",new Color(.5f,.47f,.47f));m.SetColor("_ShadowTint",new Color(.4f,.4f,.52f));
                    m.SetColor("_RimColor",new Color(.5f,.48f,.5f));m.SetFloat("_LightFloor",.16f);m.SetColor("_EmissionColor",Color.black);});
                // The melt is almost all its own light: lit by the sun it would be one flat orange in shade and in sun alike. Pale yellow-white, never a marking's orange or red.
                // Tinted: its vertex alpha is its light, which falls off to a chilled margin along each flow's bank (BuildPlate).
                Melt=ZoneKit.Toon("scoria-melt",melt,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.16f,.1f,.08f));m.SetTexture("_EmissionMap",melt);m.SetColor("_EmissionColor",new Color(1.35f,.75f,.3f));
                    m.SetFloat("_LightFloor",.5f);m.SetTextureScale("_BaseMap",Vector2.one);});
                Setts=ZoneKit.Toon("scoria-setts",setts,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.78f,.74f,.7f));m.SetColor("_ShadowTint",new Color(.44f,.45f,.58f));m.SetFloat("_LightFloor",.24f);});
                Iron=ZoneKit.Toon("scoria-iron",iron,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.82f,.84f,.88f));m.SetColor("_ShadowTint",new Color(.42f,.44f,.56f));m.SetColor("_RimColor",new Color(.5f,.5f,.54f));m.SetFloat("_LightFloor",.26f);});
                Canvas=ZoneKit.Toon("scoria-canvas",canvas,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_BaseColor",new Color(.86f,.78f,.66f));m.SetColor("_ShadowTint",new Color(.46f,.46f,.58f));m.SetFloat("_LightFloor",.3f);});
                Coals=ZoneKit.Toon("scoria-coals",crust,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.3f,.24f,.22f));m.SetColor("_EmissionColor",new Color(1.1f,.42f,.14f));});
                AssetDatabase.SaveAssets();
            }

            // ---------------------------------------------------------------- the crust, built
            /// <summary>Clips a polygon to the side of a line where dot(p,n) is at most c.</summary>
            static List<Vector2> Clip(List<Vector2> poly,Vector2 n,float c)
            {
                var o=new List<Vector2>();
                for(int i=0;i<poly.Count;i++)
                {
                    var a=poly[i];var b=poly[(i+1)%poly.Count];float da=Vector2.Dot(a,n)-c,db=Vector2.Dot(b,n)-c;
                    if(da<=0)o.Add(a);if(da<=0!=db<=0)o.Add(Vector2.Lerp(a,b,da/(da-db)));
                }
                return o;
            }
            /// <summary>A plate's crack seeds: a jittered grid inside it, a quarter left out so slabs come in several sizes, kept off the islands.</summary>
            static List<Vector2> Seeds(Plate plate,int seed)
            {
                var rng=new System.Random(seed);float R()=>(float)rng.NextDouble();var seeds=new List<Vector2>();
                float xMin=plate.corners.Min(q=>q.x),xMax=plate.corners.Max(q=>q.x),zMin=plate.corners.Min(q=>q.y),zMax=plate.corners.Max(q=>q.y);const float pitch=1.25f;
                for(float x=xMin+pitch*.5f;x<xMax;x+=pitch)for(float z=zMin+pitch*.5f;z<zMax;z+=pitch)
                {var s=new Vector2(x+(R()-.5f)*pitch*.7f,z+(R()-.5f)*pitch*.7f);bool keep=R()>.24f;if(keep&&Outside(s,plate.corners)<-.15f&&!OnIsland(s,.2f)&&Open(s)<1)seeds.Add(s);}
                if(seeds.Count==0)seeds.Add(plate.centre);
                return seeds;
            }
            /// <summary>Whether a plate's edge from a to b is also an edge of another plate of its flow.</summary>
            static bool Shared(Plate plate,Vector2 a,Vector2 b,List<Plate> field)
            {
                foreach(var other in field)
                {
                    if(other==plate)continue;
                    for(int k=0;k<other.corners.Length;k++){var c=other.corners[k];var d=other.corners[(k+1)%other.corners.Length];if((c-b).sqrMagnitude<.0025f&&(d-a).sqrMagnitude<.0025f||(c-a).sqrMagnitude<.0025f&&(d-b).sqrMagnitude<.0025f)return true;}
                }
                return false;
            }
            /// <summary>The slabs of a plate: the cells of the whole flow's crack pattern whose seed is in this plate. A cell is cut by the plate's own edge where that edge is the flow's
            /// (the bank), and runs on past it into the neighbour where the two plates meet, until the neighbour's own cracks stop it: so what lights and opens is a slab of crust with an
            /// interlocking, cracked outline, not a tile (the lead's look pass: a battery of straight-edged plates read as a game board lighting up).</summary>
            static List<List<Vector2>> Cells(Plate plate,List<Plate> field,List<Vector2> own,List<Vector2> all)
            {
                var cells=new List<List<Vector2>>();
                float xMin=plate.corners.Min(q=>q.x)-3,xMax=plate.corners.Max(q=>q.x)+3,zMin=plate.corners.Min(q=>q.y)-3,zMax=plate.corners.Max(q=>q.y)+3;
                foreach(var s in own)
                {
                    var cell=new List<Vector2>{new(xMin,zMin),new(xMax,zMin),new(xMax,zMax),new(xMin,zMax)};
                    for(int k=0;k<plate.corners.Length&&cell.Count>2;k++)
                    {
                        var a=plate.corners[k];var b=plate.corners[(k+1)%plate.corners.Length];var ab=b-a;var n=new Vector2(ab.y,-ab.x).normalized;
                        cell=Clip(cell,n,Vector2.Dot(a,n)+(Shared(plate,a,b,field)?2:0));
                    }
                    foreach(var t in all){if(cell.Count<3)break;var d=t-s;if(d.sqrMagnitude<1e-6f||d.sqrMagnitude>20)continue;cell=Clip(cell,d,Vector2.Dot((s+t)*.5f,d));}
                    if(cell.Count>2)cells.Add(cell);
                }
                return cells;
            }
            /// <summary>One plate's two renderers, both meshes of its own cells: the melt under it (a sheet at the bed, its texture rafts of dark skin with the glow in the cracks between
            /// them) and its slabs (each cell a slab with a dark join round it whose vertex alpha carries the seam's light, fading into the slab). Both lie lower than a danger marking is
            /// laid (six centimetres over the floor), so a marking is never under them.</summary>
            static (Renderer melt,Renderer slabs) BuildPlate(ZoneKit kit,Plate plate,List<List<Vector2>> cells,Transform under,int seed,Func<Vector2,float> fromBank)
            {
                var rng=new System.Random(seed);float R()=>(float)rng.NextDouble();
                GameObject Body(string name,List<Vector3> points,List<Color> tones,List<int> tris,Material material)
                {
                    var mesh=new Mesh{name=name};mesh.SetVertices(points);mesh.SetColors(tones);mesh.SetUVs(0,points.Select(q=>new Vector2(q.x,q.z)*(material==Melt?.22f:.3f)).ToList());
                    mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
                    mesh=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+kit.zone+"-"+name.Replace(' ','-')+".asset");
                    var body=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));body.GetComponent<MeshFilter>().sharedMesh=mesh;var view=body.GetComponent<MeshRenderer>();view.sharedMaterial=material;
                    view.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;body.transform.SetParent(under,true);return body;
                }
                var points=new List<Vector3>();var tones=new List<Color>();var tris=new List<int>();
                int V(Vector3 q,Color tone){points.Add(q);tones.Add(tone);return points.Count-1;}
                void Tri(int a,int b,int c){bool up=Vector3.Cross(points[b]-points[a],points[c]-points[a]).y>=0;tris.Add(a);tris.Add(up?b:c);tris.Add(up?c:b);}
                // The melt: each cell's own fan, so the open melt has the slabs' outline. Where a cell meets the flow's own bank its light falls off toward the rock (the melt's
                // material reads the vertex alpha as its light), unevenly along the bank: the open melt meets the rock along a chilled, ragged margin and not a straight cut (s47/153).
                Color Chill(Vector2 q){float d=fromBank(q);if(d>.7f)return Color.white;float n=Mathf.PerlinNoise(q.x*.8f+5,q.y*.8f+11),a=Mathf.Lerp(.08f+.42f*n,1,ZoneKit.Smooth(.05f,.7f,d));return new Color(1,1,1,a);}
                foreach(var cell in cells)
                {
                    var mid=Vector2.zero;foreach(var q in cell)mid+=q;mid/=cell.Count;int centre=V(new Vector3(mid.x,Bed+.015f,mid.y),Color.white);
                    var inner=cell.Select(q=>{var at=Vector2.Lerp(mid,q,.6f);return V(new Vector3(at.x,Bed+.015f,at.y),Chill(at));}).ToArray();
                    var ring=cell.Select(q=>V(new Vector3(q.x,Bed+.015f,q.y),Chill(q))).ToArray();
                    for(int k=0;k<ring.Length;k++){int l=(k+1)%ring.Length;Tri(centre,inner[k],inner[l]);Tri(inner[k],ring[k],ring[l]);Tri(inner[k],ring[l],inner[l]);}
                }
                var meltBody=Body("Crust "+plate.name+" melt",points,tones,tris,Melt);
                // The slabs. A Shape gives a triangle one tone, and a uniform join read as a marking's outline (blockout still first-crossing): so a join's light fades into its slab.
                points=new List<Vector3>();tones=new List<Color>();tris=new List<int>();
                foreach(var cell in cells)
                {
                    var mid=Vector2.zero;foreach(var q in cell)mid+=q;mid/=cell.Count;
                    // Slabs of several tones and casts (grey, brown, blue-black glass): a field of one tone was plain floor to the eye and to look_density.
                    float own=.62f+.5f*R(),cast=R();var face=new Color(own*(1+.08f*cast),own*(1-.02f*cast),own*(1-.06f+.12f*(1-cast)),0);var crack=new Color(.16f,.14f,.14f,1);
                    // A join on the flow's own bank carries less of the light than the cracks inside: lit in full it drew a glowing edge round the field.
                    var outer=cell.Select(q=>V(new Vector3(q.x,Bed+.03f,q.y),Flow(q)>-.05f?new Color(crack.r,crack.g,crack.b,.35f):crack)).ToArray();
                    var inner=cell.Select(q=>{var toward=mid-q;float run=Mathf.Min(.15f,toward.magnitude*.36f)*(.8f+.4f*R());var at=q+toward.normalized*run;return V(new Vector3(at.x,Bed+.046f,at.y),face);}).ToArray();
                    int top=V(new Vector3(mid.x,Bed+.054f,mid.y),face);
                    for(int k=0;k<cell.Count;k++){int l=(k+1)%cell.Count;Tri(outer[k],outer[l],inner[l]);Tri(outer[k],inner[l],inner[k]);Tri(inner[k],inner[l],top);}
                }
                var slabBody=Body("Crust "+plate.name+" slabs",points,tones,tris,Crust);
                return (meltBody.GetComponent<Renderer>(),slabBody.GetComponent<Renderer>());
            }

            /// <summary>The levee along every flow's bank (its plates' unshared edges, each with its outward side): a band of black ropy crust from the bank's foot out over the slope and
            /// a little onto the shelf, between half a metre and a metre and a half wide as the noise has it, its colour going from the crust's near-black to the ash's grey at its outer
            /// edge, three centimetres over the ground's own collider (a marking is laid six over it). The flows' plates had ended in straight cuts against the sand (s47/152 to 154).</summary>
            static void Levees(ZoneKit kit,List<(Vector2 a,Vector2 b,Vector2 n)> bank,Transform under)
            {
                var ground=UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None).FirstOrDefault(c=>c.name=="Scoria valley");
                if(ground==null){Debug.LogWarning("SCORIA_LEVEES no valley collider");return;}
                Physics.SyncTransforms();
                float On(Vector2 p,float fallback){var ray=new Ray(new Vector3(p.x,30,p.y),Vector3.down);return ground.Raycast(ray,out var hit,80)?hit.point.y:fallback;}
                var points=new List<Vector3>();var tones=new List<Color>();var tris=new List<int>();int strips=0;
                int V(Vector2 p,float lift,Color tone){points.Add(new Vector3(p.x,On(p,Bed)+lift,p.y));tones.Add(tone);return points.Count-1;}
                void Tri(int a,int b,int c){bool up=Vector3.Cross(points[b]-points[a],points[c]-points[a]).y>=0;tris.Add(a);tris.Add(up?b:c);tris.Add(up?c:b);}
                var black=new Color(.2f,.19f,.2f,1);var crust=new Color(.3f,.29f,.3f,1);var ash=new Color(.46f,.44f,.43f,1);
                foreach(var (a,b,n) in bank)
                {
                    float length=Vector2.Distance(a,b);if(length<.05f)continue;var along=(b-a)/length;int steps=Mathf.Max(2,Mathf.CeilToInt((length+.6f)/.45f));
                    float lift=.028f+.004f*(strips%2);int[] inner=null,mid=null,outer=null;
                    for(int i=0;i<=steps;i++)
                    {
                        float s=-.3f+(length+.6f)*i/steps;var q=a+along*s;float taper=Mathf.Clamp01(Mathf.Min(s+.3f,length+.3f-s)/.3f);
                        float wide=(.4f+.95f*Mathf.PerlinNoise(q.x*.6f+3,q.y*.6f+7)+.3f*Mathf.Max(0,Mathf.PerlinNoise(q.x*1.7f+1,q.y*1.7f+9)-.55f))*Mathf.Lerp(.35f,1,taper);
                        // Its skin a little apart in tone along the bank, as cooled lava is (one even black read as a strip of tar).
                        float own=.82f+.36f*Mathf.PerlinNoise(q.x*1.9f+4,q.y*1.9f+2);
                        int ii=V(q+n*.02f,lift,ZoneKit.Shape.Dim(black,own)),mm=V(q+n*wide*(.45f+.1f*Mathf.PerlinNoise(q.x*2.3f,q.y*2.3f)),lift,ZoneKit.Shape.Dim(crust,.9f+.3f*Mathf.PerlinNoise(q.x*3.1f,q.y*3.1f+5))),oo=V(q+n*wide,lift-.008f,ash);
                        if(inner!=null){Tri(inner[0],mid[0],ii);Tri(ii,mid[0],mm);Tri(mid[0],outer[0],mm);Tri(mm,outer[0],oo);}
                        inner=new[]{ii};mid=new[]{mm};outer=new[]{oo};
                    }
                    strips++;
                }
                if(tris.Count==0)return;
                var mesh=new Mesh{name="Flow levees",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(points);mesh.SetColors(tones);mesh.SetUVs(0,points.Select(p=>new Vector2(p.x,p.z)*.3f).ToList());
                mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+kit.zone+"-Levees.asset");
                var body=new GameObject("Flow levees",typeof(MeshFilter),typeof(MeshRenderer));body.GetComponent<MeshFilter>().sharedMesh=mesh;var view=body.GetComponent<MeshRenderer>();
                view.sharedMaterial=Crust;view.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;body.transform.SetParent(under,true);
                Debug.Log("SCORIA_LEVEES strips="+strips+" vertices="+points.Count);
            }
            /// <summary>The whole crust: every plate's renderers under one root and the field that runs them, with the blisters over the hides. Each flow's crack pattern is laid across
            /// the whole flow at once (seeds plate by plate, cells against every seed of the flow), so neighbouring plates interlock along their cracks.</summary>
            static CrustField Crusts(ZoneKit kit,IEnumerable<Vector2> hides)
            {
                var root=new GameObject("The crust");var field=root.AddComponent<CrustField>();var list=new List<CrustField.Plate>();
                var seedsOf=new Dictionary<Plate,List<Vector2>>();int seed=900;foreach(var plate in Plates)seedsOf[plate]=Seeds(plate,seed++);
                var flows=Plates.GroupBy(p=>p.field).ToDictionary(g=>g.Key,g=>g.ToList());
                var allOf=flows.ToDictionary(f=>f.Key,f=>f.Value.SelectMany(p=>seedsOf[p]).ToList());
                seed=1900;
                // Each flow's bank: the edges of its plates no other plate of the flow shares, each with its outward side.
                var banks=flows.ToDictionary(f=>f.Key,f=>{var edges=new List<(Vector2 a,Vector2 b,Vector2 n)>();foreach(var plate in f.Value)for(int k=0;k<plate.corners.Length;k++){var a=plate.corners[k];var b=plate.corners[(k+1)%plate.corners.Length];
                    if(!Shared(plate,a,b,f.Value)){var ab=b-a;edges.Add((a,b,new Vector2(ab.y,-ab.x).normalized));}}return edges;});
                foreach(var plate in Plates)
                {
                    var cells=Cells(plate,flows[plate.field],seedsOf[plate],allOf[plate.field]);var bank=banks[plate.field];
                    var (melt,slabs)=BuildPlate(kit,plate,cells,root.transform,seed++,q=>{float d=99;foreach(var (a,b,_) in bank)d=Mathf.Min(d,ZoneKit.Segment(q,a,b));return d;});
                    list.Add(new CrustField.Plate{name=plate.name,corners=plate.corners,floor=Bed,offset=plate.offset,dark=plate.dark,seam=plate.seam,bright=plate.bright,melt=melt,slabs=slabs,seeds=seedsOf[plate].ToArray()});
                }
                field.plates=list.ToArray();field.islands=Islands;
                // Hotter than its crust (s47/153): the open melt yellower than any seam and the brightest thing in the valley, a seam about to go no more than orange, so a field reads
                // dark crust, red seam, yellow melt. A seam once ended brighter and whiter than the melt it opened onto. The melt's own brightness is what it was (a marking on it has to
                // read: ScoriaTests.AMarkingOnBrightFlowStillReadsAsAMarking); its hue is what is hotter.
                field.open=new Color(1.22f,.88f,.4f);field.seamEnd=new Color(1.15f,.52f,.17f);
                // The levee: where a flow meets the rock it has chilled and set in a ragged band of black ropy crust over the bank, wider and narrower as it ran, its outer edge going
                // into the ash. Drawn a hand under any marking, on the ground's own collider.
                Levees(kit,banks.Values.SelectMany(e=>e).ToList(),root.transform);
                // A blister of heaved crust over each hide: lifted while something is under it, flat (not drawn) while nothing is.
                var heaves=new List<Transform>();int n=0;
                foreach(var at in hides)
                {
                    var shape=new ZoneKit.Shape();var rng=new System.Random(40+n);
                    shape.Slot(0).Lump(Vector3.zero,new Vector3(1.25f,.42f,1.05f),40+n,.32f,new Color(.82f,.78f,.76f,1),new Color(.5f,.48f,.46f,1),12,6);
                    for(int k=0;k<5;k++){float a=k*72+(float)rng.NextDouble()*30;var d=Quaternion.Euler(0,a,0)*Vector3.forward;shape.Slot(0).Lump(d*1.1f+Vector3.down*.05f,new Vector3(.5f,.16f,.4f),60+n*5+k,.3f,new Color(.7f,.66f,.64f,1),new Color(.46f,.44f,.42f,1),8,4,.25f,Quaternion.Euler(0,a,0));}
                    var heave=kit.Built("Crust heave "+n++,shape,Crust);heave.transform.position=new Vector3(at.x,Bed+.02f,at.y);heave.transform.SetParent(root.transform,true);
                    heaves.Add(heave.transform);
                }
                field.heaves=heaves.ToArray();
                return field;
            }

            // ---------------------------------------------------------------- pieces
            /// <summary>The enemy if a combat engineer has defined it, else a stand-in of the same family (logged), so the zone is walkable and fought before the roster lands.</summary>
            static string Enemy(string id)
            {
                if(ZoneKit.Find<EnemyDef>(id)!=null)return id;
                string stand=id switch{"MusterPikeguard"=>"ThawedGuard","MusterMortar"=>"Sporecaster","AshCrawler"=>"RimeStalker","ReachScav"=>"MusterScav","ReachSergeant"=>"MusterSergeant",_=>id};
                Debug.Log("SCORIA_STAND_IN "+stand+" for "+id);return stand;
            }
            static EncounterVolume Fight(ZoneKit kit,string id,Vector2 at,Vector2 size,params (string enemy,int count,Vector2 from,float yaw,float radius)[] packs)
            {
                var encounter=WorldBuilder.Encounter(id,kit.On(at.x,at.y,1.5f),new Vector3(size.x,7,size.y),Enemy(packs[0].enemy),packs[0].count);
                var spawners=new List<Spawner>{encounter.spawners[0]};
                for(int i=1;i<packs.Length;i++)spawners.Add(ZoneKit.Spawn(encounter,Enemy(packs[i].enemy),packs[i].count,Vector3.zero,2.4f));
                for(int i=0;i<packs.Length;i++)
                {
                    var spawner=spawners[i];spawner.transform.SetPositionAndRotation(kit.On(packs[i].from.x,packs[i].from.y),Quaternion.Euler(0,packs[i].yaw,0));
                    spawner.faceAsPlaced=true;spawner.radius=packs[i].count>1?packs[i].radius:0;
                }
                encounter.spawners=spawners.ToArray();encounter.requiredFlag="";return encounter;
            }
            static void Hide(Vector2 at){var spot=new GameObject("Crawler hide "+at.x+" "+at.y,typeof(AmbushSpot));spot.transform.position=new Vector3(at.x,Bed,at.y);spot.GetComponent<AmbushSpot>().radius=3.6f;}
            /// <summary>A plate's centre near a point, for a hide: something to be under.</summary>
            static Vector2 PlateNear(Vector2 at,string field){var best=Plates.Where(p=>p.field==field&&!OnIsland(p.centre,1.5f)).OrderBy(p=>Vector2.Distance(p.centre,at)).First();return best.centre;}
            /// <summary>A cache: a Muster ammunition chest, or a clan's bundle tied in canvas.</summary>
            static LootCache Stash(ZoneKit kit,string id,string label,Vector2 at,float yaw,int scrip,bool chest,params LootEntry[] contents)
            {
                var shape=new ZoneKit.Shape();
                if(chest)
                {
                    shape.Slot(0).Box(new Vector3(0,.34f,0),new Vector3(1.3f,.68f,.78f),Quaternion.identity,IronDark,IronPale).Box(new Vector3(0,.74f,0),new Vector3(1.36f,.14f,.84f),Quaternion.identity,IronPale);
                    foreach(float x in new[]{-.5f,.5f})shape.Slot(0).Box(new Vector3(x,.4f,0),new Vector3(.12f,.84f,.86f),Quaternion.identity,Hazard);
                }
                else
                {
                    shape.Slot(1).Lump(new Vector3(0,.34f,0),new Vector3(.66f,.36f,.5f),7,.12f,Color.white,new Color(.7f,.64f,.54f),10,6);
                    shape.Slot(1).Rope(new Vector3(-.66f,.5f,0),new Vector3(.66f,.5f,0),-.25f,.035f,new Color(.5f,.36f,.24f));
                    shape.Slot(0).Box(new Vector3(.5f,.12f,.3f),new Vector3(.5f,.24f,.36f),Quaternion.Euler(0,20,0),IronDark,IronPale);
                }
                var body=kit.Built((chest?"Chest ":"Bundle ")+Mathf.RoundToInt(at.x)+" "+Mathf.RoundToInt(at.y),shape,Iron,Canvas);
                body.transform.SetPositionAndRotation(kit.On(at.x,at.y),Quaternion.Euler(0,yaw,0));
                var box=body.AddComponent<BoxCollider>();box.center=Vector3.up*.42f;box.size=new Vector3(1.4f,.84f,.9f);body.isStatic=true;
                var cache=LootCache.On(body,id,label,scrip,contents);cache.range=2.6f;return cache;
            }
            /// <summary>Scoria Glass: black volcanic glass broken out of the shelf in shards, gone once gathered.</summary>
            static void Glass(ZoneKit kit,string id,Vector2 at,int seed)
            {
                var shape=new ZoneKit.Shape();var rng=new System.Random(seed);
                for(int k=0;k<6;k++)
                {
                    float a=(float)rng.NextDouble()*6.28f,tall=.25f+.35f*(float)rng.NextDouble();var foot=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(.15f+.3f*(float)rng.NextDouble());
                    var tip=foot+new Vector3(((float)rng.NextDouble()-.5f)*.25f,tall,((float)rng.NextDouble()-.5f)*.25f);
                    shape.Slot(0).Tube(new[]{foot+Vector3.down*.05f,Vector3.Lerp(foot,tip,.6f),tip},new[]{.1f,.07f,.01f},5,new Color(.3f,.3f,.36f),new Color(.6f,.62f,.72f),1,1,false);
                }
                var body=kit.Built("Scoria glass "+id,shape,Crust);body.transform.position=kit.On(at.x,at.y);
                var solid=body.AddComponent<BoxCollider>();solid.center=Vector3.up*.2f;solid.size=new Vector3(.9f,.4f,.9f);body.isStatic=true;
                var node=body.AddComponent<MineNode>();node.nodeId=id;node.material="ScoriaGlass";node.amount=2;node.prompt="Gather Scoria Glass";node.range=2.6f;
            }
            /// <summary>A garrison's cookfire behind its pike rack: a ring of stones, coals that glow, a kettle on its flat stone (where families leave messages), the rack beside it.
            /// The hand is the kettle stone's discovery, offered only while Quiet Hands is at its first step.</summary>
            static void Cookfire(ZoneKit kit,string name,Vector2 at,float yaw,string flag)
            {
                var shape=new ZoneKit.Shape();var turn=Quaternion.Euler(0,yaw,0);
                // The ring of smoke-blackened stones round a bed of ash, an iron tripod over it with the garrison's pot, and the fire itself (Hearths.Fire): it was a flat orange
                // polygon in the stones (s46/166), and a kettle-stone errand is built round three of them.
                Hearths.Ring(shape.Slot(0),Vector3.zero,.64f,9,name.Length*17,new Color(.7f,.66f,.62f),new Color(.28f,.26f,.25f));
                shape.Slot(0).Lump(Vector3.up*.01f,new Vector3(.54f,.05f,.54f),5,.3f,new Color(.6f,.58f,.56f),new Color(.36f,.34f,.33f),10,4);
                for(int k=0;k<3;k++){float a=(k*120+yaw+30)*Mathf.Deg2Rad;var leg=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.78f;shape.Slot(1).Beam(leg,Vector3.up*1.25f,.04f,.04f,IronDark,IronPale);}
                shape.Slot(1).Beam(Vector3.up*1.25f,Vector3.up*.92f,.02f,.02f,IronPale).Post(Vector3.up*.62f,.3f,.2f,.24f,10,IronDark,IronDark);
                // The kettle stone: a flat slab beside the fire with the kettle on it.
                var stone=turn*new Vector3(1.05f,0,.15f);shape.Slot(0).Box(stone+Vector3.up*.1f,new Vector3(.62f,.2f,.5f),turn,new Color(.6f,.58f,.56f),new Color(.74f,.72f,.7f));
                shape.Slot(1).Post(stone+Vector3.up*.2f,.26f,.16f,.12f,8,IronDark,IronPale).Disc(stone+Vector3.up*.47f,Vector3.up,.12f,8,IronPale);
                // The pike rack: two uprights, a bar, five pikes leaning in it.
                var rack=turn*new Vector3(-1.3f,0,.7f);var across=turn*Vector3.right;
                shape.Slot(1).Post(rack-across*.7f,1.3f,.05f,.05f,6,IronDark,IronPale).Post(rack+across*.7f,1.3f,.05f,.05f,6,IronDark,IronPale).Beam(rack-across*.75f+Vector3.up*1.1f,rack+across*.75f+Vector3.up*1.1f,.06f,.06f,IronDark);
                for(int k=0;k<5;k++){var foot=rack-across*.55f+across*(k*.28f)+turn*Vector3.back*.25f;shape.Slot(1).Tube(new[]{foot,foot+Vector3.up*1.2f+turn*Vector3.forward*.25f,foot+Vector3.up*2.5f+turn*Vector3.forward*.5f},new[]{.025f,.025f,.012f},4,IronDark,IronPale,1,1,false);}
                var body=kit.Built(name,shape,Setts,Iron);body.transform.position=kit.On(at.x,at.y);
                ZoneKit.Block(body,kit.On(at.x,at.y,.15f),new Vector3(1.4f,.3f,1.4f),yaw,"Fire ring");
                Hearths.Fire(kit,name+" fire",kit.On(at.x,at.y,.07f),.36f,.6f,6,1.5f,name.Length*31).transform.SetParent(body.transform,true);
                var hand=kit.On(at.x,at.y)+stone+turn*Vector3.back*.9f;
                var point=kit.Discovery(name+" kettle stone",new Vector3(hand.x,0,hand.z),"Leave it under the kettle stone",flag,"KettleStone",null,"Taren","QuietHands",0,2.6f);point.situation="message";
            }
            /// <summary>Brekk's stall, of Vaskar make: four iron posts under a roof of overlapping hide and iron plates gone black with soot, a counter of iron on setts with the stock on
            /// it, and a smith's red cloth hung from the roof's front. (The kit's market awning, a green leaf roof, read as borrowed from Xylos: the lead's look pass.)</summary>
            static void Stall(ZoneKit kit,Vector2 at,float yaw)
            {
                var shape=new ZoneKit.Shape();var turn=Quaternion.Euler(0,yaw,0);var foot=kit.On(at.x,at.y);Vector3 P(float x,float y,float z)=>foot+turn*new Vector3(x,y,z);
                foreach(var (x,z) in new[]{(-1.5f,-.9f),(1.5f,-.9f),(-1.5f,.9f),(1.5f,.9f)})shape.Slot(0).Post(P(x,0,z),z<0?2.5f:2.1f,.06f,.06f,6,IronDark,IronPale);
                // The roof: rafters front to back, and between them hides that sag in their bays with their hems rolled at the front, under iron plates riveted down over every
                // other bay, their front edges turned down, gone black with soot. Laid as five flat boards it read from above as one brown plane (whole-game capture s49/194).
                var soot=new Color(.2f,.18f,.18f);var hide=new Color(.38f,.25f,.17f);
                for(int k=0;k<6;k++){float x=-2f+k*.8f;shape.Slot(0).Beam(P(x,2.56f,-1.14f),P(x,2.16f,1.12f),.07f,.07f,IronDark,IronPale);}
                float Roof(float x,float z,float bay){float t=Mathf.Clamp01((z+1.1f)/2.2f),across=1-Mathf.Pow(Mathf.Clamp((x-bay)/.42f,-1,1),2);return Mathf.Lerp(2.55f,2.15f,t)-.17f*across*(.35f+.65f*Mathf.Sin(t*Mathf.PI));}
                for(int k=0;k<5;k++)
                {
                    float x=-1.6f+k*.8f;
                    if(k%2==1)
                    {
                        var slope=turn*Quaternion.Euler(Mathf.Atan2(.4f,2.26f)*Mathf.Rad2Deg,0,0);
                        shape.Slot(0).Box(P(x,2.4f,0),new Vector3(1.02f,.035f,2.34f),slope,ZoneKit.Shape.Dim(soot,.8f),soot).Box(P(x,2.5f,-1.18f),new Vector3(1.02f,.16f,.035f),turn,ZoneKit.Shape.Dim(soot,.7f),soot);
                        for(int i=0;i<6;i++)foreach(int e in new[]{-1,1})shape.Slot(0).Box(P(x+e*.43f,2.42f-i*.072f,-1.05f+i*.42f),new Vector3(.05f,.03f,.05f),slope,IronPale,IronPale);
                    }
                    else
                    {
                        for(int i=0;i<5;i++)for(int j=0;j<4;j++)
                        {
                            float z0=-1.1f+i*.44f,z1=z0+.44f,xa=x-.44f+j*.22f,xb=xa+.22f;var tone=ZoneKit.Shape.Dim(hide,.74f+.08f*((i*3+j*5+k)%4));
                            shape.Slot(1).Quad(P(xa,Roof(xa,z0,x),z0),P(xb,Roof(xb,z0,x),z0),P(xb,Roof(xb,z1,x),z1),P(xa,Roof(xa,z1,x),z1),turn*Vector3.up,tone,.5f);
                        }
                        shape.Slot(1).Tube(new[]{P(x-.45f,2.53f,-1.13f),P(x+.45f,2.53f,-1.13f)},new[]{.05f,.05f},6,ZoneKit.Shape.Dim(hide,.7f),ZoneKit.Shape.Dim(hide,.7f));
                    }
                }
                shape.Slot(0).Box(P(0,.5f,-.6f),new Vector3(2.8f,.12f,.7f),turn,IronDark,IronPale).Box(P(0,.22f,-.6f),new Vector3(2.6f,.44f,.62f),turn,new Color(.5f,.48f,.46f),new Color(.6f,.58f,.56f));
                foreach(float x in new[]{-.9f,-.2f,.6f})shape.Slot(0).Box(P(x,.66f,-.6f),new Vector3(.36f,.22f,.3f),turn*Quaternion.Euler(0,x*30,0),IronPale,Rust);
                shape.Slot(1).Quad(P(-.9f,2.5f,-1.08f),P(.9f,2.5f,-1.08f),P(.9f,1.85f,-1.1f),P(-.9f,1.85f,-1.1f),turn*Vector3.back,new Color(.62f,.12f,.08f),.5f);
                var body=kit.Built("Brecks stall",shape,Iron,Canvas);body.name="Brekk's stall";
                ZoneKit.Block(body,P(0,.35f,-.6f),new Vector3(2.8f,.7f,.8f),yaw,"Stall counter");
            }
            /// <summary>A Muster lean-to of canvas over iron poles, open to the south.</summary>
            static GameObject LeanTo(ZoneKit kit,string name,Vector2 at,float yaw,float wide=3.6f,float deep=2.6f,float tall=2.1f)
            {
                // The Muster's own lean-to where the kit has it: a canvas roof built here read from above as a flat tan box (blockout still camp).
                if(ZoneKit.Landed("MusterLeanTo")){var kitTent=kit.Prop("MusterLeanTo",new Vector3(at.x,0,at.y),tall+.3f,yaw+180);kitTent.name=name;return kitTent;}
                var shape=new ZoneKit.Shape();var turn=Quaternion.Euler(0,yaw,0);Vector3 P(float x,float y,float z)=>turn*new Vector3(x,y,z);
                foreach(float x in new[]{-wide*.5f,wide*.5f}){shape.Slot(0).Post(P(x,0,-deep*.5f),tall,.05f,.05f,6,IronDark,IronPale).Post(P(x,0,deep*.5f),tall*.55f,.05f,.05f,6,IronDark,IronPale);}
                shape.Slot(1).Quad(P(-wide*.55f,tall,-deep*.5f),P(wide*.55f,tall,-deep*.5f),P(wide*.55f,tall*.55f,deep*.55f),P(-wide*.55f,tall*.55f,deep*.55f),Vector3.up,new Color(.84f,.8f,.7f),.4f);
                shape.Slot(1).Quad(P(-wide*.55f,tall*.55f,deep*.55f),P(wide*.55f,tall*.55f,deep*.55f),P(wide*.55f,0,deep*.6f),P(-wide*.55f,0,deep*.6f),turn*Vector3.forward,new Color(.74f,.7f,.62f),.4f);
                var body=kit.Built(name,shape,Iron,Canvas);body.transform.position=kit.On(at.x,at.y);
                ZoneKit.Block(body,kit.On(at.x,at.y,tall*.3f)+turn*new Vector3(0,0,deep*.3f),new Vector3(wide,tall*.6f,deep*.6f),yaw,"Lean-to");
                return body;
            }
            /// <summary>Two basalt pylons and a lintel: the Foundry's gate (an iron door between, its hazard bands toward the valley), or the town road's posts (no door).</summary>
            static GameObject Gateway(ZoneKit kit,string name,Vector2 at,float yaw,float wide,float tall,bool door)
            {
                var shape=new ZoneKit.Shape();var turn=Quaternion.Euler(0,yaw,0);var foot=kit.On(at.x,at.y);
                foreach(int side in new[]{-1,1})shape.Slot(0).Box(foot+turn*new Vector3(side*(wide*.5f+.7f),tall*.5f,0),new Vector3(1.4f,tall,1.6f),turn,new Color(.62f,.6f,.58f),new Color(.74f,.72f,.7f));
                shape.Slot(0).Box(foot+turn*new Vector3(0,tall+.4f,0),new Vector3(wide+3.2f,.8f,1.8f),turn,new Color(.58f,.56f,.54f),new Color(.7f,.68f,.66f));
                if(door)
                {
                    shape.Slot(1).Box(foot+turn*new Vector3(0,tall*.5f,.25f),new Vector3(wide,tall,.3f),turn,IronDark,IronPale);
                    for(int k=0;k<5;k++)shape.Slot(1).Box(foot+turn*new Vector3(-wide*.4f+k*wide*.2f,.5f,.06f),new Vector3(wide*.1f,.6f,.06f),turn*Quaternion.Euler(0,0,30),Hazard);
                    shape.Slot(1).Box(foot+turn*new Vector3(0,tall*.55f,.07f),new Vector3(wide*.9f,.12f,.06f),turn,Rust);
                }
                var body=kit.Built(name,shape,Setts,Iron);
                foreach(int side in new[]{-1,1})ZoneKit.Block(body,foot+turn*new Vector3(side*(wide*.5f+.7f),tall*.5f,0),new Vector3(1.4f,tall,1.6f),yaw,"Pylon");
                if(door)ZoneKit.Block(body,foot+turn*new Vector3(0,tall*.5f,.3f),new Vector3(wide,tall,.5f),yaw,"Door");
                // The way is offered from where the gate stands: a built body sits at the world's origin (its shape is in world places), and a prompt is measured from its own
                // object, so on the body the Foundry gate and the town road were offered at the landing and nowhere near either gate (RouteFightTests, November 14).
                var holder=new GameObject(name);holder.transform.position=foot+Vector3.up*.2f;body.name=name+" drawn";body.transform.SetParent(holder.transform,true);
                return holder;
            }
            /// <summary>The garrison's causeway: squared basalt laid across F3 between its two halves, kerbed both sides.</summary>
            static void CausewayBody(ZoneKit kit)
            {
                var shape=new ZoneKit.Shape();
                for(float z=78;z<90.5f;z+=1.25f)
                {
                    float y0=kit.Y(0,z);shape.Slot(0).Box(new Vector3(0,y0+.02f,z+.6f),new Vector3(5.2f,.12f,1.2f),Quaternion.Euler(0,(z%2.5f<1.2f?.6f:-.5f),0),new Color(.66f,.64f,.62f),new Color(.78f,.76f,.74f));
                    foreach(int side in new[]{-1,1})shape.Slot(0).Box(new Vector3(side*2.75f,Mathf.Max(y0,Bed)+.02f,z+.6f),new Vector3(.4f,.3f,1.2f),Quaternion.identity,new Color(.52f,.5f,.48f),new Color(.66f,.64f,.62f));
                }
                kit.Built("Garrison causeway",shape,Setts);
            }

            // ---------------------------------------------------------------- the zone
            internal static void Build()
            {
                Materials();
                var kit=ZoneKit.Ground(ScoriaZone,Height,1.4f);bool greybox=DevArgs.Has("-scoria-greybox");
                var scene=WorldBuilder.Begin(kit.zone);
                kit.Terrain("Scoria valley",-44,80,-16,276,.5f,Basalt,Hills,Tone,new Color(.86f,.72f,.64f),new Color(.6f,.5f,.46f));
                foreach(var p in new[]{Arrival,LandingCradle,BrekkStart,CampSpawn,CampCradle,TovraAt,BrekkStall,FoundrySpawn,HarrowgateSpawn,KettleOne,KettleTwo,KettleThree,HollowCache,GlassCache,SledCache})
                    if(!Walkable(p)||OnPlate(p,.6f))throw new InvalidOperationException("SCORIA_PLAN "+p+" is not standing on shelf");

                // ---- ways in and out, services
                WorldBuilder.Spawn("Arrival",kit.On(Arrival.x,Arrival.y));WorldBuilder.Spawn("Camp",kit.On(CampSpawn.x,CampSpawn.y));
                WorldBuilder.Spawn("Foundry",kit.On(FoundrySpawn.x,FoundrySpawn.y));WorldBuilder.Spawn("Harrowgate",kit.On(HarrowgateSpawn.x,HarrowgateSpawn.y));
                var pad=kit.Prop("LandingPad",new Vector3(Pad.x,0,Pad.y),6,0);pad.name="Landing pad";
                var launch=pad.AddComponent<DockingPad>();launch.prompt="Launch — The Picket of Teeth";launch.range=4;launch.scene=PicketZone;launch.spawn=FromScoriaSpawn;
                WorldBuilder.Label("Launch — The Picket of Teeth",kit.On(Pad.x+3.5f,Pad.y-4.6f,.3f),2.2f);
                var cradle=kit.Prop("DeckConsole",new Vector3(LandingCradle.x,0,LandingCradle.y),1.4f,-40).AddComponent<RepairBay>();cradle.free=true;cradle.saveSpawn="Arrival";cradle.prompt="Landing cradle — repair and save";
                ChapterOneStaging.CradleBay("Landing cradle",cradle.gameObject,kit.On(LandingCradle.x,LandingCradle.y),kit.Y(LandingCradle.x,LandingCradle.y)+.02f,false,true);
                // The landing: what the party unloaded and what the Muster left, round the pad's edge and clear of the walk north.
                foreach(var (x,z,yaw,key,size) in new[]{(-12.5f,11f,20f,"MusterCrate",.9f),(-13.5f,9.6f,-35f,"MusterCrate",.8f),(-2.5f,1f,10f,"DressCargoPallet",.7f),(11.5f,9.5f,70f,"DressFuelTank",1.6f),(-12f,1.5f,0f,"DressSurveyBeacon",2.2f)})
                    if(ZoneKit.Landed(key)&&Walkable(new Vector2(x,z)))kit.Prop(key,new Vector3(x,0,z),size,yaw);
                var gate=Gateway(kit,"Foundry gate",FoundryGate,0,4.2f,4.6f,true);
                kit.Way(gate,"Foundry road — The Bridle Foundry",FoundryZone,"Arrival","met.Tovra","The Foundry gate is the garrison's: talk to Tovra Kesh at the Unsworn camp first",4.4f,new Vector2(FoundryGate.x+3.6f,FoundryGate.y-4.4f));
                var road=Gateway(kit,"Town road posts",HarrowgateRoad,90,4,3.4f,false);
                kit.Way(road,"Town road — Harrowgate",TownZone,"Arrival","vask.roadSaid","Tovra Kesh hasn't said where the road goes yet: talk to her at the camp",4.2f,new Vector2(HarrowgateRoad.x-4.2f,HarrowgateRoad.y-3.2f));

                // ---- the landing: Brekk waits at the shelf's edge; his talk crosses it.
                kit.Crossing("Brekk on the crust",new Vector3(1,0,19.5f),new Vector3(30,5,3),"vask.crustRead","BrekkCrust","Brekk");
                // ---- the first crossing: whoever is over F2 has crossed it, and Brekk follows.
                kit.Crossing("Over the first tongue",new Vector3(3,0,53.5f),new Vector3(30,5,2.4f),"vask.tongueCrossed",null,"Taren");
                Hail(kit,"Brekk calls the first tongue",new Vector2(4,36),1);
                // ---- the garrison causeway: Brekk stops short of it; his talk names the pikeguards; their fight, their cookfire.
                kit.Crossing("The garrison's causeway",new Vector3(0,0,67),new Vector3(26,5,3),"vask.garrisonSeen","BrekkCrossing","Brekk");
                kit.Crossing("Two pikes",new Vector3(0,0,78.5f),new Vector3(6,5,2.4f),"vask.pikesSeen",null,"Taren").situation="pikeguard";
                Fight(kit,"Scoria_Crossing",CrossingFight,new Vector2(30,18),("MusterPikeguard",2,new Vector2(0,93),180,2.1f),("ReachScav",1,new Vector2(6,98.5f),200,0));
                Cookfire(kit,"Causeway garrison fire",KettleOne,20,"vask.kettle1");
                // ---- the cinder hollow, east across F4: two crawlers under its pan and a clan's cache at the back.
                var hollowHides=new[]{PlateNear(new Vector2(21,104),"F5"),PlateNear(new Vector2(30,111),"F5")};foreach(var h in hollowHides)Hide(h);
                Fight(kit,"Scoria_Hollow",HollowFight,new Vector2(18,14),("AshCrawler",1,hollowHides[0],200,0),("AshCrawler",1,hollowHides[1],220,0));
                Stash(kit,"vask.scoria.hollow","Clan cache",HollowCache,-30,160,false,ProgressionBuilder.AnyPart(4,GearSlot.Frame,1),ProgressionBuilder.Drop("ScoriaGlass",3,1),ProgressionBuilder.Drop("RepairGel",2,1));
                // ---- the delta: island to island behind the tide; the crawlers under its plates; the glass island to the west.
                Hail(kit,"Brekk calls the delta",new Vector2(-1,104),2);
                var deltaHides=new[]{PlateNear(new Vector2(-13,115),"F6"),PlateNear(new Vector2(3,127),"F6"),PlateNear(new Vector2(-9,140),"F6")};foreach(var h in deltaHides)Hide(h);
                Fight(kit,"Scoria_Delta",DeltaFight,new Vector2(40,40),("AshCrawler",1,deltaHides[0],180,0),("AshCrawler",1,deltaHides[1],200,0),("AshCrawler",1,deltaHides[2],160,0));
                Stash(kit,"vask.scoria.glass","Smith's chest",GlassCache,15,220,true,ProgressionBuilder.AnyPart(4,GearSlot.Emitter,1),ProgressionBuilder.Drop("ScoriaGlass",4,1),ProgressionBuilder.Drop("ChargeBank",1,1));
                // ---- the north shelf: Brekk's last word on the radio, and the first squall.
                Hail(kit,"Brekk calls the squall",new Vector2(2,155),3);
                kit.Crossing("Ash coming down the valley",new Vector3(2,0,157.5f),new Vector3(22,5,2.4f),"vask.squallMet",null,"Taren");
                // ---- the battery: two mortars on the terrace above their field and a soldier at the ramp's head; the sled at the terrace's west end.
                kit.Crossing("The battery",new Vector3(1,0,165.5f),new Vector3(24,5,2.4f),"vask.mortarsSeen",null,"Taren").situation="mortar";
                Fight(kit,"Scoria_Battery",BatteryFight,new Vector2(36,32),("MusterMortar",1,new Vector2(-8,196.5f),180,0),("MusterMortar",1,new Vector2(8.5f,196),190,0),("ReachScav",1,new Vector2(0,194),180,0));
                Cookfire(kit,"Battery fire",KettleTwo,-30,"vask.kettle2");
                Stash(kit,"vask.scoria.sled","Wrecked supply sled",SledCache,70,180,true,ProgressionBuilder.AnyPart(4,GearSlot.Module,1),ProgressionBuilder.Drop("RepairSalve",1,1),ProgressionBuilder.Drop("ScoriaGlass",2,1));
                // ---- the gate garrison and the Foundry road.
                Fight(kit,"Scoria_Gate",GateFight,new Vector2(26,18),("MusterPikeguard",2,new Vector2(0,247),180,2.1f),("ReachSergeant",1,new Vector2(-7,251),170,0));
                Cookfire(kit,"Gate garrison fire",KettleThree,10,"vask.kettle3");
                // ---- the Unsworn camp
                Camp(kit,greybox);
                // ---- Brekk ahead of the party as far as the causeway.
                Ahead(kit);
                // ---- the crust itself, and the squalls
                var crust=Crusts(kit,hollowHides.Concat(deltaHides));
                Squalls(kit);
                int n=0;foreach(var (x,z) in new[]{(-9f,60f),(18.5f,111.5f),(-24.5f,138.5f),(-24f,193f),(9f,236f)})Glass(kit,"scoria_glass_"+n++,new Vector2(x,z),500+n);

                CausewayBody(kit);
                if(!greybox){Dress(kit);ClearSightlines();}

                // ---- limits: the hills are the stop; a box on each side keeps a body from finding a way up one.
                WorldBuilder.Boundary("South limit",new Vector3(18,6,-15),new Vector3(126,24,2));WorldBuilder.Boundary("North limit",new Vector3(18,6,275),new Vector3(126,24,2));
                WorldBuilder.Boundary("West limit",new Vector3(-43,6,130),new Vector3(2,24,292));WorldBuilder.Boundary("East limit",new Vector3(79,6,130),new Vector3(2,24,292));

                // ---- navigation: every place a player must stand.
                var reach=new List<Vector2>{Arrival,LandingCradle+new Vector2(-1.6f,0),Pad+new Vector2(3,0),BrekkTongue,new(4,52),new(0,62),Causeway,new(0,90),KettleOne+new Vector2(0,-1.2f),new(4,98),
                    new(11.5f,105),HollowFight,HollowCache+new Vector2(-1.4f,-1),new(-1,104),IslandA,IslandB,new(2,150),GlassIsland,GlassCache+new Vector2(0,-1.4f),NorthShelf,new(0,176),new(0,190),new(-8,196),
                    KettleTwo+new Vector2(1.2f,-.6f),SledCache+new Vector2(1.4f,0),Junction,new(28,208),CampSpawn,CampCradle+new Vector2(1.4f,0),CampBench+new Vector2(0,-1.6f),BrekkStall+new Vector2(-1,-1.6f),
                    TovraAt+new Vector2(0,-1.6f),PikeHead,HarrowgateSpawn,HarrowgateRoad+new Vector2(-2.4f,0),new(0,222),GateFight,KettleThree+new Vector2(0,-1.2f),FoundrySpawn,FoundryGate+new Vector2(0,-3)};
                reach.AddRange(new Vector2[]{new(-9,58.6f),new(18.5f,110),new(-24.5f,137),new(-22.6f,193),new(9,234.6f)});
                kit.BakeNavigation(new Bounds(new Vector3(18,2,130),new Vector3(126,26,292)),Arrival,reach);
                if(!greybox)Ground(kit);
                WorldBuilder.Save(scene,kit.zone);Debug.Log("SCORIA_FLOWS_OK plates="+Plates.Count);
            }
            /// <summary>Brekk on the radio, leading: once per save, as the lead comes within range of each of his three places.</summary>
            static void Hail(ZoneKit kit,string name,Vector2 at,int n)
            {
                var hail=new GameObject(name,typeof(RadioHail)).GetComponent<RadioHail>();hail.transform.position=kit.On(at.x,at.y);
                hail.speaker="Brekk";hail.situations=new[]{"hail.crust"};hail.heardFlag="vask.brekkHail"+n;hail.range=9;
            }

            // ---------------------------------------------------------------- people
            /// <summary>Brekk's body walking ahead (Guide): waiting at the landing's edge, then at the first tongue's south bank, then once the party is over it, on to wait short of the
            /// causeway. After the causeway's fight he has gone on to the camp's stall.</summary>
            static void Ahead(ZoneKit kit)
            {
                var prefab=ZoneKit.Prefab("Characters","BrekkBody");if(prefab==null)return;
                var holder=new GameObject("Brekk leading");holder.transform.SetPositionAndRotation(kit.On(BrekkStart.x,BrekkStart.y),Quaternion.Euler(0,0,0));
                var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab);body.transform.SetParent(holder.transform,false);
                var guide=holder.AddComponent<Guide>();
                Vector3[] Path(params Vector2[] points)=>points.Select(p=>kit.On(p.x,p.y)).ToArray();
                guide.legs=new[]{new Guide.Leg{flag="",path=Path(BrekkStart)},new Guide.Leg{flag="vask.crustRead",path=Path(BrekkStart,new Vector2(6,27),BrekkTongue)},
                    new Guide.Leg{flag="vask.tongueCrossed",path=Path(BrekkTongue,new Vector2(7,53),new Vector2(2,60),BrekkCauseway)}};
                guide.faces=new[]{kit.On(BrekkStart.x,BrekkStart.y+6),kit.On(BrekkTongue.x,BrekkTongue.y+8),kit.On(0,84)};
                guide.goneFlag="clear.Scoria_Crossing";guide.speed=4.2f;
                // He waits at a bank for the crust ahead to stay dark long enough to cross, as anyone who lives here does (he crossed open melt behind the party before).
                var wait=holder.AddComponent<CrustWait>();wait.guide=guide;
            }
            /// <summary>The Unsworn camp: a safe pocket up the side valley. Tovra in the yard, Brekk at the stall, the bench, the cradle, the forge with its pike-head, smiths and families.</summary>
            static void Camp(ZoneKit kit,bool greybox)
            {
                var pocket=new GameObject("Unsworn camp (safe)",typeof(BoxCollider),typeof(SafePocket));pocket.transform.position=kit.On(47,206,2);
                var box=pocket.GetComponent<BoxCollider>();box.size=new Vector3(36,8,26);box.isTrigger=true;
                kit.Sign("UNSWORN CAMP",33.5f,203,1.8f);kit.Sign("STALL",47.2f,198.2f,1.6f);kit.Sign("CAMP FORGE",56.5f,207.4f,1.6f);
                var bay=kit.Prop("DeckConsole",new Vector3(CampCradle.x,0,CampCradle.y),1.4f,30).AddComponent<RepairBay>();bay.free=true;bay.saveSpawn="Camp";bay.prompt="Camp cradle — repair and save";
                ChapterOneStaging.CradleBay("Camp cradle",bay.gameObject,kit.On(CampCradle.x,CampCradle.y),kit.Y(CampCradle.x,CampCradle.y)+.02f,false,true);
                var anvil=kit.Prop("LatticeAnvil",new Vector3(CampBench.x,0,CampBench.y),1.8f,10).AddComponent<Bench>();anvil.prompt="Fabricate — Lattice Anvil";
                var tovra=kit.Person("Tovra",new Vector3(TovraAt.x,0,TovraAt.y),190,"",false,null,null,"TovraFirst");tovra.repeatNode="TovraRepeat";
                var brekk=kit.Person("Brekk",new Vector3(BrekkStall.x,0,BrekkStall.y),205,"",false,"met.Brekk","BrekkCampStall","BrekkCampStall");
                brekk.repeatNode="BrekkCampRepeat";brekk.finalCompletionFlag="brekk.stallTalked";brekk.finalRepeatNode="BrekkCampRepeat";
                var here=new GameObject("Brekk is at the stall",typeof(Whereabouts)).GetComponent<Whereabouts>();here.from="clear.Scoria_Crossing";here.goneAt="chapter8.complete";here.live=true;here.targets=new[]{brekk.gameObject};
                var stays=new GameObject("Tovra is at the camp",typeof(Whereabouts)).GetComponent<Whereabouts>();stays.from="";stays.goneAt="chapter8.complete";stays.live=true;stays.targets=new[]{tovra.gameObject};
                // The stall: a canvas awning on poles over a trestle of crates, behind Brekk.
                Stall(kit,new Vector2(BrekkStall.x+.4f,BrekkStall.y+2.4f),200);
                // The forge: a stone hearth with its coals, an anvil, a quench trough; the pike-head he dressed lies on the anvil.
                // The hearth's top was a flat orange polygon on a stone block, read as slag on a crate (s49/194): a fire on its hearth now, under an iron hood on its chimney, the bellows
                // at its side.
                var forge=new ZoneKit.Shape();var at=kit.On(ForgeAt.x,ForgeAt.y);var hearthTurn=Quaternion.Euler(0,15,0);
                forge.Slot(0).Box(at+new Vector3(0,.45f,0),new Vector3(1.8f,.9f,1.3f),hearthTurn,new Color(.6f,.58f,.56f),new Color(.7f,.68f,.66f));
                foreach(int k in new[]{-1,1}){forge.Slot(1).Box(at+hearthTurn*new Vector3(k*.84f,.94f,0),new Vector3(.12f,.08f,1.3f),hearthTurn,IronDark,IronPale);forge.Slot(1).Box(at+hearthTurn*new Vector3(0,.94f,k*.6f),new Vector3(1.8f,.08f,.12f),hearthTurn,IronDark,IronPale);}
                forge.Slot(1).Box(at+new Vector3(-1.8f,.32f,-.7f),new Vector3(1.4f,.5f,.6f),hearthTurn,IronDark,IronPale);
                forge.Slot(1).Box(at+new Vector3(-1.4f,.65f,-1.3f),new Vector3(.9f,.14f,.08f),Quaternion.Euler(0,40,0),IronPale);
                forge.Slot(1).Post(at+new Vector3(.6f,.9f,.4f),2.4f,.12f,.08f,8,IronDark,Rust);
                var hood=at+hearthTurn*new Vector3(.25f,2.05f,.15f);
                forge.Slot(1).Box(hood,new Vector3(1.5f,.08f,1.1f),hearthTurn*Quaternion.Euler(8,0,0),IronDark,Rust).Box(hood+Vector3.up*.22f,new Vector3(.8f,.36f,.6f),hearthTurn,IronDark,IronPale);
                foreach(int k in new[]{-1,1})forge.Slot(1).Beam(at+hearthTurn*new Vector3(k*.8f,.94f,-.55f),hood+hearthTurn*new Vector3(k*.7f,-.04f,-.5f),.04f,.04f,IronDark);
                var bellows=at+hearthTurn*new Vector3(1.35f,0,.1f);
                forge.Slot(1).Box(bellows+Vector3.up*.75f,new Vector3(.5f,.06f,.9f),hearthTurn*Quaternion.Euler(0,0,-6),new Color(.36f,.26f,.2f),new Color(.48f,.36f,.26f))
                    .Box(bellows+Vector3.up*1.05f,new Vector3(.48f,.06f,.86f),hearthTurn*Quaternion.Euler(0,0,10),new Color(.36f,.26f,.2f),new Color(.5f,.38f,.26f))
                    .Box(bellows+Vector3.up*.36f,new Vector3(.5f,.72f,.1f),hearthTurn,IronDark,IronPale);
                forge.Slot(2).Lump(bellows+Vector3.up*.9f,new Vector3(.22f,.13f,.4f),73,.12f,new Color(.5f,.34f,.24f),new Color(.3f,.2f,.14f),10,5,.25f,hearthTurn);
                var forgeBody=kit.Built("Camp forge",forge,Setts,Iron,Canvas);ZoneKit.Block(forgeBody,at+new Vector3(-.6f,.45f,-.3f),new Vector3(3.4f,.9f,2),15,"Forge");
                Hearths.Fire(kit,"Camp forge fire",at+Vector3.up*.96f,.44f,.72f,8,1.9f,57).transform.SetParent(forgeBody.transform,true);
                var pike=kit.Discovery("The camp forge's pike-head",new Vector3(PikeHead.x,0,PikeHead.y),"Look at the pike-head","brekk.pikeDressed","",null,"Brekk",null,0,2.6f);pike.dialogueNode="BrekkPikeHead";pike.requiredFlag="vask.unswornMet";
                // Shelter along the camp's north side, the tents' backs to the hill.
                LeanTo(kit,"Camp tent west",new Vector2(37.5f,214),185,4.2f,3,2.3f);LeanTo(kit,"Camp tent east",new Vector2(62,211.5f),175,4,3,2.2f);
                if(ZoneKit.Landed("HarrowgateHouse"))kit.Prop("HarrowgateHouse",new Vector3(50.5f,0,217.2f),4.6f,175).name="Camp hut";
                foreach(var (x,z,yaw) in new[]{(32.5f,212f,10f),(59f,201f,40f),(43.5f,214.5f,-20f)})kit.Prop(ZoneKit.Kit("MusterCrate","DeckCrate"),new Vector3(x,0,z),.9f,yaw);
                kit.Lamp(new Vector3(46,2.6f,207),11,1.2f,new Color(1,.7f,.45f)).name="Camp light";
                // The camp's own fire in the yard's middle (no kettle stone: the camp's families are here), and its barrels and crates.
                var fire=new ZoneKit.Shape();var fireAt=kit.On(44.5f,205.6f);
                Hearths.Ring(fire.Slot(0),fireAt,.72f,10,30,new Color(.7f,.66f,.62f),new Color(.28f,.26f,.25f));
                fire.Slot(0).Lump(fireAt+Vector3.up*.01f,new Vector3(.6f,.05f,.6f),31,.3f,new Color(.6f,.58f,.56f),new Color(.36f,.34f,.33f),10,4);
                var fireBody=kit.Built("Camp fire",fire,Setts);ZoneKit.Block(fireBody,fireAt+Vector3.up*.15f,new Vector3(1.5f,.3f,1.5f),0,"Fire ring");
                Hearths.Fire(kit,"Camp fire flames",fireAt+Vector3.up*.07f,.44f,.82f,7,1.6f,31).transform.SetParent(fireBody.transform,true);
                foreach(var (x,z,yaw,key,size) in new[]{(34f,207.5f,0f,"DressWaterBarrels",1.1f),(57.5f,203f,20f,"DressWaterBarrels",1.1f),(39f,213.2f,-10f,"DressShelfRack",1.6f),(53.5f,201.5f,15f,"MusterCrate",.8f)})
                    if(ZoneKit.Landed(key))kit.Prop(key,new Vector3(x,0,z),size,yaw);
                if(greybox)return;
                // The camp's people: smiths at the forge and the tents, a family by the hut, two who come and go.
                // Each in a body of nobody named here, and no two alike in one frame (Folk): Brekk's body twice beside Brekk, and Vessa's, Tovra's, Odrun's and Pip's on strangers.
                var smiths=new Color(.86f,.8f,.76f);var folk=Folk.Bodies;int nth=0;
                foreach(var (x,z,yaw,tint) in new[]{(58.6f,212.6f,200f,new Color(.78f,.74f,.7f)),(48.3f,214.6f,170f,new Color(.92f,.9f,.86f)),(52.6f,214.2f,210f,new Color(.9f,.86f,.82f)),(35.8f,211.4f,160f,new Color(.76f,.72f,.7f))})
                {Folk.Set(kit,kit.Townsperson(folk[11+nth],new Vector3(x,0,z),yaw,tint),nth);nth++;}
                Folk.Set(kit,kit.Walker(folk[11+nth],new[]{new Vector2(57.5f,214.5f),new Vector2(60.5f,209.5f),new Vector2(57,206.6f)},1.3f,2.6f,smiths).gameObject,nth);nth++;
                Folk.Set(kit,kit.Walker(folk[11+nth],new[]{new Vector2(40,212.8f),new Vector2(44.6f,213.4f)},1.1f,3.2f,new Color(.7f,.68f,.66f)).gameObject,nth);
            }

            // ---------------------------------------------------------------- weather
            /// <summary>Ash squalls: the first after Brekk's warning on the north shelf, then one each time the lead walks onto the delta's south shelf or the junction. The lee of the big
            /// rocks on the north shelf and at the junction are the shelters.</summary>
            static void Squalls(ZoneKit kit)
            {
                var flake=kit.Glow("Ash",false,new Color(.7f,.66f,.64f,1));
                var ash=kit.Drift("Blown ash",kit.On(0,150,2),new Vector3(30,6,24),flake,new Color(.66f,.62f,.6f,.7f),2200,.05f,.14f,3,3);
                var main=ash.main;main.startSpeed=new ParticleSystem.MinMaxCurve(3,7);var velocity=ash.velocityOverLifetime;velocity.enabled=true;
                velocity.x=new ParticleSystem.MinMaxCurve(-2,1);velocity.y=new ParticleSystem.MinMaxCurve(-.8f,.1f);velocity.z=new ParticleSystem.MinMaxCurve(-7,-4);
                var weather=new GameObject("Ash squalls",typeof(Whiteout)).GetComponent<Whiteout>();weather.startFlag="vask.squallMet";weather.liftedFlag="vask.squallLifted";
                weather.field=new Bounds(new Vector3(18,0,130),new Vector3(126,40,292));
                weather.fronts=new[]{new Bounds(new Vector3(-1,0,103),new Vector3(22,10,5)),new Bounds(new Vector3(4,0,212),new Vector3(16,10,6))};
                weather.shelters=new[]{new Bounds(new Vector3(-8,0,161),new Vector3(6,20,6)),new Bounds(new Vector3(-5,0,213),new Vector3(6,20,6))};
                weather.warning=2.5f;weather.closing=1.5f;weather.hold=6;weather.lifting=2.5f;weather.nearFog=2;weather.farFog=14;weather.pace=.85f;weather.white=new Color(.5f,.46f,.44f);
                weather.bark="squall";weather.snow=ash;weather.calmRate=30;weather.closedRate=700;
            }

            // ---------------------------------------------------------------- dressing
            internal static readonly Vector2[][] Ways=
            {
                new Vector2[]{Arrival,new(3,18),new(6,30),new(5,40),new(4,52),new(0,64),new(0,84),new(0,96),new(-1,104),IslandA,IslandB,new(2,152),NorthShelf,new(0,176),new(0,190),new(1,198),Junction,new(1,222),new(0,240),FoundrySpawn},
                new Vector2[]{Junction,new(16,209),new(28,208),CampSpawn,new(48,204),HarrowgateSpawn},new Vector2[]{new(4,100),new(11.5f,105),HollowFight},new Vector2[]{IslandA,new(-15,132),GlassIsland},
                new Vector2[]{new(0,196),new(-12,197),SledCache},
            };
            static bool NearWay(Vector2 p,float within){foreach(var way in Ways)if(ZoneKit.FromPath(p,way)<within)return true;return false;}
            /// <summary>Nothing this tall at a plan point may stand where a walked floor lies within the fixed camera's shadow of it (it would stand between the camera and a hero).</summary>
            static bool ShadowsAFloor(Vector2 p,float tall)
            {
                float reach=ZoneKit.Shadowed(tall)+1.5f;var side=new Vector2(Away.y,-Away.x);
                for(float d=0;d<=reach;d+=.75f)foreach(float s in new[]{-2f,0,2f})if(Walkable(p+Away*d+side*s))return true;
                return false;
            }
            /// <summary>Drops any dressing rock (a tinted kit piece) that stands on the fixed camera's line to a fight's floor, by the shared test's own rule: over three metres above that
            /// floor and broader than a post. The wall-foot pass judges a rock from where it stands and missed ones on a rim above a lower fight (the hollow's pan, the gate's yard).</summary>
            static void ClearSightlines()
            {
                Physics.SyncTransforms();var toCamera=-(Quaternion.Euler(SightlineAudit.Pitch,SightlineAudit.Yaw,0)*Vector3.forward);int cleared=0;
                foreach(var fight in UnityEngine.Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None))
                {
                    var box=fight.GetComponent<BoxCollider>().bounds;
                    for(float x=box.min.x+.5f;x<box.max.x;x+=1)for(float z=box.min.z+.5f;z<box.max.z;z+=1)
                    {
                        var p=new Vector2(x,z);if(!Walkable(p))continue;var floor=new Vector3(x,Height(p),z);if(Mathf.Abs(floor.y-(box.center.y-1.5f))>1.6f)continue;
                        foreach(var hit in Physics.RaycastAll(floor+Vector3.up*SightlineAudit.Chest,toCamera,SightlineAudit.Distance-SightlineAudit.NearIgnore,~0,QueryTriggerInteraction.Ignore))
                        {
                            var size=hit.collider.bounds.size;if(hit.collider.bounds.max.y-floor.y<3||size.x<1.6f&&size.z<1.6f)continue;
                            var rock=hit.collider.GetComponentInParent<PropTint>();if(rock==null)continue;
                            UnityEngine.Object.DestroyImmediate(rock.gameObject);cleared++;Physics.SyncTransforms();
                        }
                    }
                }
                Debug.Log("SCORIA_SIGHTLINES_CLEARED "+cleared);
            }
            /// <summary>The lowest floor along the camera's line from a point (what something standing there would hide), or the point's own ground if none.</summary>
            static float Beyond(Vector2 p,float reach){float low=Height(p);var side=new Vector2(Away.y,-Away.x);for(float d=0;d<=reach;d+=.75f)foreach(float s in new[]{-2f,0,2f}){var q=p+Away*d+side*s;if(Walkable(q))low=Mathf.Min(low,Floor(q));}return low;}
            /// <summary>Drawn only, at the back and the edges: scoria boulders along every wall's foot, obsidian spires on the far sides, the flows' mouths glowing where they come out of the
            /// hills, the garrisons' tents and slag, the battery's breastwork, ash falling over the whole valley and embers over the open flows.</summary>
            static void Dress(ZoneKit kit)
            {
                var rng=new System.Random(77);float R()=>(float)rng.NextDouble();
                // ---- the wall feet: boulders of the hills' own scoria, darkened toward basalt, never on a way and never between the camera and a floor.
                var feet=new List<Vector3>();
                for(float x=-40;x<76;x+=3.4f)for(float z=-12;z<272;z+=3.4f)
                {
                    var p=new Vector2(x+R()*2.4f,z+R()*2.4f);float clear=Open(p);
                    if(clear<.4f||clear>2.6f||NearWay(p,4)||OnPlate(p,2))continue;
                    feet.Add(new Vector3(p.x,0,p.y));
                }
                var rocks=new[]{("RidgeRock1",2.2f,4.4f),("RidgeRock2",2.2f,4.4f),("RidgeRock3",2.2f,4.4f),("DressBoulders",1.2f,2.2f),("DressStrataOutcrop",1.6f,2.8f)};
                kit.WallFoot(feet,81,rocks,rock=>
                {
                    var box=ZoneKit.Drawn(rock);var at=new Vector2(box.center.x,box.center.z);
                    // Measured from the lowest floor in its shadow, not from its own foot: a boulder on the hollow's rim stood three metres over the pan's crust and hid the fight there.
                    float rise=box.max.y-Beyond(at,ZoneKit.Shadowed(box.size.y+1)+2);
                    if(rise>2.2f&&ShadowsAFloor(at,rise))return false;
                    rock.AddComponent<PropTint>().tint=new Color(.62f,.5f,.48f);return true;
                });
                // ---- obsidian: black glass in tall broken shards on the far (north and east) sides of the floors, where no floor lies beyond them.
                var glass=new ZoneKit.Shape();int spires=0;
                for(float x=-38;x<74;x+=5.2f)for(float z=-8;z<270;z+=5.2f)
                {
                    var p=new Vector2(x+R()*3,z+R()*3);float tall=2.6f+R()*3.4f;bool keep=R()<.42f;float clear=Open(p);
                    if(!keep||clear<1.2f||clear>5||NearWay(p,5)||ShadowsAFloor(p,tall))continue;
                    var foot=kit.On(p.x,p.y,-.3f);int shards=2+(int)(R()*3);
                    for(int k=0;k<shards;k++)
                    {
                        var lean=new Vector3(R()-.5f,0,R()-.5f)*.5f;float h=tall*(.5f+.5f*R()),r=.28f+.22f*R();var root=foot+new Vector3(R()-.5f,0,R()-.5f)*1.2f;
                        glass.Slot(0).Tube(new[]{root,root+Vector3.up*h*.55f+lean*h*.3f,root+Vector3.up*h+lean*h},new[]{r,r*.7f,.02f},5,new Color(.3f,.3f,.36f),new Color(.62f,.64f,.74f),1,.5f,false);
                    }
                    spires++;
                }
                if(glass.Vertices>0)kit.Built("Obsidian spires",glass,Crust);
                Debug.Log("SCORIA_SPIRES "+spires);
                // ---- where each flow comes out of the hills: a mouth of crust over a glow, and its light.
                foreach(var (x,z,yaw) in new[]{(24.5f,31.5f,90f),(18f,47.5f,90f),(24f,84f,90f),(-22f,84f,-90f),(11.5f,114.5f,0f),(20f,152f,30f),(16.5f,222.5f,90f)})
                {
                    if(Open(new Vector2(x,z))<-.5f)continue;
                    var mouth=new ZoneKit.Shape();var at=kit.On(x,z,-.2f);var turn=Quaternion.Euler(0,yaw,0);
                    mouth.Slot(0).Lump(at+turn*new Vector3(0,.7f,1.2f),new Vector3(2.6f,1.4f,1.6f),(int)(x*7+z),.3f,new Color(.7f,.66f,.64f),new Color(.4f,.38f,.36f),12,6,.25f,turn);
                    mouth.Slot(1).Disc(at+Vector3.up*.08f+turn*new Vector3(0,0,.2f),Vector3.up,1.3f,12,Color.white);
                    var body=kit.Built("Flow mouth "+Mathf.RoundToInt(x)+" "+Mathf.RoundToInt(z),mouth,Crust,Melt);
                    kit.Lamp(new Vector3(at.x,1.2f,at.z),7,1.3f,new Color(1,.62f,.3f)).transform.SetParent(body.transform,true);
                }
                // ---- the garrisons: a lean-to and a slag heap behind each fight, crates; the battery's breastwork along its terrace's front, open at the ramp.
                LeanTo(kit,"Causeway garrison lean-to",new Vector2(-6.5f,99.5f),175,4.4f,2.8f,2.2f);LeanTo(kit,"Gate garrison lean-to",new Vector2(-7.5f,255),185,4.4f,2.8f,2.2f);
                LeanTo(kit,"Battery lean-to",new Vector2(-4,200.5f),180,4,2.6f,2.1f);
                foreach(var (x,z,yaw) in new[]{(9.5f,101f,20f),(11f,99.5f,-15f),(13.5f,198.5f,30f),(-9f,253f,10f),(10.5f,252f,40f)})kit.Prop(ZoneKit.Kit("MusterCrate","DeckCrate"),new Vector3(x,0,z),.9f,yaw);
                if(ZoneKit.Landed("DressSpoilHeap"))foreach(var (x,z) in new[]{(-11f,98.5f),(15f,200f),(11.5f,255.5f)})kit.Prop("DressSpoilHeap",new Vector3(x,0,z),1.3f,R()*360,false).AddComponent<PropTint>().tint=new Color(.5f,.42f,.4f);
                // The breastwork: three courses of the garrison's bags of ash laid in bond, a ragged top, and an iron mantlet at each end of the ramp's opening; and below it the
                // terrace's face revetted in squared basalt laid in courses, battered back, and the ramp's two side walls the same. The face was the hills' red scoria on a bare
                // riser and the breastwork a row of dark boxes: a red slab with boxes on it (whole-game capture s46/127, s46/135).
                var work=new ZoneKit.Shape();var bag=new Color(.62f,.56f,.46f);var bagDark=new Color(.42f,.38f,.32f);
                for(float x=-16;x<16;x+=1.3f)
                {
                    if(Mathf.Abs(x)<3.6f)continue;var foot=kit.On(x,192.2f);float lean=R()*6-3;
                    for(int course=0;course<3;course++)for(int k=0;k<3;k++)
                    {
                        float bx=-.43f+k*.43f+(course%2)*.2f;if(bx>.55f)continue;float seedf=x*31+course*7+k;var at=foot+Quaternion.Euler(0,lean,0)*new Vector3(bx,.12f+course*.21f,(course%2)*.04f-.02f);
                        work.Slot(1).Lump(at,new Vector3(.24f,.12f-.01f*course,.33f),Mathf.RoundToInt(seedf),.14f,ZoneKit.Shape.Dim(bag,.86f+.05f*((k+course)%3)),bagDark,8,4,.4f,Quaternion.Euler(0,90+lean+(k-1)*4,0));
                    }
                }
                foreach(int side in new[]{-1,1})
                {
                    var foot=kit.On(side*3.9f,192.2f);work.Slot(2).Box(foot+new Vector3(0,.55f,0),new Vector3(.12f,1.1f,.9f),Quaternion.Euler(0,side*8,0),IronDark,IronPale);
                    foreach(float h in new[]{.25f,.85f})work.Slot(2).Box(foot+new Vector3(side*.07f,h,0),new Vector3(.04f,.08f,.92f),Quaternion.identity,Rust);
                }
                var stone=new Color(.5f,.48f,.46f);var stoneLight=new Color(.64f,.62f,.6f);
                for(float x=-27.6f;x<17.6f;x+=.62f)
                {
                    if(Mathf.Abs(x)<3.3f||Open(new Vector2(x,191.5f))>-.2f)continue;float baseY=kit.Y(x,190.6f);
                    for(int course=0;course<3;course++){float y=course*.3f+.15f,z=191.16f+.52f*(y/Terrace)-.08f;
                        work.Slot(0).Box(new Vector3(x+(course%2)*.31f,baseY+y,z),new Vector3(.6f,.28f,.22f),Quaternion.Euler(29,(x*13+course*7)%3-1.5f,0),ZoneKit.Shape.Dim(stone,.9f+.08f*((Mathf.RoundToInt(x*10)+course)%3)),stoneLight);}
                }
                // The ramp climbs before the terrace's face does, so its two sides stand over the shelf beside it, facing out: revetted the same.
                foreach(int side in new[]{-1,1})for(float z=188;z<191.6f;z+=.62f)
                {
                    float rise=Terrace*(ZoneKit.Smooth(187.6f,191.8f,z)-ZoneKit.Smooth(191.2f,191.7f,z));if(rise<.12f)continue;float baseY=kit.Y(side*3.7f,z);
                    for(int course=0;course*.3f<rise;course++){float y=Mathf.Min(course*.3f+.15f,rise-.12f);
                        work.Slot(0).Box(new Vector3(side*3.4f,baseY+y,z+(course%2)*.31f),new Vector3(.26f,.28f,.6f),Quaternion.Euler(0,0,side*20),ZoneKit.Shape.Dim(stone,.92f+.06f*(course%2)),stoneLight);}
                }
                var breastwork=kit.Built("Battery breastwork",work,Setts,Canvas,Iron);
                ZoneKit.BlockAlong(breastwork,kit.On(-16,192.2f,.32f),kit.On(-3.6f,192.2f,.32f),.7f,.64f,"Breastwork");ZoneKit.BlockAlong(breastwork,kit.On(3.6f,192.2f,.32f),kit.On(16,192.2f,.32f),.7f,.64f,"Breastwork");
                // The sled: a Muster supply sled run off the terrace's west end, on its side, its load spilt.
                var sled=new ZoneKit.Shape();var s0=kit.On(SledCache.x-1.8f,SledCache.y+1.6f);var tilt=Quaternion.Euler(0,24,18);
                sled.Slot(0).Box(s0+tilt*new Vector3(0,.4f,0),new Vector3(3.2f,.5f,1.6f),tilt,IronDark,IronPale);
                foreach(int side in new[]{-1,1})sled.Slot(0).Beam(s0+tilt*new Vector3(-1.9f,.1f,side*.7f),s0+tilt*new Vector3(1.9f,.1f,side*.7f),.12f,.1f,Rust);
                kit.Built("Wrecked supply sled",sled,Iron);
                // ---- in the air: ash falling over the whole valley, and embers lifting off the open flows.
                var ashFlake=kit.Glow("Ash fall",false,new Color(.74f,.7f,.68f,1));
                kit.Drift("Ash fall",new Vector3(18,4,130),new Vector3(120,6,290),ashFlake,new Color(.72f,.68f,.66f,.35f),1600,.04f,.1f,.25f,16);
                var ember=kit.Glow("Embers",false,new Color(1.6f,.7f,.24f,1));
                foreach(var (x,z,w,d) in new[]{(4f,47f,30f,6f),(0f,84f,40f,8f),(-6f,128f,46f,46f),(1f,177f,34f,18f),(1f,222f,28f,6f)})
                    kit.Drift("Embers "+Mathf.RoundToInt(z),new Vector3(x,.6f,z),new Vector3(w,1.4f,d),ember,new Color(1,.62f,.26f,.7f),Mathf.RoundToInt(w*d*.18f),.03f,.07f,.5f,3);
            }

            /// <summary>The floor: ash in drifts on the shelves and worn pale along the ways, cinders in patches, scree at the walls' feet, cracks and scorches near the flows; small stones; ash
            /// cloud shade drifting over. Nothing is laid on a plate or its bank: the crust is its own.</summary>
            static void Ground(ZoneKit kit)
            {
                var ground=GroundKit.Open(kit.zone,GroundKit.FindFloor(),83,Terrace+.6f);
                float Off(Vector2 p)=>OnIsland(p,-.3f)?1:GroundKit.Smooth(.6f,1.8f,Flow(p));
                foreach(var way in Ways)ground.ClearLane(way,1.5f);
                ground.Look("Canyon",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Vaskar/scoria-wall.png"));
                // Cinder a dull red-brown: at (.66,.44,.36) and four tenths cover its patches read as rust splatter across every shelf (look pass 08); grey, the shelves went plain (look pass 09).
                Color ash=new(.78f,.74f,.7f),cinder=new(.62f,.46f,.4f),scree=new(.6f,.56f,.54f);
                ground.Layers(new Vector4(.05f,.3f,.36f,.3f),1.2f,new GroundKit.Layer("sand",ash,3.8f),new GroundKit.Layer("gravel",cinder,3.2f),new GroundKit.Layer("scree",scree,4));
                ground.PaintPatches(0,6,.26f,Off,1);
                foreach(var way in Ways)ground.PaintPath(0,way,1.1f,.9f,.35f,.7f,(int)way[0].y);
                ground.PaintPatches(1,4,.38f,p=>Off(p)*(1-.6f*GroundKit.Smooth(.4f,2,-ground.Lane(p))),4);
                ground.PaintFoot(2,2.4f,.5f,7);
                ground.Fringe(1,0,.3f,.45f,Off,9);
                ground.Shade(p=>.12f*GroundKit.Smooth(BankRun+2,BankRun,Flow(p))*Off(p));
                ground.Decals(new[]{"CracksA","CracksB"},14,p=>Off(p)*GroundKit.Smooth(.4f,.6f,ground.Noise(p,9,3)),1.4f,3,new Color(.4f,.38f,.36f,.9f));
                ground.Decals(new[]{"Scorch"},2.5f,p=>Off(p)*Mathf.Max(GroundKit.Smooth(BankRun+3,BankRun+.8f,Flow(p)),1-GroundKit.Smooth(5,9,Vector2.Distance(p,Pad))),1.6f,3,new Color(.3f,.24f,.22f,.8f),false,.1f,11);
                ground.Decals(new[]{"PebblesA","PebblesB"},16,p=>Off(p)*GroundKit.Smooth(.36f,.54f,ground.Noise(p,6,5)),.8f,1.6f,new Color(.52f,.48f,.46f));
                foreach(var way in Ways)ground.Strip("Tracks",way,.2f,1.1f,new Color(.44f,.4f,.38f,.45f));
                ground.Rocks(10,p=>Off(p)*(.4f+.6f*GroundKit.Smooth(.4f,.6f,ground.Noise(p,7,15))),.08f,.28f,new Color(.5f,.46f,.46f),3,.12f,17);
                ground.Cover(new[]{"Shards"},8,p=>Off(p)*GroundKit.Smooth(.55f,.7f,ground.Noise(p,10,19)),.1f,.22f,new Color(.42f,.42f,.5f),2);
                ground.Cover(new[]{"TuftA","TuftB"},4,p=>Off(p)*(Vector2.Distance(p,new Vector2(46,207))<16?1:.15f)*GroundKit.Smooth(.5f,.62f,ground.Noise(p,5,21)),.12f,.26f,new Color(.7f,.66f,.46f),2);
                // The islands are what a crossing is for: their rims broken with rubble and black glass, their tops cracked, so they read as old rock and not as a sand patch.
                ground.Rocks(40,p=>OnIsland(p,-.2f)?1-GroundKit.Smooth(.5f,1.5f,IslandEdge(p)):0,.1f,.32f,new Color(.46f,.44f,.46f),3,.12f,23);
                ground.Decals(new[]{"CracksA","CracksB","Slabs"},30,p=>OnIsland(p,-.6f)?1:0,1,2.2f,new Color(.36f,.34f,.33f,.9f),true,.1f,25);
                ground.Cover(new[]{"Shards"},24,p=>OnIsland(p,-.2f)?1-GroundKit.Smooth(.5f,1.6f,IslandEdge(p)):0,.12f,.26f,new Color(.4f,.4f,.48f),2,.12f,27);
                ground.MovingShade("cloud",24,new Vector2(.4f,-.6f),.22f,.5f,.12f);
                ground.Finish();
            }

            /// <summary>LookDev's stills of the Flows: each crossing as it is read from its south bank, the delta from both islands, the battery, the camp, the gate; three wide views.</summary>
            internal static readonly LookDev.View[] Views=
            {
                new("arrival",new Vector3(Arrival.x,0,Arrival.y)),new("lesson",new Vector3(4,0,28)),new("first-crossing",new Vector3(4,0,39)),new("causeway",new Vector3(0,0,74)),
                new("causeway-fight",new Vector3(0,0,90)),new("hollow",new Vector3(20,0,104)),new("delta-south",new Vector3(-1,0,103)),new("island-a",new Vector3(IslandA.x,0,IslandA.y)),
                new("island-b",new Vector3(IslandB.x,0,IslandB.y)),new("glass-island",new Vector3(GlassIsland.x,0,GlassIsland.y)),new("north-shelf",new Vector3(NorthShelf.x,0,NorthShelf.y)),
                new("battery",new Vector3(0,0,170)),new("battery-terrace",new Vector3(0,.9f,195)),new("sled",new Vector3(-18,.9f,195)),new("junction",new Vector3(Junction.x,0,Junction.y)),
                new("camp",new Vector3(42,0,203)),new("camp-forge",new Vector3(52,0,206)),new("last-tongue",new Vector3(1,0,215)),new("gate",new Vector3(0,0,246)),
                new("overview-south",new Vector3(2,0,48),false,4.2f),new("overview-delta",new Vector3(-4,0,128),false,4.2f),new("overview-north",new Vector3(10,0,200),false,4.2f),
                // Where the whole-game capture's stills stood (November 20, segments 46, 47 and 49), at the play camera's zoom, for the stills beside them.
                new("s46-124",new Vector3(1.2f,0,165.7f)),new("s46-127",new Vector3(-.6f,0,186.3f)),new("s46-166",new Vector3(40.1f,0,203.7f)),new("s47-029",new Vector3(46,0,208)),
                new("s47-117",new Vector3(-12,1.2f,197.7f)),new("s47-153",new Vector3(-.8f,0,176.5f)),new("s49-194",new Vector3(48.6f,0,207.8f)),new("causeway-fire",new Vector3(4,0,95)),
            };
        }
    }
}
