using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Lattice.Core;
using Lattice.World;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lattice.EditorTools
{
    public static partial class TownLife
    {
        /// <summary>Read-only: what stands in each town as saved (every solid, every drawn piece, the named, the prompts, the floor signs, the doors and the spawns), written to
        /// Builds/logs/town-life/&lt;zone&gt;-survey.json so a resident's spot can be checked against the place before a run. Never saves a scene.
        /// Run: exec-when-green.ps1 -Method Lattice.EditorTools.TownLife.Survey -Marker TOWN_SURVEY_OK [-ExtraArgs @('-town-zones','Hub_Decks')]</summary>
        public static void Survey()=>BatchTools.Run(()=>
        {
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/logs/town-life"));Directory.CreateDirectory(folder);
            foreach(var zone in Zones())
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity");Physics.SyncTransforms();
                var json=new StringBuilder("{\"zone\":\""+zone+"\"");
                string F(float v)=>v.ToString("0.###",CultureInfo.InvariantCulture);
                string V(Vector3 v)=>$"[{F(v.x)},{F(v.y)},{F(v.z)}]";
                string S(string s)=>"\""+(s??"").Replace("\\","\\\\").Replace("\"","\\\"").Replace("\n"," ")+"\"";
                string PathOf(Transform t){var parts=new List<string>();for(var at=t;at!=null&&parts.Count<5;at=at.parent)parts.Insert(0,at.name);return string.Join("/",parts);}
                void List<T>(string key,IEnumerable<T> items,System.Func<T,string> row){json.Append(",\""+key+"\":[").Append(string.Join(",",items.Select(row))).Append("]");}
                var all=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                List("npcs",Object.FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None),n=>$"{{\"speaker\":{S(n.speaker)},\"at\":{V(n.transform.position)},\"yaw\":{F(n.transform.eulerAngles.y)},\"range\":{F(n.range)},\"active\":{(n.gameObject.activeInHierarchy?"true":"false")}}}");
                List("prompts",Object.FindObjectsByType<InteractionPrompt>(FindObjectsInactive.Include,FindObjectsSortMode.None),p=>$"{{\"type\":{S(p.GetType().Name)},\"name\":{S(p.name)},\"prompt\":{S(p.prompt)},\"at\":{V(p.transform.position)},\"range\":{F(p.range)},\"active\":{(p.gameObject.activeInHierarchy?"true":"false")}}}");
                List("signs",Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Sign_")),t=>$"{{\"text\":{S(t.text)},\"at\":{V(t.transform.position)}}}");
                List("spawns",Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None),s=>$"{{\"id\":{S(s.id)},\"at\":{V(s.transform.position)}}}");
                List("doors",all.Where(t=>t.name=="Pressure frame collision"),t=>V(t.position));
                List("colliders",Object.FindObjectsByType<Collider>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(c=>c.bounds.size.x<60&&c.bounds.size.z<60),c=>
                {
                    var b=c.bounds;string box="null";
                    if(c is BoxCollider bc){var t=c.transform;box=$"{{\"at\":{V(t.TransformPoint(bc.center))},\"size\":{V(Vector3.Scale(bc.size,t.lossyScale))},\"yaw\":{F(t.eulerAngles.y)},\"tilt\":{F(Mathf.Abs(Mathf.DeltaAngle(t.eulerAngles.x,0))+Mathf.Abs(Mathf.DeltaAngle(t.eulerAngles.z,0)))}}}";}
                    return $"{{\"path\":{S(PathOf(c.transform))},\"trigger\":{(c.isTrigger?"true":"false")},\"static\":{(c.gameObject.isStatic?"true":"false")},\"on\":{(c.enabled&&c.gameObject.activeInHierarchy?"true":"false")},\"min\":{V(b.min)},\"max\":{V(b.max)},\"box\":{box}}}";
                });
                List("renderers",Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>(r is MeshRenderer||r is SkinnedMeshRenderer)&&r.bounds.size.x<40&&r.bounds.size.z<40&&r.GetComponent<TMP_Text>()==null),r=>
                    $"{{\"path\":{S(PathOf(r.transform))},\"skinned\":{(r is SkinnedMeshRenderer?"true":"false")},\"on\":{(r.enabled&&r.gameObject.activeInHierarchy?"true":"false")},\"min\":{V(r.bounds.min)},\"max\":{V(r.bounds.max)}}}");
                json.Append("}");
                File.WriteAllText(Path.Combine(folder,zone+"-survey.json"),json.ToString());
                Debug.Log("TOWN_SURVEY "+zone);
            }
            Debug.Log("TOWN_SURVEY_OK");
        });
        /// <summary>The towns a run is for: -town-zones a,b, or all three.</summary>
        static string[] Zones()
        {
            string only=DevArgs.Value("-town-zones");
            return string.IsNullOrEmpty(only)?new[]{"Hub_Decks","TallowDrift","Lanternwick"}:only.Split(',').Select(z=>z.Trim()).Where(z=>z.Length>0).ToArray();
        }
    }
}
