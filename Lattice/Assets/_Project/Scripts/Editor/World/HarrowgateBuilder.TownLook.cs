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
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Lattice.EditorTools
{
    /// <summary>Harrowgate's look pass after the whole-game capture of November 20 (segments s49 to s53): the muster field's mark and the town's two walks laid in stone instead of flat
    /// colour, standards round the field, banners hung in folds, a doorway with something behind it, the party's parley flag at the gate, sacks of sacking, pots and kettles a hand would
    /// know, everyone standing turned to someone or something, and the Warmaster's own body (his war-frame under a crimson mantle, a head over his guard wing) wherever he is. The
    /// town's <see cref="Town.Build"/> calls these; <see cref="TownProbe"/> renders the places the capture stood at, from where its camera stood.</summary>
    public static partial class HarrowgateBuilder
    {
        /// <summary>Stills of the town from where the capture's camera stood (frames.csv of nov20-whole), each with the town in the state that still showed:
        /// <c>exec-when-green.ps1 -Method Lattice.EditorTools.HarrowgateBuilder.TownProbe -Marker TOWN_PROBE_OK -Graphics -ExtraArgs @('-probe-out','&lt;folder&gt;')</c>.
        /// Never saves the scene.</summary>
        public static void TownProbe()=>BatchTools.Run(()=>{Town.Probe(DevArgs.Value("-probe-out")??throw new ArgumentException("-probe-out <folder> is required"),DevArgs.Value("-probe-shots"));Debug.Log("TOWN_PROBE_OK");});
        /// <summary>What HarrowgateTownTests' look-pass checks read, read off the built scene in the editor and logged (TOWN_MEASURE ...): run before a rebuild, it is the old town's
        /// reading. Marker TOWN_MEASURE_OK. Never saves.</summary>
        public static void TownMeasure()=>BatchTools.Run(()=>{Town.Measure();Debug.Log("TOWN_MEASURE_OK");});

        internal static partial class Town
        {
            internal static Material Medallion,RunnerStone,Burlap;
            /// <summary>Where the party's parley flag stands: on the quay by the gate ramp's foot, three metres off the way up it, where s50/027's frame has the party walking in under it
            /// (by the west parapet it stood at the frame's top edge, cut off).</summary>
            internal static readonly Vector2 ParleyAt=new(.6f,13.2f);
            /// <summary>The middle of the guard's door in the east tower, the one Vessa goes in by.</summary>
            internal const float DoorX=13.6f;
            /// <summary>Where the cell's two Unsworn guards stand, at the landing's two ends facing the cells: well off the party, who stand at CellFront to talk through the bars (one
            /// stood 2.4 m from it, and Idra stood inside him, s52/339).</summary>
            internal static readonly (Vector2 at,float yaw)[] CellGuards={(new(50.6f,106.2f),17),(new(62f,106.4f),346)};
            static float Hash(int n){uint h=(uint)(n*374761393+668265263);h=(h^(h>>13))*1274126177;h^=h>>16;return (h&0xffff)/65535f;}

            // ---------------------------------------------------------------- a mesh with its own uvs
            /// <summary>A mesh laid with uvs of its own (a medallion's disc, a runner's band, a mantle), each face turned to whoever is meant to see it.</summary>
            sealed class Sheet
            {
                readonly List<Vector3> points=new(),normals=new();readonly List<Vector2> uv=new();readonly List<Color32> tones=new();readonly List<int> faces=new();
                public int Add(Vector3 p,Vector3 n,Vector2 t,Color tone){points.Add(p);normals.Add(n.normalized);uv.Add(t);tones.Add(tone);return points.Count-1;}
                public void Tri(int a,int b,int c,Vector3 outward){bool front=Vector3.Dot(Vector3.Cross(points[b]-points[a],points[c]-points[a]),outward)>=0;faces.Add(a);faces.Add(front?b:c);faces.Add(front?c:b);}
                public void Quad(int a,int b,int c,int d,Vector3 outward){Tri(a,b,c,outward);Tri(a,c,d,outward);}
                public bool Empty=>points.Count==0;
                public Mesh Mesh(string name)
                {
                    var mesh=new Mesh{name=name,indexFormat=points.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};
                    mesh.SetVertices(points);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetColors(tones);mesh.SetTriangles(faces,0);mesh.RecalculateBounds();return mesh;
                }
            }
            static GameObject Lay(ZoneKit kit,string name,Sheet sheet,Material material)
            {
                var mesh=ZoneKit.SaveMesh(sheet.Mesh(name),"Assets/_Project/Scenes/"+kit.zone+"-"+name.Replace(' ','-').Replace("'","")+".asset");
                var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=mesh;
                var view=go.GetComponent<MeshRenderer>();view.sharedMaterial=material;view.shadowCastingMode=ShadowCastingMode.Off;return go;
            }

            // ---------------------------------------------------------------- the stone the field and the walks are laid in
            /// <summary>The look pass's own materials: the field's medallion, the walks' runner of slate slabs between kerbstones, and sacking. Called from <see cref="Materials"/>.</summary>
            static void LookMaterials()
            {
                Medallion=ZoneKit.Toon("harrow-medallion",MedallionTexture(),m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.5f,.5f,.62f));});
                RunnerStone=ZoneKit.Toon("harrow-runner",RunnerTexture(),m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.5f,.5f,.62f));});
                var sacking=ZoneKit.Texture("Harrowgate/harrow-burlap",256,(u,v)=>
                {
                    // Coarse sacking: forty threads a repeat each way, over and under, the odd thread darker, and the cloth mottled with handling.
                    int cu=Mathf.FloorToInt(u*40),cv=Mathf.FloorToInt(v*40);float fu=u*40-cu,fv=v*40-cv;bool warpUp=((cu+cv)&1)==0;
                    float thread=warpUp?Mathf.Sin(fu*Mathf.PI):Mathf.Sin(fv*Mathf.PI);float gap=Mathf.Min(Mathf.Min(fu,1-fu),Mathf.Min(fv,1-fv));
                    float tone=.62f+.22f*thread-(gap<.08f?.14f:0)+.08f*(ZoneKit.Tile(u*12,v*12,12,31)-.5f)-(Hash(cu*7+3)>.9f?.08f:0)-(Hash(cv*13+5)>.92f?.06f:0);
                    return new Color(tone*1.02f,tone*.95f,tone*.82f,1);
                });
                Burlap=ZoneKit.Toon("harrow-burlap",sacking,m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.52f,.5f,.6f));});
            }
            /// <summary>The runner's stone, 4.8 m along by 2.4 m across a repeat: a kerb of long pale dressed stones down each side, and between them slabs of dark slate in courses across
            /// the way, two or three to a course with their joints staggered, each its own tone, worn paler down the middle where the files march, pitted, the arrises worn round.</summary>
            internal const float RunLong=4.8f,RunWide=2.4f,RunKerb=.26f;
            static Texture2D RunnerTexture()
            {
                var cut=new System.Random(73);
                var kerbs=new List<float>[2];
                for(int e=0;e<2;e++){kerbs[e]=new List<float>{0};float at=.5f+(float)cut.NextDouble()*.4f;while(at<RunLong-.5f){kerbs[e].Add(at);at+=.8f+(float)cut.NextDouble()*.6f;}}
                var courses=new List<(float from,float to,float[] across,float[] tones)>();
                for(float at=0;at<RunLong-.01f;)
                {
                    float deep=Mathf.Min(RunLong-at,.55f+(float)cut.NextDouble()*.4f);if(RunLong-at-deep<.4f)deep=RunLong-at;
                    int n=cut.Next(2,4);var across=new float[n+1];across[0]=RunKerb;across[n]=RunWide-RunKerb;
                    for(int k=1;k<n;k++)across[k]=RunKerb+(RunWide-2*RunKerb)*(k+((float)cut.NextDouble()-.5f)*.5f)/n;
                    var tones=new float[n];for(int k=0;k<n;k++)tones[k]=.82f+.34f*(float)cut.NextDouble();
                    courses.Add((at,at+deep,across,tones));at+=deep;
                }
                // Cracks across a few slabs: short wandering lines.
                var cracks=new List<Vector2[]>();var wander=new System.Random(77);
                for(int k=0;k<5;k++)
                {
                    var p=new Vector2((float)wander.NextDouble()*RunLong,RunKerb+.2f+(float)wander.NextDouble()*(RunWide-2*RunKerb-.4f));float a=(float)wander.NextDouble()*Mathf.PI*2;var line=new Vector2[5];
                    for(int i=0;i<5;i++){line[i]=p;a+=((float)wander.NextDouble()-.5f)*1.4f;p+=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(.08f+(float)wander.NextDouble()*.12f);}cracks.Add(line);
                }
                return ZoneKit.Texture("Harrowgate/harrow-runner",1024,(u,v)=>
                {
                    float x=u*RunLong,y=v*RunWide;
                    float grain=.06f*(ZoneKit.Tile(u*160,v*160,160,11)-.5f)+.05f*(ZoneKit.Cloud(u,v,10,12)-.5f);
                    if(y<RunKerb||y>RunWide-RunKerb)
                    {
                        int e=y<RunKerb?0:1;var b=kerbs[e];int k=0;for(int i=1;i<b.Count;i++)if(x>=b[i])k=i;
                        float left=x-b[k],right=(k+1<b.Count?b[k+1]:RunLong)-x,inner=e==0?RunKerb-y:y-(RunWide-RunKerb),outer=e==0?y:RunWide-y;
                        float joint=Mathf.Min(Mathf.Min(left,right),inner),tone=.72f+.1f*Hash(e*31+k)+grain;
                        if(joint<.016f)tone=.22f;else if(joint<.05f)tone+=.05f;
                        if(outer<.035f)tone-=.16f;
                        if(ZoneKit.Tile(u*380,v*380,380,15)>.92f)tone-=.06f;
                        return new Color(tone,tone*.97f,tone*.92f,1);
                    }
                    var c=courses[courses.Count-1];foreach(var each in courses)if(x<each.to){c=each;break;}
                    int s=0;for(int i=1;i<c.across.Length-1;i++)if(y>=c.across[i])s=i;
                    float edge=Mathf.Min(Mathf.Min(x-c.from,c.to-x),Mathf.Min(y-c.across[s],c.across[s+1]-y));
                    float slab=.3f*c.tones[s]+grain;
                    float mid=Mathf.Abs(y-RunWide*.5f)/(RunWide*.5f-RunKerb);slab+=.07f*(1-mid*mid)*(.5f+ZoneKit.Cloud(u,v,6,13));
                    if(ZoneKit.Tile(u*420,v*420,420,14)>.9f)slab-=.05f;
                    foreach(var line in cracks)for(int i=1;i<line.Length;i++)if(ZoneKit.Segment(new Vector2(x,y),line[i-1],line[i])<.008f)slab-=.12f;
                    if(edge<.016f)slab=.1f;else if(edge<.045f)slab+=.05f;
                    return new Color(slab*.93f,slab*.98f,slab*1.07f,1);
                });
            }
            /// <summary>The muster field's mark, 14.4 m across: the Muster's ground, laid by the Muster. From outside in: a ring of dark basalt voussoirs, a band of pale limestone, a field
            /// of slate setts laid in rings, the crest-sail's two swept vanes inlaid in pale stone in five pieces each, a ring of dark basalt, and at the middle an iron boss riveted into the
            /// stone; the Warmaster's post a pale square stone two metres north of it, where he stands to receive a challenge. Worn paler where the files march north to south and where
            /// duellists circle, pitted, cracked here and there, grit in every joint. Greys and creams only: on a floor red, orange or purple says danger (D217).</summary>
            internal const float MedallionR=7.2f;
            /// <summary>The crest-sail's west vane on the plan, in metres from the medallion's middle: a convex leading edge swept up and out to the tip, a hollow trailing edge back down to
            /// a tail under the root (three curves; straight edges read as two flat arrowheads).</summary>
            static readonly Vector2[] Vane=Curve(new(-1.05f,.55f),new(-2.7f,2.95f),new(-5.1f,3.4f),10).Concat(Curve(new(-5.1f,3.4f),new(-3.4f,1.6f),new(-2.4f,-2.05f),10).Skip(1))
                .Concat(Curve(new(-2.4f,-2.05f),new(-1.45f,-1.0f),new(-1.15f,-.25f),6).Skip(1)).ToArray();
            static IEnumerable<Vector2> Curve(Vector2 a,Vector2 c,Vector2 b,int n){for(int i=0;i<=n;i++){float t=i/(float)n;yield return (1-t)*(1-t)*a+2*(1-t)*t*c+t*t*b;}}
            static bool InPolygon(Vector2 p,Vector2[] poly)
            {
                bool inside=false;
                for(int i=0,j=poly.Length-1;i<poly.Length;j=i++)if((poly[i].y>p.y)!=(poly[j].y>p.y)&&p.x<(poly[j].x-poly[i].x)*(p.y-poly[i].y)/(poly[j].y-poly[i].y)+poly[i].x)inside=!inside;
                return inside;
            }
            static float ToPolygon(Vector2 p,Vector2[] poly){float d=float.PositiveInfinity;for(int i=0,j=poly.Length-1;i<poly.Length;j=i++)d=Mathf.Min(d,ZoneKit.Segment(p,poly[j],poly[i]));return d;}
            static Texture2D MedallionTexture()
            {
                var rng=new System.Random(79);var cracks=new List<Vector2[]>();
                for(int k=0;k<10;k++)
                {
                    float r0=2.6f+(float)rng.NextDouble()*3.6f,a0=(float)rng.NextDouble()*Mathf.PI*2;var at=new Vector2(Mathf.Cos(a0),Mathf.Sin(a0))*r0;var line=new Vector2[6];float a=(float)rng.NextDouble()*Mathf.PI*2;
                    for(int i=0;i<6;i++){line[i]=at;a+=((float)rng.NextDouble()-.5f)*1.2f;at+=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*(.16f+(float)rng.NextDouble()*.22f);}cracks.Add(line);
                }
                var mirror=Vane.Select(p=>new Vector2(-p.x,p.y)).ToArray();
                Vector2 root=new(-1.05f,.1f),tip=new(-5.1f,3.4f);var axis=(tip-root).normalized;float axisLength=Vector2.Distance(root,tip);
                return ZoneKit.Texture("Harrowgate/harrow-medallion",1024,(u,v)=>
                {
                    var p=new Vector2((u-.5f)*2*MedallionR,(v-.5f)*2*MedallionR);float r=p.magnitude,a=Mathf.Atan2(p.y,p.x);
                    float grain=.05f*(ZoneKit.Tile(u*220,v*220,220,21)-.5f)+.05f*(ZoneKit.Cloud(u,v,9,22)-.5f);
                    float tone,edge;Color hue;bool iron=false;
                    bool left=InPolygon(p,Vane),right=!left&&InPolygon(p,mirror);
                    if(r>=6.62f)
                    {
                        const int n=64;float t=(a+Mathf.PI)/(Mathf.PI*2)*n;int k=Mathf.FloorToInt(t);
                        edge=Mathf.Min(Mathf.Min(t-k,k+1-t)*Mathf.PI*2/n*r,Mathf.Min(r-6.62f,MedallionR-r));tone=.22f+.08f*Hash(k)+grain;hue=new Color(1,1.02f,1.1f);
                    }
                    else if(r>=6.42f)
                    {
                        const float n=37;float t=(a+Mathf.PI)/(Mathf.PI*2)*n;int k=Mathf.FloorToInt(t);
                        edge=Mathf.Min(Mathf.Min(t-k,k+1-t)*Mathf.PI*2/n*r,Mathf.Min(r-6.42f,6.62f-r));tone=.76f+.08f*Hash(k+200)+grain;hue=new Color(1,.98f,.92f);
                    }
                    else if(r<1.6f)
                    {
                        // The iron boss: riveted round its rim, a pale stone ring let into it, a pale boss at the very middle.
                        iron=true;edge=1.6f-r;tone=.24f+.05f*(ZoneKit.Cloud(u,v,14,23)-.5f);hue=new Color(1,1,1.06f);
                        for(int k=0;k<18;k++){float ra=k*Mathf.PI*2/18;if(Vector2.Distance(p,new Vector2(Mathf.Cos(ra),Mathf.Sin(ra))*1.4f)<.055f)tone=.62f;}
                        if(r>.82f&&r<1f){tone=.74f+grain;hue=new Color(1,.98f,.92f);iron=false;edge=Mathf.Min(r-.82f,1f-r);}
                        if(r<.34f){tone=.7f+grain;hue=new Color(1,.98f,.92f);edge=.34f-r;}
                    }
                    else if(left||right)
                    {
                        // The crest-sail's vanes: pale stone in five pieces along each vane, a dark joint round the whole and between pieces.
                        var q=right?new Vector2(-p.x,p.y):p;float s=Vector2.Dot(q-root,axis)/axisLength*5;float piece=Mathf.Abs(s-Mathf.Round(s))*axisLength/5;int k=Mathf.FloorToInt(s);
                        // Weathered limestone, a shade over the pale band: polished white it read as two flat white spikes (the first render of the pass).
                        edge=Mathf.Min(ToPolygon(q,Vane),piece);tone=.6f+.08f*Hash(k+(right?40:0))+1.4f*grain;hue=new Color(1,.97f,.9f);
                    }
                    else if(r<2.3f)
                    {
                        const int n=16;float t=(a+Mathf.PI)/(Mathf.PI*2)*n;int k=Mathf.FloorToInt(t);
                        edge=Mathf.Min(Mathf.Min(t-k,k+1-t)*Mathf.PI*2/n*r,Mathf.Min(r-1.6f,2.3f-r));tone=.2f+.07f*Hash(k+400)+grain;hue=new Color(1,1.02f,1.1f);
                    }
                    else
                    {
                        // The field of flags laid in rings: rings .82 m deep, stones about a metre along each, every ring's joints off the last's, each stone its own tone (narrow
                        // rings of small setts aliased into a record's grooves at the play camera).
                        const float deep=.82f,longest=1.05f;
                        // Each joint between two stones runs straight out from the middle: measured along the ring's own middle, not at the pixel's radius (that bent every joint
                        // into a swirl).
                        int ring=Mathf.FloorToInt((r-2.3f)/deep);float inRing=(r-2.3f)-ring*deep,middle=2.3f+(ring+.5f)*deep;
                        float count=Mathf.Max(6,Mathf.Round(Mathf.PI*2*middle/longest)),t=(a+Mathf.PI)/(Mathf.PI*2)*count+ring*.47f;int k=Mathf.FloorToInt(t);
                        edge=Mathf.Min(Mathf.Min(t-k,k+1-t)*Mathf.PI*2/count*r,Mathf.Min(inRing,deep-inRing));
                        // The vanes' outline joint carries on into the stones round them.
                        edge=Mathf.Min(edge,Mathf.Min(ToPolygon(p,Vane),ToPolygon(p,mirror)));
                        tone=.3f+.16f*Hash(ring*977+k)+1.3f*grain;hue=new Color(.96f,1,1.08f);
                    }
                    // The Warmaster's post: a pale square stone where he stands to receive a challenge.
                    var post=p-new Vector2(0,2);if(Mathf.Abs(post.x)<.42f&&Mathf.Abs(post.y)<.42f){iron=false;tone=.8f+grain;hue=new Color(1,.98f,.92f);edge=Mathf.Min(.42f-Mathf.Abs(post.x),.42f-Mathf.Abs(post.y));}
                    // Wear: paler down the field's middle where the files march, and round where duellists circle; pits; cracks.
                    if(!iron)
                    {
                        float march=1-ZoneKit.Smooth(1.2f,3.2f,Mathf.Abs(p.x)),circle=1-ZoneKit.Smooth(.3f,1.4f,Mathf.Abs(r-4.2f));
                        tone+=(.05f*march+.035f*circle)*(.5f+ZoneKit.Cloud(u,v,7,24));
                        if(ZoneKit.Tile(u*500,v*500,500,25)>.91f)tone-=.05f;
                        foreach(var line in cracks)for(int i=1;i<line.Length;i++)if(ZoneKit.Segment(p,line[i-1],line[i])<.012f)tone-=.12f;
                    }
                    // The joints: grit-dark, the arris either side worn a little paler.
                    if(edge<.028f){tone=.15f;hue=new Color(1,.96f,.9f);}else if(edge<.06f)tone+=.04f;
                    if(r>MedallionR-.025f)tone*=.6f;
                    return new Color(tone*hue.r,tone*hue.g,tone*hue.b,1);
                },false);
            }

            // ---------------------------------------------------------------- the walks' runners and the field's mark
            /// <summary>A band of the runner's stone along the plan from one point to another at the floor, following the ground, its kerbs down both sides and a kerbstone across each end.
            /// "Floor detail", so the checks read it as flat.</summary>
            static void Runner(ZoneKit kit,string name,Vector2 from,Vector2 to,float wide,float lift=.016f,float tone=1)
            {
                var sheet=new Sheet();var along=(to-from).normalized;var side=new Vector2(-along.y,along.x);float length=Vector2.Distance(from,to);
                int n=Mathf.Max(1,Mathf.CeilToInt(length/1.2f));int last=-1;
                for(int i=0;i<=n;i++)
                {
                    float s=length*i/n;var c=from+along*s;
                    // Each 1.2 m a shade apart, so the repeat doesn't read down a hundred metres of walk.
                    var shade=Color.white*(tone*(.93f+.12f*Mathf.PerlinNoise(c.x*.21f+3,c.y*.21f+9)));shade.a=1;
                    var l=c-side*wide*.5f;var r=c+side*wide*.5f;
                    int a=sheet.Add(kit.On(l.x,l.y,lift),Vector3.up,new Vector2(s/RunLong,0),shade),b=sheet.Add(kit.On(r.x,r.y,lift),Vector3.up,new Vector2(s/RunLong,1),shade);
                    if(last>=0)sheet.Quad(last,last+1,b,a,Vector3.up);last=a;
                }
                // The ends: a kerbstone laid across, in the kerb's own stone.
                foreach(var (at,dir) in new[]{(from,along),(to,-along)})
                {
                    var o=at;var i0=o-side*wide*.5f;var i1=o+side*wide*.5f;var j0=i0+dir*RunKerb*wide/RunWide;var j1=i1+dir*RunKerb*wide/RunWide;
                    var white=Color.white*tone;white.a=1;float lifted=lift+.002f;
                    int a=sheet.Add(kit.On(i0.x,i0.y,lifted),Vector3.up,new Vector2(0,0),white),b=sheet.Add(kit.On(i1.x,i1.y,lifted),Vector3.up,new Vector2(wide/RunLong,0),white);
                    int c2=sheet.Add(kit.On(j1.x,j1.y,lifted),Vector3.up,new Vector2(wide/RunLong,RunKerb/RunWide*.95f),white),d=sheet.Add(kit.On(j0.x,j0.y,lifted),Vector3.up,new Vector2(0,RunKerb/RunWide*.95f),white);
                    sheet.Quad(a,b,c2,d,Vector3.up);
                }
                Lay(kit,name,sheet,RunnerStone);
            }
            /// <summary>The two marching walks: the lower walk end to end, and the forecourt's walk, which stops short of the three ramps' heads.</summary>
            static void Walks(ZoneKit kit)
            {
                Runner(kit,"Floor detail walk runner lower",new Vector2(-48,35),new Vector2(42,35),RunWide);
                int k=0;foreach(var (x0,x1) in new[]{(-48f,-41.4f),(-35.6f,-4.6f),(4.6f,39.6f)})Runner(kit,"Floor detail walk runner forecourt "+(++k),new Vector2(x0,66),new Vector2(x1,66),RunWide);
            }
            /// <summary>The field's mark (s51/180): the medallion at the field's middle, the kerb's inner border in the runner's stone, standards at the corners.</summary>
            static void FieldMark(ZoneKit kit,Transform field)
            {
                // The medallion: a disc on the field's floor, its uvs laid flat from above.
                var disc=new Sheet();var mid=new Vector3(DuelMiddle.x,Upper+.014f,DuelMiddle.y);const int around=96;float[] rings={0,.8f,1.6f,2.3f,3.4f,4.6f,5.8f,6.62f,MedallionR};
                int centre=disc.Add(mid,Vector3.up,new Vector2(.5f,.5f),Color.white);var prior=new int[around];for(int k=0;k<around;k++)prior[k]=centre;
                for(int ri=1;ri<rings.Length;ri++)
                {
                    var now=new int[around];
                    for(int k=0;k<around;k++){float a=k*Mathf.PI*2/around;var o=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*rings[ri];now[k]=disc.Add(mid+o,Vector3.up,new Vector2(o.x/(2*MedallionR)+.5f,o.z/(2*MedallionR)+.5f),Color.white);}
                    for(int k=0;k<around;k++){int n=(k+1)%around;if(ri==1)disc.Tri(centre,now[k],now[n],Vector3.up);else disc.Quad(prior[k],now[k],now[n],prior[n],Vector3.up);}
                    prior=now;
                }
                var laid=Lay(kit,"Floor detail muster field medallion",disc,Medallion);laid.transform.SetParent(field,true);
                // The border inside the kerb, a metre and a fifth wide, in the walks' stone: the Muster's way round its own ground.
                const float B=1.2f;
                foreach(var (name,a,b) in new[]{("south",new Vector2(-FieldHalfX,FieldSouth+B*.5f),new Vector2(FieldHalfX,FieldSouth+B*.5f)),("north",new Vector2(FieldHalfX,FieldNorth-B*.5f),new Vector2(-FieldHalfX,FieldNorth-B*.5f)),
                    ("west",new Vector2(-FieldHalfX+B*.5f,FieldNorth-B),new Vector2(-FieldHalfX+B*.5f,FieldSouth+B)),("east",new Vector2(FieldHalfX-B*.5f,FieldSouth+B),new Vector2(FieldHalfX-B*.5f,FieldNorth-B))})
                    Runner(kit,"Floor detail muster field border "+name,a,b,B,.012f,.9f);
                foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.parent==null&&t.name.StartsWith("Floor detail muster field border")).ToArray())t.SetParent(field,true);
                // Standards at the corners, outside the kerb and off the camera's line to anyone on the field (the south pair stand wide of the field's own edges).
                foreach(var (name,at,yaw) in Standards)Standard(kit,"Field standard "+name,at,yaw).transform.SetParent(field,true);
            }
            /// <summary>The field's standards: at its two south corners, wide of the field so the camera's line to a fighter at the kerb passes inside them, and at its north-west corner
            /// by the cadets' files. The north-east corner is the oath bell's.</summary>
            internal static readonly (string name,Vector2 at,float yaw)[] Standards={("south-west",new(-21.4f,71.1f),180),("south-east",new(21.6f,71.1f),180),("north-west",new(-19.8f,100.8f),180)};
            /// <summary>One of the Muster's standards: an iron-shod pole leaded into a squared socket of the town's stone, a cross-bar and a bronze spear-head, the crimson standard hung in folds
            /// with the crest-sail on it and a gold fringe. Its own mesh, so its drawn box is its own (a combined one would stand over the whole field for the sightline checks).</summary>
            static GameObject Standard(ZoneKit kit,string name,Vector2 at,float yaw)
            {
                var shape=new ZoneKit.Shape();var f=kit.On(at.x,at.y);var t=Quaternion.Euler(0,yaw,0);var facing=t*Vector3.forward;var across=t*Vector3.right;
                Color iron=new(.24f,.24f,.26f),bronze=new(.86f,.7f,.44f),stone=new(.62f,.6f,.58f);
                shape.Slot(0).Box(f+Vector3.up*.3f,new Vector3(.72f,.6f,.72f),t,stone,new Color(.74f,.72f,.68f),.25f).Box(f+Vector3.up*.63f,new Vector3(.5f,.06f,.5f),t,new Color(.5f,.48f,.46f),null,.25f);
                shape.Slot(1).Post(f+Vector3.up*.6f,5.5f,.085f,.06f,8,iron,new Color(.36f,.36f,.38f));
                var head=f+Vector3.up*6.0f;
                shape.Slot(1).Beam(head-across*.78f,head+across*.78f,.07f,.07f,iron);
                foreach(int s in new[]{-1,1})shape.Slot(2).Lump(head+across*s*.82f,Vector3.one*.07f,s+3,.05f,bronze,ZoneKit.Shape.Dim(bronze,.7f),8,4);
                shape.Slot(2).Tube(new[]{head+Vector3.up*.08f,head+Vector3.up*.22f,head+Vector3.up*.34f,head+Vector3.up*.72f},new[]{.06f,.06f,.14f,.01f},8,ZoneKit.Shape.Dim(bronze,.8f),bronze,1,1);
                Hang(shape,head+Vector3.down*.06f+facing*.02f,1.36f,2.5f,facing,3,new Color(.5f,.11f,.11f),new Color(.78f,.78f,.8f),new Color(.3f,.07f,.07f),bronze);
                var drawn=kit.Built(name,shape,Stone,Iron,Bronze,Cloth);ZoneKit.Fadeable(drawn);
                ZoneKit.Block(drawn,f+Vector3.up*.5f,new Vector3(.72f,1,.72f),yaw,"Standard socket");return drawn;
            }

            /// <summary>The gallery walks' own stones (s50/131: wide bare flagstone from the cases to the camera): before each case a long slab of pale limestone let into the paving, its
            /// border cut and three lines incised in it, the crest-sail at its head: where the thing in the case was taken, and when. "Floor detail", so the checks read them as flat.</summary>
            static void CampaignStones(ZoneKit kit,IEnumerable<float> xs)
            {
                var face=ZoneKit.Texture("Harrowgate/harrow-campaign-stone",256,(u,v)=>
                {
                    float x=u*2,y=v;float tone=.76f+.05f*(ZoneKit.Tile(u*90,v*90,90,51)-.5f)+.04f*(ZoneKit.Cloud(u,v,5,52)-.5f);
                    float border=Mathf.Min(Mathf.Min(x-.08f,1.92f-x),Mathf.Min(y-.1f,.9f-y));if(Mathf.Abs(border)<.018f)tone-=.32f;
                    if(Mathf.Min(Mathf.Min(x,2-x),Mathf.Min(y,1-y))<.025f)tone-=.18f;
                    // Three lines of cut letters: strokes of their own widths, the gaps between words a little wider.
                    for(int line=0;line<3;line++)
                    {
                        float ly=.72f-line*.2f;if(Mathf.Abs(y-ly)>.05f||x<.52f||x>1.82f)continue;
                        int stroke=Mathf.FloorToInt((x-.52f)/.045f);float inStroke=(x-.52f)/.045f-stroke;bool gap=Hash(stroke*7+line*131)>.82f;
                        if(!gap&&inStroke>.2f&&inStroke<.72f&&Mathf.Abs(y-ly)<.045f*(.6f+.4f*Hash(stroke+line*17)))tone-=.3f;
                    }
                    // The crest-sail at its head, cut into the stone.
                    var p=new Vector2((x-.28f)*30,(y-.5f)*30);
                    foreach(int s in new[]{-1,1})if(InPolygon(new Vector2(p.x*s,p.y),Vane))tone-=.24f;
                    return new Color(tone,tone*.98f,tone*.93f,1);
                },false);
                var cut=ZoneKit.Toon("harrow-campaign-stone",face,m=>{ZoneKit.Tinted(m);m.SetFloat("_RampSteps",4);m.SetColor("_ShadowTint",new Color(.5f,.5f,.62f));});
                var sheet=new Sheet();int n=0;
                foreach(float cx in xs)
                {
                    var c=new Vector2(cx,105.6f);const float w=1.5f,d=.75f;var tone=Color.white*(.94f+.1f*Hash(n++));tone.a=1;
                    int a=sheet.Add(kit.On(c.x-w*.5f,c.y-d*.5f,.013f),Vector3.up,new Vector2(0,0),tone),b=sheet.Add(kit.On(c.x+w*.5f,c.y-d*.5f,.013f),Vector3.up,new Vector2(1,0),tone);
                    int e=sheet.Add(kit.On(c.x+w*.5f,c.y+d*.5f,.013f),Vector3.up,new Vector2(1,1),tone),f=sheet.Add(kit.On(c.x-w*.5f,c.y+d*.5f,.013f),Vector3.up,new Vector2(0,1),tone);
                    sheet.Quad(a,b,e,f,Vector3.up);
                }
                if(!sheet.Empty)Lay(kit,"Floor detail campaign stones",sheet,cut);
            }

            // ---------------------------------------------------------------- cloth hung in folds
            /// <summary>Cloth hung from a bar (s53/014: a banner was one flat plane): pleated in folds that deepen and billow toward the tail, each fold's two faces lit apart, a pointed
            /// tail, a hem band at the head, a fringe along the tail, and the crest-sail laid on its face over the folds. <paramref name="facing"/> is the side it shows.</summary>
            internal static void Hang(ZoneKit.Shape shape,Vector3 top,float wide,float tall,Vector3 facing,int clothSlot,Color cloth,Color? device,Color? band=null,Color? fringe=null)
            {
                facing.y=0;facing.Normalize();var across=Vector3.Cross(Vector3.up,facing).normalized;
                int folds=Mathf.Max(4,Mathf.RoundToInt(wide/.2f));if(folds%2==1)folds++;float fold=Mathf.Clamp(wide*.06f,.03f,.08f),point=Mathf.Min(.42f,tall*.16f);
                float Tri(float x){float c=(x/wide+.5f)*folds;float m=c-2*Mathf.Floor(c/2);return 1-Mathf.Abs(m-1);}
                // Straight down each fold (a curved billow left each fold's one flat quad in front of what was laid on it).
                float Depth(float x,float down){float d=Mathf.Clamp01(down/tall);return fold*(.45f+.55f*d)*Tri(x)+.06f*d;}
                Vector3 S(float x,float down)=>top+across*x+Vector3.down*down+facing*Depth(x,down);
                float Bottom(float x)=>tall+point*(1-Mathf.Abs(2*x/wide));
                shape.Slot(clothSlot);
                for(int k=0;k<folds;k++)
                {
                    float x0=-wide*.5f+wide*k/folds,x1=-wide*.5f+wide*(k+1)/folds;var tone=ZoneKit.Shape.Dim(cloth,k%2==0?1.04f:.88f);
                    shape.Quad(S(x0,0),S(x1,0),S(x1,Bottom(x1)),S(x0,Bottom(x0)),facing,tone,.6f);
                    var hem=band??ZoneKit.Shape.Dim(cloth,.62f);
                    shape.Quad(S(x0,0)+facing*.02f,S(x1,0)+facing*.02f,S(x1,.13f)+facing*.02f,S(x0,.13f)+facing*.02f,facing,ZoneKit.Shape.Dim(hem,k%2==0?1.04f:.88f),.6f);
                    float b0=Bottom(x0)-.34f,b1=Bottom(x1)-.34f;
                    shape.Quad(S(x0,b0)+facing*.02f,S(x1,b1)+facing*.02f,S(x1,b1+.12f)+facing*.02f,S(x0,b0+.12f)+facing*.02f,facing,ZoneKit.Shape.Dim(hem,k%2==0?1.04f:.88f),.6f);
                }
                if(fringe is Color gold)for(float x=-wide*.5f+.04f;x<wide*.5f;x+=.075f)
                {var b=S(x,Bottom(x))+facing*.004f;shape.Triangle(b-across*.022f,b+across*.022f,b+Vector3.down*.11f+facing*.01f,facing,ZoneKit.Shape.Dim(gold,.85f+.3f*Hash((int)(x*100)+7)),.6f);}
                if(device is not Color mark)return;
                // The crest-sail: two swept vanes, laid on the folds a fold at a time in thin bands, each band a trapezoid in its fold's own plane, so no ridge cuts across it
                // (laid as small triangles across the folds, the ridges between their corners hid half of them and the vanes came out in shards).
                float d0=tall*.46f,w=wide*.5f;
                (float lo,float hi)? Across(Vector2[] poly,float y)
                {
                    float lo=float.PositiveInfinity,hi=float.NegativeInfinity;
                    for(int i=0,j=poly.Length-1;i<poly.Length;j=i++)
                    {
                        var a=poly[j];var b=poly[i];if((a.y>y)==(b.y>y)||Mathf.Approximately(a.y,b.y))continue;
                        float x=a.x+(b.x-a.x)*(y-a.y)/(b.y-a.y);lo=Mathf.Min(lo,x);hi=Mathf.Max(hi,x);
                    }
                    return lo<hi?(lo,hi):null;
                }
                foreach(int s in new[]{-1,1})
                {
                    var vane=new[]{new Vector2(s*.03f*w,d0),new Vector2(s*.72f*w,d0-.3f*tall),new Vector2(s*.26f*w,d0+.12f*tall),new Vector2(s*.1f*w,d0+.2f*tall)};
                    float first=vane.Min(p=>p.y),last=vane.Max(p=>p.y);const float strip=.045f;
                    for(float y0=first;y0<last;y0+=strip)
                    {
                        float y1=Mathf.Min(last,y0+strip);var r0=Across(vane,y0+.001f);var r1=Across(vane,y1-.001f);if(r0==null&&r1==null)continue;
                        var (l0,h0)=r0??((r1.Value.lo+r1.Value.hi)*.5f,(r1.Value.lo+r1.Value.hi)*.5f);var (l1,h1)=r1??((l0+h0)*.5f,(l0+h0)*.5f);
                        for(int k=0;k<folds;k++)
                        {
                            float x0=-wide*.5f+wide*k/folds,x1=-wide*.5f+wide*(k+1)/folds;
                            float a0=Mathf.Clamp(l0,x0,x1),b0=Mathf.Clamp(h0,x0,x1),a1=Mathf.Clamp(l1,x0,x1),b1=Mathf.Clamp(h1,x0,x1);if(b0-a0<.002f&&b1-a1<.002f)continue;
                            var lift=facing*.024f;shape.Quad(S(a0,y0)+lift,S(b0,y0)+lift,S(b1,y1)+lift,S(a1,y1)+lift,facing,ZoneKit.Shape.Dim(mark,k%2==0?1.04f:.88f),.6f);
                        }
                    }
                }
            }

            // ---------------------------------------------------------------- the guard's door, and the party's parley flag
            /// <summary>The guard's door in the east tower's south face (s50/052: a flat black rectangle behind Hessa): a stone frame proud of the wall with a keystone and a threshold, the
            /// plank leaf stood open against its reveal, and the lit passage beyond it, a lamp's warmth on its back wall and the first step of the tower's stair.</summary>
            static void GuardDoor(ZoneKit kit,Transform parent)
            {
                var shape=new ZoneKit.Shape();
                Color stone=new(.68f,.66f,.64f),pale=new(.82f,.8f,.76f),iron=new(.26f,.26f,.28f),inside=new(.42f,.3f,.2f);
                // The tower is solid, so the passage is the frame's own depth: everything stands in front of the tower's face. East of the ramp's parapet (x 12.4), which ran across the old
                // rectangle's west half.
                const float x=DoorX,face=19.5f,deep=.46f,half=.54f;
                // The frame: jambs, lintel and keystone standing proud of the tower, and the threshold.
                foreach(int s in new[]{-1,1})shape.Slot(0).Box(new Vector3(x+s*(half+.18f),1.3f,face-deep*.5f),new Vector3(.36f,2.6f,deep),Quaternion.identity,stone,pale,.2f);
                shape.Slot(0).Box(new Vector3(x,2.78f,face-deep*.5f-.02f),new Vector3(half*2+.76f,.36f,deep+.04f),Quaternion.identity,pale,pale,.2f).Box(new Vector3(x,2.86f,face-deep-.05f),new Vector3(.32f,.5f,.06f),Quaternion.identity,pale,pale,.2f);
                shape.Slot(0).Box(new Vector3(x,.07f,face-deep*.5f-.04f),new Vector3(half*2+.3f,.14f,deep+.08f),Quaternion.identity,pale,pale,.2f);
                // The passage: its reveals in shadow, a back wall warm with the lamp inside, the foot of the tower's stair going up into it, and the raised grate's teeth under the lintel.
                foreach(int s in new[]{-1,1})shape.Slot(0).Quad(new Vector3(x+s*half,.14f,face-deep),new Vector3(x+s*half,.14f,face-.01f),new Vector3(x+s*half,2.6f,face-.01f),new Vector3(x+s*half,2.6f,face-deep),Vector3.right*-s,ZoneKit.Shape.Dim(stone,.6f),.3f);
                shape.Slot(1).Quad(new Vector3(x-half,2.6f,face-.008f),new Vector3(x+half,2.6f,face-.008f),new Vector3(x+half,.14f,face-.008f),new Vector3(x-half,.14f,face-.008f),Vector3.back,inside,.5f);
                for(int k=0;k<4;k++)shape.Slot(0).Box(new Vector3(x+.06f,.24f+k*.19f,face-.05f-.06f*(3-k)*.25f),new Vector3(half*2-.08f-k*.08f,.17f,.08f),Quaternion.identity,ZoneKit.Shape.Dim(pale,.46f+.09f*k),null,.3f);
                shape.Slot(2).Box(new Vector3(x,2.52f,face-deep*.5f),new Vector3(half*2,.06f,.06f),Quaternion.identity,iron);
                for(float bx=x-half+.08f;bx<x+half;bx+=.15f)shape.Slot(2).Box(new Vector3(bx,2.42f,face-deep*.5f),new Vector3(.04f,.24f,.04f),Quaternion.identity,iron);
                // The lamp inside: a small glow up on the back wall, and its light on the passage's floor and the threshold.
                shape.Slot(3).Lump(new Vector3(x+.3f,2.02f,face-.03f),new Vector3(.09f,.13f,.04f),5,.02f,Color.white,Color.white,6,4);
                var lamp=kit.Lamp(new Vector3(x,0,face-.3f),3.2f,.9f,new Color(1,.72f,.42f));lamp.transform.position=new Vector3(x+.2f,1.9f,face-.25f);lamp.transform.SetParent(parent,true);
                kit.Built("Harbour gate guard's door",shape,Stone,Timber,Iron,Lamp).transform.SetParent(parent,true);
            }
            /// <summary>The party's parley flag (s50/027: "walk in under a parley flag", and none to be seen): plain white on a staff, planted in an iron tripod by the ramp's foot where
            /// they asked to be let in, and left standing there while they are in the town.</summary>
            static void ParleyFlag(ZoneKit kit,Vector2 at)
            {
                var shape=new ZoneKit.Shape();var f=kit.On(at.x,at.y);Color iron=new(.26f,.26f,.28f),staff=new(.72f,.62f,.46f);
                for(int k=0;k<3;k++){float a=k*Mathf.PI*2/3+.4f;shape.Slot(1).Beam(f+new Vector3(Mathf.Cos(a)*.42f,0,Mathf.Sin(a)*.42f),f+Vector3.up*.7f,.04f,.04f,iron);}
                shape.Slot(0).Post(f,4.6f,.045f,.035f,8,staff,ZoneKit.Shape.Dim(staff,1.08f));
                var head=f+Vector3.up*4.55f;shape.Slot(1).Beam(head+Vector3.left*.62f,head+Vector3.right*.62f,.04f,.04f,iron).Lump(head+Vector3.up*.1f,Vector3.one*.06f,4,.05f,new Color(.86f,.84f,.8f),new Color(.6f,.58f,.56f),8,4);
                Hang(shape,head+Vector3.down*.04f+Vector3.back*.03f,1.12f,1.7f,Vector3.back,2,new Color(.94f,.93f,.9f),null,new Color(.82f,.84f,.86f),new Color(.86f,.86f,.84f));
                var drawn=kit.Built("The party's parley flag",shape,Timber,Iron,Cloth);ZoneKit.Fadeable(drawn);
                ZoneKit.Block(drawn,f+Vector3.up*.5f,new Vector3(.5f,1,.5f),0,"Parley flag's tripod");
            }

            // ---------------------------------------------------------------- what people carry, and sacks
            /// <summary>A sack of sacking: a full belly, its neck gathered and tied with cord, a crease where it slumps. <paramref name="mark"/> stencils the Muster's bar on its side.</summary>
            static void Sack(ZoneKit.Shape shape,Vector3 foot,float size,Color tone,int seed,Quaternion? turn=null,Color? mark=null)
            {
                var t=turn??Quaternion.Euler(0,seed*37%360,0);
                shape.Lump(foot+t*Vector3.up*size*.42f,new Vector3(size*.5f,size*.42f,size*.4f),seed,.1f,tone,ZoneKit.Shape.Dim(tone,.72f),10,6,.5f,t);
                var neck=foot+t*Vector3.up*size*.82f;
                shape.Tube(new[]{neck,neck+t*Vector3.up*size*.12f,neck+t*Vector3.up*size*.22f},new[]{size*.14f,size*.09f,size*.15f},8,ZoneKit.Shape.Dim(tone,.9f),ZoneKit.Shape.Dim(tone,1.02f),1,1);
                shape.Tube(new[]{neck+t*Vector3.up*size*.1f,neck+t*Vector3.up*size*.14f},new[]{size*.1f,size*.1f},8,new Color(.44f,.36f,.26f),new Color(.44f,.36f,.26f),1,1,false);
                if(mark is Color m){var side=foot+t*new Vector3(0,size*.42f,-size*.41f);shape.Quad(side+t*new Vector3(-size*.18f,size*.06f,0),side+t*new Vector3(size*.18f,size*.06f,0),side+t*new Vector3(size*.18f,-size*.06f,0),side+t*new Vector3(-size*.18f,-size*.06f,0),t*Vector3.back,m,.5f);}
            }
            /// <summary>A copper cook-pot held in front (s50/201: a flat grey cylinder): bellied, a rolled rim, two lugs, a lid with a knob, soot round its foot.</summary>
            static void CookPot(ZoneKit.Shape shape,Vector3 at,float size)
            {
                Color copper=new(.86f,.54f,.34f),soot=new(.24f,.2f,.18f);
                shape.Tube(new[]{at,at+Vector3.up*size*.15f,at+Vector3.up*size*.45f,at+Vector3.up*size*.72f,at+Vector3.up*size*.8f},new[]{size*.32f,size*.44f,size*.48f,size*.44f,size*.47f},14,soot,copper,1,1);
                shape.Disc(at+Vector3.up*size*.79f,Vector3.up,size*.42f,14,ZoneKit.Shape.Dim(copper,.82f)).Lump(at+Vector3.up*size*.86f,Vector3.one*size*.07f,3,.05f,new Color(.3f,.26f,.24f),new Color(.2f,.18f,.16f),6,3);
                foreach(int s in new[]{-1,1})shape.Beam(at+new Vector3(s*size*.44f,size*.66f,0),at+new Vector3(s*size*.62f,size*.66f,0),size*.07f,size*.05f,ZoneKit.Shape.Dim(copper,.7f));
            }
            /// <summary>An iron kettle with its bail: a round black belly, a spout, a lid and a brass knob.</summary>
            static void Kettle(ZoneKit.Shape shape,Vector3 at,float size)
            {
                Color black=new(.2f,.2f,.22f),sheen=new(.36f,.36f,.4f),brass=new(.8f,.64f,.38f);
                shape.Tube(new[]{at,at+Vector3.up*size*.2f,at+Vector3.up*size*.5f,at+Vector3.up*size*.68f},new[]{size*.3f,size*.42f,size*.38f,size*.22f},12,black,sheen,1,1);
                shape.Tube(new[]{at+new Vector3(size*.34f,size*.26f,0),at+new Vector3(size*.56f,size*.48f,0),at+new Vector3(size*.64f,size*.62f,0)},new[]{size*.07f,size*.05f,size*.035f},6,black,sheen,1,1);
                shape.Lump(at+Vector3.up*size*.72f,Vector3.one*size*.06f,2,.05f,brass,ZoneKit.Shape.Dim(brass,.7f),6,3);
                var arc=new Vector3[7];for(int k=0;k<7;k++){float a=k*Mathf.PI/6;arc[k]=at+new Vector3(-Mathf.Cos(a)*size*.34f,size*.62f+Mathf.Sin(a)*size*.34f,0);}
                shape.Tube(arc,new[]{size*.02f},5,black,black,1,1,false);
            }

            // ---------------------------------------------------------------- everyone faces someone or something
            /// <summary>Turns everyone standing in the town who faces open paving (s50/131, s50/190, s51/159) to the nearest person within four and a half metres, or failing that to the
            /// nearest thing within four. Someone who already has a person or a thing before them keeps the way they were set. Runs once the town's people and props all stand.</summary>
            static void FaceSomething()
            {
                Physics.SyncTransforms();
                // Whoever is watching (the drill, the duel, the verdict) faces what they watch, set where they were stood.
                var standing=UnityEngine.Object.FindObjectsByType<PropTint>(FindObjectsSortMode.None).Where(t=>t.gameObject.activeInHierarchy&&(t.name.StartsWith("Townsperson")||t.name.StartsWith("A child"))&&!t.name.Contains("watching")).Select(t=>t.transform).ToArray();
                var people=standing.Select(s=>s.position).Concat(UnityEngine.Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).Where(n=>n.gameObject.activeInHierarchy).Select(n=>n.transform.position))
                    .Concat(UnityEngine.Object.FindObjectsByType<WalkLoop>(FindObjectsSortMode.None).Where(w=>w.gameObject.activeInHierarchy).Select(w=>w.transform.position))
                    .Concat(UnityEngine.Object.FindObjectsByType<KeepsPace>(FindObjectsSortMode.None).Select(k=>k.transform.position)).ToArray();
                bool Faces(Transform one)
                {
                    var at=one.position;var ahead=one.forward;ahead.y=0;ahead.Normalize();
                    if(people.Any(p=>{var to=p-at;to.y=0;return to.magnitude>.3f&&to.magnitude<4.6f&&Vector3.Angle(ahead,to)<55;}))return true;
                    foreach(float up in new[]{.5f,1.2f})foreach(var hit in Physics.RaycastAll(at+Vector3.up*up+ahead*.45f,ahead,4.2f,~0,QueryTriggerInteraction.Ignore))if(!hit.collider.transform.IsChildOf(one))return true;
                    return false;
                }
                void Turn(Transform one,Vector3 toward){var d=toward-one.position;d.y=0;if(d.sqrMagnitude<.01f)return;one.rotation=Quaternion.Euler(0,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,0);Physics.SyncTransforms();}
                int turned=0;var left=new List<string>();
                foreach(var one in standing)
                {
                    if(Faces(one))continue;var at=one.position;
                    var near=people.Where(p=>{var d=p-at;d.y=0;return d.magnitude>.3f&&d.magnitude<4.6f;}).OrderBy(p=>Vector3.Distance(p,at)).ToArray();
                    if(near.Length>0){Turn(one,near[0]);if(Faces(one)){turned++;continue;}}
                    var things=Physics.OverlapSphere(at+Vector3.up*.9f,4,~0,QueryTriggerInteraction.Ignore).Where(c=>!c.transform.IsChildOf(one)&&c.GetComponentInParent<PropTint>()==null&&!(c is MeshCollider mesh&&!mesh.convex))
                        .Select(c=>c.ClosestPoint(at+Vector3.up*.9f)).OrderBy(p=>Vector3.Distance(p,at)).ToArray();
                    bool done=false;foreach(var p in things.Take(4)){Turn(one,p);if(Faces(one)){done=true;break;}}
                    if(done)turned++;else left.Add($"{one.name} at ({at.x:0.0}, {at.z:0.0})");
                }
                Debug.Log($"HARROWGATE_FACING turned={turned} of {standing.Length}"+(left.Count>0?" facing nothing still: "+string.Join("; ",left):""));
            }

            // ---------------------------------------------------------------- the Warmaster's own body
            const string EnemyWarmaster="Assets/_Project/Prefabs/Enemies/DraskWarmaster.prefab",PersonWarmaster="Assets/_Project/Prefabs/Characters/DraskWarmaster.prefab";
            /// <summary>How much bigger he is drawn than his own war-frame's 2.5 m: a head over his guard wing (2.3 m) wherever he stands. Only what's drawn: his capsule is his definition's.</summary>
            internal const float WarmasterScale=1.12f;
            /// <summary>Drask Vaal's one body (s52/085: in the duel he was his guard wing's own armoured shape, told apart only by the lock box; in town and in his cell a smith's red apron):
            /// his generated war-frame, the one his portrait was drawn from, under a crimson mantle of his own: a short cape over both shoulders, clasped in bronze at the collar, and a long
            /// cape down his back to the knee, gold-hemmed, lined dark. Built on the frame's upper chest, so it moves with him. Two prefabs: the duel's (the enemy's, drawn at
            /// <see cref="WarmasterScale"/> by his definition) and the person's (the same, with the combat body's ground support taken off and the scale on its root), which is his
            /// CharacterDef's natural body: at his table, yielded on the field, on the field at the verdict, in his cell and aboard Many Hands. Null until his frame has been taken in.</summary>
            internal static (GameObject enemy,GameObject person) Warmaster()
            {
                var frame=ZoneKit.Prefab("Enemies","DraskVaal");if(frame==null)return (null,null);
                var mantleMaterial=ZoneKit.Toon("harrow-mantle",ZoneKit.Texture("Harrowgate/harrow-mantle-wool",256,(u,v)=>
                {
                    // Heavy wool: a fine twill on the diagonal and the nap mottled.
                    float twill=Mathf.Sin((u+v)*Mathf.PI*72)*.035f;float tone=.8f+twill+.1f*(ZoneKit.Cloud(u,v,8,41)-.5f)+.04f*(ZoneKit.Tile(u*128,v*128,128,42)-.5f);return new Color(tone,tone,tone,1);
                }),m=>{ZoneKit.Tinted(m);m.SetColor("_ShadowTint",new Color(.5f,.46f,.56f));m.SetColor("_RimColor",new Color(.9f,.7f,.6f));});
                var root=(GameObject)PrefabUtility.InstantiatePrefab(frame);
                try
                {
                    root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);root.transform.localScale=Vector3.one;
                    Transform Bone(string name)=>root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
                    var spine=Bone("Spine")??Bone("Spine01")??root.transform;var neck=Bone("neck")??Bone("Neck")??spine;var leftArm=Bone("LeftArm");var rightArm=Bone("RightArm");var knee=Bone("LeftLeg");
                    var skin=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(s=>s.sharedMesh!=null?s.sharedMesh.vertexCount:0).FirstOrDefault();
                    // His torso as it stands: the body baked to the world, the back's line taken where the cape hangs from.
                    var body=new List<Vector3>();
                    if(skin!=null){var baked=new Mesh();skin.BakeMesh(baked,true);var m=skin.transform.localToWorldMatrix;foreach(var p in baked.vertices)body.Add(m.MultiplyPoint3x4(p));UnityEngine.Object.DestroyImmediate(baked);}
                    float shoulderY=leftArm!=null&&rightArm!=null?(leftArm.position.y+rightArm.position.y)*.5f:2.0f,neckY=neck.position.y,kneeY=knee!=null?knee.position.y:.75f;
                    float shoulderX=leftArm!=null&&rightArm!=null?Mathf.Abs(leftArm.position.x-rightArm.position.x)*.5f:.42f;float cx=leftArm!=null&&rightArm!=null?(leftArm.position.x+rightArm.position.x)*.5f:0;
                    var chest=body.Where(p=>p.y>shoulderY-.45f&&p.y<shoulderY+.05f&&Mathf.Abs(p.x-cx)<shoulderX*.8f).ToArray();
                    float back=chest.Length>0?chest.Min(p=>p.z):spine.position.z-.25f,front=chest.Length>0?chest.Max(p=>p.z):spine.position.z+.25f,cz=(back+front)*.5f,half=Mathf.Max(.14f,(front-back)*.5f);
                    var mantle=new Sheet();Color crimson=new(.56f,.1f,.1f),lining=new(.22f,.06f,.08f),gold=new(.9f,.72f,.38f);
                    // One surface round him: angle a from his front (0) round his back (pi); height from the collar down. Outside crimson, inside the dark lining.
                    void Wrap(float a0,float a1,int columns,float[] heights,Func<float,float,float> rx,Func<float,float,float> rz,Func<float,float,float> pleat,int hemRows)
                    {
                        int rows=heights.Length;var outer=new int[rows,columns+1];var inner=new int[rows,columns+1];
                        for(int j=0;j<rows;j++)for(int i=0;i<=columns;i++)
                        {
                            float a=Mathf.Lerp(a0,a1,i/(float)columns),y=heights[j],t=j/(float)(rows-1);float out_=pleat(a,t);
                            var dir=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));var p=new Vector3(cx+Mathf.Sin(a)*(rx(a,t)+out_),y,cz+Mathf.Cos(a)*(rz(a,t)+out_));var n=(dir+Vector3.up*(j==0?.6f:.1f)).normalized;
                            var tone=j>=rows-hemRows?gold:ZoneKit.Shape.Dim(crimson,.86f+.22f*Mathf.Clamp01(.5f+out_*8));
                            outer[j,i]=mantle.Add(p,n,new Vector2(a*.6f,y*.8f),tone);inner[j,i]=mantle.Add(p-dir*.016f,-n,new Vector2(a*.6f,y*.8f),j>=rows-hemRows?ZoneKit.Shape.Dim(gold,.7f):lining);
                        }
                        for(int j=0;j<rows-1;j++)for(int i=0;i<columns;i++)
                        {
                            float a=Mathf.Lerp(a0,a1,(i+.5f)/columns);var outward=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
                            mantle.Quad(outer[j,i],outer[j,i+1],outer[j+1,i+1],outer[j+1,i],outward);mantle.Quad(inner[j,i],inner[j,i+1],inner[j+1,i+1],inner[j+1,i],-outward);
                        }
                    }
                    float collar=Mathf.Max(shoulderY+.1f,neckY-.06f);
                    // The short cape over both shoulders, open at the front where it is clasped.
                    Wrap(Mathf.PI*.2f,Mathf.PI*1.8f,40,new[]{collar,collar-.1f,shoulderY-.06f,shoulderY-.2f,shoulderY-.34f,shoulderY-.4f},
                        (a,t)=>Mathf.Lerp(.17f,shoulderX+.2f,Mathf.Sqrt(t)),(a,t)=>Mathf.Lerp(half*.7f,half+.14f,Mathf.Sqrt(t)),(a,t)=>t*.035f*Mathf.Sin(a*14),1);
                    // The long cape down his back, flaring away from his legs, pleated, gold at the hem.
                    var drop=Enumerable.Range(0,10).Select(k=>Mathf.Lerp(shoulderY-.22f,kneeY-.12f,k/9f)).ToArray();
                    Wrap(Mathf.PI*.62f,Mathf.PI*1.38f,30,drop,(a,t)=>shoulderX+.06f+.24f*t,(a,t)=>half+.06f+.3f*t,(a,t)=>t*.05f*Mathf.Sin(a*18),1);
                    var mesh=mantle.Mesh("DraskMantle");Directory.CreateDirectory("Assets/_Project/Art/Generated/Harrowgate");
                    mesh=ZoneKit.SaveMesh(mesh,"Assets/_Project/Art/Generated/Harrowgate/DraskMantle.asset");
                    // The clasps: two bronze bosses at the collar's front corners, and a chain between them.
                    var clasp=new ZoneKit.Shape();
                    foreach(int s in new[]{-1,1}){float a=Mathf.PI*(s<0?1.8f:.2f);clasp.Lump(new Vector3(cx+Mathf.Sin(a)*.2f,collar-.1f,cz+Mathf.Cos(a)*(half*.75f)+.02f),Vector3.one*.06f,s+5,.05f,gold,ZoneKit.Shape.Dim(gold,.65f),8,4);}
                    clasp.Rope(new Vector3(cx-.12f,collar-.11f,cz+half*.8f+.03f),new Vector3(cx+.12f,collar-.11f,cz+half*.8f+.03f),.05f,.012f,gold,4,6);
                    var claspMesh=ZoneKit.SaveMesh(clasp.Mesh("DraskMantleClasp"),"Assets/_Project/Art/Generated/Harrowgate/DraskMantleClasp.asset");
                    var bronze=ZoneKit.Toon("harrow-mantle-clasp",null,m=>{ZoneKit.Tinted(m);m.SetColor("_BaseColor",new Color(.95f,.72f,.42f));m.SetColor("_RimColor",new Color(1,.86f,.6f));m.SetFloat("_RampSteps",5);});
                    foreach(var (name,shape,material) in new[]{("Mantle",mesh,mantleMaterial),("Mantle clasps",claspMesh,bronze)})
                    {
                        var piece=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));piece.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                        piece.GetComponent<MeshFilter>().sharedMesh=shape;piece.GetComponent<MeshRenderer>().sharedMaterial=material;piece.transform.SetParent(spine,true);
                    }
                    var enemy=PrefabUtility.SaveAsPrefabAsset(root,EnemyWarmaster);
                    foreach(var support in root.GetComponentsInChildren<GroundLifecycleSupport>(true))UnityEngine.Object.DestroyImmediate(support);
                    root.transform.localScale=Vector3.one*WarmasterScale;
                    var person=PrefabUtility.SaveAsPrefabAsset(root,PersonWarmaster);
                    Debug.Log($"HARROWGATE_WARMASTER shoulders y={shoulderY:0.00} half-width={shoulderX:0.00} back z={back:0.00} front z={front:0.00} knee y={kneeY:0.00} mantle={mesh.vertexCount} vertices");
                    return (enemy,person);
                }
                finally{UnityEngine.Object.DestroyImmediate(root);}
            }
            /// <summary>The Warmaster's person body where the town stands him by hand (the verdict on the field), drawn only.</summary>
            static GameObject WarmasterStanding(ZoneKit kit,Vector2 at,float yaw)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PersonWarmaster);if(prefab==null)return Body(kit,"DraskBody",new Vector3(at.x,0,at.y),yaw,Color.white,false);
                var person=new GameObject("Townsperson");person.transform.SetPositionAndRotation(kit.On(at.x,at.y),Quaternion.Euler(0,yaw,0));
                var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab);model.transform.SetParent(person.transform,false);model.name="Generated civilian";
                foreach(var rig in model.GetComponentsInChildren<Animator>()){rig.cullingMode=AnimatorCullingMode.CullCompletely;PrefabUtility.RecordPrefabInstancePropertyModifications(rig);}
                return person;
            }

            // ---------------------------------------------------------------- stills from where the capture stood
            /// <summary>Where each still of the capture stood: the hero, the camera (frames.csv), and what was shown and hidden in the town at that moment. Spawned are enemy bodies
            /// standing where the duel had them.</summary>
            static readonly (string name,Vector3 hero,Vector3 camera,string[] show,string[] hide,(string enemy,Vector3 at,float yaw)[] spawn)[] Shots=
            {
                ("s51-180-field",new(0,4.88f,85.47f),new(-5.11f,17.41f,72.24f),new string[0],new string[0],new (string,Vector3,float)[0]),
                ("s52-085-duel",new(-4.38f,4.88f,90.88f),new(-9.45f,17.88f,76.64f),new[]{"The town watching"},new[]{"The field at drill"},
                    new[]{("DraskVaal",new Vector3(.6f,4.8f,88.2f),230f),("MusterPikeguard",new Vector3(4.4f,4.8f,92.4f),220f),("MusterPikeguard",new Vector3(6.8f,4.8f,89.6f),240f)}),
                ("s52-138-field-east",new(5.43f,4.88f,92.71f),new(.19f,16.09f,77.94f),new[]{"The town watching"},new[]{"The field at drill"},new (string,Vector3,float)[0]),
                ("s52-254-lower-walk",new(-21.62f,2.48f,36.24f),new(-26.62f,15.01f,21.82f),new string[0],new string[0],new (string,Vector3,float)[0]),
                ("s51-200-lower-walk-east",new(7.56f,2.48f,35f),new(1.63f,15.01f,20.98f),new string[0],new string[0],new (string,Vector3,float)[0]),
                ("s52-155-forecourt",new(-8.33f,4.88f,63.73f),new(-13.21f,17.41f,50.47f),new string[0],new string[0],new (string,Vector3,float)[0]),
                ("s53-014-gate-head",new(8f,2.48f,27.46f),new(2.89f,15.01f,14.23f),new string[0],new string[0],new (string,Vector3,float)[0]),
                ("s51-149-table",new(0,7.28f,107.31f),new(-5.11f,19.78f,92.46f),new[]{"The Warmaster at his table"},new string[0],new (string,Vector3,float)[0]),
                ("s52-339-cells",new(55.92f,1.08f,107.8f),new(50.66f,13.61f,93.39f),new[]{"Drask is in his cell target","The cell's guards"},new[]{"Townsperson, the garrison's gaoler"},new (string,Vector3,float)[0]),
                ("s50-052-gate-door",new(14.83f,.08f,13.19f),new(9.32f,12.61f,-.86f),new string[0],new string[0],new (string,Vector3,float)[0]),
                ("s50-027-parley",new(1.57f,.08f,10.34f),new(-4.34f,12.61f,-3.8f),new string[0],new string[0],new (string,Vector3,float)[0]),
                ("s51-107-salt",new(44.09f,2.48f,48.59f),new(38.6f,15.01f,33.84f),new string[0],new string[0],new (string,Vector3,float)[0]),
                ("s51-208-flour",new(30.6f,.08f,10.81f),new(25.48f,12.61f,-3.63f),new string[0],new string[0],new (string,Vector3,float)[0]),
                ("s51-123-bastion",new(52.47f,4.88f,119.7f),new(47.04f,17.41f,105.42f),new string[0],new string[0],new (string,Vector3,float)[0]),
                ("s50-201-kitchen-door",new(-24.98f,2.48f,40.48f),new(-30.05f,15.01f,27.25f),new string[0],new string[0],new (string,Vector3,float)[0]),
                ("s50-131-gallery-walk",new(-30.65f,4.88f,103.74f),new(-35.1f,17.41f,89.22f),new string[0],new string[0],new (string,Vector3,float)[0]),
                ("s51-159-portico",new(-7.82f,7.28f,108.2f),new(-12.48f,19.81f,94.16f),new[]{"The Warmaster at his table"},new string[0],new (string,Vector3,float)[0]),
                // Not stills of the capture: the whole field with its standards, drawn back as the duel draws the camera back, and the cook with her pot at her errand's first stop.
                ("field-wide",new(0,4.8f,76),new(-11.2f,32.4f,46.9f),new[]{"The town watching"},new[]{"The field at drill"},new (string,Vector3,float)[0]),
                ("cook-with-pot",new(-19,2.48f,35.2f),new(-24.1f,15f,22f),new string[0],new string[0],new (string,Vector3,float)[0]),
                ("neighbour-with-pot",new(-6.4f,2.48f,41.4f),new(-11.5f,15f,28.2f),new string[0],new string[0],new (string,Vector3,float)[0]),
            };
            internal static void Measure()
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+TownZone+".unity");Physics.SyncTransforms();
                // Who faces nothing: the town as a player first finds it (what is shown before any flag), the test's rule.
                var standing=UnityEngine.Object.FindObjectsByType<PropTint>(FindObjectsSortMode.None).Where(t=>t.gameObject.activeInHierarchy&&(t.name.StartsWith("Townsperson")||t.name.StartsWith("A child"))&&!t.name.Contains("watching")).Select(t=>t.transform).ToArray();
                var people=standing.Select(s=>s.position).Concat(UnityEngine.Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).Where(n=>n.gameObject.activeInHierarchy).Select(n=>n.transform.position))
                    .Concat(UnityEngine.Object.FindObjectsByType<WalkLoop>(FindObjectsSortMode.None).Where(w=>w.gameObject.activeInHierarchy).Select(w=>w.transform.position)).Concat(UnityEngine.Object.FindObjectsByType<KeepsPace>(FindObjectsSortMode.None).Select(k=>k.transform.position)).ToArray();
                var facingNothing=new List<string>();
                foreach(var one in standing)
                {
                    var at=one.position;var ahead=one.forward;ahead.y=0;ahead.Normalize();
                    bool someone=people.Any(p=>{var to=p-at;to.y=0;return to.magnitude>.3f&&to.magnitude<4.6f&&Vector3.Angle(ahead,to)<55;});bool something=false;
                    foreach(float up in new[]{.5f,1.2f})foreach(var hit in Physics.RaycastAll(at+Vector3.up*up+ahead*.45f,ahead,4.2f,~0,QueryTriggerInteraction.Ignore))if(!hit.collider.transform.IsChildOf(one))something=true;
                    if(!someone&&!something)facingNothing.Add($"{one.name} ({at.x:0.0},{at.z:0.0})");
                }
                Debug.Log($"TOWN_MEASURE facing-nothing={facingNothing.Count} of {standing.Length}: {string.Join("; ",facingNothing)}");
                bool Over(Bounds b,float x0,float x1,float z0,float z1)=>b.max.x>x0&&b.min.x<x1&&b.max.z>z0&&b.min.z<z1;
                var flat=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.name.StartsWith("Floor detail")&&r.bounds.size.x>1&&r.bounds.size.z>1
                    &&(Over(r.bounds,-FieldHalfX,FieldHalfX,FieldSouth,FieldNorth)||Over(r.bounds,-48,42,33.5f,36.5f)||Over(r.bounds,-48,39.6f,64.5f,67.5f))&&r.sharedMaterials.Any(m=>m==null||m.GetTexture("_BaseMap")==null)).Select(r=>r.name).ToArray();
                Debug.Log($"TOWN_MEASURE flat-colour-floor-marks={flat.Length}: {string.Join("; ",flat)}");
                var guards=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name=="Townsperson, an Unsworn smith on guard").Select(t=>Vector2.Distance(new Vector2(t.position.x,t.position.z),CellFront)).ToArray();
                Debug.Log($"TOWN_MEASURE cell-guards-from-cell-front={string.Join(",",guards.Select(d=>d.ToString("0.00")))}");
                var water=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Single(r=>r.name=="Harbour water").sharedMaterial;var ground=ZoneKit.Find<CameraProfile>("Ground");
                float perMetre=1080/(2*ground.distance*Mathf.Tan(ground.fov*.5f*Mathf.Deg2Rad)),cell=1/(water.GetVector("_WaterParams").x*1.9f);
                Debug.Log($"TOWN_MEASURE ripple-px={cell*perMetre:0.0}");
                var cellWhere=UnityEngine.Object.FindObjectsByType<Whereabouts>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(w=>w.targets.Any(t=>t!=null&&t.GetComponentInChildren<Npc>(true)!=null&&t.GetComponentInChildren<Npc>(true).speaker=="Drask")).Select(w=>$"{w.name} from={w.from} gone={w.goneAt} live={w.live}").ToArray();
                Debug.Log($"TOWN_MEASURE drask-whereabouts: {string.Join("; ",cellWhere)}");
                string Shapes(GameObject go)=>string.Join(",",go.GetComponentsInChildren<Renderer>(true).Select(r=>r is SkinnedMeshRenderer s?s.sharedMesh:r.TryGetComponent<MeshFilter>(out var f)?f.sharedMesh:null).Where(m=>m!=null).Select(m=>m.name+"("+m.vertexCount+")").Distinct().OrderBy(n=>n));
                var duel=ZoneKit.Find<EnemyDef>("DraskVaal");var guard=ZoneKit.Find<EnemyDef>("MusterPikeguard");var person=ZoneKit.Find<CharacterDef>("Drask");
                Debug.Log($"TOWN_MEASURE duel-body={duel.prefab.name}[{Shapes(duel.prefab)}] x{duel.drawnScale} guard-body={guard.prefab.name}[{Shapes(guard.prefab)}] x{guard.drawnScale} person-body={(person.natural!=null?person.natural.name+"["+Shapes(person.natural)+"]":"none")}");
            }
            internal static void Probe(string folder,string only)
            {
                Directory.CreateDirectory(folder);
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+TownZone+".unity");
                foreach(var text in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))text.gameObject.SetActive(false);
                var def=ZoneKit.Find<ZoneDef>(TownZone);if(def!=null)RenderSettings.skybox=def.skybox;
                var ground=ZoneKit.Find<CameraProfile>("Ground");
                var all=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                // The cell's Warmaster and his guards hang off Whereabouts holders: the probe names them by what holds them.
                GameObject Named(string name)
                {
                    if(name=="Drask is in his cell target"){var w=UnityEngine.Object.FindObjectsByType<Whereabouts>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(x=>x.name=="Drask is in his cell");return w!=null&&w.targets.Length>0?w.targets[0]:null;}
                    if(name=="The cell's guards")return null;
                    var t=all.FirstOrDefault(x=>x!=null&&x.name==name);return t!=null?t.gameObject:null;
                }
                var camera=new GameObject("Probe camera",typeof(Camera),typeof(UniversalAdditionalCameraData)).GetComponent<Camera>();
                camera.clearFlags=RenderSettings.skybox!=null?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;camera.nearClipPlane=.3f;camera.farClipPlane=1500;camera.fieldOfView=ground.fov;
                var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.None;
                int count=0;
                foreach(var shot in Shots)
                {
                    if(!string.IsNullOrEmpty(only)&&!only.Split(',').Contains(shot.name))continue;
                    var changed=new List<(GameObject go,bool was)>();
                    void Set(GameObject go,bool on){if(go==null)return;changed.Add((go,go.activeSelf));go.SetActive(on);}
                    foreach(var name in shot.show){Set(Named(name),true);if(name=="The cell's guards")foreach(var w in UnityEngine.Object.FindObjectsByType<Whereabouts>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(x=>x.name=="The cell's guard"))foreach(var g in w.targets)Set(g,true);}
                    foreach(var name in shot.hide)Set(Named(name),false);
                    var made=new List<GameObject>();
                    foreach(var (enemy,at,yaw) in shot.spawn)
                    {
                        var e=ZoneKit.Find<EnemyDef>(enemy);if(e==null||e.prefab==null)continue;var body=(GameObject)PrefabUtility.InstantiatePrefab(e.prefab);
                        body.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));if(e.drawnScale>0)body.transform.localScale*=e.drawnScale;made.Add(body);
                    }
                    foreach(var (prefab,offset,yaw) in new[]{("TarenNatural",Vector3.zero,160f),("IdraNatural",new Vector3(1.4f,0,-1.1f),150f)})
                    {
                        var p=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/"+prefab+".prefab");if(p==null)continue;var hero=(GameObject)PrefabUtility.InstantiatePrefab(p);
                        var at=shot.hero+offset;if(Physics.Raycast(at+Vector3.up*3,Vector3.down,out var hit,8,~0,QueryTriggerInteraction.Ignore))at=hit.point;
                        hero.transform.SetPositionAndRotation(at,Quaternion.Euler(0,yaw,0));foreach(var b in hero.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;made.Add(hero);
                    }
                    foreach(var animator in UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
                    {
                        if(animator.GetComponentInParent<Posed>()!=null)continue;
                        var clips=animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.animationClips:new AnimationClip[0];var idle=clips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("idle"));
                        if(idle!=null)idle.SampleAnimation(animator.gameObject,.3f);
                    }
                    camera.transform.SetPositionAndRotation(shot.camera,Quaternion.Euler(ground.pitch,ground.yaw,0));
                    var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=4};camera.targetTexture=rt;camera.Render();camera.Render();
                    var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                    File.WriteAllBytes(Path.Combine(folder,shot.name+".png"),image.EncodeToPNG());RenderTexture.active=prior;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
                    foreach(var go in made)UnityEngine.Object.DestroyImmediate(go);
                    for(int i=changed.Count-1;i>=0;i--)changed[i].go.SetActive(changed[i].was);
                    count++;
                }
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
                Debug.Log($"TOWN_PROBE count={count} folder={folder} scene={scene.name}");
            }
        }
    }
}
