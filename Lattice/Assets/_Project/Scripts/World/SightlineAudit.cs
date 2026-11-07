using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Lattice.World
{
    /// <summary>
    /// D119 for dressing: from the fixed ground camera (40° pitch, 20° yaw,
    /// 19.5 m) nothing under <c>root</c> may rise into the sightline to a hero's
    /// chest anywhere the hero can stand and reach. Uses the exact rendered
    /// meshes on a temporary probe layer, so a thin lamp post is a thin lamp
    /// post and a stall's awning counts even though it has no collider.
    /// Shared by the editor builder (which refuses to save a violating layout)
    /// and the PlayMode regression.
    /// </summary>
    public static class SightlineAudit
    {
        public const float Pitch=40,Yaw=20,Distance=19.5f,Chest=1f,HeroRadius=.35f,HeroHeight=1.9f,Step=.35f;
        // Same short stop as the Hushwell floor sweep: contact at the hero's own body is not a hidden hero.
        public const float NearIgnore=.6f;
        const int ProbeLayer=31;

        public sealed class Report
        {
            public int samples;
            public readonly List<string> hidden=new();
            public readonly HashSet<string> offenders=new();
            /// <summary>Hidden standing point, the dressing piece that hides it and where the sightline meets it.</summary>
            public readonly List<(Vector3 floor,string who,Vector3 point)> hits=new();
            public override string ToString()=>$"{samples} standing points, {hidden.Count} hidden"+(hidden.Count>0?":\n"+string.Join("\n",hidden.GetRange(0,Mathf.Min(16,hidden.Count))):"");
        }

        public static Vector3 ToCamera=>-(Quaternion.Euler(Pitch,Yaw,0)*Vector3.forward);

        /// <summary>Audits every visible mesh under <paramref name="root"/> against the
        /// floor a hero can reach from <paramref name="start"/>. Navigation must be loaded.</summary>
        public static Report Run(Transform root,Vector3 start,float spacing=.5f)
        {
            var report=new Report();
            if(root==null)return report;
            var probes=new List<GameObject>();
            try
            {
                var regions=new List<(Bounds bounds,string name)>();
                foreach(var filter in root.GetComponentsInChildren<MeshFilter>())
                {
                    var renderer=filter.GetComponent<MeshRenderer>();
                    if(renderer==null||!renderer.enabled||!filter.gameObject.activeInHierarchy||filter.sharedMesh==null)continue;
                    // A floor patch has no height of its own: a sightline that meets one on a slope has already met the ground under it.
                    if(filter.name.StartsWith("Floor detail"))continue;
                    var probe=new GameObject("Sightline probe"){layer=ProbeLayer,hideFlags=HideFlags.DontSave};
                    probe.transform.SetParent(filter.transform,false);
                    probe.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;
                    probes.Add(probe);
                    regions.Add((renderer.bounds,Owner(filter.transform,root)));
                }
                Physics.SyncTransforms();
                var away=new Vector3(Mathf.Sin(Yaw*Mathf.Deg2Rad),0,Mathf.Cos(Yaw*Mathf.Deg2Rad));
                float rise=Mathf.Tan(Pitch*Mathf.Deg2Rad);
                var tested=new HashSet<Vector2Int>();var reachable=new Dictionary<Vector2Int,bool>();
                int mask=1<<ProbeLayer;
                foreach(var (bounds,name) in regions)
                {
                    // Only the floor in this mesh's sightline shadow can be hidden by it.
                    float shadow=Mathf.Max(0,bounds.max.y-bounds.min.y)/rise+1f;
                    var far=bounds.center+away*shadow;
                    float minX=Mathf.Min(bounds.min.x,far.x-bounds.extents.x)-1,maxX=Mathf.Max(bounds.max.x,far.x+bounds.extents.x)+1;
                    float minZ=Mathf.Min(bounds.min.z,far.z-bounds.extents.z)-1,maxZ=Mathf.Max(bounds.max.z,far.z+bounds.extents.z)+1;
                    for(float x=Mathf.Floor(minX/spacing)*spacing;x<=maxX;x+=spacing)
                    for(float z=Mathf.Floor(minZ/spacing)*spacing;z<=maxZ;z+=spacing)
                    {
                        var key=new Vector2Int(Mathf.RoundToInt(x/spacing),Mathf.RoundToInt(z/spacing));
                        if(!tested.Add(key))continue;
                        if(!Standing(new Vector3(x,bounds.max.y+3,z),bounds.min.y-3,out var floor))continue;
                        if(!Reachable(floor,start,reachable,key))continue;
                        report.samples++;
                        var body=floor+Vector3.up*Chest;var eye=body+ToCamera*Distance;
                        if(Physics.Raycast(eye,body-eye,out var hit,Distance-NearIgnore,mask,QueryTriggerInteraction.Collide))
                        {
                            string who=Owner(hit.collider.transform,root);report.offenders.Add(who);
                            report.hits.Add((floor,who,hit.point));
                            report.hidden.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,"({0:0.0},{1:0.00},{2:0.0}) hidden by {3} at {4:0.00} m",floor.x,floor.y,floor.z,who,hit.point.y));
                        }
                    }
                }
            }
            finally{foreach(var probe in probes)if(probe!=null)Object.DestroyImmediate(probe);Physics.SyncTransforms();}
            return report;
        }

        /// <summary>A place the hero's capsule fits, on a floor flat enough to stand on.</summary>
        static bool Standing(Vector3 above,float lowest,out Vector3 floor)
        {
            floor=default;
            var hits=Physics.RaycastAll(above,Vector3.down,above.y-lowest,~(1<<ProbeLayer),QueryTriggerInteraction.Ignore);
            if(hits.Length==0)return false;
            System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
            foreach(var hit in hits)
            {
                if(hit.normal.y<.7f)continue;
                var p=hit.point;
                var bottom=p+Vector3.up*(HeroRadius+Step);var top=p+Vector3.up*(HeroHeight-HeroRadius);
                if(Physics.CheckCapsule(bottom,top,HeroRadius-.02f,~(1<<ProbeLayer),QueryTriggerInteraction.Ignore))continue;
                floor=p;return true;
            }
            return false;
        }

        /// <summary>Connected to the arrival on the baked navigation. The bake is shrunk by the
        /// companion's larger radius, so a hero point within that margin of it also counts.</summary>
        static bool Reachable(Vector3 floor,Vector3 start,Dictionary<Vector2Int,bool> cache,Vector2Int key)
        {
            if(cache.TryGetValue(key,out bool known))return known;
            bool ok=false;
            if(NavMesh.SamplePosition(floor,out var hit,.8f,NavMesh.AllAreas)&&Mathf.Abs(hit.position.y-floor.y)<.5f
               &&NavMesh.SamplePosition(start,out var from,2f,NavMesh.AllAreas))
            {
                var path=new NavMeshPath();
                ok=NavMesh.CalculatePath(from.position,hit.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
            }
            cache[key]=ok;return ok;
        }

        static string Owner(Transform t,Transform root)
        {
            while(t.parent!=null&&t.parent!=root)t=t.parent;
            return t.name;
        }
    }
}
