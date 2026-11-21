using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Lattice.EditorTools
{
    /// <summary>What the replay-route generators (tools/*_routes.py) need to know of the built zones and can't work out from a map: where each fight's volume really lies.
    /// A fight begins when the party is inside its box, and a route that stops short of it presses lock at nothing (tests: RouteFightTests). Writes
    /// docs/quality/encounters.json: for every zone in the build, each EncounterVolume's id, the middle of its box on the plan, its yaw, and its width and depth.</summary>
    public static class RouteFacts
    {
        public const string File="docs/quality/encounters.json";
        public static void Encounters()=>BatchTools.Run(()=>
        {
            var text=new StringBuilder("{\n");var zones=BatchTools.SceneNames().Where(n=>n!="_Boot"&&n!="Title").ToArray();int fights=0;bool firstZone=true;
            foreach(string zone in zones)
            {
                string path="Assets/_Project/Scenes/"+zone+".unity";if(!System.IO.File.Exists(path))continue;
                EditorSceneManager.OpenScene(path);
                var rows=new List<string>();
                foreach(var volume in Object.FindObjectsByType<EncounterVolume>(FindObjectsInactive.Include,FindObjectsSortMode.None).OrderBy(v=>v.encounterId,System.StringComparer.Ordinal))
                {
                    var box=volume.GetComponent<BoxCollider>();if(box==null||string.IsNullOrEmpty(volume.encounterId))continue;
                    var middle=box.transform.TransformPoint(box.center);var scale=box.transform.lossyScale;
                    rows.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,"    {{\"id\": \"{0}\", \"x\": {1:0.##}, \"z\": {2:0.##}, \"yaw\": {3:0.##}, \"width\": {4:0.##}, \"depth\": {5:0.##}, \"flag\": \"{6}\"}}",
                        volume.encounterId,middle.x,middle.z,box.transform.eulerAngles.y,box.size.x*Mathf.Abs(scale.x),box.size.z*Mathf.Abs(scale.z),volume.requiredFlag??""));
                }
                if(rows.Count==0)continue;
                text.Append(firstZone?"":",\n").Append("  \"").Append(zone).Append("\": [\n").Append(string.Join(",\n",rows)).Append("\n  ]");firstZone=false;fights+=rows.Count;
            }
            text.Append("\n}\n");
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            System.IO.File.WriteAllText(Path.Combine(root,File),text.ToString(),new UTF8Encoding(false));
            Debug.Log("ROUTE_FACTS_OK zones="+zones.Length+" fights="+fights);
        });
    }
}
