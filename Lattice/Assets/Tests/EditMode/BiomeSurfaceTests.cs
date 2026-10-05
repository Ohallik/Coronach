using System.Collections.Generic;
using System.Linq;
using Lattice.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lattice.Tests.EditMode
{
    public sealed class BiomeSurfaceTests
    {
        [Test]
        public void NurseryWaterMeshEndsUnderTheBankInsteadOfCuttingThroughOpenWater()
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Hushwell.unity",OpenSceneMode.Additive);
            try
            {
                var ground=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshCollider>()).Single(c=>c.name=="Cave rock and floor");
                var pools=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<BiomePatch>()).Where(p=>p.water).ToArray();
                Assert.AreEqual(3,pools.Length);Physics.SyncTransforms();
                foreach(var pool in pools)
                {
                    var mesh=pool.GetComponent<MeshFilter>().sharedMesh;
                    var vertices=mesh.vertices;var triangles=mesh.triangles;
                    var edges=new Dictionary<(int,int),int>();
                    for(int t=0;t<triangles.Length;t+=3)for(int e=0;e<3;e++)
                    {
                        int a=triangles[t+e],b=triangles[t+(e+1)%3];
                        var edge=a<b?(a,b):(b,a);edges.TryGetValue(edge,out int count);edges[edge]=count+1;
                    }
                    var boundary=edges.Where(e=>e.Value==1).Select(e=>e.Key).ToArray();
                    Assert.IsNotEmpty(boundary,"the water surface has no boundary");
                    foreach(var edge in boundary)foreach(float t in new[]{0f,.5f,1f})
                    {
                        var at=pool.transform.TransformPoint(Vector3.Lerp(vertices[edge.Item1],vertices[edge.Item2],t));
                        Assert.IsTrue(ground.Raycast(new Ray(at+Vector3.up*2,Vector3.down),out var hit,4),"water extends beyond its basin ground");
                        Assert.LessOrEqual(at.y,hit.point.y+.002f,
                            pool.name+" ends above its submerged bed at "+at+"; the mesh edge creates a hard cut");
                    }
                }
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
        }

        [Test]
        public void PublicWaterHasADryRimAndADeepInteriorInsteadOfSolidFoam()
        {
            foreach(var zone in new[]{"Hushwell","TallowDrift"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity",OpenSceneMode.Additive);
                try
                {
                    foreach(var pool in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<BiomePatch>()).Where(p=>p.water))
                    {
                        var mesh=pool.GetComponent<MeshFilter>().sharedMesh;var material=pool.GetComponent<MeshRenderer>().sharedMaterial;
                        var rim=mesh.uv2;
                        Assert.AreEqual(mesh.vertexCount,rim.Length,"missing world distance to the shoreline: "+pool.name);
                        Assert.AreEqual(1,material.GetVector("_EdgeParams").x,"water must use the baked disc shoreline");
                        float foam=material.GetVector("_FoamParams").x;
                        Assert.Greater(rim[0].y,foam*3,"foam fills the pool's centre");
                        Assert.IsTrue(rim.Any(v=>v.y<.01f),"pool has no shallow edge");
                        Assert.IsTrue(rim.All(v=>v.y>=0));
                    }
                }
                finally{EditorSceneManager.CloseScene(scene,true);}
            }
        }

        [Test]
        public void WaterHasAValidTangentFrameForAnimatedNormals()
        {
            foreach(var zone in new[]{"Hushwell","TallowDrift"})
            {
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity",OpenSceneMode.Additive);
                try
                {
                    foreach(var pool in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<BiomePatch>()).Where(p=>p.water))
                    {
                        var mesh=pool.GetComponent<MeshFilter>().sharedMesh;var tangents=mesh.tangents;var normals=mesh.normals;
                        Assert.AreEqual(mesh.vertexCount,tangents.Length,"normal-mapped water has no tangents: "+pool.name);
                        for(int i=0;i<tangents.Length;i++)
                        {
                            var tangent=new Vector3(tangents[i].x,tangents[i].y,tangents[i].z);
                            Assert.That(tangent.magnitude,Is.EqualTo(1).Within(.01f));
                            Assert.That(Vector3.Dot(tangent,normals[i]),Is.EqualTo(0).Within(.01f));
                            Assert.That(Mathf.Abs(tangents[i].w),Is.EqualTo(1).Within(.01f));
                        }
                    }
                }
                finally{EditorSceneManager.CloseScene(scene,true);}
            }
        }

        [Test]
        public void NurseryWaterSitsInRealBasinsWithDryAccessToTheDiscoveryAndLift()
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Hushwell.unity",OpenSceneMode.Additive);
            try
            {
                var ground=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshCollider>()).Single(c=>c.name=="Cave rock and floor");
                var water=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<BiomePatch>()).Where(p=>p.water).ToArray();
                Assert.AreEqual(3,water.Length);
                Physics.SyncTransforms();
                foreach(var pool in water)
                {
                    Assert.IsTrue(ground.Raycast(new Ray(pool.transform.position+Vector3.up*3,Vector3.down),out var hit,6));
                    Assert.Greater(pool.transform.position.y-hit.point.y,.18f,"water lies on intact ground: "+pool.name);
                    Assert.Less(pool.transform.position.y-hit.point.y,.45f,"a decorative pool became a fall hazard");
                }
                foreach(var at in new[]{new Vector3(14,-13,328),new Vector3(14,-13,334),new Vector3(22,-13,339),new Vector3(27,-13,338)})
                {
                    Assert.IsTrue(ground.Raycast(new Ray(at,Vector3.down),out var hit,6));
                    Assert.That(hit.point.y,Is.EqualTo(-16).Within(.025f),"the main discovery/lift access was submerged");
                }
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
        }

        [Test]
        public void SorrelBoreCutsTheGroundWhileItsApproachStaysLevel()
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Sorrel_Ridges.unity",OpenSceneMode.Additive);
            try
            {
                var ground=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshCollider>()).Single(c=>c.name=="Continuous eroded basin");
                Physics.SyncTransforms();
                Assert.IsTrue(ground.Raycast(new Ray(new Vector3(8,4,177),Vector3.down),out var cut,10));
                Assert.Less(cut.point.y,-2,"the scaffold still stands above intact sand");
                Assert.IsTrue(ground.Raycast(new Ray(new Vector3(8,4,174),Vector3.down),out var approach,10));
                Assert.That(approach.point.y,Is.EqualTo(-.1f).Within(.025f));
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
        }

        [TestCase("Sorrel_Ridges", false)]
        [TestCase("Arena_Ground", false)]
        [TestCase("Hushwell", true)]
        [TestCase("TallowDrift", true)]
        public void ShippedLandscapeHasUsablePublicSurfaces(string zone, bool needsWater)
        {
            var path = "Assets/_Project/Scenes/" + zone + ".unity";
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var patches = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BiomePatch>(true)).ToArray();
                Assert.IsNotEmpty(patches, zone + " still has no authored biome surface");
                Assert.IsTrue(patches.Any(p => !p.water), "missing low vegetation");
                if (needsWater) Assert.IsTrue(patches.Any(p => p.water), "missing contained water");
                foreach (var patch in patches)
                {
                    var filter = patch.GetComponent<MeshFilter>(); var renderer = patch.GetComponent<MeshRenderer>();
                    Assert.IsNotNull(filter.sharedMesh); Assert.Greater(filter.sharedMesh.vertexCount, 3);
                    Assert.IsTrue(renderer.enabled); Assert.IsNotNull(renderer.sharedMaterial);
                    Assert.AreEqual("Lattice/Toon", renderer.sharedMaterial.shader.name, "serialized fallback must open without paid shaders");
                    Assert.IsFalse(patch.GetComponentsInChildren<Collider>().Any(), "dressing must not block movement");
                    if (!patch.water)
                    {
                        Assert.IsNotEmpty(patch.sprouts);
                        Assert.IsTrue(patch.sprouts.All(p => p.w > 0 && p.w <= .6f), "grass obscures the fixed camera's targets");
                    }
                }
                var dependencies = AssetDatabase.GetDependencies(path, true);
                Assert.IsFalse(dependencies.Any(d => d.Contains("Stylized Water") || d.Contains("staggart-creations") || d.Contains("LocalStaggart")),
                    "a public scene depends on licensed local files");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void MissingLocalArtLeavesTheWorkingFallbackIntact()
        {
            var go = new GameObject("Missing purchase", typeof(MeshFilter), typeof(MeshRenderer));
            var mesh = new Mesh(); var material = new Material(Shader.Find("Lattice/Toon"));
            try
            {
                go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material;
                var patch = go.AddComponent<BiomePatch>(); patch.resourceKey = "absent-purchase-control";
                Assert.IsFalse(patch.ApplyLocalArt());
                Assert.AreSame(mesh, go.GetComponent<MeshFilter>().sharedMesh);
                Assert.AreSame(material, go.GetComponent<MeshRenderer>().sharedMaterial);
                Assert.IsTrue(go.GetComponent<MeshRenderer>().enabled);
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(mesh); Object.DestroyImmediate(material); }
        }
    }
}
