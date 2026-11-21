using System;
using System.Collections.Generic;
using System.Linq;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object=UnityEngine.Object;

namespace Lattice.EditorTools
{
    public static partial class TownLife
    {
        // ---------------------------------------------------------------- Lanternwick
        /// <summary>The canyon town's people past its ten standing townsfolk (LanternRoadBuilder.Townsfolk): someone picking in the orchard, and, once the relay is won back
        /// from Hessa, the two who waited on the courier square for word, at the archive's lit wall instead. Standing ones are "Townsperson", as the town's own are, so the
        /// town's own checks (QaWorldSecondTests.LanternwickHasPeopleLivingInIt) hold them too.</summary>
        internal static readonly Folk[] LanternFolk=
        {
            new("Picking in the orchard","IloBody",-21.4f,62.9f,270,new Color(.82f,.72f,.62f)),
            new("Watching the archive wall","Neve",-9f,93.6f,60,new Color(.66f,.74f,.8f),from:"bossdown.HessaKorl"),
            new("Watching it with her","SurvivorNatural",-7.6f,94.8f,40,new Color(.82f,.7f,.6f),from:"bossdown.HessaKorl"),
        };
        /// <summary>Errands off the street and the orchard's diagonal (where the routes and the companion's longest walks go): the stair lamps trimmed on the lantern terrace, a
        /// parcel carried across the courier square, a crate carried between the west stalls, and the archive's racks read again once Ishen has his record.</summary>
        internal static readonly Errand[] LanternErrands=
        {
            new("Trimming the stair lamps","SurvivorNatural",new[]{new Vector2(24.2f,35),new Vector2(24.2f,39.5f),new Vector2(24.2f,44.5f)},3.2f,new Color(.78f,.82f,.68f),true),
            new("Carrying for the courier","OdaBody",new[]{new Vector2(9.6f,-12.4f),new Vector2(13f,-12.4f)},2.4f,new Color(.84f,.74f,.84f),true,carries:"parcel"),
            new("Carrying between the west stalls","IloBody",new[]{new Vector2(-11.2f,16.4f),new Vector2(-11f,19.6f)},2.6f,new Color(.74f,.8f,.9f),true,carries:"crate"),
            new("Reading at the archive racks","Orrin",new[]{new Vector2(7,94),new Vector2(9.4f,93.6f)},4,new Color(.68f,.72f,.8f),true,from:"ishen.recordRestored"),
        };
        /// <summary>Who of the builder's own townsfolk moves with the story: the pair on the courier square, waiting for word until Hessa is down, and the one by the archive's
        /// door until Ishen has the record (then they are among the racks: the errand above). Found by where the builder stands them.</summary>
        static readonly (Vector2 at,string goneAt)[] LanternLeave={(new(-9.4f,-7.2f),"bossdown.HessaKorl"),(new(7.5f,-8.4f),"bossdown.HessaKorl"),(new(-7.6f,88.4f),"ishen.recordRestored")};
        static (float[] paths,HashSet<string> reach) lanternBefore;
        static List<(Vector2 a,Vector2 b,string route)> lanternLegs;
        static Func<Vector2,float> lanternHeight;
        /// <summary>Called by LanternRoadBuilder once Lanternwick's people stand and before its bake: its own townsfolk given their beats, the rest of the town's people stood,
        /// everyone held to <see cref="Audit"/>, and the routes' paths read on the navigation the zone had before, for <see cref="LanternwickBaked"/> to compare.
        /// Everyone is left standing in the saved scene (Whereabouts decides at load): the bake walks round all of them.</summary>
        internal static void Lanternwick(Func<Vector2,float> height,IEnumerable<(string body,float x,float z,float yaw,Color tint)> townsfolk)
        {
            lanternHeight=height;
            foreach(var old in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.parent==null&&t.name==RootName).ToArray())Object.DestroyImmediate(old.gameObject);
            var root=new GameObject(RootName).transform;var kit=ZoneKit.Ground("Lanternwick",height,3.6f);
            var standing=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.parent==null&&t.name=="Townsperson").ToArray();
            foreach(var (at,goneAt) in LanternLeave)
            {
                var who=standing.Where(t=>Vector2.Distance(new Vector2(t.position.x,t.position.z),at)<.05f).ToArray();
                if(who.Length!=1)throw new InvalidOperationException($"Lanternwick: expected one townsperson at {at}, found {who.Length}");
                When(root,"Townsperson at "+at,who[0].gameObject,null,goneAt);
            }
            foreach(var f in LanternFolk){var person=Stand(kit,root,f);person.name="Townsperson";person.SetActive(true);}
            foreach(var e in LanternErrands)Walk(kit,root,e).gameObject.SetActive(true);
            var built=townsfolk.Select(t=>("Townsperson "+t.body,t.body,new Vector3(t.x,0,t.z),1f,(string)null,LanternLeave.Where(l=>Vector2.Distance(l.at,new Vector2(t.x,t.z))<.05f).Select(l=>l.goneAt).FirstOrDefault(),false));
            var faults=Audit("Lanternwick",built.Concat(LanternFolk.Select(f=>(f.name,f.body,new Vector3(f.at.x,0,f.at.y),f.build,f.from,f.goneAt,f.door))).ToArray(),
                LanternErrands.Select(e=>(e.name,e.body,e.loop,e.back,e.build,e.carries!=null,e.from,e.goneAt)).ToArray(),Enumerable.Empty<Way>(),new (Vector2,Vector2)[0],height);
            if(faults.Count>0)throw new InvalidOperationException("Lanternwick's people stand where they may not:\n"+string.Join("\n",faults.Distinct().Take(40)));
            lanternLegs=RouteLegs("Lanternwick");
            var before=AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/_Project/Scenes/Lanternwick-Navigation.asset");
            if(before==null){lanternBefore=default;return;}
            var instance=NavMesh.AddNavMeshData(before);
            try{lanternBefore=(Paths(lanternLegs,height),Reachable());}finally{instance.Remove();}
        }
        /// <summary>Called by LanternRoadBuilder after its bake: refuses a town whose routes now walk another way, or whose prompts lost their reach, with its people standing.</summary>
        internal static void LanternwickBaked()
        {
            if(lanternBefore.paths==null){Debug.Log("TOWN_LIFE Lanternwick had no navigation before: nothing to compare");return;}
            var after=WithNavigation(()=>(Paths(lanternLegs,lanternHeight),Reachable()));
            var faults=NavigationKept(lanternBefore,after).ToList();
            if(faults.Count>0)throw new InvalidOperationException("Lanternwick's navigation changed with its people standing:\n"+string.Join("\n",faults.Take(40)));
            Debug.Log($"TOWN_LIFE Lanternwick standing={LanternFolk.Length} errands={LanternErrands.Length} routes={lanternLegs.Count}");
        }
    }
}
