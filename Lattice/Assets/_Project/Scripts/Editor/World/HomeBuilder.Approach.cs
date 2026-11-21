using System;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>Chapter 9's beats in older scenes (docs/chapters/ch09-wiring.md, sections 1 and 2): Abri turning for Nacre as chapter 8 ends at Harrowgate's cells, the lane to Nacre off
    /// the Picket's north end and off Many Hands' stern, and in Many Hands the chapter's middle (Meret's notice, the pencil, Maru's answer, Oda aboard, Ket's bench) and its end (the chart).
    /// The first engineer's file.</summary>
    public static partial class HomeBuilder
    {
        // StageApproach (HomeBuilder.cs) is the entry point; this is its registration with the restage runner, so a rebuild of any of these four scenes stages the chapter again.
        [Restage.Step("chapter9.approach","Harrowgate,PicketOfTeeth,ManyHands,ManyHandsHull",After="chapter8.many-hands",Chapter=9,Leaves=new[]{
            "Harrowgate: Home: Abri turns for Nacre <AfterTalk> #1",
            "PicketOfTeeth: Home: warp to Nacre Grave <WarpBeacon> #1","PicketOfTeeth: Sign_Warp — Nacre Grave #1","PicketOfTeeth: Home: warp sign #1","PicketOfTeeth: Home: the lane to Nacre opens <FlagGate> #1",
            "ManyHands: Home: Abri turns for Nacre <AfterTalk> #1","ManyHands: Home: the three lamps found <FlagAll> #1","ManyHands: Home: Meret's notice <DiscoveryPoint> #1",
            "ManyHands: Home: Bryn's chart lesson <DiscoveryPoint> #1","ManyHands: Home: Maru names the Crawl <DiscoveryPoint> #1","ManyHands: Home: Oda's talk aboard <Npc> #1",
            "ManyHands: Home: Ket waits for his bench <Whereabouts> #1","ManyHands: Home: a bench for Ket <DiscoveryPoint> #1","ManyHands: Home: the fleet's chart <DiscoveryPoint> #1",
            "ManyHandsHull: Home: warp to Nacre Grave <WarpBeacon> #1","ManyHandsHull: Sign_Warp — Nacre Grave #1","ManyHandsHull: Home: warp sign #1","ManyHandsHull: Home: the lane to Nacre opens <FlagGate> #1",
            "ManyHandsHull: Home: spawn from Nacre <SpawnPoint> #1","ManyHandsHull: Home: spawn from the Ring <SpawnPoint> #1"})]
        static void StageApproachStep()=>StageApproach();

        /// <summary>The spawn in Many Hands' hull a ship back from the Broken Ring arrives at (the Ring's warp home names it).</summary>
        internal const string RingSpawn="Ring";
        /// <summary>Where the two warps to Nacre stand: the hull's off the stern, beside chapter 6's, 7's and 8's; the Picket's at its north end, beside where a ship off Harrowgate's quay
        /// arrives. And the spawns a ship back from Nacre and from the Ring arrives at, by the hull's.</summary>
        internal static readonly Vector3 HullWarp=new(-60,ZoneKit.FlightPlane,-176),HullFromNacre=new(-52,ZoneKit.FlightPlane,-166),HullFromRing=new(-34,ZoneKit.FlightPlane,-166),
            PicketWarp=new(40,ZoneKit.FlightPlane,776);
        /// <summary>Many Hands' places this chapter stands its beats at (plan x, z): the dock line Meret's notice is walked into, the chart table's two talks, Maru in the raft bay, Ket.</summary>
        internal static readonly Vector2 NoticeLine=new(0,-50.5f),LessonAt=new(-1.2f,45.2f),ChartReadAt=new(1.4f,45.2f),MaruTalk=new(-23.4f,-22.8f),KetTalk=new(40,10f);

        internal static class Approach
        {
            const string Mark="Home: ";
            /// <summary>Each scene in turn: what this chapter stood there before is taken away by its name (and the warps' own signs by their words), then stood again, so a second run
            /// leaves exactly one of each.</summary>
            internal static void Stage()
            {
                Harrowgate();Picket();City();Hull();
            }
            static void Clear()
            {
                foreach(var go in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t!=null&&t.name.StartsWith(Mark)).Select(t=>t.gameObject).ToArray())
                    if(go!=null)UnityEngine.Object.DestroyImmediate(go);
            }
            static Vector3 Floor(Vector2 p)
            {
                Physics.SyncTransforms();
                var hits=Physics.RaycastAll(new Vector3(p.x,60,p.y),Vector3.down,120,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).ToArray();
                if(hits.Length==0)throw new InvalidOperationException("Home staging: no floor under "+p+" in "+EditorSceneManager.GetActiveScene().name);
                return hits[0].point;
            }
            static AfterTalk TurnForNacre(Vector3 at,float delay)
            {
                var talk=new GameObject(Mark+"Abri turns for Nacre",typeof(AfterTalk)).GetComponent<AfterTalk>();talk.transform.position=at;
                talk.flag="chapter8.complete";talk.node="AbriTurnsForNacre";talk.speaker="Keeper";talk.heardFlag="home.turnedForNacre";talk.delay=delay;talk.range=0;talk.duringFight=false;
                return talk;
            }
            static DiscoveryPoint Point(string name,Vector3 at,string prompt,string flag,string report,string node,string speaker,string required,float range)
            {
                var go=new GameObject(Mark+name,typeof(DiscoveryPoint));go.transform.position=at;
                var p=go.GetComponent<DiscoveryPoint>();p.prompt=prompt;p.flag=flag;p.report=report??"";p.situation="";p.dialogueNode=node??"";p.dialogueSpeaker=speaker;
                p.requiredQuest="";p.requiredStep=0;p.requiredFlag=required??"";p.range=range;p.saveSpawn="";p.startsOnCrossing=false;return p;
            }
            /// <summary>The warp to Nacre: drawn, and open, from the moment Abri has turned the convoy for it, and not before (a lane that doesn't exist before Harrowgate answers isn't
            /// drawn), as chapter 5's dock off Tallow is. It was shown from chapter 8's end and locked until her word, and a refused press left the hidden warp saying "No lane to Nacre
            /// yet" after it had opened (the shared lock test, November 13); a party that left Harrowgate in the ten seconds before she spoke now finds no warp at the Picket, and the
            /// told chapter's arrow (HarrowgateBuilder's <c>q.next</c>) takes it aboard, where she turns the convoy.</summary>
            static void WarpToNacre(Vector3 at)
            {
                ChapterOneStaging.ClearSigns("Warp — Nacre Grave"); // the warp's own sign carries no "Home: " name: cleared by its words, so a second run adds none
                var warp=WorldBuilder.Warp("Warp — Nacre Grave",at,NacreZone,"","Arrival");warp.lockedPrompt="";warp.name=Mark+"warp to Nacre Grave";
                var sign=WorldBuilder.Label("Warp — Nacre Grave",at+new Vector3(0,-.7f,-7),3.2f);sign.name=Mark+"warp sign";
                var own=UnityEngine.Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.text=="Warp — Nacre Grave"&&t.gameObject!=sign).Select(t=>t.gameObject).ToArray();
                var gate=new GameObject(Mark+"the lane to Nacre opens",typeof(FlagGate)).GetComponent<FlagGate>();gate.flag="home.turnedForNacre";gate.showWhenSet=true;
                gate.targets=new[]{warp.gameObject,sign}.Concat(own).ToArray();
                foreach(var shown in gate.targets)shown.SetActive(false);
            }

            // ---------------------------------------------------------------- Harrowgate: Abri turns the convoy, at once, as the radio stretch at the cells ends
            static void Harrowgate()
            {
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+Harbour+".unity");Clear();
                // After Meret's all-stations and the chapter's closing card (nine seconds held), never over them.
                var at=UnityEngine.Object.FindObjectsByType<AfterTalk>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.node=="MeretAllStations");
                TurnForNacre(at!=null?at.transform.position:Vector3.zero,10);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("HOME_STAGED Harrowgate abri-turns="+(at!=null?at.transform.position.ToString():"origin"));
            }
            // ---------------------------------------------------------------- the Picket: the lane to Nacre off its north end
            static void Picket()
            {
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+HomeBuilder.Picket+".unity");Clear();
                WarpToNacre(PicketWarp);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("HOME_STAGED PicketOfTeeth warp="+PicketWarp);
            }
            // ---------------------------------------------------------------- Many Hands
            static void City()
            {
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+HomeBuilder.City+".unity");Clear();
                // For a save that left Harrowgate before Abri's word: it plays aboard instead, once (the same heard flag).
                TurnForNacre(Floor(ManyHandsBuilder.Hands.ChartTalk),2);
                var lamps=new GameObject(Mark+"the three lamps found",typeof(FlagAll)).GetComponent<FlagAll>();lamps.flags=new[]{"home.odaFound","home.tugFound","home.navigatorsFound"};lamps.result="home.nacreDone";
                // Meret's notice to all stations, walked into stepping up off the dock once all three lamps are in: Abri is standing there.
                var notice=Point("Meret's notice",Floor(NoticeLine),"Meret's notice to all stations","home.noticeHeard","","MeretBerthNotice","Meret","home.nacreDone",2);notice.startsOnCrossing=true;
                var mouth=notice.gameObject.AddComponent<BoxCollider>();mouth.isTrigger=true;mouth.size=new Vector3(16,5,3);mouth.center=Vector3.up*2.5f;
                // Bryn and her navigators at the chart table (Many Hands' arrivals stand them there from the tug's finding): the pencil.
                Point("Bryn's chart lesson",Floor(LessonAt),"Ask Bryn about her chart","home.chartLent","","BrynChartLesson","Bryn","home.noticeHeard",2.4f);
                // Idra and Maru in the raft bay: a point and not an Npc, because Idra is in the party. What the main quest's step 4 points at (report TownMovers).
                Point("Maru names the Crawl",Floor(MaruTalk),"Ask Maru who has moved a town","home.crawlNamed","TownMovers","IdraTownMovers","Idra","home.chartLent",2.4f);
                // Oda in the lane by her cabin: the body Many Hands' arrivals stand there from her finding until the Halo's last day, given her words.
                var oda=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="Oda, aboard");
                if(oda==null)throw new InvalidOperationException("Home staging: Many Hands' arrivals have no 'Oda, aboard' (ManyHandsBuilder.Hands.Arrivals.OdaCabin): rebuild Many Hands first");
                var talk=new GameObject(Mark+"Oda's talk aboard",typeof(Npc));talk.transform.SetParent(oda,false);
                var npc=talk.GetComponent<Npc>();npc.speaker="Oda";npc.firstNode=npc.repeatNode="OdaAboard";npc.postNode="";npc.postFlag="";npc.finalFlag=npc.finalNode=null;npc.prompt="Talk to Oda";npc.range=3.2f;
                WorldBuilder.Label(DisplayNames.Speaker("Oda"),oda.position+Vector3.up*2.8f,1.5f).transform.SetParent(talk.transform,true);
                // Ket in the arcade where his bench will stand, waiting for it from the day the party is home until Hal's apprentices are all lit (then Many Hands' own Ket stands there).
                var ketHolder=new GameObject(Mark+"Ket waits for his bench",typeof(Whereabouts));var where=ketHolder.GetComponent<Whereabouts>();
                var ket=new GameObject("Ket, Hal's apprentice");ket.transform.SetParent(ketHolder.transform,false);ket.transform.SetPositionAndRotation(Floor(ManyHandsBuilder.Hands.Arrivals.KetAt),Quaternion.Euler(0,180,0));
                var body=ZoneKit.Prefab("Characters","Hal");
                if(body!=null){var b=(GameObject)PrefabUtility.InstantiatePrefab(body);b.transform.SetParent(ket.transform,false);b.name="Generated civilian";b.transform.localScale=Vector3.one*.92f;
                    foreach(var rig in b.GetComponentsInChildren<Animator>())rig.cullingMode=AnimatorCullingMode.CullCompletely;}
                var solid=ket.AddComponent<BoxCollider>();solid.size=new Vector3(.7f,1.9f,.7f);solid.center=Vector3.up*.95f;
                where.from="home.returned";where.goneAt="quest.PortableBenches.complete";where.awayFrom=where.backAt="";where.live=false;where.targets=new[]{ket};
                var bench=Point("a bench for Ket",Floor(KetTalk),"Hand over the bench","bench.ket","PortableBench","","Sela","",2.4f);bench.requiredQuest="PortableBenches";bench.requiredStep=0;bench.situation="bench.ket";
                // The chapter's last talk: the fleet's chart, at the table, once the Cutter is towed off. What the main quest's last step points at (report FleetChart).
                Point("the fleet's chart",Floor(ChartReadAt),"See where the roads run","home.chartRead","FleetChart","OrrinChalk","Sela","home.cutterDisabled",2.4f);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Debug.Log("HOME_STAGED ManyHands notice="+NoticeLine+" lesson="+LessonAt+" maru="+MaruTalk+" oda="+oda.position+" ket="+KetTalk+" chart="+ChartReadAt);
            }
            // ---------------------------------------------------------------- Many Hands' hull: the lane to Nacre off the stern, and the spawns ships come home to
            static void Hull()
            {
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+HomeBuilder.Hull+".unity");Clear();
                WarpToNacre(HullWarp);
                WorldBuilder.Spawn(NacreSpawn,HullFromNacre).name=Mark+"spawn from Nacre";
                WorldBuilder.Spawn(RingSpawn,HullFromRing).name=Mark+"spawn from the Ring";
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("HOME_STAGED ManyHandsHull warp="+HullWarp+" from-nacre="+HullFromNacre+" from-ring="+HullFromRing);
            }
        }
    }
}
