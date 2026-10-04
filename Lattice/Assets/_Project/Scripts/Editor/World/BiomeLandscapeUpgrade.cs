using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Core;
using Lattice.Combat;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Lattice.EditorTools
{
    /// <summary>Only landscape surfaces: existing actors, routes, quests and props stay in place.</summary>
    public static class BiomeLandscapeUpgrade
    {
        public const string Art = "Assets/_Project/Art/Generated/Biomes/";
        static Transform root;
        static Collider[] solids;
        static MeshCollider terrain;
        static string zone;
        static readonly Vector2[] Haul = {new(0,-18),new(0,8),new(0,24),new(9,39),new(4,58),new(-8,75),new(-5,94),new(9,111),new(16,129),new(7,145)};
        static readonly Vector2[] Seam = {new(-5,27),new(-24,35),new(-36,49),new(-39,68),new(-28,88),new(-24,107),new(-13,124),new(7,145)};
        static readonly Vector2[] Service = {new(9,39),new(29,46),new(39,64),new(31,83),new(35,103),new(31,123),new(16,129)};
        public static readonly Vector4[] NurseryPools = {new(4,336,3.6f,2.8f),new(14,344,4.2f,2),new(24,334,3.2f,2.5f)};

        public static void Apply() => BatchTools.Run(() =>
        {
            Directory.CreateDirectory(Art);
            AssetDatabase.Refresh();
            foreach (var name in new[] {"GrassClump", "CalmWater"})
            {
                var importer = AssetImporter.GetAtPath(Art + name + ".png") as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Generated fallback texture missing: " + name);
                importer.alphaIsTransparency = name == "GrassClump"; importer.maxTextureSize = 1024;
                importer.wrapMode = TextureWrapMode.Clamp; importer.mipmapEnabled = true;
                importer.filterMode = FilterMode.Bilinear; importer.SaveAndReimport();
            }
            foreach (var sceneName in new[] {"Sorrel_Ridges", "Arena_Ground", "Hushwell", "TallowDrift"})
            {
                zone = sceneName;
                var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/" + zone + ".unity");
                var old = GameObject.Find("Biome surfaces"); if (old != null) Object.DestroyImmediate(old);
                root = new GameObject("Biome surfaces").transform;
                solids = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c => !c.isTrigger && c.enabled).ToArray();
                terrain = solids.OfType<MeshCollider>().FirstOrDefault(c => c.sharedMesh != null && c.sharedMesh.name.Contains(zone == "Sorrel_Ridges" ? "basin" : "Hushwell"));
                Physics.SyncTransforms();
                switch (zone)
                {
                    case "Sorrel_Ridges": Sorrel(); break;
                    case "Arena_Ground": Yard(); break;
                    case "Hushwell": Nursery(); break;
                    case "TallowDrift": Refuge(); break;
                }
                if(zone=="Hushwell"||zone=="TallowDrift"||zone=="Sorrel_Ridges")
                {
                    foreach(var navigation in Object.FindObjectsByType<GroundNavigation>(FindObjectsSortMode.None))Object.DestroyImmediate(navigation.gameObject);
                    if(zone=="Hushwell")HushwellBuilder.BakeNavigation();
                    else if(zone=="Sorrel_Ridges")SorrelRedesign.BakeNavigation();
                    else StationNavigationBake.Bake(zone);
                }
                EditorSceneManager.SaveScene(scene);
                Debug.Log("BIOME_SURFACES " + zone + " patches=" + root.GetComponentsInChildren<BiomePatch>().Length);
            }
            AssetDatabase.SaveAssets(); Debug.Log("BIOME_LANDSCAPES_OK");
        });

        static void Sorrel()
        {
            if (terrain == null) throw new InvalidOperationException("Sorrel terrain missing");
            var basin=terrain.sharedMesh;var vertices=basin.vertices;
            for(int i=0;i<vertices.Length;i++)
            {
                var v=vertices[i];float r=Vector2.Distance(new Vector2(v.x,v.z),new Vector2(8,177));
                // Preserve firm ground beneath the four scaffold feet.
                if(r<2)vertices[i].y=-.1f-3.2f*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,1.18f,r)));
            }
            basin.vertices=vertices;basin.RecalculateNormals();basin.RecalculateBounds();EditorUtility.SetDirty(basin);
            terrain.sharedMesh=null;terrain.sharedMesh=basin;Physics.SyncTransforms();
            var encounters = Object.FindObjectsByType<EncounterVolume>(FindObjectsSortMode.None);
            bool Verge(Vector2 p)
            {
                if (Mathf.Min(HushwellLayout.Distance(p, Haul), Mathf.Min(HushwellLayout.Distance(p, Seam), HushwellLayout.Distance(p, Service))) < 3.5f) return false;
                return encounters.All(e => Vector2.Distance(p, new(e.transform.position.x, e.transform.position.z)) > 6);
            }
            int id = 0;
            for (float z = 30; z < 140; z += 11) for (float x = -44; x <= 44; x += 11)
                Grass("ridge-" + id++, new Vector2(x + 2 * Mathf.Sin(z), z), new Vector2(5.8f, 4.8f), "Sorrel", .52f, Verge);
            foreach (var p in new[] {new Vector2(-22,9), new Vector2(-22,25), new Vector2(22,10), new Vector2(23,25), new Vector2(-5,24), new Vector2(8,25), new Vector2(-17,-3)})
                Grass("shelter-" + id++, p, new Vector2(3.8f,2.7f), "Sorrel", .42f);
            var ground = terrain.GetComponent<Renderer>().sharedMaterial;
            ground.EnableKeyword("_DETAIL_ON"); ground.SetFloat("_Detail", 1); ground.SetFloat("_BaseMapStrength", .8f);
            ground.SetVector("_DetailParams", new Vector4(.058f,.45f,.5f,.09f));
            ground.SetColor("_DetailTintA", new Color(.63f,.74f,.73f)); ground.SetColor("_DetailTintB", new Color(1.08f,1.04f,.97f));
            EditorUtility.SetDirty(ground);
        }

        static void Yard()
        {
            int id = 0;
            foreach (var p in new[] {new Vector2(-18,-13),new Vector2(-18,-3),new Vector2(-18,8),new Vector2(-13,19),new Vector2(0,20),new Vector2(12,19),new Vector2(18,5),new Vector2(18,-4)})
                Grass("yard-" + id++, p, new Vector2(2.3f,2.6f), "Sorrel", .42f, null, -.19f);
        }

        static void Nursery()
        {
            terrain = GameObject.Find("Cave rock and floor").GetComponent<MeshCollider>();
            // Sculpt actual shallow basins. Their floors never cover the water surface.
            var mesh = terrain.sharedMesh; var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i]; float depth = 0;
                foreach (var pool in NurseryPools)
                {
                    var q=new Vector2((v.x-pool.x)/pool.z,(v.z-pool.y)/pool.w);
                    float r = q.magnitude/WaterRadius(Mathf.Atan2(q.y,q.x));
                    depth = Mathf.Max(depth, .38f * (1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.65f,1.18f,r))));
                }
                if (depth > 0 && v.y < HushwellLayout.Lower + .2f) vertices[i].y = HushwellLayout.Lower - depth;
            }
            mesh.vertices = vertices; mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
            terrain.sharedMesh = null; terrain.sharedMesh = mesh; Physics.SyncTransforms();
            int id = 0;
            foreach (var pool in NurseryPools)
            {
                Water("nursery-pool-" + id, new Vector3(pool.x,-16.085f,pool.y), new Vector2(pool.z,pool.w), "Nursery");
                var center = new Vector2(pool.x,pool.y);
                Grass("nursery-bank-" + id++, center, new Vector2(pool.z + 1.2f,pool.w + 1.2f), "Nursery", .36f,
                    p => new Vector2((p.x-center.x)/pool.z,(p.y-center.y)/pool.w).magnitude > 1.06f);
            }
        }

        static void Refuge()
        {
            // A low recycling garden against the starboard hull, outside the public aisle.
            foreach (var item in new[] {(new Vector3(10,.30f,-2),new Vector3(.22f,.6f,5)),(new Vector3(12,.30f,-2),new Vector3(.22f,.6f,5)),
                (new Vector3(11,.30f,-4.5f),new Vector3(2,.6f,.22f)),(new Vector3(11,.30f,.5f),new Vector3(2,.6f,.22f))})
            {
                var rim = WorldBuilder.Piece("DeckWall", item.Item1, item.Item2); rim.name = "Recycling garden rim"; rim.transform.SetParent(root, true);
            }
            var basePlate = WorldBuilder.Piece("DeckFloor",new Vector3(11,.20f,-2),new Vector3(2,.16f,5));
            basePlate.name = "Recycling garden bed"; basePlate.transform.SetParent(root,true);
            Water("refuge-cistern",new Vector3(11,.35f,-2),new Vector2(.87f,2.34f),"Refuge");
            Grass("refuge-garden", new Vector2(11,-2), new Vector2(.7f,2), "Refuge", .3f, p => p.x > 11.2f || p.y > -.5f || p.y < -3.4f, .34f, false);
        }

        static void Grass(string id, Vector2 center, Vector2 radius, string palette, float height, Func<Vector2,bool> allowed = null, float? floor = null, bool avoidProps = true)
        {
            var points = new List<Vector4>();
            int seed = 17; foreach (char c in zone + id) seed = unchecked(seed * 31 + c);
            var random = new System.Random(seed);
            for (float z = -radius.y; z < radius.y; z += .62f) for (float x = -radius.x; x < radius.x; x += .62f)
            {
                var p = center + new Vector2(x + (float)random.NextDouble() * .5f, z + (float)random.NextDouble() * .5f);
                float r = new Vector2((p.x-center.x)/radius.x,(p.y-center.y)/radius.y).magnitude;
                if (r > 1 || random.NextDouble() > (1-r*.55f) || Mathf.PerlinNoise(p.x*.38f+173,p.y*.38f+291) < .39f || allowed != null && !allowed(p)) continue;
                float y = floor ?? 0;
                if (floor == null)
                {
                    if (terrain == null || !terrain.Raycast(new Ray(new Vector3(p.x,30,p.y),Vector3.down),out var hit,90) || hit.normal.y < .82f) continue;
                    y = hit.point.y + .008f;
                    if (zone == "Sorrel_Ridges" && y > 3.4f) continue;
                }
                var pos = new Vector3(p.x,y,p.y);
                if (avoidProps && solids.Any(c => c != terrain && c.bounds.size.y > .3f && c.bounds.Contains(pos + Vector3.up*.2f))) continue;
                points.Add(new Vector4(pos.x,pos.y,pos.z,height * Mathf.Lerp(.65f,1,(float)random.NextDouble())));
            }
            if (points.Count < 3) return;
            var patch = Patch(id,palette,false,Vector3.zero);
            // Store positions relative to a compact patch origin for sensible culling and wind.
            patch.transform.position = new Vector3(center.x, floor ?? points[0].y, center.y);
            patch.sprouts = points.Select(p => new Vector4(p.x-patch.transform.position.x,p.y-patch.transform.position.y,p.z-patch.transform.position.z,p.w)).ToArray();
            var card = new Mesh {name = "Generated grass card"};
            card.vertices = new[]{new Vector3(-.48f,0,0),new Vector3(.48f,0,0),new Vector3(-.48f,1,0),new Vector3(.48f,1,0)};
            card.uv = new[]{new Vector2(.04f,.065f),new Vector2(.96f,.065f),new Vector2(.04f,.95f),new Vector2(.96f,.95f)};
            card.triangles = new[]{0,2,1,1,2,3}; card.RecalculateNormals();
            var combines = new List<CombineInstance>();
            foreach (var p in patch.sprouts) for (int face = 0; face < 2; face++)
                combines.Add(new CombineInstance{mesh=card,transform=Matrix4x4.TRS(new Vector3(p.x,p.y,p.z),Quaternion.Euler(0,Yaw(p)+face*90,0),Vector3.one*p.w)});
            var mesh = new Mesh {name=zone+"-"+id}; mesh.CombineMeshes(combines.ToArray()); mesh.RecalculateBounds();
            SaveMesh(patch,mesh); Object.DestroyImmediate(card);
        }

        public static float Yaw(Vector4 p) => Mathf.Repeat((p.x * 37.73f + p.z * 13.39f) * 137.5f,360);
        static float WaterRadius(float a)=>.91f+.065f*Mathf.Sin(a*3)+.035f*Mathf.Sin(a*7+1);

        static void Water(string id, Vector3 center, Vector2 radius, string palette)
        {
            var patch = Patch(id,palette,true,center);
            const int segments=64, rings=5;
            var vertices = new List<Vector3>{Vector3.zero}; var uv = new List<Vector2>{Vector2.one*.5f}; var triangles=new List<int>();
            float narrow=Mathf.Min(radius.x,radius.y);
            var shoreline=new List<Vector2>{new Vector2(0,narrow)};
            for(int r=1;r<=rings;r++)for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments, t=(float)r/rings;
                float edge=WaterRadius(a);
                var q=palette=="Refuge"?new Vector2(Mathf.Sign(Mathf.Cos(a))*Mathf.Pow(Mathf.Abs(Mathf.Cos(a)),.16f),Mathf.Sign(Mathf.Sin(a))*Mathf.Pow(Mathf.Abs(Mathf.Sin(a)),.16f))*t:
                    new Vector2(Mathf.Cos(a),Mathf.Sin(a))*t*edge;
                vertices.Add(new Vector3(q.x*radius.x,0,q.y*radius.y)); uv.Add(q*.48f+Vector2.one*.5f);
                shoreline.Add(new Vector2(0,(1-t)*narrow));
                int current=1+(r-1)*segments+i, next=1+(r-1)*segments+(i+1)%segments;
                if(r==1)triangles.AddRange(new[]{0,next,current});
                else {int inner=current-segments,innerNext=next-segments;triangles.AddRange(new[]{inner,innerNext,current,current,innerNext,next});}
            }
            var mesh = new Mesh {name=zone+"-"+id};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetUVs(1,shoreline);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();SaveMesh(patch,mesh);
        }

        static BiomePatch Patch(string id,string palette,bool water,Vector3 position)
        {
            var go=new GameObject(id,typeof(MeshFilter),typeof(MeshRenderer),typeof(BiomePatch));go.transform.SetParent(root);go.transform.position=position;
            var patch=go.GetComponent<BiomePatch>();patch.resourceKey=zone+"-"+id;patch.water=water;patch.palette=palette;
            var renderer=go.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.sharedMaterial=Fallback(palette,water);return patch;
        }

        static Material Fallback(string palette,bool water)
        {
            string path=Art+palette+(water?"-water":"-grass")+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Lattice/Toon"));AssetDatabase.CreateAsset(material,path);}
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+(water?"CalmWater":"GrassClump")+".png"));
            material.SetColor("_BaseColor",water?new Color(.48f,.70f,.70f):palette=="Nursery"?new Color(.56f,.92f,.89f):Color.white);
            material.SetFloat("_Cull",0);material.SetFloat("_ReceiveShadows",1);
            if(!water){material.EnableKeyword("_ALPHATEST_ON");material.SetFloat("_AlphaTest",1);material.SetFloat("_Cutoff",.4f);}
            else
            {
                material.EnableKeyword("_WATER_ON");material.SetFloat("_Water",1);
                material.SetVector("_WaterParams",new Vector4(.6f,.12f,.65f,.04f));
                material.SetVector("_EdgeParams",new Vector4(1,.1f,.05f,.5f));
                material.SetVector("_FoamParams",new Vector4(.1f,.6f,.06f,.18f));
                material.SetColor("_FoamColor",new Color(.48f,.61f,.58f,1));
                material.SetColor("_DeepColor",new Color(.07f,.22f,.26f,1));
            }
            EditorUtility.SetDirty(material);return material;
        }

        static void SaveMesh(BiomePatch patch,Mesh mesh)
        {
            string path=Art+patch.resourceKey+".asset";
            var prior=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(prior==null){AssetDatabase.CreateAsset(mesh,path);prior=mesh;}
            else{EditorUtility.CopySerialized(mesh,prior);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(prior);}
            patch.GetComponent<MeshFilter>().sharedMesh=prior;
        }
    }
}
