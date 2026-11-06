using System.Linq;
using Lattice.Core;
using Lattice.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

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
        static void Cradle(string name,Vector3 at,float yaw,string spawn)
        {
            ClearNamed(name);ClearSigns(name.ToUpperInvariant());
            float floor=GroundAt(at);
            var go=Place("DeckConsole",at,1.4f,yaw,true);go.name=name;
            var bay=go.AddComponent<RepairBay>();bay.free=true;bay.saveSpawn=spawn;bay.prompt=name+" — repair and save";
            var obstacle=go.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.size=Vector3.one*1.6f;obstacle.center=Vector3.zero;obstacle.carving=true;
            WorldBuilder.Label(name.ToUpperInvariant(),new Vector3(at.x,floor+.1f,at.z-2.2f),1.6f);
            Report(name,at);
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
            var go=WorldBuilder.Piece("DockingPodRepair",at-Vector3.up*3,Vector3.one*4,"Rock",false);go.name=name;go.transform.rotation=Quaternion.Euler(0,180,0);
            var bay=go.AddComponent<RepairBay>();bay.free=true;bay.saveSpawn=spawn;bay.prompt=name+" — repair and save";bay.range=WorldBuilder.FlightDockRange;
            WorldBuilder.Label(name.ToUpperInvariant(),at+Vector3.up*1.5f,3.1f);
            Report(name,at);
        }
        public static void Decks()=>AfterTheCantor("Orrin","OrrinAfterCantor");
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
        }
        /// <summary>The disabled Burrower as a later visit finds it: nose dug in on the excavation's west side, clear of the centre line and bore
        /// scaffold every route uses, and well away from the door and its cradle. Drawn only: the site's navigation was baked without it, so it has
        /// no collider to trap anyone. BossWreck decides when it shows.</summary>
        static void Wreck()
        {
            ClearNamed("Burrower wreck");ClearNamed("Burrower wreck site");
            var body=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/Burrower.prefab");if(body==null)return;
            var at=new Vector3(-8.5f,0,165);float floor=GroundAt(at);
            var wreck=new GameObject("Burrower wreck");wreck.transform.position=new Vector3(at.x,floor,at.z);
            var rig=(GameObject)PrefabUtility.InstantiatePrefab(body);rig.name="Rig";rig.transform.SetParent(wreck.transform,false);
            // On its lower side, as it falls in the fight, but with the nose driven in: it was still going when it stopped.
            rig.transform.rotation=Quaternion.Euler(0,-60,0)*Quaternion.Euler(14,0,128);
            var lying=ModelGeometry.BoundsOf(rig);rig.transform.position+=new Vector3(at.x,floor-.3f,at.z)-new Vector3(lying.center.x,lying.min.y,lying.center.z);
            var parts=new GameObject("Thrown parts");parts.transform.SetParent(wreck.transform,false);
            foreach(var (key,x,z,size,yaw) in new[]{("DressSpoilHeap",-11.6f,167.2f,1.1f,30f),("HushwellDrillBit",-12.8f,161.6f,1.1f,205f),("HushwellRubble",-5.4f,162.9f,1f,80f)})
                Place(key,new Vector3(x,0,z),size,yaw,false).transform.SetParent(parts.transform,true);
            var site=new GameObject("Burrower wreck site",typeof(BossWreck)).GetComponent<BossWreck>();site.wreck=wreck;site.rig=rig.transform;site.thrownParts=parts;
            wreck.SetActive(false);
            Report("Burrower wreck",new Vector3(at.x,floor+.8f,at.z));
        }
        public static void Hushwell()
        {
            // The nursery is the reveal: the sign keeps the secret, the card and the locked lift point at it without naming it.
            foreach(var sign in All<TextMeshPro>().Where(t=>t.text=="NURSERY")){sign.text="LOWER CHAMBER";sign.name="Sign_LOWER CHAMBER";}
            All<WarpBeacon>().Single(w=>w.requiredFlag=="hushwell.nursery").lockedPrompt="The lift is dead. Look at what the Bellows was guarding first.";
            All<EncounterVolume>().Single(e=>e.encounterId=="Hushwell_Bellows").checkpointSpawn="BellowsDoor";
            Spawn("BellowsDoor",HushwellLayout.OnFloor(new Vector2(0,237)));
            Cradle("Survey cradle",HushwellLayout.OnFloor(new Vector2(8,231)),-60,"BellowsDoor");
            foreach(var cache in All<SalvageField>().Where(c=>c.cacheId=="HushwellMiners")){cache.owner="The miners' cache";cache.dialogueNode="";}
        }
        public static void Gullet()
        {
            All<WarpBeacon>().Single(w=>w.scene=="TallowApproach").lockedPrompt="Warp locked — free the Cantor first";
            All<EncounterVolume>().Single(e=>e.encounterId=="Gullet_Cantor").checkpointSpawn="Collar";
            Spawn("Collar",new Vector3(23,1,768));
            All<DiscoveryPoint>().Single(d=>d.flag=="gullet.collarObserved").saveSpawn="Collar";
            foreach(var cache in All<SalvageField>().Where(c=>c.cacheId=="Neve")){cache.owner="Neve's cache";cache.dialogueNode="CacheFound";cache.dialogueSpeaker="Neve";}
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
        }
        public static void Drift()
        {
            Reprompt("Launch — Tallow approach","Launch — Tallow Approach");
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
        }
    }
}
