using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// Where a seated body is, read off its skin as it is posed (November 21, after the town meeting on the day of leaving: every seated body's feet were 4 to 19 cm under
    /// the deck and its pelvis in its bench, in play and in the editor alike, because the sitting pose leaves the knees about .29 m up and its hips were set on a bench of one
    /// height). Shared by the editor's seating rule (Seating, which lifts each sitter onto their feet and gives them a seat at their own height) and the test that holds every
    /// zone to it (SeatedTests), as SightlineAudit is by its builder and its test. Reads mesh data: the editor only (a body's mesh isn't readable in play).
    /// </summary>
    public static class SeatGeometry
    {
        /// <summary>A seated body: a Posed with a seat, showing a seated pose, or one held in the Continuance's "sit" (which keeps no seat height).</summary>
        public static Posed[] Seated(IEnumerable<Posed> all)=>all.Where(p=>p!=null&&(p.seat>0&&p.Seated||p.Showing=="sit")).ToArray();
        static bool Piece(Transform t){for(var at=t;at!=null;at=at.parent){if(at.name.StartsWith("Carried ")||at.name.StartsWith("Worn ")||at.name.StartsWith("Seat "))return true;if(at.GetComponent<Posed>()!=null)break;}return false;}
        /// <summary>Every point of the body's skin, in the world, as it is posed now.</summary>
        public static List<Vector3> Skin(GameObject person)
        {
            var points=new List<Vector3>();
            foreach(var skin in person.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if(skin.sharedMesh==null||Piece(skin.transform))continue;points.AddRange(World(skin));
            }
            return points;
        }
        /// <summary>A skinned body's points in the world as posed now. Whether a baked mesh carries its renderer's scale has differed by version and by flag, so both readings are
        /// made and the one that puts the skin round its own bones is kept (a renderer's bounds are the standing body's, so a seated one's height can't tell).</summary>
        public static List<Vector3> World(SkinnedMeshRenderer skin)
        {
            var baked=new Mesh();skin.BakeMesh(baked);var verts=baked.vertices;Object.DestroyImmediate(baked);
            var scaled=skin.transform.localToWorldMatrix;var unscaled=Matrix4x4.TRS(skin.transform.position,skin.transform.rotation,Vector3.one);
            var weights=skin.sharedMesh.boneWeights;var bones=skin.bones;
            float Off(Matrix4x4 m)
            {
                double sum=0;int n=0;
                for(int i=0;i<verts.Length&&i<weights.Length;i+=7)
                {
                    int b=weights[i].boneIndex0;if(b<0||b>=bones.Length||bones[b]==null||weights[i].weight0<.9f)continue;
                    sum+=(m.MultiplyPoint3x4(verts[i])-bones[b].position).magnitude;n++;
                }
                return n==0?0:(float)(sum/n);
            }
            var use=Off(scaled)<=Off(unscaled)?scaled:unscaled;
            return verts.Select(v=>use.MultiplyPoint3x4(v)).ToList();
        }
        public static Transform Hips(GameObject person)
        {
            var rig=person.GetComponentInChildren<Animator>(true);return rig!=null&&rig.isHuman?rig.GetBoneTransform(HumanBodyBones.Hips):null;
        }
        /// <summary>The lowest of the skin: a seated body's soles.</summary>
        public static float Soles(List<Vector3> skin)=>skin.Count==0?float.NaN:skin.Min(p=>p.y);
        /// <summary>The underside of the pelvis: the lowest skin under the hips, within .16 m either side of their line, from .22 behind to .1 ahead of them and no more than
        /// .3 under them (HomeBuilder's SeatOf, which set the hips on the bench). Skin lower still in that column (a coat's or a robe's skirt hanging past the seat) is passed
        /// over, not taken for the seat: HomeBuilder's read gave up on such a body, and most townsfolk wear a coat. Infinity where there is no skin there at all.</summary>
        public static float Pelvis(List<Vector3> skin,Transform hips,Vector3 right,Vector3 forward)
        {
            float low=float.PositiveInfinity;var at=hips.position;
            foreach(var w in skin)
            {
                var d=w-at;if(d.y>0||d.y<-.3f)continue;float side=Vector3.Dot(d,right),ahead=Vector3.Dot(d,forward);
                if(Mathf.Abs(side)>=.16f||ahead<=-.22f||ahead>=.1f)continue;
                low=Mathf.Min(low,w.y);
            }
            return low;
        }

        /// <summary>The drawn surfaces round some seated people, as a ray meets them: every mesh drawn near them that isn't a body or something a body wears or carries (a seat
        /// counts), on a probe layer for as long as this lives.</summary>
        public sealed class Probe:System.IDisposable
        {
            const int Layer=31;readonly List<GameObject> probes=new();
            public Probe(IEnumerable<Vector3> near,float reach=2.5f)
            {
                var points=near.ToArray();
                foreach(var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                {
                    var r=filter.GetComponent<MeshRenderer>();if(r==null||!r.enabled||!filter.gameObject.activeInHierarchy||filter.sharedMesh==null)continue;
                    if(filter.GetComponents<Component>().Any(c=>c!=null&&c.GetType().Name.StartsWith("TextMeshPro"))||Worn(filter.transform))continue;
                    var b=r.bounds;b.Expand(new Vector3(reach,1,reach));if(!points.Any(b.Contains))continue;
                    var probe=new GameObject("Seat probe"){layer=Layer,hideFlags=HideFlags.DontSave};probe.transform.SetParent(filter.transform,false);
                    probe.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;probes.Add(probe);
                }
                Physics.SyncTransforms();
            }
            static bool Worn(Transform t){for(var at=t;at!=null;at=at.parent){if(at.name.StartsWith("Carried ")||at.name.StartsWith("Worn "))return true;if(at.GetComponent<Posed>()!=null||at.GetComponent<PropTint>()!=null)break;}return false;}
            /// <summary>The top of whatever is drawn under a body's hips: the highest surface a ray from above them meets that is no more than .2 m over the hips themselves
            /// (so a seat or a trunk they have sunk into is found by its top, and a table over a sitter's knees is passed over), no further down than <paramref name="down"/>. NaN
            /// if nothing is.</summary>
            public float Under(Vector3 hips,float down)
            {
                var from=hips+Vector3.up*.6f;
                foreach(var hit in Physics.RaycastAll(from,Vector3.down,down+.6f,1<<Layer,QueryTriggerInteraction.Collide).OrderBy(h=>h.distance))
                    if(hit.point.y<=hips.y+.2f)return hit.point.y;
                return float.NaN;
            }
            public void Dispose(){foreach(var p in probes)if(p!=null)Object.DestroyImmediate(p);probes.Clear();Physics.SyncTransforms();}
        }
    }
}
