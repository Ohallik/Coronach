using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Lattice.Combat;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Asks a zone's own baked navigation for the walkable path between points, so replay routes follow ground a body can
    /// actually cross. Read-only: it opens scenes without saving them.
    /// <c>-qa-paths file</c> holds one request per line: <c>Scene x y z x y z [x y z ...]</c> (each point is reached from
    /// the one before). Writes the corners to <c>-qa-paths-out file</c> and to the log.
    /// </summary>
    public static class QaRoutePaths
    {
        public static void Print()=>BatchTools.Run(()=>
        {
            string input=Core.DevArgs.Value("-qa-paths"),output=Core.DevArgs.Value("-qa-paths-out");
            if(string.IsNullOrEmpty(input)||string.IsNullOrEmpty(output))throw new InvalidOperationException("QaRoutePaths needs -qa-paths and -qa-paths-out");
            var report=new StringBuilder();string open=null;NavMeshDataInstance instance=default;
            foreach(var raw in File.ReadAllLines(input))
            {
                var line=raw.Trim();if(line.Length==0||line.StartsWith("#"))continue;
                var parts=line.Split(' ',StringSplitOptions.RemoveEmptyEntries);string scene=parts[0];
                if(scene!=open)
                {
                    if(instance.valid)instance.Remove();
                    EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+scene+".unity");open=scene;
                    var navigation=UnityEngine.Object.FindFirstObjectByType<GroundNavigation>(FindObjectsInactive.Include);
                    if(navigation==null||navigation.data==null)throw new InvalidOperationException(scene+" has no baked navigation");
                    instance=NavMesh.AddNavMeshData(navigation.data);
                }
                var values=parts.Skip(1).Select(p=>float.Parse(p,CultureInfo.InvariantCulture)).ToArray();
                for(int i=3;i+2<values.Length;i+=3)
                {
                    var from=new Vector3(values[i-3],values[i-2],values[i-1]);var to=new Vector3(values[i],values[i+1],values[i+2]);
                    var path=new NavMeshPath();string result;
                    if(!NavMesh.SamplePosition(from,out var a,3,NavMesh.AllAreas))result="NO_START";
                    else if(!NavMesh.SamplePosition(to,out var b,3,NavMesh.AllAreas))result="NO_GOAL";
                    else if(!NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path))result="NO_PATH";
                    else result=path.status+" "+string.Join(" ",path.corners.Select(c=>FormattableString.Invariant($"{c.x:F1},{c.y:F1},{c.z:F1}")));
                    string entry=FormattableString.Invariant($"{scene} {from.x:F1},{from.y:F1},{from.z:F1} -> {to.x:F1},{to.y:F1},{to.z:F1}: {result}");
                    report.AppendLine(entry);Debug.Log("QA_PATH "+entry);
                }
            }
            if(instance.valid)instance.Remove();
            File.WriteAllText(output,report.ToString());
            Debug.Log("QA_PATHS_OK");
        });
    }
}
