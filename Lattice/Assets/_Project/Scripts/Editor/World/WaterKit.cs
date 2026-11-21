using System;
using System.Collections.Generic;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>
    /// The builder's half of the world's water (<see cref="WaterVolume"/> and what drives it: FlowSwitch, RisingWater, FloodBasin). A zone's ground is one height function, so water is simply
    /// a level over it: this makes the sheet that's drawn, tells the volume where it lies, and puts walls and holes in the paths where it is too deep to walk.
    /// <code>
    /// var look=WaterKit.Look("xylos-river",shallow,deep,foam);
    /// var sheet=WaterKit.Sheet(kit,"Middle channel",-10,190,60,120,1,0,look,InTheMiddleChannel);
    /// var water=WaterKit.Volume(sheet,0,(new Vector2(90,90),new Vector2(200,60),0));
    /// water.drainFlag="xylos.sluiceA";water.drainedLevel=-.7f;WaterKit.Deep(kit,water,-10,190,60,120,InTheMiddleChannel);
    /// </code>
    /// A sheet's second uv set carries, for each point, the ground's height and how deep the water stands over it. The toon shader's water reads the depth for its shore foam and its
    /// shallow-to-deep tint, and WaterVolume writes it again whenever the level moves, so a drained channel's shore is where its bed says. (ZoneKit.Water makes a sheet that never
    /// moves, by distance from the bank: use that for a pond, this for anything a switch, a flood or a fight will change.)
    /// </summary>
    public static class WaterKit
    {
        /// <summary>The public look (D131), read by true depth: clear over the shallows so the bed shows, going to an opaque deep colour over <paramref name="deepAt"/> metres of water, a thin
        /// line of foam where the ground comes up through it. Drawn after everything solid and before anything laid on the ground, so a danger marking lies on the water and not under it.
        /// Lives in Resources/WorldMaterials: the water variant stays in the build.</summary>
        public static Material Look(string name,Color shallow,Color deep,Color foam,float deepAt=.8f,float ripple=.5f,float sparkle=.3f)=>ZoneKit.Toon(name,null,m=>
        {
            ZoneKit.With(m,"_WATER_ON","_Water");m.SetColor("_BaseColor",shallow);m.SetColor("_DeepColor",deep);m.SetColor("_FoamColor",foam);
            m.SetVector("_WaterParams",new Vector4(ripple,.12f,deepAt,sparkle));m.SetVector("_EdgeParams",new Vector4(1,.1f,.05f,.5f));
            // Foam within a hand of the surface, its edge wandering by half that, and one faint lap line further out.
            m.SetVector("_FoamParams",new Vector4(.08f,.7f,.03f,.3f));m.SetFloat("_BaseMapStrength",.25f);m.SetFloat("_ReceiveShadows",1);
            m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);
            m.SetOverrideTag("RenderType","Transparent");m.renderQueue=(int)RenderQueue.Transparent-30;
        });

        /// <summary>A sheet of water over a rectangle of the plan at one level, kept only where <paramref name="within"/> allows and the ground is under the highest the water will ever
        /// stand (<paramref name="highest"/>; its level, unless it rises). The object sits at the level with its points at nought, so moving it is moving the water.
        /// A BiomePatch on it lets a licensed water take the mesh over locally by name; the public scene keeps this one.</summary>
        public static GameObject Sheet(ZoneKit kit,string name,float xMin,float xMax,float zMin,float zMax,float cell,float level,Material material,Func<Vector2,bool> within=null,float highest=float.NaN,string palette="River")
        {
            float top=float.IsNaN(highest)?level:highest;
            int nx=Mathf.CeilToInt((xMax-xMin)/cell)+1,nz=Mathf.CeilToInt((zMax-zMin)/cell)+1;
            var bed=new float[nx*nz];var wet=new bool[nx*nz];
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
            {
                var p=new Vector2(xMin+x*cell,zMin+z*cell);int i=z*nx+x;bed[i]=kit.height(p);
                // A margin under the bank, so the sheet's own edge is always inside the ground.
                wet[i]=bed[i]<top+.3f&&(within==null||within(p));
            }
            var index=new int[nx*nz];for(int i=0;i<index.Length;i++)index[i]=-1;
            var points=new List<Vector3>();var uv=new List<Vector2>();var depth=new List<Vector2>();var normals=new List<Vector3>();var triangles=new List<int>();
            int Point(int x,int z)
            {
                int i=z*nx+x;if(index[i]>=0)return index[i];
                var p=new Vector2(xMin+x*cell,zMin+z*cell);index[i]=points.Count;
                points.Add(new Vector3(p.x,0,p.y));uv.Add(p/8);depth.Add(new Vector2(bed[i],level-bed[i]));normals.Add(Vector3.up);return index[i];
            }
            for(int z=0;z<nz-1;z++)for(int x=0;x<nx-1;x++)
            {
                int i=z*nx+x;if(!wet[i]&&!wet[i+1]&&!wet[i+nx]&&!wet[i+nx+1])continue;
                int a=Point(x,z),b=Point(x,z+1),c=Point(x+1,z+1),d=Point(x+1,z);
                triangles.Add(a);triangles.Add(b);triangles.Add(c);triangles.Add(a);triangles.Add(c);triangles.Add(d);
            }
            if(points.Count==0)throw new InvalidOperationException(kit.zone+": the water '"+name+"' lies over no ground lower than itself");
            var mesh=new Mesh{name=name,indexFormat=points.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.SetVertices(points);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetUVs(1,depth);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            // Room for the level to move without the sheet being culled as it goes.
            var bounds=mesh.bounds;bounds.Expand(new Vector3(0,.2f,0));mesh.bounds=bounds;
            string key=kit.zone+"-"+name.Replace(' ','-');
            mesh=ZoneKit.SaveMesh(mesh,"Assets/_Project/Scenes/"+key+".asset");
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(BiomePatch));go.transform.position=new Vector3(0,level,0);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var view=go.GetComponent<MeshRenderer>();view.sharedMaterial=material;view.shadowCastingMode=ShadowCastingMode.Off;
            var patch=go.GetComponent<BiomePatch>();patch.resourceKey=key;patch.water=true;patch.palette=palette;
            return go;
        }

        /// <summary>Makes a sheet a body of water: anyone on foot under its surface inside one of the reaches wades. A reach is a loose rectangle of the plan (centre, size, turn); the
        /// ground's own height decides who is actually in the water.</summary>
        public static WaterVolume Volume(GameObject sheet,float level,params (Vector2 centre,Vector2 size,float yaw)[] reaches)
        {
            var root=new GameObject(sheet.name+" water");sheet.transform.SetParent(root.transform,true);
            var over=new Transform[reaches.Length];
            for(int i=0;i<reaches.Length;i++)
            {
                var reach=new GameObject("Reach").transform;reach.SetParent(root.transform,false);
                reach.SetPositionAndRotation(new Vector3(reaches[i].centre.x,level,reaches[i].centre.y),Quaternion.Euler(0,reaches[i].yaw,0));reach.localScale=new Vector3(reaches[i].size.x,1,reaches[i].size.y);over[i]=reach;
            }
            var water=root.AddComponent<WaterVolume>();water.level=level;water.surface=sheet.transform;water.reaches=over;return water;
        }

        /// <summary>Stands a wall in the water wherever it is deeper than <paramref name="from"/>, and cuts the paths there: what makes deep water deep. The walls are boxes merged from a
        /// two-metre grid, not static (the bake walks the bed under them, so a drained channel needs no second bake) and each a carving obstacle. They stop a body and stand no higher
        /// than a wader's waist, so a shot or a marked strike crosses the water as it would open floor. They hang under one holder that is a <see cref="Membrane"/>: deep water is a
        /// seal like any other, so the checks that open every seal to read a zone's baked floor open this too, and the wall is never taken for an invisible one.
        /// The holder is set on the volume as its <c>deep</c>: gone when it drains, there for good if it never does.</summary>
        public static GameObject[] Deep(ZoneKit kit,WaterVolume water,float xMin,float xMax,float zMin,float zMax,Func<Vector2,bool> within=null,float from=.55f,float cell=2)
        {
            int nx=Mathf.CeilToInt((xMax-xMin)/cell),nz=Mathf.CeilToInt((zMax-zMin)/cell);var deep=new bool[nx,nz];
            bool Under(float x,float z){var p=new Vector2(x,z);return kit.height(p)<water.level-from&&(within==null||within(p));}
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
            {
                float x0=xMin+x*cell,z0=zMin+z*cell;
                deep[x,z]=Under(x0,z0)&&Under(x0+cell,z0)&&Under(x0,z0+cell)&&Under(x0+cell,z0+cell)&&Under(x0+cell*.5f,z0+cell*.5f);
            }
            // Runs along x, then runs of the same run along z: a channel becomes a handful of long boxes.
            var boxes=new List<GameObject>();var open=new Dictionary<(int from,int to),int>();
            var holder=new GameObject("Deep water",typeof(Membrane));holder.transform.SetParent(water.transform,false);
            void Close((int from,int to) run,int started,int ended)
            {
                float wide=(run.to-run.from+1)*cell,far=(ended-started+1)*cell;const float tall=2.85f;
                var block=new GameObject("Deep water",typeof(BoxCollider),typeof(NavMeshObstacle));block.transform.SetParent(holder.transform,true);
                block.transform.position=new Vector3(xMin+run.from*cell+wide*.5f,water.level+.35f-tall*.5f,zMin+started*cell+far*.5f);
                var size=new Vector3(wide,tall,far);block.GetComponent<BoxCollider>().size=size;
                var carve=block.GetComponent<NavMeshObstacle>();carve.shape=NavMeshObstacleShape.Box;carve.size=size;carve.carving=true;carve.carveOnlyStationary=false;
                boxes.Add(block);
            }
            for(int z=0;z<=nz;z++)
            {
                var runs=new HashSet<(int,int)>();
                if(z<nz)for(int x=0;x<nx;x++){if(!deep[x,z])continue;int start=x;while(x+1<nx&&deep[x+1,z])x++;runs.Add((start,x));}
                foreach(var run in new List<(int from,int to)>(open.Keys))if(!runs.Contains(run)){Close(run,open[run],z-1);open.Remove(run);}
                foreach(var run in runs)if(!open.ContainsKey(run))open[run]=z;
            }
            var all=new List<GameObject>(water.deep){holder};water.deep=all.ToArray();
            return boxes.ToArray();
        }
        /// <summary>The blockers of every water in the scene, switched off for a bake's reach check (the paths are checked with every channel drained and every door open; what is shut at
        /// first is play's business) and back on after.</summary>
        public static void WithOpenWater(Action bake)
        {
            var shut=new List<GameObject>();
            foreach(var water in UnityEngine.Object.FindObjectsByType<WaterVolume>(FindObjectsSortMode.None))foreach(var block in water.deep)if(block!=null&&block.activeSelf){block.SetActive(false);shut.Add(block);}
            try{bake();}finally{foreach(var block in shut)block.SetActive(true);}
        }

        /// <summary>A spoked wheel about its own middle, turning about its own forward axis: a sluice's hand wheel, a valve's. Stood and turned by its transform, so a FlowSwitch can spin it.</summary>
        public static GameObject Wheel(ZoneKit kit,string name,float radius,Material timber,Color rim,Color spoke,int spokes=6)
        {
            var shape=new ZoneKit.Shape();var ring=new Vector3[17];var thick=new float[17];
            for(int k=0;k<17;k++){float a=k*Mathf.PI*2/16;ring[k]=new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0);thick[k]=radius*.11f;}
            shape.Tube(ring,thick,6,rim,rim,1,.6f,false);
            for(int k=0;k<spokes;k++)
            {
                float a=k*Mathf.PI*2/spokes;var spokeOut=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                shape.Tube(new[]{Vector3.zero,spokeOut*radius},new[]{radius*.075f,radius*.06f},5,spoke,spoke,1,.6f,false);
                // A grip standing out of the rim at every spoke: what a hand takes hold of, and what shows the wheel is turning.
                shape.Tube(new[]{spokeOut*radius,spokeOut*radius*1.28f},new[]{radius*.07f,radius*.085f},5,rim,rim);
            }
            shape.Tube(new[]{Vector3.back*radius*.16f,Vector3.forward*radius*.16f},new[]{radius*.2f,radius*.2f},8,spoke,spoke);
            return kit.Built(name,shape,timber);
        }
    }
}
