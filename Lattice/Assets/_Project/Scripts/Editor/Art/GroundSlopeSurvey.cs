using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Lattice.EditorTools
{
    /// <summary>How steep the ground a hero can walk on really is, zone by zone, so the uphill audit is run on the slopes the
    /// game has and not only on the 30 and 39 degree floors it was written for. Each ground zone's scene is opened (never
    /// saved), its baked navigation is sampled about every half metre, and the physical ground under each sample is read
    /// two ways: the slope of the face the feet's own ray finds there, and the rise over a metre along that face's fall
    /// line (what a whole step climbs). A zone with no baked navigation is left out and named.
    /// Run: exec-when-green.ps1 -Method Lattice.EditorTools.GroundSlopeSurvey.Run -Marker GROUND_SLOPE_SURVEY_OK
    /// Writes Builds/quality/C2/step-height/slope-survey.txt.</summary>
    public static class GroundSlopeSurvey
    {
        public static void Run()=>BatchTools.Run(()=>
        {
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/C2/step-height"));Directory.CreateDirectory(folder);
            var lines=new List<string>();
            var zones=AssetDatabase.FindAssets("t:ZoneDef").Select(g=>AssetDatabase.LoadAssetAtPath<ZoneDef>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(z=>z!=null&&(z.kind==ZoneKind.GroundSafe||z.kind==ZoneKind.GroundCombat)).OrderBy(z=>z.chapter).ThenBy(z=>z.id).ToArray();
            foreach(var zone in zones)
            {
                string path="Assets/_Project/Scenes/"+zone.scene+".unity";
                if(!File.Exists(path)){lines.Add($"SLOPE_ZONE {zone.id} chapter={zone.chapter} no scene");continue;}
                EditorSceneManager.OpenScene(path,OpenSceneMode.Single);Physics.SyncTransforms();
                var navigation=Object.FindObjectsByType<GroundNavigation>(FindObjectsSortMode.None).FirstOrDefault(n=>n.data!=null);
                if(navigation==null){lines.Add($"SLOPE_ZONE {zone.id} chapter={zone.chapter} no baked navigation");continue;}
                var instance=NavMesh.AddNavMeshData(navigation.data);
                try{lines.AddRange(Survey(zone));}
                finally{instance.Remove();}
            }
            File.WriteAllLines(Path.Combine(folder,"slope-survey.txt"),lines);
            foreach(var line in lines)Debug.Log(line);
            Debug.Log("GROUND_SLOPE_SURVEY_OK "+Path.Combine(folder,"slope-survey.txt"));
        });
        static IEnumerable<string> Survey(ZoneDef zone)
        {
            var mesh=NavMesh.CalculateTriangulation();var random=new System.Random(41);
            var faces=new List<(float slope,float weight)>();var steps=new List<(float slope,float weight,Vector3 at,string what)>();
            for(int t=0;t+2<mesh.indices.Length;t+=3)
            {
                Vector3 a=mesh.vertices[mesh.indices[t]],b=mesh.vertices[mesh.indices[t+1]],c=mesh.vertices[mesh.indices[t+2]];
                float area=Vector3.Cross(b-a,c-a).magnitude*.5f;if(area<.0001f)continue;
                int count=Mathf.Max(1,Mathf.CeilToInt(area/.25f));float weight=area/count;
                for(int i=0;i<count;i++)
                {
                    float u=(float)random.NextDouble(),v=(float)random.NextDouble();if(u+v>1){u=1-u;v=1-v;}
                    var point=a+(b-a)*u+(c-a)*v;
                    if(!Ground(point,out var hit))continue;
                    faces.Add((Vector3.Angle(hit.normal,Vector3.up),weight));
                    // The rise a whole step climbs: the ground half a metre either side along this face's fall line.
                    var fall=new Vector3(hit.normal.x,0,hit.normal.z);
                    if(fall.sqrMagnitude<1e-6f){steps.Add((0,weight,hit.point,hit.collider.name));continue;}
                    fall.Normalize();
                    if(Ground(hit.point+fall*.5f,out var low)&&Ground(hit.point-fall*.5f,out var high)&&Mathf.Abs(low.point.y-hit.point.y)<.8f&&Mathf.Abs(high.point.y-hit.point.y)<.8f)
                        steps.Add((Mathf.Atan2(high.point.y-low.point.y,1)*Mathf.Rad2Deg,weight,hit.point,hit.collider.name));
                }
            }
            if(faces.Count==0){yield return $"SLOPE_ZONE {zone.id} chapter={zone.chapter} navigation has no sampled ground";yield break;}
            string Spread(List<(float slope,float weight)> values)
            {
                var sorted=values.OrderBy(v=>v.slope).ToArray();float total=sorted.Sum(v=>v.weight);
                float At(float share){float sum=0;foreach(var v in sorted){sum+=v.weight;if(sum>=share*total)return v.slope;}return sorted[^1].slope;}
                float Over(float degrees)=>sorted.Where(v=>v.slope>degrees).Sum(v=>v.weight)/total*100;
                return System.FormattableString.Invariant($"p50={At(.5f):F1} p90={At(.9f):F1} p99={At(.99f):F1} max={sorted[^1].slope:F1} over15={Over(15):F1}% over20={Over(20):F1}% over25={Over(25):F1}% over30={Over(30):F1}% area={total:F0}m2");
            }
            yield return $"SLOPE_ZONE {zone.id} chapter={zone.chapter} samples={faces.Count} face[{Spread(faces)}] step[{Spread(steps.Select(s=>(s.slope,s.weight)).ToList())}]";
            // Where the steepest whole-step ground is, a metre apart at least, so each can be found in the scene.
            var shown=new List<Vector3>();
            foreach(var s in steps.OrderByDescending(s=>s.slope))
            {
                if(shown.Count>=6)break;if(shown.Any(p=>Vector3.Distance(p,s.at)<6))continue;shown.Add(s.at);
                yield return System.FormattableString.Invariant($"SLOPE_STEEP {zone.id} step={s.slope:F1} at=({s.at.x:F1},{s.at.y:F1},{s.at.z:F1}) on={s.what}");
            }
        }
        // The feet's own query: straight down from above, the first solid thing, walkable faces only (a capsule slides off past 45).
        static bool Ground(Vector3 point,out RaycastHit hit)=>Physics.Raycast(point+Vector3.up*1.2f,Vector3.down,out hit,2.4f,~0,QueryTriggerInteraction.Ignore)&&hit.normal.y>=Mathf.Cos(45*Mathf.Deg2Rad);
    }
}
