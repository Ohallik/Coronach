using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>Driftreed (town). Map brief and evidence: docs/maps/Driftreed.md.</summary>
    public static partial class WendmireBuilder
    {
        // ---------------------------------------------------------------- the ground: woven rafts on the mire
        internal const float ReedWater=-.7f,ReedBed=-2.3f,RaftRimTall=.45f,RaftRimWide=1f,WalkHalf=1.2f;
        /// <summary>The rafts, each a rounded rectangle (centre, half its size, turned by yaw degrees, the corners' radius): the pilots' stage, the stilt market and the smokehouse's lobe (overlapping it by more than two rims' width, or the rims rise where the two meet),
        /// the weaving hall's raft, Quill's pump raft, the school raft and the gate raft.</summary>
        internal static readonly (Vector2 c,Vector2 half,float yaw,float round)[] Rafts={(new(0,12),new(15,6),0,2.5f),(new(0,46),new(16,12),0,3),(new(21,41),new(10,6.5f),0,2.5f),
            (new(0,86),new(20,11),0,3),(new(-40,44),new(10,9),8,2.5f),(new(40,58),new(10,8),-10,2.5f),(new(46,96),new(8,7),15,2.5f)};
        /// <summary>Rope walks (each end where it meets a raft's edge). The first three are fixed; S1 and S2 swing away when the tide turns, S3 and S4 swing across (<see cref="TideSwing"/>).
        /// Each swings about its first end, by <c>away</c> degrees from across.</summary>
        internal static readonly (string name,Vector2 from,Vector2 to,int tide,float away)[] Walks=
        {
            ("Stage walk",new(0,18),new(0,34),0,0),("Hall walk",new(0,58),new(0,75),0,0),("Pump walk",new(-16,46),new(-30.2f,46),0,0),
            ("School walk",new(16,54),new(29.3f,55),-1,78),("Stage-pump walk",new(-10,18),new(-33.8f,35.8f),-1,58),
            ("Hall-school walk",new(20,80),new(37.45f,66.6f),1,-52),("Gate walk",new(45.3f,65.2f),new(45.3f,88.6f),1,-80),
        };
        static Vector2 Turn(Vector2 v,float degrees){float a=degrees*Mathf.Deg2Rad,c=Mathf.Cos(a),s=Mathf.Sin(a);return new Vector2(v.x*c-v.y*s,v.x*s+v.y*c);}
        static float RoundBox(Vector2 p,Vector2 c,Vector2 half,float yaw,float round)
        {
            var q=Turn(p-c,-yaw);var d=new Vector2(Mathf.Abs(q.x)-half.x+round,Mathf.Abs(q.y)-half.y+round);
            return new Vector2(Mathf.Max(d.x,0),Mathf.Max(d.y,0)).magnitude+Mathf.Min(Mathf.Max(d.x,d.y),0)-round;
        }
        /// <summary>Metres from the edge of the rafts, negative on them.</summary>
        internal static float RaftLand(Vector2 p){float d=float.PositiveInfinity;foreach(var (c,half,yaw,round) in Rafts)d=Mathf.Min(d,RoundBox(p,c,half,yaw,round));return d;}
        /// <summary>Where a rim is left open: each end of each walk, from a step outside the raft's edge in through the rim's whole depth, a walk's width across.</summary>
        static IEnumerable<(Vector2 from,Vector2 to)> Openings()
        {
            // Deep enough in for a walk that meets its raft's edge aslant: measured along the walk, the rim is deeper than it is across.
            foreach(var (_,a,b,_,_) in Walks){var d=(b-a).normalized;yield return (a+d*.5f,a-d*(RaftRimWide+1.6f));yield return (b-d*.5f,b+d*(RaftRimWide+1.6f));}
        }
        static readonly (Vector2 from,Vector2 to)[] OpeningList=Openings().ToArray();
        static float Opening(Vector2 p){float open=0;foreach(var (from,to) in OpeningList)open=Mathf.Max(open,1-ZoneKit.Smooth(WalkHalf,WalkHalf+.25f,ZoneKit.Segment(p,from,to)));return open;}
        /// <summary>The town's ground. Rafts of woven reed lying on the water, each with a knee-high rim of bundled reed along every edge, steep both sides, so the edge of the floor is
        /// something drawn and nobody walks off into the mire; open only where a rope walk lands. Mud banks north and east carry the mangroves; south and west stay open water,
        /// the fixed camera's side (D119).</summary>
        static float ReedHeight(Vector2 p)
        {
            float low=RaftLand(p);
            // The weave's own swell: a few centimetres, gentler than a step (GroundRelief.Check).
            float relief=.035f*(Mathf.PerlinNoise(p.x*.14f+3,p.y*.14f+9)-.5f),lumpy=.82f+.36f*Mathf.PerlinNoise(p.x*.42f,p.y*.42f);
            float rim=RaftRimTall*lumpy*ZoneKit.Smooth(-RaftRimWide,-RaftRimWide+.35f,low)*(1-Opening(p));
            float y=low<0?relief+rim:Mathf.Lerp(relief+rim,ReedBed,ZoneKit.Smooth(0,.6f,low));
            float bank=ZoneKit.Smooth(110,116,p.y)*(2.4f+1.4f*Mathf.PerlinNoise(p.x*.05f,1))+ZoneKit.Smooth(72,78,p.x)*(2.4f+1.4f*Mathf.PerlinNoise(2,p.y*.05f));
            return Mathf.Max(y,ReedBed+bank);
        }
        /// <summary>The floor's own colour: straw-gold mat, paler where feet go, darker and damp toward each rim; a rim's crest is the green-brown of bundled reed.</summary>
        static Color ReedTone(Vector2 p)
        {
            float low=RaftLand(p),edge=-low;
            // The mire's bed, under the water: dark peat, so neither the weave nor the shade drifting over the floor shows through the water as blotches.
            if(low>.4f){float silt=.8f+.2f*Mathf.PerlinNoise(p.x*.08f,p.y*.08f);return new Color(.2f*silt,.22f*silt,.17f*silt,1);}
            float worn=0;foreach(var way in TownWays)worn=Mathf.Max(worn,1-ZoneKit.Smooth(.9f,2.4f,ZoneKit.FromPath(p,way)));
            float mottle=.9f+.2f*Mathf.PerlinNoise(p.x*.21f+3,p.y*.21f+7),patch=Mathf.PerlinNoise(p.x*.07f+11,p.y*.07f+5);
            var mat=Color.Lerp(new Color(.8f,.7f,.48f),new Color(.86f,.78f,.54f),ZoneKit.Smooth(.35f,.7f,patch));
            var tone=Color.Lerp(mat,new Color(.95f,.88f,.68f),worn*.7f);
            tone=Color.Lerp(tone,new Color(.5f,.48f,.36f),(1-ZoneKit.Smooth(1.1f,3f,edge))*.7f);
            if(edge<RaftRimWide-.3f&&Opening(p)<.5f)tone=new Color(.56f,.52f,.34f);
            return new Color(tone.r*mottle,tone.g*mottle,tone.b*mottle,1);
        }

        // ---------------------------------------------------------------- who and what stands where (plan coordinates; routes, tests and LookDev read these)
        internal static readonly Vector2 ReedArrival=new(0,8.5f),ReedPad=new(-8,11),MaruAtStage=new(9.5f,13.5f),LineAt=new(13.4f,10.6f),LineMoored=new(22.6f,9.5f),LineAdrift=new(31,4),MaruMast=new(12.2f,16),
            NoticeAt=new(-6,39.5f),DovAt=new(19.5f,45),SmokehouseAt=new(26.5f,42.5f),QuillAt=new(-34.5f,46),PressAt=new(-43.5f,49.5f),FrameAt=new(-42.5f,39.6f),PumpSpawn=new(-36,41.5f),
            HallAt=new(-6,89.5f),OnaAt=new(-.5f,82.2f),StoreAt=new(-15.5f,84.5f),PimAt=new(10.5f,82),SchoolAt=new(36.5f,55.5f),GateAt=new(48.2f,99.4f),MireSpawn=new(45.5f,92.2f),AbriCallAt=new(0,26),MarketAt=new(0,46);
        static readonly Vector2[] ReedPeople={MaruAtStage,DovAt,QuillAt,OnaAt,PimAt};
        /// <summary>The ways feet go between the town's places: nothing is scattered within reach of these, and the ground kit keeps them clear.</summary>
        static readonly Vector2[][] TownWays=
        {
            new[]{ReedArrival,new Vector2(0,18),new Vector2(0,34),MarketAt,new Vector2(0,58),new Vector2(0,75),new Vector2(1,78.5f),OnaAt},
            new[]{ReedArrival,new Vector2(7,10.5f),LineAt},new[]{ReedArrival,ReedPad},new[]{new Vector2(0,13),MaruAtStage},
            new[]{MarketAt,new Vector2(-16,46),new Vector2(-30.2f,46),QuillAt},new[]{QuillAt,new Vector2(-38,44),PumpSpawn},new[]{QuillAt,new Vector2(-40,48.5f),PressAt},
            new[]{MarketAt,new Vector2(10,45.5f),DovAt},new[]{new Vector2(-1,42),NoticeAt},
            new[]{MarketAt,new Vector2(12,52),new Vector2(16,54),new Vector2(29.3f,55),SchoolAt},
            new[]{new Vector2(-6,16),new Vector2(-10,18),new Vector2(-33.8f,35.8f),new Vector2(-35,40),QuillAt},
            new[]{new Vector2(1,78.5f),new Vector2(12,80.5f),new Vector2(20,80),new Vector2(37.45f,66.6f),new Vector2(40,63)},
            new[]{new Vector2(40,63),new Vector2(45.3f,65.2f),new Vector2(45.3f,88.6f),MireSpawn,GateAt},new[]{new Vector2(12,80.5f),PimAt},new[]{new Vector2(-4,80),StoreAt},
        };
        /// <summary>The floor signs, for whatever is scattered to keep off: their spots and half their widths. MIRE GATE (the last) stands on the camera's side of the arch, 5 m from it:
        /// at (42.6, 92), beside the spawn, it was 9.3 m from the way it names and the arch read as an unlabelled column (QaStoryWorldTests.EveryChapterTwoExitIsSigned, every
        /// way out within 7 m of a sign).</summary>
        static readonly (Vector2 at,float half)[] ReedSigns={(new(-4.5f,7.6f),2.8f),(new(-9,37.6f),2.8f),(new(26,36.4f),2.4f),(new(-8,77.6f),2.8f),(new(-42,37),2.2f),(new(40.5f,51.2f),2.4f),(new(45,95.6f),2.2f)};
        /// <summary>People with no names, standing: the coat (an Ilu body), where, facing, tint. Off every way between the named, off the signs, and no two side by side in one coat.</summary>
        static readonly (string body,float x,float z,float yaw,Color tint)[] ReedFolk=
        {
            // two pilots coiling line on the stage, and someone fishing off its west end
            ("DovBody",-4.2f,15.6f,40,new(.86f,.92f,1.08f)),("QuillBody",5.2f,16,215,new(1.04f,.92f,.86f)),("OnaBody",-13.2f,8.6f,235,new(.9f,1.04f,.92f)),
            // the market: a buyer at the produce stall and its keeper; a buyer at the reed-goods stall and its keeper
            ("MaruBody",-8.8f,47.2f,10,new(1.02f,.9f,1f)),("DovBody",-9.4f,53.3f,185,new(.92f,.98f,1.1f)),("PimBody",7.6f,49.2f,5,new(1.06f,1f,.88f)),("QuillBody",8.2f,55.4f,185,new(.9f,1.06f,1f)),
            // someone looking over the mats laid out to dry (the weaver turning them walks: Errands)
            ("QuillBody",-6.2f,49.8f,300,new(1.04f,.96f,1.06f)),
            // weavers at the racks east of the hall, backs to the camera (a third walks the rack: Errands)
            ("MaruBody",9.6f,90.6f,355,new(.94f,.9f,1.1f)),("DovBody",16.6f,90.2f,350,new(1.06f,.98f,.9f)),
            // Iri on the school raft, and the children facing her
            ("OnaBody",42.6f,60.2f,200,new(.9f,1f,1.08f)),
        };
        /// <summary>People on errands (WalkLoop): the coat, the loop on the plan, its pace and the wait at each stop, the tint, whether it goes there and back, and its name. Off every
        /// way between the named people, kept clear of the scatter (<see cref="ReedKeepsOff"/>), and checked as built: a walker stands still for good at anything solid in its way.
        /// Three walked at first and the town read as posed; now seven are about their work.</summary>
        static readonly (string body,Vector2[] loop,float speed,float pause,Color tint,bool back,string name)[] Errands=
        {
            ("OnaBody",new[]{new Vector2(7,85.6f),new Vector2(15.5f,86),new Vector2(16.5f,88.4f),new Vector2(8,88.2f)},1,2.6f,new(.96f,1.04f,.9f),false,"A weaver with a mat"),
            ("PimBody",new[]{new Vector2(34.2f,57.4f),new Vector2(35.8f,62.4f),new Vector2(38.6f,61.8f),new Vector2(36.4f,58.2f)},2.2f,.4f,new(1.06f,.96f,1f),false,"A child running the rim"),
            ("MaruBody",new[]{new Vector2(-10.8f,15.8f),new Vector2(-12.2f,11.8f)},.9f,3.2f,new(.9f,.96f,1.08f),true,"A pilot with a coil"),
            ("OnaBody",new[]{new Vector2(4.6f,38.2f),new Vector2(10.8f,38.4f),new Vector2(10.6f,43.1f),new Vector2(4.8f,42.9f)},.8f,2.8f,new(.94f,1.04f,.96f),false,"A weaver turning the mats"),
            ("DovBody",new[]{new Vector2(21.4f,37),new Vector2(29.2f,37.2f)},.9f,3,new(1.08f,.94f,.86f),true,"The smokehouse's hand with a creel"),
            ("QuillBody",new[]{new Vector2(-14.4f,77.3f),new Vector2(-6.6f,77.2f)},.85f,2.4f,new(1.04f,.96f,1.06f),true,"A weaver bringing reed to the hall"),
            ("PimBody",new[]{new Vector2(11.4f,89.4f),new Vector2(15,89.4f)},.6f,3.4f,new(1f,1.08f,.94f),true,"A weaver looking the mats over"),
        };
        /// <summary>The children facing Iri, each in a different coat: side by side, one coat three times read as one child copied.</summary>
        static readonly (string body,float x,float z,float yaw,Color tint)[] Children={("PimBody",40.2f,57.4f,20,new(1.08f,.96f,.9f)),("MaruBody",38.6f,58.4f,30,new(.92f,1.06f,1f)),("OnaBody",41.4f,56.2f,15,new(1f,.94f,1.1f))};
        static bool ReedKeepsOff(Vector2 p)
        {
            foreach(var person in ReedPeople)if(ZoneKit.InSightOf(p,person,2.6f,4.5f,1.6f))return true;
            if(ZoneKit.OnASign(p,ReedSigns))return true;
            foreach(var (_,x,z,_,_) in ReedFolk)if(Vector2.Distance(p,new Vector2(x,z))<2.2f)return true;
            foreach(var (_,x,z,_,_) in Children)if(Vector2.Distance(p,new Vector2(x,z))<1.8f)return true;
            foreach(var errand in Errands)for(int k=0;k<errand.loop.Length;k++){if(!errand.back||k+1<errand.loop.Length){if(ZoneKit.Segment(p,errand.loop[k],errand.loop[(k+1)%errand.loop.Length])<1.3f)return true;}}
            foreach(var spot in new[]{ReedArrival,ReedPad,LineAt,NoticeAt,PressAt,FrameAt,PumpSpawn,SchoolAt,GateAt,MireSpawn,StoreAt})if(Vector2.Distance(p,spot)<3)return true;
            return Vector2.Distance(p,HallAt)<7||Vector2.Distance(p,SmokehouseAt)<5;
        }
        static float FromTownWays(Vector2 p){float d=float.PositiveInfinity;foreach(var way in TownWays)d=Mathf.Min(d,ZoneKit.FromPath(p,way));return d;}
        /// <summary>Refuses a thing that stands on the floor but was put on a rim or in the water: a raft is a rounded box, and a number a metre out is a weaver up to the knees in reed.</summary>
        static void OnTheRaft(string what,params Vector2[] spots)
        {
            foreach(var at in spots)
            {
                float inside=-RaftLand(at);
                if(inside<RaftRimWide+.15f&&Opening(at)<.5f)throw new InvalidOperationException($"Driftreed: {what} at {at} is {inside:0.0} m from the floor's edge: on a rim or in the water");
            }
        }
        static bool OnAWalk(Vector2 p){foreach(var (_,a,b,_,_) in Walks)if(ZoneKit.Segment(p,a,b)<WalkHalf+.3f)return true;return false;}

        // ---------------------------------------------------------------- what the town is made of
        internal static Material Plank,Bark,Leaf,Cloth,Lantern,Rope,Loam,Weave,ReedMat,ReedBundle,ReedThatch,MatDye,Brass,Water;
        static void DriftreedMaterials()
        {
            // Timber, bark, leaf, peat and basketwork, painted near neutral and tinted in the vertices, so one plank covers every deck and one bark every mangrove. Painted here, not borrowed
            // from another region's builder, so nothing of Wendmire's changes when Xylos's is retuned.
            var plank=ZoneKit.Texture("Wendmire/wend-plank",512,(u,v)=>
            {
                float row=v*6;int board=Mathf.FloorToInt(row);float across=row-board,seam=Mathf.Min(across,1-across);
                float own=ZoneKit.Tile(board*3.7f,.5f,6,41),grain=ZoneKit.Tile(u*8+board*2.3f,v*90,90,6),end=Mathf.Abs(Mathf.Repeat(u+own*.63f,1)-.5f);
                float tone=.6f+.2f*(own-.5f)+.1f*(grain-.5f)-(seam<.06f?.3f:0)-(end<.006f?.25f:0);
                return new Color(tone*1.03f,tone,tone*.9f,1);
            });
            var bark=ZoneKit.Texture("Wendmire/wend-bark",512,(u,v)=>
            {
                float wander=ZoneKit.Cloud(u,v*.5f,6,27)*1.6f,ridge=Mathf.Abs(Mathf.Sin((u*9+wander)*Mathf.PI)),scale=ZoneKit.Cloud(u,v,14,5);
                float tone=.42f+.36f*Mathf.Pow(ridge,.7f)+.18f*(scale-.5f)-(ZoneKit.Tile(u*14,v*30,30,8)>.84f?.12f:0);
                return new Color(tone,tone*.98f,tone*.96f,1);
            });
            var leaf=ZoneKit.Texture("Wendmire/wend-leaf",256,(u,v)=>
            {
                float clump=ZoneKit.Cloud(u,v,7,23),fine=ZoneKit.Cloud(u,v,20,9),vein=Mathf.Abs(u-.5f)<.035f?.1f:0;
                float tone=.6f+.3f*(clump-.5f)+.16f*(fine-.5f)+vein;return new Color(tone,tone*1.02f,tone*.98f,1);
            });
            var peat=ZoneKit.Texture("Wendmire/wend-peat",512,(u,v)=>
            {
                float big=ZoneKit.Cloud(u,v,5,13),fine=ZoneKit.Cloud(u,v,24,3),fleck=ZoneKit.Tile(u*96,v*96,96,11);
                float tone=.66f+.26f*(big-.5f)+.2f*(fine-.5f)+(fleck>.9f?.12f:fleck<.07f?-.14f:0);return new Color(tone,tone*.96f,tone*.9f,1);
            });
            var basket=ZoneKit.Texture("Wendmire/wend-basket",512,(u,v)=>
            {
                float x=u*12,y=v*12;int cx=Mathf.FloorToInt(x),cy=Mathf.FloorToInt(y);float fx=x-cx-.5f,fy=y-cy-.5f;bool over=(cx+cy)%2==0;
                float strand=over?1-Mathf.Abs(fy)*1.7f:1-Mathf.Abs(fx)*1.7f,edge=over?Mathf.Abs(fx):Mathf.Abs(fy);
                float tone=.4f+.42f*Mathf.Clamp01(strand)-(edge>.44f?.16f:0)+.1f*(ZoneKit.Cloud(u,v,30,7)-.5f);return new Color(tone*1.04f,tone,tone*.88f,1);
            });
            Plank=ZoneKit.Toon("wend-plank",plank,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.52f,.5f,.66f));});
            Bark=ZoneKit.Toon("wend-bark",bark,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.6f,.54f,.48f));m.SetColor("_ShadowTint",new Color(.48f,.48f,.62f));});
            Leaf=ZoneKit.Toon("wend-leaf",leaf,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);ZoneKit.Swaying(m,.04f,1.2f,1.2f);m.SetColor("_ShadowTint",new Color(.44f,.5f,.58f));});
            Loam=ZoneKit.Toon("wend-peat",peat,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.5f,.5f,.62f));});
            Weave=ZoneKit.Toon("wend-basket",basket,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.84f,.72f,.5f));});
            Cloth=ZoneKit.Toon("wend-cloth",null,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);});
            Lantern=ZoneKit.Toon("wend-lantern",null,m=>{m.SetColor("_BaseColor",new Color(1,.8f,.46f));m.SetColor("_EmissionColor",new Color(2.2f,1.4f,.5f));});
            Rope=ZoneKit.Toon("wend-rope",null,m=>m.SetColor("_BaseColor",new Color(.86f,.76f,.54f)),"Universal Render Pipeline/Unlit");
            // The raft floor: mats laid edge to edge, as a raft town floors itself, each its own size, weave and age, with a bound edge. Plain weave in half-metre checks, twill in
            // diagonal steps, herringbone, and long stalks lashed with cords; fresh gold, weathered grey-green, a few with a dyed binding. One sheet to sixteen metres (the floor's
            // uv is a repeat to eight, scaled by half on the material), a thousand texels across, so as dense as the half-sheet it replaced. One weave in squares all over read as tile.
            const float Span=16;var lay=new System.Random(53);var strips=new List<(float y0,float y1,List<(float x0,float x1,int weave,float cell,float tone,int hue,float bind,bool turned)> mats)>();
            float[] rows={3.5f,4.5f,3.8f,4.2f};float rowAt=0;
            foreach(float tall in rows)
            {
                var mats=new List<(float,float,int,float,float,int,float,bool)>();float x=0;
                while(x<Span-.01f){float wide=Mathf.Min(Span-x,2.4f+(float)lay.NextDouble()*2.8f);if(Span-x-wide<1.8f)wide=Span-x;
                    mats.Add((x,x+wide,lay.Next(4),.42f+.16f*(float)lay.NextDouble(),.5f+.12f*(float)lay.NextDouble(),lay.Next(5),.12f+.08f*(float)lay.NextDouble(),lay.Next(2)==0));x+=wide;}
                strips.Add((rowAt,rowAt+tall,mats));rowAt+=tall;
            }
            var mat=ZoneKit.Texture("Wendmire/wend-reed-mat",1024,(u,v)=>
            {
                float mx=u*Span,my=v*Span;var strip=strips[strips.Count-1];foreach(var s in strips)if(my<s.y1){strip=s;break;}
                var m=strip.mats[strip.mats.Count-1];foreach(var each in strip.mats)if(mx<each.x1){m=each;break;}
                var (x0,x1,weave,cell,baseTone,hue,bind,turned)=m;
                float lx=mx-x0,ly=my-strip.y0,w=x1-x0,h=strip.y1-strip.y0;if(turned){(lx,ly)=(ly,lx);(w,h)=(h,w);}
                float edge=Mathf.Min(Mathf.Min(lx,w-lx),Mathf.Min(ly,h-ly)),tone;
                Color tint=hue switch{0=>new Color(1.06f,1,.8f),1=>new Color(1.04f,1,.84f),2=>new Color(.94f,.98f,.86f),3=>new Color(1.08f,.98f,.78f),_=>new Color(.98f,.96f,.84f)};
                if(edge<bind)
                {
                    // The binding: a roll of reed wrapped tight across, every six centimetres.
                    float along=Mathf.Abs(lx-w*.5f)>Mathf.Abs(ly-h*.5f)?ly:lx,wrap=Mathf.Abs(Mathf.Sin(along/.06f*Mathf.PI)),roundness=Mathf.Sin(edge/bind*Mathf.PI);
                    tone=.36f+.22f*roundness+.1f*wrap;
                    if(hue==3)tint=new Color(1.04f,.84f,.72f);else if(hue==4)tint=new Color(.86f,.9f,1f);
                }
                else
                {
                    int cx=Mathf.FloorToInt(lx/cell),cy=Mathf.FloorToInt(ly/cell);float fx=lx/cell-cx,fy=ly/cell-cy;bool across;float run,seam;
                    switch(weave)
                    {
                        case 1:{int tx=Mathf.FloorToInt(lx/(cell*.5f)),ty=Mathf.FloorToInt(ly/(cell*.5f));across=((tx+ty)%4+4)%4<2;fx=lx/(cell*.5f)-tx;fy=ly/(cell*.5f)-ty;run=across?fy:fx;seam=across?Mathf.Min(fx,1-fx):Mathf.Min(fy,1-fy);break;}
                        case 2:{int tx=Mathf.FloorToInt(lx/(cell*.5f)),ty=Mathf.FloorToInt(ly/(cell*.5f));int k=(tx/4)%2==0?tx+ty:tx-ty;across=(k%4+4)%4<2;fx=lx/(cell*.5f)-tx;fy=ly/(cell*.5f)-ty;run=across?fy:fx;seam=across?Mathf.Min(fx,1-fx):Mathf.Min(fy,1-fy);break;}
                        case 3:{across=true;run=ly/.07f;float cord=Mathf.Abs(Mathf.Repeat(lx,.9f)-.45f);seam=cord<.035f?0:1;fx=lx;break;}
                        default:{across=(cx+cy)%2==0;run=across?fy:fx;seam=Mathf.Min(Mathf.Min(fx,1-fx),Mathf.Min(fy,1-fy));break;}
                    }
                    float stalk=weave==3?Mathf.Abs(Mathf.Sin(run*Mathf.PI)):Mathf.Abs(Mathf.Sin(run*Mathf.PI*7)),fibre=ZoneKit.Tile(u*256,v*256,256,5+weave);
                    tone=baseTone+.26f*Mathf.Pow(stalk,.7f)+.04f*(ZoneKit.Tile(cx*1.7f+x0,cy*2.3f+strip.y0,64,17)-.5f)+.06f*(fibre-.5f);
                    tone-=weave==3?(seam<.5f?.16f:0):seam<.03f?.12f:seam<.07f?.04f:0;
                }
                if(edge<.025f)tone-=.24f;
                tone+=.05f*(ZoneKit.Cloud(u,v,6,9)-.5f);
                return new Color(tone*tint.r,tone*tint.g,tone*tint.b,1);
            });
            // Where feet go: the same weave trodden flat and pale, frayed at the edges of the squares. A ground-kit layer of Driftreed's own (GroundKit reads ground-<name> from its folder).
            ZoneKit.Texture("Ground/ground-wend-reedpath",512,(u,v)=>
            {
                float x=u*8,y=v*8;int cx=Mathf.FloorToInt(x),cy=Mathf.FloorToInt(y);float fx=x-cx,fy=y-cy;bool across=(cx+cy)%2==0;
                float run=across?fy:fx,stalk=Mathf.Abs(Mathf.Sin(run*Mathf.PI*7)),scuff=ZoneKit.Cloud(u,v,10,9),fray=ZoneKit.Tile(u*96,v*96,96,4);
                float seam=Mathf.Min(Mathf.Min(fx,1-fx),Mathf.Min(fy,1-fy));
                float tone=.72f+.1f*Mathf.Pow(stalk,.5f)+.14f*(scuff-.5f)-(seam<.025f?.14f:0)+(fray>.92f?.08f:0);
                return new Color(tone,tone,tone,1);
            });
            // A rim: thick bundles bound along the raft's edge, read along the wall (the landform lays a wall's texture level).
            var bundle=ZoneKit.Texture("Wendmire/wend-reed-bundle",512,(u,v)=>
            {
                float row=v*5;int r=Mathf.FloorToInt(row);float across=row-r,round=Mathf.Sqrt(Mathf.Max(0,1-(across*2-1)*(across*2-1)));
                float stalk=ZoneKit.Tile(u*3,v*90,90,r+4),bind=Mathf.Abs(Mathf.Repeat(u*3+r*.37f,1)-.5f);
                float tone=.32f+.42f*round+.14f*(stalk-.5f)-(bind<.035f?.22f:0);
                return new Color(tone,tone*1.02f,tone*.8f,1);
            });
            // Reed thatch: long straws laid in courses, each course's lower edge ragged.
            var thatch=ZoneKit.Texture("Wendmire/wend-reed-thatch",512,(u,v)=>
            {
                float row=v*6;int r=Mathf.FloorToInt(row);float down=1-(row-r),straw=ZoneKit.Tile(u*120,r*3.3f,120,5),rag=ZoneKit.Tile(u*30,r,30,9)*.25f;
                float tone=.4f+.36f*Mathf.Clamp01(down-rag)+.16f*(straw-.5f);
                return new Color(tone*1.02f,tone,tone*.82f,1);
            });
            // An air-mat on a drying rack: a tight weave in dyed bands, each mat its own colours from the vertices.
            var dye=ZoneKit.Texture("Wendmire/wend-mat-dye",512,(u,v)=>
            {
                float x=u*48,y=v*48;int cx=Mathf.FloorToInt(x),cy=Mathf.FloorToInt(y);bool over=(cx+cy)%2==0;float fx=x-cx-.5f,fy=y-cy-.5f;
                float strand=over?1-Mathf.Abs(fy)*1.6f:1-Mathf.Abs(fx)*1.6f;int band=Mathf.FloorToInt(v*7);
                float stripe=band%3==0?.62f:band%3==1?.9f:.76f;float edge=Mathf.Abs(v*7-band-.5f)>.44f?.5f:1;
                float tone=stripe*edge*(.7f+.3f*Mathf.Clamp01(strand))+.06f*(ZoneKit.Cloud(u,v,16,4)-.5f);
                return new Color(tone,tone,tone,1);
            });
            ReedMat=ZoneKit.Toon("wend-reed-mat",mat,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.5f,.5f,.66f));m.SetTextureScale("_BaseMap",new Vector2(.5f,.5f));});
            ReedBundle=ZoneKit.Toon("wend-reed-bundle",bundle,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.78f,.74f,.5f));m.SetColor("_ShadowTint",new Color(.48f,.48f,.62f));});
            ReedThatch=ZoneKit.Toon("wend-reed-thatch",thatch,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_ShadowTint",new Color(.5f,.5f,.64f));});
            MatDye=ZoneKit.Toon("wend-mat-dye",dye,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_ShadowTint",new Color(.52f,.5f,.66f));m.SetFloat("_LightFloor",.28f);});
            Brass=ZoneKit.Toon("wend-brass",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.92f,.74f,.4f));m.SetColor("_RimColor",new Color(1,.9f,.6f));m.SetFloat("_RampSteps",5);});
            // Mire water: dark and still, tea-brown under the green, glints kept small (chapter 3: a big ripple read as slabs of white).
            // Ripples small and soft: at a slower ripple the dark water broke into blotches a few metres across, like camouflage.
            Water=ZoneKit.Toon("wend-water",null,m=>ZoneKit.WaterLook(m,new Color(.3f,.42f,.34f,.86f),new Color(.11f,.17f,.15f,.97f),new Color(.74f,.82f,.7f,1),1.1f,3.4f,.07f,.3f));
        }

        static void Driftreed()
        {
            var kit=ZoneKit.Ground(Town,ReedHeight,3.2f);
            var scene=WorldBuilder.Begin(kit.zone);
            var land=kit.Terrain("Reed rafts",-66,82,-14,120,.5f,ReedMat,ReedBundle,ReedTone,new Color(.62f,.6f,.5f),new Color(1,.98f,.92f));
            var water=kit.Water("Mire water",-66,82,-14,120,1,ReedWater,Water,5);
            var patch=water.AddComponent<BiomePatch>();patch.resourceKey="Driftreed-mire";patch.palette="River";patch.water=true;
            WorldBuilder.Spawn("Arrival",kit.On(ReedArrival.x,ReedArrival.y));WorldBuilder.Spawn("Mire",kit.On(MireSpawn.x,MireSpawn.y));WorldBuilder.Spawn("Pump",kit.On(PumpSpawn.x,PumpSpawn.y));

            OnTheRaft("someone with a name",ReedPeople);OnTheRaft("a floor sign",ReedSigns.Select(s=>s.at).ToArray());OnTheRaft("a townsperson",ReedFolk.Select(f=>new Vector2(f.x,f.z)).ToArray());
            OnTheRaft("a child",Children.Select(c=>new Vector2(c.x,c.z)).ToArray());
            OnTheRaft("a place to stand",ReedArrival,ReedPad,LineAt,NoticeAt,PressAt,FrameAt,PumpSpawn,SchoolAt,GateAt,MireSpawn,StoreAt);
            foreach(var way in TownWays)OnTheRaft("a way's corner",way.Where(p=>!OnAWalk(p)).ToArray());

            // ---- the pilots' stage: the pad the ships tie up at, and the way back up
            var padAt=kit.On(ReedPad.x,ReedPad.y);var stage=new ZoneKit.Shape();Color board=new(.86f,.72f,.5f),pale=new(1,.96f,.82f),dark=new(.58f,.46f,.32f);
            stage.Slot(0).Disc(padAt+Vector3.up*.1f,Vector3.up,2.8f,24,board,0,new Color(.7f,.58f,.4f),.5f).Tube(new[]{padAt+Vector3.down*.1f,padAt+Vector3.up*.1f},new[]{2.9f,2.8f},24,dark,new Color(.6f,.5f,.36f),8,1,false);
            stage.Slot(1).Disc(padAt+Vector3.up*.115f,Vector3.up,2.2f,28,pale,1.9f).Disc(padAt+Vector3.up*.115f,Vector3.up,.5f,14,pale);
            for(int k=0;k<4;k++){float a=k*Mathf.PI*.5f+.785f;var q=padAt+new Vector3(Mathf.Cos(a)*3,0,Mathf.Sin(a)*3);stage.Slot(0).Post(q,.9f,.1f,.08f,6,dark,board).Slot(2).Lump(q+Vector3.up*1f,new Vector3(.14f,.18f,.14f),k,.05f,Color.white,Color.white,7,4);}
            var dock=kit.Built("Pilots' pad",stage.Slot(2),Plank,Cloth,Lantern);ZoneKit.Block(dock,padAt+Vector3.up*.02f,new Vector3(3.9f,.16f,3.9f),0,"Pad deck");ZoneKit.Block(dock,padAt+Vector3.up*.02f,new Vector3(3.9f,.16f,3.9f),45,"Pad deck");
            var point=new GameObject("Landing pad",typeof(DockingPad));point.transform.position=padAt;dock.transform.SetParent(point.transform,true);
            var pad=point.GetComponent<DockingPad>();pad.prompt="Launch — The Spore Veil";pad.range=4;pad.scene=Veil;pad.spawn=Town;
            kit.Sign("PILOTS' STAGE",ReedSigns[0].at.x,ReedSigns[0].at.y);

            RopeWalks(kit,out var swingDecks);
            Market(kit);
            Smokehouse(kit);
            PumpRaft(kit);
            Hall(kit);
            School(kit);
            Mangroves(kit);
            WaterLife(kit);
            TideLife(kit);
            Lamps(kit);

            // ---- the mire gate, on the gate raft: reached only once the tide has brought the gate walk across
            // The way out stands where the arch stands: a built mesh sits at the world's origin, and a prompt measures from its object's position (it was answering at the origin).
            var arch=ReedArch(kit,"Mire gate arch",new Vector3(GateAt.x,0,GateAt.y),5.8f,15);
            var gateHolder=new GameObject("Mire gate");gateHolder.transform.position=kit.On(GateAt.x,GateAt.y);arch.transform.SetParent(gateHolder.transform,true);
            kit.Way(gateHolder,"Mire gate — The Drowned Waystation",Station,"Arrival","wend.tideTurned","The mire gate is out past open water until the tide turns: ask Maru Wend at the pilots' stage",4.4f,null);
            kit.Sign("MIRE GATE",ReedSigns[6].at.x,ReedSigns[6].at.y);

            // ---- people
            var maru=kit.Person("Maru",new Vector3(MaruAtStage.x,0,MaruAtStage.y),105,"wend.decided");maru.postNode="MaruPost";
            // At the stage until she takes the party into the mire; out there (the waystation's own Maru) until the bloom is dead; back at the stage from then on. Decided as the town loads.
            Where("Maru is at the pilots' stage",maru.gameObject,null,"wend.tideTurned","wend.rotbloomDead",null,false);
            var ona=kit.Person("Ona",new Vector3(OnaAt.x,0,OnaAt.y),200,"",false,"wend.decided","OnaAfter","OnaTerms");ona.repeatNode="OnaRepeat";
            var dov=kit.Person("Dov",new Vector3(DovAt.x,0,DovAt.y),245,"wend.decided");
            var quill=kit.Person("Quill",new Vector3(QuillAt.x,0,QuillAt.y),95,"wend.decided");
            // Pim at the bench in the hall once the bloom is dead (PimFound at the station is the meeting; here it is only ever PimHome).
            var pim=kit.Person("Pim",new Vector3(PimAt.x,0,PimAt.y),210,"",false,null,null,"PimHome");pim.repeatNode="PimHome";
            Where("Pim is home at the bench",pim.gameObject,"wend.rotbloomDead",null,null,null,false);
            kit.Townsfolk(ReedFolk.Select(f=>(Ilu(f.body),f.x,f.z,f.yaw,f.tint)));
            foreach(var (body,x,z,yaw,tint) in Children){var child=kit.Townsperson(Ilu(body),new Vector3(x,0,z),yaw,tint,"Neve");child.transform.localScale=Vector3.one*.74f;child.name="A child on the school raft";}
            // Errands: each loop walked by someone carrying what the errand is about (a mat, a creel of smoked eel, a bundle of reed).
            foreach(var (body,loop,speed,pause,tint,back,name) in Errands)
            {
                var walker=kit.Walker(Ilu(body),loop,speed,pause,tint,"Neve");walker.thereAndBack=back;walker.name=name;
                if(name.StartsWith("A child"))walker.transform.localScale=Vector3.one*.72f;
                ZoneKit.Shape load=null;Material[] stuff=null;
                if(name.Contains("with a mat")){load=new ZoneKit.Shape().Slot(0).Box(new Vector3(0,1.1f,.45f),new Vector3(1.1f,.9f,.06f),Quaternion.Euler(-12,0,0),new Color(.5f,.66f,.86f));stuff=new[]{MatDye};}
                else if(name.Contains("creel")){load=new ZoneKit.Shape().Slot(0).Tube(new[]{new Vector3(.34f,.78f,.1f),new Vector3(.34f,1.18f,.1f)},new[]{.16f,.22f},9,new Color(.8f,.68f,.46f),new Color(.9f,.8f,.56f),2,.8f);stuff=new[]{Weave};}
                else if(name.Contains("reed")){load=new ZoneKit.Shape().Slot(0).Tube(new[]{new Vector3(-.7f,1.62f,.05f),new Vector3(.9f,1.5f,.05f)},new[]{.16f,.12f},8,new Color(.84f,.78f,.5f),new Color(.92f,.86f,.58f),3,.6f);stuff=new[]{ReedBundle};}
                if(load!=null){var carried=kit.Built(name.Contains("with a mat")?"Mat being carried":name+" carries",load,stuff);carried.transform.SetParent(walker.transform,false);carried.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;}
            }

            // What the people standing about are doing: the angler's pole and line out over the water, the pilots' coils at their feet.
            var doing=new ZoneKit.Shape();var tip=new Vector3(-17.2f,2.4f,5.2f);
            doing.Slot(0).Tube(new[]{kit.On(-13.5f,8.3f,1.1f),tip},new[]{.035f,.015f},5,new Color(.6f,.48f,.32f),new Color(.8f,.68f,.46f),1,.6f).Rope(tip,new Vector3(-17.6f,ReedWater+.05f,4.7f),.05f,.008f,new Color(.9f,.86f,.7f),4,4);
            doing.Slot(1).Lump(new Vector3(-17.6f,ReedWater+.04f,4.7f),new Vector3(.08f,.06f,.08f),1,.1f,new Color(.96f,.5f,.3f),new Color(.7f,.3f,.2f),6,4);
            foreach(var (x,z) in new[]{(-3.5f,16.1f),(4.5f,16.5f)})for(int turn=0;turn<3;turn++){var coil=new Vector3[13];var thick=new float[13];for(int k=0;k<13;k++){float a=k*Mathf.PI*2/12;coil[k]=kit.On(x+Mathf.Cos(a)*(.42f-turn*.09f),z+Mathf.Sin(a)*(.42f-turn*.09f),.05f+turn*.08f);thick[k]=.045f;}doing.Slot(0).Tube(coil,thick,5,new Color(.95f,.86f,.62f),new Color(.95f,.86f,.62f),1,1,false);}
            kit.Built("Pole and coils",doing,Plank,Cloth).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;

            // ---- the arrival: the stern-line raft surging on the turning tide, Maru's call, and the line put in Sela's hands
            SternLine(kit,maru,ona);

            // ---- things to read, find and hear
            kit.Discovery("The Compact notice",new Vector3(NoticeAt.x,0,NoticeAt.y),"Read the Compact notice","wend.noticeRead","","SelaWendNotice","Sela",null,0,2.4f);
            kit.Discovery("The school raft",new Vector3(SchoolAt.x,0,SchoolAt.y),"Watch the school raft","wend.schoolSeen","","SelaSchoolRaft","Sela",null,0,2.6f);
            // Abri's call, on the walk back to the stage once Driftreed has decided: nobody is standing there, and it ends the chapter.
            // It stands on the walk's deck: placed on the floor it went to the mire's bed under the walk, two metres down, and no prompt there could be reached.
            var abriCall=kit.Crossing("Keeper Abri calls about the name",new Vector3(AbriCallAt.x,0,AbriCallAt.y),new Vector3(6,5,3),"chapter4.complete","AbriManyHandsCall","Keeper","wend.decided");
            abriCall.transform.position=new Vector3(AbriCallAt.x,.05f,AbriCallAt.y);

            // ---- what grows on the rafts
            var rng=new System.Random(71);
            // Along the rims, off the ways: what a raft town keeps outside its doors, woven and fired here. The station's planters and steel barrels stood here at first and read as
            // somebody else's town. Every draw is taken whether or not its piece is kept (D213).
            var goods=new ZoneKit.Shape();var goodsBlocks=new GameObject("Raft goods");int kept=0;
            for(int tries=0;tries<220&&kept<46;tries++)
            {
                var p=new Vector2((float)rng.NextDouble()*120-56,(float)rng.NextDouble()*104+2);int kind=rng.Next(4);float size=.55f+(float)rng.NextDouble()*.4f,yaw=(float)rng.NextDouble()*360;
                float d=RaftLand(p);if(!(d<-1.8f&&d>-4.4f&&FromTownWays(p)>3)||ReedKeepsOff(p)||OnAWalk(p))continue;
                kept++;var foot=kit.On(p.x,p.y);var turn=Quaternion.Euler(0,yaw,0);
                if(kind==0){goods.Slot(0).Tube(new[]{foot,foot+Vector3.up*size*.75f},new[]{size*.45f,size*.52f},11,new Color(.78f,.66f,.44f),new Color(.95f,.84f,.58f),2,1,false);
                    goods.Slot(1).Lump(foot+Vector3.up*size*.72f,new Vector3(size*.46f,size*.2f,size*.46f),kept,.25f,new Color(.55f,.8f,.5f),new Color(.36f,.5f,.3f),8,4);}
                else if(kind==1){goods.Slot(2).Lump(foot+Vector3.up*size*.48f,new Vector3(size*.4f,size*.5f,size*.4f),kept,.04f,new Color(.74f,.5f,.36f),new Color(.5f,.32f,.24f),12,7);
                    goods.Slot(2).Tube(new[]{foot+Vector3.up*size*.9f,foot+Vector3.up*size*1.05f},new[]{size*.16f,size*.2f},10,new Color(.66f,.44f,.32f),new Color(.74f,.5f,.36f),1,1,false);}
                else if(kind==2){for(int k=0;k<9;k++){var lean=turn*new Vector3((k%3-1)*.12f,0,(k/3-1)*.12f);goods.Slot(3).Tube(new[]{foot+lean,foot+lean*1.6f+Vector3.up*(1.3f+.1f*(k%2))},new[]{.035f,.025f},4,new Color(.66f,.62f,.4f),new Color(.86f,.8f,.5f),1,.8f);}
                    goods.Slot(0).Tube(new[]{foot+Vector3.up*.4f,foot+Vector3.up*.5f},new[]{.24f,.24f},8,new Color(.5f,.36f,.26f),new Color(.5f,.36f,.26f),1,1,false);}
                else goods.Slot(0).Box(foot+Vector3.up*size*.3f,new Vector3(size*1.2f,size*.6f,size*.8f),turn,new Color(.72f,.6f,.4f),new Color(.86f,.74f,.5f),.8f);
                ZoneKit.Block(goodsBlocks,foot+Vector3.up*.35f,new Vector3(size*.9f,.7f,size*.9f),yaw,"Raft goods");
            }
            kit.Built("Raft goods",goods,Weave,Leaf,Loam,ReedThatch).transform.SetParent(goodsBlocks.transform,true);

            // Every errand's loop clear of anything solid at a body's height: a walker stands still for good at whatever is in its way.
            Physics.SyncTransforms();var blocked=new List<string>();
            foreach(var (_,loop,_,_,_,back,name) in Errands)
                for(int k=0;k<loop.Length-(back?1:0);k++)
                {
                    var a=kit.On(loop[k].x,loop[k].y)+Vector3.up*.95f;var next=loop[(k+1)%loop.Length];var b=kit.On(next.x,next.y)+Vector3.up*.95f;var run=b-a;run.y=0;
                    foreach(var hit in Physics.SphereCastAll(a,.4f,run.normalized,run.magnitude,~0,QueryTriggerInteraction.Ignore))
                        if(hit.collider.GetComponentInParent<WalkLoop>()==null)blocked.Add($"{name}: {hit.collider.name} at ({hit.point.x:0.0}, {hit.point.z:0.0})");
                }
            if(blocked.Count>0)throw new InvalidOperationException("Driftreed: an errand runs into something: "+string.Join("; ",blocked.Distinct()));

            // Bake with every swinging walk across: the tide's seals cut what isn't there (TideSwing). The bake reads only static boxes, so the walks' own decks (which move) are stood in
            // for by static ones while it runs, and taken away again.
            var bakeDecks=new GameObject("Walks across, for the bake");
            // Each runs on two metres into the raft at either end, past the rim, so the bake joins it to open floor whatever the rim does where the walk lands.
            foreach(var (from,to) in swingDecks){var d=(to-from).normalized*2.2f;ZoneKit.BlockAlong(bakeDecks,new Vector3(from.x-d.x,-.24f,from.y-d.y),new Vector3(to.x+d.x,-.24f,to.y+d.y),WalkHalf*2,.5f,"Walk deck");}
            var bounds=new Bounds(new Vector3(8,3,52),new Vector3(160,24,130));
            // The seals' carving obstacles cut the editor's paths too: a shut seal at the gate walk's foot made the gate raft unreachable in the bake's own check. Every seal is opened
            // for the bake (the must-reach list is the town at both tides) and put back as the tide leaves it before the turn.
            var seals=UnityEngine.Object.FindObjectsByType<TideSwing>(FindObjectsSortMode.None).SelectMany(t=>t.ends.Select(e=>(seal:e,open:e.Open))).ToArray();
            foreach(var (seal,_) in seals)seal.SetOpen(true);
            try{kit.BakeNavigation(bounds,ReedArrival,new[]{ReedPad+new Vector2(0,-3.4f),MaruAtStage+new Vector2(-1.6f,0),LineAt+new Vector2(-1.4f,.3f),NoticeAt+new Vector2(0,-1.2f),
                DovAt+new Vector2(-1.6f,0),QuillAt+new Vector2(1.6f,0),PressAt+new Vector2(1.4f,-1.4f),FrameAt+new Vector2(1.4f,1),PumpSpawn,OnaAt+new Vector2(-.6f,-1.6f),StoreAt+new Vector2(0,-1.6f),
                PimAt+new Vector2(-1.4f,-1),SchoolAt,GateAt+new Vector2(-1,-3),MireSpawn,new Vector2(0,26)});}
            catch(InvalidOperationException)
            {
                // Say where along the way to the gate the floor stops, then refuse as before.
                Physics.SyncTransforms();
                foreach(var hit in Physics.OverlapBox(new Vector3(45.3f,.3f,65.2f),new Vector3(2.5f,1.5f,2.5f)))
                    Debug.LogError("DRIFTREED_AT_GATE_WALK_FOOT "+hit.name+" static="+hit.gameObject.isStatic+" trigger="+hit.isTrigger+" bounds="+hit.bounds+" parent="+(hit.transform.parent!=null?hit.transform.parent.name:"-"));
                var way=new[]{SchoolAt,new Vector2(42,62),new Vector2(45.3f,64),new Vector2(45.3f,64.5f),new Vector2(45.3f,65),new Vector2(45.3f,65.5f),new Vector2(45.3f,66.5f),new Vector2(45.3f,77),new Vector2(45.3f,88),new Vector2(45.3f,90.5f),MireSpawn,GateAt+new Vector2(-1,-3)};
                for(int k=1;k<=way.Length;k++)
                {
                    try{kit.BakeNavigation(bounds,ReedArrival,way.Take(k).ToArray());}
                    catch(InvalidOperationException stop){Debug.LogError("DRIFTREED_ROUTE_STOPS at "+way[k-1]+": "+stop.Message);break;}
                }
                throw;
            }
            UnityEngine.Object.DestroyImmediate(bakeDecks);
            foreach(var (seal,open) in seals)seal.SetOpen(open);
            ReedGround(kit,land);
            var motes=kit.Glow("Mire motes",false,new Color(1.1f,1.5f,1.1f,1));
            kit.Drift("Mire motes",new Vector3(8,2.6f,52),new Vector3(130,5,120),motes,new Color(.8f,1,.7f,.45f),420,.04f,.12f,.18f,14);
            WorldBuilder.Save(scene,kit.zone);
            Debug.Log("WENDMIRE_COORDS Driftreed arrival="+ReedArrival+" pad="+ReedPad+" maru="+MaruAtStage+" line="+LineAt+" crossing=(6,12) notice="+NoticeAt+" dov="+DovAt+" quill="+QuillAt+" press="+PressAt
                +" frame="+FrameAt+" pump-spawn="+PumpSpawn+" ona="+OnaAt+" store="+StoreAt+" pim="+PimAt+" school="+SchoolAt+" gate="+GateAt+" mire-spawn="+MireSpawn+" abri-call="+AbriCallAt
                +" walks="+string.Join(" ",Walks.Select(w=>w.name.Replace(' ','-')+":"+w.from+"->"+w.to)));
            Debug.Log("DRIFTREED_OK");
        }
        /// <summary>One of the Ilu coats (Maru's, Ona's, Dov's, Quill's, Pim's), or Maru's own while the others aren't taken in.</summary>
        static string Ilu(string coat)=>ZoneKit.Prefab("Characters",coat)!=null?coat:ZoneKit.Prefab("Characters","MaruBody")!=null?"MaruBody":"Neve";
        static void Where(string name,GameObject who,string from,string awayFrom,string backAt,string goneAt,bool live)
        {
            var where=new GameObject(name,typeof(Whereabouts)).GetComponent<Whereabouts>();where.from=from??"";where.awayFrom=awayFrom??"";where.backAt=backAt??"";where.goneAt=goneAt??"";where.live=live;where.targets=new[]{who};
        }

        // ---------------------------------------------------------------- the stern line
        /// <summary>A raft surging off the stage's east end on the turning tide, a pilot's line from its cleat to the stage, and the hold that takes three surges to settle it: the
        /// MooringHaul from chapter 3 with Driftreed's words (a hold, not a haul: nothing is made fast). Maru calls each surge; she and Ona answer with a holding line until it lies easy.</summary>
        static void SternLine(ZoneKit kit,Npc maru,Npc ona)
        {
            GameObject raft;
            if(ZoneKit.Landed("ReedRaft")){raft=kit.Prop("ReedRaft",Vector3.zero,1.2f,0,false,false);raft.transform.localScale*=8/Mathf.Max(.1f,ZoneKit.Drawn(raft).size.z);}
            else
            {
                var shape=new ZoneKit.Shape();var rng=new System.Random(5);
                for(int k=0;k<9;k++)shape.Slot(0).Tube(new[]{new Vector3(-2.2f+k*.55f,-.1f,-4),new Vector3(-2.2f+k*.55f,-.1f,4)},new[]{.28f,.28f},7,new Color(.82f,.74f,.5f),new Color(.9f,.82f,.56f),1,.4f);
                foreach(float z in new[]{-3.2f,0f,3.2f})shape.Slot(0).Beam(new Vector3(-2.5f,.18f,z),new Vector3(2.5f,.18f,z),.16f,.12f,new Color(.6f,.48f,.32f));
                raft=kit.Built("Stern-line raft",shape,ReedBundle);
            }
            raft.name="The raft on the stern line";foreach(var t in raft.GetComponentsInChildren<Transform>())t.gameObject.isStatic=false;
            // A load lashed on it: rolled mats and a crate, so it reads as somebody's cargo and not a loose floor.
            var load=new ZoneKit.Shape().Slot(0).Tube(new[]{new Vector3(-1,.55f,-1.6f),new Vector3(1,.55f,-1.6f)},new[]{.42f,.42f},10,new Color(.5f,.66f,.86f),new Color(.86f,.6f,.4f),2,.6f)
                .Tube(new[]{new Vector3(-1,.55f,-.7f),new Vector3(1,.55f,-.7f)},new[]{.4f,.4f},10,new Color(.86f,.56f,.36f),new Color(.6f,.8f,.5f),2,.6f).Slot(1).Box(new Vector3(.4f,.5f,1.6f),new Vector3(1.2f,.8f,1),Quaternion.Euler(0,12,0),new Color(.7f,.56f,.38f),new Color(.86f,.7f,.48f));
            var cargo=kit.Built("Stern-line raft load",load,MatDye,Plank);cargo.transform.SetParent(raft.transform,false);cargo.transform.localPosition=Vector3.zero;
            var moored=new Vector3(LineMoored.x,ReedWater+.35f,LineMoored.y);var adrift=new Vector3(LineAdrift.x,ReedWater+.35f,LineAdrift.y);raft.transform.position=moored;
            var cleat=new GameObject("Stern cleat").transform;cleat.SetParent(raft.transform,false);cleat.localPosition=new Vector3(-2.2f,.6f,-3.2f);
            var postFoot=kit.On(LineAt.x+1.1f,LineAt.y-.9f);
            var posts=new ZoneKit.Shape().Post(postFoot,1.3f,.24f,.2f,8,new Color(.74f,.64f,.5f),Color.white);
            for(int turn=0;turn<3;turn++){var coil=new Vector3[13];var thick=new float[13];for(int k=0;k<13;k++){float a=k*Mathf.PI*2/12;coil[k]=kit.On(LineAt.x-.4f+Mathf.Cos(a)*(.46f-turn*.1f),LineAt.y+.9f+Mathf.Sin(a)*(.46f-turn*.1f),.06f+turn*.09f);thick[k]=.05f;}posts.Tube(coil,thick,5,new Color(.95f,.86f,.62f),new Color(.95f,.86f,.62f),1,1,false);}
            var post=kit.Built("Stern-line post",posts,Plank);ZoneKit.Block(post,postFoot+Vector3.up*.6f,new Vector3(.5f,1.2f,.5f),0,"Line post");
            var hand=new GameObject("Line in hand").transform;hand.position=postFoot+Vector3.up*1.1f;
            var line=new GameObject("Stern line",typeof(LineRenderer)).GetComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=9;line.startWidth=line.endWidth=.09f;line.sharedMaterial=Rope;line.shadowCastingMode=ShadowCastingMode.Off;
            for(int k=0;k<9;k++){float t=k/8f;line.SetPosition(k,Vector3.Lerp(cleat.position,hand.position,t)+Vector3.down*.5f*4*t*(1-t));}
            var hold=new GameObject("The stern line",typeof(MooringHaul)).GetComponent<MooringHaul>();hold.transform.position=kit.On(LineAt.x,LineAt.y);
            hold.arrivalFlag="wend.arrivalSeen";hold.hauledFlag="wend.lineHeld";hold.landedFlag="wend.arrived";hold.caller="Maru";hold.bark="line";hold.pulls=3;hold.swing=30;
            hold.prompt="Hold the stern line";hold.range=2.8f;hold.island=raft.transform;hold.adrift=adrift;hold.moored=moored;hold.lines=new[]{line};hold.cleats=new[]{cleat};hold.hands=new[]{hand};
            hold.held=new[]{maru,ona};hold.holdNodes=new[]{"MaruLineHold","OnaLineHold"};
            // Across the stage, between the pad and the line: the talk can't be walked round, and it is over before the line is in reach.
            kit.Crossing("Maru puts a line in Sela's hands",new Vector3(6,0,12),new Vector3(3,5,13),"wend.arrivalSeen","MaruArrival","Maru");
            // Maru's lamp on the stage mast: long, long, short ("still afloat"), at every tide for two nights, until Ona answers it.
            Mast(kit,"Maru's lamp",MaruMast,LampRhythm.Count.LongLongShort,"wend.decided");
        }
        /// <summary>A pilot's signal mast: a pole with a lamp in a cage at its head, keeping a count (<see cref="LampRhythm"/>).</summary>
        static void Mast(ZoneKit kit,string name,Vector2 at,LampRhythm.Count count,string darkFlag)
        {
            var foot=kit.On(at.x,at.y);GameObject body;Renderer[] lamps;
            if(ZoneKit.Landed("PulseBeacon")){body=kit.Prop("PulseBeacon",new Vector3(at.x,0,at.y),5.4f,200);body.name=name;lamps=body.GetComponentsInChildren<Renderer>();}
            else{var shape=new ZoneKit.Shape().Slot(0).Post(foot,5,.12f,.09f,7,new Color(.6f,.5f,.36f),new Color(.86f,.74f,.5f)).Slot(1).Lump(foot+Vector3.up*5.2f,new Vector3(.32f,.42f,.32f),3,.05f,Color.white,Color.white,8,5);
                body=kit.Built(name,shape,Plank,Lantern);ZoneKit.Block(body,foot+Vector3.up*1,new Vector3(.4f,2,.4f),0,"Mast");lamps=new[]{body.GetComponent<Renderer>()};}
            var light=kit.Lamp(new Vector3(at.x,4.8f,at.y),9,1.4f,new Color(1,.8f,.5f));light.transform.SetParent(body.transform,true);
            var rhythm=body.AddComponent<LampRhythm>();rhythm.count=count;rhythm.retuned=count;rhythm.litFlag="";rhythm.retunedFlag="";rhythm.darkFlag=darkFlag??"";
            rhythm.lights=new[]{light};rhythm.lamps=lamps;rhythm.colour=new Color(1,.8f,.48f);rhythm.intensity=1.8f;
        }

        // ---------------------------------------------------------------- rope walks and the tide
        /// <summary>The rope walks: boards on slung lines, a rail each side on a box a body stops at. The three fixed ones are part of the town; the four the tide moves turn on a pivot at one
        /// end (<see cref="TideSwing"/>), with a seal at each end that stands while the walk is away. Hands back where the moving ones' decks lie when across, for the bake.</summary>
        static void RopeWalks(ZoneKit kit,out List<(Vector2 from,Vector2 to)> swingDecks)
        {
            swingDecks=new List<(Vector2,Vector2)>();var rng=new System.Random(19);Color wood=new(.86f,.72f,.5f),dark=new(.58f,.46f,.32f),hemp=new(.95f,.86f,.62f);
            var tide=new GameObject("The tide");
            foreach(var (name,from,to,turn,away) in Walks)
            {
                // Built about its first end, so the swing's pivot is the object's own position.
                var pivot=new Vector3(from.x,0,from.y);var run=new Vector3(to.x-from.x,0,to.y-from.y);float length=run.magnitude;var along=run/length;var across=Vector3.Cross(Vector3.up,along);
                var shape=new ZoneKit.Shape();
                for(int log=0;log<3;log++)shape.Slot(0).Tube(new[]{along*-.3f+across*(log-1)*.8f+Vector3.down*.32f,along*(length+.3f)+across*(log-1)*.8f+Vector3.down*.32f},new[]{.26f,.26f},7,Color.white,new Color(.86f,.84f,.8f),1,.3f);
                shape.Slot(1).Deck(Vector3.zero+along*-.2f+Vector3.up*.02f,along*(length+.2f)+Vector3.up*.02f,WalkHalf*2,.09f,wood,rng);
                int posts=Mathf.Max(2,Mathf.RoundToInt(length/4.5f));
                for(int k=0;k<=posts;k++)foreach(int side in new[]{-1,1})shape.Slot(1).Post(along*(length*k/posts)+across*side*(WalkHalf+.05f)+Vector3.down*.2f,1.3f,.08f,.07f,6,dark,wood);
                foreach(int side in new[]{-1,1})foreach(float high in new[]{1f,.6f})for(int k=0;k<posts;k++)
                    shape.Slot(1).Rope(along*(length*k/posts)+across*side*(WalkHalf+.05f)+Vector3.up*high,along*(length*(k+1)/posts)+across*side*(WalkHalf+.05f)+Vector3.up*high,.14f,.035f,hemp);
                var walk=kit.Built(name,shape,ReedBundle,Plank);
                var holder=new GameObject(name+" pivot");holder.transform.position=pivot;walk.transform.SetParent(holder.transform,false);walk.transform.localPosition=Vector3.zero;
                ZoneKit.Block(walk,pivot+along*(length*.5f)+Vector3.down*.24f,new Vector3(WalkHalf*2,.5f,length+1),Mathf.Atan2(along.x,along.z)*Mathf.Rad2Deg,"Walk deck");
                foreach(int side in new[]{-1,1})ZoneKit.BlockAlong(walk,pivot+across*side*(WalkHalf+.08f)+Vector3.up*.55f,pivot+along*length+across*side*(WalkHalf+.08f)+Vector3.up*.55f,.16f,1.1f,"Walk rail");
                if(turn==0)continue;
                // A walk the tide moves: nothing of it is static (it turns), its ends are seals, and the swing is what the flag plays.
                foreach(var t in holder.GetComponentsInChildren<Transform>())t.gameObject.isStatic=false;
                swingDecks.Add((from,to));
                var ends=new List<Membrane>();
                foreach(var (end,inward) in new[]{(from,-along),(to,along)})ends.Add(TideSeal(kit,name+" end",new Vector2(end.x,end.y),new Vector3(inward.x,0,inward.z),across,turn<0));
                var swing=holder.AddComponent<TideSwing>();swing.flag="wend.tideTurned";swing.walk=walk.transform;swing.arrives=turn>0;swing.fromYaw=turn<0?0:-away;swing.toYaw=turn<0?-away:0;swing.ends=ends.ToArray();swing.seconds=7;
                swing.marker="";walk.transform.localRotation=Quaternion.Euler(0,swing.fromYaw,0);
                swing.transform.SetParent(tide.transform,true);
            }
        }
        /// <summary>A seal across a walk's landing: two short posts on the raft's rim either side of the gap and a rope slung between, a box a body stops at and a carving obstacle. It stands
        /// while its walk is away; <paramref name="open"/> is its state before the tide.</summary>
        static Membrane TideSeal(ZoneKit kit,string name,Vector2 at,Vector3 inward,Vector3 across,bool open)
        {
            var foot=kit.On(at.x,at.y)+inward*.7f;var shape=new ZoneKit.Shape();Color dark=new(.58f,.46f,.32f),hemp=new(.95f,.86f,.62f);
            foreach(int side in new[]{-1,1})shape.Slot(0).Post(foot+across*side*(WalkHalf+.15f)+Vector3.down*.1f,1.25f,.09f,.08f,6,dark,new Color(.8f,.68f,.48f));
            shape.Slot(0).Rope(foot-across*(WalkHalf+.15f)+Vector3.up*1f,foot+across*(WalkHalf+.15f)+Vector3.up*1f,.18f,.045f,hemp).Rope(foot-across*(WalkHalf+.15f)+Vector3.up*.55f,foot+across*(WalkHalf+.15f)+Vector3.up*.55f,.12f,.04f,hemp);
            // A float hung on the rope, so it reads as a barrier and not a stray line.
            shape.Slot(1).Lump(foot+Vector3.up*.78f,new Vector3(.2f,.24f,.2f),3,.1f,new Color(.96f,.62f,.3f),new Color(.7f,.4f,.2f),7,4);
            var holder=new GameObject(name,typeof(Membrane));holder.transform.position=foot;
            var rope=kit.Built(name+" rope "+Mathf.RoundToInt(at.x)+" "+Mathf.RoundToInt(at.y),shape,Plank,Cloth);rope.transform.SetParent(holder.transform,true);
            var stop=new GameObject("Seal",typeof(BoxCollider));stop.transform.SetParent(holder.transform,false);stop.transform.SetPositionAndRotation(foot+Vector3.up*.9f,Quaternion.LookRotation(inward));
            stop.GetComponent<BoxCollider>().size=new Vector3(WalkHalf*2+.6f,1.8f,.5f);
            var carve=stop.AddComponent<NavMeshObstacle>();carve.carving=true;carve.shape=NavMeshObstacleShape.Box;carve.size=new Vector3(WalkHalf*2+.8f,1.8f,1.2f);carve.center=Vector3.zero;
            var seal=holder.GetComponent<Membrane>();seal.SetOpen(open);return seal;
        }

        // ---------------------------------------------------------------- the stilt market and the smokehouse
        /// <summary>The market raft on its stilts: a ring of posts down into the water with braces between, three stalls under reed awnings, the post with the Compact notice, crates and
        /// baskets of what's sold.</summary>
        static void Market(ZoneKit kit)
        {
            var rng=new System.Random(37);Color wood=new(.86f,.72f,.5f),dark=new(.58f,.46f,.32f);
            // Stilts: the market stands in the mire on these, and the rim lies on them.
            var stilts=new ZoneKit.Shape();
            for(int k=0;k<40;k++)
            {
                float t=k/40f*Mathf.PI*2;var dir=new Vector2(Mathf.Cos(t),Mathf.Sin(t));var p=MarketAt+new Vector2(dir.x*17.2f,dir.y*13.2f);
                if(RaftLand(p)<.3f||OpeningNear(p,2.4f))continue;
                var foot=new Vector3(p.x,ReedBed+.5f,p.y);stilts.Slot(0).Post(foot,2.6f+.3f*(k%3),.16f,.13f,7,dark,wood);
                if(k%2==0){var next=MarketAt+new Vector2(Mathf.Cos(t+.32f)*17.2f,Mathf.Sin(t+.32f)*13.2f);if(RaftLand(next)>.3f)stilts.Slot(0).Beam(foot+Vector3.up*.6f,new Vector3(next.x,ReedBed+2.2f,next.y),.08f,.08f,dark);}
            }
            kit.Built("Market stilts",stilts,Plank).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            var blocks=new GameObject("Market stalls");
            ReedStall(kit,"Produce stall",blocks,new Vector3(-9.4f,0,51.2f),0,new Color(.62f,.82f,.42f),new Color(.95f,.6f,.3f));
            ReedStall(kit,"Reed-goods stall",blocks,new Vector3(8.2f,0,53.2f),0,new Color(.5f,.66f,.86f),new Color(.86f,.56f,.36f));
            ReedStall(kit,"Net stall",blocks,new Vector3(-11.6f,0,40.4f),70,new Color(.8f,.74f,.5f),new Color(.6f,.8f,.7f));
            // The market post: a pole with a crossbar of notices, the Compact's pinned on with a fish hook.
            var postFoot=kit.On(NoticeAt.x,NoticeAt.y+.9f);
            var postShape=new ZoneKit.Shape().Slot(0).Post(postFoot,3.2f,.14f,.11f,7,dark,wood).Beam(postFoot+new Vector3(-.9f,2.5f,0),postFoot+new Vector3(.9f,2.5f,0),.1f,.08f,dark)
                .Slot(1).Box(postFoot+new Vector3(0,1.55f,-.16f),new Vector3(.62f,.82f,.03f),Quaternion.Euler(0,0,3),new Color(.95f,.94f,.88f)).Box(postFoot+new Vector3(-.55f,1.95f,-.14f),new Vector3(.4f,.5f,.03f),Quaternion.Euler(0,0,-6),new Color(.86f,.74f,.52f))
                .Box(postFoot+new Vector3(.56f,2,-.14f),new Vector3(.36f,.46f,.03f),Quaternion.Euler(0,0,8),new Color(.9f,.8f,.6f));
            var post=kit.Built("Market post",postShape,Plank,Cloth);ZoneKit.Block(post,postFoot+Vector3.up*1.2f,new Vector3(.4f,2.4f,.4f),0,"Market post");
            kit.Sign("STILT MARKET",ReedSigns[1].at.x,ReedSigns[1].at.y);
            // Baskets and crates of what's sold, at the stalls' feet and along the rim.
            var goods=new ZoneKit.Shape();
            void Basket(float x,float z,float size,Color what){var foot=kit.On(x,z);goods.Slot(0).Tube(new[]{foot,foot+Vector3.up*size*.7f},new[]{size*.42f,size*.5f},10,new Color(.8f,.68f,.44f),new Color(.95f,.82f,.56f),2,1,false);
                goods.Slot(1).Lump(foot+Vector3.up*size*.66f,new Vector3(size*.44f,size*.2f,size*.44f),(int)(x*7+z),.25f,what,ZoneKit.Shape.Dim(what,.6f),8,4);}
            Basket(-7,49.6f,.7f,new Color(.6f,.86f,.4f));Basket(-11.8f,49.8f,.6f,new Color(.95f,.6f,.32f));Basket(10.6f,51.4f,.8f,new Color(.5f,.66f,.86f));Basket(5.8f,51.6f,.6f,new Color(.9f,.84f,.6f));
            Basket(-13.2f,43.6f,.7f,new Color(.7f,.8f,.6f));Basket(12.6f,36.8f,.7f,new Color(.86f,.56f,.36f));Basket(-12.8f,36.6f,.6f,new Color(.6f,.84f,.8f));
            kit.Built("Market baskets",goods,Weave,Cloth).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.On;
            foreach(var (x,z) in new[]{(-7f,49.6f),(-11.8f,49.8f),(10.6f,51.4f),(5.8f,51.6f),(-13.2f,43.6f),(12.6f,36.8f),(-12.8f,36.6f)})ZoneKit.Block(blocks,kit.On(x,z,.35f),new Vector3(.8f,.7f,.8f),0,"Basket");
            // Rolled mats stood on end at the reed-goods stall: the town's trade, before the hall's racks are seen.
            var rolls=new ZoneKit.Shape();Color[] dyes={new(.5f,.66f,.86f),new(.86f,.56f,.36f),new(.62f,.8f,.5f),new(.9f,.8f,.5f),new(.7f,.5f,.8f)};
            for(int k=0;k<6;k++){var foot=kit.On(11.6f+(k%3)*.5f,54.4f+(k/3)*.5f);rolls.Tube(new[]{foot,foot+Vector3.up*(1.1f+.2f*(k%2))},new[]{.2f,.2f},10,dyes[k%dyes.Length],ZoneKit.Shape.Dim(dyes[(k+2)%dyes.Length],.9f),2,1.2f);}
            var rolled=kit.Built("Rolled mats",rolls,MatDye);ZoneKit.Block(rolled,kit.On(12.1f,54.65f,.6f),new Vector3(1.5f,1.2f,1),0,"Rolled mats");
            // New mats laid flat on the boards to dry, off the ways, each in its own dye: the market's floor is the weavers' drying ground as much as anyone's.
            var drying=new ZoneKit.Shape();
            foreach(var (x,z,yaw,k) in new[]{(5.5f,38.5f,12f,0),(9.6f,37.4f,-10f,1),(5,42.4f,4f,2),(-5,51.6f,-8f,3),(-4,55f,6f,4),(-8,36.2f,3f,5)})
            {
                var turn=Quaternion.Euler(0,yaw,0);var c=kit.On(x,z,.025f);var a=c+turn*new Vector3(-.7f,0,-1.1f);var b=c+turn*new Vector3(.7f,0,-1.1f);var e=c+turn*new Vector3(.7f,0,1.1f);var f=c+turn*new Vector3(-.7f,0,1.1f);
                drying.Slot(0).Quad(a,b,e,f,Vector3.up,dyes[k%dyes.Length],.5f).Slot(1).Quad(a+Vector3.up*.005f,b+Vector3.up*.005f,b+turn*Vector3.forward*.16f+Vector3.up*.005f,a+turn*Vector3.forward*.16f+Vector3.up*.005f,Vector3.up,dyes[(k+3)%dyes.Length],.5f);
            }
            var laid=kit.Built("Mats drying on the boards",drying,MatDye,MatDye);laid.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            // Lines strung over the market from mast to mast, hung with dyed pennants and a lantern or two: what a raft town that talks in light hangs over its square. Four masts at
            // the market's corners carry them, so the floor between stays open to walk and the lines' shadows cross it.
            var lines=new ZoneKit.Shape();var masts=new GameObject("Market masts");
            Vector3 Mast(float x,float z){var foot=kit.On(x,z);lines.Slot(0).Post(foot,5.2f,.12f,.09f,7,dark,wood);ZoneKit.Block(masts,foot+Vector3.up*1.2f,new Vector3(.34f,2.4f,.34f),0,"Market mast");return foot+Vector3.up*5;}
            var nw=Mast(-12.6f,56.4f);var ne=Mast(14.4f,49.4f);var sw=Mast(-12.8f,35.4f);var se=Mast(12.6f,35.2f);
            var pennants=new[]{new Color(.48f,.62f,.86f),new Color(.88f,.58f,.38f),new Color(.6f,.8f,.48f),new Color(.92f,.82f,.5f),new Color(.72f,.52f,.82f),new Color(.86f,.44f,.4f)};int flag=0;
            foreach(var (a,b) in new[]{(nw,se),(ne,sw),(nw,ne),(sw,se)})
            {
                float sag=1.1f;lines.Slot(0).Rope(a,b,sag,.03f,new Color(.95f,.86f,.62f),5,16);
                float length=Vector3.Distance(a,b);int count=Mathf.FloorToInt(length/1.3f);var across=Vector3.Cross(Vector3.up,(b-a).normalized);
                for(int k=1;k<count;k++)
                {
                    float t=k/(float)count;var at=Vector3.Lerp(a,b,t)+Vector3.down*sag*4*t*(1-t);
                    if(k%5==0)lines.Slot(2).Lump(at+Vector3.down*.35f,new Vector3(.16f,.22f,.16f),k,.05f,Color.white,Color.white,8,5);
                    else lines.Slot(1).Triangle(at-across*.2f,at+across*.2f,at+Vector3.down*.55f,Vector3.up+(b-a).normalized,pennants[flag++%pennants.Length],1);
                }
            }
            var strung=kit.Built("Market lines",lines,Plank,Cloth,Lantern);strung.transform.SetParent(masts.transform,true);
            // Fish traps stacked by the smokehouse, and two coils of line.
            var gear=new ZoneKit.Shape();
            foreach(var (x,z,yaw) in new[]{(19f,36.4f,20f),(19.9f,36f,70f),(19.4f,37.4f,-30f)}){var foot=kit.On(x,z,.3f);var t=Quaternion.Euler(0,yaw,0);gear.Slot(0).Tube(new[]{foot+t*new Vector3(0,0,-.55f),foot+t*new Vector3(0,0,.55f)},new[]{.32f,.1f},9,new Color(.8f,.7f,.48f),new Color(.9f,.8f,.56f),2,.8f);}
            foreach(var (x,z) in new[]{(12.8f,39.8f),(-14,50.6f)})for(int turn=0;turn<3;turn++){var coil=new Vector3[13];var thick=new float[13];for(int k=0;k<13;k++){float a=k*Mathf.PI*2/12;coil[k]=kit.On(x+Mathf.Cos(a)*(.5f-turn*.1f),z+Mathf.Sin(a)*(.5f-turn*.1f),.06f+turn*.09f);thick[k]=.05f;}gear.Slot(1).Tube(coil,thick,5,new Color(.95f,.86f,.62f),new Color(.95f,.86f,.62f),1,1,false);}
            var gearView=kit.Built("Market gear",gear,Weave,Plank);ZoneKit.Block(gearView,kit.On(19.4f,36.6f,.3f),new Vector3(1.6f,.6f,1.6f),0,"Traps");
        }
        static bool OpeningNear(Vector2 p,float reach){foreach(var (from,to) in OpeningList)if(ZoneKit.Segment(p,from,to)<reach)return true;return false;}
        /// <summary>A stall under a reed awning: a counter of boards on posts, four poles and an awning of woven reed pitched to the back, its edge dyed. Each its own small mesh, so the ground kit
        /// keeps its details off it and not off a bare rectangle the size of the market.</summary>
        static void ReedStall(ZoneKit kit,string name,GameObject blocks,Vector3 at,float yaw,Color awning,Color edge)
        {
            var shape=new ZoneKit.Shape();var foot=kit.On(at);var turn=Quaternion.Euler(0,yaw,0);Color wood=new(.86f,.72f,.5f),dark=new(.58f,.46f,.32f);
            shape.Slot(0).Box(foot+turn*new Vector3(0,.5f,0),new Vector3(3.2f,.9f,1),turn,dark,wood).Box(foot+turn*new Vector3(0,.99f,0),new Vector3(3.5f,.08f,1.25f),turn,wood);
            foreach(var (x,z,tall) in new[]{(-1.7f,-.75f,2.2f),(1.7f,-.75f,2.2f),(-1.7f,1.1f,2.9f),(1.7f,1.1f,2.9f)})shape.Slot(0).Post(foot+turn*new Vector3(x,0,z),tall,.08f,.07f,6,dark,wood);
            // The awning: a woven sheet from the high back poles forward and down over the counter, its front edge a dyed band.
            Vector3 A=foot+turn*new Vector3(-1.95f,2.95f,1.2f),B=foot+turn*new Vector3(1.95f,2.95f,1.2f),C=foot+turn*new Vector3(1.95f,2.15f,-1.05f),D=foot+turn*new Vector3(-1.95f,2.15f,-1.05f);
            shape.Slot(1).Quad(A,B,C,D,turn*new Vector3(0,1,-.5f),awning,.8f).Quad(D,C,C+Vector3.down*.32f,D+Vector3.down*.32f,turn*Vector3.back,edge,.8f);
            for(int k=0;k<4;k++)shape.Slot(2).Lump(foot+turn*new Vector3(-1.2f+k*.8f,1.14f,.05f*(k%2)),new Vector3(.2f,.13f,.2f),k+(int)at.x,.2f,k%2==0?new Color(.6f,.86f,.42f):new Color(.96f,.7f,.36f),new Color(.4f,.3f,.2f),7,4);
            kit.Built(name,shape,Plank,MatDye,Cloth).transform.SetParent(blocks.transform,true);
            ZoneKit.Block(blocks,foot+turn*new Vector3(0,.55f,0),new Vector3(3.4f,1.1f,1.2f),yaw,"Stall counter");
        }
        /// <summary>Dov's smokehouse on the market's east lobe: a reed-walled house under a steep thatch with a smoke hole, a long rack of eels gone blue over Glowspore under its eave, smoke going
        /// up all day, and his counter in front of it.</summary>
        static void Smokehouse(ZoneKit kit)
        {
            var foot=kit.On(SmokehouseAt.x,SmokehouseAt.y);var shape=new ZoneKit.Shape();Color wall=new(.82f,.76f,.56f),dark=new(.58f,.46f,.32f),wood=new(.86f,.72f,.5f);
            // Walls of upright reed bundles, a doorway on the west, the thatch over it.
            shape.Slot(0).Box(foot+Vector3.up*1.4f,new Vector3(6,2.8f,5),Quaternion.identity,wall,wall,.7f);
            shape.Slot(1).Quad(foot+new Vector3(-3.6f,2.6f,-3.1f),foot+new Vector3(3.6f,2.6f,-3.1f),foot+new Vector3(3.6f,4.9f,0),foot+new Vector3(-3.6f,4.9f,0),new Vector3(0,1,-1),new Color(.86f,.8f,.56f),.6f)
                .Quad(foot+new Vector3(3.6f,2.6f,3.1f),foot+new Vector3(-3.6f,2.6f,3.1f),foot+new Vector3(-3.6f,4.9f,0),foot+new Vector3(3.6f,4.9f,0),new Vector3(0,1,1),new Color(.8f,.74f,.52f),.6f);
            shape.Slot(0).Triangle(foot+new Vector3(-3,2.8f,-2.5f),foot+new Vector3(-3,2.8f,2.5f),foot+new Vector3(-3,4.7f,0),Vector3.left,wall,.7f).Triangle(foot+new Vector3(3,2.8f,-2.5f),foot+new Vector3(3,2.8f,2.5f),foot+new Vector3(3,4.7f,0),Vector3.right,wall,.7f);
            shape.Slot(2).Box(foot+new Vector3(-3.02f,1.1f,0),new Vector3(.08f,2.2f,1.4f),Quaternion.identity,new Color(.1f,.08f,.07f));
            // The smoke hole's cowl at the ridge.
            shape.Slot(3).Tube(new[]{foot+new Vector3(.8f,4.6f,0),foot+new Vector3(.8f,5.5f,0)},new[]{.42f,.3f},8,dark,new Color(.4f,.32f,.26f),1,.5f,false);
            // The eel rack under the south eave: a pole on two forks, eels hanging blue.
            shape.Slot(3).Post(foot+new Vector3(-2.6f,0,-3.4f),2.2f,.07f,.06f,6,dark,wood).Post(foot+new Vector3(2.6f,0,-3.4f),2.2f,.07f,.06f,6,dark,wood).Beam(foot+new Vector3(-2.7f,2.15f,-3.4f),foot+new Vector3(2.7f,2.15f,-3.4f),.08f,.08f,dark);
            for(int k=0;k<11;k++){float x=-2.3f+k*.46f;shape.Slot(4).Tube(new[]{foot+new Vector3(x,2.1f,-3.4f),foot+new Vector3(x+.04f,1.5f,-3.42f),foot+new Vector3(x,.95f,-3.4f)},new[]{.07f,.09f,.04f},6,new Color(.34f,.56f,.8f),new Color(.2f,.36f,.56f),1,1);}
            var house=kit.Built("Smokehouse",shape.Slot(4),ReedBundle,ReedThatch,Cloth,Plank,Cloth);ZoneKit.Fadeable(house);
            ZoneKit.Block(house,foot+Vector3.up*1.4f,new Vector3(6,2.8f,5),0,"Smokehouse wall");ZoneKit.Block(house,foot+new Vector3(0,1.1f,-3.4f),new Vector3(5.6f,2.2f,.4f),0,"Eel rack");
            // Smoke: from the cowl, all day, drifting east.
            var smoke=kit.Drift("Smokehouse smoke",foot+new Vector3(.8f,6.6f,0),new Vector3(.6f,.4f,.6f),kit.Glow("Smoke",false,new Color(.62f,.62f,.66f,1)),new Color(.7f,.7f,.74f,.35f),60,.8f,1.8f,.7f,6);
            var main=smoke.main;main.startSpeed=.9f;var shapeModule=smoke.shape;shapeModule.shapeType=ParticleSystemShapeType.Cone;shapeModule.angle=12;smoke.transform.rotation=Quaternion.Euler(-80,90,0);
            var view=smoke.GetComponent<ParticleSystemRenderer>();var smokeMat=new Material(Resources.Load<Material>("Effects/flare_01"));smokeMat.SetColor("_BaseColor",new Color(.7f,.7f,.74f,.5f));
            UnityEditor.AssetDatabase.CreateAsset(smokeMat,"Assets/_Project/Scenes/"+kit.zone+"-Smoke-soft.mat");view.sharedMaterial=smokeMat;
            ReedStall(kit,"Dov's counter",house,new Vector3(17.6f,0,41.2f),-90,new Color(.6f,.52f,.42f),new Color(.34f,.56f,.8f));
            kit.Lamp(new Vector3(SmokehouseAt.x-3.2f,2.3f,SmokehouseAt.y-2.4f),7,1.1f,new Color(1,.7f,.42f));
            kit.Sign("SMOKEHOUSE",ReedSigns[2].at.x,ReedSigns[2].at.y);
        }

        // ---------------------------------------------------------------- Quill's pump raft
        /// <summary>Quill's raft, west: his pump-and-press (a copy of the old engineers' pump, counted from its pipes: the generated Ancelane pump where it is taken in), its pipes down into
        /// the mire, crates of brass fittings, and the frame a Shaper is mended in (repair and save).</summary>
        static void PumpRaft(ZoneKit kit)
        {
            Color wood=new(.86f,.72f,.5f),dark=new(.58f,.46f,.32f),brass=new(.95f,.78f,.44f);
            var pressFoot=kit.On(PressAt.x,PressAt.y);GameObject press;
            if(ZoneKit.Landed("AncelanePump")){press=kit.Prop("AncelanePump",new Vector3(PressAt.x,0,PressAt.y),2.8f,150);press.name="Quill's pump-and-press";}
            else
            {
                var shape=new ZoneKit.Shape().Slot(0).Box(pressFoot+Vector3.up*.5f,new Vector3(1.8f,1,1.4f),Quaternion.Euler(0,30,0),dark,wood).Slot(1).Post(pressFoot+Vector3.up*1,1.4f,.42f,.36f,12,brass,new Color(1,.9f,.6f))
                    .Tube(new[]{pressFoot+new Vector3(-1.3f,2.2f,0),pressFoot+new Vector3(1.3f,2.2f,0)},new[]{.9f,.9f},16,brass,brass,1,.5f,false);
                press=kit.Built("Quill's pump-and-press",shape,Plank,Brass);
            }
            ZoneKit.Block(press,pressFoot+Vector3.up*1.1f,new Vector3(2,2.2f,1.8f),30,"Press");
            var bench=new GameObject("The pump-and-press",typeof(Bench)).GetComponent<Bench>();bench.transform.position=kit.On(PressAt.x+1.3f,PressAt.y-1.3f);bench.prompt="Fabricate — Quill's Pump-and-Press";bench.range=2.6f;
            // Pipes from the pump down into the mire, and a brass gauge on a post: what a pump-wright's raft looks like from above.
            var pipes=new ZoneKit.Shape();
            pipes.Slot(0).Tube(new[]{pressFoot+new Vector3(-.6f,1,-.2f),pressFoot+new Vector3(-2.4f,.8f,.4f),pressFoot+new Vector3(-4.6f,.2f,.8f),new Vector3(PressAt.x-6.4f,ReedWater-.4f,PressAt.y+1.2f)},new[]{.16f,.16f,.16f,.16f},8,brass,ZoneKit.Shape.Dim(brass,.7f),1,.6f);
            pipes.Slot(0).Tube(new[]{pressFoot+new Vector3(.4f,1.4f,.5f),pressFoot+new Vector3(.8f,2.6f,1.6f),pressFoot+new Vector3(1.6f,2.6f,3.2f),kit.On(PressAt.x+1.8f,PressAt.y+3.2f)},new[]{.12f,.12f,.12f,.12f},8,brass,ZoneKit.Shape.Dim(brass,.8f),1,.6f);
            pipes.Slot(1).Post(kit.On(-46.5f,44.5f),1.2f,.06f,.05f,6,dark,wood).Slot(0).Disc(kit.On(-46.5f,44.5f,1.3f),new Vector3(-.4f,.5f,-1),.28f,14,brass,0,ZoneKit.Shape.Dim(brass,.6f));
            var piped=kit.Built("Quill's pipes",pipes,Brass,Plank);ZoneKit.Block(piped,kit.On(PressAt.x+1.8f,PressAt.y+3.2f,.5f),new Vector3(.4f,1,.4f),0,"Pipe foot");
            // Crates of fittings and a bench of parts.
            var crates=new ZoneKit.Shape();
            foreach(var (x,z,yaw,tall) in new[]{(-47f,40.6f,10f,.7f),(-47.6f,41.8f,-15f,.5f),(-46f,49f,25f,.6f),(-33.4f,50.6f,5f,.55f)})
            {
                var foot=kit.On(x,z);crates.Slot(0).Box(foot+Vector3.up*tall*.5f,new Vector3(.9f,tall,.7f),Quaternion.Euler(0,yaw,0),dark,wood);
                for(int k=0;k<5;k++)crates.Slot(1).Lump(foot+Quaternion.Euler(0,yaw,0)*new Vector3(-.3f+k*.15f,tall+.06f,(k%2-.5f)*.25f),new Vector3(.07f,.05f,.07f),k,.2f,brass,ZoneKit.Shape.Dim(brass,.6f),6,3);
            }
            var stock=kit.Built("Fittings",crates,Plank,Brass);
            foreach(var (x,z,yaw,tall) in new[]{(-47f,40.6f,10f,.7f),(-47.6f,41.8f,-15f,.5f),(-46f,49f,25f,.6f),(-33.4f,50.6f,5f,.55f)})ZoneKit.Block(stock,kit.On(x,z,tall*.5f),new Vector3(.9f,tall,.7f),yaw,"Crate");
            // The mending frame: an A-frame with a sling and a lamp.
            var frameAt=kit.On(FrameAt.x,FrameAt.y);var frame=new ZoneKit.Shape();var turn=Quaternion.Euler(0,-20,0);
            foreach(int end in new[]{-1,1})foreach(int leg in new[]{-1,1})frame.Slot(0).Tube(new[]{frameAt+turn*new Vector3(end*1.3f,0,leg*.7f),frameAt+turn*new Vector3(end*1.2f,2f,0)},new[]{.08f,.07f},6,dark,wood);
            frame.Slot(0).Beam(frameAt+turn*new Vector3(-1.35f,2,0),frameAt+turn*new Vector3(1.35f,2,0),.1f,.1f,wood);
            frame.Slot(1).Lump(frameAt+Vector3.up*.75f,new Vector3(1.05f,.2f,.5f),5,.08f,new Color(.56f,.6f,.72f),new Color(.36f,.38f,.46f),10,5,.25f,turn);
            frame.Slot(2).Lump(frameAt+turn*new Vector3(0,1.72f,0),new Vector3(.17f,.22f,.17f),3,.05f,Color.white,Color.white,8,5);
            var cradle=kit.Built("Quill's mending frame",frame.Slot(2),Plank,MatDye,Lantern);ZoneKit.Block(cradle,frameAt+Vector3.up*.5f,new Vector3(2.6f,1,1.2f),-20,"Frame");
            var repair=new GameObject("Mending frame point",typeof(RepairBay)).GetComponent<RepairBay>();repair.transform.position=frameAt+turn*new Vector3(0,0,1.15f);repair.transform.SetParent(cradle.transform,true);
            repair.prompt="Quill's mending frame — repair and save";repair.saveSpawn="Pump";repair.range=2.6f;
            kit.Lamp(new Vector3(FrameAt.x,1.7f,FrameAt.y),5,.7f,new Color(1,.8f,.5f));
            kit.Sign("PUMP RAFT",ReedSigns[4].at.x,ReedSigns[4].at.y);
        }

        // ---------------------------------------------------------------- the weaving hall
        /// <summary>The landmark, on the north raft: the weaving hall (the generated hall where it is taken in), its drying racks of air-mats behind it and along its east side, taller than anything
        /// else in town and dyed in bands, the reed store west of the door with its eleven poles locked behind a bar (open from <c>wend.decided</c>), and Pim's bench.</summary>
        static void Hall(ZoneKit kit)
        {
            Color wood=new(.86f,.72f,.5f),dark=new(.58f,.46f,.32f);
            GameObject hall;var hallFoot=kit.On(HallAt.x,HallAt.y);
            if(ZoneKit.Landed("WeavingHall")){hall=kit.Prop("WeavingHall",new Vector3(HallAt.x,0,HallAt.y),8.6f,180,false);hall.name="Weaving hall";}
            else
            {
                var shape=new ZoneKit.Shape().Slot(0).Quad(hallFoot+new Vector3(-6,0,-5),hallFoot+new Vector3(6,0,-5),hallFoot+new Vector3(6,8,0),hallFoot+new Vector3(-6,8,0),new Vector3(0,.6f,-1),Color.white,.5f)
                    .Quad(hallFoot+new Vector3(6,0,5),hallFoot+new Vector3(-6,0,5),hallFoot+new Vector3(-6,8,0),hallFoot+new Vector3(6,8,0),new Vector3(0,.6f,1),Color.white,.5f);
                hall=kit.Built("Weaving hall",shape,ReedThatch);
            }
            ZoneKit.Fadeable(hall);var drawn=ZoneKit.Drawn(hall);
            ZoneKit.Block(hall,new Vector3(drawn.center.x,hallFoot.y+2,drawn.center.z),new Vector3(drawn.size.x*.84f,4,drawn.size.z*.8f),0,"Hall wall");
            // The racks: tall frames of poles, each hung with mats in a dyed band, behind the hall and down its east side. Taller than anything else in town, at the back where the tall belongs.
            var racks=new ZoneKit.Shape();var mats=new ZoneKit.Shape();var rackBlocks=new GameObject("Drying racks");var rng=new System.Random(43);var rackRuns=new List<(Vector3 a,Vector3 b)>();
            Color[] dyes={new(.48f,.62f,.86f),new(.88f,.58f,.38f),new(.6f,.8f,.48f),new(.92f,.82f,.5f),new(.72f,.52f,.82f),new(.86f,.44f,.4f)};
            void Rack(Vector2 from,Vector2 to,float tall,int rows)
            {
                var a=kit.On(from.x,from.y);var b=kit.On(to.x,to.y);var run=b-a;run.y=0;float length=run.magnitude;var along=run/length;var face=Vector3.Cross(along,Vector3.up);
                int bays=Mathf.Max(1,Mathf.RoundToInt(length/2.4f));
                for(int k=0;k<=bays;k++){var foot=Vector3.Lerp(a,b,k/(float)bays);racks.Slot(0).Post(foot,tall,.11f,.08f,6,dark,wood);}
                for(int r=1;r<=rows;r++){float y=tall*r/(rows+.4f);racks.Slot(0).Beam(a+Vector3.up*y,b+Vector3.up*y,.08f,.08f,dark);}
                for(int k=0;k<bays;k++)for(int r=1;r<=rows;r++)
                {
                    if(rng.Next(7)==0)continue;
                    float y=tall*r/(rows+.4f),drop=tall/(rows+.4f)*.82f;var left=Vector3.Lerp(a,b,(k+.06f)/bays)+Vector3.up*y;var right=Vector3.Lerp(a,b,(k+.94f)/bays)+Vector3.up*y;var sway=face*(((float)rng.NextDouble()-.5f)*.3f);
                    var dye=dyes[rng.Next(dyes.Length)];var shade=.86f+.2f*(float)rng.NextDouble();
                    mats.Quad(left,right,right+Vector3.down*drop+sway,left+Vector3.down*drop+sway,face,ZoneKit.Shape.Dim(dye,shade),.5f);
                }
                rackRuns.Add((a,b));
            }
            Rack(new Vector2(-18,95.2f),new Vector2(-6.5f,95.6f),8.6f,4);Rack(new Vector2(6.5f,95.6f),new Vector2(18,95.2f),8.2f,4);Rack(new Vector2(-4.5f,96.2f),new Vector2(4.5f,96.2f),9.2f,4);
            Rack(new Vector2(8.2f,92.4f),new Vector2(18.2f,92.4f),6.8f,3);
            var rackView=kit.Built("Drying racks",racks,Plank);rackView.transform.SetParent(rackBlocks.transform,true);
            // Each rack's box hangs under the drawn frames, so what stops a body is the thing it can see: under a bare holder it read as an invisible wall across open deck.
            foreach(var (a,b) in rackRuns)ZoneKit.BlockAlong(rackView,a+Vector3.up*1.2f,b+Vector3.up*1.2f,.4f,2.4f,"Rack");
            var hung=kit.Built("Air-mats drying",mats,MatDye);hung.transform.SetParent(rackBlocks.transform,true);hung.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.TwoSided;
            // The reed store: a low hut of bundles west of the door, a rack of poles barred across its front; the bar gone and the poles out by the door once Ona has decided.
            var storeFoot=kit.On(StoreAt.x,StoreAt.y+2.4f);var store=new ZoneKit.Shape();
            store.Slot(0).Box(storeFoot+Vector3.up*1.1f,new Vector3(4.4f,2.2f,3),Quaternion.identity,new Color(.82f,.76f,.56f),null,.7f);
            store.Slot(1).Quad(storeFoot+new Vector3(-2.6f,2.1f,-1.9f),storeFoot+new Vector3(2.6f,2.1f,-1.9f),storeFoot+new Vector3(2.6f,3.2f,0),storeFoot+new Vector3(-2.6f,3.2f,0),new Vector3(0,1,-1),new Color(.86f,.8f,.56f),.6f)
                .Quad(storeFoot+new Vector3(2.6f,2.1f,1.9f),storeFoot+new Vector3(-2.6f,2.1f,1.9f),storeFoot+new Vector3(-2.6f,3.2f,0),storeFoot+new Vector3(2.6f,3.2f,0),new Vector3(0,1,1),new Color(.8f,.74f,.52f),.6f);
            var storeBody=kit.Built("Reed store",store,ReedBundle,ReedThatch);ZoneKit.Block(storeBody,storeFoot+Vector3.up*1.1f,new Vector3(4.4f,2.2f,3),0,"Reed store wall");
            var locked=new ZoneKit.Shape();var open=new ZoneKit.Shape();
            for(int k=0;k<11;k++)
            {
                var lean=storeFoot+new Vector3(-1.9f+k*.38f,0,-1.62f);locked.Slot(0).Tube(new[]{lean,lean+new Vector3(.12f,3.4f,-.3f)},new[]{.045f,.035f},5,new Color(.8f,.66f,.44f),new Color(.9f,.78f,.52f),1,.6f);
                var outAt=kit.On(StoreAt.x+2.6f+(k%2)*.3f,StoreAt.y+1.6f+(k/2)*.3f);open.Slot(0).Tube(new[]{outAt,outAt+new Vector3(-.5f,3.3f,.1f)},new[]{.045f,.035f},5,new Color(.8f,.66f,.44f),new Color(.9f,.78f,.52f),1,.6f);
            }
            locked.Slot(1).Beam(storeFoot+new Vector3(-2.2f,1.3f,-1.8f),storeFoot+new Vector3(2.2f,1.3f,-1.8f),.14f,.12f,new Color(.4f,.3f,.22f)).Slot(1).Box(storeFoot+new Vector3(0,1.3f,-1.9f),new Vector3(.24f,.3f,.12f),Quaternion.identity,new Color(.5f,.52f,.56f));
            var poles=kit.Built("Poles locked in the store",locked,Plank,Plank);var outPoles=kit.Built("Poles out by the door",open,Plank);
            var shut=new GameObject("The reed store is locked",typeof(FlagGate)).GetComponent<FlagGate>();shut.flag="wend.decided";shut.showWhenSet=false;shut.targets=new[]{poles};
            var opened=new GameObject("The reed store is open",typeof(FlagGate)).GetComponent<FlagGate>();opened.flag="wend.decided";opened.targets=new[]{outPoles};outPoles.SetActive(false);
            // Pim's bench at the hall's front: a low frame with a half-woven mat on it and a comb.
            var benchFoot=kit.On(PimAt.x+1,PimAt.y+1.2f);var bench=new ZoneKit.Shape().Slot(0).Box(benchFoot+Vector3.up*.35f,new Vector3(1.8f,.7f,.8f),Quaternion.Euler(0,-20,0),new Color(.58f,.46f,.32f),new Color(.86f,.72f,.5f))
                .Slot(1).Box(benchFoot+Vector3.up*.73f,new Vector3(1.2f,.03f,.6f),Quaternion.Euler(0,-20,0),new Color(.5f,.66f,.86f)).Slot(0).Box(benchFoot+new Vector3(.6f,.76f,.1f),new Vector3(.24f,.03f,.08f),Quaternion.Euler(0,30,0),new Color(.9f,.82f,.6f));
            var benchBody=kit.Built("Pim's bench",bench,Plank,MatDye);ZoneKit.Block(benchBody,benchFoot+Vector3.up*.35f,new Vector3(1.8f,.7f,.8f),-20,"Bench");
            kit.Lamp(new Vector3(HallAt.x+4,3,HallAt.y-6),9,1.3f,new Color(1,.8f,.5f));
            kit.Sign("WEAVING HALL",ReedSigns[3].at.x,ReedSigns[3].at.y);
        }

        // ---------------------------------------------------------------- the school raft
        static void School(ZoneKit kit)
        {
            Color wood=new(.86f,.72f,.5f),dark=new(.58f,.46f,.32f);var foot=kit.On(46.2f,58.6f);var turn=Quaternion.Euler(0,-10,0);
            // A shelter: four poles and a reed roof, slates and a bench under it, and the bell frame on the rim with nothing in it.
            var shelter=new ZoneKit.Shape();
            foreach(var (x,z) in new[]{(-2.2f,-1.6f),(2.2f,-1.6f),(-2.2f,1.6f),(2.2f,1.6f)})shelter.Slot(0).Post(foot+turn*new Vector3(x,0,z),2.6f,.09f,.08f,6,dark,wood);
            shelter.Slot(1).Quad(foot+turn*new Vector3(-2.6f,2.4f,-2),foot+turn*new Vector3(2.6f,2.4f,-2),foot+turn*new Vector3(2.6f,3.1f,2),foot+turn*new Vector3(-2.6f,3.1f,2),Vector3.up,new Color(.86f,.8f,.56f),.6f);
            shelter.Slot(0).Box(foot+turn*new Vector3(0,.25f,.9f),new Vector3(3.4f,.5f,.5f),turn,dark,wood);
            for(int k=0;k<4;k++)shelter.Slot(2).Box(foot+turn*new Vector3(-1.2f+k*.8f,.53f,.9f),new Vector3(.4f,.03f,.3f),turn*Quaternion.Euler(0,k*9,0),new Color(.22f,.24f,.26f));
            var bell=kit.On(35.2f,64.4f);
            shelter.Slot(0).Post(bell+new Vector3(-.6f,0,0),2.2f,.07f,.06f,6,dark,wood).Post(bell+new Vector3(.6f,0,0),2.2f,.07f,.06f,6,dark,wood).Beam(bell+new Vector3(-.7f,2.15f,0),bell+new Vector3(.7f,2.15f,0),.09f,.09f,dark);
            shelter.Slot(0).Rope(bell+new Vector3(0,2.1f,0),bell+new Vector3(.1f,1.5f,.1f),.02f,.025f,new Color(.95f,.86f,.62f));
            var school=kit.Built("School shelter",shelter,Plank,ReedThatch,Cloth);ZoneKit.Fadeable(school);
            foreach(var (x,z) in new[]{(-2.2f,-1.6f),(2.2f,-1.6f),(-2.2f,1.6f),(2.2f,1.6f)})ZoneKit.Block(school,foot+turn*new Vector3(x,1.3f,z),new Vector3(.3f,2.6f,.3f),-10,"Shelter post");
            ZoneKit.Block(school,foot+turn*new Vector3(0,.25f,.9f),new Vector3(3.4f,.5f,.5f),-10,"School bench");ZoneKit.Block(school,bell+Vector3.up*1.1f,new Vector3(1.6f,2.2f,.3f),0,"Bell frame");
            kit.Sign("SCHOOL RAFT",ReedSigns[5].at.x,ReedSigns[5].at.y);
        }

        /// <summary>An arch of bundled reed bent over and bound at its crown, tall enough to walk under: the mire gate. The fade is told where the way through is.</summary>
        static GameObject ReedArch(ZoneKit kit,string name,Vector3 at,float tall,float yaw)
        {
            var foot=kit.On(at);var turn=Quaternion.Euler(0,yaw,0);var shape=new ZoneKit.Shape();float half=tall*.38f;
            foreach(int side in new[]{-1,1})
            {
                var path=new List<Vector3>();var thick=new List<float>();
                for(int k=0;k<=10;k++){float t=k/10f,a=t*Mathf.PI*.5f;path.Add(foot+turn*new Vector3(side*half*Mathf.Cos(a)*1.05f,tall*Mathf.Sin(a),0));thick.Add(Mathf.Lerp(.42f,.3f,t));}
                shape.Slot(0).Tube(path,thick,9,new Color(.78f,.74f,.5f),new Color(.9f,.86f,.6f),3,.5f);
                for(int b=0;b<4;b++){float t=.15f+b*.22f,a=t*Mathf.PI*.5f;var c=foot+turn*new Vector3(side*half*Mathf.Cos(a)*1.05f,tall*Mathf.Sin(a),0);shape.Slot(1).Tube(new[]{c+turn*Vector3.back*.46f,c+turn*Vector3.forward*.46f},new[]{.46f,.46f},9,new Color(.5f,.36f,.26f),new Color(.5f,.36f,.26f),1,1,false);}
            }
            // Lamps hung either side of the way through, keeping Driftreed's count.
            foreach(int side in new[]{-1,1})shape.Slot(2).Lump(foot+turn*new Vector3(side*half*.7f,tall*.62f,-.3f),new Vector3(.2f,.26f,.2f),3,.05f,Color.white,Color.white,8,5);
            var arch=kit.Built(name,shape,ReedBundle,Plank,Lantern);
            ZoneKit.Fadeable(arch);var fade=arch.GetComponent<SceneryOcclusion>();if(fade!=null)fade.opening=new Bounds(foot+Vector3.up*tall*.4f,turn*new Vector3(half*1.4f,tall*.8f,2.4f));
            foreach(int side in new[]{-1,1})ZoneKit.Block(arch,foot+turn*new Vector3(side*half,1.2f,0),new Vector3(1,2.4f,1),yaw,"Arch foot");
            kit.Lamp(new Vector3(at.x,tall*.62f,at.z-.6f),6,.9f,new Color(1,.8f,.5f));
            return arch;
        }

        // ---------------------------------------------------------------- the mire round the town
        /// <summary>Mangroves on the banks north and east, behind the hall and past the gate: trunks on arched stilt roots, crowns over them, so the town is a clearing in something; and a
        /// few dead snags and stilt huts out on the water to the west, low, on the camera's side.</summary>
        static void Mangroves(ZoneKit kit)
        {
            var rng=new System.Random(113);var trunks=new ZoneKit.Shape();var crowns=new ZoneKit.Shape();
            void Tree(float x,float z,float tall,float girth,bool crowned)
            {
                var foot=new Vector3(x,Mathf.Max(ReedWater,kit.Y(x,z))+1.4f,z);float lean=((float)rng.NextDouble()-.5f)*1.6f;
                var path=new List<Vector3>();var thick=new List<float>();
                for(int k=0;k<=6;k++){float t=k/6f;path.Add(foot+new Vector3(lean*t+Mathf.Sin(t*3+x)*.3f,tall*t,Mathf.Cos(t*2.5f+z)*.3f));thick.Add(girth*Mathf.Lerp(1,.55f,Mathf.Sqrt(t)));}
                trunks.Tube(path,thick,9,new Color(.56f,.52f,.5f),new Color(.86f,.84f,.8f),3,.16f);
                int roots=5+rng.Next(4);
                for(int r=0;r<roots;r++)
                {
                    float a=r*Mathf.PI*2/roots+(float)rng.NextDouble()*.5f,reach=girth*(3+2*(float)rng.NextDouble());var out2=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                    var end=foot+out2*reach;end.y=ReedWater-.6f;
                    trunks.Tube(new[]{foot+Vector3.up*.4f+out2*girth*.4f,foot+out2*reach*.55f+Vector3.up*1.2f,end},new[]{girth*.36f,girth*.24f,girth*.14f},6,new Color(.6f,.56f,.52f),new Color(.8f,.78f,.74f),1,.2f);
                }
                if(!crowned)return;
                for(int c=0;c<4;c++)
                {
                    var top=path[path.Count-1]+new Vector3(((float)rng.NextDouble()-.5f)*girth*6,-(float)rng.NextDouble()*2,((float)rng.NextDouble()-.3f)*girth*5);float size=girth*(2.8f+1.6f*(float)rng.NextDouble());
                    crowns.Lump(top,new Vector3(size*1.2f,size*.6f,size),rng.Next(999),.24f,new Color(.42f+.12f*(float)rng.NextDouble(),.62f,.4f),new Color(.18f,.28f,.22f),12,6);
                }
            }
            // North, behind the hall: every crown north of the racks, so it hides only more mire.
            for(float x=-60;x<70;x+=9+(float)rng.NextDouble()*4)Tree(x,112+(float)rng.NextDouble()*4,11+(float)rng.NextDouble()*6,1+(float)rng.NextDouble()*.5f,true);
            // East, past the gate and the school: their crowns east of everything walked.
            for(float z=-6;z<108;z+=9+(float)rng.NextDouble()*4)Tree(76+(float)rng.NextDouble()*4,z,10+(float)rng.NextDouble()*6,1+(float)rng.NextDouble()*.4f,true);
            // Dead snags in the open water west and south: no crown, low.
            foreach(var (x,z) in new[]{(-58f,22f),(-60,66),(-52,90),(-28,-8),(24,-10),(58,22),(-56,4)})Tree(x,z,3.6f+(float)rng.NextDouble()*2,.4f,false);
            var trunkView=kit.Built("Mangrove trunks",trunks,Bark);trunkView.isStatic=true;
            kit.Built("Mangrove crowns",crowns,Leaf).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            // Stilt huts out on the mire to the west: somebody else's rafts, low against the water.
            var huts=new ZoneKit.Shape();
            foreach(var (x,z,yaw) in new[]{(-60f,40f,20f),(-58f,80f,-15f)})
            {
                var foot=new Vector3(x,ReedWater,z);var t=Quaternion.Euler(0,yaw,0);
                foreach(var (px,pz) in new[]{(-1.6f,-1.4f),(1.6f,-1.4f),(-1.6f,1.4f),(1.6f,1.4f)})huts.Slot(0).Post(foot+t*new Vector3(px,-.8f,pz),2.6f,.09f,.08f,6,new Color(.58f,.46f,.32f),new Color(.8f,.68f,.48f));
                huts.Slot(0).Box(foot+Vector3.up*1.9f,new Vector3(3.6f,.2f,3.2f),t,new Color(.7f,.6f,.42f));
                huts.Slot(1).Box(foot+Vector3.up*2.8f,new Vector3(3,1.6f,2.6f),t,new Color(.8f,.74f,.52f));
                huts.Slot(2).Quad(foot+t*new Vector3(-2,3.5f,-1.8f),foot+t*new Vector3(2,3.5f,-1.8f),foot+t*new Vector3(2,4.6f,0),foot+t*new Vector3(-2,4.6f,0),t*new Vector3(0,1,-1),new Color(.86f,.8f,.56f),.6f)
                    .Quad(foot+t*new Vector3(2,3.5f,1.8f),foot+t*new Vector3(-2,3.5f,1.8f),foot+t*new Vector3(-2,4.6f,0),foot+t*new Vector3(2,4.6f,0),t*new Vector3(0,1,1),new Color(.8f,.74f,.52f),.6f);
            }
            kit.Built("Stilt huts on the mire",huts,Plank,ReedBundle,ReedThatch);
        }
        /// <summary>What lies on and stands in the water between the rafts, thick within a frame's reach of where anyone walks: lily rafts, reed beds, moored canoes and floats on lines,
        /// fish traps on poles. A bare sheet of water was the plainest thing in Rootmarket's frames until it had these.</summary>
        static void WaterLife(ZoneKit kit)
        {
            var rng=new System.Random(131);var reeds=new ZoneKit.Shape();var pads=new ZoneKit.Shape();var gear=new ZoneKit.Shape();
            for(int k=0;k<1400;k++)
            {
                var p=new Vector2((float)rng.NextDouble()*146-64,(float)rng.NextDouble()*128-12);float size=.5f+(float)rng.NextDouble()*.9f,turn=(float)rng.NextDouble()*6.28f;int kind=rng.Next(6);bool far=rng.Next(4)!=0;
                if(ReedHeight(p)>ReedWater-.5f)continue;
                float out2=RaftLand(p);if(out2<1.4f)continue;if(out2>12&&far)continue;
                bool walk=false;foreach(var (_,a,b,_,_) in Walks)if(ZoneKit.Segment(p,a,b)<3.2f){walk=true;break;}
                foreach(var (_,a,b,turned,away) in Walks)if(turned!=0){var end=a+Turn(b-a,away);if(ZoneKit.Segment(p,a,end)<3.2f)walk=true;}
                if(walk||ZoneKit.Segment(p,LineMoored,LineAdrift)<7)continue;
                var at=new Vector3(p.x,ReedWater,p.y);
                if(kind<2)for(int r=0;r<5;r++)reeds.Frond(at+new Vector3(Mathf.Cos(r*1.3f)*.2f,-.1f,Mathf.Sin(r*1.3f)*.2f),new Vector3(Mathf.Cos(turn+r*1.26f),0,Mathf.Sin(turn+r*1.26f)),.5f,.1f,1.1f+size,.2f,new Color(.32f,.44f,.28f),new Color(.66f,.78f,.44f),3);
                else
                {
                    for(int r=0;r<1+rng.Next(3);r++)
                    {
                        var c=at+new Vector3(Mathf.Cos(turn+r*2)*size*.9f,.03f+.004f*r,Mathf.Sin(turn+r*2)*size*.9f);float green=.46f+.26f*(float)rng.NextDouble();
                        pads.Slot(0).Disc(c,Vector3.up,size*(.5f+.2f*r),9,new Color(.34f,green,.34f),0,new Color(.26f,green*.8f,.26f));
                        // Wendmire's lilies glow faintly at dusk: one in four.
                        if(rng.Next(4)==0)pads.Slot(1).Lump(c+Vector3.up*.08f,new Vector3(.14f,.1f,.14f),k,.2f,new Color(.7f,1,.86f),new Color(.4f,.7f,.6f),7,4);
                    }
                }
            }
            // Canoes and floats moored along the rafts, each on a short line.
            foreach(var (x,z,yaw) in new[]{(-18f,8f,80f),(17.6f,20.6f,-10f),(-20.6f,54f,15f),(19.6f,62f,-30f),(-24f,90f,70f),(24.6f,96f,-60f),(31f,72f,40f),(-28.6f,30f,30f),(54.6f,52f,-20f),(-52.6f,52f,80f)})
            {
                var at=new Vector3(x,ReedWater+.05f,z);
                gear.Slot(0).Lump(at,new Vector3(.66f,.42f,2.5f),(int)(x*3+z),.05f,new Color(.82f,.74f,.52f),new Color(.56f,.48f,.34f),10,5,.25f,Quaternion.Euler(0,yaw,0));
                gear.Slot(1).Box(at+Vector3.up*.3f,new Vector3(.8f,.05f,.22f),Quaternion.Euler(0,yaw,0),new Color(.6f,.48f,.34f));
            }
            // Fish traps: woven cones on a pole each, stood in the water off the stage and the pump raft.
            foreach(var (x,z) in new[]{(-17f,2f),(-19.6f,4.6f),(-52f,34f),(-54f,38f),(20f,-1f)})
            {
                var at=new Vector3(x,ReedWater,z);gear.Slot(1).Post(at+Vector3.down*.6f,2.2f,.06f,.05f,6,new Color(.58f,.46f,.32f),new Color(.8f,.68f,.48f));
                gear.Slot(2).Tube(new[]{at+Vector3.up*.2f,at+Vector3.up*1.2f},new[]{.42f,.12f},9,new Color(.8f,.7f,.48f),new Color(.9f,.8f,.56f),2,.8f,false);
            }
            kit.Built("Reed beds",reeds,ReedThatch).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            kit.Built("Lily rafts",pads,Cloth,Lantern).GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            kit.Built("Moored canoes and traps",gear,Weave,Plank,Weave);
        }
        /// <summary>What a town that drifts on the tide has under it and between its rafts, beyond the lilies and reeds: lines strung raft to raft over the water, hung with lanterns,
        /// nets, floats and fish drying; skiffs moored along the rims, each on its line to a post, some with a lantern at the stern and a load aboard; and weed lying in long drifts the
        /// way the tide runs. Every lantern on the water keeps the town's count and lies on the water below it as a streak of its light, toward the camera, blinking with it.
        /// The water between the rafts was plain, dark and empty.</summary>
        static void TideLife(ZoneKit kit)
        {
            var rng=new System.Random(167);
            float FromWalks(Vector2 p){float d=float.PositiveInfinity;foreach(var (_,a,b,tide,away) in Walks){d=Mathf.Min(d,ZoneKit.Segment(p,a,b));if(tide!=0)d=Mathf.Min(d,ZoneKit.Segment(p,a,a+Turn(b-a,away)));}return d;}
            // From a point over the water toward a raft until it is on the rim's crest.
            Vector2 Rim(Vector2 from,Vector2 toward)
            {
                var p=from;for(int i=0;i<400;i++){if(RaftLand(p)<=-.5f)return p;p+=toward.normalized*.1f;}
                throw new InvalidOperationException("Driftreed: no raft from "+from+" toward "+toward);
            }
            var lines=new ZoneKit.Shape();var hung=new ZoneKit.Shape();var root=new GameObject("Life on the water");var glint=kit.Glow("Water glints",false,new Color(2.4f,1.7f,.9f,1));
            Color hemp=new(.92f,.82f,.6f),pole=new(.58f,.46f,.32f),poleTop=new(.84f,.7f,.48f);
            var toCamera=new Vector3(-Mathf.Sin(20*Mathf.Deg2Rad),0,-Mathf.Cos(20*Mathf.Deg2Rad));int lanterns=0;
            void HangLantern(Vector3 at)
            {
                var lamp=kit.Built("Water lantern "+lanterns,new ZoneKit.Shape().Slot(0).Lump(at,new Vector3(.15f,.21f,.15f),lanterns,.05f,Color.white,Color.white,8,5),Lantern);
                lamp.transform.SetParent(root.transform,true);lamp.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                var streak=GameObject.CreatePrimitive(PrimitiveType.Quad);streak.name="A lantern's light on the water";UnityEngine.Object.DestroyImmediate(streak.GetComponent<Collider>());streak.transform.SetParent(root.transform,false);
                streak.transform.position=new Vector3(at.x,ReedWater+.03f,at.z)+toCamera*2f;streak.transform.rotation=Quaternion.Euler(90,20,0);streak.transform.localScale=new Vector3(1.1f,4.6f,1);
                var view=streak.GetComponent<Renderer>();view.sharedMaterial=glint;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
                var rhythm=lamp.AddComponent<LampRhythm>();rhythm.count=rhythm.retuned=LampRhythm.Count.TwoShortOneLong;rhythm.litFlag=rhythm.retunedFlag=rhythm.darkFlag="";
                rhythm.lamps=new[]{lamp.GetComponent<Renderer>()};rhythm.halos=new[]{view};rhythm.colour=new Color(1,.78f,.46f);rhythm.rest=.3f;rhythm.offset=(lanterns%5)*.37f;
                lanterns++;
            }
            // ---- lines raft to raft, each from a point over the gap out to both rims, posts on the rims; nothing within reach of a walk, across or swung away
            foreach(var (mid,dir) in new[]{(new Vector2(12,26),Vector2.up),(new Vector2(-10,66),Vector2.up),(new Vector2(11,66),Vector2.up),(new Vector2(-23,50.5f),Vector2.right),(new Vector2(29,93),Vector2.right)})
            {
                if(FromWalks(mid)<4)throw new InvalidOperationException("Driftreed: a line at "+mid+" hangs over a walk");
                var a2=Rim(mid,-dir);var b2=Rim(mid,dir);var a=kit.On(a2.x,a2.y);var b=kit.On(b2.x,b2.y);
                lines.Slot(0).Post(a,2.9f,.08f,.06f,6,pole,poleTop).Post(b,2.9f,.08f,.06f,6,pole,poleTop);
                var from=a+Vector3.up*2.7f;var to=b+Vector3.up*2.7f;const float Sag=.7f;lines.Slot(1).Rope(from,to,Sag,.025f,hemp,5,14);
                Vector3 On(float t)=>Vector3.Lerp(from,to,t)+Vector3.down*Sag*4*t*(1-t);
                int item=0;
                for(float t=.16f;t<.9f;t+=.14f+.04f*(float)rng.NextDouble(),item++)
                {
                    var at=On(t);int kind=(item+(int)mid.x)%4;
                    if(kind==0){lines.Slot(1).Rope(at,at+Vector3.down*.3f,0,.01f,hemp,4,1);HangLantern(at+Vector3.down*.5f);}
                    else if(kind==1)
                    {
                        // A net hung to dry: a sheet of mesh in a long drop, darker where it is wet at the foot.
                        var side=Vector3.Cross(to-from,Vector3.up).normalized*.02f;var along=(to-from).normalized*.45f;
                        hung.Slot(0).Quad(at-along+side,at+along+side,at+along+Vector3.down*1.3f+side,at-along+Vector3.down*1.1f+side,Vector3.Cross(to-from,Vector3.up),new Color(.66f,.62f,.5f),1.2f);
                    }
                    else if(kind==2)for(int f=0;f<3;f++){var fish=at+(to-from).normalized*(f-1)*.18f;hung.Slot(1).Lump(fish+Vector3.down*.32f,new Vector3(.05f,.26f,.08f),f+item,.08f,new Color(.82f,.74f,.6f),new Color(.56f,.46f,.36f),6,5);}
                    else{hung.Slot(1).Lump(at+Vector3.down*.22f,new Vector3(.17f,.17f,.17f),item,.05f,new Color(.95f,.55f,.3f),new Color(.7f,.36f,.2f),8,5).Lump(at+Vector3.down*.5f,new Vector3(.13f,.13f,.13f),item+1,.05f,new Color(.96f,.86f,.5f),new Color(.7f,.6f,.3f),8,5);}
                }
            }
            // ---- skiffs moored along the rims, outside them, lying along the edge, each with a line to a post on the rim
            var skiffs=new ZoneKit.Shape();var placed=new List<Vector2>{new(-18,8),new(17.6f,20.6f),new(-20.6f,54),new(19.6f,62),new(-24,90),new(24.6f,96),new(31,72),new(-28.6f,30),new(54.6f,52),new(-52.6f,52)};
            Vector2 Outward(Vector2 p){const float E=.05f;return new Vector2(RaftLand(p+Vector2.right*E)-RaftLand(p-Vector2.right*E),RaftLand(p+Vector2.up*E)-RaftLand(p-Vector2.up*E)).normalized;}
            int moored=0;
            foreach(var (c,half,yaw,_) in Rafts)
            {
                int here=0;
                for(int k=0;k<24&&here<2;k++)
                {
                    float a=(k*137.5f+rng.Next(20))*Mathf.Deg2Rad;var dir=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var p=c;
                    for(int i=0;i<400&&RaftLand(p)<1.7f;i++)p+=dir*.1f;
                    if(FromWalks(p)<5||Opening(p)>0||ZoneKit.Segment(p,LineMoored,LineAdrift)<9||Vector2.Distance(p,LineAt)<8||ReedHeight(p)>ReedWater-.4f)continue;
                    bool crowded=false;foreach(var q in placed)if(Vector2.Distance(p,q)<6){crowded=true;break;}if(crowded)continue;
                    placed.Add(p);here++;moored++;
                    var out2=Outward(p);var along=new Vector3(-out2.y,0,out2.x);var at=new Vector3(p.x,ReedWater+.04f,p.y);var turn=Quaternion.LookRotation(along);
                    Color wood=moored%3==0?new Color(.62f,.5f,.36f):moored%3==1?new Color(.8f,.68f,.48f):new Color(.56f,.6f,.5f);
                    // The hull, and its well a shade darker inside it: from above, an open boat and not a stone.
                    skiffs.Slot(0).Lump(at,new Vector3(.62f,.34f,2.1f),moored,.03f,wood,ZoneKit.Shape.Dim(wood,.6f),12,5,.25f,turn).Lump(at+Vector3.up*.12f,new Vector3(.48f,.22f,1.8f),moored+50,.02f,ZoneKit.Shape.Dim(wood,.55f),ZoneKit.Shape.Dim(wood,.45f),12,5,.25f,turn);
                    foreach(float s in new[]{-.6f,.55f})skiffs.Slot(0).Box(at+turn*new Vector3(0,.3f,s),new Vector3(1.02f,.05f,.18f),turn,ZoneKit.Shape.Dim(wood,.9f));
                    skiffs.Slot(0).Tube(new[]{at+turn*new Vector3(.3f,.36f,-1.7f),at+turn*new Vector3(-.2f,.4f,1.9f)},new[]{.03f,.03f},5,pole,poleTop,1,.6f);
                    int load=moored%4;
                    if(load==0)skiffs.Slot(1).Tube(new[]{at+turn*new Vector3(0,.42f,-.2f),at+turn*new Vector3(0,.42f,.25f)},new[]{.24f,.24f},9,new Color(.82f,.72f,.5f),new Color(.9f,.8f,.56f),2,.8f);
                    else if(load==1)skiffs.Slot(2).Tube(new[]{at+turn*new Vector3(-.15f,.4f,-1),at+turn*new Vector3(-.1f,.44f,.9f)},new[]{.16f,.14f},8,new Color(.84f,.78f,.5f),new Color(.92f,.86f,.58f),3,.6f);
                    var rim=Rim(p,-out2);var post=kit.On(rim.x,rim.y);lines.Slot(0).Post(post,1.1f,.07f,.06f,6,pole,poleTop);
                    lines.Slot(1).Rope(at+turn*new Vector3(0,.4f,2.05f),post+Vector3.up*1f,.2f,.02f,hemp,4,6);
                    if(load==2){var stern=at+turn*new Vector3(0,.3f,-1.75f);skiffs.Slot(0).Post(stern,1.2f,.035f,.03f,5,pole,poleTop);HangLantern(stern+Vector3.up*1.32f);}
                }
            }
            // ---- weed lying in long drifts the way the tide runs (toward the south-west), in the gaps within a frame of the rafts and off every walk
            var weed=new ZoneKit.Shape();var tide=new Vector2(-.5f,-.866f);int drifts=0;
            for(int k=0;k<900&&drifts<220;k++)
            {
                var p=new Vector2((float)rng.NextDouble()*120-56,(float)rng.NextDouble()*116-10);float length=1.8f+(float)rng.NextDouble()*3.4f,wide=1.1f+(float)rng.NextDouble()*1.2f,swing=((float)rng.NextDouble()-.5f)*.5f;int tone=rng.Next(3);
                float out2=RaftLand(p);if(out2<1.6f||out2>11||FromWalks(p)<3.4f||ReedHeight(p)>ReedWater-.4f||ZoneKit.Segment(p,LineMoored,LineAdrift)<7)continue;
                bool clash=false;foreach(var q in placed)if(Vector2.Distance(p,q)<2.6f){clash=true;break;}if(clash)continue;
                drifts++;var run=Turn(tide,swing*57.3f);var across=new Vector2(-run.y,run.x);
                Color c=tone==0?new Color(.28f,.34f,.18f):tone==1?new Color(.34f,.27f,.18f):new Color(.2f,.28f,.2f);
                // A drift is a loose mat of small floating rosettes, thickest a little behind its head and thinning along the run: as strips, the tide's way toward the camera, it
                // read as sticks stood in the water.
                int blobs=9+rng.Next(7);
                for(int s=0;s<blobs;s++)
                {
                    float t=(float)rng.NextDouble(),side=((float)rng.NextDouble()-.5f)*wide*(1.2f-.6f*t),r=(.16f+.3f*(float)rng.NextDouble())*(1.1f-.6f*t);var at=p+run*t*length+across*side;
                    weed.Slot(0).Disc(new Vector3(at.x,ReedWater+.015f+s*.0015f,at.y),Vector3.up,r,7,ZoneKit.Shape.Dim(c,.85f+.3f*(float)rng.NextDouble()),0,ZoneKit.Shape.Dim(c,.7f));
                }
                if(rng.Next(5)==0)weed.Slot(0).Lump(new Vector3(p.x,ReedWater+.05f,p.y)+new Vector3(run.x,0,run.y)*length*.4f,new Vector3(.07f,.05f,.07f),k,.2f,new Color(.96f,.94f,.86f),new Color(.8f,.78f,.7f),6,4);
            }
            var lineView=kit.Built("Lines between the rafts",lines,Plank,Rope);lineView.transform.SetParent(root.transform,true);
            // The lanterns hang from the lines (and the skiffs' stern posts), so they belong to them: on their own each read as a prop floating three metres off the water.
            var hanging=new List<Transform>();foreach(Transform child in root.transform)if(child!=lineView.transform)hanging.Add(child);
            foreach(var child in hanging)child.SetParent(lineView.transform,true);
            var hungView=kit.Built("Hung on the lines",hung,Cloth,Weave);hungView.transform.SetParent(root.transform,true);hungView.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.TwoSided;
            var skiffView=kit.Built("Skiffs moored along the rims",skiffs,Plank,Weave,ReedBundle);skiffView.transform.SetParent(root.transform,true);
            var weedView=kit.Built("Weed on the tide",weed,Cloth);weedView.transform.SetParent(root.transform,true);weedView.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            Debug.Log($"DRIFTREED_TIDE_LIFE lines=5 skiffs={moored} lanterns={lanterns} weed={drifts}");
        }
        /// <summary>Driftreed's lamps: the generated pulse beacon on posts round the rafts, every one keeping the town's count; one in three with a real light, the rest a painted pool.</summary>
        static void Lamps(ZoneKit kit)
        {
            int n=0;
            foreach(var (x,z) in new[]{(-12.6f,16.4f),(12f,8f),(-3.2f,36.4f),(13.6f,36.4f),(-13.8f,56f),(-4f,56f),(4.4f,77f),(-16.8f,78.6f),(17.6f,78.4f),(-33.6f,52.4f),(-47.6f,37.6f),(33.6f,60.4f),(47.6f,53.2f),(41.2f,90.6f),(-14f,92f)})
            {
                var at=new Vector2(x,z);if(RaftLand(at)>-1.4f)throw new InvalidOperationException("Driftreed: a lamp at "+at+" is not on a raft");
                GameObject body;Renderer[] lamps;
                if(ZoneKit.Landed("PulseBeacon")){body=kit.Prop("PulseBeacon",new Vector3(x,0,z),3.4f,(n*67)%360);body.name="Pulse lamp";lamps=body.GetComponentsInChildren<Renderer>();}
                else{var foot=kit.On(x,z);var shape=new ZoneKit.Shape().Slot(0).Post(foot,3,.09f,.07f,6,new Color(.58f,.46f,.32f),new Color(.86f,.72f,.5f)).Slot(1).Lump(foot+Vector3.up*3.1f,new Vector3(.22f,.3f,.22f),3,.05f,Color.white,Color.white,8,5);
                    body=kit.Built("Pulse lamp "+n,shape,Plank,Lantern);ZoneKit.Block(body,foot+Vector3.up*1.4f,new Vector3(.3f,2.8f,.3f),0,"Lamp post");lamps=new[]{body.GetComponent<Renderer>()};}
                var rhythm=body.AddComponent<LampRhythm>();rhythm.count=LampRhythm.Count.TwoShortOneLong;rhythm.retuned=rhythm.count;rhythm.litFlag=rhythm.retunedFlag=rhythm.darkFlag="";rhythm.lamps=lamps;rhythm.offset=(n%4)*.4f;
                if(n%3==0){var light=kit.Lamp(new Vector3(x,3,z),7,1.2f,new Color(1,.78f,.46f));rhythm.lights=new[]{light};rhythm.intensity=1.2f;}
                n++;
            }
        }

        /// <summary>The floor's dressing, last: the ground kit's layers (trodden mat along the ways, reed litter, damp toward the rims), flat details, low cover, and the shade of the mangroves
        /// moving over it. Without the kit's textures the floor keeps its vertex tones and says so.</summary>
        static void ReedGround(ZoneKit kit,ZoneKit.Land land)
        {
            if(!File.Exists(GroundKit.Art+"ground-litter.png")||!File.Exists(GroundKit.Art+"ground-decals.png")||!File.Exists(GroundKit.Art+"ground-cover.png")||!File.Exists(GroundKit.Art+"ground-shade-dapple.png"))
            {Debug.LogWarning("WENDMIRE_GROUND_KIT_PENDING Driftreed: the ground kit's textures aren't painted yet, so the floor has its vertex tones only");return;}
            var ground=GroundKit.Open(kit.zone,land.FloorFilter,41,3.2f);
            foreach(var way in TownWays)ground.ClearLane(way,1.4f);
            float Inside(Vector2 p)=>-RaftLand(p);
            float Open(Vector2 p)=>ZoneKit.Smooth(RaftRimWide+.2f,RaftRimWide+1.2f,Inside(p));
            // Trodden mat along the ways (Driftreed's own layer, not sand: there is no sand on a raft), cut reed and leaf off them, wet toward every rim.
            ground.Layers(new Vector4(.05f,.34f,.4f,.28f),1.1f,new GroundKit.Layer("wend-reedpath",new Color(.96f,.88f,.66f,1),4),new GroundKit.Layer("litter",new Color(.78f,.74f,.46f,1),2.4f),new GroundKit.Layer("damp",new Color(.42f,.46f,.38f,.9f),3.2f));
            foreach(var way in TownWays)ground.PaintPath(0,way,1.5f,.8f,.55f,.7f,way.Length);
            foreach(var at in new[]{OnaAt,DovAt,QuillAt,MaruAtStage,NoticeAt,ReedPad})ground.PaintAround(0,at,2.4f,1,(int)at.x);
            ground.PaintPatches(1,4.5f,.46f,p=>Open(p)*ZoneKit.Smooth(1.4f,3.2f,FromTownWays(p)),3);
            ground.Paint(2,p=>(1-ZoneKit.Smooth(RaftRimWide+.2f,RaftRimWide+1.6f,Inside(p)+(ground.Noise(p,3.2f,5)-.5f)*1.2f))*ZoneKit.Smooth(.2f,RaftRimWide,Inside(p)));
            ground.Shade(p=>.1f*(1-ZoneKit.Smooth(RaftRimWide,RaftRimWide+1.4f,Inside(p)))-.05f*(1-ZoneKit.Smooth(.4f,1.6f,FromTownWays(p))));
            Color straw=new(.82f,.74f,.46f,1),stone=new(.6f,.58f,.5f,1);
            ground.Decals(new[]{"Litter","Lichen"},10,p=>Open(p)*ZoneKit.Smooth(1.1f,2.8f,FromTownWays(p)),.9f,2.2f,straw,false,.12f,1);
            ground.Decals(new[]{"Stain","SandDrift"},5,p=>Open(p),1.4f,3.2f,new Color(.4f,.42f,.32f,.7f),false,.1f,3);
            ground.Decals(new[]{"PebblesA","Slabs"},3,p=>Open(p)*ZoneKit.Smooth(.8f,2.4f,FromTownWays(p)),.6f,1.3f,stone,true,.1f,2);
            // Cut reed and fish scales trodden into the ways themselves, so a trodden way is a used one and not a bare band.
            ground.Decals(new[]{"Litter","Stain"},9,p=>Open(p)*(1-ZoneKit.Smooth(.8f,2.2f,FromTownWays(p))),.6f,1.4f,new Color(.6f,.54f,.36f,.75f),false,.12f,7);
            foreach(var way in TownWays)ground.Strip("Tracks",way,.2f,1f,new Color(.5f,.44f,.3f,.5f));
            ground.Cover(new[]{"Reeds","TuftA","TuftC"},7,p=>(1-ZoneKit.Smooth(RaftRimWide+.4f,RaftRimWide+1.8f,Inside(p)))*ZoneKit.Smooth(RaftRimWide,RaftRimWide+.3f,Inside(p))*(ReedKeepsOff(p)?0:1),.26f,.52f,new Color(.62f,.76f,.46f,1),3,.14f,3);
            ground.Cover(new[]{"Twigs","FungusA","Sprouts"},4,p=>Open(p)*ZoneKit.Smooth(2,3.4f,FromTownWays(p))*(ReedKeepsOff(p)?0:1),.14f,.3f,new Color(.9f,.86f,.68f,1),2,.14f,2);
            ground.Glows(3,p=>Open(p)*ZoneKit.Smooth(1.6f,3,FromTownWays(p))*(ReedKeepsOff(p)?0:1),.12f,.28f,new Color(.5f,1,.7f,.6f),5);
            ground.Look("River",null).LampPools(2.4f,new Color(1,.76f,.45f,.5f));
            // Slow cloud shadow off the mire's sky, wide and faint. Leaf dapple eleven metres across lay over the open water as well as the mats (it is the zone's one moving shade)
            // and drew the water between the rafts as camouflage; there is no canopy over the town to throw it.
            ground.MovingShade("cloud",70,new Vector2(.6f,.35f),.25f,.5f,.05f,default,new Vector2(14,30));
            ground.Finish();
        }
    }
}
