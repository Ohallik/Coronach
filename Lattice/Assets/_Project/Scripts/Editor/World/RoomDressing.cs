using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Lattice.EditorTools
{
    /// <summary>Isolated room and outpost dressing. See Apply.</summary>
    public static partial class RoomDressing
    {
        /// <summary>Read-only: writes every static collider, prompt and spawn of the dressed zones to Builds/logs/dressing-survey-*.txt.</summary>
        public static void Survey()=>BatchTools.Run(()=>
        {
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/logs"));
            foreach(var zone in new[]{"Hub_Decks","TallowDrift","Sorrel_Ridges"})
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity");
                var text=new StringBuilder();
                foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).OrderBy(c=>c.bounds.center.x).ThenBy(c=>c.bounds.center.z))
                {
                    if(!c.enabled||c.isTrigger)continue;var b=c.bounds;
                    if(zone=="Sorrel_Ridges"&&c is MeshCollider&&b.size.x>100)continue;
                    text.AppendLine($"COLLIDER {Locate(c.transform)} | {c.GetType().Name} static={c.gameObject.isStatic} min=({b.min.x:0.00},{b.min.y:0.00},{b.min.z:0.00}) max=({b.max.x:0.00},{b.max.y:0.00},{b.max.z:0.00})");
                }
                foreach(var p in Object.FindObjectsByType<Lattice.Core.InteractionPrompt>(FindObjectsSortMode.None))
                    text.AppendLine($"PROMPT {p.GetType().Name} {Locate(p.transform)} at {p.transform.position} range={p.range}");
                foreach(var s in Object.FindObjectsByType<Lattice.Core.SpawnPoint>(FindObjectsSortMode.None))
                    text.AppendLine($"SPAWN {s.id} at {s.transform.position}");
                foreach(var e in Object.FindObjectsByType<Lattice.World.EncounterVolume>(FindObjectsSortMode.None))
                    text.AppendLine($"ENCOUNTER {e.encounterId} bounds {e.GetComponent<Collider>().bounds}");
                foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.transform.parent==null||r.transform.root.name.Contains("Biome")).Take(400))
                    if(r.name.Contains("Biome")||r.GetComponent<Lattice.World.BiomePatch>()!=null)text.AppendLine($"BIOME {Locate(r.transform)} bounds {r.bounds}");
                File.WriteAllText(Path.Combine(folder,"dressing-survey-"+zone+".txt"),text.ToString());
            }
            Debug.Log("DRESSING_SURVEY_OK");
        });
        static string Locate(Transform t){var s=t.name;while(t.parent!=null){t=t.parent;s=t.name+"/"+s;}return s;}
    }
}
