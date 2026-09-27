using Lattice.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Lattice.Tests.EditMode
{
    public sealed class FlightFoldAssetTests
    {
        [TestCase("Taren")][TestCase("Sela")]
        public void ShippedHullRetainsTheGeometryRequiredForFolding(string hero)
        {
            var hull=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Characters/"+hero+"Flight.prefab");
            var filters=hull.GetComponentsInChildren<MeshFilter>();Assert.IsNotEmpty(filters);
            foreach(var filter in filters)
                Assert.IsTrue(filter.sharedMesh.isReadable,hero+" player discards CPU hull data; editor-only vertex access hides this defect");
        }
    }
}
