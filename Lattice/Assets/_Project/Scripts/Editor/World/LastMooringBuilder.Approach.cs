using System;
using System.Linq;
using Lattice.Core;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>Chapter 10's way in (docs/chapters/ch10-wiring.md, sections 1 and 2, "Many Hands"): chapter 9 ends aboard Many Hands at the fleet's chart (<c>home.chartRead</c>,
    /// <c>chapter9.complete</c>), and chapter 10 opens at the same table. Idra's one plan there (a point: she is in the party), then Orrin's roll at his board straight after (an after-talk
    /// on the plan's flag, which starts the main quest), Sela's survey offer at the table, and outside, the lanes off the hull's stern beside chapters 6 to 9's: to Sorrel's second sky
    /// once the plan is said, to Last Mooring once Mother of Doors is free (the convoy has moved up), and the spawns a ship back from either arrives at. Abri's and Drask's lines aboard
    /// are beats (<c>SideStories</c>, chapter 10's block). The first engineer's file.</summary>
    public static partial class LastMooringBuilder
    {
        // StageApproach (LastMooringBuilder.cs) is the entry point; this is its registration with the restage runner, so a rebuild of either scene stages the chapter again. After
        // chapter 9's, which stands the fleet's chart at the same table (the chapter's end) and the stern's spawns this one sits beside.
        [Restage.Step("chapter10.many-hands","ManyHands,ManyHandsHull",After="chapter9.approach",Chapter=10,Leaves=new[]{
            "ManyHands: End: Idra's one plan <DiscoveryPoint> #1","ManyHands: End: Orrin's roll <AfterTalk> #1","ManyHands: End: the nursery on the chart <DiscoveryPoint> #1",
            "ManyHandsHull: End: warp to Sorrel's Second Sky <WarpBeacon> #1","ManyHandsHull: Sign_Warp — Sorrel's Second Sky #1","ManyHandsHull: End: Sorrel's Second Sky's warp sign #1",
            "ManyHandsHull: End: the lane to Sorrel's Second Sky opens <FlagGate> #1",
            "ManyHandsHull: End: warp to Last Mooring <WarpBeacon> #1","ManyHandsHull: Sign_Warp — Last Mooring #1","ManyHandsHull: End: Last Mooring's warp sign #1",
            "ManyHandsHull: End: the lane to Last Mooring opens <FlagGate> #1",
            "ManyHandsHull: End: spawn from Sorrel <SpawnPoint> #1","ManyHandsHull: End: spawn from Last Mooring <SpawnPoint> #1"})]
        static void StageApproachStep()=>StageApproach();

        /// <summary>The spawns in Many Hands' hull a ship back from Sorrel and from Last Mooring arrives at (each zone's way home names its own).</summary>
        internal const string SorrelSpawn="Sorrel",MooringSpawn="Mooring";
        /// <summary>Where the two warps stand off the hull's stern, east of chapter 9's to Nacre (-60, -176) along the same row (chapters 6 to 8 are at -90, -120 and -150), and the
        /// spawns beside them, on the row chapter 9's spawns from Nacre (-52, -166) and the Ring (-34, -166) stand on.</summary>
        internal static readonly Vector3 SorrelWarp=new(-6,ZoneKit.FlightPlane,-176),MooringWarp=new(24,ZoneKit.FlightPlane,-176),
            HullFromSorrel=new(2,ZoneKit.FlightPlane,-166),HullFromMooring=new(32,ZoneKit.FlightPlane,-166);
        /// <summary>Many Hands' places (plan x, z): Idra's plan where every chapter's plan at the table is heard (the city's <c>ChartTalk</c>), the survey's offer at the table's west end
        /// (chapter 9's lesson and chart points at (-1.2, 45.2) and (1.4, 45.2) are done by then), and Orrin at his board under the gallery, where the arrivals stand him from the launch.</summary>
        internal static Vector2 PlanAt=>ManyHandsBuilder.Hands.ChartTalk;
        internal static readonly Vector2 SurveyAt=new(-2.8f,45.2f),RollAt=new(14.8f,36.1f);

        internal static class Approach
        {
            const string Mark="End: ";
            /// <summary>Each scene in turn: what this chapter stood there before is taken away by its name (and the warps' own signs by their words), then stood again, so a second run
            /// leaves exactly one of each.</summary>
            internal static void Stage(){City();Hull();}
            static void Clear()
            {
                foreach(var go in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t!=null&&t.name.StartsWith(Mark)).Select(t=>t.gameObject).ToArray())
                    if(go!=null)UnityEngine.Object.DestroyImmediate(go);
            }
            static Vector3 Floor(Vector2 p)
            {
                Physics.SyncTransforms();
                var hits=Physics.RaycastAll(new Vector3(p.x,60,p.y),Vector3.down,120,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance).ToArray();
                if(hits.Length==0)throw new InvalidOperationException("Chapter 10 staging: no floor under "+p+" in "+EditorSceneManager.GetActiveScene().name);
                return hits[0].point;
            }
            static DiscoveryPoint Point(string name,Vector3 at,string prompt,string flag,string report,string node,string speaker,string required,float range)
            {
                var go=new GameObject(Mark+name,typeof(DiscoveryPoint));go.transform.position=at;
                var p=go.GetComponent<DiscoveryPoint>();p.prompt=prompt;p.flag=flag;p.report=report??"";p.situation="";p.dialogueNode=node??"";p.dialogueSpeaker=speaker;
                p.requiredQuest="";p.requiredStep=0;p.requiredFlag=required??"";p.range=range;p.saveSpawn="";p.startsOnCrossing=false;return p;
            }
            /// <summary>A lane off the stern: drawn, and open, from the moment the story has a reason for it, and not before (as chapter 9's lane to Nacre: a warp kept shut and drawn
            /// left a refused press saying its refusal after it had opened). Its own sign carries no "End: " name, so it is cleared by its words and gated with the warp.</summary>
            static void Lane(string label,string name,Vector3 at,string zone,string opens)
            {
                ChapterOneStaging.ClearSigns(label);
                var warp=WorldBuilder.Warp(label,at,zone,"","Arrival");warp.lockedPrompt="";warp.name=Mark+"warp to "+name;
                var sign=WorldBuilder.Label(label,at+new Vector3(0,-.7f,-7),3.2f);sign.name=Mark+name+"'s warp sign";
                var own=UnityEngine.Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.text==label&&t.gameObject!=sign).Select(t=>t.gameObject).ToArray();
                var gate=new GameObject(Mark+"the lane to "+name+" opens",typeof(FlagGate)).GetComponent<FlagGate>();gate.flag=opens;gate.showWhenSet=true;
                gate.targets=new[]{warp.gameObject,sign}.Concat(own).ToArray();
                foreach(var shown in gate.targets)shown.SetActive(false);
            }

            // ---------------------------------------------------------------- Many Hands: the plan, the roll, the survey's offer
            static void City()
            {
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+LastMooringBuilder.City+".unity");Clear();
                // Idra's one plan at the chart table, from the chapter's last talk at it: what the told chapter's arrow points at (HouseLeaves.next, report OnePlan). A point and not an
                // Npc, because Idra is in the party; the point's own flag, which the talk doesn't set (no node is offered at a point whose own flag it sets).
                Point("Idra's one plan",Floor(PlanAt),"Hear Idra's plan","end.planHeard","OnePlan","IdraOnePlan","Idra","chapter9.complete",2.4f);
                // Orrin's roll straight after, at his board (manifest: "Orrin at his board, straight after. Each crew stands as it is pointed at"). An after-talk on the plan's flag, so it
                // can't be walked away from between the two, and a save made between them plays it as soon as Many Hands has settled. It starts the main quest and sets its own heard flag.
                // (Not raycast: the gallery is over the board, and an after-talk of range 0 plays wherever it stands.)
                var roll=new GameObject(Mark+"Orrin's roll",typeof(AfterTalk)).GetComponent<AfterTalk>();roll.transform.position=new Vector3(RollAt.x,0,RollAt.y);
                roll.flag="end.planHeard";roll.node="OrrinRoll";roll.speaker="Orrin";roll.heardFlag="end.planSaid";roll.delay=.6f;roll.range=0;roll.duringFight=false;
                // Sela's survey of the young's road, offered at the table once the plan is said (and in the epilogue at the travel terminal, which is the neighbourhood's: both points
                // carry this flag, so the offer is heard once). The talk starts LastSurvey; the point's flag is its own.
                Point("the nursery on the chart",Floor(SurveyAt),"Look at the nursery on the chart","sela.surveyOffered","","SelaLastSurveyOffer","Sela","end.planSaid",2.4f);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Debug.Log("MOORING_STAGED ManyHands plan="+PlanAt+" survey="+SurveyAt+" roll="+RollAt);
            }
            // ---------------------------------------------------------------- Many Hands' hull: the lanes off the stern, and the spawns ships come home to
            static void Hull()
            {
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+LastMooringBuilder.Hull+".unity");Clear();
                Lane("Warp — Sorrel's Second Sky","Sorrel's Second Sky",SorrelWarp,SorrelZone,"end.planSaid");
                // The convoy moves up to the mooring once she is free: from then the hull's own lane goes straight there, so a party that went home for its shops isn't sent back by Sorrel.
                Lane("Warp — Last Mooring","Last Mooring",MooringWarp,MooringZone,"end.motherFreed");
                WorldBuilder.Spawn(SorrelSpawn,HullFromSorrel).name=Mark+"spawn from Sorrel";
                WorldBuilder.Spawn(MooringSpawn,HullFromMooring).name=Mark+"spawn from Last Mooring";
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Debug.Log("MOORING_STAGED ManyHandsHull sorrel="+SorrelWarp+" mooring="+MooringWarp+" from-sorrel="+HullFromSorrel+" from-mooring="+HullFromMooring);
            }
        }
    }
}
