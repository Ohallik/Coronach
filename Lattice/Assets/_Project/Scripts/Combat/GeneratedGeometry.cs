using UnityEngine;

namespace Lattice.Combat
{
    public static class GeneratedGeometry
    {
        // Renderer transform scale is not the skinned body's scale. Evaluate
        // bone matrices directly for imported rigs with scaled mesh siblings.
        public static Vector3[] WorldSkinPoints(SkinnedMeshRenderer skin)
        {
            var mesh=skin.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;
            var poses=mesh.bindposes;var bones=skin.bones;
            if(weights.Length!=vertices.Length||bones.Length!=poses.Length)throw new System.InvalidOperationException("Invalid generated skin "+skin.name);
            var matrices=new Matrix4x4[bones.Length];for(int i=0;i<matrices.Length;i++)matrices[i]=bones[i].localToWorldMatrix*poses[i];
            var points=new Vector3[vertices.Length];
            for(int i=0;i<vertices.Length;i++)
            {
                var w=weights[i];var v=vertices[i];Vector3 p=Vector3.zero;
                if(w.weight0>0)p+=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0;
                if(w.weight1>0)p+=matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1;
                if(w.weight2>0)p+=matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2;
                if(w.weight3>0)p+=matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;
                points[i]=p;
            }
            return points;
        }
    }
}
