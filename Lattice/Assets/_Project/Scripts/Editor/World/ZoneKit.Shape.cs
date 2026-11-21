using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>What a builder can make without waiting for art (guide 4.2): a mesh put together from boards, tubes, cones, discs and lumps, tinted in its vertices so one material serves
    /// a whole town's timber. A region's builder says what the shapes are (a jetty, a buttress root, a cracked mirror); this only knows geometry.</summary>
    public sealed partial class ZoneKit
    {
        public sealed class Shape
        {
            readonly List<Vector3> points=new(),normals=new();readonly List<Vector2> uv=new();readonly List<Color32> tones=new();readonly List<List<int>> faces=new(){new List<int>()};
            int slot;
            public int Vertices=>points.Count;
            /// <summary>A tone darker or lighter, its alpha left alone: vertex alpha is the material's to read.</summary>
            public static Color Dim(Color tone,float by)=>new(tone.r*by,tone.g*by,tone.b*by,tone.a);
            /// <summary>What follows goes in this material slot of the mesh.</summary>
            public Shape Slot(int index){while(faces.Count<=index)faces.Add(new List<int>());slot=index;return this;}
            int Add(Vector3 p,Vector3 n,Vector2 t,Color tone){points.Add(p);normals.Add(n);uv.Add(t);tones.Add(tone);return points.Count-1;}
            void Tri(int a,int b,int c,Vector3 outward)
            {
                // Unity's front face is clockwise: cross(b-a,c-a) points at whoever sees it that way.
                bool front=Vector3.Dot(Vector3.Cross(points[b]-points[a],points[c]-points[a]),outward)>=0;
                faces[slot].Add(a);faces[slot].Add(front?b:c);faces[slot].Add(front?c:b);
            }
            /// <summary>A flat four-sided face seen from <paramref name="outward"/>, its texture laid at <paramref name="texel"/> repeats a metre along the face's own two directions.</summary>
            public Shape Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 outward,Color tone,float texel=.5f)
            {
                var n=Vector3.Cross(b-a,c-a);if(n.sqrMagnitude<1e-12f)n=Vector3.Cross(c-a,d-a);n.Normalize();if(Vector3.Dot(n,outward)<0)n=-n;
                var along=(b-a).normalized;var up=Vector3.Cross(n,along);
                Vector2 Map(Vector3 p)=>new(Vector3.Dot(p,along)*texel,Vector3.Dot(p,up)*texel);
                int i=Add(a,n,Map(a),tone);Add(b,n,Map(b),tone);Add(c,n,Map(c),tone);Add(d,n,Map(d),tone);
                Tri(i,i+1,i+2,n);Tri(i,i+2,i+3,n);return this;
            }
            public Shape Triangle(Vector3 a,Vector3 b,Vector3 c,Vector3 outward,Color tone,float texel=.5f)
            {
                var n=Vector3.Cross(b-a,c-a).normalized;if(Vector3.Dot(n,outward)<0)n=-n;
                var along=(b-a).normalized;var up=Vector3.Cross(n,along);
                Vector2 Map(Vector3 p)=>new(Vector3.Dot(p,along)*texel,Vector3.Dot(p,up)*texel);
                int i=Add(a,n,Map(a),tone);Add(b,n,Map(b),tone);Add(c,n,Map(c),tone);Tri(i,i+1,i+2,n);return this;
            }
            /// <summary>A box about its centre. Its top can take another tone: a deck's boards are paler than its frame.</summary>
            public Shape Box(Vector3 centre,Vector3 size,Quaternion turn,Color tone,Color? top=null,float texel=.5f)
            {
                Vector3 x=turn*Vector3.right*size.x*.5f,y=turn*Vector3.up*size.y*.5f,z=turn*Vector3.forward*size.z*.5f;
                Quad(centre+y-x-z,centre+y-x+z,centre+y+x+z,centre+y+x-z,y,top??tone,texel);
                Quad(centre-y-x-z,centre-y+x-z,centre-y+x+z,centre-y-x+z,-y,tone,texel);
                Quad(centre-z-x-y,centre-z-x+y,centre-z+x+y,centre-z+x-y,-z,tone,texel);
                Quad(centre+z-x-y,centre+z+x-y,centre+z+x+y,centre+z-x+y,z,tone,texel);
                Quad(centre-x-z-y,centre-x+z-y,centre-x+z+y,centre-x-z+y,-x,tone,texel);
                Quad(centre+x-z-y,centre+x-z+y,centre+x+z+y,centre+x+z-y,x,tone,texel);
                return this;
            }
            /// <summary>A squared timber from one point to another: a rail, a joist, a pole lying down.</summary>
            public Shape Beam(Vector3 from,Vector3 to,float wide,float thick,Color tone,Color? top=null,float texel=.5f)
            {
                var along=to-from;if(along.sqrMagnitude<1e-8f)return this;
                var turn=Quaternion.LookRotation(along.normalized,Mathf.Abs(along.normalized.y)>.98f?Vector3.forward:Vector3.up);
                return Box((from+to)*.5f,new Vector3(wide,thick,along.magnitude),turn,tone,top,texel);
            }
            /// <summary>A walkway of single boards laid across its run, each a little off the last in tone and length so it reads as carpentry from above. Its top is at the two points given.</summary>
            public Shape Deck(Vector3 from,Vector3 to,float wide,float thick,Color tone,System.Random rng,float board=.42f,float gap=.04f)
            {
                var run=to-from;float length=run.magnitude;if(length<.01f)return this;var along=run/length;
                var turn=Quaternion.LookRotation(along,Vector3.up);int count=Mathf.Max(1,Mathf.RoundToInt(length/board));float each=length/count;
                for(int i=0;i<count;i++)
                {
                    float shade=.86f+.2f*(float)rng.NextDouble(),over=(float)rng.NextDouble()*.14f;
                    var centre=from+along*((i+.5f)*each)+Vector3.down*thick*.5f+turn*Vector3.right*((float)rng.NextDouble()-.5f)*.08f;
                    Box(centre,new Vector3(wide+over,thick,each-gap),turn,Dim(tone,.6f),Dim(tone,shade),.5f);
                }
                return this;
            }
            /// <summary>A round member along a path, as thick at each point as <paramref name="radii"/> says: a trunk, a root, a post, a rope. Smooth round it, tinted from one end to the other.</summary>
            public Shape Tube(IReadOnlyList<Vector3> path,IReadOnlyList<float> radii,int sides,Color from,Color to,float around=1,float perMetre=.25f,bool capped=true)
            {
                int rings=path.Count;if(rings<2)return this;int start=points.Count;float run=0;
                var u=Vector3.zero;
                for(int i=0;i<rings;i++)
                {
                    var t=(path[Mathf.Min(rings-1,i+1)]-path[Mathf.Max(0,i-1)]).normalized;
                    if(i==0){u=Vector3.Cross(t,Mathf.Abs(t.y)>.9f?Vector3.right:Vector3.up).normalized;}
                    else{u=(u-t*Vector3.Dot(u,t)).normalized;run+=Vector3.Distance(path[i],path[i-1]);}
                    var v=Vector3.Cross(t,u);float r=radii[Mathf.Min(i,radii.Count-1)];
                    // The face leans with the taper: a cone lit as a cylinder is one flat band all the way up.
                    float slope=i<rings-1?(r-radii[Mathf.Min(i+1,radii.Count-1)])/Mathf.Max(.001f,Vector3.Distance(path[i+1],path[i])):(radii[Mathf.Min(i-1,radii.Count-1)]-r)/Mathf.Max(.001f,Vector3.Distance(path[i],path[i-1]));
                    var tone=Color.Lerp(from,to,i/(float)(rings-1));
                    for(int k=0;k<=sides;k++)
                    {
                        float a=k*Mathf.PI*2/sides;var radial=u*Mathf.Cos(a)+v*Mathf.Sin(a);
                        Add(path[i]+radial*r,(radial+t*slope).normalized,new Vector2(k/(float)sides*around,run*perMetre),tone);
                    }
                }
                for(int i=0;i<rings-1;i++)for(int k=0;k<sides;k++)
                {
                    int a=start+i*(sides+1)+k,b=a+1,c=a+sides+1,d=c+1;var outward=normals[a]+normals[d];
                    Tri(a,c,d,outward);Tri(a,d,b,outward);
                }
                if(!capped)return this;
                foreach(int end in new[]{0,rings-1})
                {
                    float r=radii[Mathf.Min(end,radii.Count-1)];if(r<.02f)continue;
                    var outward=end==0?path[0]-path[1]:path[rings-1]-path[rings-2];outward.Normalize();
                    int centre=Add(path[end],outward,new Vector2(.5f,.5f),end==0?from:to),ring=points.Count;
                    for(int k=0;k<=sides;k++){var p=points[start+end*(sides+1)+k];Add(p,outward,new Vector2(.5f,.5f)+new Vector2(Mathf.Cos(k*Mathf.PI*2/sides),Mathf.Sin(k*Mathf.PI*2/sides))*.4f,end==0?from:to);}
                    for(int k=0;k<sides;k++)Tri(centre,ring+k,ring+k+1,outward);
                }
                return this;
            }
            /// <summary>A straight round post or drum.</summary>
            public Shape Post(Vector3 foot,float tall,float low,float high,int sides,Color from,Color to,float perMetre=.25f)
                =>Tube(new[]{foot,foot+Vector3.up*tall},new[]{low,high},sides,from,to,1,perMetre);
            /// <summary>A line slung between two points, hanging by <paramref name="sag"/> at its middle.</summary>
            public Shape Rope(Vector3 from,Vector3 to,float sag,float radius,Color tone,int sides=5,int spans=8)
            {
                var path=new Vector3[spans+1];var radii=new float[spans+1];
                for(int i=0;i<=spans;i++){float t=i/(float)spans;path[i]=Vector3.Lerp(from,to,t)+Vector3.down*sag*4*t*(1-t);radii[i]=radius;}
                return Tube(path,radii,sides,tone,tone,1,1.5f,false);
            }
            /// <summary>A flat round plate facing <paramref name="normal"/>, or a ring of one when <paramref name="inner"/> is given.</summary>
            public Shape Disc(Vector3 centre,Vector3 normal,float radius,int sides,Color tone,float inner=0,Color? rim=null,float texel=.25f)
            {
                normal.Normalize();var u=Vector3.Cross(normal,Mathf.Abs(normal.y)>.9f?Vector3.right:Vector3.up).normalized;var v=Vector3.Cross(normal,u);int start=points.Count;
                for(int k=0;k<=sides;k++)
                {
                    float a=k*Mathf.PI*2/sides;var radial=u*Mathf.Cos(a)+v*Mathf.Sin(a);
                    Add(centre+radial*inner,normal,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*inner*texel,tone);Add(centre+radial*radius,normal,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*texel,rim??tone);
                }
                for(int k=0;k<sides;k++){int a=start+k*2;Tri(a,a+1,a+3,normal);if(inner>0)Tri(a,a+3,a+2,normal);}
                return this;
            }
            /// <summary>A lump: a crown of leaves, a boulder, a seed pod, a knot of root. A sphere pushed in and out by <paramref name="rough"/> of its radius, darker underneath.</summary>
            public Shape Lump(Vector3 centre,Vector3 radii,int seed,float rough,Color top,Color under,int around=10,int rings=6,float texel=.25f,Quaternion? turned=null)
            {
                int start=points.Count;float s=seed*12.9898f;var turn=turned??Quaternion.identity;
                for(int j=0;j<=rings;j++)for(int k=0;k<=around;k++)
                {
                    float lat=j/(float)rings*Mathf.PI,lon=k%around/(float)around*Mathf.PI*2;
                    var dir=new Vector3(Mathf.Sin(lat)*Mathf.Cos(lon),Mathf.Cos(lat),Mathf.Sin(lat)*Mathf.Sin(lon));
                    float lump=1+(Mathf.PerlinNoise(dir.x*1.7f+s+dir.y*1.3f,dir.z*1.7f-s*.37f+dir.y*.9f)-.5f)*2*rough;
                    var p=turn*(Vector3.Scale(dir,radii)*lump);
                    Add(centre+p,turn*new Vector3(dir.x/Mathf.Max(.01f,radii.x),dir.y/Mathf.Max(.01f,radii.y),dir.z/Mathf.Max(.01f,radii.z)).normalized,new Vector2(k/(float)around*Mathf.Max(radii.x,radii.z)*6*texel,j/(float)rings*radii.y*3*texel),Color.Lerp(under,top,Mathf.SmoothStep(0,1,(turn*dir).y*.5f+.5f)));
                }
                for(int j=0;j<rings;j++)for(int k=0;k<around;k++)
                {
                    int a=start+j*(around+1)+k,b=a+1,c=a+around+1,d=c+1;var outward=points[a]+points[d]-centre*2;
                    if(j>0)Tri(a,b,d,outward);if(j<rings-1)Tri(a,d,c,outward);
                }
                return this;
            }
            /// <summary>The upper half of a flattened globe, its texture laid flat from above (no pole to pinch under a camera that looks down on it), going from one tone at the
            /// crown to another at the limb: a planet below a flight lane.</summary>
            public Shape Dome(Vector3 centre,float radius,float tall,int rings,int around,Color crown,Color limb)
            {
                int start=points.Count;
                for(int j=0;j<=rings;j++)for(int k=0;k<=around;k++)
                {
                    float r=Mathf.Sin(j/(float)rings*Mathf.PI*.5f),a=k%around*Mathf.PI*2/around,x=Mathf.Cos(a)*r,z=Mathf.Sin(a)*r,y=Mathf.Sqrt(Mathf.Max(0,1-r*r));
                    Add(centre+new Vector3(x*radius,y*tall,z*radius),new Vector3(x/radius,y/Mathf.Max(.01f,tall),z/radius).normalized,new Vector2(x,z)*.5f+Vector2.one*.5f,Color.Lerp(crown,limb,r*r*r*r));
                }
                for(int j=0;j<rings;j++)for(int k=0;k<around;k++)
                {
                    int a=start+j*(around+1)+k,b=a+1,c=a+around+1,d=c+1;
                    Tri(a,c,d,Vector3.up);if(j>0)Tri(a,d,b,Vector3.up);
                }
                return this;
            }
            /// <summary>One long leaf from its root: it rises, then droops past its middle, widest a third of the way out. Drawn by a two-sided material.</summary>
            public Shape Frond(Vector3 root,Vector3 direction,float length,float width,float rise,float droop,Color stem,Color tip,int spans=4)
            {
                direction.y=0;direction.Normalize();var side=Vector3.Cross(Vector3.up,direction);int start=points.Count;
                for(int i=0;i<=spans;i++)
                {
                    float t=i/(float)spans,w=width*.5f*Mathf.Sin(Mathf.PI*Mathf.Pow(t,.7f))+.01f;
                    var spine=root+direction*length*t+Vector3.up*(rise*t-droop*t*t);var tone=Color.Lerp(stem,tip,t);
                    var lean=new Vector3(-direction.x*(rise-2*droop*t),1,-direction.z*(rise-2*droop*t)).normalized;
                    Add(spine-side*w,lean,new Vector2(0,t),tone);Add(spine+side*w,lean,new Vector2(1,t),tone);
                }
                for(int i=0;i<spans;i++){int a=start+i*2;Tri(a,a+2,a+3,Vector3.up);Tri(a,a+3,a+1,Vector3.up);}
                return this;
            }
            /// <summary>A plant of fronds round one root: a fern underfoot, a palm's head, a leaf awning's fringe.</summary>
            public Shape Fern(Vector3 root,float size,int leaves,System.Random rng,Color stem,Color tip)
            {
                float turn=(float)rng.NextDouble()*360;
                for(int i=0;i<leaves;i++)
                {
                    float yaw=turn+i*360f/leaves+((float)rng.NextDouble()-.5f)*30,reach=size*(.7f+.5f*(float)rng.NextDouble());
                    Frond(root,Quaternion.Euler(0,yaw,0)*Vector3.forward,reach,reach*.32f,reach*.75f,reach*.7f,stem,Dim(tip,.85f+.3f*(float)rng.NextDouble()));
                }
                return this;
            }
            /// <summary>Nothing was added: a scatter whose every piece was kept out. Such a shape is not stood in the scene (an empty mesh has no uvs, and a textured
            /// material over it is what <c>NothingDrawsAMeshWithNoUvsOverATexture</c> finds).</summary>
            public bool Empty=>points.Count==0;
            public Mesh Mesh(string name)
            {
                var mesh=new Mesh{name=name,indexFormat=points.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16,subMeshCount=faces.Count};
                mesh.SetVertices(points);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetColors(tones);
                for(int i=0;i<faces.Count;i++)mesh.SetTriangles(faces[i],i);
                mesh.RecalculateBounds();return mesh;
            }
        }

        /// <summary>The lines along which a field over the plan crosses a level, as chains of points in order (a closed one ends where it began): the edge of a platform, the shore of a
        /// pool, a contour of a hill. Marching squares over a grid of <paramref name="cell"/> metres, each crossing placed along its grid edge by the field's own values, so a cell of
        /// half a metre gives a line smooth enough to lay a root or a kerb along. Chains come out in a fixed order for a fixed field.</summary>
        public static List<List<Vector2>> Contours(Func<Vector2,float> field,float level,float xMin,float xMax,float zMin,float zMax,float cell)
        {
            int nx=Mathf.CeilToInt((xMax-xMin)/cell)+1,nz=Mathf.CeilToInt((zMax-zMin)/cell)+1;var f=new float[nx*nz];
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){float v=field(new Vector2(xMin+x*cell,zMin+z*cell))-level;f[z*nx+x]=v==0?1e-6f:v;}
            var at=new Dictionary<long,Vector2>();var links=new Dictionary<long,List<long>>();var order=new List<long>();
            // A crossing is named by the grid edge it lies on: twice the lower point's index for the edge along x, one more for the edge along z.
            long Cross(int x0,int z0,bool alongX)
            {
                long id=((long)z0*nx+x0)*2+(alongX?0:1);if(at.ContainsKey(id))return id;
                int x1=alongX?x0+1:x0,z1=alongX?z0:z0+1;float a=f[z0*nx+x0],b=f[z1*nx+x1];
                at[id]=Vector2.Lerp(new Vector2(xMin+x0*cell,zMin+z0*cell),new Vector2(xMin+x1*cell,zMin+z1*cell),a/(a-b));links[id]=new List<long>(2);order.Add(id);return id;
            }
            void Link(long a,long b){links[a].Add(b);links[b].Add(a);}
            for(int z=0;z<nz-1;z++)for(int x=0;x<nx-1;x++)
            {
                bool a=f[z*nx+x]>0,b=f[z*nx+x+1]>0,c=f[(z+1)*nx+x+1]>0,d=f[(z+1)*nx+x]>0;
                if(a==b&&b==c&&c==d)continue;
                long south=a!=b?Cross(x,z,true):-1,east=b!=c?Cross(x+1,z,false):-1,north=d!=c?Cross(x,z+1,true):-1,west=a!=d?Cross(x,z,false):-1;
                if(south>=0&&east>=0&&north>=0&&west>=0)
                {
                    // Two lines cross this cell: whatever its middle is joins the two corners like it.
                    bool middle=f[z*nx+x]+f[z*nx+x+1]+f[(z+1)*nx+x+1]+f[(z+1)*nx+x]>0;
                    if(middle==a){Link(south,east);Link(north,west);}else{Link(south,west);Link(north,east);}
                    continue;
                }
                long one=-1,two=-1;foreach(long e in new[]{south,east,north,west})if(e>=0){if(one<0)one=e;else two=e;}
                if(two>=0)Link(one,two);
            }
            var chains=new List<List<Vector2>>();var seen=new HashSet<long>();
            void Walk(long from)
            {
                var chain=new List<Vector2>();long here=from,last=-1;
                while(true)
                {
                    chain.Add(at[here]);seen.Add(here);long next=-1;
                    foreach(long l in links[here])if(l!=last&&(!seen.Contains(l)||l==from&&chain.Count>2)){next=l;break;}
                    if(next<0)break;if(next==from){chain.Add(at[from]);break;}
                    last=here;here=next;
                }
                if(chain.Count>1)chains.Add(chain);
            }
            // Open lines from their ends first, then whatever is left is a loop.
            foreach(long id in order)if(!seen.Contains(id)&&links[id].Count==1)Walk(id);
            foreach(long id in order)if(!seen.Contains(id))Walk(id);
            return chains;
        }
        /// <summary>Stands a built shape in the scene, its mesh saved beside the scene as <c>Zone-name.asset</c>. Drawn only: give it colliders with <see cref="Block"/>, which is what
        /// the navigation bake and a body read. Its points are the world's, so the object stays at the origin.</summary>
        public GameObject Built(string name,Shape shape,params Material[] materials)
        {
            var mesh=SaveMesh(shape.Mesh(name),"Assets/_Project/Scenes/"+zone+"-"+name.Replace(' ','-').Replace("'","")+".asset");
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=materials;
            return go;
        }
        /// <summary>A solid static box under a parent: what stops a body and what the bake walks on or round. Never left without something drawn where it stands (no invisible walls in the open).
        /// A kit piece is an empty wrapper round its drawn body, so a box given to one goes under the body: a box with nothing drawn above it in the scene's tree is what the
        /// invisible-wall check takes for a wall nobody can see (the seed house's three were).</summary>
        public static BoxCollider Block(GameObject parent,Vector3 centre,Vector3 size,float yaw=0,string name="Block")
        {
            var under=parent.transform;
            if(parent.GetComponentInParent<Renderer>()==null){var drawn=parent.GetComponentInChildren<Renderer>();if(drawn!=null)under=drawn.transform;}
            var block=new GameObject(name,typeof(BoxCollider));block.isStatic=true;block.transform.SetParent(under,true);
            block.transform.SetPositionAndRotation(centre,Quaternion.Euler(0,yaw,0));var box=block.GetComponent<BoxCollider>();box.size=size;return box;
        }
        /// <summary>A solid box along a line between two points of the world: a rail, a kerb, a deck's footing.</summary>
        public static BoxCollider BlockAlong(GameObject parent,Vector3 from,Vector3 to,float wide,float tall,string name="Block")
        {
            var run=to-from;run.y=0;var mid=(from+to)*.5f;
            return Block(parent,mid,new Vector3(wide,tall,run.magnitude),Mathf.Atan2(run.x,run.z)*Mathf.Rad2Deg,name);
        }
    }
}
