using UnityEditor;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>Reuse generated meshes/textures; no primitive final surfaces.</summary>
    public static class StationSurfaces
    {
        public static void SoftenFloors()=>BatchTools.Run(()=>
        {
            foreach(string area in new[]{"public","arrival","quiet","work","service","rooms"})
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/station-floor-"+area+".mat");
                if(material==null)throw new System.InvalidOperationException("Missing station finish "+area);
                FloorDetail(material,area);
            }
            AssetDatabase.SaveAssets();Debug.Log("STATION_FLOOR_FINISH_OK");
        });
        public static void FloorFinish(GameObject root,string area)
        {
            Color color=area switch
            {
                "public"=>new Color(.49f,.60f,.63f),
                "arrival"=>new Color(.47f,.51f,.55f),
                "quiet"=>new Color(.54f,.49f,.42f),
                "work"=>new Color(.44f,.48f,.49f),
                "service"=>new Color(.29f,.36f,.39f),
                _=>new Color(.38f,.45f,.52f)
            };
            Tint(root,"station-floor-"+area,color);
            FloorDetail(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/station-floor-"+area+".mat"),area);
            // The generated walking panels contain genuine grille openings.
            // A recessed, generated pressure plate closes the deck beneath them.
            var bounds=ModelGeometry.BoundsOf(root);
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/DeckWall.prefab");
            if(source==null)throw new System.InvalidOperationException("Missing generated pressure plate source");
            var backing=new GameObject("Sealed deck backing");
            var plateBody=(GameObject)PrefabUtility.InstantiatePrefab(source);
            plateBody.transform.SetParent(backing.transform,false);
            plateBody.transform.localRotation=Quaternion.Euler(90,0,0);
            Fit(backing,new Vector3(bounds.size.x,.035f,bounds.size.z));
            backing.transform.position+=new Vector3(bounds.center.x,bounds.min.y-.025f,bounds.center.z)-ModelGeometry.BoundsOf(backing).center;
            foreach(var collider in backing.GetComponentsInChildren<Collider>())Object.DestroyImmediate(collider);
            Tint(backing,"station-pressure-plate",new Color(.28f,.33f,.36f));
            var plate=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/station-pressure-plate.mat");
            plate.SetFloat("_BaseMapStrength",.18f);plate.SetColor("_EmissionColor",Color.black);EditorUtility.SetDirty(plate);
            backing.isStatic=true;backing.transform.SetParent(root.transform,true);
        }
        static void FloorDetail(Material material,string area)
        {
            // Preserve the generated panel texture and geometry while reducing
            // its repeated dark sockets/glow beneath people and furniture.
            material.SetFloat("_BaseMapStrength",area=="work"?.45f:area=="service"?.38f:.28f);
            material.SetColor("_EmissionColor",new Color(.006f,.018f,.024f));
            EditorUtility.SetDirty(material);
        }
        static void Tint(GameObject root,string name,Color color)
        {
            string path="Assets/_Project/Resources/WorldMaterials/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if(material==null)
                {
                    material=new Material(renderer.sharedMaterial);
                    AssetDatabase.CreateAsset(material,path);
                }
                material.SetColor("_BaseColor",color);EditorUtility.SetDirty(material);
                renderer.sharedMaterial=material;
            }
        }
        public static void ClosedHatch(GameObject frame,float yaw)
        {
            DoorwayCollision(frame);
            var panel=WorldBuilder.Piece("DeckWall",frame.transform.position+Quaternion.Euler(0,yaw,0)*new Vector3(0,-.1f,.06f),new Vector3(2.2f,2.7f,.16f));
            panel.name="Closed private pressure hatch";panel.transform.rotation=Quaternion.Euler(0,yaw,0);
            Tint(panel,"station-closed-hatch",new Color(.24f,.31f,.37f));
        }
        public static void DoorwayCollision(GameObject frame)
        {
            // Generated frames have empty openings; a single box would seal
            // them, while no collision let the partner walk through a jamb.
            var inverse=Quaternion.Inverse(frame.transform.rotation);
            var points=ModelGeometry.Points(frame);
            var bounds=new Bounds(inverse*(points[0]-frame.transform.position),Vector3.zero);
            foreach(var point in points)bounds.Encapsulate(inverse*(point-frame.transform.position));
            var root=new GameObject("Pressure frame collision");root.isStatic=true;
            root.transform.SetPositionAndRotation(frame.transform.position,frame.transform.rotation);
            float post=bounds.size.x*.14f;
            foreach(int side in new[]{-1,1})
            {
                var jamb=new GameObject("Door jamb",typeof(BoxCollider));jamb.isStatic=true;jamb.transform.SetParent(root.transform,false);
                jamb.transform.localPosition=bounds.center+Vector3.right*side*(bounds.size.x-post)*.5f;
                jamb.GetComponent<BoxCollider>().size=new Vector3(post,bounds.size.y,Mathf.Max(.5f,bounds.size.z));
            }
            var lintel=new GameObject("Door lintel",typeof(BoxCollider));lintel.isStatic=true;lintel.transform.SetParent(root.transform,false);
            lintel.transform.localPosition=bounds.center+Vector3.up*(bounds.size.y*.5f-.15f);
            lintel.GetComponent<BoxCollider>().size=new Vector3(bounds.size.x,.3f,Mathf.Max(.5f,bounds.size.z));
        }
        public static void Fit(GameObject root,Vector3 size)
        {
            var bounds=ModelGeometry.BoundsOf(root);
            root.transform.localScale=Vector3.Scale(root.transform.localScale,new Vector3(size.x/bounds.size.x,size.y/bounds.size.y,size.z/bounds.size.z));
        }
    }
}
