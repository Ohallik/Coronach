using System;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using UnityEditor;
using UnityEngine;

namespace Lattice.EditorTools
{
    public static class CantorCollarArt
    {
        const string Folder="Assets/_Project/Art/Generated/Models/CantorCollarPlate/";
        const string Source="Assets/_Project/Prefabs/Environment/CantorCollarPlate.prefab";
        const string Equipment="Assets/_Project/Prefabs/Environment/CantorCollarEquipment.prefab";
        static readonly Vector3 PlateScale=new Vector3(.85f,.3f,.45f);
        public static void Prepare()=>BatchTools.Run(()=>
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Source);
            if(prefab==null)throw new InvalidOperationException("Run isolated generated intake first");
            var source=UnityEngine.Object.Instantiate(prefab);
            GameObject equipment=null;
            try
            {
                var filters=source.GetComponentsInChildren<MeshFilter>();
                if(filters.Length!=1)throw new InvalidOperationException("Expected one inspected collar mesh");
                var filter=filters[0];var imported=filter.sharedMesh;
                // Imported scenery is GPU-only. This small shared derivative
                // retains CPU geometry for actual equipment-envelope checks.
                var mesh=new Mesh{vertices=imported.vertices,normals=imported.normals,uv=imported.uv,uv2=imported.uv2,
                    colors=imported.colors,triangles=imported.triangles};
                if(mesh.vertexCount<100||mesh.triangles.Length/3>1500||mesh.uv.Length!=mesh.vertexCount)
                    throw new InvalidOperationException("Generated collar mesh/UV budget failed");
                var matrix=source.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                var normalMatrix=matrix.inverse.transpose;
                var vertices=mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
                var bounds=new Bounds(vertices[0],Vector3.zero);foreach(var vertex in vertices)bounds.Encapsulate(vertex);
                float fit=PlateScale.x/bounds.size.x;
                if(bounds.size.y*fit>PlateScale.y||bounds.size.z*fit>PlateScale.z)
                    throw new InvalidOperationException("Generated collar exceeds the original plate envelope");
                // Uniformly fit the generated shape, then encode its inverse
                // placement scale in the mesh. Runtime recovery retains the
                // original transform contract without stretching the artwork.
                mesh.vertices=vertices.Select(v=>
                {
                    v=(v-bounds.center)*fit;
                    return new Vector3(v.x/PlateScale.x,v.y/PlateScale.y,v.z/PlateScale.z);
                }).ToArray();
                mesh.normals=mesh.normals.Select(n=>Vector3.Scale(normalMatrix.MultiplyVector(n).normalized,PlateScale).normalized).ToArray();
                mesh.name="CantorCollarPlate_RuntimeMesh";mesh.RecalculateBounds();mesh.RecalculateTangents();
                string meshPath=Folder+mesh.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if(existing==null)AssetDatabase.CreateAsset(mesh,meshPath);
                else{EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}
                var material=filter.GetComponent<Renderer>().sharedMaterial;
                if(material==null||material.GetTexture("_BaseMap")==null||material.GetTexture("_EmissionMap")==null)
                    throw new InvalidOperationException("Generated collar material is incomplete");
                material.enableInstancing=true;EditorUtility.SetDirty(material);
                equipment=new GameObject("CantorCollarEquipment",typeof(MeshFilter),typeof(MeshRenderer));
                equipment.transform.localScale=PlateScale;
                equipment.GetComponent<MeshFilter>().sharedMesh=mesh;equipment.GetComponent<MeshRenderer>().sharedMaterial=material;
                var result=PrefabUtility.SaveAsPrefabAsset(equipment,Equipment);
                var definition=GameCatalog.Find<EnemyDef>("Cantor");definition.collarPlate=result;EditorUtility.SetDirty(definition);
                AssetDatabase.SaveAssets();
                Debug.Log($"COLLAR_ART_GEOMETRY vertices={mesh.vertexCount} triangles={mesh.triangles.Length/3} size={bounds.size*fit}");
            }
            finally{if(equipment!=null)UnityEngine.Object.DestroyImmediate(equipment);UnityEngine.Object.DestroyImmediate(source);}
            Debug.Log("COLLAR_ART_READY");
        });
    }
}
