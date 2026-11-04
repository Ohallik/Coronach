using System.Linq;
using Lattice.Core;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>
    /// The first town's morning on the Decks (D195). Isolated: replaces only its own root and the walk components it adds
    /// to Sela and Mira; never rebuilds the station or its dressing. The bridge between Orrin's office and the market is
    /// closed by two low gates at its market end with a service cover open in its deck; Mira's mobile stall straddles the
    /// safety line just beyond. Refuses to save if either civilian's authored walk is blocked by the actual scene.
    /// </summary>
    public static class MarketOpeningStaging
    {
        const string RootName="Market walkway service job";
        const float GateX=-12.55f,SouthWall=-5.75f,NorthWall=-.25f;
        public static readonly Vector3 RepairPoint=new Vector3(-15,0,-3);
        public static readonly Vector3[] SelaApproach={new Vector3(-4.5f,0,-3.6f),new Vector3(-9,0,-4.2f),new Vector3(-11.3f,0,-4.4f)};
        public static readonly Vector3[] SelaExit={new Vector3(-11.3f,0,-4.4f),new Vector3(-12.3f,0,-3.4f),new Vector3(-16,0,-3.2f),new Vector3(-19.5f,0,-2.4f),new Vector3(-23,0,-1)};
        public static readonly Vector3 MiraAtStall=new Vector3(-9.4f,0,-3.5f);

        public static void Refresh()=>BatchTools.Run(()=>
        {
            foreach(string kit in new[]{"DeckWall","DeckFloor","DressMarketStall"})
                if(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/"+kit+".prefab")==null)
                    throw new System.InvalidOperationException("Missing generated market kit "+kit);
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/Hub_Decks.unity");
            var npcs=Object.FindObjectsByType<Npc>(FindObjectsSortMode.None);
            var orrin=npcs.Single(n=>n.speaker=="Orrin");var sela=npcs.Single(n=>n.speaker=="Sela");var mira=npcs.Single(n=>n.speaker=="Mira");
            var prior=GameObject.Find(RootName);if(prior!=null)Object.DestroyImmediate(prior);
            var root=new GameObject(RootName);
            var sequence=root.AddComponent<OpeningMarketSequence>();
            sequence.orrin=orrin;sequence.sela=sela;sequence.mira=mira;

            // The service cover: an opening in the bridge deck with its lid hinged up on the office side.
            var opening=Piece(root,"DeckWall","Walkway service opening",new Vector3(-13.6f,.008f,-3),new Vector3(1.5f,.014f,1.5f),false);
            foreach(var r in opening.GetComponentsInChildren<Renderer>())r.sharedMaterial=Flat("market-service-opening",new Color(.05f,.06f,.08f));
            var guard=new GameObject("Service opening guard",typeof(BoxCollider));guard.transform.SetParent(root.transform,false);
            guard.transform.position=new Vector3(-13.6f,.5f,-3);guard.GetComponent<BoxCollider>().size=new Vector3(1.6f,1,1.6f);
            var hinge=new GameObject("Service cover hinge");hinge.transform.SetParent(root.transform,false);hinge.transform.position=new Vector3(-14.4f,.03f,-3);
            var lid=Piece(root,"DeckFloor","Walkway service cover",new Vector3(-13.6f,.03f,-3),new Vector3(1.6f,.05f,1.6f),false);
            lid.transform.SetParent(hinge.transform,true);
            sequence.cover=hinge.transform;sequence.openCoverEuler=new Vector3(0,0,70);sequence.openingGuard=guard.GetComponent<Collider>();

            // Two low safety gates close the bridge's market end and fold back along its side walls once the job is done.
            var gates=new Transform[2];
            foreach(var (index,wall,sign) in new[]{(0,SouthWall,1f),(1,NorthWall,-1f)})
            {
                var post=new GameObject(index==0?"South safety gate":"North safety gate");post.transform.SetParent(root.transform,false);
                post.transform.position=new Vector3(GateX,0,wall);
                var panel=Piece(root,"DeckWall","Safety gate panel",new Vector3(GateX,.55f,wall+sign*1.375f),new Vector3(.14f,1.1f,2.7f));
                // Construction amber, so the closed walkway reads at a glance against the white bridge walls.
                foreach(var r in panel.GetComponentsInChildren<Renderer>())r.sharedMaterial=Flat("market-safety-gate",new Color(.95f,.66f,.12f));
                panel.transform.SetParent(post.transform,true);gates[index]=post.transform;
            }
            sequence.gates=gates;sequence.gateOpenYaw=new[]{-90f,90f};

            // The safety line, and Mira's mobile counter across it; it settles back behind the line afterwards.
            var line=GameObject.CreatePrimitive(PrimitiveType.Quad);line.name="Walkway safety line";Object.DestroyImmediate(line.GetComponent<Collider>());
            line.transform.SetParent(root.transform,false);// Above the deck runners and studs (which sit up to 2 cm proud), so the line shows under the cart as well.
            line.transform.position=new Vector3(-10.6f,.035f,-3);line.transform.rotation=Quaternion.Euler(90,0,0);line.transform.localScale=new Vector3(.24f,5.2f,1);
            line.GetComponent<Renderer>().sharedMaterial=SafetyLine();
            var stall=new GameObject("Mira's mobile produce stall");stall.transform.SetParent(root.transform,false);stall.transform.position=new Vector3(-10.6f,0,-1.5f);
            var body=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Environment/DressMarketStall.prefab"));
            body.transform.SetParent(stall.transform,false);body.transform.localRotation=Quaternion.Euler(0,90,0);
            // A mobile cart on open floor stays under the fixed camera's 1.4 m sightline rule (D119).
            var b=ModelGeometry.BoundsOf(body);float scale=1.35f/Mathf.Max(.01f,b.size.y);body.transform.localScale*=scale;b=ModelGeometry.BoundsOf(body);
            body.transform.position-=new Vector3(b.center.x-stall.transform.position.x,b.min.y,b.center.z-stall.transform.position.z);
            foreach(var c in body.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);
            b=ModelGeometry.BoundsOf(body);var box=stall.AddComponent<BoxCollider>();box.center=stall.transform.InverseTransformPoint(b.center);box.size=b.size*.9f;
            sequence.stall=stall.transform;sequence.stallBack=new Vector3(.9f,0,0);

            // The job itself, on the office side of the open cover.
            var point=new GameObject("Finish the walkway repair",typeof(DiscoveryPoint));point.transform.SetParent(root.transform,false);point.transform.position=RepairPoint;
            var repair=point.GetComponent<DiscoveryPoint>();repair.flag=OpeningMarketSequence.Repaired;repair.dialogueNode="SelaWalkwayRepair";repair.dialogueSpeaker="Sela";
            repair.prompt="Finish the walkway repair";repair.range=1.8f;repair.report="";repair.situation="";sequence.repair=repair;

            // Sela comes across from the market as Taren reaches the job, and walks on to wait for Orrin afterwards;
            // Mira returns to her sales bay once the walkway is open.
            foreach(var old in sela.GetComponents<ScriptedWalk>().Concat(mira.GetComponents<ScriptedWalk>()))Object.DestroyImmediate(old);
            var selaWalk=sela.gameObject.AddComponent<ScriptedWalk>();selaWalk.path=SelaApproach;selaWalk.exitPath=SelaExit;
            selaWalk.beginNear=point.transform;selaWalk.beginDistance=11;selaWalk.speed=1.1f;selaWalk.skipIfJoined="Sela";selaWalk.walkThroughDialogue=true;
            var miraHome=mira.transform.position;
            var miraWalk=mira.gameObject.AddComponent<ScriptedWalk>();miraWalk.path=new[]{MiraAtStall};
            miraWalk.exitPath=new[]{MiraAtStall,new Vector3(-6,0,-2.6f),new Vector3(-2,0,.6f),new Vector3(miraHome.x,0,miraHome.z)};miraWalk.speed=.9f;
            sequence.selaWalk=selaWalk;
            if(Vector3.Distance(new Vector3(sela.transform.position.x,0,sela.transform.position.z),SelaExit[SelaExit.Length-1])>.3f)
                throw new System.InvalidOperationException("Sela's exit must end where the scene places her: "+sela.transform.position);

            // The authored walks must be clear in the actual scene (gates open, stall settled, cover closed).
            Physics.SyncTransforms();
            foreach(var gate in gates)gate.gameObject.SetActive(false);guard.SetActive(false);
            stall.transform.position+=sequence.stallBack;Physics.SyncTransforms();
            Check("Sela's approach",SelaApproach,sela.transform,stall.transform);Check("Sela's exit",SelaExit,sela.transform,null);Check("Mira's return",miraWalk.exitPath,mira.transform,null);
            stall.transform.position-=sequence.stallBack;foreach(var gate in gates)gate.gameObject.SetActive(true);guard.SetActive(true);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log($"MARKET_OPENING_STAGING_OK stall={b.size} mira-home={miraHome}");
        });

        static void Check(string label,Vector3[] route,Transform self,Transform alsoIgnore)
        {
            for(int i=1;i<route.Length;i++)
            {
                var from=route[i-1]+Vector3.up*.95f;var delta=route[i]-route[i-1];
                foreach(var hit in Physics.SphereCastAll(from,.4f,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                {
                    var t=hit.collider.transform;
                    if(t.IsChildOf(self)||alsoIgnore!=null&&t.IsChildOf(alsoIgnore))continue;
                    if(hit.collider.GetComponentInParent<Lattice.Combat.CombatActor>()!=null)continue;
                    throw new System.InvalidOperationException($"{label} is blocked by {t.name} between {route[i-1]} and {route[i]}");
                }
            }
        }
        static GameObject Piece(GameObject root,string key,string name,Vector3 position,Vector3 size,bool solid=true)
        {
            var result=WorldBuilder.Piece(key,position,size,"Rock",solid);result.name=name;result.transform.SetParent(root.transform,true);
            foreach(var t in result.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=false;return result;
        }
        static Material Flat(string name,Color color)
        {
            string path="Assets/_Project/Resources/WorldMaterials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Lattice/Toon"));AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",color);material.SetColor("_EmissionColor",Color.black);EditorUtility.SetDirty(material);return material;
        }
        static Material SafetyLine()
        {
            const string path="Assets/_Project/Resources/WorldMaterials/market-safety-line.mat";
            var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Resources/WorldMaterials/dress-detail-DecalHazard.mat")??throw new System.InvalidOperationException("Hazard paint material missing");
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(source);AssetDatabase.CreateAsset(material,path);}
            material.CopyPropertiesFromMaterial(source);material.SetTextureScale("_BaseMap",new Vector2(1,5.2f/.9f));material.renderQueue=source.renderQueue+10;EditorUtility.SetDirty(material);return material;
        }
    }
}
