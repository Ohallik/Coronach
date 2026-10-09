using System.Linq;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lattice.EditorTools
{
    // Isolated story blockout. Never regenerate the refuge or its garden.
    public static class TallowMooringStaging
    {
        const string RootName="Port mooring service arm";
        static readonly Vector3 MarkerPosition=new Vector3(-34,1,5);
        static readonly Vector3 FittingPosition=new Vector3(-29,-2.4f,3);
        public static void RefreshFitting()=>BatchTools.Run(()=>
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/TallowApproach.unity");
            GameObject.Find("Free docking-line fitting").transform.position=FittingPosition;
            var cable=GameObject.Find("Slack docking line").GetComponent<LineRenderer>();
            var points=CablePoints();cable.positionCount=points.Length;cable.SetPositions(points);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("TALLOW_FITTING_PLACEMENT_OK");
        });
        public static void RefreshMarker()=>BatchTools.Run(()=>
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/TallowApproach.unity");
            Object.FindObjectsByType<TallowMarkerDrift>(FindObjectsSortMode.None).Single().marker.position=MarkerPosition;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("TALLOW_MARKER_PLACEMENT_OK");
        });
        public static void Refresh()=>BatchTools.Run(()=>
        {
            var exterior=EditorSceneManager.OpenScene("Assets/_Project/Scenes/TallowApproach.unity");
            ConfigureApproach();EditorSceneManager.MarkSceneDirty(exterior);EditorSceneManager.SaveScene(exterior);
            var interior=EditorSceneManager.OpenScene("Assets/_Project/Scenes/TallowDrift.unity");
            ConfigureKeeper(Object.FindObjectsByType<Npc>(FindObjectsSortMode.None).Single(n=>n.speaker=="Keeper"));
            EditorSceneManager.MarkSceneDirty(interior);EditorSceneManager.SaveScene(interior);AssetDatabase.SaveAssets();
            Debug.Log("TALLOW_MOORING_STAGING_OK");
        });
        public static void ConfigureKeeper(Npc keeper)
        {
            keeper.finalFlag="tallow.lineObserved";keeper.finalNode="KeeperDriftReturn";
            keeper.finalCompletionFlag="tallow.driftDiscussed";keeper.finalRepeatNode="KeeperDriftRepeat";
        }
        public static void ConfigureApproach()
        {
            foreach(string key in new[]{"DeckWall","DeckFloor","CoilMooring","DeckCrate","WarpBeacon"})
                if(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/"+key+".prefab")==null)
                    throw new System.InvalidOperationException("Missing generated mooring kit: "+key);
            var old=GameObject.Find(RootName);if(old!=null)Object.DestroyImmediate(old);
            var root=new GameObject(RootName);
            foreach(float z in new[]{4.2f,5.8f})
                Piece(root,"DeckWall","Mooring cantilever",new Vector3(-18.5f,-3.95f,z),new Vector3(13,.6f,.65f));
            Piece(root,"DeckFloor","Mooring reel platform",new Vector3(-24,-3.5f,5),new Vector3(4,.5f,3));
            Piece(root,"CoilMooring","Port docking reel",new Vector3(-24,-2.6f,5),new Vector3(3.3f,1.4f,3.7f));
            var end=Piece(root,"DeckCrate","Free docking-line fitting",FittingPosition,Vector3.one*.6f,false);
            var cable=new GameObject("Slack docking line",typeof(LineRenderer)).GetComponent<LineRenderer>();
            cable.transform.SetParent(root.transform,false);cable.useWorldSpace=false;
            var points=CablePoints();
            cable.positionCount=points.Length;cable.SetPositions(points);cable.startWidth=cable.endWidth=.13f;cable.numCornerVertices=4;cable.numCapVertices=4;
            cable.sharedMaterial=CableMaterial();
            var point=new GameObject("Inspect the slack port line",typeof(DiscoveryPoint));point.transform.SetParent(root.transform,false);point.transform.position=new Vector3(-25,1,1);
            var discovery=point.GetComponent<DiscoveryPoint>();discovery.prompt="Inspect the slack line";discovery.range=4;
            discovery.flag="tallow.lineObserved";discovery.dialogueNode="SelaTallowLine";discovery.dialogueSpeaker="Sela";discovery.report="";discovery.situation="";
            var marker=Piece(root,"WarpBeacon","Detached anchorage marker",MarkerPosition,new Vector3(2,3,2),false);
            var drift=root.AddComponent<TallowMarkerDrift>();drift.marker=marker.transform;drift.observation=point.transform;
        }
        static Vector3[] CablePoints()
        {
            // One open, sagging bight. Smooth the authored path rather than drawing a knot.
            var knots=new[]{new Vector3(-24,-2.2f,5),new Vector3(-26,-2.8f,3.5f),new Vector3(-27,-3.8f,1),new Vector3(-30,-3.6f,-.5f),new Vector3(-31,-2.9f,1.5f),FittingPosition};
            const int samples=12;var points=new Vector3[(knots.Length-1)*samples+1];
            for(int segment=0;segment<knots.Length-1;segment++)for(int step=0;step<samples;step++)
            {
                float t=step/(float)samples;var a=knots[Mathf.Max(0,segment-1)];var b=knots[segment];
                var c=knots[segment+1];var d=knots[Mathf.Min(knots.Length-1,segment+2)];
                points[segment*samples+step]=.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
            }
            points[points.Length-1]=FittingPosition;return points;
        }
        static GameObject Piece(GameObject root,string key,string label,Vector3 position,Vector3 size,bool solid=true)
        {var piece=WorldBuilder.Piece(key,position,size,"Rock",solid);piece.name=label;piece.transform.SetParent(root.transform,true);return piece;}
        static Material CableMaterial()
        {
            const string path="Assets/_Project/Resources/WorldMaterials/tallow-docking-line.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Lattice/Toon"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",new Color(.64f,.43f,.18f));material.SetColor("_EmissionColor",Color.black);EditorUtility.SetDirty(material);return material;
        }
    }
}
