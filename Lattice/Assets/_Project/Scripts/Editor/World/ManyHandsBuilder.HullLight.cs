using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>
    /// The Hull of Many Hands at night: the city's own light (pools under its lamps, running lights along its edges, lit signs, glass lit from inside), the tall pieces that give
    /// its roofs sides and shadows (the liner's upper decks, the hauler's bridge, the spine's gantries, the dock's tower, the clock tower, stacked cabins), the wear on its broad decks,
    /// and each district's own kind of house. Everything here is drawn only and stays under the flight plane; nothing moves a contract coordinate (docs/maps/ManyHandsHull.md).
    /// </summary>
    public static partial class ManyHandsBuilder
    {
        internal static partial class Outside
        {
            // ---------------------------------------------------------------- light and wear lying on the decks
            /// <summary>Every pool of lamp light on a deck and every stain, gathered while the ship is built and drawn as two meshes at the end.</summary>
            static ZoneKit.Shape Pools,Stains;
            internal static Material Stain,GardenGlass,ArcadeGlass;
            /// <summary>What a pool adds to the deck under it: warm under the city's lamps, pale under work lights, cool under a dock's floods, green under the garden's grow lamps.
            /// None of them orange, red or purple, which on a floor mean danger (D217).</summary>
            static readonly Color PoolWarm=new(.46f,.36f,.22f),PoolPale=new(.36f,.36f,.32f),PoolCool=new(.2f,.3f,.38f),PoolGreen=new(.2f,.34f,.22f);
            static Color Lit(System.Random rng,float warm=.8f)=>rng.NextDouble()<warm?new Color(1,.82f,.55f)*(.9f+.2f*(float)rng.NextDouble()):new Color(.74f,.9f,1f)*(.9f+.2f*(float)rng.NextDouble());
            static readonly Color Unlit=new(.1f,.12f,.16f);

            static void LightMaterials()
            {
                // Wear: a soft blot with a ragged edge and a mottled middle, multiplied into what is under it (src zero, dst one-minus-src), so its vertex colour is how much it takes away.
                var stain=ZoneKit.Texture("ManyHands/hands-stain",256,(u,v)=>
                {
                    float du=Mathf.Min(u,1-u),dv=Mathf.Min(v,1-v);float d=Mathf.Sqrt(du*du+dv*dv)/.5f;float edge=.5f+.42f*ZoneKit.Cloud(u,v,5,31);
                    float t=Mathf.Clamp01((edge-d)/.22f);t=t*t*(3-2*t)*(.55f+.45f*ZoneKit.Cloud(u,v,24,8));return new Color(t,t,t,1);
                });
                Stain=ZoneKit.Toon("hands-deck-wear",stain,m=>{ZoneKit.Tinted(m);ZoneKit.With(m,"_GLOW_ON","_Glow");m.SetVector("_GlowParams",new Vector4(1,0,0,0));
                    m.SetFloat("_SrcBlend",(float)BlendMode.Zero);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcColor);m.SetFloat("_ZWrite",0);m.SetOverrideTag("RenderType","Transparent");
                    m.renderQueue=(int)RenderQueue.Transparent-35;m.SetFloat("_ReceiveShadows",0);});
                // The garden's vault and the arcade's promenade, lit from under: a little of their own light in the glass, so they glow among the dark roofs.
                Material LitGlass(string name,Color tint,Color glow,Color rim)=>ZoneKit.Toon(name,null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",tint);m.SetColor("_EmissionColor",glow);m.SetColor("_RimColor",rim);m.SetFloat("_RimPower",2.2f);
                    m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.SetOverrideTag("RenderType","Transparent");
                    m.renderQueue=(int)RenderQueue.Transparent-40;m.SetFloat("_ReceiveShadows",0);});
                GardenGlass=LitGlass("hands-garden-glass",new Color(.8f,1f,.86f,.34f),new Color(.16f,.3f,.2f),new Color(.6f,1f,.75f));
                ArcadeGlass=LitGlass("hands-arcade-glass",new Color(1f,.94f,.8f,.34f),new Color(.36f,.26f,.14f),new Color(1f,.86f,.6f));
            }
            /// <summary>Lamp light lying on a deck: a soft disc added to what is under it.</summary>
            static void PoolAt(Vector3 at,float radius,Color tone)=>Pools.Disc(at+Vector3.up*.07f,Vector3.up,radius,22,tone,0,tone,.5f/radius);
            /// <summary>Wear on a deck: a few overlapping blots, so no two stains have the same edge.</summary>
            static void StainAt(Vector3 at,float radius,Color depth,System.Random rng,int blots=3)
            {
                for(int k=0;k<blots;k++)
                {
                    float r=radius*(.45f+.55f*(float)rng.NextDouble());var off=new Vector3((float)rng.NextDouble()*2-1,0,(float)rng.NextDouble()*2-1)*radius*.45f;
                    Stains.Disc(at+off+Vector3.up*.045f,Vector3.up,r,16,depth,0,depth,.5f/r);
                }
            }
            /// <summary>Thruster scorch where ships set down: a dark middle and streaks out from it.</summary>
            static void ScorchAt(Vector3 at,float radius,System.Random rng)
            {
                StainAt(at,radius*.5f,new Color(.6f,.6f,.58f),rng,4);
                for(int k=0;k<16;k++)
                {
                    float a=k*Mathf.PI*2/16+(float)rng.NextDouble()*.3f;var dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));float reach=radius*(.75f+.35f*(float)rng.NextDouble());
                    for(int i=1;i<=4;i++){float t=i/5f;float r=radius*.15f*(1-t*.55f);Stains.Disc(at+dir*reach*t+Vector3.up*.045f,Vector3.up,r,12,new Color(.52f,.52f,.5f)*(1-t*.45f),0,null,.5f/r);}
                }
            }

            // ---------------------------------------------------------------- lettering standing up: lit signs
            /// <summary>The capitals and figures the lettering needs beyond the dock's name, in the same box (one wide, 1.4 tall).</summary>
            static void MoreGlyphs()
            {
                void Add(char c,params Vector2[][] lines){if(!Glyphs.ContainsKey(c))Glyphs[c]=lines;}
                Vector2[] L(params float[] xy){var p=new Vector2[xy.Length/2];for(int i=0;i<p.Length;i++)p[i]=new Vector2(xy[i*2],xy[i*2+1]);return p;}
                Add('R',L(0,0,0,1.4f,.7f,1.4f,.95f,1.25f,1,1.05f,.95f,.85f,.7f,.7f,0,.7f),L(.55f,.7f,1,0));
                Add('E',L(1,1.4f,0,1.4f,0,0,1,0),L(0,.7f,.75f,.7f));
                Add('G',L(1,1.16f,.8f,1.38f,.2f,1.38f,0,1.1f,0,.3f,.2f,.02f,.8f,.02f,1,.3f,1,.62f,.55f,.62f));
                Add('O',L(.2f,0,.8f,0,1,.3f,1,1.1f,.8f,1.4f,.2f,1.4f,0,1.1f,0,.3f,.2f,0));
                Add('P',L(0,0,0,1.4f,.7f,1.4f,.95f,1.25f,1,1.05f,.95f,.85f,.7f,.7f,0,.7f));
                Add('B',L(0,0,0,1.4f,.7f,1.4f,.92f,1.26f,.95f,1.06f,.82f,.78f,.6f,.72f,0,.72f),L(.6f,.72f,.9f,.62f,1,.4f,.95f,.16f,.75f,0,0,0));
                Add('T',L(0,1.4f,1,1.4f),L(.5f,1.4f,.5f,0));
                Add('I',L(.5f,0,.5f,1.4f));
                Add('F',L(1,1.4f,0,1.4f,0,0),L(0,.72f,.7f,.72f));
                Add('W',L(0,1.4f,.25f,0,.5f,.8f,.75f,0,1,1.4f));
                Add('V',L(0,1.4f,.5f,0,1,1.4f));
                Add('0',L(.2f,0,.8f,0,1,.3f,1,1.1f,.8f,1.4f,.2f,1.4f,0,1.1f,0,.3f,.2f,0));
                Add('1',L(.25f,1.1f,.55f,1.4f,.55f,0),L(.2f,0,.9f,0));
                Add('2',L(0,1.1f,.2f,1.38f,.8f,1.38f,1,1.1f,1,.85f,0,0,1,0));
                Add('3',L(0,1.25f,.25f,1.4f,.8f,1.4f,1,1.2f,1,.95f,.75f,.72f,.35f,.72f),L(.75f,.72f,1,.5f,1,.2f,.8f,0,.2f,0,0,.15f));
                Add('4',L(.75f,0,.75f,1.4f,0,.45f,1,.45f));
                Add('5',L(1,1.4f,.1f,1.4f,0,.75f,.7f,.8f,1,.55f,1,.2f,.8f,0,.2f,0,0,.15f));
                Add('6',L(.9f,1.3f,.6f,1.4f,.25f,1.35f,0,1,0,.3f,.2f,0,.8f,0,1,.25f,1,.5f,.8f,.75f,.2f,.75f,0,.55f));
                Add('7',L(0,1.4f,1,1.4f,.35f,0));
                Add('8',L(.5f,.75f,.15f,.9f,.1f,1.2f,.3f,1.4f,.7f,1.4f,.9f,1.2f,.85f,.9f,.5f,.75f,.1f,.55f,0,.25f,.2f,0,.8f,0,1,.25f,.9f,.55f,.5f,.75f));
                Add('9',L(.1f,.1f,.4f,0,.75f,.05f,1,.4f,1,1.1f,.8f,1.4f,.2f,1.4f,0,1.15f,0,.9f,.2f,.65f,.8f,.65f,1,.85f));
            }
            static float WordWide(string word,float tall)=>word.Length*tall/1.4f*1.36f-tall/1.4f*.36f;
            /// <summary>A word standing up on a south face (seen from the fixed camera), letters <paramref name="tall"/> high from <paramref name="at"/> (its foot's left end).</summary>
            static void LettersUp(ZoneKit.Shape s,int slot,string word,Vector3 at,float tall,Color tone)
            {
                float unit=tall/1.4f,stroke=tall*.14f,advance=unit*1.36f;
                foreach(char ch in word)
                {
                    if(Glyphs.TryGetValue(ch,out var lines))foreach(var line in lines)for(int i=1;i<line.Length;i++)
                    {
                        Vector3 a=at+new Vector3(line[i-1].x*unit,line[i-1].y*unit,0),b=at+new Vector3(line[i].x*unit,line[i].y*unit,0);var dir=(b-a).normalized;var side=Vector3.Cross(Vector3.back,dir)*stroke*.5f;
                        a-=dir*stroke*.4f;b+=dir*stroke*.4f;s.Slot(slot).Quad(a-side,a+side,b+side,b-side,Vector3.back,tone,.5f);
                    }
                    at+=Vector3.right*advance;
                }
            }
            /// <summary>A lit sign standing on a roof facing the camera: a dark board on two legs, its word lit, a lit rule under the word. Lays a pool of its own light in front.</summary>
            static void SignBoard(ZoneKit.Shape s,string word,Vector3 foot,float tall,Color glow,Color board,float legs=.2f)
            {
                float wide=WordWide(word,tall)+tall*.9f,high=tall*1.7f;var centre=foot+Vector3.up*(legs+high*.5f);
                foreach(float side in new[]{-1f,1f})s.Slot(1).Box(foot+new Vector3(side*(wide*.5f-.3f),(legs+high*.5f)*.5f,.1f),new Vector3(.14f,legs+high*.5f,.14f),Quaternion.identity,new Color(.2f,.2f,.22f));
                s.Slot(1).Box(centre,new Vector3(wide,high,.16f),Quaternion.identity,board);
                LettersUp(s,2,word,centre+new Vector3(-WordWide(word,tall)*.5f,-tall*.42f,-.1f),tall,glow);
                s.Slot(2).Box(centre+new Vector3(0,-high*.5f+.12f,-.1f),new Vector3(wide-.3f,.06f,.02f),Quaternion.identity,glow*.8f);
                PoolAt(new Vector3(foot.x,foot.y,foot.z-1.2f),Mathf.Max(2,wide*.5f),glow*.22f);
            }

            // ---------------------------------------------------------------- running lights
            /// <summary>Lights along an edge of the ship's outside, a lamp every five metres, white with every fourth teal: they draw the city's outline against space.</summary>
            static void Running(ZoneKit.Shape s,Vector2 a,Vector2 b,float y,Vector3 outward,int seed,float every=5)
            {
                var from=new Vector3(a.x,y,a.y);var to=new Vector3(b.x,y,b.y);float length=Vector3.Distance(from,to);int n=Mathf.Max(1,Mathf.RoundToInt(length/every));
                for(int i=0;i<=n;i++)
                {
                    var p=Vector3.Lerp(from,to,i/(float)n)+outward*.32f;bool teal=(i+seed)%4==0;var c=teal?new Color(.5f,.95f,1f):new Color(1f,.95f,.84f);
                    s.Slot(2).Lump(p,new Vector3(.2f,.15f,.2f),i+seed,.05f,c,c,6,3);
                }
            }

            // ---------------------------------------------------------------- the liner's upper decks
            /// <summary>The liner's upper decks: two tiers stepping up either side of its promenade, as far out as the joins' drums let them (each keeps 11 m from a drum's middle and
            /// 4.5 from a cover piece), so the old ship stands up out of its own roof.</summary>
            internal static readonly Rect[] UpperDecks={Rect.MinMaxRect(-37.5f,-20,-18,-10),Rect.MinMaxRect(18,-20,39,-10),Rect.MinMaxRect(49,-20,59,-10),Rect.MinMaxRect(-37.5f,10,-18,20),Rect.MinMaxRect(18,10,37.5f,20)};
            const float TierOne=2.6f,TierTwo=2.2f;
            static Rect Grown(Rect r,float by)=>Rect.MinMaxRect(r.xMin-by,r.yMin-by,r.xMax+by,r.yMax+by);
            static void LinerDecks(ZoneKit kit,Transform root)
            {
                var s=new ZoneKit.Shape();var rng=new System.Random(701);float y=LinerY;var m=LinerMake;Color cream=m.paint,teal=m.stripe,deck=new(.7f,.62f,.5f),roofTone=new(.8f,.78f,.72f);
                string[] signs={"ARCADE","TEA ROOM",null,null,null};
                for(int b=0;b<UpperDecks.Length;b++)
                {
                    var r=UpperDecks[b];float y1=y+TierOne,y2=y1+TierTwo;bool two=r.width>15;
                    // Tier one: the liner's cabin deck, portholes along its south face, a teal band under its edge, doors with lamps onto the deck.
                    s.Slot(0).Box(new Vector3(r.center.x,y+TierOne*.5f,r.center.y),new Vector3(r.width,TierOne,r.height),Quaternion.identity,cream,deck,1/4f);
                    s.Slot(1).Box(new Vector3(r.center.x,y1-.32f,r.yMin-.03f),new Vector3(r.width+.06f,.42f,.06f),Quaternion.identity,teal).Beam(new Vector3(r.xMin,y1+.1f,r.yMin),new Vector3(r.xMax,y1+.1f,r.yMin),.3f,.2f,m.trim);
                    for(float x=r.xMin+1.1f;x<r.xMax-.8f;x+=1.9f)
                    {
                        var c=new Vector3(x,y+1.3f,r.yMin-.03f);bool lit=rng.Next(6)>0;
                        s.Slot(1).Disc(c,Vector3.back,.42f,12,m.trim);s.Slot(lit?2:1).Disc(c+Vector3.back*.01f,Vector3.back,.33f,12,lit?Lit(rng,.85f):Unlit);
                    }
                    for(float x=r.xMin+3.5f;x<r.xMax-2;x+=6.5f)
                    {
                        var door=new Vector3(x,y+1,r.yMin-.04f);Pane(s,1,door,Vector3.right,1,2,Vector3.back,new Color(.24f,.3f,.32f));
                        s.Slot(2).Lump(door+new Vector3(0,1.35f,-.15f),new Vector3(.2f,.14f,.14f),(int)x,.05f,new Color(1,.86f,.6f),new Color(1,.8f,.5f),6,3);PoolAt(new Vector3(x,y,r.yMin-1.8f),2.6f,PoolWarm);
                    }
                    // A rail along tier one's edge, with deck lamps along the walk in front of tier two, each with its pool.
                    for(float x=r.xMin+.4f;x<=r.xMax;x+=2.4f)s.Slot(1).Post(new Vector3(x,y1,r.yMin+.25f),.9f,.04f,.04f,4,m.trim,m.trim);
                    s.Slot(1).Beam(new Vector3(r.xMin+.4f,y1+.9f,r.yMin+.25f),new Vector3(r.xMax,y1+.9f,r.yMin+.25f),.06f,.06f,m.trim);
                    if(!two)
                    {
                        // The short block by the galley: one tier, its roof a radio room's (a mast, a dish, lamps at its corners).
                        var top=new Vector3(r.center.x,y1,r.center.y);s.Slot(1).Box(top+new Vector3(-1,1,1),new Vector3(3.6f,2,3),Quaternion.identity,roofTone,null,.5f);
                        for(int i=0;i<2;i++){var wc=top+new Vector3(-2+i*1.8f,1.2f,-.53f);Pane(s,2,wc,Vector3.right,1.2f,.8f,Vector3.back,Lit(rng,.3f));}
                        // Its frame, so it reads as the liner's radio room and not a pale box with two windows (s29/046, 052): teal corner posts and cornice in the liner's
                        // paint, a dark plinth, its door with a lamp on the east end, louvres under the windows.
                        var room=top+new Vector3(-1,1,1);
                        foreach(float sx in new[]{-1f,1f})foreach(float sz in new[]{-1f,1f})s.Slot(1).Box(room+new Vector3(sx*1.78f,0,sz*1.48f),new Vector3(.16f,2,.16f),Quaternion.identity,teal);
                        s.Slot(1).Box(room+Vector3.up*1.02f,new Vector3(3.8f,.16f,3.2f),Quaternion.identity,teal).Box(room+Vector3.down*.86f,new Vector3(3.66f,.28f,3.06f),Quaternion.identity,m.trim);
                        for(int k=0;k<4;k++)s.Slot(1).Box(room+new Vector3(-1.1f+k*.5f,-.38f,-1.52f),new Vector3(.36f,.05f,.04f),Quaternion.identity,m.trim);
                        Pane(s,1,room+new Vector3(1.81f,-.1f,.2f),Vector3.forward,.8f,1.7f,Vector3.right,new Color(.24f,.3f,.32f));
                        s.Slot(2).Lump(room+new Vector3(1.95f,.9f,.2f),new Vector3(.1f,.1f,.12f),b+3,.05f,new Color(1,.86f,.6f),new Color(1,.8f,.5f),6,3);
                        s.Slot(1).Post(top+new Vector3(2.5f,0,2),4.4f,.08f,.05f,5,m.trim,m.trim).Disc(top+new Vector3(3,2.2f,1.4f),new Vector3(.3f,.5f,-.8f),.9f,14,new Color(.86f,.86f,.82f));
                        s.Slot(2).Lump(top+new Vector3(2.5f,4.5f,2),Vector3.one*.13f,b,.05f,new Color(.5f,.95f,1f),new Color(.5f,.95f,1f),6,3);
                        foreach(var (dx,dz) in new[]{(r.xMin+.5f,r.yMin+.5f),(r.xMax-.5f,r.yMin+.5f)}){s.Slot(2).Lump(new Vector3(dx,y1+.35f,dz),Vector3.one*.16f,(int)dx,.05f,new Color(1,.9f,.7f),new Color(1,.9f,.7f),6,3);PoolAt(new Vector3(dx,y1,dz+.4f),1.6f,PoolWarm);}
                        continue;
                    }
                    for(float x=r.xMin+2;x<r.xMax-1;x+=3.4f){s.Slot(1).Post(new Vector3(x,y1,r.yMin+1.3f),.75f,.05f,.04f,5,m.trim,m.trim);s.Slot(2).Lump(new Vector3(x,y1+.85f,r.yMin+1.3f),new Vector3(.15f,.12f,.15f),(int)x,.05f,new Color(1,.86f,.6f),new Color(1,.8f,.5f),6,3);PoolAt(new Vector3(x,y1,r.yMin+1.2f),1.7f,PoolWarm);}
                    // Tier two, set back: the lounges, a long row of tall windows lit warm, a teal cornice; deck chairs on the walk in front.
                    var r2=Rect.MinMaxRect(r.xMin+1.5f,r.yMin+2.4f,r.xMax-1.5f,r.yMax-.6f);
                    s.Slot(0).Box(new Vector3(r2.center.x,y1+TierTwo*.5f,r2.center.y),new Vector3(r2.width,TierTwo,r2.height),Quaternion.identity,ZoneKit.Shape.Dim(cream,1.04f),roofTone,1/4f);
                    s.Slot(1).Box(new Vector3(r2.center.x,y2+.06f,r2.center.y),new Vector3(r2.width+.4f,.14f,r2.height+.4f),Quaternion.identity,teal);
                    for(float x=r2.xMin+1.4f;x<r2.xMax-1;x+=2.3f)
                    {
                        var c=new Vector3(x,y1+1.15f,r2.yMin-.03f);Pane(s,1,c,Vector3.right,1.95f,1.45f,Vector3.back,m.trim);bool lit=rng.Next(8)>0;
                        Pane(s,lit?2:1,c+Vector3.back*.01f,Vector3.right,1.75f,1.25f,Vector3.back,lit?Lit(rng,.9f):Unlit);
                    }
                    for(float x=r2.xMin+1;x<r2.xMax-1;x+=1.6f)if(rng.Next(3)>0){var chair=new Vector3(x,y1+.25f,r.yMin+.9f);s.Slot(1).Box(chair,new Vector3(.6f,.12f,1.3f),Quaternion.Euler(-8,0,0),rng.Next(2)==0?teal:new Color(.92f,.9f,.84f));}
                    // On tier two's roof: skylights over the lounges, vents, lamps at its corners, and on one a wheelhouse, on two a lit sign.
                    for(float x=r2.xMin+2;x<r2.xMax-2;x+=4.2f){var sk=new Vector3(x,y2+.15f,r2.center.y+.6f);s.Slot(1).Box(sk,new Vector3(2.4f,.16f,1.6f),Quaternion.identity,m.trim);Flat(s,2,sk+Vector3.up*.09f,2.1f,1.3f,0,Lit(rng,.9f)*.85f);}
                    foreach(var (dx,dz) in new[]{(r2.xMin+.4f,r2.yMin+.4f),(r2.xMax-.4f,r2.yMin+.4f)}){s.Slot(2).Lump(new Vector3(dx,y2+.4f,dz),Vector3.one*.16f,(int)dx,.05f,new Color(1,.92f,.74f),new Color(1,.9f,.7f),6,3);PoolAt(new Vector3(dx,y2,dz+.5f),1.5f,PoolWarm);}
                    if(signs[b] is string word)SignBoard(s,word,new Vector3(r2.center.x,y2+.13f,r2.yMin+.9f),.85f,b==0?new Color(.5f,1f,.95f):new Color(1f,.9f,.68f),new Color(.12f,.14f,.16f),.15f);
                    else if(b==4)
                    {
                        // The liner's old wheelhouse: a glazed cab on tier two's roof, its windows lit cool by the instruments, a short mast with a teal light.
                        var wh=new Vector3(r2.xMax-4,y2,r2.yMin+2.2f);s.Slot(0).Box(wh+Vector3.up*.65f,new Vector3(5.2f,1.3f,2.6f),Quaternion.identity,ZoneKit.Shape.Dim(cream,1.06f),roofTone,1/4f);
                        for(int i=0;i<4;i++)Pane(s,2,wh+new Vector3(-1.85f+i*1.23f,.78f,-1.33f),Vector3.right,1.05f,.6f,Vector3.back,new Color(.7f,.9f,1f));
                        s.Slot(1).Box(wh+Vector3.up*1.36f,new Vector3(5.6f,.12f,3),Quaternion.identity,teal);s.Slot(2).Lump(wh+new Vector3(1.8f,1.62f,.6f),Vector3.one*.14f,7,.05f,new Color(.5f,.95f,1f),new Color(.5f,.95f,1f),6,3);
                    }
                    else{var vent=new Vector3(r2.xMax-3,y2,r2.center.y);s.Slot(1).Tube(new[]{vent,vent+Vector3.up*1.1f,vent+new Vector3(0,1.45f,-.35f)},new[]{.28f,.26f,.38f},10,m.trim,teal,1,.5f);}
                }
                var built=kit.Built("Liner upper decks",s.Slot(2),Liner,Trim,Glow);built.transform.SetParent(root,true);
            }

            // ---------------------------------------------------------------- the hauler's bridge and the spine's gantries
            /// <summary>The hauler's bridge at the spine's stern, the tallest thing aft: its accommodation block the spine's width, the bridge set back on it with its wheelhouse windows
            /// lit cool, wing cabs at its ends.</summary>
            internal static readonly Rect BridgeAt=Rect.MinMaxRect(-14.5f,-142,14.5f,-129);
            /// <summary>Where the spine's two gantries cross it.</summary>
            internal static readonly float[] GantryZ={-100,92};
            static void BridgeTower(ZoneKit.Shape s,System.Random rng)
            {
                float y=SpineY;var r=BridgeAt;Color white=new(.86f,.84f,.78f),roofTone=new(.56f,.56f,.56f),oxide=SpineMake.paint,trim=SpineMake.trim;
                s.Slot(0).Box(new Vector3(0,y+1.3f,r.center.y),new Vector3(r.width,2.6f,r.height),Quaternion.identity,white,roofTone,1/6f);
                s.Slot(1).Box(new Vector3(0,y+.35f,r.yMin-.03f),new Vector3(r.width+.06f,.7f,.06f),Quaternion.identity,oxide).Beam(new Vector3(r.xMin,y+2.66f,r.yMin),new Vector3(r.xMax,y+2.66f,r.yMin),.35f,.14f,trim);
                for(float x=r.xMin+1.4f;x<r.xMax-1;x+=2.4f)
                {
                    if(Mathf.Abs(x)<2.2f)continue;var c=new Vector3(x,y+1.5f,r.yMin-.03f);bool lit=rng.Next(5)>0;
                    Pane(s,1,c,Vector3.right,1.5f,1.1f,Vector3.back,trim);Pane(s,lit?2:1,c+Vector3.back*.01f,Vector3.right,1.3f,.9f,Vector3.back,lit?Lit(rng,.85f):Unlit);
                }
                // The door the ridge walk runs into, a lamp over it and its light on the walk.
                Pane(s,1,new Vector3(0,y+1.2f,r.yMin-.04f),Vector3.right,1.8f,2.1f,Vector3.back,new Color(.2f,.22f,.24f));
                s.Slot(2).Lump(new Vector3(0,y+2.45f,r.yMin-.2f),new Vector3(.26f,.14f,.16f),3,.05f,new Color(1,.88f,.62f),new Color(1,.8f,.5f),6,3);PoolAt(new Vector3(0,y+.36f,r.yMin-2.4f),3.4f,PoolWarm);
                // The bridge: set back, the full band of its south face windows, a cornice; wing cabs at the hull's edges; lamps at its corners.
                var r2=Rect.MinMaxRect(-10,r.yMin+2.6f,10,r.yMax-1.2f);float y1=y+2.6f;
                s.Slot(0).Box(new Vector3(0,y1+1.12f,r2.center.y),new Vector3(r2.width,2.24f,r2.height),Quaternion.identity,white,roofTone,1/6f);
                s.Slot(1).Box(new Vector3(0,y1+2.28f,r2.center.y),new Vector3(r2.width+.4f,.12f,r2.height+.4f),Quaternion.identity,trim);
                for(float x=r2.xMin+.9f;x<r2.xMax-.5f;x+=1.55f){var c=new Vector3(x,y1+1.35f,r2.yMin-.03f);Pane(s,1,c,Vector3.right,1.45f,1.05f,Vector3.back,trim);Pane(s,2,c+Vector3.back*.01f,Vector3.right,1.3f,.9f,Vector3.back,rng.Next(5)>0?new Color(.68f,.88f,1f):new Color(1,.84f,.6f));}
                foreach(float side in new[]{-1f,1f})
                {
                    var cab=new Vector3(side*12.6f,y1,r.yMin+2.4f);s.Slot(0).Box(cab+Vector3.up*.75f,new Vector3(3.2f,1.5f,2.6f),Quaternion.identity,white,roofTone,1/6f);
                    Pane(s,2,cab+new Vector3(0,.85f,-1.33f),Vector3.right,2.6f,.7f,Vector3.back,new Color(.68f,.88f,1f));
                    s.Slot(2).Lump(cab+new Vector3(side*1.5f,1.7f,-1.1f),Vector3.one*.17f,side>0?1:2,.05f,new Color(.5f,.95f,1f),new Color(.5f,.95f,1f),6,3);
                    for(float x=side*4;Mathf.Abs(x)<11;x+=side*3.4f){s.Slot(1).Post(new Vector3(x,y1,r.yMin+.4f),.7f,.05f,.04f,5,trim,trim);s.Slot(2).Lump(new Vector3(x,y1+.8f,r.yMin+.4f),new Vector3(.14f,.11f,.14f),(int)x,.05f,new Color(1,.88f,.62f),new Color(1,.8f,.5f),6,3);PoolAt(new Vector3(x,y1,r.yMin+.9f),1.6f,PoolWarm);}
                }
                // On the bridge's roof, as high as the room under the plane lets it: the radar's bar on its pedestal, the hauler's horn.
                s.Slot(1).Post(new Vector3(7,y1+2.34f,r2.center.y+2.4f),.15f,.3f,.25f,8,trim,trim).Box(new Vector3(7,y1+2.52f,r2.center.y+2.4f),new Vector3(4.2f,.1f,.4f),Quaternion.Euler(0,24,0),new Color(.86f,.86f,.82f));
                s.Slot(1).Box(new Vector3(-7,y1+2.44f,r2.center.y+2.6f),new Vector3(1.6f,.16f,.9f),Quaternion.identity,new Color(.3f,.32f,.34f));
                // The bridge's roof sits at the top of the room under the plane, so it is dressed flat: the hauler's oxide band along its front, a row of skylights over the
                // wheelhouse lit cool by its instruments, two hatches, lamps let into its corners, and the wear of years.
                var rc=new Vector3(0,y1+2.35f,r2.center.y);
                Flat(s,1,rc+new Vector3(0,.01f,-3.6f),r2.width-.8f,.9f,0,oxide);
                for(int i=0;i<5;i++){Flat(s,1,rc+new Vector3(-8+i*4,.012f,-1.2f),2.7f,1.7f,0,trim);Flat(s,2,rc+new Vector3(-8+i*4,.02f,-1.2f),2.3f,1.3f,0,new Color(.56f,.74f,.84f));}
                foreach(float hx in new[]{-3.5f,2.5f}){Flat(s,1,rc+new Vector3(hx,.012f,2.4f),2.2f,2.2f,0,trim);Flat(s,1,rc+new Vector3(hx,.018f,2.4f),1.8f,1.8f,0,ZoneKit.Shape.Dim(roofTone,.8f));}
                foreach(var (cx,cz) in new[]{(r2.xMin+.5f,r2.yMin+.5f),(r2.xMax-.5f,r2.yMin+.5f),(r2.xMin+.5f,r2.yMax-.5f),(r2.xMax-.5f,r2.yMax-.5f)})s.Slot(2).Disc(new Vector3(cx,rc.y+.02f,cz),Vector3.up,.22f,10,new Color(1,.9f,.7f));
                for(int i=0;i<5;i++)StainAt(rc+new Vector3((float)rng.NextDouble()*18-9,0,(float)rng.NextDouble()*7-3.5f),.8f+(float)rng.NextDouble()*1.6f,new Color(.3f,.31f,.33f),rng,2);
            }
            /// <summary>A gantry crane across the spine: legs on rails at the hauler's edges, two girders over the stacks (under the plane), its cab, a trolley with a spreader hanging,
            /// floodlights under the girders laying their light on the stacks and the walk.</summary>
            static void SpineGantry(ZoneKit.Shape s,float z,System.Random rng)
            {
                float y=SpineY,tall=3.7f;Color steel=new(.52f,.55f,.56f),accent=new(.22f,.48f,.52f),dark=new(.22f,.23f,.25f);
                foreach(float side in new[]{-1f,1f})
                {
                    var f=new Vector3(side*15,y,z);
                    s.Slot(1).Beam(f+new Vector3(0,.5f,-6),f+new Vector3(0,.5f,6),.5f,.3f,dark).Box(f+Vector3.up*.45f,new Vector3(1.2f,.7f,5.2f),Quaternion.identity,accent);
                    s.Slot(1).Beam(f+new Vector3(0,.8f,-2.2f),f+new Vector3(0,tall,-.9f),.5f,.5f,steel).Beam(f+new Vector3(0,.8f,2.2f),f+new Vector3(0,tall,.9f),.5f,.5f,steel).Beam(f+new Vector3(0,2.4f,-1.6f),f+new Vector3(0,2.4f,1.6f),.25f,.25f,steel);
                }
                // Two trussed girders: top and bottom chords with a zigzag web between them on the face the camera sees, so the bridge reads as steelwork and not a plank.
                foreach(float dz in new[]{-1.05f,1.05f})
                {
                    s.Slot(1).Beam(new Vector3(-16,y+tall+.3f,z+dz),new Vector3(16,y+tall+.3f,z+dz),.4f,.24f,steel,ZoneKit.Shape.Dim(steel,1.15f)).Beam(new Vector3(-16,y+tall-.45f,z+dz),new Vector3(16,y+tall-.45f,z+dz),.4f,.24f,steel);
                    for(float x=-16;x<15.9f;x+=1.6f)s.Slot(1).Beam(new Vector3(x,y+tall-.45f,z+dz),new Vector3(x+1.6f,y+tall+.3f,z+dz)*1+Vector3.zero,.14f,.14f,ZoneKit.Shape.Dim(steel,.85f));
                }
                for(float x=-15;x<16;x+=3)s.Slot(1).Beam(new Vector3(x,y+tall+.3f,z-1.05f),new Vector3(x,y+tall+.3f,z+1.05f),.14f,.14f,steel);
                s.Slot(1).Box(new Vector3(-12.2f,y+tall-1.4f,z),new Vector3(2.2f,1.4f,2),Quaternion.identity,accent,steel);Pane(s,2,new Vector3(-12.2f,y+tall-1.35f,z-1.02f),Vector3.right,1.6f,.66f,Vector3.back,new Color(1,.84f,.58f));
                float t=(float)rng.NextDouble()*12-6;var trolley=new Vector3(t,y+tall-.8f,z);
                s.Slot(1).Box(trolley,new Vector3(2.4f,.7f,2.6f),Quaternion.identity,dark,steel);
                foreach(float dx in new[]{-.8f,.8f})s.Slot(1).Tube(new[]{trolley+new Vector3(dx,0,0),trolley+new Vector3(dx,-1.4f,0)},new[]{.04f,.04f},4,dark,dark,1,1,false);
                s.Slot(1).Box(trolley+Vector3.down*1.55f,new Vector3(2.6f,.25f,2.4f),Quaternion.identity,accent);
                for(int k=-2;k<=2;k++)
                {
                    var lamp=new Vector3(k*6.2f,y+tall-.62f,z-1.05f);s.Slot(2).Box(lamp,new Vector3(.7f,.14f,.4f),Quaternion.identity,new Color(.86f,.94f,1f));
                    PoolAt(new Vector3(k*6.2f,k==0?y+.36f:y+.6f,z-2.2f),3.8f,PoolCool*1.25f);
                }
                s.Slot(2).Lump(new Vector3(-16.2f,y+tall+.6f,z),Vector3.one*.18f,3,.05f,new Color(.5f,.95f,1f),new Color(.5f,.95f,1f),6,3).Lump(new Vector3(16.2f,y+tall+.6f,z),Vector3.one*.18f,4,.05f,new Color(.5f,.95f,1f),new Color(.5f,.95f,1f),6,3);
            }
            /// <summary>The arcade's clock seen from outside, as a tower: a plinth with its door and lit windows, the shaft with the old clock face, a cornice, a lantern stage lit inside.</summary>
            static void ClockTower(ZoneKit.Shape s,float y)
            {
                var foot=new Vector3(7,y,0);Color cream=new(.9f,.86f,.76f),teal=new(.2f,.56f,.58f),dark=new(.16f,.15f,.14f);
                s.Slot(0).Box(foot+Vector3.up*.6f,new Vector3(5.4f,1.2f,5.4f),Quaternion.identity,ZoneKit.Shape.Dim(cream,.92f),ZoneKit.Shape.Dim(cream,.86f),.25f).Slot(1).Box(foot+Vector3.up*1.24f,new Vector3(5.6f,.12f,5.6f),Quaternion.identity,teal);
                Pane(s,1,foot+new Vector3(0,.55f,-2.73f),Vector3.right,1,1.05f,Vector3.back,new Color(.24f,.3f,.32f));
                foreach(float dx in new[]{-1.7f,1.7f}){Pane(s,1,foot+new Vector3(dx,.62f,-2.73f),Vector3.right,.9f,.8f,Vector3.back,teal);Pane(s,2,foot+new Vector3(dx,.62f,-2.74f),Vector3.right,.72f,.62f,Vector3.back,new Color(1,.84f,.56f));}
                s.Slot(0).Box(foot+Vector3.up*2.55f,new Vector3(4.2f,2.7f,4.2f),Quaternion.identity,cream,ZoneKit.Shape.Dim(cream,1.05f),.25f).Slot(1).Box(foot+Vector3.up*4f,new Vector3(4.8f,.22f,4.8f),Quaternion.identity,teal);
                var face=foot+new Vector3(0,2.55f,-2.13f);s.Slot(0).Disc(face,Vector3.back,1.32f,24,teal).Slot(2).Disc(face+Vector3.back*.02f,Vector3.back,1.12f,24,new Color(1,.93f,.76f));
                for(int k=0;k<12;k++){var dir=Quaternion.AngleAxis(k*30,Vector3.forward)*Vector3.up;s.Slot(1).Box(face+dir*.92f+Vector3.back*.05f,new Vector3(.08f,k%3==0?.26f:.13f,.02f),Quaternion.LookRotation(Vector3.forward,dir),dark);}
                s.Slot(1).Box(face+Quaternion.AngleAxis(-60,Vector3.forward)*Vector3.up*.32f+Vector3.back*.07f,new Vector3(.1f,.68f,.02f),Quaternion.LookRotation(Vector3.forward,Quaternion.AngleAxis(-60,Vector3.forward)*Vector3.up),dark)
                    .Box(face+Quaternion.AngleAxis(25,Vector3.forward)*Vector3.up*.45f+Vector3.back*.08f,new Vector3(.07f,.94f,.02f),Quaternion.LookRotation(Vector3.forward,Quaternion.AngleAxis(25,Vector3.forward)*Vector3.up),dark);
                // The lantern stage: four posts round a lit core, a thin teal cap; the top stays under the plane.
                foreach(var (dx,dz) in new[]{(-.95f,-.95f),(.95f,-.95f),(-.95f,.95f),(.95f,.95f)})s.Slot(1).Box(foot+new Vector3(dx,4.5f,dz),new Vector3(.18f,.7f,.18f),Quaternion.identity,cream);
                s.Slot(2).Box(foot+Vector3.up*4.48f,new Vector3(1.5f,.62f,1.5f),Quaternion.identity,new Color(1,.86f,.6f));
                {var apex=foot+Vector3.up*5.12f;float e=1.15f,b=4.85f;Vector3 C(float x,float z)=>foot+new Vector3(x,b,z);
                    s.Slot(1).Triangle(C(-e,-e),C(e,-e),apex,Vector3.back+Vector3.up,teal).Triangle(C(e,-e),C(e,e),apex,Vector3.right+Vector3.up,ZoneKit.Shape.Dim(teal,.85f)).Triangle(C(e,e),C(-e,e),apex,Vector3.forward+Vector3.up,ZoneKit.Shape.Dim(teal,.75f)).Triangle(C(-e,e),C(-e,-e),apex,Vector3.left+Vector3.up,ZoneKit.Shape.Dim(teal,.9f));}
                PoolAt(foot+Vector3.up*.36f+Vector3.back*3.4f,3.6f,PoolWarm);PoolAt(foot+Vector3.up*4.05f,2.4f,PoolWarm*.7f);
            }

            // ---------------------------------------------------------------- the dock: its tower, its floods, its wear
            static readonly Vector2 DockTowerAt=new(-22,-108);
            static readonly Vector2[] DockFloods={new(-59,-119),new(-18.6f,-121)};
            static void DockDressing(ZoneKit kit,Transform root)
            {
                var s=new ZoneKit.Shape();var rng=new System.Random(811);float y=DockY;Color grey=DockMake.paint,teal=DockMake.stripe,trim=DockMake.trim,white=new(.9f,.9f,.86f);
                // The dock's tower: a shaft with two rows of lit windows and DOCK down its face, a glazed cab on top lit cool, its roof and a short aerial under the plane.
                var t=P(DockTowerAt,y);
                s.Slot(0).Box(t+Vector3.up*2.1f,new Vector3(4.2f,4.2f,4.2f),Quaternion.identity,ZoneKit.Shape.Dim(grey,.92f),grey,1/6f).Slot(1).Box(t+Vector3.up*3.9f,new Vector3(4.3f,.36f,4.3f),Quaternion.identity,teal);
                // DOCK runs down the face's west half, a letter over the next, the windows in the east half. Across the face's foot the word ended under the objective panel from the
                // arrival (its K at 1360 px of 1920, the panel from 1340: s44/097); this column stands at 1268 to 1305 px there. It stops under 2.9 m: the cab overhangs the face by
                // 1.1 m at 4.3, and from the flight camera's 48 degrees it hides the face above 3 m. The skipped window still draws its light, so every other lit window and the
                // dock's wear come out as they did.
                for(int row=0;row<2;row++)for(int i=0;i<2;i++){var c=t+new Vector3(-1+i*2,1.2f+row*1.4f,-2.13f);if(i==0){if(row==1)Lit(rng,.6f);continue;}Pane(s,1,c,Vector3.right,1.2f,.8f,Vector3.back,trim);Pane(s,2,c+Vector3.back*.01f,Vector3.right,1,.62f,Vector3.back,Lit(rng,.6f));}
                for(int k=0;k<4;k++)LettersUp(s,2,"DOCK"[k].ToString(),t+new Vector3(-1.65f,.3f+(3-k)*.68f,-2.16f),.56f,new Color(.55f,1f,.95f));
                s.Slot(1).Box(t+Vector3.up*4.18f,new Vector3(6.6f,.2f,6.6f),Quaternion.identity,trim).Box(t+Vector3.up*5.52f,new Vector3(6.9f,.2f,6.9f),Quaternion.identity,trim);
                foreach(var (normal,along) in new[]{(Vector3.back,Vector3.right),(Vector3.left,Vector3.forward),(Vector3.right,Vector3.forward),(Vector3.forward,Vector3.right)})
                    for(int i=0;i<4;i++){var c=t+Vector3.up*4.85f+normal*3.3f+along*(-2.4f+i*1.6f);Pane(s,2,c,along,1.42f,1.05f,normal,new Color(.62f,.86f,1f));}
                s.Slot(0).Box(t+Vector3.up*4.85f,new Vector3(6.4f,1.15f,6.4f),Quaternion.identity,new Color(.2f,.22f,.25f));
                s.Slot(1).Post(t+new Vector3(1.8f,5.62f,1.8f),.4f,.06f,.04f,5,trim,trim);s.Slot(2).Lump(t+new Vector3(1.8f,6.05f,1.8f),Vector3.one*.12f,5,.05f,new Color(.5f,.95f,1f),new Color(.5f,.95f,1f),6,3);
                PoolAt(t+Vector3.back*3.6f,3.2f,PoolCool*1.2f);PoolAt(t,5.4f,PoolCool*.7f);
                // Floodlight masts over the dock, their light on the deck where launches come out.
                foreach(var at in DockFloods)
                {
                    var f=P(at,y);s.Slot(1).Post(f,3.8f,.22f,.14f,8,trim,ZoneKit.Shape.Dim(grey,1.1f)).Box(f+Vector3.up*.3f,new Vector3(.9f,.6f,.9f),Quaternion.identity,trim);
                    s.Slot(1).Box(f+new Vector3(0,3.85f,-.2f),new Vector3(1.4f,.34f,.5f),Quaternion.Euler(30,0,0),trim);s.Slot(2).Box(f+new Vector3(0,3.74f,-.36f),new Vector3(1.2f,.1f,.32f),Quaternion.Euler(30,0,0),new Color(.95f,.98f,1f));
                    PoolAt(f+Vector3.back*2.2f,6.5f,PoolPale);
                }
                // Where ships set down: scorch under the pylon's pad and on both landing pads, a ring painted round the pylon's foot, the pads' numbers; taxi lines in teal between them.
                var pylon=P(DockAt,y);ScorchAt(pylon,6.5f,rng);
                for(int k=0;k<24;k++){float a=k*Mathf.PI*2/24;Flat(s,0,pylon+new Vector3(Mathf.Cos(a)*7.4f,.03f,Mathf.Sin(a)*7.4f),1.2f,.3f,-a*Mathf.Rad2Deg+90,white);}
                Letters(s,0,"1",pylon+new Vector3(8.6f,.035f,-5.6f),2.4f,white);
                foreach(var (pad,num) in new[]{(new Vector2(-48,-149),"2"),(new Vector2(-27,-147),"3")})
                {
                    ScorchAt(P(pad,y+.05f),7,rng);PoolAt(P(pad,y),9.5f,PoolPale*.8f);Letters(s,0,num,P(pad,y)+new Vector3(-8.6f,.035f,5.4f),2.4f,white);
                    var from=pylon+new Vector3(0,.03f,-7.6f);var to=P(pad,y+.03f);var dir=(to-from).normalized;float run=Vector3.Distance(from,to)-7;
                    for(float d=0;d<run;d+=2.2f)Flat(s,0,from+dir*d,.28f,1.3f,Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg,teal);
                }
                // Oil and wear where cargo moves: blots across the deck, skid marks in the lanes.
                for(int k=0;k<26;k++){var at=new Vector3(Mathf.Lerp(-68,-18,(float)rng.NextDouble()),y,Mathf.Lerp(-158,-105,(float)rng.NextDouble()));StainAt(at,.9f+(float)rng.NextDouble()*2.2f,new Color(.42f,.44f,.46f),rng,2+rng.Next(3));}
                for(int k=0;k<8;k++){var at=new Vector3(Mathf.Lerp(-56,-20,(float)rng.NextDouble()),y,Mathf.Lerp(-136,-118,(float)rng.NextDouble()));float yaw=(float)rng.NextDouble()*.5f-.25f;
                    for(int i=0;i<26;i++){var p=at+new Vector3(Mathf.Sin(yaw)*i*.28f,0,Mathf.Cos(yaw)*i*.28f);float fade=.3f*(1-i/30f);foreach(float dx in new[]{0f,1.6f})Stains.Disc(p+new Vector3(dx,.045f,0),Vector3.up,.34f,8,new Color(fade,fade,fade),0,null,.5f/.34f);}}
                var top=kit.Built("Dock tower and floods",s.Slot(2),Heavy,Trim,Glow);top.transform.SetParent(root,true);
                // Stowed gear: what the dock keeps on its deck between launches.
                foreach(var (key,at,size,yaw) in new[]{("DressCargoPallet",new Vector2(-55,-130),1.8f,10f),("DressCargoPallet",new Vector2(-53,-127.5f),1.6f,-20f),("DressCableDrum",new Vector2(-56,-121),2.2f,0f),
                    ("DressFuelTank",new Vector2(-20.5f,-128),3.4f,90f),("DressPowerCell",new Vector2(-19.5f,-133),2.2f,0f),("DressCargoPallet",new Vector2(-34,-108.4f),1.7f,5f),("DressCableDrum",new Vector2(-45,-108.6f),2f,0f)})
                    if(ZoneKit.Landed(key)){var piece=kit.Prop(key,P(at,y),size,yaw,false,false);piece.name="Dock gear";piece.transform.SetParent(root,true);}
            }

            // ---------------------------------------------------------------- the bow: Pell's lamp shop under the chart room's dome
            static void LampShop(ZoneKit kit,Transform root,Vector3 c,float radius)
            {
                var s=new ZoneKit.Shape();var rng=new System.Random(907);Color wood=new(.42f,.3f,.2f),brass=new(.78f,.6f,.32f);
                Color[] lamps={new(1,.78f,.44f),new(1,.93f,.76f),new(.84f,1f,.72f),new(.72f,.95f,1f),new(1,.86f,.6f)};
                // Shelves in a ring under the glass, lamps of every make on them; lamps hung from the ribs; her bench by the great lamp.
                for(int k=0;k<10;k++)
                {
                    float a=k*Mathf.PI*2/10+.15f;var at=c+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(radius-1.8f);var turn=Quaternion.LookRotation(new Vector3(Mathf.Cos(a),0,Mathf.Sin(a)));
                    s.Slot(0).Box(at+Vector3.up*.45f,new Vector3(3.2f,.9f,.6f),turn,wood,ZoneKit.Shape.Dim(wood,1.2f));
                    for(int i=0;i<4;i++){var l=at+turn*new Vector3(-1.2f+i*.8f,0,0)+Vector3.up*(1.02f+.06f*(i%2));var tone=lamps[rng.Next(lamps.Length)];s.Slot(1).Lump(l,new Vector3(.14f,.2f,.14f),k*4+i,.05f,tone,tone,6,4);}
                }
                for(int k=0;k<8;k++){float a=k*Mathf.PI/4+.4f;var at=c+new Vector3(Mathf.Cos(a)*4.2f,1.7f,Mathf.Sin(a)*4.2f);s.Slot(0).Beam(at,at+Vector3.up*.6f,.03f,.03f,brass);var tone=lamps[k%lamps.Length];s.Slot(1).Lump(at,new Vector3(.2f,.26f,.2f),k,.05f,tone,tone,6,4);}
                s.Slot(0).Box(c+new Vector3(2.6f,.42f,-2.2f),new Vector3(2.4f,.84f,1.2f),Quaternion.Euler(0,20,0),wood,ZoneKit.Shape.Dim(wood,1.25f));
                for(int i=0;i<3;i++)s.Slot(1).Lump(c+new Vector3(2f+i*.6f,.98f,-2.3f+i*.2f),Vector3.one*.12f,i,.05f,lamps[i],lamps[i],6,4);
                var shop=kit.Built("Pell's lamp shop",s.Slot(1),Trim,Glow);shop.transform.SetParent(root,true);
                PoolAt(c,radius-.3f,PoolWarm*1.35f);PoolAt(c,radius*.45f,PoolWarm*.8f);
            }
            /// <summary>A compass rose painted round the chart room on the gallery's deck, in the bow's cream and teal: sixteen points, a ring.</summary>
            static void CompassRose(ZoneKit.Shape s,int slot,Vector3 c,float inner,float outer)
            {
                Color cream=new(.9f,.84f,.68f),teal=new(.22f,.5f,.5f);
                s.Slot(slot).Disc(c+Vector3.up*.03f,Vector3.up,inner+.35f,48,cream,inner+.12f);
                for(int k=0;k<16;k++)
                {
                    float a=k*Mathf.PI/8,half=Mathf.PI/16*.55f;float reach=k%4==0?outer:k%2==0?outer*.8f+inner*.2f:outer*.62f+inner*.38f;
                    var tip=c+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*reach+Vector3.up*.035f;var l=c+new Vector3(Mathf.Cos(a-half),0,Mathf.Sin(a-half))*(inner+.35f)+Vector3.up*.035f;var r=c+new Vector3(Mathf.Cos(a+half),0,Mathf.Sin(a+half))*(inner+.35f)+Vector3.up*.035f;
                    s.Slot(slot).Triangle(l,tip,r,Vector3.up,k%2==0?cream:teal);
                }
            }

            // ---------------------------------------------------------------- the houses each district builds
            /// <summary>A deckhouse in its district's manner: 1 the liner's (rounded ends, portholes, a teal band), 2 the cabins' (stacked up to three floors, each set back, balconies),
            /// 3 the raft folk's (a shack under a pitched reed roof, a lamp on a pole), 4 the dock's (a kiosk glazed across its front under a lit canopy), 5 the bow's (a lamp stall
            /// under a striped awning). Returns false when the room over the roof doesn't take it.</summary>
            static bool House(ZoneKit.Shape s,int house,Vector3 p,Make m,Color paint,float room,float a,float b,float c,int sub,System.Random rng,float texel)
            {
                Color dark=Unlit,trim=m.trim;
                switch(house)
                {
                    case 1:
                    {
                        float w=5+a*4,d=2.6f+b*1.4f,h=room>5.4f&&c<.45f?4.4f:2.3f;if(h+1>room)return false;float body=w-d;
                        s.Slot(0).Box(p+Vector3.up*h*.5f,new Vector3(body,h,d),Quaternion.identity,paint,ZoneKit.Shape.Dim(paint,1.08f),texel);
                        foreach(float side in new[]{-1f,1f})s.Slot(0).Post(p+Vector3.right*side*body*.5f,h,d*.5f,d*.5f,14,paint,ZoneKit.Shape.Dim(paint,1.05f));
                        for(float row=1.25f;row<h-.4f;row+=2.1f)
                        {
                            s.Slot(1).Box(p+new Vector3(0,row+.72f,-d*.5f-.02f),new Vector3(body,.22f,.04f),Quaternion.identity,m.stripe);
                            for(float x=-body*.5f+.7f;x<body*.5f-.4f;x+=1.3f){var wc=p+new Vector3(x,row,-d*.5f-.03f);bool lit=rng.Next(6)>0;s.Slot(1).Disc(wc,Vector3.back,.36f,10,trim);s.Slot(lit?2:1).Disc(wc+Vector3.back*.01f,Vector3.back,.27f,10,lit?Lit(rng,.85f):dark);}
                        }
                        s.Slot(1).Box(p+Vector3.up*(h+.07f),new Vector3(body,.14f,d+.2f),Quaternion.identity,m.stripe);
                        s.Slot(2).Lump(p+new Vector3(0,h+.3f,0),new Vector3(.18f,.12f,.18f),sub,.05f,new Color(1,.9f,.7f),new Color(1,.9f,.7f),6,3);PoolAt(p+Vector3.back*(d*.5f+1.4f),2.2f,PoolWarm*.8f);
                        return true;
                    }
                    case 2:
                    {
                        int floors=c<.3f?3:c<.75f?2:1;while(floors>1&&floors*2.3f+1>room)floors--;if(2.3f+1>room)return false;
                        float w=4+a*3.6f,d=3.4f+b*1.8f,fx=0,fz=0;
                        for(int f=0;f<floors;f++)
                        {
                            var fp=p+new Vector3(fx,f*2.3f,fz);var tone=ZoneKit.Shape.Dim(Color.Lerp(paint,Paints[(sub+f*5)%Paints.Length],.18f*f),1+.04f*f);tone.a=1;
                            s.Slot(0).Box(fp+Vector3.up*1.15f,new Vector3(w,2.3f,d),Quaternion.identity,tone,ZoneKit.Shape.Dim(tone,1.08f),texel).Slot(1).Box(fp+Vector3.up*2.36f,new Vector3(w+.24f,.12f,d+.24f),Quaternion.identity,trim);
                            int n=Mathf.Max(1,Mathf.FloorToInt((w-1)/1.4f));
                            for(int i=0;i<n;i++){var wc=fp+new Vector3(-w*.5f+1+i*1.4f,1.3f,-d*.5f-.03f);bool lit=rng.Next(7)>0;Pane(s,1,wc,Vector3.right,.95f,.8f,Vector3.back,trim);Pane(s,lit?2:1,wc+Vector3.back*.01f,Vector3.right,.8f,.65f,Vector3.back,lit?Lit(rng,.8f):dark);}
                            if(f>0)
                            {
                                // The balcony the floor below leaves: a rail along its front, a lamp, sometimes a line of washing.
                                var front=fp+new Vector3(0,0,-d*.5f-.65f);s.Slot(1).Beam(front+new Vector3(-w*.5f,.85f,0),front+new Vector3(w*.5f,.85f,0),.05f,.05f,trim);
                                for(float x=-w*.5f;x<=w*.5f+.01f;x+=w/3)s.Slot(1).Post(front+new Vector3(x,0,0),.85f,.03f,.03f,4,trim,trim);
                                if(rng.Next(2)==0)for(int i=0;i<3;i++){var cl=front+new Vector3(-w*.3f+i*w*.25f,.55f,.05f);s.Slot(1).Quad(cl+new Vector3(-.3f,.3f,0),cl+new Vector3(.3f,.3f,0),cl+new Vector3(.3f,-.25f,0),cl+new Vector3(-.3f,-.25f,0),Vector3.back,Paints[(sub+i*3)%Paints.Length]);}
                                PoolAt(front+Vector3.forward*.3f,1.2f,PoolWarm*.6f);
                            }
                            if(f==floors-1)
                            {
                                // On the top roof: a lit skylight, a water tank, or a planter of greens, so the stack doesn't end in a bare lid.
                                var roofAt=fp+Vector3.up*2.42f;int what=sub%3;
                                if(what==0){s.Slot(1).Box(roofAt+new Vector3(-w*.15f,.1f,0),new Vector3(w*.4f,.2f,d*.4f),Quaternion.identity,trim);Flat(s,2,roofAt+new Vector3(-w*.15f,.21f,0),w*.34f,d*.32f,0,Lit(rng,.9f)*.8f);}
                                else if(what==1&&room>floors*2.3f+2.2f)s.Slot(1).Post(roofAt+new Vector3(w*.2f,0,d*.1f),1.4f,.6f,.6f,12,new Color(.6f,.62f,.64f),new Color(.7f,.72f,.74f));
                                else for(int i=0;i<3;i++)s.Slot(1).Box(roofAt+new Vector3(-w*.3f+i*w*.3f,.2f,-d*.25f),new Vector3(w*.26f,.4f,.6f),Quaternion.identity,new Color(.42f,.3f,.22f)).Lump(roofAt+new Vector3(-w*.3f+i*w*.3f,.55f,-d*.25f),new Vector3(w*.12f,.3f,.3f),sub+i,.3f,new Color(.36f,.6f,.32f),new Color(.22f,.4f,.2f),8,4);
                            }
                            fx+=(rng.Next(3)-1)*.7f;fz+=.65f+d*.06f;w*=.82f;d*=.86f;
                        }
                        var door=p+new Vector3(-.4f,1,-(3.4f+b*1.8f)*.5f-.04f);Pane(s,1,door,Vector3.right,.9f,1.8f,Vector3.back,new Color(.22f,.24f,.26f));
                        s.Slot(2).Lump(door+new Vector3(0,1.15f,-.12f),new Vector3(.16f,.1f,.1f),sub,.05f,new Color(1,.84f,.58f),new Color(1,.84f,.58f),6,3);PoolAt(door+new Vector3(0,-1,-1.4f),2.1f,PoolWarm);
                        return true;
                    }
                    case 3:
                    {
                        float w=3.4f+a*2.6f,d=2.6f+b*1.2f,h=2.1f,ridge=1.2f;if(h+ridge+.4f>room)return false;
                        Color reed=Color.Lerp(new Color(.66f,.56f,.36f),new Color(.5f,.44f,.3f),c),reedDark=ZoneKit.Shape.Dim(reed,.75f);
                        s.Slot(0).Box(p+Vector3.up*h*.5f,new Vector3(w,h,d),Quaternion.identity,paint,null,texel);
                        Vector3 e0=p+new Vector3(-w*.5f-.35f,h,-d*.5f-.4f),e1=p+new Vector3(w*.5f+.35f,h,-d*.5f-.4f),n0=p+new Vector3(-w*.5f-.35f,h,d*.5f+.4f),n1=p+new Vector3(w*.5f+.35f,h,d*.5f+.4f),r0=p+new Vector3(-w*.5f-.35f,h+ridge,0),r1=p+new Vector3(w*.5f+.35f,h+ridge,0);
                        s.Slot(1).Quad(e0,e1,r1,r0,Vector3.back+Vector3.up,reed).Quad(n0,n1,r1,r0,Vector3.forward+Vector3.up,reedDark).Triangle(e0,r0,n0,Vector3.left,reedDark).Triangle(e1,r1,n1,Vector3.right,reed);
                        for(float x=-w*.5f;x<w*.5f;x+=.45f)s.Slot(1).Beam(p+new Vector3(x,h+.02f,-d*.5f-.38f),p+new Vector3(x,h+ridge+.02f,0),.04f,.02f,reedDark);
                        var wc=p+new Vector3(-w*.18f,1.2f,-d*.5f-.03f);Pane(s,1,wc,Vector3.right,1,.75f,Vector3.back,trim);Pane(s,2,wc+Vector3.back*.01f,Vector3.right,.85f,.6f,Vector3.back,Lit(rng,.95f));
                        Pane(s,1,p+new Vector3(w*.28f,.9f,-d*.5f-.04f),Vector3.right,.8f,1.7f,Vector3.back,new Color(.3f,.26f,.2f));
                        var pole=p+new Vector3(w*.5f+.9f,0,-d*.5f-.6f);s.Slot(1).Post(pole,2.6f,.06f,.05f,5,trim,trim);s.Slot(2).Lump(pole+Vector3.up*2.55f,new Vector3(.2f,.26f,.2f),sub,.05f,new Color(1,.82f,.5f),new Color(1,.74f,.42f),6,4);PoolAt(pole,2.8f,PoolWarm);
                        return true;
                    }
                    case 4:
                    {
                        float w=4.4f+a*3.4f,d=2.8f+b*1.4f,h=2.6f;if(h+.8f>room)return false;
                        s.Slot(0).Box(p+Vector3.up*h*.5f,new Vector3(w,h,d),Quaternion.identity,paint,ZoneKit.Shape.Dim(paint,1.08f),texel).Slot(1).Box(p+Vector3.up*(h+.08f),new Vector3(w+.3f,.16f,d+.3f),Quaternion.identity,trim);
                        Pane(s,1,p+new Vector3(0,1.45f,-d*.5f-.03f),Vector3.right,w-.6f,1.2f,Vector3.back,trim);Pane(s,2,p+new Vector3(0,1.45f,-d*.5f-.04f),Vector3.right,w-.8f,1f,Vector3.back,Lit(rng,.5f));
                        var canopy=p+new Vector3(0,h-.2f,-d*.5f-.9f);s.Slot(1).Box(canopy,new Vector3(w+.4f,.12f,1.8f),Quaternion.identity,m.stripe);
                        foreach(float side in new[]{-1f,1f})s.Slot(1).Post(p+new Vector3(side*(w*.5f),0,-d*.5f-1.7f),h-.26f,.05f,.05f,5,trim,trim);
                        s.Slot(2).Box(canopy+Vector3.down*.1f,new Vector3(w-.4f,.04f,.16f),Quaternion.identity,new Color(.86f,.96f,1f));PoolAt(p+Vector3.back*(d*.5f+1.2f),2.6f,PoolCool*1.2f);
                        return true;
                    }
                    case 5:
                    {
                        float w=2.2f+a*1.8f,d=1.6f+b*.8f,h=1.7f;if(h+.6f>room)return false;Color cream=new(.92f,.88f,.76f),teal=new(.24f,.52f,.52f);
                        s.Slot(0).Box(p+Vector3.up*h*.5f,new Vector3(w,h,d),Quaternion.identity,paint,null,texel);
                        int stripes=Mathf.Max(3,Mathf.RoundToInt(w/.5f));for(int i=0;i<stripes;i++){float x0=-w*.5f-.1f+i*(w+.2f)/stripes,x1=x0+(w+.2f)/stripes;
                            s.Slot(1).Quad(p+new Vector3(x0,h+.05f,-d*.5f),p+new Vector3(x1,h+.05f,-d*.5f),p+new Vector3(x1,h-.4f,-d*.5f-1.1f),p+new Vector3(x0,h-.4f,-d*.5f-1.1f),Vector3.back+Vector3.up*2,i%2==0?cream:teal);}
                        for(int i=0;i<4;i++){var l=p+new Vector3(-w*.4f+i*w*.27f,h-.75f,-d*.5f-1f);s.Slot(1).Beam(l+Vector3.up*.3f,l+Vector3.up*.5f,.02f,.02f,trim);var tone=i%2==0?new Color(1,.8f,.46f):new Color(1,.94f,.78f);s.Slot(2).Lump(l,new Vector3(.13f,.18f,.13f),sub+i,.05f,tone,tone,6,4);}
                        s.Slot(1).Box(p+new Vector3(0,.5f,-d*.5f-.3f),new Vector3(w*.9f,1,.5f),Quaternion.identity,ZoneKit.Shape.Dim(paint,.8f));PoolAt(p+Vector3.back*(d*.5f+1.2f),2.4f,PoolWarm*1.1f);
                        return true;
                    }
                }
                return false;
            }
            /// <summary>A corner of a roof where people sit or keep their things, in its district's manner: café tables under umbrellas and planters (the liner), planters, a bench
            /// and washing (the cabins), a rack of mats drying (the raft folk), a tool cart, a hose reel and drums (the dock), coiled lines and a crate of lamps (the bow).</summary>
            static void Corner(ZoneKit.Shape s,int house,Vector3 p,Make m,float room,int sub,System.Random rng)
            {
                Color trim=m.trim,leaf=new(.34f,.58f,.32f),pot=new(.5f,.36f,.26f);
                switch(house)
                {
                    case 1:
                        for(int i=0;i<3;i++)
                        {
                            var t=p+new Vector3((i-1)*2.1f,0,(i%2)*1.2f-.6f);s.Slot(1).Post(t,.7f,.06f,.06f,6,trim,trim).Disc(t+Vector3.up*.72f,Vector3.up,.45f,12,new Color(.9f,.88f,.82f));
                            s.Slot(1).Post(t,2,.03f,.03f,4,trim,trim).Lump(t+Vector3.up*2,new Vector3(1,.18f,1),sub+i,.02f,i%2==0?m.stripe:new Color(.94f,.9f,.8f),ZoneKit.Shape.Dim(m.stripe,.7f),10,3);
                        }
                        foreach(float dx in new[]{-3.4f,3.4f}){var pl=p+new Vector3(dx,0,1.6f);s.Slot(1).Box(pl+Vector3.up*.35f,new Vector3(.9f,.7f,.9f),Quaternion.identity,pot).Lump(pl+Vector3.up*1,new Vector3(.6f,.5f,.6f),sub,.25f,leaf,ZoneKit.Shape.Dim(leaf,.6f),8,5);}
                        s.Slot(2).Lump(p+new Vector3(0,1.6f,-1.6f),Vector3.one*.16f,sub,.05f,new Color(1,.86f,.6f),new Color(1,.86f,.6f),6,3);PoolAt(p,3.4f,PoolWarm*.9f);
                        break;
                    case 2:
                        for(int i=0;i<3;i++){var pl=p+new Vector3(-2+i*1.1f,0,1.2f);s.Slot(1).Box(pl+Vector3.up*.3f,new Vector3(.9f,.6f,.7f),Quaternion.identity,pot).Lump(pl+Vector3.up*.85f,new Vector3(.5f,.45f,.45f),sub+i,.25f,leaf,ZoneKit.Shape.Dim(leaf,.6f),8,5);}
                        s.Slot(1).Box(p+new Vector3(1.6f,.4f,-.6f),new Vector3(2,.12f,.6f),Quaternion.identity,new Color(.5f,.38f,.26f));
                        {var a0=p+new Vector3(-2.6f,0,-1.4f);var a1=p+new Vector3(2.6f,0,-1.8f);s.Slot(1).Post(a0,1.8f,.04f,.04f,4,trim,trim).Post(a1,1.8f,.04f,.04f,4,trim,trim).Rope(a0+Vector3.up*1.75f,a1+Vector3.up*1.75f,.2f,.02f,new Color(.8f,.78f,.7f));
                            for(int i=0;i<4;i++){var cl=Vector3.Lerp(a0,a1,(i+1)/5f)+Vector3.up*1.35f;s.Slot(1).Quad(cl+new Vector3(-.3f,.3f,0),cl+new Vector3(.3f,.3f,0),cl+new Vector3(.3f,-.3f,0),cl+new Vector3(-.3f,-.3f,0),Vector3.back,Paints[(sub+i*5)%Paints.Length]);}}
                        s.Slot(2).Lump(p+new Vector3(0,1.4f,1.4f),Vector3.one*.14f,sub,.05f,new Color(1,.86f,.6f),new Color(1,.86f,.6f),6,3);PoolAt(p,2.6f,PoolWarm*.8f);
                        break;
                    case 3:
                        foreach(float dx in new[]{-2.2f,2.2f}){var f=p+new Vector3(dx,0,0);s.Slot(1).Beam(f+new Vector3(0,0,-.8f),f+Vector3.up*1.9f,.08f,.08f,new Color(.5f,.42f,.3f)).Beam(f+new Vector3(0,0,.8f),f+Vector3.up*1.9f,.08f,.08f,new Color(.5f,.42f,.3f));}
                        s.Slot(1).Beam(p+new Vector3(-2.4f,1.9f,0),p+new Vector3(2.4f,1.9f,0),.08f,.08f,new Color(.5f,.42f,.3f));
                        for(int i=0;i<3;i++){var top=p+new Vector3(-1.4f+i*1.4f,1.88f,0);var dye=new[]{new Color(.82f,.62f,.36f),new Color(.6f,.7f,.42f),new Color(.86f,.8f,.6f),new Color(.5f,.62f,.66f)}[(sub+i)%4];
                            s.Slot(1).Quad(top+new Vector3(-.6f,0,0),top+new Vector3(.6f,0,0),top+new Vector3(.6f,-1.3f,-.35f),top+new Vector3(-.6f,-1.3f,-.35f),Vector3.back+Vector3.up,dye);}
                        break;
                    case 4:
                    {
                        s.Slot(1).Box(p+new Vector3(-1.6f,.55f,0),new Vector3(1.6f,.7f,.9f),Quaternion.identity,m.stripe).Box(p+new Vector3(-1.6f,.95f,0),new Vector3(1.7f,.08f,1),Quaternion.identity,trim);
                        foreach(float dx in new[]{-2.2f,-1f})s.Slot(1).Disc(p+new Vector3(dx,.18f,-.47f),Vector3.back,.18f,8,new Color(.15f,.15f,.16f));
                        var reel=p+new Vector3(1.4f,.7f,.4f);s.Slot(1).Disc(reel+Vector3.back*.3f,Vector3.back,.7f,14,new Color(.7f,.72f,.74f)).Disc(reel+Vector3.back*.32f,Vector3.back,.55f,14,new Color(.22f,.24f,.26f),.2f).Box(reel+Vector3.down*.45f,new Vector3(.9f,.5f,.8f),Quaternion.identity,trim);
                        Color[] drums={new(.24f,.42f,.58f),new(.36f,.5f,.4f),new(.62f,.64f,.66f)};
                        for(int i=0;i<3;i++){var dr=p+new Vector3(.4f+i*.75f,0,-1.4f+(i%2)*.4f);s.Slot(1).Post(dr,1,.34f,.34f,10,drums[(sub+i)%3],drums[(sub+i)%3]);}
                        StainAt(p+new Vector3(.8f,0,-1),1.2f,new Color(.45f,.46f,.48f),rng,2);
                        break;
                    }
                    case 5:
                    {
                        var coil=p+new Vector3(-1.4f,.08f,0);s.Slot(1).Disc(coil,Vector3.up,.75f,16,new Color(.74f,.66f,.48f),.3f).Disc(coil+Vector3.up*.08f,Vector3.up,.6f,16,new Color(.8f,.72f,.52f),.32f);
                        var crate=p+new Vector3(1.2f,.4f,0);s.Slot(1).Box(crate,new Vector3(1.4f,.8f,1),Quaternion.Euler(0,12,0),new Color(.56f,.42f,.28f));
                        for(int i=0;i<4;i++){var tone=i%2==0?new Color(1,.8f,.46f):new Color(1,.94f,.78f);s.Slot(2).Lump(crate+Quaternion.Euler(0,12,0)*new Vector3(-.45f+i*.3f,.48f,0),new Vector3(.1f,.14f,.1f),sub+i,.05f,tone,tone,6,4);}
                        PoolAt(crate,1.6f,PoolWarm*.7f);
                        break;
                    }
                    default:
                        for(int i=0;i<3;i++)s.Slot(3).Box(p+new Vector3((i%2)*1.5f-.7f,.6f+(i/2)*1.2f,0),Vector3.one*1.2f,Quaternion.Euler(0,(i*17+sub)%20-10,0),Paints[(sub+i)%Paints.Length],null,.6f);
                        break;
                }
            }
            /// <summary>The liner's lido: a pool lit from under its water, its rim, a ladder, loungers along its side and lamps at its corners.</summary>
            static void Lido(ZoneKit.Shape s,Vector3 p,Make m,int sub)
            {
                float w=6.4f,d=3.4f;Color rim=new(.9f,.9f,.86f);
                s.Slot(1).Box(p+Vector3.up*.18f,new Vector3(w+.8f,.36f,d+.8f),Quaternion.identity,rim);s.Slot(2).Box(p+Vector3.up*.3f,new Vector3(w,.14f,d),Quaternion.identity,new Color(.34f,.82f,.86f));
                for(int i=0;i<4;i++)s.Slot(1).Box(p+new Vector3(-w*.5f+.8f+i*1.6f,.38f,0),new Vector3(.04f,.02f,d-.2f),Quaternion.identity,new Color(.5f,.9f,.92f));
                foreach(float dx in new[]{-.3f,.3f})s.Slot(1).Tube(new[]{p+new Vector3(w*.5f-.6f+dx,.36f,-d*.5f-.1f),p+new Vector3(w*.5f-.6f+dx,1.1f,-d*.5f-.2f),p+new Vector3(w*.5f-.6f+dx,1.1f,-d*.5f+.2f)},new[]{.04f},4,rim,rim,1,1,false);
                for(int i=0;i<4;i++){var l=p+new Vector3(-w*.5f+.9f+i*1.6f,.2f,-d*.5f-1.5f);s.Slot(1).Box(l,new Vector3(.62f,.14f,1.6f),Quaternion.Euler(-6,0,0),i%2==0?m.stripe:rim);}
                foreach(var (dx,dz) in new[]{(-w*.5f-.6f,-d*.5f-.6f),(w*.5f+.6f,-d*.5f-.6f)}){s.Slot(1).Post(p+new Vector3(dx,0,dz),1,.05f,.04f,5,m.trim,m.trim);s.Slot(2).Lump(p+new Vector3(dx,1.08f,dz),Vector3.one*.15f,sub,.05f,new Color(1,.92f,.74f),new Color(1,.92f,.74f),6,3);}
                PoolAt(p,4.6f,new Color(.12f,.3f,.32f));PoolAt(p+Vector3.back*(d*.5f+1.4f),2.6f,PoolWarm*.7f);
            }

            // ---------------------------------------------------------------- drawing the light and the wear
            static void CityLight(ZoneKit kit,Transform root)
            {
                var light=kit.Built("City light on the decks",Pools,Pool);light.transform.SetParent(root,true);
                var wear=kit.Built("Wear on the decks",Stains,Stain);wear.transform.SetParent(root,true);
                foreach(var r in new[]{light.GetComponent<MeshRenderer>(),wear.GetComponent<MeshRenderer>()}){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
            }
        }
    }
}
