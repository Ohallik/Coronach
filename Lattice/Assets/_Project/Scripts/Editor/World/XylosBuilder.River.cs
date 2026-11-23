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
    /// <summary>The Braided River (wild; rule: the sluices decide which channel is ground). Map brief and evidence: docs/maps/BraidedRiver.md.</summary>
    public static partial class XylosBuilder
    {
        /// <summary>The chapter's two combat zones alone: the same as <c>BuildZones -xylos-zones BraidedRiver,WalkingCanopy</c> followed by <c>ZoneLook.Apply</c> for the two, in one
        /// run and under a name of its own, so that two engineers' runs don't write one log over the other and a rebuilt zone is never left in the builder's flat light.
        /// <c>-xylos-wild BraidedRiver</c> builds one; <c>-xylos-greybox</c> leaves out dressing and the ground kit (the blockout).</summary>
        public static void BuildWild()=>BatchTools.Run(()=>
        {
            string arg=DevArgs.Value("-xylos-wild");var zones=string.IsNullOrEmpty(arg)?new[]{"BraidedRiver","WalkingCanopy"}:arg.Split(',',StringSplitOptions.RemoveEmptyEntries);
            Define();foreach(var zone in zones)BuildZone(zone.Trim());
            BatchTools.RegisterScenes();AssetDatabase.SaveAssets();
            foreach(var zone in zones)
            {
                var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone.Trim()+".unity");var volume=ZoneLook.SceneVolume(zone.Trim());
                ZoneLook.Configure(zone.Trim(),volume.sharedProfile);EditorUtility.SetDirty(volume.sharedProfile);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);Debug.Log("ZONE_LOOK_APPLIED "+zone.Trim());
            }
            AssetDatabase.SaveAssets();Debug.Log("XYLOS_WILD_OK "+string.Join(",",zones));
        });

        // ---------------------------------------------------------------- the plan
        // x runs east and z north. The river comes from the east, where the Walking Canopy sits on the crossing, and runs west to the town: so a sluice stands at the east end of the
        // channel it feeds. Three channels: the north (the main stem, deep and fast, never waded: along the foot of the jungle, behind the north bank and the second island), the middle
        // (between the north bank and the first island, with the cut between the two islands; Sluice A) and the south (down the east end of the second island and west along the foot
        // of both; Sluice B). The fixed camera looks north and sees about twelve metres that way and five the other, so every piece of land is a strip with water along its north
        // shore: the river is the back of each view, and a walked line is never more than a few strides from it.
        const float RiverTop=.6f,NorthBed=-1.7f,MiddleBed=-.9f,SouthBed=-1.2f,SouthLevel=-.3f,MiddleDrained=-.7f,SouthDrained=-1f,BankRun=4.5f;
        /// <summary>Land as chains of discs (x, z, radius): a point is on a body when it is inside the hull of two neighbouring discs.</summary>
        static readonly (float x,float z,float r)[] NorthBank={(-40,121,8),(10,121,8),(40,121,6.5f),(120,121,6.5f),(150,121,7),(168,118,6.5f)};
        static readonly (float x,float z,float r)[] IslandOne={(31,88,7),(60,87,8.5f),(110,86.5f,9),(150,86,8.5f),(163,86.5f,6)};
        static readonly (float x,float z,float r)[] IslandTwo={(192,106,7),(197,93,8),(230,96,9),(270,97,9.5f),(300,98,9),(319,102,7)};
        static readonly (float x,float z,float r)[] UlmasIsland={(246,70,5),(256,69.5f,5)};
        static readonly (float x,float z,float r)[] SeedfallBank={(352,68,13),(362,90,25),(346,108,8),(368,111,12)},MooringNeck={(378,90,11),(404,90,9)};
        /// <summary>North of this line the water is the north channel's. It runs down the middle of the north bank and the second island, so where it crosses water it is a sluice's own line.</summary>
        static readonly Vector2[] NorthLine={new(-40,121),new(120,121),new(150,121),new(168,118),new(192,106),new(230,96),new(270,97),new(300,98),new(319,102),new(346,108),new(368,111),new(376,104),new(392,100.5f),new(420,100)};
        /// <summary>Between that line and this one, from the riffle to the second island, it is the middle channel's: this runs down the first island and across the weir.</summary>
        static readonly Vector2[] SouthLine={new(24,88),new(31,88),new(60,87),new(110,86.5f),new(150,86),new(163,86.5f),new(197,93),new(230,96)};
        const float RiffleX=24.5f;
        /// <summary>Where Reedstalkers hide, pocket by pocket, as x along a north shore (the bank's, the first island's, the second island's): reed beds at the water's edge,
        /// 6.5 m or more apart, clear of the arches' feet and of anything to pick up. The shore trees keep off them.</summary>
        static readonly float[] BankHides={120,134.5f,141.5f},IslandOneHides={41,49,57},IslandTwoHides={250.5f,258,265.5f,273};
        static readonly Vector2 RiverArena=new(362,88),RiverCamp=new(104,87),SouthBar=new(150,66);
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
        static float LineZ(Vector2[] line,float x)
        {
            if(x<=line[0].x)return line[0].y;
            for(int i=1;i<line.Length;i++)if(x<=line[i].x)return Mathf.Lerp(line[i-1].y,line[i].y,Mathf.InverseLerp(line[i-1].x,line[i].x,x));
            return line[line.Length-1].y;
        }
        /// <summary>Whose water a point of the river is: 0 the north channel's, 1 the middle's, 2 the south's.</summary>
        static int RiverSystem(Vector2 p)
        {
            if(p.y>LineZ(NorthLine,p.x))return 0;
            return p.x>=RiffleX&&p.x<=230&&p.y>LineZ(SouthLine,p.x)?1:2;
        }
        static float RiverLevel(Vector2 p)=>RiverSystem(p)==2?SouthLevel:0;
        /// <summary>The foot of the jungle at the back, and of the levee along the south.</summary>
        static float WallZ(float x)=>x<150?146:x<190?Mathf.Lerp(146,130,(x-150)/40):x<330?130:x<350?Mathf.Lerp(130,136,(x-330)/20):136;
        static float LeveeZ(float x)=>58-9*Mathf.Exp(-(x-250)*(x-250)/(26f*26f))+ZoneKit.Smooth(318,338,x)*2+ZoneKit.Smooth(352,388,x)*20+2*(Mathf.PerlinNoise(x*.05f,3.3f)-.5f);
        /// <summary>Metres outside the nearest land: negative on it. A shore wanders a little, so no bank is an arc.</summary>
        static float RiverShore(Vector2 p)
        {
            float d=Mathf.Min(Mathf.Min(Chain(p,NorthBank),Chain(p,IslandOne)),Mathf.Min(Chain(p,IslandTwo),Mathf.Min(Chain(p,UlmasIsland),Mathf.Min(Chain(p,SeedfallBank),Chain(p,MooringNeck)))));
            return d+(Mathf.PerlinNoise(p.x*.06f+11,p.y*.06f+3)-.5f)*3.6f+(Mathf.PerlinNoise(p.x*.19f+5,p.y*.19f+9)-.5f)*1.1f;
        }
        /// <summary>A sill across the water: the bed raised to <paramref name="top"/> along a line, easing back down to the bed over three metres either side, so it is a rise a body
        /// can wade over and not a step. It is what keeps two channels' levels apart.</summary>
        static float Sill(Vector2 p,Vector2 a,Vector2 b,float top,float half,float bed)=>Mathf.Lerp(Mathf.Max(bed,top),bed,ZoneKit.Smooth(half,half+3,ZoneKit.Segment(p,a,b)));
        static readonly Vector2 GateA1=new(168,118),GateA2=new(192,106),GateB1=new(319,102),GateB2=new(346,108),Weir1=new(163,86.5f),Weir2=new(197,93);
        static float ChannelBed(Vector2 p)
        {
            int system=RiverSystem(p);
            // A bed with bars in it: once a channel is down, the high ones stand out of what's left.
            float bed=(system==0?NorthBed:system==1?MiddleBed:SouthBed)+(Mathf.PerlinNoise(p.x*.09f+31,p.y*.09f+7)-.5f)*(system==0?.2f:.36f);
            bed=Sill(p,GateA1,GateA2,-.28f,1.3f,bed);bed=Sill(p,GateB1,GateB2,-.28f,1.3f,bed);
            bed=Sill(p,Weir1,Weir2,-.1f,1.2f,bed);bed=Sill(p,new Vector2(RiffleX,90),new Vector2(RiffleX,114),-.1f,1.6f,bed);
            // The ford to Ulma's island, waded from the start; and a gravel bar the south channel leaves dry, its sides gentle enough to walk up once the water is off them.
            bed=Sill(p,new Vector2(250,86),new Vector2(251,75),SouthLevel-.32f,3.2f,bed);
            return Mathf.Lerp(Mathf.Max(bed,SouthLevel+.3f),bed,ZoneKit.Smooth(4.5f,10.5f,Vector2.Distance(p,SouthBar)));
        }
        static float RiverHeight(Vector2 p)
        {
            float top=RiverTop+.22f*(Mathf.PerlinNoise(p.x*.08f+3,p.y*.08f+17)-.5f);
            float h=Mathf.Lerp(top,ChannelBed(p),ZoneKit.Smooth(0,BankRun,RiverShore(p)));
            float rough=Mathf.PerlinNoise(p.x*.045f+7,p.y*.05f);
            // The jungle's bank at the back and the Canopy's flank across the east end stand tall; the west bank and the south levee are the camera's foreground and stay low.
            h=Mathf.Lerp(h,RiverTop+6.5f+3*rough,ZoneKit.Smooth(WallZ(p.x),WallZ(p.x)+6,p.y));
            h=Mathf.Lerp(h,RiverTop+9+2.5f*rough,ZoneKit.Smooth(398,404,p.x));
            float levee=LeveeZ(p.x);
            h=Mathf.Lerp(h,1,ZoneKit.Smooth(levee+1.6f,levee,p.y));
            // South of its foot the levee is a bluff no body climbs, with the old forest floor on top of it out to the edge of the world: the river's south side has an edge that
            // is seen, and no floor behind it. (It was a low levee with a backwater beyond and a row of invisible boxes along it: the party could walk to the boxes.) It stays under
            // the sightline to whoever stands at its foot.
            h=Mathf.Lerp(h,3.2f+.3f*rough,ZoneKit.Smooth(levee-.3f,levee-1.9f,p.y));
            // The west end is a bank of root and earth standing straight up behind the town gate, too steep for a foot and drawn as bank, with the channels' heads against it: it was
            // a gentle rise to a flat shelf with an invisible box across it, and the party walked up to the box (QaPlanWorldTests.NoInvisibleWallStandsInTheOpen). Every camera is
            // east of it, so it may stand a head over the floor.
            return Mathf.Lerp(h,RiverTop+3.6f+1.4f*rough,ZoneKit.Smooth(WestFoot-.2f,WestFoot-2.8f,p.x+1.2f*(Mathf.PerlinNoise(p.y*.11f,4.2f)-.5f)));
        }
        /// <summary>Where the west bank stands up out of the floor (x), behind the town gate.</summary>
        const float WestFoot=-21.4f;
        /// <summary>Moss-green loam on the banks, a pale gravel fan at Seedfall, a dark wet band at each waterline and silt on the beds: the colour says where the water has been.
        /// Kept to the town's own greens (a floor that is brighter than what stands on it reads as a board, not as ground).</summary>
        static Color RiverTone(Vector2 p)
        {
            float h=RiverHeight(p),level=RiverLevel(p),patch=Mathf.PerlinNoise(p.x*.05f+2,p.y*.05f+40),mottle=.92f+.16f*Mathf.PerlinNoise(p.x*.21f+3,p.y*.21f+7);
            // Moss and bare brown earth in drifts, a little darker than the leaves: at a pale lime the banks read as a lawn under the party.
            var tone=Color.Lerp(new Color(.43f,.56f,.3f),new Color(.55f,.6f,.34f),ZoneKit.Smooth(.35f,.7f,patch));
            tone=Color.Lerp(tone,new Color(.58f,.5f,.34f),.75f*ZoneKit.Smooth(.52f,.7f,Mathf.PerlinNoise(p.x*.09f+50,p.y*.09f+8)));
            float fan=1-ZoneKit.Smooth(17,25,Vector2.Distance(p,RiverArena));
            tone=Color.Lerp(tone,new Color(.84f,.78f,.6f),fan*.85f);
            tone=Color.Lerp(tone,new Color(.4f,.46f,.32f),ZoneKit.Smooth(level+.5f,level+.08f,h));
            tone=Color.Lerp(tone,new Color(.3f,.4f,.34f),ZoneKit.Smooth(level,level-.5f,h));
            // The levee and the far bank's top are not the party's ground: darker, so the eye stays on the river.
            if(h>RiverTop+.8f)tone=Color.Lerp(tone,new Color(.38f,.5f,.3f),ZoneKit.Smooth(RiverTop+.8f,RiverTop+2,h));
            return new Color(tone.r*mottle,tone.g*mottle,tone.b*mottle,1);
        }
        /// <summary>Where the ground goes under the water between two points of dry land, and where it comes out again: a sluice's or a weir's two ends.</summary>
        static (Vector2 from,Vector2 to) Gap(Vector2 a,Vector2 b,float level)
        {
            Vector2 enter=a,exit=b;bool wet=false;
            for(float t=0;t<=1;t+=.004f)
            {
                var p=Vector2.Lerp(a,b,t);bool under=RiverHeight(p)<level;
                if(under&&!wet){enter=p;wet=true;}
                if(under)exit=p;
            }
            return (enter,exit);
        }
        [ZoneLook.Source] static void RiverLooks()
        {
            // A bright, humid river day: a high warm sun through haze, green coming back off the water and the leaves into every shadow, gold in the highlights. The haze is pale and far.
            ZoneLook.Register("BraidedRiver",new ZoneLook.Spec{sun=new Color(1,.96f,.82f),sunIntensity=1.35f,sunPitch=54,shadowStrength=.8f,
                sky=new Color(.5f,.64f,.6f),equator=new Color(.44f,.5f,.38f),ground=new Color(.22f,.27f,.16f),
                fog=true,fogColor=new Color(.72f,.82f,.66f),fogStart=60,fogEnd=260,
                contrast=10,saturation=8,splitShadows=new Color(.42f,.52f,.52f),splitHighlights=new Color(.57f,.55f,.44f),
                bloomThreshold=1,bloomIntensity=.5f,vignette=.2f});
        }

        // ---------------------------------------------------------------- what the river is made of
        internal static Material RiverFloor,RiverBank,RiverWater,PadLeaf,PadFlower;
        static readonly Color BarkDark=new(.6f,.56f,.5f),BarkPale=new(1,.96f,.88f),LeafTop=new(.46f,.78f,.3f),LeafUnder=new(.2f,.42f,.24f),LeafGold=new(.72f,.84f,.3f);
        static void RiverMaterials()
        {
            var loam=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Xylos/xylos-loam.png");var bark=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Xylos/xylos-bark.png");
            RiverFloor=ZoneKit.Toon("xylos-river-floor",loam,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_BaseColor",Color.white);m.SetColor("_ShadowTint",new Color(.5f,.6f,.62f));});
            // Whatever rises out of the floor is root and packed earth: the bark, dark, its ridges running up.
            RiverBank=ZoneKit.Toon("xylos-river-bank",bark,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.76f,.7f,.54f));m.SetColor("_ShadowTint",new Color(.46f,.54f,.6f));});
            // Small glints that run with the water, not slabs of light lying on it: the ripple's grain is under half a metre.
            // The glint is small and faint: at a grain of 1.7 and a strength of .14 it lay on the dark water as pale blots a metre across, camouflage over every channel (the bed
            // was blamed first; it isn't the bed, as making the water opaque showed).
            // Leaf lying on water draws after the water and writes no depth, so a danger marking on the bed under it (Telegraph, Transparent-10) still draws over it whole:
            // with the leaves solid, a lane run under a raft of pads lost its fill wherever a leaf lay.
            var leafMap=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Xylos/xylos-leaf.png");
            void Afloat(Material m){ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_ShadowTint",new Color(.46f,.58f,.56f));m.SetFloat("_ZWrite",0);m.renderQueue=(int)UnityEngine.Rendering.RenderQueue.Transparent-25;}
            PadLeaf=ZoneKit.Toon("xylos-pads",leafMap,Afloat);PadFlower=ZoneKit.Toon("xylos-pad-flowers",null,Afloat);
            RiverWater=WaterKit.Look("xylos-river-water",new Color(.36f,.66f,.56f,.5f),new Color(.07f,.3f,.3f,.94f),new Color(.88f,.97f,.9f,1),.8f,3.4f,.1f);
        }

        /// <summary>A jungle tree: a trunk on flared buttress roots, and a crown of leaves when it stands where nothing walks under it. Bark in slot 0, leaves in slot 1.</summary>
        static void JungleTree(ZoneKit.Shape shape,Vector3 foot,float tall,float girth,int seed,float crown)
        {
            var rng=new System.Random(seed);float R()=>(float)rng.NextDouble();
            var lean=new Vector3(R()-.5f,0,R()-.5f)*.22f;var path=new Vector3[5];var radii=new float[5];
            for(int i=0;i<5;i++){float t=i/4f;path[i]=foot+Vector3.down*.4f*(1-t)+Vector3.up*tall*t+lean*tall*t*t;radii[i]=girth*Mathf.Lerp(1.15f,.62f,t);}
            shape.Slot(0).Tube(path,radii,8,BarkDark,BarkPale,2,.22f);
            int fins=4+rng.Next(3);float turn=R()*360;
            for(int k=0;k<fins;k++)
            {
                var away=Quaternion.Euler(0,turn+k*360f/fins+R()*24,0)*Vector3.forward;float reach=girth*(2.4f+R()*1.6f);
                shape.Tube(new[]{foot+away*reach+Vector3.down*.35f,foot+away*reach*.55f+Vector3.up*tall*.07f,foot+away*girth*.9f+Vector3.up*tall*(.22f+.1f*R()),foot+away*girth*.3f+Vector3.up*tall*.4f},
                    new[]{girth*.16f,girth*.3f,girth*.34f,girth*.2f},5,BarkDark,BarkPale,1,.22f);
            }
            if(crown<=0)return;
            // The crown is fronds, as the town's trees are (Crown, drawn with the frond material): heads of leaf round the top and lower on the boughs. Lumps of leaf read as
            // green balls on sticks in every overview.
            var head=path[4];int heads=2+rng.Next(2);
            for(int k=0;k<heads;k++)
            {
                var off=k==0?Vector3.zero:Quaternion.Euler(0,R()*360,0)*Vector3.forward*crown*(.4f+.3f*R())+Vector3.down*crown*(.15f+.3f*R());
                if(k>0)shape.Slot(0).Tube(new[]{Vector3.Lerp(path[3],head,.5f),head+off},new[]{girth*.34f,girth*.18f},5,BarkDark,BarkPale,1,.22f);
                Crown(shape.Slot(1),head+off,crown*(k==0?1.2f:.85f),rng,k==0?12:9);
            }
            shape.Slot(0);
        }
        /// <summary>A root thrown from one foot to another in an arc, with lesser roots wound along it. Slot 0.</summary>
        static void RootSpan(ZoneKit.Shape shape,Vector3 from,Vector3 to,float rise,float thick,int seed)
        {
            var rng=new System.Random(seed);
            for(int strand=0;strand<3;strand++)
            {
                var path=new Vector3[11];var radii=new float[11];float own=strand==0?1:.42f+.2f*(float)rng.NextDouble(),phase=(float)rng.NextDouble()*6;
                var side=Vector3.Cross(Vector3.up,(to-from).normalized);
                for(int i=0;i<11;i++)
                {
                    float t=i/10f,arc=4*t*(1-t);
                    path[i]=Vector3.Lerp(from,to,t)+Vector3.up*(rise*arc*(strand==0?1:.93f)-.4f*(1-arc))+(strand==0?Vector3.zero:side*Mathf.Sin(t*9+phase)*thick*1.2f+Vector3.up*Mathf.Cos(t*9+phase)*thick*.7f);
                    radii[i]=thick*own*Mathf.Lerp(1.5f,.8f,arc);
                }
                shape.Slot(0).Tube(path,radii,strand==0?8:5,BarkDark,BarkPale,1,.22f);
            }
        }
        /// <summary>Hanging creeper from a point: thin green lines of different lengths. Slot as given.</summary>
        static void Creepers(ZoneKit.Shape shape,int slot,Vector3 from,float spread,float longest,int count,int seed)
        {
            var rng=new System.Random(seed);
            for(int k=0;k<count;k++)
            {
                var at=from+new Vector3(((float)rng.NextDouble()-.5f)*spread,0,((float)rng.NextDouble()-.5f)*spread*.4f);float drop=longest*(.4f+.6f*(float)rng.NextDouble());
                shape.Slot(slot).Tube(new[]{at,at+Vector3.down*drop*.5f+Vector3.right*.06f,at+Vector3.down*drop},new[]{.045f,.04f,.02f},4,new Color(.7f,.9f,.5f),new Color(.5f,.8f,.36f),1,1,false);
            }
        }
        /// <summary>Creeper hung along a span thrown by <see cref="RootSpan"/> with the same feet and rise: each from the root itself, and no longer than half the air under it.</summary>
        static void Hung(ZoneKit.Shape shape,int slot,Vector3 from,Vector3 to,float rise,float thick,int count,int seed)
        {
            var rng=new System.Random(seed);
            for(int k=0;k<count;k++)
            {
                float t=.2f+.6f*(k+(float)rng.NextDouble())/count,arc=4*t*(1-t);var at=Vector3.Lerp(from,to,t)+Vector3.up*(rise*arc-.4f*(1-arc)-thick);
                Creepers(shape,slot,at,.5f,Mathf.Max(.6f,(at.y-Mathf.Max(from.y,to.y))*.5f),1,seed+k*7);
            }
        }
        /// <summary>Rafts of floating leaf scattered over a rectangle of a water's surface, as that surface's children: they ride it up and go down with it. A leaf lies only where
        /// <paramref name="depth"/> says the water is between the two depths.</summary>
        static GameObject Rafts(ZoneKit kit,string name,Transform surface,float level,float xMin,float xMax,float zMin,float zMax,int rafts,int seed,float smallest,float largest,Func<Vector2,float> depth,float shallowest=.1f,float deepest=9)
        {
            var shape=new ZoneKit.Shape();var rng=new System.Random(seed);float F()=>(float)rng.NextDouble();
            for(int raft=0;raft<rafts;raft++)
            {
                var c=new Vector2(Mathf.Lerp(xMin,xMax,F()),Mathf.Lerp(zMin,zMax,F()));int leaves=4+rng.Next(7);float turn=F()*6.28f;
                for(int k=0;k<leaves;k++)
                {
                    float a=turn+k*2.4f,far=(.2f+.4f*Mathf.Sqrt(k))*largest*2,size=Mathf.Lerp(smallest,largest,F());var at=new Vector3(c.x+Mathf.Cos(a)*far,level+.02f+.003f*k,c.y+Mathf.Sin(a)*far);
                    float under=depth(new Vector2(at.x,at.z));if(under<shallowest||under>deepest)continue;
                    shape.Slot(0).Disc(at,Vector3.up,size,8,Color.Lerp(LeafTop,LeafGold,F()*.5f),0,LeafUnder);
                    if(k%5==1)shape.Slot(1).Lump(at+Vector3.up*.06f,new Vector3(.1f,.07f,.1f),seed+raft*9+k,.2f,new Color(1.3f,1.2f,1.1f),new Color(1.2f,.8f,.9f),6,4);
                }
            }
            if(shape.Vertices==0)return null;
            var lying=kit.Built(name,shape.Slot(1),PadLeaf,PadFlower);lying.transform.SetParent(surface,true);return lying;
        }
        /// <summary>A kit piece fitted by how long it lies, where Prop fits by how tall it stands.</summary>
        static GameObject Lying(ZoneKit kit,string key,Vector3 at,float length,float yaw,bool solid=true)
        {
            var probe=WorldBuilder.Piece(key,Vector3.zero,Vector3.one,"Rock",false);var box=ModelGeometry.BoundsOf(probe);UnityEngine.Object.DestroyImmediate(probe);
            return kit.Prop(key,at,length*box.size.y/Mathf.Max(box.size.x,box.size.z),yaw,solid);
        }
        /// <summary>The kit's root arch (6.5 m tall for an 8.7 m span, walked through along its own z) on its feet: drawn, with a solid block at each foot and the fade told where the
        /// way through is. Until the piece is taken in, two roots thrown across.</summary>
        static GameObject KitArch(ZoneKit kit,string name,Vector3 at,float tall,float yaw)
        {
            var foot=kit.On(at);var turn=Quaternion.Euler(0,yaw,0);float half=tall*8.7f/6.5f*.5f;GameObject arch;
            if(ZoneKit.Landed("RootArch"))
            {
                arch=kit.Prop("RootArch",at,tall,yaw,false);
                var fade=arch.GetComponent<SceneryOcclusion>();if(fade!=null)fade.opening=new Bounds(arch.transform.InverseTransformPoint(foot+Vector3.up*tall*.36f),new Vector3(half*1.3f,tall*.72f,2.6f));
            }
            else
            {
                var shape=new ZoneKit.Shape();RootSpan(shape,foot+turn*Vector3.left*half,foot+turn*Vector3.right*half,tall,.44f,(int)(at.x*3+at.z));RootSpan(shape,foot+turn*new Vector3(-half*.9f,0,1.1f),foot+turn*new Vector3(half*.92f,0,.9f),tall*.9f,.3f,(int)(at.x+at.z*5));
                Creepers(shape,1,foot+Vector3.up*tall*.94f,half,tall*.3f,8,(int)at.x);arch=kit.Built(name,shape.Slot(1),Bark,Vine);
            }
            arch.name=name;
            foreach(int side in new[]{-1,1})ZoneKit.Block(arch,foot+turn*Vector3.right*side*half*.86f+Vector3.up*1.2f,new Vector3(half*.36f,2.4f,1.9f),yaw,"Arch foot");
            return arch;
        }
        /// <summary>A seed carrier aground: the kit's seed barge heeled on the bank as the river left her (bow along her own z). Until that piece is taken in, a ribbed pod hull with a
        /// plank deck, seed trays, a wheelhouse under thatch and a mast, built about its own keel. With a nest, something has moved in aft.</summary>
        static GameObject Carrier(ZoneKit kit,string name,Vector3 at,float yaw,float heel,int seed,bool nest=false)
        {
            if(nest)
            {
                // What moved in: woven stalks heaped over her stern, seed pods tucked into it. Its own mesh, so it sits on whichever hull is there.
                var heap=new ZoneKit.Shape();var aft=kit.On(at.x,at.z)+Quaternion.Euler(0,yaw,0)*new Vector3(0,0,-4.4f);
                for(int k=0;k<9;k++){float a=k*.7f;heap.Slot(0).Lump(aft+new Vector3(Mathf.Cos(a)*1.5f,2.9f+.25f*(k%3),Mathf.Sin(a)*1.6f),new Vector3(1.1f,.5f,1.1f),seed+20+k,.3f,new Color(.82f,.8f,.5f),new Color(.5f,.5f,.3f),8,5);}
                for(int k=0;k<4;k++)heap.Slot(1).Lump(aft+new Vector3(-.9f+k*.6f,3.6f,-.4f+(k%2)*.8f),new Vector3(.4f,.55f,.4f),seed+40+k,.1f,new Color(1,.86f,.5f),new Color(.7f,.5f,.3f),8,6);
                kit.Built(name+" nest",heap.Slot(1),Vine,Husk);
            }
            if(ZoneKit.Landed("SeedBarge"))
            {
                var barge=Lying(kit,"SeedBarge",new Vector3(at.x,at.y,at.z),15.5f,yaw);barge.name=name;
                barge.transform.rotation=Quaternion.Euler(0,yaw,heel);var lies=ModelGeometry.BoundsOf(barge);
                barge.transform.position+=new Vector3(at.x-lies.center.x,kit.Y(at.x,at.z)-.45f-lies.min.y,at.z-lies.center.z);
                return barge;
            }
            var shape=new ZoneKit.Shape();var rng=new System.Random(seed);
            shape.Slot(0).Lump(new Vector3(0,.5f,0),new Vector3(2.5f,1.5f,8.2f),seed,.05f,Color.white,new Color(.56f,.5f,.44f),14,8,.12f);
            // The deck: boards across, and trays of seedlings in two rows forward of the house.
            shape.Slot(1).Deck(new Vector3(0,1.72f,-6.2f),new Vector3(0,1.72f,6.4f),3.5f,.14f,new Color(.92f,.86f,.72f),rng,.5f);
            for(int row=0;row<5;row++)foreach(int side in new[]{-1,1})
            {
                var tray=new Vector3(side*.9f,1.86f,-.6f+row*1.3f);
                shape.Slot(1).Box(tray,new Vector3(1.4f,.2f,1.05f),Quaternion.identity,new Color(.62f,.52f,.4f));
                shape.Slot(3).Lump(tray+Vector3.up*.22f,new Vector3(.62f,.2f,.44f),seed+row*2+side,.3f,new Color(.6f,.9f,.38f),new Color(.3f,.56f,.26f),7,4);
            }
            // The wheelhouse aft, its roof leaf thatch.
            shape.Slot(1).Box(new Vector3(0,2.5f,-4.4f),new Vector3(2.6f,1.5f,2.6f),Quaternion.identity,new Color(.74f,.66f,.52f));
            shape.Slot(2).Quad(new Vector3(-1.7f,3.25f,-6f),new Vector3(-1.7f,3.25f,-2.8f),new Vector3(0,4.05f,-2.8f),new Vector3(0,4.05f,-6f),new Vector3(-1,1,0),new Color(.82f,.86f,.5f));
            shape.Slot(2).Quad(new Vector3(1.7f,3.25f,-6f),new Vector3(1.7f,3.25f,-2.8f),new Vector3(0,4.05f,-2.8f),new Vector3(0,4.05f,-6f),new Vector3(1,1,0),new Color(.72f,.78f,.44f));
            // The mast and its furled leaf sail, and the lashings that hold a pod together.
            shape.Slot(1).Post(new Vector3(0,1.7f,2.2f),5.2f,.17f,.1f,7,new Color(.7f,.6f,.46f),new Color(.86f,.78f,.6f));
            shape.Slot(1).Tube(new[]{new Vector3(0,5.6f,2.2f),new Vector3(0,4.9f,-1.6f)},new[]{.09f,.07f},6,new Color(.7f,.6f,.46f),new Color(.7f,.6f,.46f));
            shape.Slot(2).Lump(new Vector3(0,5.1f,.3f),new Vector3(.34f,.36f,2f),seed+5,.12f,new Color(.84f,.88f,.52f),new Color(.6f,.66f,.36f),7,5);
            foreach(float z in new[]{-5.2f,-.2f,4.8f})
            {
                float girth=Mathf.Sqrt(Mathf.Max(.05f,1-z*z/(8.2f*8.2f)));var band=new Vector3[13];var thick=new float[13];
                for(int k=0;k<13;k++){float a=k*Mathf.PI*2/12;band[k]=new Vector3(Mathf.Cos(a)*2.6f*girth,.5f+Mathf.Sin(a)*1.6f*girth,z);thick[k]=.14f;}
                shape.Slot(4).Tube(band,thick,5,Color.white,Color.white,1,.5f,false);
            }
            // A curled stem at the prow, as a pod has.
            var stem=new Vector3[7];var taper=new float[7];for(int k=0;k<7;k++){float a=k/6f*Mathf.PI*1.2f;stem[k]=new Vector3(0,.9f+Mathf.Sin(a)*1.5f,7.8f+(1-Mathf.Cos(a))*1.1f);taper[k]=Mathf.Lerp(.36f,.07f,k/6f);}
            shape.Slot(0).Tube(stem,taper,6,Color.white,new Color(.74f,.82f,.52f),1,.4f);
            var hull=kit.Built(name,shape.Slot(4),Husk,Plank,Thatch,Leaf,Vine);
            // Heeled over as the bank left her: turned about the keel, then stood on the ground, a little into it.
            hull.transform.SetPositionAndRotation(kit.On(at.x,at.z,-.55f+at.y),Quaternion.Euler(0,yaw,heel));
            var box=hull.AddComponent<BoxCollider>();box.center=new Vector3(0,1.3f,0);box.size=new Vector3(4.4f,3.2f,15.6f);hull.isStatic=true;
            ZoneKit.Fadeable(hull);
            return hull;
        }
        /// <summary>A sluice: a wall of posts and woven panels across the head of a channel, and on its bank end a frame with the wheel that works it. Returns the gate and its wheel.</summary>
        static (GameObject gate,GameObject wheel,Vector2 workedFrom) Sluice(ZoneKit kit,string name,Vector2 from,Vector2 to,float level,int seed)
        {
            var (bank,far)=Gap(from,to,level+.05f);var run=(far-bank).normalized;bank-=run*1.2f;far+=run*1.2f;float length=Vector2.Distance(bank,far);
            Vector3 P(float along,float up)=>new(bank.x+run.x*along,level+up,bank.y+run.y*along);
            var shape=new ZoneKit.Shape();var rng=new System.Random(seed);int posts=Mathf.Max(3,Mathf.RoundToInt(length/2.6f)+1);
            // The kit's sluice gate (5 m wide for 3.4 m tall, across its own x), one bay after another across the channel's head; each stands on the sill. Until it is taken in, posts and woven panels.
            bool kitGate=ZoneKit.Landed("SluiceGate");int bays=Mathf.Max(1,Mathf.RoundToInt(length/4.7f));var wall=new List<GameObject>();
            if(kitGate)for(int i=0;i<bays;i++){var mid=P((i+.5f)*length/bays,0);var bay=kit.Prop("SluiceGate",new Vector3(mid.x,0,mid.z),3.4f*length/bays/5,Mathf.Atan2(-run.y,run.x)*Mathf.Rad2Deg);bay.name=name+" bay";wall.Add(bay);}
            for(int i=0;i<posts&&!kitGate;i++)
            {
                float along=length*i/(posts-1);
                shape.Slot(0).Post(P(along,-1.6f),3.5f+.3f*(float)rng.NextDouble(),.24f,.18f,7,BarkDark,BarkPale);
                if(i==0)continue;
                // A panel of woven root between each pair of posts, standing in the water to above a head.
                float before=length*(i-1)/(posts-1);Vector3 a=P(before+.2f,-.5f),b=P(along-.2f,-.5f),up=Vector3.up*2.1f;var face=new Vector3(-run.y,0,run.x);
                shape.Slot(1).Quad(a,b,b+up,a+up,face,Color.white,.35f).Quad(a,b,b+up,a+up,-face,new Color(.8f,.8f,.8f),.35f);
            }
            if(!kitGate){shape.Slot(0).Beam(P(-.3f,1.75f),P(length+.3f,1.75f),.3f,.26f,BarkDark,BarkPale);shape.Slot(0).Beam(P(-.3f,.5f),P(length+.3f,.5f),.2f,.2f,BarkDark);}
            // The wheel's frame on the near bank: two posts and a cross-piece at chest height, the axle between.
            // It faces the fixed camera whichever way the gate runs: a wheel seen edge-on is a stick, and its turning is the whole of the feedback.
            var stand=P(-1.9f,0);stand.y=kit.Y(stand.x,stand.z);float view=SightlineAudit.Yaw*Mathf.Deg2Rad;var across=new Vector3(Mathf.Cos(view),0,-Mathf.Sin(view));
            foreach(int side in new[]{-1,1})shape.Slot(0).Post(stand+across*side*.75f+Vector3.down*.3f,2.3f,.16f,.13f,6,BarkDark,BarkPale);
            shape.Slot(0).Beam(stand+across*-.9f+Vector3.up*1.9f,stand+across*.9f+Vector3.up*1.9f,.18f,.18f,BarkPale);
            // Rope from the wheel to the gate's head: what the wheel hauls on.
            shape.Slot(2).Rope(stand+Vector3.up*1.9f,P(.2f,1.9f),.25f,.05f,new Color(.8f,.7f,.48f));
            var gate=kit.Built(name,shape.Slot(2),Bark,Weave,Cloth);foreach(var bay in wall)bay.transform.SetParent(gate.transform,true);
            if(!kitGate)ZoneKit.BlockAlong(gate,P(0,0),P(length,0),.7f,3.4f,"Sluice wall");
            ZoneKit.Block(gate,stand+Vector3.up*1.1f,new Vector3(1.9f,2.2f,.7f),SightlineAudit.Yaw,"Wheel frame");
            var wheel=WaterKit.Wheel(kit,name+" wheel",.78f,Bark,BarkPale,BarkDark);
            wheel.transform.SetPositionAndRotation(stand+Vector3.up*1.25f,Quaternion.LookRotation(-SightlineAudit.ToCamera.x*Vector3.right-SightlineAudit.ToCamera.z*Vector3.forward));
            // Worked from the camera's side of the wheel, so the hero never stands behind it.
            var hand=stand+SightlineAudit.ToCamera*1.5f;
            return (gate,wheel,new Vector2(hand.x,hand.z));
        }
        /// <summary>Every leg a scripted walker walks, swept as the walker sweeps it (a sphere of .38 at .95 m, ScriptedWalk) against everything solid in the finished scene, with every
        /// pod already fallen: a leg that meets something is a person standing still in front of it for good (Teo stood at a Rootfibre hank by the bridge). Throws WALKER_BLOCKED.</summary>
        static void WalkersKeepClear(string zone)
        {
            var fallen=UnityEngine.Object.FindObjectsByType<CoverPod>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(p=>p.fallen).Where(f=>f!=null&&!f.activeSelf).ToList();
            foreach(var f in fallen)f.SetActive(true);Physics.SyncTransforms();var faults=new List<string>();int legs=0;
            try
            {
                var walks=new List<(Transform who,Vector3[] path)>();
                foreach(var walk in UnityEngine.Object.FindObjectsByType<ScriptedWalk>(FindObjectsInactive.Include,FindObjectsSortMode.None)){walks.Add((walk.transform,walk.path));walks.Add((walk.transform,walk.exitPath));}
                foreach(var leave in UnityEngine.Object.FindObjectsByType<LeavesAfter>(FindObjectsInactive.Include,FindObjectsSortMode.None))walks.Add((leave.transform,leave.path));
                foreach(var (who,path) in walks)
                {
                    if(path==null)continue;
                    for(int i=1;i<path.Length;i++)
                    {
                        var run=path[i]-path[i-1];if(run.sqrMagnitude<.0001f)continue;legs++;
                        foreach(var hit in Physics.SphereCastAll(path[i-1]+Vector3.up*.95f,.38f,run.normalized,run.magnitude,~0,QueryTriggerInteraction.Ignore))
                            if(hit.collider.transform!=who&&!hit.collider.transform.IsChildOf(who))faults.Add($"{who.name}'s leg from ({path[i-1].x:0.0}, {path[i-1].z:0.0}) to ({path[i].x:0.0}, {path[i].z:0.0}) runs into {hit.collider.name} at ({hit.point.x:0.0}, {hit.point.z:0.0})");
                    }
                }
            }
            finally{foreach(var f in fallen)f.SetActive(false);Physics.SyncTransforms();}
            if(faults.Count>0)throw new InvalidOperationException("WALKER_BLOCKED "+zone+": "+string.Join("; ",faults));
            Debug.Log($"WALKERS_CLEAR {zone} legs={legs}");
        }
        /// <summary>For stills only (a scene built with it is never the zone): a water stood at another level as play would leave it, its deep blocks gone if it is down.</summary>
        static void StillLevel(WaterVolume water,float level)
        {
            water.surface.position=new Vector3(0,level,0);if(level<water.level)foreach(var block in water.deep)block.SetActive(false);
            var mesh=UnityEngine.Object.Instantiate(water.surface.GetComponent<MeshFilter>().sharedMesh);var depth=new List<Vector2>();mesh.GetUVs(1,depth);
            for(int i=0;i<depth.Count;i++)depth[i]=new Vector2(depth[i].x,level-depth[i].x);mesh.SetUVs(1,depth);water.surface.GetComponent<MeshFilter>().sharedMesh=mesh;
        }
        /// <summary>For stills only: a boss's drawn body stood where its fight wakes it, so a still can say whether its arena reads round it. <c>-xylos-stills</c>.</summary>
        static void StillBoss(string id,Vector3 at,float yaw)
        {
            var def=ZoneKit.Find<EnemyDef>(id);if(def==null||def.prefab==null){Debug.LogWarning("XYLOS_STILL_BOSS_MISSING "+id);return;}
            var body=(GameObject)PrefabUtility.InstantiatePrefab(def.prefab);body.name=id+" (still)";body.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));
            if(def.drawnScale>0&&def.drawnScale!=1)body.transform.localScale*=def.drawnScale;
        }
        /// <summary>A grower's basket with its lid tied down: what a cache looks like on this river.</summary>
        static GameObject Basket(ZoneKit kit,string name,Vector3 at,float yaw)
        {
            var shape=new ZoneKit.Shape();
            shape.Slot(0).Post(Vector3.zero,.78f,.52f,.62f,10,new Color(.86f,.8f,.66f),Color.white,.8f).Lump(new Vector3(0,.82f,0),new Vector3(.66f,.2f,.66f),3,.05f,Color.white,new Color(.7f,.64f,.5f),10,5);
            shape.Slot(1).Rope(new Vector3(-.66f,.6f,0),new Vector3(.66f,.6f,0),-.32f,.035f,new Color(.84f,.76f,.5f)).Rope(new Vector3(0,.6f,-.66f),new Vector3(0,.6f,.66f),-.32f,.035f,new Color(.84f,.76f,.5f));
            shape.Slot(2).Lump(new Vector3(0,1.08f,0),Vector3.one*.13f,5,.05f,Color.white,Color.white,7,5);
            var basket=kit.Built(name,shape.Slot(2),Weave,Cloth,Lantern);basket.transform.SetPositionAndRotation(kit.On(at),Quaternion.Euler(0,yaw,0));
            basket.AddComponent<BoxCollider>().center=Vector3.up*.5f;basket.GetComponent<BoxCollider>().size=new Vector3(1.2f,1,1.2f);basket.isStatic=true;
            return basket;
        }
        static LootCache Stash(ZoneKit kit,string id,string label,Vector3 at,float yaw,int scrip,params LootEntry[] contents)
        {
            // The mesh is saved under the basket's name: a plain one, from where it is, and the label is the card's.
            var cache=LootCache.On(Basket(kit,"Basket "+Mathf.RoundToInt(at.x)+" "+Mathf.RoundToInt(at.z),at,yaw),id,label,scrip,contents);cache.range=2.6f;return cache;
        }
        /// <summary>Something to gather: a burl bleeding amber sap on a root, or a hank of pale rootfibre. Gone once taken, like a mined crystal.</summary>
        static MineNode Harvest(ZoneKit kit,string id,string material,Vector3 at,int seed)
        {
            var shape=new ZoneKit.Shape();bool sap=material=="AmberSap";
            if(sap)
            {
                shape.Slot(0).Lump(new Vector3(0,.3f,0),new Vector3(.55f,.42f,.5f),seed,.25f,BarkPale,BarkDark,9,6);
                for(int k=0;k<4;k++)shape.Slot(1).Lump(new Vector3(-.2f+k*.14f,.52f-.07f*k,.34f-.03f*k),new Vector3(.13f,.2f-.02f*k,.1f),seed+k,.2f,Color.white,Color.white,7,5);
            }
            else
            {
                shape.Slot(0).Post(Vector3.down*.1f,.5f,.2f,.14f,7,BarkDark,BarkPale);
                var rng=new System.Random(seed);
                for(int k=0;k<9;k++){var d=Quaternion.Euler(0,k*40+(float)rng.NextDouble()*20,0)*Vector3.forward;shape.Slot(2).Frond(new Vector3(0,.42f,0),d,.9f+.4f*(float)rng.NextDouble(),.12f,.5f,.95f,new Color(.96f,.92f,.74f),new Color(.86f,.84f,.62f));}
            }
            var body=kit.Built(sap?"Amber sap burl "+id:"Rootfibre hank "+id,shape.Slot(2),Bark,Lantern,Cloth);body.transform.position=kit.On(at);
            var solid=body.AddComponent<BoxCollider>();solid.center=Vector3.up*.35f;solid.size=new Vector3(.9f,.7f,.9f);body.isStatic=true;
            var node=body.AddComponent<MineNode>();node.nodeId=id;node.material=material;node.amount=sap?2:3;node.prompt=sap?"Gather Amber Sap":"Cut Rootfibre";node.range=2.6f;
            return node;
        }

        // ---------------------------------------------------------------- the zone
        static void River()
        {
            RiverMaterials();
            var kit=ZoneKit.Ground("BraidedRiver",RiverHeight,2.6f);bool greybox=DevArgs.Has("-xylos-greybox"),drained=DevArgs.Has("-xylos-drained");
            // A river and insects for a bed of its own (tools/water_sounds.py): the town's would make two places one.
            var bed=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Ambience/AMB_BraidedRiver.ogg");
            if(bed!=null){var def=ZoneKit.Find<ZoneDef>("BraidedRiver");def.ambientAudio=bed;def.ambientVolume=.2f;EditorUtility.SetDirty(def);}
            var scene=WorldBuilder.Begin(kit.zone);
            var land=kit.Terrain("Braided river",-44,412,30,160,.75f,RiverFloor,RiverBank,RiverTone,new Color(.36f,.46f,.32f),new Color(.62f,.66f,.5f));

            // ---- water. Three bodies, each a level over the one ground: the north never changes, the middle and the south each go down on their sluice's flag. A reach never lies
            // over another water's bed (a drained channel would be waded as if it were still full), so where two meet at a gate or the weir the reach is turned to lie along it.
            const float alongA=26.57f,alongB=-12.53f,alongWeir=-10.82f;
            var northSheet=WaterKit.Sheet(kit,"North channel",-44,412,100,152,1,0,RiverWater,p=>RiverSystem(p)==0);
            var north=WaterKit.Volume(northSheet,0,(new Vector2(78,136),new Vector2(244,28),0),(new Vector2(182.9f,117.8f),new Vector2(27,12),alongA),(new Vector2(259.5f,125),new Vector2(119,50),0),
                (new Vector2(331.1f,111.3f),new Vector2(28,12),alongB),(new Vector2(332.5f,131),new Vector2(27,38),0),(new Vector2(379,130.75f),new Vector2(66,38.5f),0),(new Vector2(395,106.2f),new Vector2(34,10.6f),0));
            WaterKit.Deep(kit,north,-44,412,100,152,p=>RiverSystem(p)==0);
            var middleSheet=WaterKit.Sheet(kit,"Middle channel",20,200,84,122,1,0,RiverWater,p=>RiverSystem(p)==1);
            var middle=WaterKit.Volume(middleSheet,0,(new Vector2(94.75f,105),new Vector2(140.5f,31),0),(new Vector2(176.9f,105.7f),new Vector2(27,13),alongA),(new Vector2(178.8f,96.1f),new Vector2(30,12),alongWeir));
            middle.drainFlag="xylos.sluiceA";middle.drainedLevel=MiddleDrained;middle.drainSeconds=3;
            WaterKit.Deep(kit,middle,20,200,84,122,p=>RiverSystem(p)==1);
            var southSheet=WaterKit.Sheet(kit,"South channel",-44,350,44,122,1,SouthLevel,RiverWater,p=>RiverSystem(p)==2&&p.y>LeveeZ(p.x)-.5f);
            var south=WaterKit.Volume(southSheet,SouthLevel,(new Vector2(93,65.5f),new Vector2(274,41),0),(new Vector2(181.2f,83.4f),new Vector2(34,12),alongWeir),(new Vector2(274,70),new Vector2(148,52),0),
                (new Vector2(-9.75f,83),new Vector2(68.5f,75),0),(new Vector2(333.5f,72.5f),new Vector2(29,55),0),(new Vector2(333.9f,98.7f),new Vector2(28,12),alongB));
            south.drainFlag="xylos.sluiceB";south.drainedLevel=SouthDrained;south.drainSeconds=3.4f;
            WaterKit.Deep(kit,south,-44,350,44,122,p=>RiverSystem(p)==2&&p.y>LeveeZ(p.x)-.5f);
            if(drained)
            {
                // For stills of the river as it is once both sluices are open (never saved as the zone: rebuild without the flag).
                foreach(var (water,level) in new[]{(middle,MiddleDrained),(south,SouthDrained)})StillLevel(water,level);
            }

            // ---- ways in and out
            WorldBuilder.Spawn("Arrival",kit.On(0,122.6f));WorldBuilder.Spawn("Camp",kit.On(100,88));WorldBuilder.Spawn("Seedfall",kit.On(343,90));WorldBuilder.Spawn("Canopy",kit.On(383,90));WorldBuilder.Spawn("Bridge",kit.On(350,72));
            KitArch(kit,"River gate",new Vector3(-17,0,121),5.2f,90);
            var townWay=new GameObject("River gate — Rootmarket");townWay.transform.position=kit.On(-16,121,1);
            kit.Way(townWay,"River gate — Rootmarket","Rootmarket","River",null,null,4.2f,new Vector2(-11,116.6f));
            // The Canopy's mooring roots at the east end: shut until Teo has said what is sitting on the crossing, because the anchor roots are counted from that talk on.
            KitArch(kit,"Canopy mooring",new Vector3(392.5f,0,90),6.4f,90);
            var canopyWay=new GameObject("Mooring roots — The Walking Canopy");canopyWay.transform.position=kit.On(392.5f,90,1);
            // Its name on the ground at the foot of the roots, within a few strides of them: at eight metres it read as belonging to the fan (QaStoryWorldTests.EveryChapterTwoExitIsSigned).
            kit.Way(canopyWay,"Mooring roots — The Walking Canopy","WalkingCanopy","Arrival","xylos.seedthaneGone","The mooring roots are drawn shut: ask Teo Marrow at Marrow-3 what holds them",4.4f,new Vector2(387.8f,86.6f));
            // The way back to town from Seedfall: a root thrown over the levee toward Rootmarket's bank.
            var bridgeShape=new ZoneKit.Shape();RootSpan(bridgeShape,kit.On(349,62),kit.On(342,45.5f),3.2f,.55f,14);RootSpan(bridgeShape,kit.On(351.5f,61.5f),kit.On(345,44.5f),3.4f,.4f,15);
            kit.Built("Root bridge",bridgeShape,Bark);
            var bridgeWay=new GameObject("Root bridge — Rootmarket");bridgeWay.transform.position=kit.On(350,64.5f,1);
            kit.Way(bridgeWay,"Root bridge — Rootmarket","Rootmarket","River",null,null,3.8f,new Vector2(352.5f,68.6f));

            // ---- the sluices. Each is a gate across the head of its channel, worked from its bank end; the point a hero works it at is a discovery (the talk is the working, and its
            // last line sets the flag), and the switch is what answers: the wheel, the sound, and the view drawn back over the water that is about to go.
            var (gateA,wheelA,handA)=Sluice(kit,"Middle sluice",GateA1,GateA2,0,21);
            var workA=kit.Discovery("Middle sluice wheel",new Vector3(handA.x,0,handA.y),"Open the middle sluice","xylos.sluiceA","SluiceA","SelaSluiceA","Sela",null,0,3.4f);
            var switchA=gateA.AddComponent<FlowSwitch>();switchA.flag="xylos.sluiceA";switchA.wheel=wheelA.transform;switchA.point=workA;switchA.shows=new Bounds(new Vector3(158,0,104),new Vector3(34,2,16));
            kit.Sign("MIDDLE SLUICE",handA.x-4.6f,handA.y+1.6f,1.8f);
            var (gateB,wheelB,handB)=Sluice(kit,"South sluice",GateB1,GateB2,0,22);
            var workB=kit.Discovery("South sluice wheel",new Vector3(handB.x,0,handB.y),"Open the south sluice","xylos.sluiceB","SluiceB","SelaSluiceB","Sela",null,0,3.4f);
            var switchB=gateB.AddComponent<FlowSwitch>();switchB.flag="xylos.sluiceB";switchB.wheel=wheelB.transform;switchB.point=workB;switchB.shows=new Bounds(new Vector3(331,0,92),new Vector3(14,2,26));
            kit.Sign("SOUTH SLUICE",handB.x-4.6f,handB.y+1.6f,1.8f);
            // The weir between the islands and the riffle at the first island's tip: dams of root laid across, which is why one channel can stand empty beside a full one. They stand
            // out of the water and stop a body for good: the way from one island to the other is the drained cut north of the weir, and the way to Marrow-1 is along her own island.
            var weirs=new ZoneKit.Shape();var dams=new List<(Vector3 from,Vector3 to)>();
            void Weir(Vector2 a,Vector2 b,float level,int seed)
            {
                var (from,to)=Gap(a,b,RiverTop-.35f);var rng=new System.Random(seed);var run=(to-from).normalized;from-=run*.6f;to+=run*.6f;int logs=Mathf.CeilToInt(Vector2.Distance(from,to)/3.2f);
                var side=new Vector2(-run.y,run.x);
                for(int i=0;i<logs;i++)
                {
                    var p=Vector2.Lerp(from,to,(i-.08f)/logs);var q=Vector2.Lerp(from,to,(i+1.08f)/logs);var m=(p+q)*.5f+side*((float)rng.NextDouble()-.5f)*.7f;float y=level+.02f,lift=.1f+.22f*(float)rng.NextDouble();
                    weirs.Slot(0).Tube(new[]{new Vector3(p.x,y-.25f,p.y),new Vector3(m.x,y+lift,m.y),new Vector3(q.x,y-.3f,q.y)},new[]{.34f,.4f,.3f},7,BarkDark,BarkPale,1,.3f);
                    // A lesser root woven over each, and a stake driven through it.
                    var o=side*(.34f+.2f*(float)rng.NextDouble())*(i%2==0?1:-1);
                    weirs.Slot(0).Tube(new[]{new Vector3(p.x+o.x,y-.3f,p.y+o.y),new Vector3(m.x-o.x*.6f,y+lift+.26f,m.y-o.y*.6f),new Vector3(q.x+o.x,y-.34f,q.y+o.y)},new[]{.16f,.2f,.14f},5,BarkDark,BarkPale,1,.3f);
                    weirs.Slot(0).Post(new Vector3(q.x,y-1.1f,q.y),1.75f+.3f*(float)rng.NextDouble(),.11f,.08f,5,BarkDark,BarkPale);
                }
                dams.Add((new Vector3(from.x,level,from.y),new Vector3(to.x,level,to.y)));
            }
            Weir(Weir1,Weir2,0,31);Weir(new Vector2(RiffleX,90),new Vector2(RiffleX,114),0,32);
            var weirBody=kit.Built("Root weirs",weirs,Bark);foreach(var (from,to) in dams)ZoneKit.BlockAlong(weirBody,from+Vector3.down*.2f,to+Vector3.down*.2f,1.3f,1.5f,"Root weir");

            // ---- the mid-river camp on the first island: Jask's ferry stage, a press-hand's anvil, a cradle, a cache. Nothing fights here.
            var camp=new GameObject("Mid-river camp (safe)",typeof(BoxCollider),typeof(SafePocket));camp.transform.position=kit.On(RiverCamp.x,RiverCamp.y,1.5f);
            var campBox=camp.GetComponent<BoxCollider>();campBox.size=new Vector3(30,6,20);campBox.isTrigger=true;
            var cradle=kit.Prop("DeckConsole",new Vector3(96,0,83),1.4f,35).AddComponent<RepairBay>();cradle.free=true;cradle.saveSpawn="Camp";cradle.prompt="Ferry cradle — repair and save";
            ChapterOneStaging.CradleBay("Ferry cradle",cradle.gameObject,kit.On(96,83),kit.Y(96,83)+.02f,true,true);
            var anvil=kit.Prop("LatticeAnvil",new Vector3(112.5f,0,84),1.8f,-40).AddComponent<Bench>();anvil.prompt="Fabricate — Lattice Anvil";
            Stash(kit,"xylos.river.camp","Ferry hands' basket",new Vector3(110,0,79.6f),20,60,ProgressionBuilder.Drop("RepairGel",2,1),ProgressionBuilder.Drop("Rootfibre",3,1));
            // The stage itself: a plank deck out over the island's north shore, where the middle channel is, and the raft moored to it.
            // Its deck is level with the bank where it leaves it, so a body (and the paths) step onto it without a step.
            var stage=new ZoneKit.Shape();var stageRng=new System.Random(61);const float deck=.76f;
            stage.Slot(0).Deck(new Vector3(104,deck,90.6f),new Vector3(104,deck,99.4f),4.6f,.12f,new Color(.94f,.88f,.72f),stageRng,.46f);
            foreach(var (x,z) in new[]{(102f,92.8f),(106f,92.8f),(102f,99f),(106f,99f)})stage.Slot(1).Post(new Vector3(x,-1.4f,z),3.1f,.15f,.12f,6,BarkDark,BarkPale);
            stage.Slot(0).Deck(new Vector3(107.6f,-.05f,100.2f),new Vector3(112.4f,-.05f,100.8f),2.6f,.16f,new Color(.84f,.78f,.62f),stageRng,.5f);
            stage.Slot(1).Tube(new[]{new Vector3(109.5f,.1f,100.7f),new Vector3(110.6f,1.6f,101.2f),new Vector3(111.6f,3.3f,101.8f)},new[]{.05f,.045f,.035f},5,BarkPale,BarkPale);
            stage.Slot(2).Rope(new Vector3(106,.95f,99),new Vector3(107.8f,.1f,100.3f),.3f,.035f,new Color(.82f,.72f,.5f));
            stage.Slot(3).Lump(new Vector3(102,1.95f,99),Vector3.one*.2f,4,.05f,Color.white,Color.white,7,5);
            var stageBody=kit.Built("Ferry stage",stage.Slot(3),Plank,Bark,Cloth,Lantern);
            ZoneKit.Block(stageBody,new Vector3(104,deck-.4f,95),new Vector3(4.6f,.8f,8.8f),0,"Stage deck");
            kit.Lamp(new Vector3(102,2.1f,98.8f),7,1.4f);
            kit.Sign("FERRY STAGE",104,88.9f,1.8f);
            // Jask poles the ferry out here from the day the party first comes down to the river: first words if they never met in town, the long talk once the sluice is open and
            // they have, and the short one after (Npc's own tiers; no beat needed).
            var jask=kit.Person("Jask",new Vector3(103.2f,deck-kit.Y(103.2f,94.4f),94.4f),200,"",false,"xylos.sluiceA","JaskFerry","JaskFerryFirst");
            jask.repeatNode="JaskFerryRepeat";jask.finalCompletionFlag="jask.ferryTalk";jask.finalRepeatNode="JaskFerryRepeat";
            var hail=new GameObject("Jask hails from the stage",typeof(RadioHail)).GetComponent<RadioHail>();hail.transform.position=kit.On(104,104);
            hail.speaker="Jask";hail.situations=new[]{"hail.ferry"};hail.needs=new[]{"xylos.sluiceA"};hail.silencedBy="jask.ferryTalk";hail.heardFlag="jask.ferryHailed";hail.range=30;
            // He is here from the moment the party has looked down the river at the first arch (the town's ferry loses him on the same flag, so there is only ever one of him), and
            // after the vote he is nowhere (he has stowed away). The arch is sixty metres from the stage, so he comes while nobody can see the stage.
            var jaskHere=new GameObject("Jask is at the ferry stage",typeof(Whereabouts)).GetComponent<Whereabouts>();jaskHere.from="xylos.archSeen";jaskHere.goneAt="xylos.voteHeard";jaskHere.live=true;
            jaskHere.targets=new[]{jask.gameObject,hail.gameObject};

            // ---- the carriers. Each lies where the storm left her with her crew shut inside; the fight round her is the relief, and the crew's speaker comes out once it's won.
            // Each is on a north shore, behind her own fight: the back of the view, never the front of it.
            Carrier(kit,"Marrow-1",new Vector3(28,0,99),-58,-9,71);
            Carrier(kit,"Marrow-2",new Vector3(214,0,105.6f),82,7,72);
            Carrier(kit,"Marrow-3",new Vector3(363,0,107.5f),96,-6,73,true);
            // What Marrow-2's crew got off her before she settled, out on the bank in her lee: two stacks of seed trays with the seedlings still in them, two baskets, and her sail
            // furled on its spar, laid down where they could reach it. Island two's west end was open moss here (look_density 0.23 at Dabb's carrier).
            void Unloaded()
            {
                foreach(var (x,z,turn,seed) in new[]{(219.4f,94.4f,12f,1),(221.9f,96.5f,-20f,2)})
                {
                    var stack=new ZoneKit.Shape();var foot=kit.On(x,z);var rot=Quaternion.Euler(0,turn,0);int trays=3-(seed%2);
                    for(int k=0;k<trays;k++)
                    {
                        var tray=foot+Vector3.up*(.1f+k*.24f)+rot*new Vector3(.04f*(k%2),0,-.05f*k);
                        stack.Slot(0).Box(tray,new Vector3(1.1f,.18f,.8f),rot*Quaternion.Euler(0,k*6,0),new Color(.62f,.52f,.4f),new Color(.76f,.66f,.5f));
                    }
                    for(int k=0;k<4;k++)stack.Slot(1).Lump(foot+Vector3.up*(.24f+(trays-1)*.24f)+rot*new Vector3(-.36f+k*.24f,0,(k%2)*.16f-.08f),new Vector3(.16f,.12f,.14f),seed*9+k,.3f,new Color(.6f,.9f,.38f),new Color(.3f,.56f,.26f),7,4);
                    var body=kit.Built("Marrow-2 seed trays "+seed,stack.Slot(1),Plank,Leaf);ZoneKit.Block(body,foot+Vector3.up*(.12f+trays*.12f),new Vector3(1.2f,.24f+trays*.24f,.9f),turn,"Seed trays");
                }
                Basket(kit,"Basket 217 93",new Vector3(217.4f,0,93.2f),30);Basket(kit,"Basket 224 95",new Vector3(223.6f,0,95.1f),-50);
                var sail=new ZoneKit.Shape();Vector3 from=kit.On(209.4f,92.6f),to=kit.On(215.6f,91.6f);   // under seven metres across the camera's view: a wide thing on the camera's side of a fight is the suite's business
                sail.Slot(0).Tube(new[]{from+Vector3.up*.12f,Vector3.Lerp(from,to,.5f)+Vector3.up*.12f,to+Vector3.up*.12f},new[]{.11f,.12f,.1f},7,BarkDark,BarkPale,1,.3f);
                var furl=new Vector3[6];var girth=new float[6];
                for(int i=0;i<6;i++){float t=.08f+.84f*i/5f;furl[i]=Vector3.Lerp(from,to,t)+Vector3.up*.3f+Vector3.forward*.05f*Mathf.Sin(i*2.1f);girth[i]=.2f+.06f*Mathf.Sin(i*1.7f+.5f);}
                sail.Slot(1).Tube(furl,girth,8,new Color(.84f,.86f,.56f),new Color(.72f,.78f,.48f),1,.4f);
                foreach(float t in new[]{.25f,.55f,.82f}){var at=Vector3.Lerp(from,to,t)+Vector3.up*.3f;sail.Slot(2).Tube(new[]{at+Vector3.down*.23f+Vector3.forward*.2f,at+Vector3.up*.25f,at+Vector3.down*.23f-Vector3.forward*.2f},new[]{.035f,.035f,.035f},5,new Color(.84f,.72f,.5f),new Color(.84f,.72f,.5f),1,1,false);}
                kit.Built("Marrow-2 sail on its spar",sail.Slot(2),Bark,Thatch,Cloth);
            }
            Unloaded();
            kit.Sign("MARROW-1",41,92.6f,1.8f);kit.Sign("MARROW-2",218,100.2f,1.8f);kit.Sign("MARROW-3",369,102,1.8f);
            Npc Crew(string id,Vector3 at,float yaw,string fight)
            {
                var person=kit.Person(id,at,yaw,"",false,null,null,id+"Relief");
                var comes=new GameObject(DisplayNames.Speaker(id)+" comes out",typeof(FlagGate)).GetComponent<FlagGate>();comes.flag="clear."+fight;comes.targets=new[]{person.gameObject};person.gameObject.SetActive(false);
                return person;
            }
            Crew("Garo",new Vector3(35,0,91.6f),150,"River_Marrow1");
            Crew("Dabb",new Vector3(212,0,100.8f),170,"River_Marrow2");
            // Teo walks out to his mother's boat once the Seedthane has gone, and is back in town after the vote. Met or not, his first words here are the ones that send the party east.
            var teo=kit.Person("Teo",new Vector3(355,0,101),170,"",false,"bossdown.Seedthane","TeoSeedfall","TeoSeedfall");
            teo.repeatNode="TeoSeedfallRepeat";teo.finalCompletionFlag="xylos.seedthaneGone";teo.finalRepeatNode="TeoSeedfallRepeat";
            // The complement of the town's own Teo (away from bossdown.Seedthane until xylos.voteHeard), so there is only ever one of him: followed live, since the fall that brings
            // him is in front of the party and his talk is what opens the mooring roots.
            var teoHere=new GameObject("Teo is at Marrow-3",typeof(Whereabouts)).GetComponent<Whereabouts>();teoHere.from="bossdown.Seedthane";teoHere.goneAt="xylos.voteHeard";teoHere.live=true;
            teoHere.targets=new[]{teo.gameObject};teo.gameObject.SetActive(false);
            // He arrives; he never appears. Brought in by the Seedthane's fall, he starts on the root bridge's foot off the fan's south edge, out of the frame of anyone fighting on the
            // fan, and walks up across it to the carrier, through Sela's line about the Seedthane going (it popped him into the frame beside the party). Once his talk is had, a load
            // finds him at the carrier.
            var arrives=teo.gameObject.AddComponent<ScriptedWalk>();arrives.requireFlag="bossdown.Seedthane";arrives.exitFlag="xylos.seedthaneGone";arrives.walkThroughDialogue=true;
            // East of the Rootfibre hank at (349, 73): the first leg ran over it and he stood there (WalkersKeepClear checks every leg at build).
            arrives.path=new[]{kit.On(348.5f,65.5f),kit.On(351.4f,72.5f),kit.On(351.6f,80),kit.On(351.6f,91),kit.On(352.2f,98.6f),kit.On(355,101)};arrives.speed=2.4f;
            var fanMiddle=new GameObject("Teo walks up from the bridge (begins)");fanMiddle.transform.position=kit.On(RiverArena.x,RiverArena.y);arrives.beginNear=fanMiddle.transform;arrives.beginDistance=80;

            // ---- things to find
            // The first arch: roots thrown from the jungle over the main stem to the bank, beside the path and never across it. The talk under it names the three channels and the
            // thing at the far end.
            float archFoot=Waterline(36,123,.3f)-.4f;var archShape=new ZoneKit.Shape();RootSpan(archShape,kit.On(35,147.5f),kit.On(36,archFoot),7.4f,.66f,41);RootSpan(archShape,kit.On(37.5f,146.6f),kit.On(37.4f,archFoot+.8f),8.2f,.42f,42);Hung(archShape,1,kit.On(35,147.5f),kit.On(36,archFoot),7.4f,.66f,12,43);
            var firstArch=kit.Built("First arch",archShape.Slot(1),Bark,Vine);ZoneKit.Block(firstArch,kit.On(36.6f,archFoot+.4f,.9f),new Vector3(2.6f,2.2f,1.8f),0,"Arch foot");
            kit.Crossing("Look down the river",new Vector3(39,0,121),new Vector3(3,5,18),"xylos.archSeen","SelaRiverArch","Sela");
            kit.Sign("FIRST ARCH",31.4f,126,1.8f);
            // Ulma's island: the old ring is under water on the upstream side, and the post goes in the fork that faces the town.
            var post=kit.Discovery("Ulma's mooring fork",new Vector3(244.6f,0,70.8f),"Set Senn's mooring post","senn.postSet","SennPost","SelaMooringPost","Taren","MooringPost",0,3);
            var fork=new ZoneKit.Shape();
            fork.Slot(0).Tube(new[]{kit.On(243.6f,70.4f,-.3f),kit.On(243.4f,70.4f,.7f),kit.On(242.8f,70.8f,1.3f)},new[]{.2f,.16f,.1f},6,BarkDark,BarkPale).Tube(new[]{kit.On(243.4f,70.4f,.6f),kit.On(243.9f,69.9f,1.2f)},new[]{.13f,.08f},6,BarkDark,BarkPale);
            // The ring's stump off the east side, half drowned: what Taren wanted to mend.
            fork.Slot(0).Post(new Vector3(262.6f,SouthLevel-.5f,69.2f),.75f,.17f,.14f,6,BarkDark,BarkDark).Disc(new Vector3(262.9f,SouthLevel+.02f,69.8f),Vector3.up,.34f,10,new Color(.5f,.48f,.4f),.24f);
            kit.Built("Ulma fork and ring",fork,Bark);
            var carved=new ZoneKit.Shape();
            carved.Slot(0).Post(kit.On(243.5f,70.5f,.5f),1.7f,.13f,.11f,8,new Color(1,.92f,.74f),new Color(1,.96f,.84f)).Lump(kit.On(243.5f,70.5f,2.25f),new Vector3(.2f,.24f,.2f),8,.1f,new Color(1,.94f,.78f),new Color(.8f,.72f,.56f),8,6);
            carved.Slot(1).Rope(kit.On(243.5f,70.5f,1.5f),kit.On(246,70,.3f),.3f,.035f,new Color(.84f,.76f,.52f));
            var set=kit.Built("Mooring post set",carved.Slot(1),Plank,Cloth);
            var stands=new GameObject("Senn's post stands",typeof(FlagGate)).GetComponent<FlagGate>();stands.flag="senn.postSet";stands.targets=new[]{set};set.SetActive(false);
            kit.Sign("ULMA'S ISLAND",251,72.6f,1.6f);
            // Binna's three: red on the first island, blue in the shade of Marrow-2 on the second, yellow by the Canopy's mooring. Each is a bush with its pods on; picked, the pods are gone.
            void Pods(string colour,Vector3 at,Color pod,int seed)
            {
                // The kit's spice plant, tinted to its kind, with its pods in the kind's own colour on top: the whole plant is what is there to pick, and it is gone once picked.
                var rng=new System.Random(seed);var plant=kit.Prop(ZoneKit.Kit("SpicePodPlant","DressSucculent"),at,1.15f,seed*40,false);plant.name="Spice plant "+colour;
                plant.AddComponent<PropTint>().tint=Color.Lerp(Color.white,new Color(pod.r,pod.g,pod.b)/1.5f,.45f);
                var fruit=new ZoneKit.Shape();
                for(int k=0;k<7;k++){var d=Quaternion.Euler(0,k*51+(float)rng.NextDouble()*20,0)*Vector3.forward*(.25f+.4f*(float)rng.NextDouble());fruit.Slot(0).Lump(d+Vector3.up*(.5f+.3f*(float)rng.NextDouble()),new Vector3(.1f,.17f,.1f),seed+k,.1f,pod,pod*.7f,6,5);}
                var pods=kit.Built("Spice pods "+colour,fruit,Cloth);pods.transform.position=kit.On(at);pods.transform.SetParent(plant.transform,true);pods=plant;
                var pick=kit.Discovery(char.ToUpper(colour[0])+colour.Substring(1)+" spice pods",at+new Vector3(0,0,-1.1f),"Pick the "+colour+" spice pods","xylos.pods."+colour,"SpicePods",null,"Sela","ThreeBaskets",0,2.8f);
                pick.situation="pods."+colour;pick.visual=pods;pods.SetActive(false);
            }
            Pods("red",new Vector3(133,0,80.4f),new Color(1.5f,.24f,.16f),81);Pods("blue",new Vector3(223,0,99.6f),new Color(.3f,.5f,1.5f),82);Pods("yellow",new Vector3(383,0,96.6f),new Color(1.5f,1.2f,.2f),83);
            Stash(kit,"xylos.river.arch","Growers' basket",new Vector3(41,0,125.4f),70,90,ProgressionBuilder.Drop("AmberSap",3,1),ProgressionBuilder.Drop("ChargeCell",1,1));
            Stash(kit,"xylos.river.marrow2","Marrow-2's stores",new Vector3(225,0,102),-30,0,ProgressionBuilder.AnyPart(3,GearSlot.Edge,1),ProgressionBuilder.Drop("RepairGel",1,1));
            Stash(kit,"xylos.river.ulma","Ulma's basket",new Vector3(256.4f,0,67.8f),10,140,ProgressionBuilder.Drop("AmberSap",2,1));
            // On the bar the south channel leaves dry: there for whoever comes back along the bed once the last sluice is open.
            Stash(kit,"xylos.river.bar","Drowned basket",new Vector3(151.5f,0,67.2f),-60,120,ProgressionBuilder.AnyPart(3,GearSlot.Module,1),ProgressionBuilder.Drop("Rootfibre",4,1));
            int n=0;
            foreach(var (x,z) in new[]{(54f,125),(118f,125),(206f,99.6f),(374f,117.6f)})Harvest(kit,"river_sap_"+n,"AmberSap",new Vector3(x,0,z),90+n++);
            n=0;
            foreach(var (x,z) in new[]{(18f,117.4f),(88f,91.6f),(282f,90.8f),(349f,73)})Harvest(kit,"river_fibre_"+n,"Rootfibre",new Vector3(x,0,z),95+n++);

            // ---- fights. Six pockets and the two carriers', growing from one grazer and its flies to mixed packs; Reedstalkers keep to the deep water's edge, Sporecasters to cover.
            EncounterVolume Fight(string id,Vector2 at,Vector2 size,params (string enemy,int count,float dx,float dz)[] packs)
            {
                var encounter=WorldBuilder.Encounter(id,kit.On(at.x,at.y,1.5f),new Vector3(size.x,6,size.y),packs[0].enemy,packs[0].count);
                var spawners=new List<Spawner>{encounter.spawners[0]};spawners[0].transform.position=kit.On(at.x+packs[0].dx,at.y+packs[0].dz);
                for(int i=1;i<packs.Length;i++)spawners.Add(ZoneKit.Spawn(encounter,packs[i].enemy,packs[i].count,kit.On(at.x+packs[i].dx,at.y+packs[i].dz),2.6f));
                encounter.spawners=spawners.ToArray();return encounter;
            }
            Fight("River_0",new Vector2(76,121),new Vector2(24,16),("Thornback",1,3,1),("MireflySwarm",2,-4,3));
            Fight("River_1",new Vector2(132,121),new Vector2(24,16),("Reedstalker",3,3,4));
            Fight("River_Marrow1",new Vector2(48,88),new Vector2(26,16),("Thornback",2,-3,2),("Reedstalker",2,5,-3));
            Fight("River_3",new Vector2(146,86),new Vector2(22,16),("Sporecaster",2,5,2),("MireflySwarm",2,-2,-3));
            Fight("River_Marrow2",new Vector2(214,95),new Vector2(26,16),("Sporecaster",3,5,3),("Thornback",1,-4,-2));
            Fight("River_5",new Vector2(260,98),new Vector2(26,16),("Reedstalker",4,2,5));
            Fight("River_6",new Vector2(296,98),new Vector2(24,16),("Thornback",2,3,2),("Sporecaster",2,-5,4));
            Fight("River_Bar",new Vector2(SouthBar.x,SouthBar.y),new Vector2(16,12),("MireflySwarm",3,0,1),("Reedstalker",2,3,-1.5f));
            // ---- hides. A Reedstalker goes under in reedy water with a ring round it: these are the reed beds it uses, three or four to a pocket at the water's edge (with none
            // in reach it makes its own on the bank). Each has its reeds, standing whether or not anything is in them: where the reeds are thick is where to look for a ring.
            var reeds=new ZoneKit.Shape();int hides=0;
            void Hide(float x,float z)
            {
                var spot=new GameObject("Reed hide "+(++hides),typeof(AmbushSpot));spot.transform.position=kit.On(x,z);spot.GetComponent<AmbushSpot>().radius=3.2f;
                var rr=new System.Random(700+hides);float H()=>(float)rr.NextDouble();
                for(int k=0;k<18;k++)
                {
                    float a=H()*6.28f,far=.3f+1.5f*Mathf.Sqrt(H()),tall=.7f+.5f*H();var foot=kit.On(x+Mathf.Cos(a)*far,z+Mathf.Sin(a)*far,-.05f);var lean=new Vector3((H()-.5f)*.3f,1,(H()-.5f)*.3f);
                    reeds.Slot(0).Tube(new[]{foot,foot+lean*tall*.5f,foot+lean*tall+Vector3.right*.06f},new[]{.03f,.026f,.012f},4,new Color(.5f,.72f,.36f),new Color(.74f,.86f,.5f),1,1,false);
                    if(k%3==0)reeds.Slot(1).Lump(foot+lean*tall*.92f,new Vector3(.045f,.13f,.045f),700+hides*20+k,.05f,new Color(.62f,.44f,.26f),new Color(.4f,.28f,.18f),6,5);
                }
            }
            foreach(float x in BankHides)Hide(x,Waterline(x,123,.2f,-.05f));
            foreach(float x in IslandOneHides)Hide(x,Waterline(x,89,.2f,MiddleDrained+.04f));
            foreach(float x in IslandTwoHides)Hide(x,Waterline(x,99,.2f,-.05f));
            // Round the bar: east, west and north of it. South of it is the levee's foot.
            foreach(float bearing in new[]{20f,150f,90f})Hide(SouthBar.x+Mathf.Cos(bearing*Mathf.Deg2Rad)*8.5f,SouthBar.y+Mathf.Sin(bearing*Mathf.Deg2Rad)*8.5f);
            kit.Built("Reed beds",reeds.Slot(1),Vine,Husk);

            // ---- Seedfall: the gravel fan, Marrow-3 across its back with the nest on her, the pod trees behind, and the Seedthane's ground in front. No door: the fan is open on every
            // side, and the talk at its edge is where a lost fight starts again.
            kit.Sign("SEEDFALL",346,85.4f,2);
            var edge=kit.Crossing("Look at what's on Marrow-3",new Vector3(339.6f,0,88),new Vector3(3,5,60),"xylos.seedfallSeen","SelaSeedfall","Taren");edge.saveSpawn="Seedfall";
            var duel=WorldBuilder.Encounter("Seedthane",kit.On(RiverArena.x,RiverArena.y,1.5f),new Vector3(30,6,28),"Seedthane",1);
            var perch=duel.spawners[0];perch.transform.SetPositionAndRotation(kit.On(362,97.5f),Quaternion.Euler(0,180,0));perch.faceAsPlaced=true;
            duel.requiredFlag="xylos.seedfallSeen";duel.checkpointSpawn="Seedfall";
            if(DevArgs.Has("-xylos-stills"))StillBoss("Seedthane",perch.transform.position,180);
            var seen=new GameObject("Seedthane preview");seen.transform.position=kit.On(341,88);duel.previewPoint=seen.transform;duel.previewRadius=6;edge.previewEncounter=duel;
            // The same talk for whoever comes up onto the fan from the south: off the root bridge from town, or along the drained bed.
            var edgeSouth=kit.Crossing("Look at what's on Marrow-3 from the south",new Vector3(359,0,72.6f),new Vector3(38,5,3),"xylos.seedfallSeen","SelaSeedfall","Taren");edgeSouth.saveSpawn="Seedfall";edgeSouth.previewEncounter=duel;
            var upriver=new GameObject("The Seedthane's way out",typeof(BossExit)).GetComponent<BossExit>();upriver.transform.position=kit.On(374,105.5f);upriver.boss="Seedthane";
            var gone=new GameObject("The Seedthane goes upriver",typeof(AfterTalk)).GetComponent<AfterTalk>();gone.flag="bossdown.Seedthane";gone.node="SelaSeedthaneGone";gone.speaker="Sela";gone.heardFlag="xylos.seedthaneSeenOff";gone.delay=2.5f;
            // Its pods hang over the floor from stalks planted at the fan's rim: bodies to knock down, and cover once they are down (CoverPod; the Seedthane's own moves can drop them too).
            // Each stalk is a piece of its own, planted close to its pod: four in one mesh were one thing thirty metres wide and five high, standing across the camera's side of
            // the fight as far as anything that reads a prop by its box could tell (guide 3.5).
            int podSeed=120;
            foreach(var (x,z,fromX,fromZ) in new[]{(354.5f,95f,350f,100.5f),(370.5f,94.5f,375f,100f),(354f,80.5f,349.5f,75f),(371f,81f,375.5f,75.5f)})
            {
                var stalks=new ZoneKit.Shape();
                Vector3 foot=kit.On(fromX,fromZ),over=kit.On(x,z,3.6f),crest=Vector3.Lerp(foot,over,.55f)+Vector3.up*3.4f;
                var bend=new Vector3[10];var taper=new float[10];var knee=crest+Vector3.up*1.8f;
                for(int i=0;i<10;i++){float t=i/9f;bend[i]=Vector3.Lerp(Vector3.Lerp(foot+Vector3.down*.3f,knee,t),Vector3.Lerp(knee,over,t),t);taper[i]=Mathf.Lerp(.17f,.05f,t);}
                stalks.Slot(0).Tube(bend,taper,6,new Color(.8f,.9f,.56f),new Color(.6f,.78f,.4f));
                stalks.Slot(0).Tube(new[]{over,over+Vector3.down*.9f},new[]{.04f,.04f},4,new Color(.6f,.78f,.4f),new Color(.6f,.78f,.4f),1,1,false);
                // Its stem is a thing of its own over head height, so a pod hangs from something and isn't a body adrift in the air.
                ZoneKit.Block(kit.Built("Pod stalk "+(podSeed-119),stalks,Vine),over+Vector3.down*.45f,new Vector3(.12f,1,.12f),0,"Pod stem");
                var pod=new GameObject("Seed pod "+(podSeed-119));pod.transform.position=kit.On(x,z);
                var hungShape=new ZoneKit.Shape();hungShape.Slot(0).Lump(Vector3.zero,new Vector3(.7f,.98f,.7f),podSeed,.08f,new Color(1,.9f,.56f),new Color(.74f,.56f,.32f),10,7);
                var hung=kit.Built("Hanging pod "+(podSeed-119),hungShape,Husk);hung.transform.SetParent(pod.transform,false);hung.transform.localPosition=Vector3.up*1.9f;
                var health=hung.AddComponent<Health>();health.id="SeedPod";health.maximum=health.integrity=260;health.breakThreshold=100000;health.weakness=DamageType.Kinetic;health.resistance=DamageType.Beam;
                var struck=hung.AddComponent<CapsuleCollider>();struck.radius=.75f;struck.height=2.9f;struck.center=Vector3.down*.45f;hung.AddComponent<Hurtbox>().owner=health;
                var downShape=new ZoneKit.Shape();
                downShape.Slot(0).Lump(new Vector3(0,.62f,0),new Vector3(1.05f,.8f,1.2f),podSeed+7,.14f,new Color(1,.9f,.56f),new Color(.7f,.52f,.3f),10,7).Lump(new Vector3(.5f,.3f,.7f),new Vector3(.5f,.34f,.46f),podSeed+9,.2f,new Color(.96f,.94f,.74f),new Color(.7f,.6f,.4f),8,5);
                var down=kit.Built("Fallen pod "+(podSeed-119),downShape,Husk);down.transform.SetParent(pod.transform,false);down.transform.localPosition=Vector3.zero;
                var cover=down.AddComponent<BoxCollider>();cover.center=Vector3.up*.75f;cover.size=new Vector3(2,1.5f,2.2f);
                var carve=down.AddComponent<NavMeshObstacle>();carve.shape=NavMeshObstacleShape.Box;carve.center=cover.center;carve.size=cover.size;carve.carving=true;carve.carveOnlyStationary=false;
                var cp=pod.AddComponent<CoverPod>();cp.hanging=hung;cp.fallen=down;cp.settledBy="Seedthane";down.SetActive(false);podSeed++;
            }
            kit.Sign("CANOPY MOORING",379,85.4f,1.8f);

            if(!greybox)RiverDress(kit,north,middle,south);

            // ---- limits: the walls are the stop, and a box on each keeps a body from finding a way up one.
            WorldBuilder.Boundary("River north limit",new Vector3(184,6,157),new Vector3(470,24,2));
            WorldBuilder.Boundary("River east limit",new Vector3(405,6,95),new Vector3(2,24,140));
            WorldBuilder.Boundary("River west limit",new Vector3(-34,6,95),new Vector3(2,24,140));
            // The south needs none: the bluff is the stop, and it is drawn.

            // ---- navigation, with every channel down and the pods hung: what is shut at first is play's business.
            var reach=new List<Vector2>{new(34,121),new(76,121),new(132,121),handA,new(104,104),new(100,88),new(103.2f,93.2f),new(96,85),new(112.5f,86),new(110,81.2f),new(48,88),new(35,90),
                new(146,86),new(133,79.3f),new(179,99),new(214,95),new(212,99.2f),new(223,98.5f),new(225,100.5f),new(260,98),new(296,98),handB,new(250.5f,80),new(244.6f,70.8f),new(256.4f,69.4f),
                new(331,92),new(343,90),new(362,88),new(355,99.4f),new(383,95.5f),new(383,90),new(389.4f,90),new(350,66.4f),new(SouthBar.x,SouthBar.y),new(151.5f,65.6f),new(41,123.9f),new(-13,121),
                new(18,119),new(88,90),new(282,92.4f),new(349,74.6f),new(54,123.5f),new(118,123.5f),new(206,98),new(374,116)};
            WaterKit.WithOpenWater(()=>kit.BakeNavigation(new Bounds(new Vector3(184,2,95),new Vector3(470,26,140)),new Vector2(0,121),reach));
            if(!greybox)RiverGround(kit);
            WalkersKeepClear(kit.zone);
            WorldBuilder.Save(scene,kit.zone);Debug.Log("BRAIDED_RIVER_OK");
        }

        // ---------------------------------------------------------------- dressing: drawn only, at the back and the edges
        /// <summary>Where the ground first goes under <paramref name="under"/> on the way from a point along z (a step north or south): the waterline a thing is planted against.</summary>
        static float Waterline(float x,float from,float step,float under=0)
        {
            float z=from;for(int i=0;i<160&&RiverHeight(new Vector2(x,z))>under;i++)z+=step;return z;
        }
        /// <summary>The jungle. Nothing with a crown stands over ground a body can walk. The fixed camera sees about twelve metres north of whoever it follows, so the far wall of
        /// trees is the back of the overviews and never of a play frame: what a play frame has behind the party is the main stem and the trees standing in its margin, with their
        /// flared feet in the water where it is over a head (nobody is ever behind one); roots thrown over a channel land on a shore beside the paths; and the islands carry only
        /// roots, ferns and the carriers. The levee beyond the south channel is the camera's foreground and stays low.</summary>
        static void RiverDress(ZoneKit kit,WaterVolume north,WaterVolume middle,WaterVolume south)
        {
            var rng=new System.Random(47);float R()=>(float)rng.NextDouble();
            // What lies on the water, so a channel is never one flat sheet: pads in rafts along the margins, each raft a few leaves of different sizes with a pale flower or two.
            // They belong to their water and go down with it: a drained channel has them lying on its bed.
            void Afloat(WaterVolume water,int system,float xMin,float xMax,float zMin,float zMax,int seed)
            {
                var pads=new ZoneKit.Shape();var fl=new System.Random(seed);float F()=>(float)fl.NextDouble();int rafts=0;
                for(float x=xMin;x<xMax;x+=5.5f)for(float z=zMin;z<zMax;z+=5.5f)
                {
                    var c=new Vector2(x+F()*5.5f,z+F()*5.5f);int leaves=4+fl.Next(6);float turn=F()*6.28f;bool keep=F()<.62f;
                    if(RiverSystem(c)!=system)continue;float depth=water.level-RiverHeight(c);
                    // Out of the fast middle of the north channel, off the ford and the crossing by the ferry stage.
                    if(!keep||depth<.3f||depth>(system==0?1.25f:1.6f)||Mathf.Abs(c.x-104)<5&&system==1||Vector2.Distance(c,new Vector2(250.5f,80))<7)continue;
                    for(int k=0;k<leaves;k++)
                    {
                        float a=turn+k*2.4f,far=.3f+.42f*Mathf.Sqrt(k),size=.26f+.3f*F();var at=new Vector3(c.x+Mathf.Cos(a)*far,water.level+.025f+.004f*k,c.y+Mathf.Sin(a)*far);
                        if(RiverHeight(new Vector2(at.x,at.z))>water.level-.12f)continue;
                        pads.Slot(0).Disc(at,Vector3.up,size,9,Color.Lerp(LeafTop,LeafGold,F()*.5f),0,LeafUnder);
                        if(k%4==1)pads.Slot(1).Lump(at+Vector3.up*.07f,new Vector3(.11f,.08f,.11f),seed+k,.2f,new Color(1.3f,1.2f,1.1f),new Color(1.2f,.8f,.9f),6,4);
                    }
                    rafts++;
                }
                if(pads.Vertices==0)return;
                var lying=kit.Built(water.surface.name+" pads",pads.Slot(1),PadLeaf,PadFlower);lying.transform.SetParent(water.surface,true);
                Debug.Log("RIVER_PADS "+water.surface.name+" rafts="+rafts);
            }
            Afloat(north,0,-40,400,100,150,301);Afloat(middle,1,24,196,86,120,302);Afloat(south,2,-40,348,46,118,303);
            // Flecks of foam going down with the current (west), fastest in the main stem: the water is seen to run.
            var foam=kit.Glow("Foam",false,new Color(.9f,1,.95f,1));
            void Current(WaterVolume water,Vector3 centre,float along,float across,int count,float speed)
            {
                var flecks=kit.Drift(water.surface.name+" current "+Mathf.RoundToInt(centre.x),new Vector3(centre.x,water.level+.04f,centre.z),new Vector3(across,.02f,along),foam,new Color(.92f,1,.96f,.5f),count,.1f,.3f,speed,along/speed*.33f);
                flecks.transform.rotation=Quaternion.Euler(0,-90,0);flecks.transform.SetParent(water.surface,true);
            }
            Current(north,new Vector3(56,0,137.5f),188,11,420,2.2f);Current(north,new Vector3(258,0,119),122,15,360,2.2f);Current(north,new Vector3(372,0,130),52,7,90,2.2f);
            Current(middle,new Vector3(96,0,105.5f),136,8,240,1.1f);Current(south,new Vector3(150,0,66.5f),372,11,460,1.3f);
            // The back wall: three ranks of trees up the bank, the nearest at its foot with their roots in the water's edge.
            var wall=new ZoneKit.Shape();
            for(float x=-36;x<400;x+=5.5f+R()*3)
            {
                float foot=WallZ(x);
                for(int rank=0;rank<3;rank++)
                {
                    float z=foot+2.5f+rank*4.6f+R()*2.4f,tall=8+rank*2.6f+R()*4,girth=.5f+R()*.5f;var at=new Vector3(x+R()*3-1.5f,0,z);
                    JungleTree(wall,kit.On(at),tall,girth,(int)(x*13)+rank,3.4f+R()*2.2f);
                }
            }
            kit.Built("Jungle at the back",wall.Slot(1),Bark,CanopyLeaf);
            // Trees along every north shore, their feet at the water's edge: the back of each view from the bank and from the second island, a few strides north of the walked
            // line, where a trunk and its flared roots are in the frame and its crown is over the water (the camera never loses ground under a crown that stands north of it).
            // Each trunk is solid. Kept off the arches' feet, the gates, the carriers and whatever is there to be picked up.
            var shore=new ZoneKit.Shape();var trunks=new List<(Vector3 foot,float girth)>();
            float[] keepOff={36,41,54,88,118,127,150,214,246,286};float[] room={7,3,3,3.5f,3,3.5f,6,11,3.5f,3.5f};
            for(float x=-30;x<396;x+=6.5f+R()*5.5f)
            {
                float back=R()*.5f,tall=8.5f+R()*4,girth=.42f+R()*.34f,crown=3.2f+R()*1.6f;bool clear=!(x>158&&x<204||x>311&&x<352);
                for(int k=0;k<keepOff.Length;k++)if(Mathf.Abs(x-keepOff[k])<room[k])clear=false;
                foreach(float hide in BankHides)if(Mathf.Abs(x-hide)<2.6f)clear=false;
                foreach(float hide in IslandTwoHides)if(Mathf.Abs(x-hide)<2.6f)clear=false;
                if(!clear)continue;
                float z=Waterline(x,LineZ(NorthLine,x)+2,.3f,.2f)-.2f-back;if(z>WallZ(x)-5)continue;
                var foot=kit.On(x,z);JungleTree(shore,foot,tall,girth,(int)(x*11)+5,crown);trunks.Add((foot,girth));
            }
            var shoreBody=kit.Built("Shore trees",shore.Slot(1),Bark,CanopyLeaf);
            foreach(var (foot,girth) in trunks)ZoneKit.Block(shoreBody,foot+Vector3.up*1.6f,new Vector3(girth*2.1f,3.4f,girth*2.1f),0,"Shore trunk");
            Debug.Log("RIVER_SHORE_TREES "+trunks.Count);
            // Their roots run out over the ground they stand on, half buried, south across the strip and along the shore: every walked stretch has a few lines in it that lead back to
            // a trunk, which is what a jungle floor is, and the islands' middles stop being one lawn. Ridges a hand high, drawn only: a foot steps over them. Their own random, so the
            // dressing drawn after them stands where it did.
            var roots=new ZoneKit.Shape();var rr=new System.Random(57);float Q()=>(float)rr.NextDouble();int ran=0;
            foreach(var (foot,girth) in trunks)
            {
                int count=4+rr.Next(3);
                for(int k=0;k<count;k++)
                {
                    float bearing=Mathf.Lerp(-170,-10,(k+Q()*.8f)/count)*Mathf.Deg2Rad,length=3+5.5f*Q(),wander=(Q()-.5f)*1.2f,phase=Q()*6;var path=new List<Vector3>();var radii=new List<float>();
                    for(int i=0;i<=10;i++)
                    {
                        float t=i/10f,turn=bearing+wander*t+Mathf.Sin(t*4+phase)*.18f;var q=new Vector2(foot.x,foot.z)+new Vector2(Mathf.Cos(turn),Mathf.Sin(turn))*(girth*.8f+length*t);
                        if(RiverHeight(q)<RiverLevel(q)+.15f)break;float r=Mathf.Lerp(girth*.42f,.05f,t);
                        path.Add(new Vector3(q.x,kit.Y(q.x,q.y)-r*.45f,q.y));radii.Add(r);
                    }
                    if(path.Count>=4){roots.Slot(0).Tube(path,radii,6,BarkDark,BarkPale,1,.3f);ran++;}
                }
            }
            kit.Built("Shore roots",roots,Bark);Debug.Log("RIVER_SURFACE_ROOTS "+ran);
            // The west bank stands behind the town gate, and the jungle stands on it: two ranks at its lip from the bluff to the back wall, so the river's head is trees and not a
            // shelf of bare ground.
            var west=new ZoneKit.Shape();
            for(float z=36;z<152;z+=4.4f+Q()*2.6f)for(int rank=0;rank<2;rank++)
            {
                float x=WestFoot-3.6f-rank*5f-Q()*1.8f,zz=z+rank*2.2f;
                JungleTree(west,kit.On(x,zz),8+rank*3+Q()*4,.45f+Q()*.45f,(int)(zz*19)+rank,3.2f+Q()*2);
            }
            kit.Built("Jungle on the west bank",west.Slot(1),Bark,CanopyLeaf);
            // Snags in the main stem's margin: trunks the storm brought down, lying half under where the water is over a head, and burls of root standing out of it. They are what
            // the current is read against.
            var snags=new ZoneKit.Shape();
            for(float x=-24;x<392;x+=9+R()*9)
            {
                float turn=R()*360,length=3.5f+R()*3.5f,girth=.22f+R()*.16f,off=1.2f+R()*3.4f;bool burl=R()<.4f;
                if(Mathf.Abs(x-36)<5||x>160&&x<202||x>313&&x<350||Mathf.Abs(x-214)<10)continue;
                float z=Waterline(x,LineZ(NorthLine,x)+2,.4f,-.85f)+off;if(z>WallZ(x)-3||RiverSystem(new Vector2(x,z))!=0||RiverHeight(new Vector2(x,z))>-.8f)continue;
                var at=new Vector3(x,0,z);
                if(burl){for(int k=0;k<3;k++)snags.Slot(0).Lump(at+new Vector3(k*.9f-.9f,-.1f+.1f*k,(k%2)*.7f-.3f),new Vector3(.55f+.2f*k,.42f+.1f*(k%2),.5f+.15f*k),(int)(x*5)+k,.3f,BarkPale,BarkDark,9,6);continue;}
                var along=Quaternion.Euler(0,turn,0)*Vector3.forward;
                snags.Slot(0).Tube(new[]{at-along*length*.5f+Vector3.down*.35f,at+Vector3.up*.12f,at+along*length*.5f+Vector3.up*.34f},new[]{girth*1.3f,girth*1.1f,girth*.7f},7,BarkDark,BarkPale,1,.25f);
                snags.Slot(0).Tube(new[]{at+along*length*.2f+Vector3.up*.2f,at+along*length*.3f+Vector3.up*.9f+Vector3.Cross(Vector3.up,along)*.5f},new[]{girth*.4f,girth*.2f},5,BarkDark,BarkPale);
                snags.Slot(1).Lump(at+Vector3.up*(.12f+girth),new Vector3(girth*1.4f,.1f,.8f),(int)x,.3f,LeafTop,LeafUnder,7,4);
            }
            kit.Built("Snags",snags.Slot(1),Bark,Leaf);
            // The Canopy's flank across the east end: a wall of root with the island's own trees above it.
            var flank=new ZoneKit.Shape();
            for(float z=34;z<158;z+=4.2f+R()*2.2f)
            {
                float x=401.5f+R()*3;JungleTree(flank,kit.On(x,z),11+R()*5,.7f+R()*.6f,(int)(z*17),4+R()*2);
                if(Mathf.Abs(z-90)>8)RootSpan(flank,kit.On(397.5f,z-2.4f),kit.On(397.5f,z+2.4f),2.2f+R()*1.6f,.3f,(int)(z*5));
            }
            kit.Built("The Canopy's flank",flank.Slot(1),Bark,CanopyLeaf);
            // The bluff's top: low growth only, saplings under two and a half metres set back from its edge, so nothing on the camera's side of the south channel tops the sightline.
            var leveeGrowth=new ZoneKit.Shape();
            for(float x=-36;x<400;x+=4.5f+R()*4)
            {
                float z=LeveeZ(x)-5-R()*5;if(RiverHeight(new Vector2(x,z))<2.6f)continue;
                JungleTree(leveeGrowth,kit.On(x,z),1.5f+R()*.8f,.16f+R()*.1f,(int)(x*7)+3,1.1f+R()*.4f);
            }
            kit.Built("Levee growth",leveeGrowth.Slot(1),Bark,CanopyLeaf);
            // "Under root arches": roots thrown over the main stem from the jungle to a north shore, and over the middle channel from the bank to the first island. Each is its own
            // piece, so the view opens a hole in the one a hero is behind and in no other; each foot on a shore is solid.
            int span=0;
            void Thrown(float x,float fromZ,float toZ,float rise,float thick,int seed)
            {
                var arch=new ZoneKit.Shape();Vector3 a=kit.On(x,fromZ),b=kit.On(x+1.5f,toZ);
                RootSpan(arch,a,b,rise,thick,seed);Hung(arch,1,a,b,rise,thick,9,seed+50);
                var body=kit.Built("Root arch "+(++span),arch.Slot(1),Bark,Vine);
                foreach(var foot in new[]{a,b})if(foot.z<WallZ(x))ZoneKit.Block(body,foot+Vector3.up*.8f,new Vector3(thick*3.4f,2,thick*3.4f),0,"Arch foot");
                ZoneKit.Fadeable(body);
            }
            foreach(var (x,seed) in new[]{(88f,205),(127f,206)})Thrown(x,WallZ(x)+1.5f,Waterline(x,LineZ(NorthLine,x)+1,.3f)-.5f,6.6f+R()*1.4f,.5f,seed);
            foreach(var (x,seed) in new[]{(246f,207),(286f,208)})Thrown(x,WallZ(x)+1.5f,Waterline(x,LineZ(NorthLine,x)+3,.3f)-.5f,6.4f+R()*1.4f,.5f,seed);
            foreach(var (x,seed) in new[]{(70f,201),(136f,202)})Thrown(x,Waterline(x,LineZ(NorthLine,x)-1,-.3f)+.6f,Waterline(x+1.5f,LineZ(SouthLine,x)+1,.3f)-.6f,5.8f+R(),.46f,seed);
            // Seed-pod trees at the back of Seedfall, behind Marrow-3: tall, bare-limbed, hung with pale pods that catch the light.
            var grove=new ZoneKit.Shape();
            foreach(var (x,z) in new[]{(350f,116f),(358f,119f),(366.5f,120.5f),(374.5f,119f),(380.5f,115f)})
            {
                JungleTree(grove,kit.On(x,z),9+R()*3,.5f+R()*.2f,(int)(x*3),3+R());
                for(int k=0;k<5;k++)
                {
                    var hang=kit.On(x+R()*5-2.5f,z+R()*3-3.4f,5.2f+R()*2.4f);
                    grove.Slot(0).Tube(new[]{hang+Vector3.up*1.6f,hang},new[]{.03f,.03f},4,new Color(.6f,.78f,.4f),new Color(.6f,.78f,.4f),1,1,false);
                    grove.Slot(2).Lump(hang+Vector3.down*.3f,new Vector3(.3f,.44f,.3f),(int)(x+k),.1f,new Color(1,.92f,.6f),new Color(.8f,.6f,.34f),8,6);
                }
            }
            kit.Built("Seed-pod trees",grove.Slot(2),Bark,CanopyLeaf,Husk);
            // What the camp is made of: the ferry hands' awning at the back of it (the kit's stall, its baskets full), and crates of seed beside it. Until the piece is taken
            // in, a leaf awning on four poles.
            var awning=new ZoneKit.Shape();
            if(ZoneKit.Landed("MarketAwning"))kit.Prop("MarketAwning",new Vector3(116.6f,0,93),2.6f,200).name="Ferry hands awning";
            else
            {
                foreach(var (x,z) in new[]{(113.8f,91.2f),(119.4f,91.2f),(113.8f,94.8f),(119.4f,94.8f)})awning.Slot(0).Post(kit.On(x,z,-.2f),2.5f+(z>93?.5f:0),.1f,.08f,6,BarkDark,BarkPale);
                awning.Slot(1).Quad(kit.On(113.2f,90.6f,2.3f),kit.On(120,90.6f,2.3f),kit.On(120,95.4f,2.95f),kit.On(113.2f,95.4f,2.95f),Vector3.up,new Color(.84f,.9f,.5f),.3f);
            }
            for(int k=0;k<4;k++)awning.Slot(2).Box(kit.On(121.6f+.3f*(k%2),90.6f+k*1.2f,.36f),new Vector3(.9f,.7f,.8f),Quaternion.Euler(0,k*17,0),new Color(.8f,.72f,.56f),new Color(.96f,.9f,.74f));
            var stores=kit.Built("Camp stores",awning.Slot(2),Bark,Thatch,Plank);ZoneKit.Block(stores,kit.On(121.7f,92.4f,.36f),new Vector3(1.3f,.72f,4.6f),0,"Seed crates");
            // The sluice keepers' house, at the back of the north bank where it runs out to the wheel: whoever turns these gates lives beside them. A seed house, smaller than the town's.
            if(ZoneKit.Landed("SeedHouse"))kit.Prop("SeedHouse",new Vector3(150,0,124.4f),5,205).name="Sluice keepers house";
            // Pollen in the sun over the whole river, and midges over the water.
            var motes=kit.Glow("Motes",false,new Color(1.4f,1.3f,.8f,1));
            kit.Drift("Pollen",new Vector3(184,3,95),new Vector3(450,5,110),motes,new Color(1,.95f,.6f,.3f),1300,.05f,.16f,.22f,18);
            kit.Drift("Midges",new Vector3(184,.9f,84),new Vector3(430,1.2f,70),motes,new Color(.9f,1,.8f,.26f),600,.03f,.07f,.7f,7);
        }

        /// <summary>The floor itself: leaf litter in drifts on the banks, river gravel on the fan, the bars and the beds, damp earth along each waterline, a trodden line where the
        /// growers walk; ferns and tufts in patches, reeds at the water's edge, flowers where the camp and Ulma kept the ground open. Leaf shadow sways over all of it.</summary>
        static void RiverGround(ZoneKit kit)
        {
            var ground=GroundKit.Open(kit.zone,GroundKit.FindFloor(),59,2.4f);
            float Smooth(float a,float b,float x)=>GroundKit.Smooth(a,b,x);
            float Over(Vector2 p)=>RiverHeight(p)-RiverLevel(p);
            float Dry(Vector2 p)=>Smooth(.12f,.4f,Over(p));
            float Wet(Vector2 p){float over=Over(p);return Smooth(-.5f,-.1f,over)*(1-Smooth(.25f,.6f,over));}
            float Wetness(Vector2 p)=>Smooth(1.1f,.3f,Over(p));
            float Fan(Vector2 p)=>1-Smooth(15,25,Vector2.Distance(p,RiverArena));
            float Land(Vector2 p)=>Dry(p)*(RiverHeight(p)<RiverTop+1?1:0);
            Vector2[] bankWay={new(-13,121.4f),new(6,123.4f),new(34,124.4f),new(76,124.5f),new(120,124),new(138,121.5f),new(151,118.2f),new(166,118.4f)},
                firstWay={new(40,91),new(70,92),new(92,89),new(104,88.5f),new(124,88),new(140,91),new(158,88.5f)},
                crossWay={new(158,88.5f),new(179,97),new(200,97),new(214,99.6f),new(260,102.5f),new(300,103),new(317,103)},
                fallWay={new(322,101),new(331,95),new(340,90),new(362,88),new(389,90)},ulmaWay={new(250,88),new(250.5f,80),new(250,73)};
            var ways=new[]{bankWay,firstWay,crossWay,fallWay,ulmaWay};
            ground.ClearLane(bankWay,1.7f).ClearLane(firstWay,1.5f).ClearLane(crossWay,1.5f).ClearLane(fallWay,1.6f).ClearLane(ulmaWay,1.3f).Look("River",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Generated/Xylos/xylos-bark.png"));
            // The damp band at each waterline tinted up: at .72 its ragged, inked edge ran along every shore as a black saw-tooth.
            Color litter=new(.82f,.74f,.43f),gravel=new(.8f,.84f,.86f),damp=new(.86f,.92f,.8f);
            ground.Layers(new Vector4(.05f,.34f,.4f,.3f),1.3f,new GroundKit.Layer("litter",litter,3.6f),new GroundKit.Layer("gravel",gravel,3.4f),new GroundKit.Layer("damp",damp,4.5f));
            ground.PaintPatches(0,7,.45f,p=>Dry(p)*(1-.75f*Fan(p)),1);
            // The fan is gravel the floods spread, in tongues with bare earth between them: never one sheet of it.
            ground.Paint(1,p=>Mathf.Max(Fan(p)*Dry(p)*(.4f+.6f*Smooth(.36f,.54f,ground.Noise(p,8,21))),Mathf.Max(Smooth(-.2f,-.55f,Over(p)),1-Smooth(4.5f,8,Vector2.Distance(p,SouthBar))))).PaintPatches(1,5,.22f,Dry,4);
            // The trodden line: worn to gravel and bare earth, broken where it crosses a bed.
            foreach(var way in ways)ground.PaintPath(1,way,1.05f,.8f,.4f,.7f,(int)way[0].x);
            // The bar stands a hand above its channel and is gravel to its top: left to the damp it was one dark disc in the water.
            ground.Paint(2,p=>Wet(p)*Smooth(5,9,Vector2.Distance(p,SouthBar)));
            ground.Shade(p=>.1f*Smooth(1.2f,.2f,Over(p))*Dry(p));
            // A broad light and dark over the gravel, a few metres across: one tile of it every 3.4 m read as the same stones over and over on the fan (s20 359, November 22).
            ground.Shade(p=>.16f*(ground.Noise(p,7,51)-.5f)*Land(p)*Mathf.Max(Fan(p),.35f));

            ground.Decals(new[]{"Litter"},10,p=>Land(p)*(1-Fan(p)),1.1f,2.2f,new Color(.7f,.66f,.36f,.9f),false);
            ground.Decals(new[]{"PebblesA","PebblesB"},5,p=>Land(p)*Mathf.Max(Fan(p),.3f),.8f,1.7f,new Color(.66f,.64f,.56f));
            // Seedfall is named for what lies on it: spilt seed in drifts, pale against the gravel, and husks among it.
            ground.Decals(new[]{"PebblesA","PebblesB"},13,p=>Land(p)*Fan(p),.7f,1.5f,new Color(1,.9f,.6f),true,.1f,31);
            // Thin and near the gravel's own colour, so seed lies on the fan: pale and solid at .6 they were flat stickers on the green (s20 327, November 22).
            ground.Decals(new[]{"SandDrift"},5,p=>Land(p)*Fan(p),1.6f,3.2f,new Color(.88f,.84f,.62f,.32f),false,.1f,32);
            ground.Rocks(7,p=>Land(p)*Fan(p),.1f,.32f,new Color(1,.84f,.5f),3,.12f,33);
            // Round each carrier, the seed she spilled going aground: gold in drifts on the bank beside her, thinning out across her island.
            Vector2[] carriers={new(28,99),new(214,105.6f)};float Spill(Vector2 p){float d=float.PositiveInfinity;foreach(var c in carriers)d=Mathf.Min(d,Vector2.Distance(p,c));return 1-Smooth(5,13,d);}
            ground.Decals(new[]{"PebblesA","PebblesB"},26,p=>Land(p)*Spill(p),.9f,1.8f,new Color(1,.88f,.52f),true,.1f,34);
            ground.Decals(new[]{"Litter"},8,p=>Land(p)*Spill(p),1.2f,2.4f,new Color(.86f,.76f,.42f,.9f),false,.1f,35);
            ground.Cover(new[]{"TuftA","TuftB","Sprouts"},22,p=>Land(p)*Spill(p)*Smooth(.3f,.5f,ground.Noise(p,3,41)),.14f,.3f,new Color(.6f,.84f,.44f),2);
            ground.Rocks(3,p=>Land(p)*Spill(p),.1f,.26f,new Color(.82f,.78f,.66f),2,.12f,42);
            ground.Decals(new[]{"Lichen"},6,p=>Land(p)*Smooth(.46f,.62f,ground.Noise(p,13,3)),1.2f,2.6f,new Color(.4f,.64f,.36f,.9f),false);
            ground.Decals(new[]{"Stain"},3,p=>Land(p)*Mathf.Max(Wetness(p),.4f),2.2f,4,new Color(.36f,.42f,.32f,.7f),false);
            foreach(var way in ways)ground.Strip("Tracks",way,.2f,1.1f,new Color(.5f,.44f,.3f,.5f));
            Color fern=new(.5f,.8f,.38f),reed=new(.6f,.8f,.42f),dull=new(.6f,.62f,.4f);
            ground.Cover(new[]{"Fern"},13,p=>Land(p)*Smooth(.44f,.6f,ground.Noise(p,9,7))*(1-Fan(p)),.26f,.54f,fern,3);
            ground.Cover(new[]{"TuftA","TuftB","TuftC","Sprouts"},15,p=>Land(p)*Smooth(.36f,.56f,ground.Noise(p,7,9))*(1-.7f*Fan(p)),.14f,.32f,new Color(.56f,.82f,.42f),3);
            ground.Cover(new[]{"Reeds"},16,p=>Dry(p)*Wetness(p)*Smooth(.34f,.5f,ground.Noise(p,6,11)),.34f,.58f,reed,3);
            ground.Cover(new[]{"FungusA","FungusB"},2,p=>Land(p)*Smooth(.6f,.7f,ground.Noise(p,11,5)),.12f,.26f,new Color(.95f,.86f,.68f),2);
            ground.Cover(new[]{"Flowers"},9,p=>Land(p)*(Vector2.Distance(p,RiverCamp)<15||Vector2.Distance(p,new Vector2(251,70))<8?Smooth(.4f,.56f,ground.Noise(p,4,13)):0),.16f,.3f,new Color(.96f,.86f,.82f),2);
            ground.Cover(new[]{"Twigs"},3,p=>Land(p)*Mathf.Max(Fan(p),.2f),.16f,.3f,dull);
            ground.Rocks(4,p=>Mathf.Max(Land(p)*Fan(p),Land(p)*.25f),.08f,.3f,new Color(.7f,.7f,.62f));
            // Leaf shadow from a canopy that is never drawn overhead: it sways where it lies and drifts a little downstream.
            // At .42 and crisp the shade lay on the channels as broad pale-and-dark camouflage (the toon water is lit like the floor): kept light and soft-edged, a shade the
            // eye reads as leaves.
            ground.MovingShade("dapple",13,new Vector2(-.12f,.05f),.24f,.5f,.09f,new Vector2(.5f,.3f),new Vector2(4,7));
            ground.Finish();
        }
    }
}
