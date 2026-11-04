using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Lattice.Tests.EditMode
{
    /// <summary>
    /// D192 detail layer: the flat floor details (guide studs, pictograms, hazard paint, inset
    /// grates, ground patches, tyre tracks) must lie on the real ground: the deck floor, or on
    /// Sorrel the terrain itself, never a rock, roof or pad a decal draped over. D193 adds Hushwell
    /// (its cave floor) and the Gullet (its membrane bed, which must also stay far below the ships'
    /// flight plane with nothing solid on it), and Hushwell's lit render chunks. Checked on the
    /// saved scenes, where the generated meshes are still readable (play mode static-batches them).
    /// </summary>
    public sealed class RoomDressingDetailTests
    {
        /// <summary>The Gullet's ships fly at y=1; dressing on its bed stays under y=-0.6 (RoomDressing.FlightCeiling, D193).</summary>
        const float FlightCeiling=-.6f;

        static Transform Dressing()=>EditorSceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="Room dressing").transform;

        /// <summary>What counts as the ground a detail must lie on, per zone.</summary>
        static Func<RaycastHit,bool> GroundOf(string zone)=>zone switch
        {
            "Sorrel_Ridges" or "Hushwell"=>h=>h.collider.bounds.size.x>100&&h.collider.bounds.size.z>100,
            "Gullet_Tunnel"=>h=>h.collider.name=="Gullet membrane shell",
            _=>h=>h.normal.y>.5f
        };
        static bool FromAbove(string zone)=>zone is "Sorrel_Ridges" or "Hushwell" or "Gullet_Tunnel";

        static List<string> Floating(Transform root,string zone)
        {
            Physics.SyncTransforms();
            var bad=new List<string>();var ground=GroundOf(zone);bool above=FromAbove(zone);
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("Floor detail")))
            {
                var m=filter.transform.localToWorldMatrix;
                foreach(var local in filter.sharedMesh.vertices)
                {
                    var v=m.MultiplyPoint3x4(local);float? floor=null;
                    foreach(var hit in Physics.RaycastAll(v+Vector3.up*(above?40:.3f),Vector3.down,above?80:1.5f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
                    {
                        if(hit.collider.transform.IsChildOf(root))continue;
                        if(ground(hit)){floor=hit.point.y;break;}
                    }
                    float lift=floor.HasValue?v.y-floor.Value:float.PositiveInfinity;
                    if(lift>.15f||lift<-.05f){bad.Add($"{filter.name} vertex {v} is {lift:0.00} m above the ground");break;}
                }
            }
            return bad;
        }

        [Test] public void FloorDetailsLieOnTheGround([Values("Hub_Decks","TallowDrift","Sorrel_Ridges","Hushwell","Gullet_Tunnel")] string zone)
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity",OpenSceneMode.Single);
            var root=Dressing();
            Assert.Greater(root.GetComponentsInChildren<MeshFilter>().Count(f=>f.name.StartsWith("Floor detail")),10,zone+" has almost no floor detail");
            var bad=Floating(root,zone);
            Assert.IsEmpty(bad,zone+": floor details float or sink:\n"+string.Join("\n",bad.Take(12)));
        }

        [Test] public void TheGroundCheckCatchesAFloatingDetail([Values("Hub_Decks","Sorrel_Ridges","Hushwell","Gullet_Tunnel")] string zone)
        {
            // Negative control: a decal bridging 2.5 m above the ground (as tyre tracks draped over a rock once did) must be reported.
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity",OpenSceneMode.Single);
            var root=Dressing();
            var at=zone switch{"Sorrel_Ridges"=>new Vector3(-5,0,96),"Hushwell"=>new Vector3(0,0,164),"Gullet_Tunnel"=>new Vector3(0,0,165),_=>new Vector3(-20,0,-4.6f)};
            Physics.SyncTransforms();
            if(Physics.Raycast(at+Vector3.up*40,Vector3.down,out var floor,80,~0,QueryTriggerInteraction.Ignore))at.y=floor.point.y;
            var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Floor detail negative control";Object.DestroyImmediate(quad.GetComponent<Collider>());
            try
            {
                quad.transform.SetParent(root,false);quad.transform.SetPositionAndRotation(at+Vector3.up*2.5f,Quaternion.Euler(90,0,0));
                var bad=Floating(root,zone);
                Assert.IsTrue(bad.Any(b=>b.StartsWith("Floor detail negative control")),zone+": the ground check missed a decal floating 2.5 m up");
            }
            finally{Object.DestroyImmediate(quad);}
        }

        // ---------- the Gullet: nothing on its bed may touch flight ----------
        static List<string> TouchesFlight(Transform root)
        {
            var bad=root.GetComponentsInChildren<Renderer>().Where(r=>r.bounds.max.y>FlightCeiling).Select(r=>$"{r.name} rises to y={r.bounds.max.y:0.00}").ToList();
            bad.AddRange(root.GetComponentsInChildren<Collider>().Select(c=>$"{c.name} has a collider"));
            return bad;
        }

        [Test] public void TheGulletBedStaysFarBelowTheFlightPlane()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gullet_Tunnel.unity",OpenSceneMode.Single);
            var root=Dressing();
            Assert.Greater(root.GetComponentsInChildren<Renderer>().Length,50,"the Gullet bed has almost no dressing");
            var bad=TouchesFlight(root);
            Assert.IsEmpty(bad,"Gullet dressing reaches toward the flight plane:\n"+string.Join("\n",bad.Take(12)));
        }

        [Test] public void TheFlightCheckCatchesAPieceAtShipHeight()
        {
            // Negative control: a solid block standing up to the ships' plane on the bed must be reported twice (height and collider).
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Gullet_Tunnel.unity",OpenSceneMode.Single);
            var root=Dressing();
            var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.name="Flight negative control";
            try
            {
                block.transform.SetParent(root,false);block.transform.position=new Vector3(0,-2,165);block.transform.localScale=new Vector3(2,4,2);
                var bad=TouchesFlight(root);
                Assert.AreEqual(2,bad.Count(b=>b.StartsWith("Flight negative control")),"the flight check missed a block reaching y=0:\n"+string.Join("\n",bad));
            }
            finally{Object.DestroyImmediate(block);}
        }

        // ---------- Hushwell: the cave is drawn in lit chunks ----------
        /// <summary>Index count drawn by the chunks, or -1 with a reason when the cave is not chunked as required.</summary>
        static int ChunkCoverage(out string why,int skip=-1)
        {
            why=null;
            var cave=GameObject.Find("Cave rock and floor");
            var chunks=EditorSceneManager.GetActiveScene().GetRootGameObjects().SingleOrDefault(g=>g.name=="Lit render chunks");
            if(cave==null||chunks==null){why="the cave or its chunk root is missing";return -1;}
            if(cave.GetComponent<MeshRenderer>().enabled){why="the whole-cave renderer is still on, so Forward's per-object light limit applies to the whole cave";return -1;}
            var collider=cave.GetComponent<MeshCollider>();
            if(collider==null||!collider.enabled||collider.sharedMesh!=cave.GetComponent<MeshFilter>().sharedMesh){why="the cave's collision changed";return -1;}
            int drawn=0,i=0;
            foreach(var filter in chunks.GetComponentsInChildren<MeshFilter>())
            {
                if(i++==skip)continue;
                var b=filter.GetComponent<MeshRenderer>().bounds;
                if(b.size.x>18||b.size.z>18){why=filter.name+" spans "+b.size+", too wide to keep its own lights";return -1;}
                for(int s=0;s<filter.sharedMesh.subMeshCount;s++)drawn+=(int)filter.sharedMesh.GetIndexCount(s);
            }
            return drawn;
        }

        [Test] public void HushwellIsDrawnInLitChunksThatCoverTheWholeCave()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Hushwell.unity",OpenSceneMode.Single);
            var mesh=GameObject.Find("Cave rock and floor").GetComponent<MeshFilter>().sharedMesh;
            int total=Enumerable.Range(0,mesh.subMeshCount).Sum(s=>(int)mesh.GetIndexCount(s));
            int drawn=ChunkCoverage(out var why);
            Assert.IsNull(why,why);
            Assert.AreEqual(total,drawn,"the lit chunks do not draw exactly the cave's triangles");
            // Negative control: a missing chunk (a hole in the cave) is caught.
            Assert.AreNotEqual(total,ChunkCoverage(out _,skip:3),"dropping a chunk went unnoticed");
        }
    }
}
