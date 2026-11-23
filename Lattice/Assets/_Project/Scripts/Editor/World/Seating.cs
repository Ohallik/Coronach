using System.Collections.Generic;
using System.Linq;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace Lattice.EditorTools
{
    /// <summary>
    /// The seating rule (November 21): everyone who sits, sits on something of their own height. At the town meeting on the day of leaving every seated body's feet were 4
    /// to 19 cm under the deck and its pelvis in its bench (s57/136; D350), because the sitting pose (HomeBuilder's and the Continuance's, one for every humanoid body) leaves
    /// the knees about .29 m up whatever the body's legs, and the hips were set on benches of one height. So the seat is fitted to the sitter instead, for every body a Posed
    /// holds seated, in every scene as it is saved (TownsfolkLooks' hook, after the townsfolk are dressed, so a taller or shorter build is what is fitted):
    /// <list type="number">
    /// <item>the body is lifted until its soles rest on the floor it was placed on;</item>
    /// <item>what is drawn under its pelvis is found; where that is higher than the pelvis (a bench too tall for a short body) the body is lifted onto it, its feet hanging;</item>
    /// <item>where the pelvis is over it, the sitter gets a seat of their own under the pelvis, its top at the pelvis's underside: a folded blanket or a cushion on a bench,
    /// a crate where there is only the floor.</item>
    /// </list>
    /// The body's facing and its turn on the seat (<see cref="Posed.SeatTurn"/>) are the builder's; nothing solid is added. What it did is kept on the body (<see cref="SeatFit"/>).
    /// Read off the skin by <see cref="SeatGeometry"/>, which SeatedTests holds every zone to.
    /// </summary>
    public static class Seating
    {
        const string Worn="Assets/_Project/Art/Generated/Townsfolk/Worn",GoodsPath="Assets/_Project/Resources/WorldMaterials/townsfolk-goods.mat";
        static readonly string[] Tones={"moss","rust","slate","oxblood","sand","indigo"};
        public static string Fit(Scene scene,string zone)
        {
            var seated=SeatGeometry.Seated(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Posed>(true)));if(seated.Length==0)return null;
            var goods=AssetDatabase.LoadAssetAtPath<Material>(GoodsPath);
            // As the builder left them: each one's model where it was put, the seat this rule gave it taken away.
            var hidden=new List<GameObject>();
            foreach(var p in seated)
            {
                for(var t=p.transform;t!=null;t=t.parent)if(!t.gameObject.activeSelf){t.gameObject.SetActive(true);hidden.Add(t.gameObject);}
                var model=Model(p);var record=p.GetComponent<SeatFit>();
                if(record==null){record=p.gameObject.AddComponent<SeatFit>();record.builtLocal=model.localPosition;}
                else{if(record.seat!=null)Object.DestroyImmediate(record.seat);record.seat=null;model.localPosition=record.builtLocal;}
                p.Hold();
            }
            Physics.SyncTransforms();
            int lifted=0,raised=0,given=0;float most=0;
            using(var probe=new SeatGeometry.Probe(seated.Select(p=>p.transform.position)))
                foreach(var p in seated)
                {
                    var model=Model(p);var record=p.GetComponent<SeatFit>();var skin=SeatGeometry.Skin(p.gameObject);var hips=SeatGeometry.Hips(p.gameObject);
                    if(skin.Count==0||hips==null){record.fit="no skin or no hips: left";continue;}
                    float floor=p.transform.position.y,soles=SeatGeometry.Soles(skin),pelvis=SeatGeometry.Pelvis(skin,hips,p.transform.right,p.transform.forward);
                    // 1. Onto its feet.
                    float lift=Mathf.Clamp(floor-soles+.004f,-.06f,.7f);
                    float top=probe.Under(hips.position+Vector3.up*Mathf.Max(0,lift),1.6f+Mathf.Max(0,lift));if(float.IsNaN(top)||top<floor)top=floor;
                    float under=float.IsInfinity(pelvis)?float.NaN:pelvis+lift;
                    // 2. Onto what it sits on, where that is higher than its pelvis now.
                    float onto=!float.IsNaN(under)&&top>under?top-under+.004f:0;
                    Move(p,model,lift+onto);if(Mathf.Abs(lift)>.005f)lifted++;if(onto>0)raised++;most=Mathf.Max(most,lift+onto);
                    string sits="what it was given";
                    // 3. A seat of its own under the pelvis.
                    if(!float.IsNaN(under)&&goods!=null)
                    {
                        float gap=under+onto-top;
                        if(gap>.02f)
                        {
                            bool floorOnly=top<=floor+.03f;var forward=p.transform.forward;var at=new Vector3(hips.position.x,0,hips.position.z)-forward*.04f;
                            int tone=Mathf.FloorToInt(Mathf.Repeat(Mathf.Sin(at.x*12.9898f+at.z*78.233f)*43758.5453f,1)*Tones.Length)%Tones.Length;
                            record.seat=floorOnly&&gap>.22f?Crate(p,at,top,gap,tone,goods):Cushion(p,at,top,gap,tone,goods,gap>.12f);
                            if(record.seat!=null){given++;sits=record.seat.name;}
                        }
                    }
                    record.fit=$"lifted {lift:0.000} onto its feet, {onto:0.000} onto its seat; sits on {sits}";EditorUtility.SetDirty(record);
                }
            foreach(var h in hidden)h.SetActive(false);
            return $"SEATING {zone} seated={seated.Length} lifted={lifted} onto-seat={raised} seats-given={given} most={most:0.00}";
        }
        static Transform Model(Posed p)=>p.transform.Find(TownsfolkLook.Model)??(p.transform.childCount>0?p.transform.GetChild(0):p.transform);
        static void Move(Posed p,Transform model,float metres)
        {
            if(Mathf.Abs(metres)<.0005f)return;var parent=model.parent!=null?model.parent:p.transform;
            model.localPosition+=parent.InverseTransformVector(Vector3.up*metres);PrefabUtility.RecordPrefabInstancePropertyModifications(model);Physics.SyncTransforms();
        }
        /// <summary>A cushion, or a blanket folded twice, under a sitter on a bench: as high as the gap between the bench and the sitter's pelvis, a little wider than the hips.</summary>
        static GameObject Cushion(Posed p,Vector3 at,float top,float gap,int tone,Material goods,bool folded)
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>($"{Worn}/{(folded?"blanket":"cushion")}-{Tones[tone]}.asset");if(mesh==null)return null;
            var go=new GameObject($"Seat {(folded?"blanket":"cushion")} ({Tones[tone]})",typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=goods;r.shadowCastingMode=ShadowCastingMode.Off;
            go.transform.SetPositionAndRotation(new Vector3(at.x,top,at.z),p.transform.rotation);go.transform.localScale=new Vector3(.44f,gap,.38f);go.transform.SetParent(p.transform,true);
            return go;
        }
        /// <summary>A packing crate under a sitter who has only the floor, as tall as the sitter's pelvis is high (a lashing strap in the sitter's tone across its lid).</summary>
        static GameObject Crate(Posed p,Vector3 at,float top,float gap,int tone,Material goods)
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>($"{Worn}/crate-{Tones[tone]}.asset");if(mesh==null)return null;
            var go=new GameObject("Seat crate",typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=goods;r.shadowCastingMode=ShadowCastingMode.On;
            go.transform.SetPositionAndRotation(new Vector3(at.x,top,at.z),p.transform.rotation);go.transform.localScale=new Vector3(.46f,gap,.42f);go.transform.SetParent(p.transform,true);
            return go;
        }
    }
}
