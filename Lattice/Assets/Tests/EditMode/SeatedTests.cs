using System.Collections.Generic;
using System.Linq;
using Lattice.Data;
using Lattice.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lattice.Tests.EditMode
{
    /// <summary>Everyone who sits, sits on something of their own height (November 21). At the town meeting on the day of leaving every seated body's feet were 4 to 19 cm
    /// under the deck and its pelvis in its bench (s57/136, and the editor's own still of it), because the sitting pose leaves the knees about .29 m up and the hips were set
    /// on benches of one height. In every zone, each body held seated by a Posed, posed as the scene loads (Posed.Hold, what a still and play both show): its soles are not
    /// under the floor it was placed on, its pelvis is not inside what it sits on, and it does sit on something (not more than five centimetres over it). Read off the skin
    /// (SeatGeometry, shared with the seating rule), in the editor, where a body's mesh can be read. One case per zone.</summary>
    public sealed class SeatedTests
    {
        static IEnumerable<string> Zones()=>Resources.LoadAll<ZoneDef>("Definitions/ZoneDef").Where(z=>z.kind==ZoneKind.GroundSafe||z.kind==ZoneKind.GroundCombat).Select(z=>z.scene).Where(s=>!string.IsNullOrEmpty(s)).Distinct().OrderBy(s=>s);
        const float Under=.02f,Inside=.02f,Over=.05f;

        [TestCaseSource(nameof(Zones))] public void EveryoneSeatedSitsOnSomethingOfTheirOwnHeight(string zone)
        {
            string path="Assets/_Project/Scenes/"+zone+".unity";
            if(!System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath,"..",path))){Assert.Ignore(zone+" is not built");return;}
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var seated=SeatGeometry.Seated(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Posed>(true)));
                TestContext.WriteLine($"SEATED {zone} bodies={seated.Length}");
                if(seated.Length==0)return;
                foreach(var p in seated){if(!p.gameObject.activeInHierarchy)Activate(p.transform);p.Hold();}
                Physics.SyncTransforms();
                using var probe=new SeatGeometry.Probe(seated.Select(p=>p.transform.position));
                // The check can fail: a seated body over a seat (something drawn under its hips, above the floor), dropped 8 cm (less than its hips stand over its pelvis's
                // underside, so the seat's top is still under the hips), has its soles under the floor and its pelvis in its seat.
                var first=seated.FirstOrDefault(p=>{var h=SeatGeometry.Hips(p.gameObject);return h!=null&&probe.Under(h.position,1.5f)>p.transform.position.y+.1f&&!float.IsInfinity(SeatGeometry.Pelvis(SeatGeometry.Skin(p.gameObject),h,p.transform.right,p.transform.forward));});
                if(first!=null)
                {
                    var model=first.transform.Find(TownsfolkLook.Model)??first.transform.GetChild(0);
                    var was=model.position;model.position-=Vector3.up*.08f;Physics.SyncTransforms();
                    var control=Read(first,probe);model.position=was;Physics.SyncTransforms();
                    Assert.IsTrue(control.under>Under&&control.inside>Inside,$"control: {first.name} dropped 8 cm passes (soles {control.under:0.000} under, pelvis {control.inside:0.000} inside)");
                }
                else TestContext.WriteLine($"SEATED {zone}: no seated body is over a seat, so the control is not run");
                List<string> Faults(out float worstUnder,out float worstInside)
                {
                    var found=new List<string>();worstUnder=0;worstInside=0;
                    foreach(var p in seated)
                    {
                        var r=Read(p,probe);string who=$"{p.name} at ({p.transform.position.x:0.0}, {p.transform.position.z:0.0})";
                        worstUnder=Mathf.Max(worstUnder,r.under);worstInside=Mathf.Max(worstInside,r.inside);
                        if(r.under>Under)found.Add($"{who}: soles {r.under*100:0} cm under the floor");
                        if(r.inside>Inside)found.Add($"{who}: pelvis {r.inside*100:0} cm inside its seat");
                        if(r.over>Over)found.Add($"{who}: sits {r.over*100:0} cm over its seat, on nothing");
                    }
                    return found;
                }
                // As the builder seated them, before the seating rule (each one's SeatFit keeps where its model was put): the meeting as the capture played it.
                var undo=new List<System.Action>();
                foreach(var fit in seated.Select(p=>p.GetComponent<SeatFit>()).Where(f=>f!=null))
                {
                    var m=fit.transform.Find(TownsfolkLook.Model)??fit.transform.GetChild(0);var at=m.localPosition;m.localPosition=fit.builtLocal;
                    bool shown=fit.seat!=null&&fit.seat.activeSelf;if(fit.seat!=null)fit.seat.SetActive(false);
                    undo.Add(()=>{m.localPosition=at;if(fit.seat!=null)fit.seat.SetActive(shown);});
                }
                Physics.SyncTransforms();var asBuilt=Faults(out float builtUnder,out float builtInside);foreach(var u in undo)u();Physics.SyncTransforms();
                var faults=Faults(out float worstUnder,out float worstInside);
                TestContext.WriteLine($"SEATED {zone} bodies={seated.Length} faults={faults.Count} worst-under={worstUnder:0.000} worst-inside={worstInside:0.000} as-built-faults={asBuilt.Count} as-built-under={builtUnder:0.000} as-built-inside={builtInside:0.000}");
                Assert.IsEmpty(faults,$"{zone}: {faults.Count} faults among {seated.Length} seated:\n"+string.Join("\n",faults.Take(30)));
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
        }
        static void Activate(Transform t){for(var at=t;at!=null;at=at.parent)at.gameObject.SetActive(true);}
        /// <summary>How far its soles are under the floor it was placed on (its own place's height), how far its pelvis is inside the top of what is drawn under it, and how far
        /// over that top it sits (each nought when it isn't).</summary>
        static (float under,float inside,float over) Read(Posed p,SeatGeometry.Probe probe)
        {
            var skin=SeatGeometry.Skin(p.gameObject);var hips=SeatGeometry.Hips(p.gameObject);if(skin.Count==0||hips==null)return (0,0,0);
            float floor=p.transform.position.y,soles=SeatGeometry.Soles(skin);
            float pelvis=SeatGeometry.Pelvis(skin,hips,p.transform.right,p.transform.forward);
            if(float.IsInfinity(pelvis))return (Mathf.Max(0,floor-soles),0,0);
            float top=probe.Under(hips.position,1.5f);if(float.IsNaN(top)||top<floor)top=floor;
            return (Mathf.Max(0,floor-soles),Mathf.Max(0,top-pelvis),Mathf.Max(0,pelvis-top));
        }
    }
}
