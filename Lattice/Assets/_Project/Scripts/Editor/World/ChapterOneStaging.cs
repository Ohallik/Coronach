using System.Linq;
using Lattice.Core;
using Lattice.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Isolated staging for the November QA pass on chapter 1's zones and the way home (D203). Opens each scene, changes only
    /// what is named here and leaves its geometry, encounters and navigation alone. The redesign builders call the same
    /// pieces, so a rebuild keeps them: boss-door checkpoints and cradles, the warp back to the Halo once the Cantor is free,
    /// locked text that names what opens it, one name per place, and Pell aboard Tallow. Runs: <c>Apply</c>.
    /// </summary>
    public static class ChapterOneStaging
    {
        public static void Apply()=>BatchTools.Run(()=>
        {
            Stage("Hub_CinderHalo",Halo);Stage("Hub_Decks",Decks);Stage("Sorrel_Ridges",Sorrel);Stage("Hushwell",Hushwell);Stage("Gullet_Tunnel",Gullet);Stage("TallowApproach",Approach);Stage("TallowDrift",Drift);
            AssetDatabase.SaveAssets();Debug.Log("CHAPTER_ONE_STAGED_OK");
        });
        static void Stage(string zone,System.Action stage)
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity");stage();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("STAGED "+zone);
        }
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        public static void ClearSigns(string text){foreach(var sign in All<TextMeshPro>().Where(t=>t.text==text).ToArray())Object.DestroyImmediate(sign.gameObject);}
        static void ClearNamed(string name){foreach(var go in All<Transform>().Where(t=>t!=null&&t.name==name).Select(t=>t.gameObject).ToArray())if(go!=null)Object.DestroyImmediate(go);}
        static void Reprompt(string from,string to){foreach(var prompt in All<InteractionPrompt>())if(prompt.prompt==from)prompt.prompt=to;}
        /// <summary>A warp's name on its prompt, its object and its sign, where the builder's words have moved on since the scene was built.</summary>
        static void RenameWarp(WarpBeacon warp,string to)
        {
            foreach(var sign in All<TextMeshPro>().Where(t=>t.text==warp.prompt)){sign.text=to;sign.name="Sign_"+to;}
            warp.prompt=to;warp.name=to;
        }
        static void Spawn(string id,Vector3 at)
        {
            foreach(var old in All<SpawnPoint>().Where(s=>s.id==id).ToArray())Object.DestroyImmediate(old.gameObject);
            WorldBuilder.Spawn(id,at);Report(id,at);
        }
        /// <summary>Stated in the log so a hand-placed point in a built scene can be checked without a capture.</summary>
        static void Report(string what,Vector3 at)
        {
            Physics.SyncTransforms();
            bool solid=Physics.CheckSphere(at+Vector3.up*.2f,.6f,~0,QueryTriggerInteraction.Ignore);
            Debug.Log($"STAGED_POINT {what} {at} overlaps={solid}");
        }
        static float GroundAt(Vector3 at,float above=6)
        {
            Physics.SyncTransforms();
            foreach(var hit in Physics.RaycastAll(at+Vector3.up*above,Vector3.down,above+12,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
                if(hit.normal.y>.5f)return hit.point.y;
            return at.y;
        }
        static GameObject Place(string key,Vector3 at,float size,float yaw,bool solid)
        {
            float ground=GroundAt(at);
            var go=WorldBuilder.Piece(key,Vector3.zero,Vector3.one*size,"Rock",solid);go.transform.rotation=Quaternion.Euler(0,yaw,0);
            var bounds=ModelGeometry.BoundsOf(go);go.transform.position+=new Vector3(at.x,ground+.01f,at.z)-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            return go;
        }
        // ---- the November eye pass: shared bits of staging. Nothing in these is solid unless it says so: navigation was baked without them.
        const string WorldMaterials="Assets/_Project/Resources/WorldMaterials/",Models="Assets/_Project/Art/Generated/Models/";
        static Material Variant(string name,string source,System.Action<Material> tune)
        {
            var from=AssetDatabase.LoadAssetAtPath<Material>(source);if(from==null)throw new System.InvalidOperationException("Missing material "+source);
            string path=WorldMaterials+name+".mat";var made=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(made==null){made=new Material(from);AssetDatabase.CreateAsset(made,path);}
            made.CopyPropertiesFromMaterial(from);tune(made);EditorUtility.SetDirty(made);return made;
        }
        static void Wear(GameObject go,Material material)
        {
            foreach(var part in go.GetComponentsInChildren<Renderer>(true))
            {
                part.sharedMaterial=material;
                if(PrefabUtility.IsPartOfPrefabInstance(part))PrefabUtility.RecordPrefabInstancePropertyModifications(part);
            }
        }
        /// <summary>Soft dark paint: Lattice/SignPlate, whose alpha the mesh feathers.</summary>
        internal static Material Soft(string name,Color paint)
        {
            string path=WorldMaterials+name+".mat";var made=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(made==null){made=new Material(Shader.Find("Lattice/SignPlate"));AssetDatabase.CreateAsset(made,path);}
            made.SetColor("_BaseColor",paint);EditorUtility.SetDirty(made);return made;
        }
        /// <summary>A soft shape lying on the ground, solid two thirds of the way out and gone at its rim. A flat one for a hole, or one that follows the ground.</summary>
        internal static GameObject SoftPatch(string name,Transform parent,Vector3 centre,float rx,float rz,float yaw,Material material,bool follow)
        {
            const int Around=28;float[] rings={0,.64f,1};byte[] alpha={255,255,0};
            var rotation=Quaternion.Euler(0,yaw,0);var vertices=new Vector3[Around*3];var colors=new Color32[vertices.Length];var triangles=new int[Around*(3+6)*3];int t=0;
            for(int ring=0;ring<3;ring++)for(int i=0;i<Around;i++)
            {
                float a=i*Mathf.PI*2/Around;var p=centre+rotation*new Vector3(Mathf.Cos(a)*rx*rings[ring],0,Mathf.Sin(a)*rz*rings[ring]);
                if(follow)p.y=GroundAt(new Vector3(p.x,centre.y,p.z))+.04f;
                vertices[ring*Around+i]=p;colors[ring*Around+i]=new Color32(255,255,255,alpha[ring]);
                if(ring==2)continue;
                int here=ring*Around+i,next=ring*Around+(i+1)%Around;
                triangles[t++]=here;triangles[t++]=next;triangles[t++]=here+Around;
                triangles[t++]=next;triangles[t++]=next+Around;triangles[t++]=here+Around;
            }
            System.Array.Resize(ref triangles,t);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,true);go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            var mesh=new Mesh{name=name,vertices=vertices,colors32=colors,triangles=triangles};mesh.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var view=go.GetComponent<MeshRenderer>();view.sharedMaterial=material;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
            return go;
        }
        /// <summary>A soft line of light, uniform along its length: the flare's texture read down its middle.</summary>
        static Material LightLine=>Variant("staged-light-line","Assets/_Project/Resources/Effects/flare_01.mat",m=>
        {
            m.SetTextureScale("_BaseMap",new Vector2(0,1));m.SetTextureOffset("_BaseMap",new Vector2(.5f,0));m.SetColor("_BaseColor",new Color(1.7f,1.7f,1.7f,1));
        });
        static void Beam(string name,Transform parent,Vector3 from,Vector3 to,float width,Color bottom,Color top)
        {
            var line=new GameObject(name,typeof(LineRenderer)).GetComponent<LineRenderer>();line.transform.SetParent(parent,false);
            line.useWorldSpace=true;line.positionCount=2;line.SetPosition(0,from);line.SetPosition(1,to);line.startWidth=line.endWidth=width;
            line.startColor=bottom;line.endColor=top;line.sharedMaterial=LightLine;line.shadowCastingMode=ShadowCastingMode.Off;line.receiveShadows=false;
        }
        static void Glow(string name,Transform parent,Vector3 at,Color color,float range,float intensity)
        {
            var light=new GameObject(name,typeof(Light)).GetComponent<Light>();light.transform.SetParent(parent,true);light.transform.position=at;
            light.type=LightType.Point;light.color=color;light.range=range;light.intensity=intensity;light.shadows=LightShadows.None;
        }
        /// <summary>In a batch run a position set on a sign's RectTransform kept only its z unless something read it back, so a moved sign stayed where it was: its anchored position is set outright.</summary>
        static void MoveSign(string text,Vector3 to){foreach(var sign in All<TextMeshPro>().Where(t=>t.text==text)){sign.transform.position=to;sign.rectTransform.anchoredPosition=new Vector2(to.x,to.y);}}

        const string CradleSign="REPAIR CRADLE";
        static void Cradle(string name,Vector3 at,float yaw,string spawn)
        {
            ClearNamed(name);ClearNamed(name+" bay");ClearSigns(name.ToUpperInvariant());ClearSigns(CradleSign);
            float floor=GroundAt(at);
            var go=Place("DeckConsole",at,1.4f,yaw,true);go.name=name;
            var bay=go.AddComponent<RepairBay>();bay.free=true;bay.saveSpawn=spawn;bay.prompt=name+" — repair and save";bay.range=4;
            var obstacle=go.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.size=Vector3.one*1.6f;obstacle.center=Vector3.zero;obstacle.carving=true;
            WorldBuilder.Label(CradleSign,new Vector3(at.x,floor+.1f,at.z-2.6f),1.6f);
            CradleBay(name,go,at,floor);
            Report(name,at);
        }
        /// <summary>A console alone reads as a shut crate. A service pad under it, its screen lit, a pair of glowing bollards and a soft column of light say
        /// a free repair stands here from across the excavation. All of it is drawn only.</summary>
        internal static void CradleBay(string name,GameObject console,Vector3 at,float floor,bool glows=true,bool mirrored=false)
        {
            var bay=new GameObject(name+" bay").transform;
            // Thin, so a body walking through it has its feet on the pad: nothing solid here, and the site's navigation doesn't know it.
            var pad=WorldBuilder.Piece("DeckFloor",new Vector3(at.x,floor-.01f,at.z),new Vector3(3.6f,.06f,3.6f),"Ground",false);pad.name="Bay pad";StationSurfaces.FloorFinish(pad,"work");pad.transform.SetParent(bay,true);
            foreach(var (dx,dz) in new[]{(-2.5f,-1.1f),(2.2f,1.5f)})
            {var post=Place("DressLightBollard",at+new Vector3(mirrored?-dx:dx,0,dz),.9f,0,false);post.name="Bay bollard";post.transform.SetParent(bay,true);}
            var picto=SoftPatch("Bay mark",bay,new Vector3(at.x-.3f,floor+.035f,at.z-1.2f),.72f,.72f,0,AssetDatabase.LoadAssetAtPath<Material>(WorldMaterials+"dress-detail-PictoRepair.mat"),false);
            var mesh=picto.GetComponent<MeshFilter>().sharedMesh;var uv=new Vector2[mesh.vertexCount];var centre=new Vector3(at.x-.3f,floor+.035f,at.z-1.2f);
            for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(.5f+(mesh.vertices[i].x-centre.x)/1.44f,.5f+(mesh.vertices[i].z-centre.z)/1.44f);
            mesh.uv=uv;mesh.RecalculateNormals();picto.GetComponent<MeshRenderer>().receiveShadows=true;
            Wear(console,Variant("repair-cradle",Models+"DeckConsole/DeckConsole_Toon.mat",m=>m.SetColor("_EmissionColor",new Color(.2f,2.6f,2.4f))));
            var lit=new Color(.4f,1,.85f,.7f);var top=new Vector3(at.x,ModelGeometry.BoundsOf(console).max.y,at.z);
            Beam("Bay light",bay,top,top+Vector3.up*1.9f,.62f,lit,new Color(lit.r,lit.g,lit.b,0));
            if(glows)Glow("Bay glow",bay,top+Vector3.up*.5f,new Color(.4f,1,.85f),5,1.5f);
        }
        /// <summary>The first cradle anyone meets was a shut box beside a bench. The same bay, without a light of its own: Sorrel's terrain is one renderer, with all six of its lights spent.</summary>
        static void OutpostCradle()
        {
            var bay=All<RepairBay>().Single(b=>b.prompt.StartsWith("Outpost cradle"));
            ClearNamed("Outpost cradle bay");
            // Measured from the console's own foot: straight down from above, the ground here is the console's solid top.
            var at=new Vector3(bay.transform.position.x,0,bay.transform.position.z);
            CradleBay("Outpost cradle",bay.gameObject,at,ModelGeometry.BoundsOf(bay.gameObject).min.y,false);
        }

        /// <summary>'The repair cradle's free', Abri says, and there stood a box like the ones stacked round the deck as storage. The same bay as the drill's. Its deck is tiles, each with its own lights, so it can have its glow.</summary>
        static void RefugeCradle()
        {
            var bay=All<RepairBay>().Single();
            ClearNamed("Refuge cradle bay");
            CradleBay("Refuge cradle",bay.gameObject,new Vector3(bay.transform.position.x,0,bay.transform.position.z),ModelGeometry.BoundsOf(bay.gameObject).min.y);
        }

        public static void Halo()
        {
            var warps=All<WarpBeacon>();
            var sorrel=warps.Single(w=>w.scene=="Sorrel_Ridges");sorrel.lockedPrompt="Warp locked — see Orrin at the dock office first";
            ClearSigns("Sorrel approach");ClearSigns("Warp — Sorrel");sorrel.prompt="Warp — Sorrel";sorrel.name="Warp — Sorrel";
            WorldBuilder.Label("Warp — Sorrel",sorrel.transform.position+Vector3.up*4,1.5f);
            var gullet=warps.Single(w=>w.scene=="Gullet_Tunnel");gullet.lockedPrompt="Warp locked — recover Sorrel's survey key";
            OneWay(gullet);
            Reprompt("Dock — Dock Office","Dock — Arrivals / Dock Office");Reprompt("Dock — Outfitter","Dock — Market / Outfitter");Reprompt("Dock — Repair Bay","Dock — Receiving / Repair Bay");
            // The road back: the Gullet is one way only until the Cantor is free. Open quests were stranded for good (D203).
            ClearNamed("Warp — Tallow Approach");ClearSigns("Warp — Tallow Approach");
            var back=WorldBuilder.Warp("Warp — Tallow Approach",new Vector3(18,1,148),"TallowApproach","bossdown.Cantor","Halo");
            back.lockedPrompt="Warp locked — cross the Gullet first";
            Spawn("Tallow",new Vector3(12,1,140));Report("Warp — Tallow Approach",back.transform.position);
            AfterTheCantor("Neve","NeveAfterCantor");
            Tender("Repair tender",new Vector3(-9,1,145),"Outer");
            ClearNamed("Orrin's hail");
            var hail=new GameObject("Orrin's hail",typeof(RadioHail)).GetComponent<RadioHail>();hail.transform.position=gullet.transform.position;
            hail.speaker="Orrin";hail.situations=new[]{"hail.outer","hail.outer.after"};hail.needs=new[]{"warpkey","hushwell.nursery"};hail.heardFlag="orrin.outerHail";hail.silencedBy="bossdown.Cantor";
            // Their later lines wait for what they talk about. On the survey key alone, Neve sent the party by a chart that didn't exist yet to a warp that refused them, and Ilo asked about a Gullet nobody had seen.
            All<Npc>().Single(n=>n.speaker=="Neve").postFlag="hushwell.nursery";All<Npc>().Single(n=>n.speaker=="Ilo").postFlag="bossdown.Cantor";
            BridgeCasings();
        }
        /// <summary>The Gullet runs one way until the Cantor is free, and the autosave is written on arrival, so the warp says so before anyone
        /// takes it, in its prompt and under its sign. Neither names the Cantor: the sign is up from the first visit here, and nobody has heard
        /// of it until the Gullet. Both go quiet once the way home opens. At 3.1 the sign is 24 px under the flight camera, the type floor, and
        /// .46 m down its own plane it clears the warp's sign above it, which ArenaRuntime raises to 3.87 as the zone loads.</summary>
        static void OneWay(WarpBeacon warp)
        {
            const string Note="ONE WAY UNTIL YOU'RE THROUGH",Gate="One way sign gate";
            warp.wayBackFlag="bossdown.Cantor";warp.oneWayNote=" — one way until you're through";
            ClearSigns(Note);ClearNamed(Gate);
            var sign=WorldBuilder.Label(Note,warp.transform.position+new Vector3(0,3.77f,-.40f),3.1f);
            var gate=new GameObject(Gate,typeof(FlagGate)).GetComponent<FlagGate>();gate.flag="bossdown.Cantor";gate.showWhenSet=false;gate.targets=new[]{sign};
        }
        /// <summary>A flight zone's free repair: a console has nothing to stand on out here, so a repair pod moored below the flight plane, mouth to
        /// the camera, that a ship flies up to. Nothing solid: a hull in the lane costs integrity to touch, and every route's press at the warp stays the warp's.</summary>
        static void Tender(string name,Vector3 at,string spawn)
        {
            ClearNamed(name);ClearSigns(name.ToUpperInvariant());
            // A pod no bigger than the warp beside it, in a cooler working grey: 4 m tall and nearly 6 wide in its own cream, it read as a giant blown-out helmet with its sign on its hull.
            var go=WorldBuilder.Piece("DockingPodRepair",at-Vector3.up*3,Vector3.one*2.2f,"Rock",false);go.name=name;go.transform.rotation=Quaternion.Euler(0,180,0);
            Wear(go,Variant("docking-pod-tender",Models+"DockingPodRepair/DockingPodRepair_Toon.mat",m=>{m.SetColor("_BaseColor",new Color(.56f,.58f,.64f));m.SetColor("_EmissionColor",new Color(.1f,.72f,.75f));}));
            var bay=go.AddComponent<RepairBay>();bay.free=true;bay.saveSpawn=spawn;bay.prompt=name+" — repair and save";bay.range=WorldBuilder.FlightDockRange;
            WorldBuilder.Label(name.ToUpperInvariant(),at+Vector3.up*1.5f,3.1f);
            Report(name,at);
        }
        public static void Decks()
        {
            AfterTheCantor("Orrin","OrrinAfterCantor");
            BridgeCasings();
            // At its new size its corner met the door's lintel.
            MoveSign("ARRIVALS / DOCK OFFICE",new Vector3(-28,.12f,-.8f));
            // Into the room it names: on the promenade it lay along the bottom of the screen wherever the party stands at Hal's anvil, half under the button bar.
            MoveSign("RECEIVING / REPAIR",new Vector3(26.5f,.12f,2.6f));
            ServiceJob();
            // The first job's service opening lay a millimetre under the promenade's runner: a lid standing on an unbroken walkway, with no hole.
            foreach(var hole in All<Transform>().Where(t=>t.name=="Walkway service opening"))hole.position=new Vector3(hole.position.x,MarketOpeningStaging.OpeningHeight,hole.position.z);
        }
        /// <summary>The first job was two boards and a black plate tilted up in the walkway, with nothing to say what they were. Whoever's doing it has left their kit out: painted edges round the hole, a work
        /// lamp, a cable drum and a power cell along the bridge's walls. Drawn only and off Sela's and Mira's walks. It's there only from the morning the job's on: a save from before this scene never sees it. The painted edge goes with the hole. The kit stays on its spots, as if someone's still packing up, until the party has the survey key and has been away.</summary>
        static void ServiceJob()
        {
            const string Kit="Service job kit",Pieces="Service job pieces",Edges="Service job edges",Gone="Service job edges go",Comes="Service job kit comes",Packed="Service job kit packed";
            ClearNamed(Kit);ClearNamed("Service job kit goes");ClearNamed(Gone);ClearNamed(Comes);ClearNamed(Packed);
            var root=new GameObject(Kit);var kit=new GameObject(Pieces).transform;kit.SetParent(root.transform,false);var edges=new GameObject(Edges).transform;edges.SetParent(kit,false);
            var hazard=AssetDatabase.LoadAssetAtPath<Material>(WorldMaterials+"dress-detail-DecalHazard.mat");
            foreach(var (x,z,yaw) in new[]{(-13.6f,-3.82f,0f),(-13.6f,-2.18f,0f),(-14.42f,-3f,90f),(-12.78f,-3f,90f)})
            {
                var edge=GameObject.CreatePrimitive(PrimitiveType.Quad);edge.name="Opening edge";Object.DestroyImmediate(edge.GetComponent<Collider>());edge.transform.SetParent(edges,true);
                edge.transform.SetPositionAndRotation(new Vector3(x,MarketOpeningStaging.OpeningHeight+.012f,z),Quaternion.Euler(90,yaw,0));edge.transform.localScale=new Vector3(1.8f,.16f,1);
                edge.GetComponent<MeshRenderer>().sharedMaterial=hazard;
            }
            foreach(var (key,x,z,size,yaw) in new[]{("DressWorkLamp",-14.9f,-5.1f,1.2f,40f),("DressCableDrum",-15.3f,-.95f,.8f,0f),("DressPowerCell",-13.8f,-5.2f,.55f,90f)})
                Place(key,new Vector3(x,0,z),size,yaw,false).transform.SetParent(kit,true);
            var gone=new GameObject(Gone,typeof(FlagGate)).GetComponent<FlagGate>();gone.flag=OpeningMarketSequence.Repaired;gone.showWhenSet=false;gone.targets=new[]{edges.gameObject};
            var packed=new GameObject(Packed,typeof(FlagGate)).GetComponent<FlagGate>();packed.flag="warpkey";packed.showWhenSet=false;packed.targets=new[]{root};
            var comes=new GameObject(Comes,typeof(FlagGate)).GetComponent<FlagGate>();comes.flag=OpeningMarketSequence.Requested;comes.targets=new[]{kit.gameObject};
        }
        /// <summary>The four boxes under the Decks' bridges, in the interior and the exterior scene alike. Named for what they became, so a second pass finds none.</summary>
        static void BridgeCasings()
        {
            foreach(float x in new[]{-14f,14f})foreach(float z in new[]{-3f,21.5f})
                foreach(var block in All<Transform>().Where(t=>t.name=="DeckWall"&&t.parent==null&&(t.position-new Vector3(x,-1.5f,z)).sqrMagnitude<.01f).ToArray())StationRedesign.BridgeCasing(block.gameObject);
        }
        /// <summary>Back from Tallow on the warp, their old advice about getting there is stale.</summary>
        static void AfterTheCantor(string speaker,string node)
        {
            var person=All<Npc>().Single(n=>n.speaker==speaker);person.finalFlag="bossdown.Cantor";person.finalNode=node;
        }
        public static void Sorrel()
        {
            Reprompt("Link to the Lattice Anvil","Fabricate — Lattice Anvil");
            Reprompt("Outpost cradle — repair & save","Outpost cradle — repair and save");
            Reprompt("Launch — outer Halo beacon","Launch — the Halo's outer warp");
            var dace=All<Npc>().Single(n=>n.speaker=="Survivor");dace.finalCompletionFlag="sorrel.nurseryHeard";dace.finalRepeatNode="SurvivorPostNursery";
            All<EncounterVolume>().Single(e=>e.encounterId=="Sorrel_Burrower").checkpointSpawn="DrillDoor";
            Spawn("DrillDoor",new Vector3(1,GroundAt(new Vector3(1,0,148)),148));
            Cradle("Drill site cradle",new Vector3(-3,0,140.5f),60,"DrillDoor");
            Wreck();
            DrillSeals();KeyCradle();BoreMouth();OutpostCradle();RigWrecked();
            // Cliffs take the ground's texture from the side, as the rock's would be: a floor projection smeared them into streaks a third of a screen long.
            var ground=AssetDatabase.LoadAssetAtPath<Material>(WorldMaterials+"sorrel-ground.mat");
            ground.EnableKeyword("_TRIPLANAR_ON");ground.SetFloat("_Triplanar",1);ground.SetFloat("_TriplanarScale",.125f);EditorUtility.SetDirty(ground);
        }
        /// <summary>The site's rig stood square and clean beside a hole a boss had come out of. Burned and dead, canted down on its east legs on the footing, with what shook loose lying along the footing's edge.
        /// All drawn only: the rig's collider is the wrapper's and doesn't lean, and the site's navigation is as it was.</summary>
        static void RigWrecked()
        {
            const string Loose="Rig wreckage";ClearNamed(Loose);
            var rig=All<Transform>().First(t=>t.name=="OutpostDrill"&&t.parent==null);
            if(rig.GetComponent<PropTint>()==null)rig.gameObject.AddComponent<PropTint>().tint=new Color(.66f,.6f,.54f);
            if(rig.GetComponent<StaysDark>()==null)rig.gameObject.AddComponent<StaysDark>();
            // From where the body rests upright, so a second staging run doesn't lean it twice.
            var body=rig.GetChild(0);body.localRotation=Quaternion.identity;
            body.position+=rig.position-ModelGeometry.BoundsOf(body.gameObject).center;
            body.localRotation=Quaternion.Euler(1.5f,0,-4);body.position+=Vector3.down*.14f;
            var parts=new GameObject(Loose).transform;
            foreach(var (key,x,z,size,yaw) in new[]{("HushwellRubble",-2.2f,177.4f,.7f,40f),("DressPipeRun",-2.4f,181.6f,.8f,65f),("DressCableDrum",-2f,184.8f,.8f,200f),("HushwellDrillBit",-5.4f,188.2f,1f,110f)})
                Place(key,new Vector3(x,0,z),size,yaw,false).transform.SetParent(parts,true);
            SoftPatch("Rig scorch",parts,new Vector3(-1.9f,GroundAt(new Vector3(-1.9f,0,180.6f)),180.6f),1.6f,4.6f,4,Soft("wreck-scorch",new Color(.02f,.016f,.016f,.72f)),true);
        }
        /// <summary>The drill's seals were a stretched cube of the Gullet's membrane, a navy plank at a dusty site. Now a line of bollards along the cut that always
        /// stands, and low hazard lines between them that only show while the seal is shut. The collider and the carving obstacle are exactly as they were.</summary>
        static void DrillSeals()
        {
            ClearNamed("Drill seal posts");var posts=new GameObject("Drill seal posts").transform;
            float[] stops={-12.4f,-8,-3.6f,3.6f,8,12.4f};var amber=new Color(1,.68f,.22f,.9f);
            foreach(var seal in All<Membrane>().Where(m=>m.name=="Drill entry seal"||m.name=="Drill cradle seal"))
            {
                var box=seal.GetComponent<BoxCollider>();bool shut=box.enabled;box.enabled=false;
                var look=seal.GetComponent<MeshRenderer>();if(look!=null)Object.DestroyImmediate(look);
                var shape=seal.GetComponent<MeshFilter>();if(shape!=null)Object.DestroyImmediate(shape);
                foreach(var old in seal.GetComponentsInChildren<LineRenderer>(true))Object.DestroyImmediate(old.gameObject);
                float z=seal.transform.position.z;
                foreach(float x in stops){var post=Place("DressLightBollard",new Vector3(x,0,z),.95f,0,false);post.name="Seal post";post.transform.SetParent(posts,true);}
                for(int i=1;i<stops.Length;i++)foreach(float height in new[]{.34f,.76f})
                    Beam("Hazard line",seal.transform,new Vector3(stops[i-1],GroundAt(new Vector3(stops[i-1],0,z))+height,z),new Vector3(stops[i],GroundAt(new Vector3(stops[i],0,z))+height,z),.15f,amber,amber);
                box.enabled=shut;seal.SetOpen(!shut);
            }
        }
        /// <summary>The drill's cradle was a second Lattice Anvil that vanished whole with the key. It's a low core rack on the drill's service stand with the survey key
        /// plugged into it, lit, under a soft column of light. The key goes when it's taken; the machine stays. Only the rack is solid.</summary>
        static void KeyCradle()
        {
            var cradle=All<KeyPickup>().Single();cradle.name="Drill's cradle";
            var box=cradle.GetComponent<BoxCollider>();box.enabled=false;
            for(int i=cradle.transform.childCount-1;i>=0;i--)Object.DestroyImmediate(cradle.transform.GetChild(i).gameObject);
            var at=new Vector3(cradle.transform.position.x,0,cradle.transform.position.z);float floor=GroundAt(at);
            void Stand(string piece,float dx,float dz,float size,float yaw)=>Place(piece,at+new Vector3(dx,0,dz),size,yaw,false).transform.SetParent(cradle.transform,true);
            var rack=Place("DressCoreRack",at,.62f,0,false);rack.transform.SetParent(cradle.transform,true);
            Stand("DressPipeRun",0,1.05f,.8f,0);Stand("HushwellDrillBit",-2.5f,.5f,1,70);Stand("DressCableDrum",2.5f,.7f,1,-20);Stand("DressWorkLamp",-1.9f,1.5f,1.25f,160);
            float shelf=ModelGeometry.BoundsOf(rack).max.y;
            var key=Place("DressPowerCell",at,.46f,20,false);key.name="Survey key";key.transform.position+=Vector3.up*(shelf-floor-.03f);key.transform.SetParent(cradle.transform,true);
            Wear(key,Variant("survey-key",Models+"DressPowerCell/DressPowerCell_Toon.mat",m=>{m.SetColor("_BaseColor",new Color(.72f,.86f,.95f));m.SetColor("_EmissionColor",new Color(.25f,2.4f,2.8f));}));
            var lit=new Color(.45f,1,1,.75f);var crown=new Vector3(at.x,ModelGeometry.BoundsOf(key).max.y,at.z);
            Beam("Key light",key.transform,crown,crown+Vector3.up*1.8f,.55f,lit,new Color(lit.r,lit.g,lit.b,0));
            Glow("Key glow",key.transform,crown+Vector3.up*.4f,new Color(.4f,.95f,1),5,1.5f);
            cradle.visual=key;
            box.center=cradle.transform.InverseTransformPoint(new Vector3(at.x,floor+.55f,at.z));box.size=new Vector3(2.5f,1.1f,1.2f);box.enabled=true;
        }
        /// <summary>The way down looked like a table over a brown stain. A dark shaft under the scaffold, a rim of what the bore threw up, and the miners' lamp and cable beside it.
        /// The south side stays open: every route stands there.</summary>
        static void BoreMouth()
        {
            ClearNamed("Bore mouth");
            var at=All<WarpBeacon>().Single(w=>w.scene=="Hushwell").transform.position;float floor=GroundAt(new Vector3(at.x,0,at.z-4));
            var mouth=new GameObject("Bore mouth").transform;
            SoftPatch("Bore shaft",mouth,new Vector3(at.x,floor+.05f,at.z),2.9f,2.4f,0,Soft("bore-shaft",new Color(.012f,.01f,.014f,.97f)),false);
            foreach(var (key,angle,reach,size,yaw) in new[]{("HushwellRubble",-15f,3.2f,.6f,40f),("HushwellRubble",32f,3.3f,.72f,130f),("HushwellLowRidge",85f,3.1f,.52f,-8f),("HushwellRubble",130f,3.3f,.66f,220f),("HushwellRubble",178f,3.2f,.56f,300f)})
            {
                float a=angle*Mathf.Deg2Rad;
                var piece=Place(key,new Vector3(at.x+Mathf.Cos(a)*reach,0,at.z+Mathf.Sin(a)*reach),size,yaw,false);piece.name="Bore rim";piece.transform.SetParent(mouth,true);
            }
            foreach(var (key,x,z,size,yaw) in new[]{("DressCableDrum",at.x+4.7f,at.z+.1f,1f,-30f),("DressWorkLamp",at.x-3.6f,at.z+1.7f,1.25f,125f)})
            {var piece=Place(key,new Vector3(x,0,z),size,yaw,false);piece.transform.SetParent(mouth,true);}
        }
        /// <summary>The disabled Burrower as a later visit finds it: nose dug in on the excavation's west side, clear of the centre line and bore
        /// scaffold every route uses, and well away from the door and its cradle. Drawn only: the site's navigation was baked without it, so it has
        /// no collider to trap anyone. BossWreck decides when it shows.</summary>
        static void Wreck()
        {
            ClearNamed("Burrower wreck");ClearNamed("Burrower wreck site");
            var body=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/Burrower.prefab");if(body==null)return;
            // Far enough in from the west that the walk past it has it on screen: at -8.5 half of it was off the left edge, and all that showed was a ribbed cone.
            var at=new Vector3(-6.7f,0,165);float floor=GroundAt(at);
            const float Heading=140;
            var wreck=new GameObject("Burrower wreck");wreck.transform.position=new Vector3(at.x,floor,at.z);
            var rig=(GameObject)PrefabUtility.InstantiatePrefab(body);rig.name="Rig";rig.transform.SetParent(wreck.transform,false);
            // On its lower side, as it falls in the fight, with its dead maw toward the road: from there it's the boss you beat, where its tail alone read as a spare
            // drill bit. The nose is driven into the ground: it was still going when it stopped.
            rig.transform.rotation=Quaternion.Euler(0,Heading,0)*Quaternion.Euler(22,0,128);
            // Burned: from the road its clean grey ribs read as a heap of boulders.
            rig.AddComponent<PropTint>().tint=new Color(.58f,.5f,.46f);
            var lying=ModelGeometry.BoundsOf(rig);rig.transform.position+=new Vector3(at.x,floor-.9f,at.z)-new Vector3(lying.center.x,lying.min.y,lying.center.z);
            var parts=new GameObject("Thrown parts");parts.transform.SetParent(wreck.transform,false);
            // The furrow it ploughed, and the ground it threw up round its nose.
            SoftPatch("Wreck scar",parts.transform,new Vector3(at.x,floor,at.z),5.6f,2.4f,Heading-90,Soft("wreck-soot",new Color(.05f,.035f,.03f,.62f)),true);
            // The scorch it left where it burned out, across the scar and out toward the road. The spoil heap's neat oval of orange nuggets looked stamped: broken ground instead.
            SoftPatch("Wreck scorch",parts.transform,new Vector3(at.x+1.2f,floor,at.z-2.6f),4.2f,3.2f,Heading-40,Soft("wreck-scorch",new Color(.02f,.016f,.016f,.72f)),true);
            foreach(var (key,x,z,size,yaw) in new[]{("HushwellRubble",-4.4f,162f,1.3f,30f),("HushwellDrillBit",-11f,161.6f,1.1f,205f),("HushwellRubble",-7.2f,160.6f,1f,80f),("DressCableDrum",-4.4f,168.6f,.9f,130f)})
                Place(key,new Vector3(x,0,z),size,yaw,false).transform.SetParent(parts.transform,true);
            var site=new GameObject("Burrower wreck site",typeof(BossWreck)).GetComponent<BossWreck>();site.wreck=wreck;site.rig=rig.transform;site.thrownParts=parts;
            wreck.SetActive(false);
            Report("Burrower wreck",new Vector3(at.x,floor+.8f,at.z));
        }
        public static void Hushwell()
        {
            // The nursery is the reveal: the sign keeps the secret, the card and the locked lift point at it without naming it.
            foreach(var sign in All<TextMeshPro>().Where(t=>t.text=="NURSERY")){sign.text="LOWER CHAMBER";sign.name="Sign_LOWER CHAMBER";}
            // The first label in the cave lay under the climb scaffold, and the grid's on its own carved slab, grey on grey.
            MoveSign("DRILL BREACH",new Vector3(-2,.1f,9));MoveSign("SURVEY GRID",new Vector3(8,.1f,94.5f));
            // The breach's big work lights hung over nothing: flat orange ovals on the floor with no lamp near them. They hang over the camp's own lamps.
            var lamps=new[]{new Vector3(-7,3.2f,5.6f),new Vector3(5.2f,3.2f,11.6f)};int hung=0;
            foreach(var light in All<Light>().Where(l=>l.name=="Work light"))light.transform.position=lamps[hung++%lamps.Length];
            var rock=AssetDatabase.LoadAssetAtPath<Material>(WorldMaterials+"hushwell-rock.mat");
            rock.EnableKeyword("_TRIPLANAR_ON");rock.SetFloat("_Triplanar",1);rock.SetFloat("_TriplanarScale",.166f);EditorUtility.SetDirty(rock);
            All<WarpBeacon>().Single(w=>w.requiredFlag=="hushwell.nursery").lockedPrompt="The lift is dead. Look at what the Bellows was guarding first.";
            // A warp with no lock has no locked words: the bore's still held the retired 'Sorrel key'.
            foreach(var open in All<WarpBeacon>().Where(w=>string.IsNullOrEmpty(w.requiredFlag)))open.lockedPrompt="";
            All<EncounterVolume>().Single(e=>e.encounterId=="Hushwell_Bellows").checkpointSpawn="BellowsDoor";
            Spawn("BellowsDoor",HushwellLayout.OnFloor(new Vector2(0,237)));
            Cradle("Survey cradle",HushwellLayout.OnFloor(new Vector2(8,231)),-60,"BellowsDoor");
            foreach(var cache in All<SalvageField>().Where(c=>c.cacheId=="HushwellMiners")){cache.owner="The miners' cache";cache.dialogueNode="";}
            PoolShores();
        }
        /// <summary>An invisible kerb at a pool's water line: what keeps the party on the shore. No renderer and not static, so no camera, bake or dressing check reads it as scenery. RoomDressing's two checks of what stands where know it by this name.</summary>
        internal const string PoolEdge="Pool edge";
        /// <summary>The nursery's pools were dark patches that faded into the floor. A band of damp rock round each, with a thin pale lip at the water's edge, gives them a shore. The water itself is the
        /// licensed layer's, and its sheen stays Nathan's call. With a shore they read as water, and a partner standing out on one read as walking on it: each now has a kerb and a hole in the navigation.</summary>
        static void PoolShores()
        {
            const string Name="Pool shores",Edges="Pool edges";ClearNamed(Name);ClearNamed(Edges);var shores=new GameObject(Name).transform;Physics.SyncTransforms();
            var damp=Soft("pool-damp",new Color(.02f,.025f,.05f,.62f));var lip=Soft("pool-lip",new Color(.5f,.58f,.66f,.34f));
            foreach(var pool in BiomeLandscapeUpgrade.NurseryPools)
            {
                var centre=new Vector3(pool.x,HushwellLayout.Lower,pool.y);
                ShoreRing("Pool shore",shores,centre,pool.z,pool.w,.82f,1,1.34f,damp);
                ShoreRing("Pool lip",shores,centre,pool.z,pool.w,.84f,.93f,1.03f,lip);
            }
            // After the rings: each ring's rays look for a mesh collider, and these are too.
            var edges=new GameObject(Edges).transform;
            foreach(var pool in BiomeLandscapeUpgrade.NurseryPools)Kerb(edges,new Vector3(pool.x,HushwellLayout.Lower,pool.y),pool.z,pool.w);
        }
        static void Kerb(Transform parent,Vector3 centre,float rx,float rz)
        {
            const int Around=16;const float Under=-1,Over=1.4f;
            var points=new Vector3[Around*2];var faces=new int[Around*6+(Around-2)*6];int t=0;
            for(int i=0;i<Around;i++)
            {
                float a=i*Mathf.PI*2/Around;points[i]=new Vector3(Mathf.Cos(a)*rx,Under,Mathf.Sin(a)*rz);points[i+Around]=new Vector3(points[i].x,Over,points[i].z);
                int next=(i+1)%Around;
                faces[t++]=i;faces[t++]=i+Around;faces[t++]=next;faces[t++]=next;faces[t++]=i+Around;faces[t++]=next+Around;
            }
            for(int i=1;i<Around-1;i++){faces[t++]=0;faces[t++]=i+1;faces[t++]=i;faces[t++]=Around;faces[t++]=Around+i;faces[t++]=Around+i+1;}
            var go=new GameObject(PoolEdge,typeof(MeshCollider),typeof(NavMeshObstacle));go.transform.SetParent(parent,false);go.transform.position=centre;
            var mesh=new Mesh{name=PoolEdge,vertices=points,triangles=faces};mesh.RecalculateBounds();
            var solid=go.GetComponent<MeshCollider>();solid.sharedMesh=mesh;solid.convex=true;
            var hole=go.GetComponent<NavMeshObstacle>();hole.shape=NavMeshObstacleShape.Box;hole.center=Vector3.up*.2f;hole.size=new Vector3(rx*2,2.4f,rz*2);hole.carving=true;
        }
        /// <summary>A soft band lying on the ground round an ellipse: nothing at its inner and outer radii, full at the one between.</summary>
        static void ShoreRing(string name,Transform parent,Vector3 centre,float rx,float rz,float inner,float middle,float outer,Material material)
        {
            const int Around=40;float[] rings={inner,middle,outer};byte[] alpha={0,255,0};
            var vertices=new Vector3[Around*3];var colors=new Color32[vertices.Length];var triangles=new int[Around*12];int t=0;
            for(int ring=0;ring<3;ring++)for(int i=0;i<Around;i++)
            {
                float a=i*Mathf.PI*2/Around;var p=centre+new Vector3(Mathf.Cos(a)*rx*rings[ring],0,Mathf.Sin(a)*rz*rings[ring]);
                // The cave's own floor, under anything standing on it: a ring laid on whatever came first ran up the side of an egg cradle.
                foreach(var hit in Physics.RaycastAll(p+Vector3.up*3,Vector3.down,8,~0,QueryTriggerInteraction.Ignore))if(hit.collider is MeshCollider)p.y=hit.point.y;
                p.y+=.05f;vertices[ring*Around+i]=p;colors[ring*Around+i]=new Color32(255,255,255,alpha[ring]);
                if(ring==2)continue;
                int here=ring*Around+i,next=ring*Around+(i+1)%Around;
                triangles[t++]=here;triangles[t++]=next;triangles[t++]=here+Around;
                triangles[t++]=next;triangles[t++]=next+Around;triangles[t++]=here+Around;
            }
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,true);go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            var mesh=new Mesh{name=name,vertices=vertices,colors32=colors,triangles=triangles};mesh.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var view=go.GetComponent<MeshRenderer>();view.sharedMaterial=material;view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;
        }
        public static void Gullet()
        {
            var exit=All<WarpBeacon>().Single(w=>w.scene=="TallowApproach");exit.lockedPrompt="Warp locked — free the Cantor first";
            // It lands in the open space above Tallow, with the dock still ahead, as the Halo's and the Reach's warps do. One name for the one place.
            RenameWarp(exit,"Exit warp — Tallow Approach");
            All<EncounterVolume>().Single(e=>e.encounterId=="Gullet_Cantor").checkpointSpawn="Collar";
            Spawn("Collar",new Vector3(23,1,768));
            All<DiscoveryPoint>().Single(d=>d.flag=="gullet.collarObserved").saveSpawn="Collar";
            foreach(var cache in All<SalvageField>().Where(c=>c.cacheId=="Neve")){cache.owner="Neve's cache";cache.dialogueNode="CacheFound";cache.dialogueSpeaker="Neve";Salvaged(cache);}
            foreach(var loose in All<Transform>().Where(t=>t.name=="DeckCrate"&&t.parent==null).ToArray())Wear(loose.gameObject,Weathered("DeckCrate/DeckCrate_Toon.mat","gullet-crate"));
            GulletBuilder.Shell();
            // Off the floor at the foot of a wall it hung in the void. On the bed, in front of the pocket it names.
            MoveSign("SALVAGE EDDY",new Vector3(4,-1,397));
            // A level sign rests on the bed's high side, so one laid well off the tube's middle hovers. This one was laid at the far chamber's centre.
            MoveSign("SLALOM THROAT",new Vector3(1.4f,-1,325));
        }
        static Material Weathered(string source,string name)=>Variant(name,Models+source,m=>{m.SetColor("_BaseColor",new Color(.5f,.47f,.48f));m.SetColor("_EmissionColor",new Color(.03f,.12f,.14f));});
        /// <summary>A clean deck crate, pale as paper with a black triangle on its lid, was the brightest thing in a dim room. The cache is the miners' kind of crate stack. Those crates' faces are near
        /// black in their own paint, and dimmed for the eddy they went solid black, a hole in the room and bigger than a ship: so the paint is lifted, the stack is a size smaller, and a warm work light
        /// picks it out. Gullet's shell has no point lights to compete with. The trigger stays exactly as it was.</summary>
        static void Salvaged(SalvageField cache)
        {
            for(int i=cache.transform.childCount-1;i>=0;i--)Object.DestroyImmediate(cache.transform.GetChild(i).gameObject);
            var pile=WorldBuilder.Piece("HushwellCrates",cache.transform.position,Vector3.one*1.6f,"Rock",false);pile.transform.SetParent(cache.transform,true);
            Wear(pile,Variant("gullet-cache",Models+"HushwellCrates/HushwellCrates_Toon.mat",m=>{m.SetColor("_BaseColor",new Color(.66f,.6f,.55f));m.SetFloat("_BaseMapStrength",.78f);m.SetColor("_EmissionColor",new Color(.03f,.12f,.14f));}));
            Glow("Cache light",cache.transform,cache.transform.position+new Vector3(-1.2f,2.6f,-1.2f),new Color(1,.74f,.46f),8,3);
        }
        public static void Approach()
        {
            var wick=All<WarpBeacon>().Single(w=>w.scene=="WickReach");wick.lockedPrompt="No course plotted. Keeper Abri holds Tallow's old routes.";
            // Nobody has named the place until Abri does: the warp stands there unnamed, and its locked text says who holds the route.
            const string Named="Warp — The Wick Reach",Unnamed="Warp — No course plotted";
            wick.unnamedPrompt=Unnamed;
            ClearSigns(Named);ClearSigns(Unnamed);ClearNamed("Wick sign named");ClearNamed("Wick sign unnamed");
            var named=WorldBuilder.Label(Named,wick.transform.position+Vector3.up*4,1.5f);var unnamed=WorldBuilder.Label(Unnamed,wick.transform.position+Vector3.up*4,1.5f);
            var known=new GameObject("Wick sign named",typeof(FlagGate)).GetComponent<FlagGate>();known.flag="lantern.routeKnown";known.targets=new[]{named};
            var unknown=new GameObject("Wick sign unnamed",typeof(FlagGate)).GetComponent<FlagGate>();unknown.flag="lantern.routeKnown";unknown.showWhenSet=false;unknown.targets=new[]{unnamed};
            named.SetActive(false);
            ClearNamed("Warp — Cinder Halo");ClearSigns("Warp — Cinder Halo");
            var home=WorldBuilder.Warp("Warp — Cinder Halo",new Vector3(-22,1,-20),"Hub_CinderHalo","bossdown.Cantor","Tallow");
            home.lockedPrompt="Warp locked — free the collared Cantor in the Gullet first";
            Spawn("Halo",new Vector3(-15,1,-23));Report("Warp — Cinder Halo",home.transform.position);
            // Her job, in her words: reaching the line first used to skip Abri's introduction.
            Reprompt("Inspect the slack line","Inspect the port docking line");
            All<DiscoveryPoint>().Single(d=>d.flag=="tallow.lineObserved").requiredFlag="met.Keeper";
            DockStruts();
        }
        /// <summary>The apron stands on two lengths of wall in a wall's own pale paint, which stopped short on the hull as flat sticks with nothing to say they met it. Dark steel, and a soft shade on the hull at each foot.
        /// The hull has no collider, as ships pass over it, so one stands in while the shade is laid.</summary>
        static void DockStruts()
        {
            const string Feet="Dock strut feet";ClearNamed(Feet);var feet=new GameObject(Feet).transform;
            var survey=new System.Collections.Generic.List<MeshCollider>();
            foreach(var filter in GameObject.Find("TallowStationHull").GetComponentsInChildren<MeshFilter>())
            {
                var holder=new GameObject("Dock survey");holder.transform.SetParent(filter.transform,false);
                var probe=holder.AddComponent<MeshCollider>();probe.sharedMesh=filter.sharedMesh;survey.Add(probe);
            }
            Physics.SyncTransforms();
            var steel=Variant("dock-strut",Models+"DeckWall/DeckWall_Toon.mat",m=>{m.SetColor("_BaseColor",new Color(.3f,.33f,.38f));m.SetColor("_EmissionColor",Color.black);});
            var shade=Soft("dock-foot",new Color(.015f,.018f,.03f,.7f));
            foreach(int side in new[]{-1,1})
            {
                foreach(var strut in All<Transform>().Where(t=>t.name=="DeckWall"&&Mathf.Abs(t.position.x-side*2)<.01f&&Mathf.Abs(t.position.z-.5f)<.01f).ToArray())Wear(strut.gameObject,steel);
                var mesh=SoftPatch("Dock foot",feet,new Vector3(side*2,-3,-1.2f),1,1.8f,0,shade,false).GetComponent<MeshFilter>().sharedMesh;
                var lying=mesh.vertices;
                for(int i=0;i<lying.Length;i++)foreach(var hull in survey)if(hull.Raycast(new Ray(new Vector3(lying[i].x,10,lying[i].z),Vector3.down),out var hit,40))lying[i].y=hit.point.y+.06f;
                mesh.vertices=lying;mesh.RecalculateBounds();
            }
            foreach(var probe in survey)if(probe!=null)Object.DestroyImmediate(probe.gameObject);
            Physics.SyncTransforms();
        }
        public static void Drift()
        {
            Reprompt("Launch — Tallow approach","Launch — Tallow Approach");
            // Two metres from the outer hatch, its frame hid the hero from the chest down on every launch. From here the prompt still reaches the lobby's far end.
            var launch=All<DockingPad>().Single(d=>d.scene=="TallowApproach");launch.transform.position=new Vector3(0,launch.transform.position.y,-8);
            // In the aisle in front of a cabin door, beside the bed pictogram RoomDressing paints. Behind the Keeper it lay right where her name hangs from this camera, so neither could be read.
            MoveSign("SERVICE / REST CABINS",new Vector3(5.9f,.12f,7.4f));
            // Beside her, where her name and Pell's stay clear of it. On the far side of the room it named empty deck.
            MoveSign("REFUGE / KEEPER",new Vector3(-4.2f,.12f,3.4f));
            // At the sign style's size these words were wider than the lobby, and both ends ran into its low walls.
            foreach(var sign in All<TextMeshPro>().Where(t=>t.text=="ARRIVALS / PRESSURE LIFT"))sign.text="PRESSURE LIFT";
            // On the lift's own door, along the top of the lintel. Under the launch spot it only ever showed faded to a ghost, and on the runner the frame cut it to 'RESS' from the lobby while from Abri's stop it stacked over her prompt
            // as a heading. Up here nothing stands in front of it from either side, and SignStyle sees it rests on no floor anyone stands on.
            MoveSign("PRESSURE LIFT",new Vector3(0,3.3f,-7));
            ClearNamed("Pell");ClearNamed("Pell aboard");ClearNamed("Pell's lamp");ClearNamed("Pell's lamp burns");ClearNamed("Pell comes aboard");
            var pell=WorldBuilder.Npc("Pell",new Vector3(-5.5f,0,-1.5f));pell.transform.rotation=Quaternion.Euler(0,70,0);
            pell.firstNode="PellTallow";pell.repeatNode="PellTallow";pell.postNode="";
            var aboard=new GameObject("Pell aboard",typeof(FlagGate)).GetComponent<FlagGate>();aboard.flag="lanternwick.chartGiven";aboard.targets=new[]{pell.gameObject};pell.gameObject.SetActive(false);
            // The chewed lamp, lit, once she has it back.
            var lamp=Place("LampPost",new Vector3(-6.7f,0,-.4f),1.5f,0,false);lamp.name="Pell's lamp";
            var light=new GameObject("Lamp light",typeof(Light));light.transform.SetParent(lamp.transform,false);light.transform.position=lamp.transform.position+Vector3.up*1.4f;
            var l=light.GetComponent<Light>();l.type=LightType.Point;l.color=new Color(1,.78f,.45f);l.range=6;l.intensity=1.6f;l.shadows=LightShadows.None;
            // Parented to her: it can only show while she is aboard.
            lamp.transform.SetParent(pell.transform,true);
            var burns=new GameObject("Pell's lamp burns",typeof(FlagGate)).GetComponent<FlagGate>();burns.flag="pell.lampReturned";burns.targets=new[]{lamp};lamp.SetActive(false);
            // Reaching Tallow with the chart is when she has left Lanternwick: she is never in both places.
            var comes=new GameObject("Pell comes aboard",typeof(FlagAll)).GetComponent<FlagAll>();comes.flags=new[]{"lanternwick.chartGiven"};comes.result="pell.aboard";
            Report("Pell",pell.transform.position);
            RefugeCradle();
        }
    }
}
