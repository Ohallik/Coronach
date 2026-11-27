using System.Linq;
using Lattice.Data;
using NUnit.Framework;
using UnityEngine;

namespace Lattice.Tests.PlayMode
{
    /// <summary>W1-mooring-05, round ten: the flat grey blotches on the Tollkeeper's white hull (nov25-whole s65/056, 064, 126, 131). D419 took them for the generated texture's window band on small
    /// faces; drawn bare through the fight's camera the texture is white where the blotches are (r10-hull-before, the render beside its bare texture). They were faces whose three corner normals,
    /// as the model carried them and Unity imported them, stood 60 to 90 degrees off the face's own facing, so the toon shader gave each a shade of its own, flat slate, beside neighbours in the sun:
    /// 109 of the hull's 11,400 faces (141 of its 10,135 square metres, 42 of them facing the fight's camera) and 29 of the outrigger's 5,968 (2.8 of 984), read from the FBX's corner normals
    /// (tools/blender/dump_corner_normals.py). Both models now take normals calculated from their faces at the importer (60 degrees, area and angle weighted); their texture, emission, UVs and
    /// material are as they were. The measure is shown first on a fixture with one face turned, then read on both bodies as the game loads them.</summary>
    public sealed class TollkeeperHullTests
    {
        /// <summary>Faces whose three corner normals all stand more than 60 degrees off the face's own facing, and their area in the body's own units. Winding is read from the mesh itself
        /// (the sign under which most of its area agrees with its normals).</summary>
        static (int faces,float area,float total,float agreeing) Turned(Mesh mesh)
        {
            var v=mesh.vertices;var n=mesh.normals;var t=mesh.triangles;
            Assert.AreEqual(v.Length,n.Length,"fixture: "+mesh.name+" carries a normal at every vertex");
            float plus=0,minus=0;
            var facing=new Vector3[t.Length/3];var area=new float[t.Length/3];
            for(int i=0;i<t.Length;i+=3)
            {
                // Divided by hand: the model is one unit long before its prefab scales it, and Vector3.normalized gives zero under 1e-5, which a small face's cross product is.
                var c=Vector3.Cross(v[t[i+1]]-v[t[i]],v[t[i+2]]-v[t[i]]);float m=c.magnitude;area[i/3]=m*.5f;facing[i/3]=m>0?c/m:Vector3.zero;if(m<=0)continue;
                float d=Vector3.Dot(facing[i/3],(n[t[i]]+n[t[i+1]]+n[t[i+2]]).normalized);if(d>=0)plus+=area[i/3];else minus+=area[i/3];
            }
            float sign=plus>=minus?1:-1;int faces=0;float turned=0;
            for(int i=0;i<t.Length;i+=3)
            {
                var f=facing[i/3]*sign;if(f==Vector3.zero)continue;
                if(Vector3.Dot(f,n[t[i]])<.5f&&Vector3.Dot(f,n[t[i+1]])<.5f&&Vector3.Dot(f,n[t[i+2]])<.5f){faces++;turned+=area[i/3];}
            }
            return (faces,turned,plus+minus,Mathf.Max(plus,minus)/Mathf.Max(plus+minus,1e-12f));
        }

        static Mesh Quad(bool turned)
        {
            var mesh=new Mesh{name=turned?"quad, one face turned":"quad"};
            // A centimetre across, as the hull's small faces are in the model's own units.
            mesh.vertices=new[]{new Vector3(0,0,0),new Vector3(0,0,1),new Vector3(1,0,1),new Vector3(0,0,0),new Vector3(1,0,1),new Vector3(1,0,0)}.Select(p=>p*.001f).ToArray();
            var up=Vector3.up;var side=Vector3.forward;
            mesh.normals=turned?new[]{up,up,up,side,side,side}:new[]{up,up,up,up,up,up};
            mesh.triangles=new[]{0,1,2,3,4,5};
            return mesh;
        }

        [Test] public void NoFaceOfTheHullIsShadedAsIfItFacedElsewhere()
        {
            var wrong=Turned(Quad(true));var right=Turned(Quad(false));
            Assert.AreEqual(1,wrong.faces,"fixture: a face whose corners' normals are turned a right angle from it is found");
            Assert.AreEqual(0,right.faces,"fixture: a flat quad lit as it faces has no turned face");
            var def=Resources.Load<EnemyDef>("Definitions/EnemyDef/Tollkeeper");Assert.IsNotNull(def,"fixture: the Tollkeeper's definition");
            foreach(var (what,body) in new[]{("the carrier",def.prefab),("its outrigger",def.bodySegment)})
            {
                Assert.IsNotNull(body,"fixture: "+what+"'s body");
                var meshes=body.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).Where(m=>m!=null).Distinct().ToList();
                Assert.IsNotEmpty(meshes,"fixture: "+what+" draws a mesh");
                foreach(var mesh in meshes)
                {
                    var r=Turned(mesh);
                    TestContext.WriteLine($"TOLL_HULL {what} {mesh.name} triangles={mesh.triangles.Length/3} turned={r.faces} area={r.area/Mathf.Max(r.total,1e-12f):P2} agreeing={r.agreeing:P1}");
                    Assert.Greater(r.agreeing,.9f,$"{what} ({mesh.name}): most of its area is lit by normals on its own side (the winding is read right)");
                    // A mesh measure, the same every run. As imported before: the carrier 109 faces, 1.39% of its area (r10hull-before3). Calculated: 33 faces, under .005% (r10hull-after),
                    // slivers of no drawn size in creases. The bar is on what is drawn: .05% of the area.
                    float share=r.area/Mathf.Max(r.total,1e-12f);
                    Assert.Less(share,.0005f,$"{what} ({mesh.name}): {r.faces} faces ({share:P3} of its area) are shaded by normals turned more than 60 degrees from them, each a flat patch of the wrong shade");
                }
            }
        }
    }
}
