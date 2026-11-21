using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>Textures and materials a builder makes itself, for shapes that have no generated art behind them: painted near neutral, so a mesh's vertex colours decide what it is
    /// (one bark for every trunk and root). Deterministic, so a rebuild writes the same bytes and nothing reimports.</summary>
    public sealed partial class ZoneKit
    {
        /// <summary>Value noise that repeats every <paramref name="period"/> cells, so a painted texture tiles. x and y in cells.</summary>
        public static float Tile(float x,float y,int period,int seed=0)
        {
            int x0=Mathf.FloorToInt(x),y0=Mathf.FloorToInt(y);float fx=x-x0,fy=y-y0;fx=fx*fx*(3-2*fx);fy=fy*fy*(3-2*fy);
            float At(int ix,int iy)
            {
                ix=(ix%period+period)%period;iy=(iy%period+period)%period;
                uint h=(uint)(ix*374761393+iy*668265263+seed*1442695041);h=(h^(h>>13))*1274126177;h^=h>>16;return (h&0xffff)/65535f;
            }
            return Mathf.Lerp(Mathf.Lerp(At(x0,y0),At(x0+1,y0),fx),Mathf.Lerp(At(x0,y0+1),At(x0+1,y0+1),fx),fy);
        }
        /// <summary>Three octaves of <see cref="Tile"/> over the unit square: u and v from 0 to 1, repeating.</summary>
        public static float Cloud(float u,float v,int period,int seed=0)
            =>(Tile(u*period,v*period,period,seed)*4+Tile(u*period*2,v*period*2,period*2,seed+7)*2+Tile(u*period*4,v*period*4,period*4,seed+13))/7;
        /// <summary>Paints a square texture pixel by pixel (u,v from 0 to 1) and saves it under <c>Art/Generated/</c>. Written only when its bytes change.</summary>
        public static Texture2D Texture(string file,int size,Func<float,float,Color> paint,bool repeat=true,bool alpha=false)
        {
            string path="Assets/_Project/Art/Generated/"+file+".png";Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            var made=new Texture2D(size,size,alpha?TextureFormat.RGBA32:TextureFormat.RGB24,false);var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)pixels[y*size+x]=paint((x+.5f)/size,(y+.5f)/size);
            made.SetPixels(pixels);var bytes=made.EncodeToPNG();UnityEngine.Object.DestroyImmediate(made);
            bool changed=!File.Exists(path);
            if(!changed){var old=File.ReadAllBytes(path);changed=old.Length!=bytes.Length;for(int i=0;!changed&&i<old.Length;i++)changed=old[i]!=bytes[i];}
            if(changed){File.WriteAllBytes(path,bytes);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);}
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer!=null&&(importer.wrapMode!=(repeat?TextureWrapMode.Repeat:TextureWrapMode.Clamp)||!importer.mipmapEnabled||importer.maxTextureSize!=1024||importer.alphaIsTransparency!=alpha))
            {importer.wrapMode=repeat?TextureWrapMode.Repeat:TextureWrapMode.Clamp;importer.mipmapEnabled=true;importer.maxTextureSize=1024;importer.alphaIsTransparency=alpha;importer.SaveAndReimport();}
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        /// <summary>Kept exactly as painted, uncompressed: for a map whose alpha is cut at a level (a coast, a cloud's edge, a leaf's outline), where block compression puts steps in
        /// the line the cut follows.</summary>
        public static Texture2D Exact(Texture2D texture)
        {
            var importer=AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            if(importer!=null&&importer.textureCompression!=TextureImporterCompression.Uncompressed){importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
            return texture;
        }
        /// <summary>A toon material of the world's, made or brought back to the shader's defaults and then set: what the lambda names is what it is, every run (tune here, never in the asset).
        /// It lives in <c>Resources/WorldMaterials</c>, so every keyword it turns on is a variant the build keeps.</summary>
        public static Material Toon(string name,Texture2D map,Action<Material> set=null,string shader="Lattice/Toon")
        {
            string path="Assets/_Project/Resources/WorldMaterials/"+name+".mat";var made=AssetDatabase.LoadAssetAtPath<Material>(path);
            var fresh=new Material(Shader.Find(shader)){name=name};
            if(made==null){made=fresh;AssetDatabase.CreateAsset(made,path);}
            else{made.shader=fresh.shader;made.CopyPropertiesFromMaterial(fresh);made.shaderKeywords=Array.Empty<string>();made.renderQueue=-1;UnityEngine.Object.DestroyImmediate(fresh);}
            if(map!=null)made.SetTexture("_BaseMap",map);
            set?.Invoke(made);EditorUtility.SetDirty(made);return made;
        }
        /// <summary>Turns one of LatticeToon's features on: the keyword and the toggle the inspector shows for it.</summary>
        public static Material With(Material material,string keyword,string toggle){material.EnableKeyword(keyword);material.SetFloat(toggle,1);return material;}
        /// <summary>Vertex colour tints it: one material for everything built of the same stuff.</summary>
        public static Material Tinted(Material material)=>With(material,"_VCOLOR_ON","_VColor");
        /// <summary>Drawn from both sides: a leaf, a cloth, a pane.</summary>
        public static Material TwoSided(Material material){material.SetFloat("_Cull",(float)CullMode.Off);return material;}
        /// <summary>Sways from its foot: x how far (metres), y how fast, z the height at which it sways in full, w how much flutter.</summary>
        public static Material Swaying(Material material,float reach,float rate,float tall,float flutter=.35f){With(material,"_WIND_ON","_Wind");material.SetVector("_WindParams",new Vector4(reach,rate,tall,flutter));return material;}
        /// <summary>The toon shader's water (the public look, D131): shallow colour at the shore going to deep, ripples, and foam where the mesh's second uv set says the shore is
        /// (uv2.y is metres from it; see <see cref="Water"/>). A licensed water can take its place locally through a BiomePatch on the same object.</summary>
        public static Material WaterLook(Material material,Color shallow,Color deep,Color foam,float depth=2.2f,float ripple=.45f,float sparkle=.3f,float foamWide=.35f)
        {
            With(material,"_WATER_ON","_Water");material.SetColor("_BaseColor",shallow);material.SetColor("_DeepColor",deep);material.SetColor("_FoamColor",foam);
            material.SetVector("_WaterParams",new Vector4(ripple,.12f,depth,sparkle));material.SetVector("_EdgeParams",new Vector4(1,.1f,.05f,.5f));
            material.SetVector("_FoamParams",new Vector4(foamWide,.7f,.12f,.3f));material.SetFloat("_BaseMapStrength",.25f);material.SetFloat("_ReceiveShadows",1);
            // Clear at the bank, so the bed shows through the shallows; drawn after everything solid and before what is laid on the ground (WaterKit.Look keeps the same order).
            material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);
            material.SetOverrideTag("RenderType","Transparent");material.renderQueue=(int)RenderQueue.Transparent-30;
            return material;
        }
        /// <summary>A sheet of water over a rectangle at one level, drawn only where the ground is below it (and a margin under the bank, so no edge shows). Each point's second uv carries how
        /// far it is from dry ground, which is what the water look's depth and foam go by. The mesh is saved beside the scene.</summary>
        public GameObject Water(string name,float xMin,float xMax,float zMin,float zMax,float cell,float level,Material material,float bankReach=6)
        {
            int nx=Mathf.CeilToInt((xMax-xMin)/cell)+1,nz=Mathf.CeilToInt((zMax-zMin)/cell)+1;
            var points=new Vector3[nx*nz];var uv=new Vector2[points.Length];var shore=new Vector2[points.Length];var wet=new bool[points.Length];var normals=new Vector3[points.Length];
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
            {
                int i=z*nx+x;var p=new Vector2(xMin+x*cell,zMin+z*cell);points[i]=new Vector3(p.x,level,p.y);uv[i]=p/8;normals[i]=Vector3.up;wet[i]=height(p)<level+.25f;
            }
            // Metres to the nearest dry point, by walking outward over the grid: the bank's own shape, with no need for the height function to be a distance.
            int reach=Mathf.Max(1,Mathf.CeilToInt(bankReach/cell));
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
            {
                int i=z*nx+x;float near=bankReach;
                if(height(new Vector2(points[i].x,points[i].z))>=level){shore[i]=Vector2.zero;continue;}
                for(int dz=-reach;dz<=reach;dz+=Mathf.Max(1,reach/6))for(int dx=-reach;dx<=reach;dx+=Mathf.Max(1,reach/6))
                {
                    var q=new Vector2(points[i].x+dx*cell,points[i].z+dz*cell);
                    if(height(q)>=level)near=Mathf.Min(near,Mathf.Sqrt(dx*dx+dz*dz)*cell);
                }
                shore[i]=new Vector2(0,near);
            }
            var triangles=new System.Collections.Generic.List<int>();
            for(int z=0;z<nz-1;z++)for(int x=0;x<nx-1;x++)
            {
                int i=z*nx+x;if(!wet[i]&&!wet[i+1]&&!wet[i+nx]&&!wet[i+nx+1])continue;
                triangles.Add(i);triangles.Add(i+nx);triangles.Add(i+nx+1);triangles.Add(i);triangles.Add(i+nx+1);triangles.Add(i+1);
            }
            var mesh=new Mesh{name=name,indexFormat=points.Length>65000?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.vertices=points;mesh.normals=normals;mesh.uv=uv;mesh.uv2=shore;mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            mesh=SaveMesh(mesh,"Assets/_Project/Scenes/"+zone+"-"+name.Replace(' ','-')+".asset");
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var view=go.GetComponent<MeshRenderer>();view.sharedMaterial=material;view.shadowCastingMode=ShadowCastingMode.Off;
            return go;
        }
    }
}
