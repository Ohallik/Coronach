using System.Linq;
using Lattice.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Lattice.Tests.EditMode
{
    public sealed class FlightSocketTests
    {
        [TestCase("Taren")]
        [TestCase("Sela")]
        public void FlightCollisionContainsTheActualMeshThroughEveryBankAndRoll(string hero)
        {
            var hull=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/"+hero+"Flight.prefab"));
            try
            {
                var points=hull.GetComponentsInChildren<MeshFilter>().SelectMany(f=>f.sharedMesh.vertices.Select(v=>hull.transform.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();
                float furthest=points.Max(p=>p.magnitude);
                Assert.GreaterOrEqual(HeroCollision.HullRadius(hero)-.005f,furthest,"stable flight sphere fails to contain rotated hull geometry");
                Assert.Less(HeroCollision.HullRadius(hero)-furthest,.04f,"movement footprint is unnecessarily inflated beyond measured hull");
            }
            finally{Object.DestroyImmediate(hull);}
        }
        [TestCase("TarenFlight",4)]
        [TestCase("SelaFlight",3)]
        public void AttachmentsRemainOnTheMeasuredGeneratedHull(string id,int engineCount)
        {
            var hull=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/"+id+".prefab"));
            try
            {
                var sockets=hull.GetComponent<FlightSockets>();Assert.IsNotNull(sockets);Assert.IsNotNull(sockets.muzzle);
                var points=hull.GetComponentsInChildren<MeshFilter>().SelectMany(f=>f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v))).ToArray();
                float nose=points.Max(p=>p.z);
                Assert.That(sockets.muzzle.position.z-nose,Is.InRange(-.02f,.08f),"muzzle must meet the real nose tip");
                Assert.Less(points.Min(p=>Vector3.Distance(p,sockets.muzzle.position)),.09f);
                Assert.AreEqual(engineCount,sockets.engines.Length);
                foreach(var engine in sockets.engines)
                {
                    Assert.Less(points.Min(p=>Vector3.Distance(p,engine.position)),.13f,engine.name+" floats beyond the generated nozzle");
                    Assert.Less(engine.localPosition.z,-.7f);Assert.Less(Vector3.Dot(engine.forward,hull.transform.forward),-.99f);
                }
            }
            finally{Object.DestroyImmediate(hull);}
        }
    }
}
