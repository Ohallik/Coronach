using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Lattice.EditorTools
{
    // Evaluate skinning in world space; renderer bounds and renderer scale are not measurements.
    public static class ModelGeometry
    {
        public static Vector3[] Points(GameObject root)
        {
            var points=new List<Vector3>();
            foreach(var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh=skin.sharedMesh;if(mesh==null)continue;
                var vertices=mesh.vertices;var weights=mesh.boneWeights;var poses=mesh.bindposes;var bones=skin.bones;
                if(weights.Length!=vertices.Length||bones.Length!=poses.Length)throw new InvalidOperationException("Invalid skin data "+skin.name);
                var matrices=bones.Select((b,i)=>b.localToWorldMatrix*poses[i]).ToArray();
                for(int i=0;i<vertices.Length;i++)
                {
                    var w=weights[i];var v=vertices[i];Vector3 p=Vector3.zero;
                    if(w.weight0>0)p+=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0;
                    if(w.weight1>0)p+=matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1;
                    if(w.weight2>0)p+=matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2;
                    if(w.weight3>0)p+=matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
                    points.Add(p);
                }
            }
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
                if(filter.sharedMesh!=null)points.AddRange(filter.sharedMesh.vertices.Select(v=>filter.transform.TransformPoint(v)));
            if(points.Count==0)throw new InvalidOperationException("No measurable geometry: "+root.name);
            return points.ToArray();
        }
        public static Bounds BoundsOf(GameObject root)
        {
            var points=Points(root);var bounds=new Bounds(points[0],Vector3.zero);
            foreach(var p in points)bounds.Encapsulate(p);return bounds;
        }
    }
}
