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
        public static void RefreshHardware()=>BatchTools.Run(()=>
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/TallowApproach.unity");
            ConfigureHardware(GameObject.Find(RootName));
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("TALLOW_MOORING_HARDWARE_OK");
        });
        static void ConfigureHardware(GameObject root)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/TallowMooringCoupler.prefab");
            if(prefab==null)throw new System.InvalidOperationException("Missing generated Tallow mooring coupler");
            var fitting=root.transform.Find("Free docking-line fitting");
            foreach(Transform child in fitting.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
            var body=(GameObject)PrefabUtility.InstantiatePrefab(prefab,fitting);body.transform.localRotation=Quaternion.Euler(-20,0,0);
            // The opened intake views identify the ferrule at the model's -X
            // end. Seat the actual rim at the existing cable endpoint, keeping
            // the uniform intake scale and the wrapper's stable scene identity.
            var points=ModelGeometry.Points(body);float end=points.Min(p=>p.x);
            var rim=points.Where(p=>p.x<end+.02f).ToArray();
            if(rim.Length<6)throw new System.InvalidOperationException("Coupler cable rim is not measurable");
            var socket=new Vector3(end,rim.Average(p=>p.y),rim.Average(p=>p.z));
            body.transform.position+=fitting.position-socket;
            var reel=root.transform.Find("Port docking reel");reel.position=new Vector3(-24,-2.6f,8);
            var platform=root.transform.Find("Mooring reel platform");platform.position=new Vector3(-24,-3.5f,8);
            platform.localScale=new Vector3(1,1,1.1f);
            var beams=root.transform.Cast<Transform>().Where(t=>t.name=="Mooring cantilever").ToArray();
            if(beams.Length!=2)throw new System.InvalidOperationException("Mooring platform needs both existing cantilevers");
            for(int i=0;i<beams.Length;i++)
            {
                // Tiled wall groups keep a zero wrapper origin. Translate the
                // evaluated geometry to the support position, not its wrapper.
                float actualZ=ModelGeometry.BoundsOf(beams[i].gameObject).center.z;
                beams[i].position+=Vector3.forward*(7.2f+1.6f*i-actualZ);
            }
            var line=root.transform.Find("Slack docking line").GetComponent<LineRenderer>();
            var cablePoints=CablePoints();line.positionCount=cablePoints.Length;line.SetPositions(cablePoints);
            var machinery=ModelGeometry.BoundsOf(reel.gameObject);var deck=ModelGeometry.BoundsOf(platform.gameObject);
            if(beams.Any(b=>!ModelGeometry.BoundsOf(b.gameObject).Intersects(deck)))throw new System.InvalidOperationException("A cantilever does not reach the platform");
            var attached=line.transform.TransformPoint(line.GetPosition(0));
            Debug.Log($"TALLOW_HARDWARE socketRimVertices={rim.Length} coupler={ModelGeometry.BoundsOf(body)} reel={machinery} platform={deck} cableStart={attached}");
            if(!machinery.Contains(attached))throw new System.InvalidOperationException("Docking line no longer meets the reel");
            ConfigureReadingView(root.GetComponent<TallowMarkerDrift>());
        }
        /// <summary>November 4: a braided rope instead of a flat colour ribbon, and the reel that winds the free line in
        /// once the party has seen it holds nothing. Isolated: changes only the line's look and adds the reel.</summary>
        public static void RefreshCable()=>BatchTools.Run(()=>
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/TallowApproach.unity");
            ConfigureCable(GameObject.Find(RootName));
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("TALLOW_CABLE_OK");
        });
        const string RopeTexture="Art/Generated/Dressing/TallowRope_base_color.png";
        // The generated rope tile spans four braid repeats at 1.593 times the rope's thickness.
        const float RopeTileAspect=1.593f;
        static void ConfigureCable(GameObject root)
        {
            PackStaging.StageFile("art-src/Generated/C8-Nov04/TallowRope_base_color.png",RopeTexture);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/_Project/"+RopeTexture);
            importer.wrapModeU=TextureWrapMode.Repeat;importer.wrapModeV=TextureWrapMode.Clamp;importer.maxTextureSize=512;importer.mipmapEnabled=true;importer.SaveAndReimport();
            var line=root.transform.Find("Slack docking line").GetComponent<LineRenderer>();
            line.textureMode=LineTextureMode.Tile;line.textureScale=Vector2.one;
            var material=CableMaterial();
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/"+RopeTexture));material.SetFloat("_BaseMapStrength",1);
            material.SetTextureScale("_BaseMap",new Vector2(1/(line.startWidth*RopeTileAspect),1));
            material.SetColor("_BaseColor",new Color(.9f,.86f,.82f));EditorUtility.SetDirty(material);
            line.sharedMaterial=material;
            var reel=root.GetComponent<TallowLineReel>()??root.AddComponent<TallowLineReel>();
            reel.line=line;reel.fitting=root.transform.Find("Free docking-line fitting");
            if(reel.fitting==null)throw new System.InvalidOperationException("Missing the free docking-line fitting");
            Debug.Log($"TALLOW_CABLE texture={material.GetTexture("_BaseMap")} repeat={1/(line.startWidth*RopeTileAspect):0.00}/m points={line.positionCount}");
        }
        public static void RefreshReadingView()=>BatchTools.Run(()=>
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/TallowApproach.unity");
            ConfigureReadingView(Object.FindFirstObjectByType<TallowMarkerDrift>());
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("TALLOW_READING_VIEW_STAGING_OK");
        });
        static void ConfigureReadingView(TallowMarkerDrift drift)
        {
            var view=drift.GetComponent<TallowLineView>()??drift.gameObject.AddComponent<TallowLineView>();view.drift=drift;
            view.subjects=new[]{drift.marker,drift.transform.Find("Free docking-line fitting"),drift.transform.Find("Port docking reel"),drift.transform.Find("Slack docking line")};
            if(view.subjects.Any(t=>t==null))throw new System.InvalidOperationException("Missing actual Tallow story subject");
        }
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
            ConfigureHardware(root);
            ConfigureReadingView(drift);
            ConfigureCable(root);
        }
        static Vector3[] CablePoints()
        {
            // One open, sagging bight. Smooth the authored path rather than drawing a knot.
            var knots=new[]{new Vector3(-24,-2.2f,6.5f),new Vector3(-29,-2.8f,5.5f),new Vector3(-31.5f,-3.8f,3.5f),new Vector3(-31,-3.6f,-.5f),new Vector3(-29.5f,-2.9f,0),FittingPosition};
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
            material.SetColor("_EmissionColor",Color.black);EditorUtility.SetDirty(material);return material;
        }
    }
}
