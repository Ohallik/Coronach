using System.Collections.Generic;
using System.Linq;
using Lattice.Core;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>
    /// The detail layer (dressing pass 2): flat, alpha-clipped, collider-free floor details
    /// (inset grates and hatches, glowing guide studs, painted pictograms and hazard bands,
    /// Sorrel's ground patches and tyre tracks), batched per kind and per 8 m cell so each
    /// batch is lit by its own nearby lamps; and lit bollard lamps. Details lie on the floor,
    /// so they cannot block a route, a door or a sightline; they only avoid each other, rugs,
    /// runners, walls and furniture so nothing z-fights or vanishes underneath a piece.
    /// </summary>
    public static partial class RoomDressing
    {
        const string DetailSource="art-src/Generated/C8-Dressing2/decals/";
        static readonly string[] DetailKinds={"DecalGrate","DecalHatch","DecalTread","DecalTrench","DecalStud","DecalDrain","DecalHazard",
            "PictoGalley","PictoMarket","PictoRepair","PictoCabins","PictoOffice","PictoDock",
            "GroundGravel","GroundClay","GroundStain","GroundPebbles","GroundRipples","GroundScuff","GroundTracks"};
        static readonly HashSet<string> Glowing=new(){"DecalGrate","DecalTrench","DecalStud"};
        static readonly HashSet<string> Tiled=new(){"DecalHazard","GroundTracks"};
        const float DetailCell=8;

        sealed class DetailBatch{public readonly List<Vector3> v=new();public readonly List<Vector2> uv=new();public readonly List<int> t=new();}
        static readonly Dictionary<(string kind,int x,int z),DetailBatch> details=new();
        static readonly List<Rect> detailRects=new();
        static List<Rect> obstacles;
        static int detailPlaced,detailSkipped;

        static void BeginDetails(){details.Clear();detailRects.Clear();obstacles=null;detailPlaced=detailSkipped=0;}

        static void StageDetailTextures()
        {
            foreach(var kind in DetailKinds)
            {
                PackStaging.StageFile(DetailSource+kind+"_base_color.png","Art/Generated/Dressing/"+kind+"_base_color.png");
                if(Glowing.Contains(kind))PackStaging.StageFile(DetailSource+kind+"_emission.png","Art/Generated/Dressing/"+kind+"_emission.png");
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var kind in DetailKinds)
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(DecalFolder+kind+"_base_color.png");
                if(importer==null)throw new System.InvalidOperationException("Missing staged detail texture "+kind);
                importer.alphaIsTransparency=true;importer.mipMapsPreserveCoverage=true;importer.alphaTestReferenceValue=.5f;
                importer.wrapMode=Tiled.Contains(kind)?TextureWrapMode.Repeat:TextureWrapMode.Clamp;importer.maxTextureSize=512;
                importer.SaveAndReimport();
            }
        }

        static Material DetailMaterial(string kind)
        {
            string path="Assets/_Project/Resources/WorldMaterials/dress-detail-"+kind+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Lattice/Toon"));AssetDatabase.CreateAsset(m,path);}
            m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(DecalFolder+kind+"_base_color.png"));
            m.SetFloat("_BaseMapStrength",1);
            // Stencilled paint is a worn cream, tracks are pressed ground (darker than the soil
            // they sit in), everything else keeps its painted colour.
            m.SetColor("_BaseColor",kind switch
            {
                _ when kind.StartsWith("Picto")=>new Color(.88f,.84f,.7f),
                "DecalHazard"=>new Color(.95f,.9f,.8f),
                // Ground decals blend into the soil: muted toward the terrain colour, partly transparent.
                "GroundTracks"=>new Color(.42f,.27f,.2f,.42f),
                "GroundRipples"=>new Color(.84f,.72f,.64f,.7f),
                "GroundScuff"=>new Color(.86f,.74f,.66f,.75f),
                "GroundClay"=>new Color(.88f,.76f,.72f,.8f),
                _ when kind.StartsWith("Ground")=>new Color(.9f,.82f,.78f,.82f),
                _=>Color.white
            });
            if(kind.StartsWith("Ground"))
            {
                // Soft-edged and blended: a hard alpha-clipped patch read as a sticker on the soil.
                m.DisableKeyword("_ALPHATEST_ON");m.SetFloat("_AlphaTest",0);
                m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);
                m.renderQueue=(int)RenderQueue.Transparent;
            }
            else
            {
                m.EnableKeyword("_ALPHATEST_ON");m.SetFloat("_AlphaTest",1);m.SetFloat("_Cutoff",.5f);
                m.SetFloat("_SrcBlend",(float)BlendMode.One);m.SetFloat("_DstBlend",(float)BlendMode.Zero);m.SetFloat("_ZWrite",1);
                m.renderQueue=(int)RenderQueue.AlphaTest;
            }
            if(Glowing.Contains(kind))
            {
                m.SetTexture("_EmissionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(DecalFolder+kind+"_emission.png"));
                m.SetColor("_EmissionColor",kind=="DecalStud"?new Color(1f,.68f,.28f)*2.4f:new Color(.13f,.9f,1f)*1.8f);
            }
            else m.SetColor("_EmissionColor",Color.black);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Walls and standing fixtures already in the scene, as floor rectangles.</summary>
        static List<Rect> Obstacles()
        {
            if(obstacles!=null)return obstacles;
            // Station decks are at y=0, so a standing fixture reaches above .35 m from below .6 m;
            // Sorrel's ground varies, so there any collider short of the terrain itself counts.
            bool deck=zone!="Sorrel_Ridges";
            obstacles=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
                .Where(c=>c.enabled&&!c.isTrigger&&!c.transform.IsChildOf(root)&&c.bounds.size.x<60&&c.bounds.size.z<60
                    &&(deck?c.bounds.max.y>.35f&&c.bounds.min.y<.6f:c.bounds.size.y>.3f))
                .Select(c=>Rect.MinMaxRect(c.bounds.min.x,c.bounds.min.z,c.bounds.max.x,c.bounds.max.z)).ToList();
            return obstacles;
        }

        static Rect Aabb(float x,float z,float w,float l,float yaw)
        {
            var r=Quaternion.Euler(0,yaw,0);var a=r*new Vector3(w*.5f,0,l*.5f);var b=r*new Vector3(w*.5f,0,-l*.5f);
            float hx=Mathf.Max(Mathf.Abs(a.x),Mathf.Abs(b.x)),hz=Mathf.Max(Mathf.Abs(a.z),Mathf.Abs(b.z));
            return Rect.MinMaxRect(x-hx,z-hz,x+hx,z+hz);
        }

        /// <summary>A flat detail of <paramref name="kind"/>, w across local x and l along local z.
        /// Overlay details (hazard bands over runners) pass <paramref name="overlay"/> and skip the overlap rule.</summary>
        static bool Detail(string kind,float x,float z,float w,float l,float yaw,float lift=.009f,bool overlay=false,float tile=0)
        {
            var rect=Aabb(x,z,w,l,yaw);
            if(!overlay)
            {
                var inner=Rect.MinMaxRect(rect.xMin+.03f,rect.yMin+.03f,rect.xMax-.03f,rect.yMax-.03f);
                bool blocked=detailRects.Any(r=>r.Overlaps(inner))||Obstacles().Any(r=>r.Overlaps(inner))
                    ||placed.Any(p=>p.solid&&Footprint2D(p.go).Overlaps(inner));
                if(blocked){detailSkipped++;Debug.Log($"DRESSING_DETAIL_SKIPPED {zone} {kind} at ({x:0.0},{z:0.0})");return false;}
                detailRects.Add(rect);
            }
            var rotation=Quaternion.Euler(0,yaw,0);var centre=new Vector3(x,0,z);
            float u=Tiled.Contains(kind)?(kind=="GroundTracks"?1:w/tile):1,v=Tiled.Contains(kind)?l/(kind=="GroundTracks"?w:tile):1;
            bool conform=zone=="Sorrel_Ridges";
            int nx=conform?Mathf.Max(1,Mathf.CeilToInt(w/.5f)):1,nz=conform?Mathf.Max(1,Mathf.CeilToInt(l/.5f)):1;
            float flat=conform?0:Ground(x,z,new Bounds(Vector3.zero,Vector3.zero),0);
            var verts=new Vector3[(nx+1)*(nz+1)];var uvs=new Vector2[verts.Length];
            for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++)
            {
                var local=new Vector3(-w*.5f+w*i/nx,0,-l*.5f+l*j/nz);var p=centre+rotation*local;
                float y=conform?Surface(p.x,p.z):flat;
                if(float.IsNaN(y)){detailSkipped++;return false;}
                verts[j*(nx+1)+i]=new Vector3(p.x,y+lift,p.z);uvs[j*(nx+1)+i]=new Vector2(u*i/nx,v*j/nz);
            }
            // A patch across a cliff or step would stand up off the ground: refuse it.
            if(conform&&verts.Max(q=>q.y)-verts.Min(q=>q.y)>Mathf.Max(w,l)*.45f){detailSkipped++;return false;}
            Append(kind,x,z,verts,uvs,nx,nz);
            detailPlaced++;return true;
        }

        /// <summary>A strip following a path on the ground (tyre tracks), conforming to the terrain.</summary>
        static void Strip(string kind,Vector2[] path,float offset,float width,float lift,float from=float.NegativeInfinity,float to=float.PositiveInfinity)
        {
            const int across=4;const float step=.75f;
            var rows=new List<(Vector3[] row,float along)>();float along=0;
            for(int i=1;i<path.Length;i++)
            {
                Vector2 a=path[i-1],d=path[i]-a;float length=d.magnitude;var n=new Vector2(-d.y,d.x)/length;
                for(float s=0;s<length;s+=step,along+=step)
                {
                    var c=a+d*(s/length)+n*offset;
                    if(c.y<from||c.y>to)continue;
                    var row=new Vector3[across+1];bool ok=true;
                    for(int k=0;k<=across&&ok;k++)
                    {
                        // Columns run left to right of the direction of travel, so the quads face up.
                        var q=c-n*width*(k/(float)across-.5f);float y=Surface(q.x,q.y);
                        ok=!float.IsNaN(y);row[k]=new Vector3(q.x,y+lift,q.y);
                    }
                    // Only ground the tracks could actually run on: no steps across the strip, nothing standing over it.
                    if(ok&&row.Max(q=>q.y)-row.Min(q=>q.y)<.5f&&!Covered(c,width*.5f))rows.Add((row,along));
                }
            }
            // One row of quads per step, appended to the cell of its midpoint.
            for(int i=1;i<rows.Count;i++)
            {
                var (r0,a0)=rows[i-1];var (r1,a1)=rows[i];
                if(a1-a0>step*1.5f||Mathf.Abs(r1[across/2].y-r0[across/2].y)>.4f)continue;
                var verts=r0.Concat(r1).ToArray();
                var uvs=Enumerable.Range(0,across+1).Select(k=>new Vector2(k/(float)across,a0/width))
                    .Concat(Enumerable.Range(0,across+1).Select(k=>new Vector2(k/(float)across,a1/width))).ToArray();
                var mid=(r0[across/2]+r1[across/2])*.5f;Append(kind,mid.x,mid.z,verts,uvs,across,1);
            }
            detailPlaced++;
        }

        static void Append(string kind,float x,float z,Vector3[] verts,Vector2[] uvs,int nx,int nz)
        {
            var key=(kind,Mathf.FloorToInt(x/DetailCell),Mathf.FloorToInt(z/DetailCell));
            if(!details.TryGetValue(key,out var batch))details[key]=batch=new DetailBatch();
            int start=batch.v.Count;batch.v.AddRange(verts);batch.uv.AddRange(uvs);
            for(int j=0;j<nz;j++)for(int i=0;i<nx;i++)
            {
                int a=start+j*(nx+1)+i,b=a+1,c=a+nx+1,d=c+1;
                batch.t.AddRange(new[]{a,c,b,c,d,b});
            }
        }

        /// <summary>Sorrel's terrain is the one collider wider than 100 m; rocks, habs and pads are not ground.</summary>
        static bool IsTerrain(Collider c)=>c.bounds.size.x>100&&c.bounds.size.z>100;

        /// <summary>Height of the terrain itself at a point (never a rock, roof or dressing), or NaN.</summary>
        static float Surface(float x,float z)
        {
            foreach(var hit in Physics.RaycastAll(new Vector3(x,80,z),Vector3.down,200,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
                if(IsTerrain(hit.collider))return hit.point.y;
            return float.NaN;
        }
        /// <summary>True when something other than terrain stands over the point (a hab, a rock, a pad).</summary>
        static bool Covered(Vector2 p,float radius)=>Obstacles().Any(r=>p.x>r.xMin-radius&&p.x<r.xMax+radius&&p.y>r.yMin-radius&&p.y<r.yMax+radius);

        static void FlushDetails()
        {
            var materials=new Dictionary<string,Material>();
            foreach(var group in details.OrderBy(d=>d.Key.kind).ThenBy(d=>d.Key.x).ThenBy(d=>d.Key.z))
            {
                var (kind,cx,cz)=group.Key;var batch=group.Value;
                if(!materials.TryGetValue(kind,out var material))materials[kind]=material=DetailMaterial(kind);
                var mesh=new Mesh{name=$"{zone} detail {kind} {cx},{cz}",indexFormat=batch.v.Count>65000?IndexFormat.UInt32:IndexFormat.UInt16};
                mesh.SetVertices(batch.v);mesh.SetUVs(0,batch.uv);mesh.SetTriangles(batch.t,0);
                mesh.RecalculateNormals();mesh.RecalculateBounds();decalMeshes.Add(mesh);
                var go=new GameObject("Floor detail "+kind,typeof(MeshFilter),typeof(MeshRenderer)){isStatic=true};
                go.transform.SetParent(root,false);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;
                r.shadowCastingMode=ShadowCastingMode.Off;
            }
            Debug.Log($"DRESSING_DETAIL {zone} placed={detailPlaced} skipped={detailSkipped} batches={details.Count}");
        }

        /// <summary>A waist-high bollard whose glowing top reads from the camera, with a real warm pool on the deck.</summary>
        static void Lamp(float x,float z,float yaw=0,bool lit=true)
        {
            var go=Put("DressLightBollard",x,z,yaw);
            if(!lit)return;
            var holder=new GameObject("Lamp light"){isStatic=true};holder.transform.SetParent(go.transform,false);holder.transform.localPosition=Vector3.up*1.2f;
            var light=holder.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1f,.76f,.48f);
            light.range=4.6f;light.intensity=1.5f;light.shadows=LightShadows.None;light.lightmapBakeType=LightmapBakeType.Realtime;
        }

        /// <summary>Glowing guide studs on both edges of a straight walkway, skipping its doorways.</summary>
        static void Studs(Vector2 a,Vector2 b,float edge,float step,params float[] gaps)
        {
            var d=b-a;float length=d.magnitude;var dir=d/length;var n=new Vector2(-dir.y,dir.x);
            for(float s=step*.5f;s<length;s+=step)
            {
                var c=a+dir*s;
                if(gaps.Any(g=>Mathf.Abs((Mathf.Abs(dir.x)>.5f?c.x:c.y)-g)<.9f))continue;
                foreach(int side in new[]{-1,1}){var p=c+n*side*edge;Detail("DecalStud",p.x,p.y,.3f,.3f,0,.02f);}
            }
        }

        /// <summary>A painted hazard outline along the given open edges of a rectangle.</summary>
        static void HazardEdges(float xMin,float zMin,float xMax,float zMax,bool west,bool south,bool east,bool north,float band=.32f)
        {
            if(west)Detail("DecalHazard",xMin,(zMin+zMax)*.5f,band,zMax-zMin,0,.018f,true,.9f);
            if(east)Detail("DecalHazard",xMax,(zMin+zMax)*.5f,band,zMax-zMin,0,.018f,true,.9f);
            if(south)Detail("DecalHazard",(xMin+xMax)*.5f,zMin,band,xMax-xMin,90,.018f,true,.9f);
            if(north)Detail("DecalHazard",(xMin+xMax)*.5f,zMax,band,xMax-xMin,90,.018f,true,.9f);
        }
    }
}
