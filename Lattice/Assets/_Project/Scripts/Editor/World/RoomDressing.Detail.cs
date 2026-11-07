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
        /// <summary>Dressing pass 3 (D193): Hushwell's cave floor and rails, the Gullet's floor anatomy.</summary>
        const string DetailSource3="art-src/Generated/C8-Dressing3/decals/";
        static readonly string[] DetailKinds={"DecalGrate","DecalHatch","DecalTread","DecalTrench","DecalStud","DecalDrain","DecalHazard",
            "PictoGalley","PictoMarket","PictoRepair","PictoCabins","PictoOffice","PictoDock",
            "GroundGravel","GroundClay","GroundStain","GroundPebbles","GroundRipples","GroundScuff","GroundTracks"};
        static readonly string[] DetailKinds3={"DecalRails","GroundBasalt","GroundDust","GroundCrust","GroundFissure",
            "GroundVeins","GroundPool","GroundEggs","GroundSilt","GroundPore","GroundScar","GroundScour"};
        static IEnumerable<string> AllDetailKinds=>DetailKinds.Concat(DetailKinds3);
        static string SourceOf(string kind)=>(DetailKinds3.Contains(kind)?DetailSource3:DetailSource)+kind;
        static readonly HashSet<string> Glowing=new(){"DecalGrate","DecalTrench","DecalStud","GroundVeins","GroundEggs","GroundPore","GroundScour"};
        static readonly HashSet<string> Tiled=new(){"DecalHazard","GroundTracks","DecalRails","GroundScour"};
        /// <summary>Width/length of one repeat of a strip texture, as measured when it was cut.</summary>
        static readonly Dictionary<string,float> StripAspect=new(){["GroundTracks"]=1f,["DecalRails"]=.702f,["GroundScour"]=.652f};
        /// <summary>The Gullet's ships fly at y=1; dressing on its bed stays well under them (D193).</summary>
        public const float FlightCeiling=-.6f;
        const float DetailCell=8;

        sealed class DetailBatch{public readonly List<Vector3> v=new();public readonly List<Vector2> uv=new();public readonly List<int> t=new();public readonly List<Color32> c=new();}
        /// <summary>Strip kinds whose runs fade out at both ends instead of stopping square (vertex alpha, `_VCOLOR_ON`).</summary>
        static readonly HashSet<string> FadedEnds=new(){"GroundScour","GroundTracks"};
        /// <summary>Ground patches that must not read as stickers: each takes its own crop of the texture, its own shade and an irregular feathered edge, so no two match and none
        /// shows the stamp's outline. A crust marks a plume's reach, and a pool, a pore or eggs are things in their own right: those keep their shape.</summary>
        static readonly HashSet<string> Feathered=new(){"GroundClay","GroundStain","GroundRipples","GroundGravel","GroundPebbles","GroundScuff","GroundBasalt","GroundDust","GroundFissure","GroundSilt","GroundScar","GroundVeins"};
        static bool VertexColoured(string kind)=>FadedEnds.Contains(kind)||Feathered.Contains(kind);
        /// <summary>Crop, shade and edge from the patch's own position: no random stream moves, so every other piece stays where it was.</summary>
        static Color32[] Feather(float x,float z,Vector2[] uvs,int nx,int nz)
        {
            float Hash(float seed)=>Mathf.Repeat(Mathf.Sin(x*12.9898f+z*78.233f+seed)*43758.5453f,1);
            float crop=Mathf.Lerp(.58f,.86f,Hash(1)),du=Mathf.Lerp(.07f,.93f-crop,Hash(2)),dv=Mathf.Lerp(.07f,.93f-crop,Hash(3)),wobble=Hash(6)*40;
            bool flipU=Hash(4)>.5f,flipV=Hash(5)>.5f;byte tone=(byte)Mathf.RoundToInt(255*Mathf.Lerp(.84f,1,Hash(7)));
            var shades=new Color32[uvs.Length];
            for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++)
            {
                float s=i/(float)nx,t=j/(float)nz;int k=j*(nx+1)+i;
                uvs[k]=new Vector2(du+crop*(flipU?1-s:s),dv+crop*(flipV?1-t:t));
                var q=new Vector2(2*s-1,2*t-1);float angle=Mathf.Atan2(q.y,q.x);
                float edge=.78f+.22f*Mathf.PerlinNoise(Mathf.Cos(angle)*1.4f+wobble,Mathf.Sin(angle)*1.4f+wobble);
                shades[k]=new Color32(tone,tone,tone,(byte)Mathf.RoundToInt(255*(1-Mathf.SmoothStep(edge*.42f,edge,q.magnitude))));
            }
            return shades;
        }
        /// <summary>What a floor sign will cover once it's sized for the camera: ground patches keep off it, and so does whatever stands.</summary>
        static List<Rect> signRects=new();
        static List<Rect> SignRects()
        {
            var rects=new List<Rect>();if(Station||Exterior)return rects;
            foreach(var sign in Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(!sign.name.StartsWith("Sign_")||sign.transform.parent!=null)continue;
                var p=sign.transform.position;float half=sign.text.Length*.2f+.5f;
                rects.Add(Rect.MinMaxRect(p.x-half,p.z-1,p.x+half,p.z+1));
            }
            return rects;
        }
        static readonly Dictionary<(string kind,int x,int z),DetailBatch> details=new();
        static readonly List<Rect> detailRects=new();
        static List<Rect> obstacles;
        static int detailPlaced,detailSkipped;

        static void BeginDetails(){details.Clear();detailRects.Clear();obstacles=null;detailPlaced=detailSkipped=0;signRects=SignRects();}

        static void StageDetailTextures()
        {
            foreach(var kind in AllDetailKinds)
            {
                PackStaging.StageFile(SourceOf(kind)+"_base_color.png","Art/Generated/Dressing/"+kind+"_base_color.png");
                if(Glowing.Contains(kind))PackStaging.StageFile(SourceOf(kind)+"_emission.png","Art/Generated/Dressing/"+kind+"_emission.png");
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var kind in AllDetailKinds)
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
                "GroundRipples"=>new Color(.84f,.72f,.64f,.5f),
                "GroundScuff"=>new Color(.86f,.74f,.66f,.5f),
                "GroundClay"=>new Color(.8f,.68f,.64f,.5f),
                "GroundGravel" or "GroundPebbles" or "GroundStain"=>new Color(.9f,.82f,.78f,.6f),
                // Hushwell: the cave floor is a dark warm brown under a dim key, so its patches are muted toward it.
                "GroundBasalt"=>new Color(.84f,.8f,.82f,.36f),
                "GroundDust"=>new Color(.6f,.5f,.42f,.38f),
                "GroundCrust"=>new Color(.9f,.86f,.8f,.9f),
                "GroundFissure"=>new Color(.42f,.34f,.32f,.7f),
                // The Gullet: tissue patches sit on the tinted membrane, slightly translucent so the cells show through.
                "GroundScour"=>new Color(.72f,.68f,.85f,.5f),
                // In play the Gullet's bloom made full-strength vessels the brightest thing on screen, the same cyan as shots: kept quiet.
                "GroundVeins"=>new Color(.42f,.48f,.68f,.55f),
                _ when DetailKinds3.Contains(kind)&&kind.StartsWith("Ground")=>new Color(.86f,.84f,.95f,.85f),
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
            if(VertexColoured(kind)){m.EnableKeyword("_VCOLOR_ON");m.SetFloat("_VColor",1);}
            else{m.DisableKeyword("_VCOLOR_ON");m.SetFloat("_VColor",0);}
            if(Glowing.Contains(kind))
            {
                m.SetTexture("_EmissionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(DecalFolder+kind+"_emission.png"));
                m.SetColor("_EmissionColor",kind switch
                {
                    "DecalStud"=>new Color(1f,.68f,.28f)*2.4f,
                    // Bioluminescence carries the Gullet's colour: veins and scour lines brighter, eggs a soft pale glow.
                    // Quieter than anything that flies over them: they shared the darts' blue and outshone them.
                    "GroundVeins"=>new Color(.13f,.9f,1f)*.12f,
                    "GroundScour"=>new Color(.13f,.9f,1f)*1.1f,
                    "GroundEggs"=>new Color(.6f,.9f,1f)*1.6f,
                    "GroundPore"=>new Color(.3f,.6f,1f)*2f,
                    _=>new Color(.13f,.9f,1f)*1.8f
                });
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
            bool deck=Station;
            obstacles=Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
                .Where(c=>c.enabled&&!c.isTrigger&&!c.transform.IsChildOf(root)&&c.bounds.size.x<60&&c.bounds.size.z<60
                    &&(deck?c.bounds.max.y>.35f&&c.bounds.min.y<.6f:c.bounds.size.y>.3f)
                    // In the Gullet the dormant Cantor floats at flight height over the coil: only what reaches down toward the bed counts.
                    &&(!Flight||c.bounds.min.y<FlightCeiling))
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
                bool blocked=signRects.Any(r=>r.Overlaps(inner))||detailRects.Any(r=>r.Overlaps(inner))||(Cave?BlockedRect(x,z,w,l,yaw):Exterior?RoofBlocked(x,z,w,l,yaw):Obstacles().Any(r=>r.Overlaps(inner)))
                    ||placed.Any(p=>p.solid&&Footprint2D(p.go).Overlaps(inner));
                if(blocked){detailSkipped++;Debug.Log($"DRESSING_DETAIL_SKIPPED {zone} {kind} at ({x:0.0},{z:0.0})");return false;}
                detailRects.Add(rect);
            }
            var rotation=Quaternion.Euler(0,yaw,0);var centre=new Vector3(x,0,z);
            float u=Tiled.Contains(kind)?(kind=="GroundTracks"?1:w/tile):1,v=Tiled.Contains(kind)?l/(kind=="GroundTracks"?w:tile):1;
            bool conform=!Station;
            int nx=conform?Mathf.Max(1,Mathf.CeilToInt(w/.5f)):1,nz=conform?Mathf.Max(1,Mathf.CeilToInt(l/.5f)):1;
            float flat=conform?0:Ground(x,z,new Bounds(Vector3.zero,Vector3.zero),0);
            var verts=new Vector3[(nx+1)*(nz+1)];var uvs=new Vector2[verts.Length];var valid=new bool[verts.Length];
            // In the cave and the Gullet a patch may end at the foot of a wall: cells with no floor under them are
            // dropped (and their vertices removed), so a crust beside a wall loses only its transparent corner.
            // Elsewhere, and for any patch that would lose half its cells, a missing floor still refuses it.
            bool trim=Cave||Flight;
            for(int j=0;j<=nz;j++)for(int i=0;i<=nx;i++)
            {
                var local=new Vector3(-w*.5f+w*i/nx,0,-l*.5f+l*j/nz);var p=centre+rotation*local;
                float y=conform?Surface(p.x,p.z):flat;int k=j*(nx+1)+i;valid[k]=!float.IsNaN(y);
                if(!valid[k]&&!trim){detailSkipped++;if(!overlay)detailRects.Remove(rect);return false;}
                verts[k]=new Vector3(p.x,valid[k]?y+lift:0,p.z);uvs[k]=new Vector2(u*i/nx,v*j/nz);
            }
            int cells=0;for(int j=0;j<nz;j++)for(int i=0;i<nx;i++){int a=j*(nx+1)+i;if(valid[a]&&valid[a+1]&&valid[a+nx+1]&&valid[a+nx+2])cells++;}
            if(cells<nx*nz*.5f){detailSkipped++;if(!overlay)detailRects.Remove(rect);Debug.Log($"DRESSING_DETAIL_REFUSED {zone} {kind} at ({x:0.0},{z:0.0}): floor under only {cells}/{nx*nz} cells");return false;}
            // A patch across a cliff or step would stand up off the ground: refuse it. (The Gullet's bed is one smooth
            // curve that the 0.5 m grid follows, so there the flight ceiling, not a step rule, bounds a patch.)
            var kept=verts.Where((q,k)=>valid[k]).ToArray();
            if(conform&&!Flight&&kept.Max(q=>q.y)-kept.Min(q=>q.y)>Mathf.Max(w,l)*.45f){detailSkipped++;if(!overlay)detailRects.Remove(rect);if(trim)Debug.Log($"DRESSING_DETAIL_REFUSED {zone} {kind} at ({x:0.0},{z:0.0}): crosses a step");return false;}
            Append(kind,x,z,verts,uvs,nx,nz,valid,Feathered.Contains(kind)?Feather(x,z,uvs,nx,nz):null);
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
                    // The Gullet's coil bed is sculpted but smooth; a scour band may follow it across a metre of rise.
                    if(ok&&row.Max(q=>q.y)-row.Min(q=>q.y)<(Flight?1.2f:.5f)&&!(Cave?BlockedRect(c.x,c.y,width,step,0,row[across/2].y):Covered(c,width*.5f)))rows.Add((row,along));
                }
            }
            bool Linked(int i)=>i>0&&rows[i].along-rows[i-1].along<=step*1.5f&&Mathf.Abs(rows[i].row[across/2].y-rows[i-1].row[across/2].y)<=(Flight?.8f:.4f);
            // Each continuous run tapers over its last few metres at both ends, so a band wears out instead of stopping square.
            var alpha=new float[rows.Count];
            for(int s=0;s<rows.Count;)
            {
                int e=s;while(e+1<rows.Count&&Linked(e+1))e++;
                float start=rows[s].along,end=rows[e].along,fade=Mathf.Min(3.5f,(end-start)*.35f);
                for(int i=s;i<=e;i++)alpha[i]=!FadedEnds.Contains(kind)||fade<=0?1:
                    Mathf.SmoothStep(0,1,Mathf.Clamp01((rows[i].along-start)/fade))*Mathf.SmoothStep(0,1,Mathf.Clamp01((end-rows[i].along)/fade));
                s=e+1;
            }
            // One row of quads per step, appended to the cell of its midpoint.
            for(int i=1;i<rows.Count;i++)
            {
                var (r0,a0)=rows[i-1];var (r1,a1)=rows[i];
                if(!Linked(i))continue;
                var verts=r0.Concat(r1).ToArray();
                byte f0=(byte)Mathf.RoundToInt(alpha[i-1]*255),f1=(byte)Mathf.RoundToInt(alpha[i]*255);
                var colors=Enumerable.Repeat(new Color32(255,255,255,f0),across+1).Concat(Enumerable.Repeat(new Color32(255,255,255,f1),across+1)).ToArray();
                float repeat=width/StripAspect[kind];
                var uvs=Enumerable.Range(0,across+1).Select(k=>new Vector2(k/(float)across,a0/repeat))
                    .Concat(Enumerable.Range(0,across+1).Select(k=>new Vector2(k/(float)across,a1/repeat))).ToArray();
                var mid=(r0[across/2]+r1[across/2])*.5f;Append(kind,mid.x,mid.z,verts,uvs,across,1,null,colors);
            }
            if(Cave||Flight)Debug.Log($"DRESSING_STRIP {zone} {kind} rows={rows.Count} length={along:0}");
            detailPlaced++;
        }

        static void Append(string kind,float x,float z,Vector3[] verts,Vector2[] uvs,int nx,int nz,bool[] valid=null,Color32[] colors=null)
        {
            var key=(kind,Mathf.FloorToInt(x/DetailCell),Mathf.FloorToInt(z/DetailCell));
            if(!details.TryGetValue(key,out var batch))details[key]=batch=new DetailBatch();
            // Only vertices of kept cells enter the batch, so no stray vertex sits off the floor.
            var index=new int[verts.Length];for(int k=0;k<index.Length;k++)index[k]=-1;
            int Use(int k){if(index[k]<0){index[k]=batch.v.Count;batch.v.Add(verts[k]);batch.uv.Add(uvs[k]);batch.c.Add(colors!=null?colors[k]:new Color32(255,255,255,255));}return index[k];}
            for(int j=0;j<nz;j++)for(int i=0;i<nx;i++)
            {
                int a=j*(nx+1)+i,b=a+1,c=a+nx+1,d=c+1;
                if(valid!=null&&!(valid[a]&&valid[b]&&valid[c]&&valid[d]))continue;
                int ia=Use(a),ib=Use(b),ic=Use(c),id=Use(d);
                batch.t.AddRange(new[]{ia,ic,ib,ic,id,ib});
            }
        }

        /// <summary>Sorrel's terrain and Hushwell's cave are the one collider wider than 100 m; rocks, habs and pads
        /// are not ground. The Gullet's ground is its membrane shell (narrower than 100 m at the coil).</summary>
        static bool IsTerrain(Collider c)=>Flight?c.name==(zone=="TallowApproach"?TempHullCollider:"Gullet membrane shell"):Exterior?!c.transform.IsChildOf(root):c.bounds.size.x>100&&c.bounds.size.z>100;

        /// <summary>The point's height if it is floor a piece may stand on, else NaN: in Hushwell the cave floor of its
        /// level (never a rock top or lip), in the Gullet only the bed well below the flight plane.</summary>
        static float Floor(Vector3 p)
        {
            if(Cave)return p.y<=HushwellLayout.Level(p.z)+.3f?p.y:float.NaN;
            if(Flight)return p.y<=FlightCeiling-.4f?p.y:float.NaN;
            if(Exterior)return p.y>=RoofMin?p.y:float.NaN;
            return p.y;
        }

        /// <summary>Height of the terrain itself at a point (never a rock, roof or dressing), or NaN.</summary>
        static float Surface(float x,float z)
        {
            foreach(var hit in Physics.RaycastAll(new Vector3(x,80,z),Vector3.down,200,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
                if(IsTerrain(hit.collider))return Floor(hit.point);
            return float.NaN;
        }
        /// <summary>In the cave, the exact rotated colliders standing on the floor over a rectangle (the grooved walls'
        /// axis-aligned boxes reach metres onto the floor, so the box test refused rails and patches that were clear).</summary>
        static bool BlockedRect(float x,float z,float w,float l,float yaw,float? floor=null)
        {
            float y=floor??HushwellLayout.Level(z);
            foreach(var c in Physics.OverlapBox(new Vector3(x,y+.65f,z),new Vector3(w*.5f,.5f,l*.5f),Quaternion.Euler(0,yaw,0),~0,QueryTriggerInteraction.Ignore))
                if(!IsTerrain(c)&&!c.transform.IsChildOf(root))return true;
            return false;
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
                mesh.SetVertices(batch.v);mesh.SetUVs(0,batch.uv);mesh.SetTriangles(batch.t,0);if(VertexColoured(kind))mesh.SetColors(batch.c);
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
