using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>
    /// A heightfield landform as three meshes: the collision heightfield (a plain welded grid, which navigation and the shadow pass read), the walkable floor and the rock that rises
    /// out of it. Floor and rock used to be the grid's own triangles sorted by steepness, so the line between them ran along cell edges and diagonals: a saw of hard teeth along every
    /// cliff foot that finer cells only made smaller (E1-lanternwick-13), and a chopped wedge where a ledge's face tapered out. Here each grid point is floor or rock by the height
    /// function's own slope there, and a cell with both is cut along the line where the slope crosses over (found on the function itself, not interpolated), so the foot of a cliff
    /// is one smooth line whatever the cell size. Both drawn meshes share those points, are welded, take their normals from the function and carry byte colours: the gorge's rock was
    /// 61 MB as unwelded triangles and is a fifth of that.
    /// </summary>
    public static class GroundTerrain
    {
        /// <summary>Ground whose normal is at least this upright is floor, as steep as a body can stand on; past it, rock.</summary>
        public const float FloorNormal=.78f;
        /// <summary>The toon ramp bands the sun, and a floor lit near a step of the ramp changes band on the slightest bump: Lanternwick's low sun meets level ground at .53 against a
        /// step at .5, so a rise two degrees off the sun dropped a whole band and left a hard, stair-stepped dark shape on the floor with nothing casting it. Ground this gentle is lit as level.</summary>
        public const float LevelFloorY=.97f;
        const float FootDark=.62f,FootReach=2.6f;
        /// <summary>A cut never lands closer than this share of an edge to a grid point: a sliver's worth of floor there would be a triangle too thin to light.</summary>
        const float NearestCut=.06f;

        public sealed class Landform
        {
            public Mesh collision,floor,cliffs;
            /// <summary>Cells cut along the foot line.</summary>
            public int cut;
        }

        /// <summary>Floor and strata meet along a line, and at full contrast that line was the hardest thing on the screen. Both darken toward it, as ground does where a wall rises out
        /// of it, so the line sits in shade. Worked out per point of the grid, so the two meshes agree exactly where they meet.</summary>
        static float[] FootShading(Vector3[] grid,int nx,int nz,float cell)
        {
            var shade=new float[grid.Length];int reach=Mathf.Max(1,Mathf.RoundToInt(FootReach/cell));
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
            {
                float y=grid[z*nx+x].y,low=y,high=y;
                for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)
                {
                    float other=grid[Mathf.Clamp(z+dz*reach,0,nz-1)*nx+Mathf.Clamp(x+dx*reach,0,nx-1)].y;
                    low=Mathf.Min(low,other);high=Mathf.Max(high,other);
                }
                shade[z*nx+x]=Mathf.Lerp(1,FootDark,Mathf.InverseLerp(.5f,2.4f,high-y)*(1-Mathf.InverseLerp(.2f,2.4f,y-low)));
            }
            return shade;
        }
        static Color32 Shaded(Color tone,float by)=>new Color(tone.r*by,tone.g*by,tone.b*by,tone.a);

        /// <summary>The landform over a rectangle of ground. A tone colours the floor's vertices, and a wall top darkens the walls toward their tops from a wall foot, white unless it's given.
        /// Level ground above <paramref name="cliffTop"/> is "above the cliffs": rock, not floor. <paramref name="floorWithin"/>, where a zone knows its own floors, is positive
        /// on them and negative off them (a distance will do): whatever lies outside is rock however gently it slopes. Without it a bank drawn at an easy pitch below
        /// <paramref name="cliffTop"/> is floor to this, textured as floor and scattered with the floor's grass, where nobody can walk (the Walking Canopy's near walls, once they were
        /// drawn under the camera's line: November 13).</summary>
        public static Landform Build(string label,float xMin,float xMax,float zMin,float zMax,float cell,Func<Vector2,float> height,float cliffTop,Func<Vector2,Color> floorTone=null,Color? wallTop=null,Color? wallFoot=null,Func<Vector2,float> floorWithin=null)
        {
            int nx=Mathf.CeilToInt((xMax-xMin)/cell)+1,nz=Mathf.CeilToInt((zMax-zMin)/cell)+1,count=nx*nz;
            var grid=new Vector3[count];var triangles=new int[(nx-1)*(nz-1)*6];int ti=0;
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++){var p=new Vector2(xMin+x*cell,zMin+z*cell);grid[z*nx+x]=new Vector3(p.x,height(p),p.y);}
            // Each cell's diagonal runs between its two nearest heights, along the ground's contour. With one fixed diagonal, a wall running across it broke at its foot into single triangles.
            bool Along(int i)=>Mathf.Abs(grid[i].y-grid[i+nx+1].y)<Mathf.Abs(grid[i+1].y-grid[i+nx].y);
            for(int z=0;z<nz-1;z++)for(int x=0;x<nx-1;x++)
            {
                int i=z*nx+x;
                if(Along(i)){triangles[ti++]=i;triangles[ti++]=i+nx;triangles[ti++]=i+nx+1;triangles[ti++]=i;triangles[ti++]=i+nx+1;triangles[ti++]=i+1;}
                else{triangles[ti++]=i;triangles[ti++]=i+nx;triangles[ti++]=i+1;triangles[ti++]=i+1;triangles[ti++]=i+nx;triangles[ti++]=i+nx+1;}
            }
            var collision=new Mesh{name=label+" collision",indexFormat=IndexFormat.UInt32};collision.vertices=grid;collision.triangles=triangles;collision.RecalculateNormals();collision.RecalculateBounds();
            // The grid is also what a landform's shadow is cast from, on a renderer of its own, and a mesh with no uvs drawn under a textured material is one flat colour from a
            // single texel (QaChapterTwoLookTests.NothingDrawsAMeshWithNoUvsOverATexture). It carries the floor's plan, so whatever draws it is textured as the ground is.
            var plan=new Vector2[count];for(int i=0;i<count;i++)plan[i]=new Vector2(grid[i].x,grid[i].z)/8;collision.uv=plan;

            // ---- what's floor and what's rock, from the function itself
            float reach=cell*.5f;
            Vector3 Normal(Vector2 p)
            {
                float gx=(height(p+Vector2.right*reach)-height(p-Vector2.right*reach))/(2*reach),gz=(height(p+Vector2.up*reach)-height(p-Vector2.up*reach))/(2*reach);
                return new Vector3(-gx,1,-gz).normalized;
            }
            // Positive on floor. Never exactly nothing, so every point is one or the other.
            float Field(Vector2 p){float f=Mathf.Min(Normal(p).y-FloorNormal,(cliffTop-height(p))*.2f);if(floorWithin!=null)f=Mathf.Min(f,floorWithin(p));return f==0?1e-6f:f;}
            var field=new float[count];for(int i=0;i<count;i++)field[i]=Field(new Vector2(grid[i].x,grid[i].z));
            var foot=FootShading(grid,nx,nz,cell);

            // ---- the points both meshes are made of: grid points by their index, and a cut on a grid edge by the edge
            var cutAt=new Dictionary<int,(Vector2 p,float foot)>();
            int Cut(int a,int b)
            {
                // Edges are named by their lower point: twice its index along x, one more along z.
                int low=Mathf.Min(a,b),high=Mathf.Max(a,b),key=count+low*2+(high-low==1?0:1);
                if(cutAt.ContainsKey(key))return key;
                Vector2 from=new(grid[low].x,grid[low].z),to=new(grid[high].x,grid[high].z);float lo=0,hi=1;bool floorAtLow=field[low]>0;
                for(int step=0;step<14;step++){float mid=(lo+hi)*.5f;if(Field(Vector2.Lerp(from,to,mid))>0==floorAtLow)lo=mid;else hi=mid;}
                float t=Mathf.Clamp((lo+hi)*.5f,NearestCut,1-NearestCut);
                cutAt[key]=(Vector2.Lerp(from,to,t),Mathf.Lerp(foot[low],foot[high],t));return key;
            }
            Vector2 Plan(int id)=>id<count?new Vector2(grid[id].x,grid[id].z):cutAt[id].p;
            Vector3 Point(int id){if(id<count)return grid[id];var p=cutAt[id].p;return new Vector3(p.x,height(p),p.y);}
            float Foot(int id)=>id<count?foot[id]:cutAt[id].foot;

            var floorPoints=new List<Vector3>();var floorNormals=new List<Vector3>();var floorTones=new List<Color32>();var floorUv=new List<Vector2>();var floorTris=new List<int>();var floorIndex=new Dictionary<int,int>();
            var cliffPoints=new List<Vector3>();var cliffNormals=new List<Vector3>();var cliffTones=new List<Color32>();var cliffUv=new List<Vector2>();var cliffTris=new List<int>();var cliffIndex=new Dictionary<long,int>();
            int FloorVertex(int id)
            {
                if(floorIndex.TryGetValue(id,out int at))return at;
                var p=Plan(id);var n=Normal(p);
                floorIndex[id]=at=floorPoints.Count;floorPoints.Add(Point(id));floorNormals.Add(n.y>=LevelFloorY?Vector3.up:n);
                floorTones.Add(Shaded(floorTone!=null?floorTone(p):Color.white,Foot(id)));floorUv.Add(p/8);return at;
            }
            // A wall's texture is laid along it, so its bands stay level; a level top takes the floor's plan. One point can stand in faces of each kind, with a coordinate for each.
            int CliffVertex(int id,int lay)
            {
                long key=(long)id*3+lay;if(cliffIndex.TryGetValue(key,out int at))return at;
                var v=Point(id);
                cliffIndex[key]=at=cliffPoints.Count;cliffPoints.Add(v);cliffNormals.Add(Normal(Plan(id)));
                cliffUv.Add(lay==2?new Vector2(v.x,v.z)/8:new Vector2((lay==0?v.z:v.x)/7,v.y/5));
                var wall=wallTop is Color top?Color.Lerp(wallFoot??Color.white,top,lay==2?1:Mathf.InverseLerp(.5f,5,v.y)):Color.white;
                cliffTones.Add(Shaded(wall,Foot(id)));return at;
            }
            void Face(bool floor,int a,int b,int c)
            {
                Vector3 pa=Point(a),pb=Point(b),pc=Point(c);var n=Vector3.Cross(pb-pa,pc-pa);
                if(n.sqrMagnitude<1e-10f)return;
                if(floor){floorTris.Add(FloorVertex(a));floorTris.Add(FloorVertex(b));floorTris.Add(FloorVertex(c));return;}
                n.Normalize();int lay=Mathf.Abs(n.y)>=FloorNormal?2:Mathf.Abs(n.x)>Mathf.Abs(n.z)?0:1;
                cliffTris.Add(CliffVertex(a,lay));cliffTris.Add(CliffVertex(b,lay));cliffTris.Add(CliffVertex(c,lay));
            }
            void Fan(bool floor,List<int> ring){for(int k=1;k+1<ring.Count;k++)Face(floor,ring[0],ring[k],ring[k+1]);}

            int cutCells=0;var around=new List<int>(8);var part=new List<int>(8);
            for(int z=0;z<nz-1;z++)for(int x=0;x<nx-1;x++)
            {
                // Clockwise from above: this point, the one north of it, north-east, east.
                int a=z*nx+x,d=a+nx,c=d+1,b=a+1;bool fa=field[a]>0,fd=field[d]>0,fc=field[c]>0,fb=field[b]>0;
                if(fa==fd&&fd==fc&&fc==fb)
                {
                    if(Along(a)){Face(fa,a,d,c);Face(fa,a,c,b);}else{Face(fa,a,d,b);Face(fa,b,d,c);}
                    continue;
                }
                cutCells++;
                if(fa==fc&&fd==fb)
                {
                    // Opposite corners alike: two feet cross this cell. Whatever its middle is joins its two corners; the other two are cut off alone.
                    int ad=Cut(a,d),dc=Cut(d,c),cb=Cut(c,b),ba=Cut(b,a);
                    bool middle=Field((Plan(a)+Plan(c))*.5f)>0;
                    if(middle==fa){Fan(fa,new List<int>{a,ad,dc,c,cb,ba});Face(fd,ad,d,dc);Face(fb,cb,b,ba);}
                    else{Fan(fd,new List<int>{d,dc,cb,b,ba,ad});Face(fa,ba,a,ad);Face(fc,dc,c,cb);}
                    continue;
                }
                // One foot crosses the cell: walk round it, and each run of corners between the two cuts is a piece.
                around.Clear();
                void Corner(int from,int to,bool fromFloor,bool toFloor){around.Add(from);if(fromFloor!=toFloor)around.Add(Cut(from,to));}
                Corner(a,d,fa,fd);Corner(d,c,fd,fc);Corner(c,b,fc,fb);Corner(b,a,fb,fa);
                int first=around.FindIndex(id=>id>=count);
                for(int piece=0;piece<2;piece++)
                {
                    part.Clear();part.Add(around[first]);int k=(first+1)%around.Count;
                    while(around[k]<count){part.Add(around[k]);k=(k+1)%around.Count;}
                    part.Add(around[k]);
                    Fan(field[part[1]]>0,part);first=k;
                }
            }

            var floorMesh=new Mesh{name=label,indexFormat=floorPoints.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};
            floorMesh.SetVertices(floorPoints);floorMesh.SetNormals(floorNormals);floorMesh.SetColors(floorTones);floorMesh.SetUVs(0,floorUv);floorMesh.SetTriangles(floorTris,0);floorMesh.RecalculateBounds();
            var cliffMesh=new Mesh{name=label+" cliffs",indexFormat=cliffPoints.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};
            cliffMesh.SetVertices(cliffPoints);cliffMesh.SetNormals(cliffNormals);cliffMesh.SetColors(cliffTones);cliffMesh.SetUVs(0,cliffUv);cliffMesh.SetTriangles(cliffTris,0);cliffMesh.RecalculateBounds();
            Debug.Log($"GROUND_TERRAIN {label} grid={nx}x{nz} cut={cutCells} floor={floorPoints.Count}v/{floorTris.Count/3}t cliffs={cliffPoints.Count}v/{cliffTris.Count/3}t teeth={ToothShare(floorMesh):0.000}");
            return new Landform{collision=collision,floor=floorMesh,cliffs=cliffMesh,cut=cutCells};
        }

        /// <summary>The share of the points along a floor's edge where the edge turns back on itself by more than a right angle: QaWorldThirdTests' measure of the teeth, here so a build states it.</summary>
        public static float ToothShare(Mesh floor)
        {
            var at=floor.vertices;var tri=floor.triangles;var uses=new Dictionary<long,int>();
            void Use(int a,int b){long key=Math.Min(a,b)*(long)at.Length+Math.Max(a,b);uses.TryGetValue(key,out int n);uses[key]=n+1;}
            for(int t=0;t<tri.Length;t+=3){Use(tri[t],tri[t+1]);Use(tri[t+1],tri[t+2]);Use(tri[t+2],tri[t]);}
            var links=new Dictionary<int,List<int>>();
            foreach(var pair in uses)
            {
                if(pair.Value!=1)continue;
                int a=(int)(pair.Key/at.Length),b=(int)(pair.Key%at.Length);
                if(!links.TryGetValue(a,out var la))links[a]=la=new List<int>();la.Add(b);
                if(!links.TryGetValue(b,out var lb))links[b]=lb=new List<int>();lb.Add(a);
            }
            int points=0,teeth=0;
            foreach(var pair in links)
            {
                if(pair.Value.Count!=2)continue;
                var one=at[pair.Value[0]]-at[pair.Key];var two=at[pair.Value[1]]-at[pair.Key];one.y=two.y=0;points++;
                if(Vector3.Dot(one.normalized,two.normalized)>-.17f)teeth++;
            }
            return teeth/(float)Mathf.Max(1,points);
        }
    }
}
