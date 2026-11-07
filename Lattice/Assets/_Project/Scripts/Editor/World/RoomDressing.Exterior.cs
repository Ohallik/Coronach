using System.Collections.Generic;
using System.Linq;
using Lattice.Core;
using Lattice.UI;
using Lattice.World;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Exterior dressing (November 4): the Cinder Halo's hull roofs and the Tallow refuge's deck. Both were bare
    /// repeated plating seen from the flight camera. Ships fly at y=1: the Halo's roofs stand above that plane (y~3.9),
    /// so roof equipment there is collider-free and held back from every roof edge; the refuge deck lies below it, so
    /// there only flat details below the flight ceiling are allowed (the Gullet's rule, `FlightAudit`).
    /// </summary>
    public static partial class RoomDressing
    {
        static bool Exterior=>zone=="Hub_CinderHalo";
        const float RoofMin=2.5f,RoofEdgeMargin=2f;
        const string TempHullCollider="Dressing survey hull collider";

        /// <summary>The roof height under a point (first surface that is not dressing), or NaN.</summary>
        static float RoofAt(float x,float z)
        {
            foreach(var hit in Physics.RaycastAll(new Vector3(x,60,z),Vector3.down,120,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
                if(!hit.collider.transform.IsChildOf(root))return hit.normal.y>.9f?hit.point.y:float.NaN;
            return float.NaN;
        }
        /// <summary>Something standing on the roof over a rectangle (plant housings, hatches, walls), not the roof itself.</summary>
        static bool RoofBlocked(float x,float z,float w,float l,float yaw)
        {
            float y=RoofAt(x,z);if(float.IsNaN(y))return true;
            foreach(var c in Physics.OverlapBox(new Vector3(x,y+.6f,z),new Vector3(w*.5f,.42f,l*.5f),Quaternion.Euler(0,yaw,0),~0,QueryTriggerInteraction.Ignore))
                if(!c.transform.IsChildOf(root))return true;
            return false;
        }

        static void Halo()
        {
            string[] pictos={"PictoOffice","PictoMarket","PictoRepair"};
            float roof=float.NaN;
            for(int i=0;i<StationRedesign.HullX.Length;i++)
            {
                float x=StationRedesign.HullX[i];bool work=x>20;
                roof=RoofAt(x,-6);
                // The hull's trade, painted large on its roof where a pilot reads it on approach.
                Detail(pictos[i],x+4.5f,-9.5f,3.4f,3.4f,0,.02f);
                // Docking lane paint leads from the roof's front edge toward the hull's purpose mark.
                Detail("DecalHazard",x,-13.2f,.5f,18,90,.02f,true,.9f);
                // Solar arrays tilt over the front roof; the market and office roofs carry four, the workshop two.
                foreach(var (dx,dz) in work?new[]{(-5.5f,-3f),(-5.5f,1.5f)}:new[]{(-5.5f,-3f),(-5.5f,1.5f),(5.5f,-3f),(5.5f,1.5f)})
                    RoofPut("DressSolarArray",x+dx,dz,90);
                // Tanks feed the plant housing; a pipe run follows the roof spine from the front plates to it.
                RoofPut("DressFuelTank",x+(work?5.5f:-1.2f),work?-1.5f:5.2f,0,work?1:.85f);
                for(float z=-6.5f;z<=4f;z+=2.6f)if(!work||z<1)RoofPut("DressPipeRun",x+(work?1.5f:2.2f),z,90);
                // Inspection hatches and grates between the equipment, hazard paint around the plant housing.
                foreach(var (dx,dz) in new[]{(-1.5f,-11f),(7.5f,-12.2f),(-8f,7.5f),(8.2f,20.6f)})Detail("DecalHatch",x+dx,dz,1.5f,1.5f,0,.02f);
                foreach(var (dx,dz) in new[]{(1.5f,-12.6f),(-7.6f,-12.6f),(5f,7.6f)})Detail("DecalGrate",x+dx,dz,1.4f,1.4f,0,.02f);
                float width=x<-20?10:x>20?7:12,depth=work?16:10;
                foreach(int side in new[]{-1,1})
                {
                    Detail("DecalHazard",x+side*(width*.5f+.6f),14,.4f,depth+1.2f,0,.02f,true,.9f);
                    Detail("DecalHazard",x,14+side*(depth*.5f+.6f),.4f,width+1.6f,90,.02f,true,.9f);
                }
                // One comms mast per hull on the back roof, away from the docking front.
                RoofPut("DressCommsMast",x+(work?-7.5f:7.4f),work?17:20.4f,0,.72f);
                // Navigation lamps at the four roof corners.
                foreach(int sx in new[]{-1,1})foreach(float z in new[]{-13.4f,21.4f})RoofLamp(x+sx*9.4f,z);
            }
            Debug.Log($"DRESSING_HALO roof={roof:0.00}");
        }

        /// <summary>Collider-free roof pieces: their footprint is the rendered model's.</summary>
        static Rect VisualFootprint(GameObject go)
        {
            var b=ModelGeometry.BoundsOf(go);
            return Rect.MinMaxRect(b.min.x+.05f,b.min.z+.05f,b.max.x-.05f,b.max.z-.05f);
        }
        static GameObject RoofPut(string key,float x,float z,float yaw,float scale=1)
        {
            var go=Put(key,x,z,yaw,scale,false);
            return go;
        }
        static void RoofLamp(float x,float z)
        {
            var go=RoofPut("DressDeckLamp",x,z,0,.8f);
            var holder=new GameObject("Navigation lamp light"){isStatic=true};holder.transform.SetParent(go.transform,false);holder.transform.localPosition=Vector3.up*1.1f;
            var light=holder.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1f,.78f,.45f);
            light.range=4.5f;light.intensity=.9f;light.shadows=LightShadows.None;
        }

        /// <summary>Roof equipment may never meet a ship or hide one beside the hull: no colliders, standing on a roof
        /// above the flight plane, the same roof continuing at least 2 m past its footprint, clear of roof structures
        /// and of each other. Floor details on the roofs stay above the flight plane too.</summary>
        static void ExteriorAudit()
        {
            int checkedPieces=0;
            foreach(var item in placed.Where(p=>!p.structural))
            {
                var go=item.go;string at=$"{item.key} at {go.transform.position}";checkedPieces++;
                if(go.GetComponentsInChildren<Collider>().Length>0){problems.Add(at+" has a collider");continue;}
                float baseY=go.transform.position.y;
                if(baseY<RoofMin){problems.Add($"{at} stands below the roofs (y={baseY:0.00})");continue;}
                var rect=VisualFootprint(go);var grown=Rect.MinMaxRect(rect.xMin-RoofEdgeMargin,rect.yMin-RoofEdgeMargin,rect.xMax+RoofEdgeMargin,rect.yMax+RoofEdgeMargin);
                foreach(var p in new[]{new Vector2(grown.xMin,grown.yMin),new Vector2(grown.xMax,grown.yMin),new Vector2(grown.xMin,grown.yMax),new Vector2(grown.xMax,grown.yMax),
                    new Vector2(grown.center.x,grown.yMin),new Vector2(grown.center.x,grown.yMax),new Vector2(grown.xMin,grown.center.y),new Vector2(grown.xMax,grown.center.y)})
                {
                    // A probe may land on roof equipment (a housing) but never off the roof or on a lower deck.
                    float y=RoofAt(p.x,p.y);
                    if(float.IsNaN(y)||y<baseY-.2f){problems.Add($"{at} is within {RoofEdgeMargin} m of a roof edge (probe {p} -> {y:0.00})");break;}
                }
                var bounds=ModelGeometry.BoundsOf(go);
                var centre=bounds.center;var half=bounds.extents*.92f;centre.y=baseY+.25f+half.y*.5f;half.y=Mathf.Max(.05f,half.y*.5f);
                var blocker=Physics.OverlapBox(centre,half,Quaternion.identity,~0,QueryTriggerInteraction.Ignore).FirstOrDefault(c=>!c.transform.IsChildOf(root));
                if(blocker!=null){problems.Add($"{at} intersects {blocker.name}");continue;}
                foreach(var other in placed)
                {
                    if(other==item||placed.IndexOf(other)>placed.IndexOf(item))continue;
                    var a=VisualFootprint(other.go);if(rect.Overlaps(Rect.MinMaxRect(a.xMin+.05f,a.yMin+.05f,a.xMax-.05f,a.yMax-.05f))){problems.Add($"{at} intersects dressing {other.key}");break;}
                }
            }
            int low=0;
            foreach(var r in root.GetComponentsInChildren<MeshRenderer>())
                if(r.name.StartsWith("Floor detail")&&r.GetComponent<MeshFilter>().sharedMesh.vertices.Any(v=>v.y<RoofMin))low++;
            if(low>0)problems.Add($"{low} roof detail batches dip below the roofs");
            Debug.Log($"DRESSING_EXTERIOR {zone} pieces={checkedPieces} problems={problems.Count}");
        }

        // ---------- the Tallow refuge deck, seen from above: flat details only, below the flight ceiling ----------
        static List<Collider> SurveyHull()
        {
            var made=new List<Collider>();
            var hull=GameObject.Find("TallowStationHull");if(hull==null){problems.Add("TallowStationHull not found");return made;}
            foreach(var filter in hull.GetComponentsInChildren<MeshFilter>())
            {
                var holder=new GameObject(TempHullCollider);holder.transform.SetParent(filter.transform,false);
                var c=holder.AddComponent<MeshCollider>();c.sharedMesh=filter.sharedMesh;made.Add(c);
            }
            Physics.SyncTransforms();return made;
        }

        /// <summary>Sixteen short plates, each laid on the hull by itself: its stripes jumped where they met, and where the hull's rim falls away a plate's middle went under the surface, leaving a dashed sliver of stripes.
        /// One band, each row laid on the hull and clear of it, its stripes running on all the way round. A hole in the hull, under its roof cover, would drop a ray ten metres: a row there keeps its neighbour's height.</summary>
        static void DockRing(Vector3 dock)
        {
            const int Rows=96,Across=2;const float Radius=5.6f,Width=.45f,Lift=.12f,Tile=.9f;
            int repeats=Mathf.RoundToInt(2*Mathf.PI*Radius/Tile);
            var verts=new Vector3[(Rows+1)*(Across+1)];var uvs=new Vector2[verts.Length];float last=float.NaN;
            for(int j=0;j<=Rows;j++)for(int i=0;i<=Across;i++)
            {
                float a=j%Rows*Mathf.PI*2/Rows,reach=Radius+Width*(i/(float)Across-.5f);
                float x=dock.x+Mathf.Cos(a)*reach,z=dock.z+Mathf.Sin(a)*reach,y=Surface(x,z);
                if(float.IsNaN(y)||!float.IsNaN(last)&&Mathf.Abs(y-last)>.8f)y=float.IsNaN(last)?FlightCeiling-.5f:last;
                last=y;int k=j*(Across+1)+i;verts[k]=new Vector3(x,y+Lift,z);uvs[k]=new Vector2(Width/Tile*i/Across,repeats*(float)j/Rows);
            }
            Append("DecalHazard",dock.x,dock.z,verts,uvs,Across,Rows);
        }

        static void TallowRefuge()
        {
            var random=new System.Random(5309);int count=0;
            float R(float a,float b)=>a+(float)random.NextDouble()*(b-a);
            var hull=GameObject.Find("TallowStationHull");var b=ModelGeometry.BoundsOf(hull);
            // The dock and the line inspection keep their own clear deck around them.
            var keep=Object.FindObjectsByType<InteractionPrompt>(FindObjectsSortMode.None).Select(p=>new Vector2(p.transform.position.x,p.transform.position.z)).ToList();
            keep.AddRange(Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Select(s=>new Vector2(s.transform.position.x,s.transform.position.z)));
            // Plates, hatches and grates in a loose grid over the flat deck: the generated hull texture is soft at this
            // scale, so the crisp details carry the surface.
            string[] kinds={"DecalHatch","DecalGrate","DecalTread","DecalDrain","DecalHatch","DecalTrench"};
            for(float z=b.min.z+4;z<=b.max.z-4;z+=R(5.5f,7.5f))for(float x=b.min.x+4;x<=b.max.x-4;x+=R(5.5f,7.5f))
            {
                var p=new Vector2(x+R(-1.4f,1.4f),z+R(-1.4f,1.4f));
                if(keep.Any(k=>Vector2.Distance(k,p)<4.5f)||random.NextDouble()<.25)continue;
                string kind=kinds[random.Next(kinds.Length)];float size=kind=="DecalTrench"?2.4f:kind=="DecalTread"?1.8f:1.5f;
                if(Detail(kind,p.x,p.y,size,kind=="DecalTrench"?.9f:size,random.Next(4)*90,.02f))count++;
            }
            // Hazard paint rings the docking apron where hulls set down, and studs mark its approach.
            var dock=Object.FindObjectsByType<InteractionPrompt>(FindObjectsSortMode.None).Where(p=>p.name.ToLowerInvariant().Contains("dock")).Select(p=>p.transform.position).ToArray();
            foreach(var d in dock)
            {
                DockRing(d);count++;
                if(Detail("PictoDock",d.x-7.6f,d.z-2,1.8f,1.8f,0,.025f))count++;
            }
            Debug.Log($"DRESSING_TALLOW_DECK details={count} docks={dock.Length} hull={b}");
        }
    }
}
