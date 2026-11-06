using UnityEditor;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>What a skill or a boss throws, loaded at runtime from Resources: kit pieces with their colliders taken off. Runs: <c>Apply</c>,
    /// after GenIntake.BuildPrefix has staged the drone.</summary>
    public static class CombatBodies
    {
        public static void Apply()=>BatchTools.Run(()=>
        {
            Stage("Assets/_Project/Prefabs/Environment/SurveyDrone.prefab","SurveyDrone",1);
            // Sorrel's own clay boulders, cut down to a clump: the rig is throwing what it dug up.
            Stage("Assets/_Project/Prefabs/Environment/DressBoulders.prefab","SpoilClump",.42f);
            AssetDatabase.SaveAssets();Debug.Log("COMBAT_BODIES_OK");
        });
        static void Stage(string source,string name,float scale)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(source);if(prefab==null)throw new System.Exception("missing "+source);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            PrefabUtility.UnpackPrefabInstance(instance,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            foreach(var collider in instance.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
            foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true))renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
            instance.transform.position=Vector3.zero;instance.transform.localScale*=scale;
            PrefabUtility.SaveAsPrefabAsset(instance,"Assets/_Project/Resources/Effects/"+name+".prefab");Object.DestroyImmediate(instance);
            Debug.Log("COMBAT_BODY "+name);
        }
    }
}
