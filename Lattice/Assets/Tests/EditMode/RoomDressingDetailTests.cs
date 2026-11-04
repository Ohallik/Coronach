using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lattice.Tests.EditMode
{
    /// <summary>
    /// D192 detail layer: the flat floor details (guide studs, pictograms, hazard paint, inset
    /// grates, ground patches, tyre tracks) must lie on the real ground: the deck floor, or on
    /// Sorrel the terrain itself, never a rock, roof or pad a decal draped over. Checked on the
    /// saved scenes, where the generated meshes are still readable (play mode static-batches them).
    /// </summary>
    public sealed class RoomDressingDetailTests
    {
        static Transform Dressing()=>EditorSceneManager.GetActiveScene().GetRootGameObjects().Single(g=>g.name=="Room dressing").transform;

        static List<string> Floating(Transform root,bool terrain)
        {
            Physics.SyncTransforms();
            var bad=new List<string>();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("Floor detail")))
            {
                var m=filter.transform.localToWorldMatrix;
                foreach(var local in filter.sharedMesh.vertices)
                {
                    var v=m.MultiplyPoint3x4(local);float? ground=null;
                    foreach(var hit in Physics.RaycastAll(v+Vector3.up*(terrain?40:.3f),Vector3.down,terrain?80:1.5f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
                    {
                        if(hit.collider.transform.IsChildOf(root))continue;
                        if(terrain?hit.collider.bounds.size.x>100&&hit.collider.bounds.size.z>100:hit.normal.y>.5f){ground=hit.point.y;break;}
                    }
                    float lift=ground.HasValue?v.y-ground.Value:float.PositiveInfinity;
                    if(lift>.15f||lift<-.05f){bad.Add($"{filter.name} vertex {v} is {lift:0.00} m above the ground");break;}
                }
            }
            return bad;
        }

        [Test] public void FloorDetailsLieOnTheGround([Values("Hub_Decks","TallowDrift","Sorrel_Ridges")] string zone)
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity",OpenSceneMode.Single);
            var root=Dressing();
            Assert.Greater(root.GetComponentsInChildren<MeshFilter>().Count(f=>f.name.StartsWith("Floor detail")),10,zone+" has almost no floor detail");
            var bad=Floating(root,zone=="Sorrel_Ridges");
            Assert.IsEmpty(bad,zone+": floor details float or sink:\n"+string.Join("\n",bad.Take(12)));
        }

        [Test] public void TheGroundCheckCatchesAFloatingDetail([Values("Hub_Decks","Sorrel_Ridges")] string zone)
        {
            // Negative control: a decal bridging 2.5 m above the ground (as tyre tracks draped over a rock once did) must be reported.
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity",OpenSceneMode.Single);
            var root=Dressing();var at=zone=="Sorrel_Ridges"?new Vector3(-5,0,96):new Vector3(-20,0,-4.6f);
            Physics.SyncTransforms();
            if(Physics.Raycast(at+Vector3.up*40,Vector3.down,out var floor,80,~0,QueryTriggerInteraction.Ignore))at.y=floor.point.y;
            var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Floor detail negative control";Object.DestroyImmediate(quad.GetComponent<Collider>());
            try
            {
                quad.transform.SetParent(root,false);quad.transform.SetPositionAndRotation(at+Vector3.up*2.5f,Quaternion.Euler(90,0,0));
                var bad=Floating(root,zone=="Sorrel_Ridges");
                Assert.IsTrue(bad.Any(b=>b.StartsWith("Floor detail negative control")),zone+": the ground check missed a decal floating 2.5 m up");
            }
            finally{Object.DestroyImmediate(quad);}
        }
    }
}
