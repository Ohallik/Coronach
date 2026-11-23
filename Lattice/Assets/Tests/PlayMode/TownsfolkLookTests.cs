using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using Lattice.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lattice.Tests.PlayMode
{
    /// <summary>Guide 0.1's line on people ("at least three looks per group, no two neighbours alike") as a player reads it, from the whole-game capture of November 20: every
    /// reviewer but one reported clones (four of one red-haired woman at the decks' meeting, five aproned smiths in a row in the foundry, one antennaed body in colour swaps across
    /// Driftreed), and every older people test passed, because each measured a material's name or a tint, never what stands together in one screen. This measures that: along
    /// every leg a replay route walks in the zone, the play camera's frame with the hero at its middle (the zone's own profile, 16:9), and in each frame every ordinary townsperson
    /// whose body is in it. Two of one template read as copies unless three of their clothes, head, what they carry and their build differ (<see cref="TownsfolkLook"/>: a
    /// colour swap of one figure is a copy, and so is the same figure carrying a crate). Two the story never has there together (one gone at the flag the other comes with) are
    /// not compared. One case per zone, as the shared zone checks are (ZoneWalk), so <c>focused.ps1 -Zones</c> finds it.</summary>
    public sealed class TownsfolkLookTests
    {
        static string[] PeopledZones=>ContentRegistry.With(nameof(NoTwoTownsfolkInOneFrameWearOneLook),new[]{"Hub_Decks","TallowDrift","Lanternwick","SunkenRelay"},ContentRegistry.OnFoot);
        [UnitySetUp] public IEnumerator Boot()
        {
            GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.5f);
            DevLoadout.Apply("starter");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {GameTime.Reset();SceneManager.LoadScene("_Boot");yield return null;yield return new WaitForSecondsRealtime(.3f);}
        static T[] All<T>()where T:Object=>Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        static IEnumerator Zone(string zone)
        {
            SceneFlow.Current.LoadZone(zone,"Arrival");float deadline=Time.unscaledTime+25;
            while(SceneFlow.Current.Loading&&Time.unscaledTime<deadline)yield return null;
            Assert.IsFalse(SceneFlow.Current.Loading,zone+" never finished loading");
            foreach(var enemy in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))enemy.Passive=true;
            foreach(var actor in PartyController.Current.members){actor.GetComponent<PlayerBrain>().AutoPilot=true;actor.GetComponent<PartnerBrain>().enabled=false;}
            yield return new WaitForSecondsRealtime(.4f);
        }

        // ---------------------------------------------------------------- who is seen, and as what
        /// <summary>One ordinary townsperson as a player sees them (<see cref="TownsfolkLook"/>: the template, the painted clothes and head, what is carried and worn, the
        /// build), every place on the plan it can stand (an errand's whole loop), and what the story asks of its being there (flags it needs set, flags it is gone at).</summary>
        sealed class Seen
        {
            public TownsfolkLook.Look look;public GameObject who=>look.who;public float tall=>look.tall;public Vector3[] at;public readonly HashSet<string> needs=new(),never=new();
            public string Name=>look.ToString();
        }
        static GameObject[] Residents()=>All<PropTint>().Select(t=>t.gameObject).Where(TownsfolkLook.Resident).ToArray();
        static Seen Read(GameObject who,Dictionary<GameObject,List<(string flag,bool set)>> gates)
        {
            var look=TownsfolkLook.Read(who);if(look==null)return null;var seen=new Seen{look=look};
            var walk=who.GetComponent<WalkLoop>();
            if(walk!=null&&walk.path!=null&&walk.path.Length>1)
            {
                var points=new List<Vector3>();int legs=walk.path.Length-(walk.thereAndBack?1:0);
                for(int k=0;k<legs;k++){Vector3 a=walk.path[k],b=walk.path[(k+1)%walk.path.Length];int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)));for(int s=0;s<steps;s++)points.Add(Vector3.Lerp(a,b,s/(float)steps));}
                points.Add(walk.path[^1]);seen.at=points.ToArray();
            }
            else seen.at=new[]{who.transform.position};
            for(var t=who.transform;t!=null;t=t.parent)if(gates.TryGetValue(t.gameObject,out var says))foreach(var (flag,set) in says)(set?seen.needs:seen.never).Add(flag);
            return seen;
        }
        /// <summary>What the story says about who is where: each Whereabouts and FlagGate target with the flags it needs set and the flags it is gone at.</summary>
        static Dictionary<GameObject,List<(string flag,bool set)>> Gates()
        {
            var gates=new Dictionary<GameObject,List<(string,bool)>>();
            void Say(GameObject target,string flag,bool set){if(target==null||string.IsNullOrEmpty(flag))return;if(!gates.TryGetValue(target,out var list))gates[target]=list=new();list.Add((flag,set));}
            foreach(var w in All<Whereabouts>())foreach(var t in w.targets??new GameObject[0]){Say(t,w.from,true);Say(t,w.goneAt,false);if(t!=null&&!gates.ContainsKey(t))gates[t]=new();}
            foreach(var g in All<FlagGate>())foreach(var t in g.targets??new GameObject[0])Say(t,g.flag,g.showWhenSet);
            return gates;
        }
        /// <summary>Never shown: hidden in the saved scene with nothing in the story to bring it (or an ancestor) back.</summary>
        static bool NeverShown(GameObject who,Dictionary<GameObject,List<(string flag,bool set)>> gates)
        {for(var t=who.transform;t!=null;t=t.parent)if(!t.gameObject.activeSelf&&!gates.ContainsKey(t.gameObject))return true;return false;}
        static bool Together(Seen a,Seen b)=>!a.needs.Overlaps(b.never)&&!b.needs.Overlaps(a.never);

        // ---------------------------------------------------------------- the frames a player is shown
        /// <summary>Where the hero stands, every three metres of every leg a replay route walks in this zone, from the nearest spawn to its first point (as TownLife reads them).</summary>
        static List<Vector3> Stands(string zone)
        {
            var stands=new List<Vector3>();string root=System.IO.Path.Combine(Application.dataPath,"..","..","docs","quality","routes");
            var spawns=All<SpawnPoint>().Select(s=>s.transform.position).ToArray();
            foreach(var file in System.IO.Directory.GetFiles(root,"*.json").OrderBy(f=>f))
            {
                QualityRoute route;try{route=JsonUtility.FromJson<QualityRoute>(System.IO.File.ReadAllText(file));}catch{continue;}
                if(route?.steps==null)continue;string current=route.scene;Vector3? last=null;
                foreach(var step in route.steps)
                {
                    string scene=string.IsNullOrEmpty(step.expectedScene)?current:step.expectedScene;
                    if(scene!=current){current=scene;last=null;}
                    if(scene!=zone){last=null;continue;}
                    if(!step.navigate||step.approachTarget||step.approachPartner)continue;
                    var p=step.point;var from=last??(spawns.Length>0?spawns.OrderBy(s=>Vector3.Distance(s,p)).First():p);
                    int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(from,p)/3));for(int s=0;s<=steps;s++)stands.Add(Vector3.Lerp(from,p,s/(float)steps));
                    last=p;
                }
            }
            // Thinned to one stand per metre and a half: a leg walked by ten routes is one walk to a player.
            var kept=new List<Vector3>();foreach(var p in stands)if(!kept.Any(k=>(k-p).sqrMagnitude<2.25f))kept.Add(p);
            return kept;
        }
        /// <summary>The play camera with the hero's feet at the middle of its view, as CameraRig poses it at its profile's distance: whether a body standing here is in the frame.</summary>
        sealed class Frame
        {
            readonly Vector3 eye;readonly Quaternion inverse;readonly float tan,aspect;public readonly Vector3 hero;
            public Frame(Vector3 hero,CameraProfile profile)
            {this.hero=hero;var rotation=Quaternion.Euler(profile.pitch,profile.yaw,0);eye=hero-rotation*Vector3.forward*profile.distance;inverse=Quaternion.Inverse(rotation);tan=Mathf.Tan(profile.fov*.5f*Mathf.Deg2Rad);aspect=16f/9;}
            public bool Holds(Vector3 feet,float tall)
            {
                var p=inverse*(feet+Vector3.up*1.1f*tall-eye);if(p.z<.5f)return false;
                return Mathf.Abs(p.x/(p.z*tan*aspect))<=1&&Mathf.Abs(p.y/(p.z*tan))<=1;
            }
        }
        /// <summary>Every pair of copies seen together in any one frame, and the most people in one frame who have a copy beside them.</summary>
        static (Dictionary<(int,int),string> pairs,int worst) Measure(IReadOnlyList<Seen> folk,IEnumerable<Frame> frames)
        {
            var pairs=new Dictionary<(int,int),string>();int worst=0;
            foreach(var frame in frames)
            {
                var here=folk.Where(s=>s.at.Any(p=>frame.Holds(p,s.tall))).ToArray();var copied=new HashSet<Seen>();
                for(int a=0;a<here.Length;a++)for(int b=a+1;b<here.Length;b++)
                {
                    if(!Together(here[a],here[b])||!TownsfolkLook.Copies(here[a].look,here[b].look,out string differ))continue;copied.Add(here[a]);copied.Add(here[b]);
                    int x=here[a].who.GetInstanceID(),y=here[b].who.GetInstanceID();var key=x<y?(x,y):(y,x);
                    if(!pairs.ContainsKey(key))pairs[key]=$"{here[a].Name} and {here[b].Name}: one {here[a].look.template} differing only in {differ}";
                }
                worst=Mathf.Max(worst,copied.Count);
            }
            return (pairs,worst);
        }

        /// <summary>Everyone as their builder left them, before the townsfolk pass dressed them (what each one's TownsfolkDress says they were given, and every piece the pass
        /// put on them taken off): the town as it was played on November 20. Returns what puts them back.</summary>
        static List<System.Action> AsBuilt()
        {
            var undo=new List<System.Action>();
            foreach(var dress in All<TownsfolkDress>())
            {
                var body=TownsfolkLook.Body(dress.gameObject);var model=dress.transform.Find(TownsfolkLook.Model);if(body==null||model==null)continue;
                var material=body.sharedMaterial;var scale=model.localScale;
                if(dress.built!=null)body.sharedMaterial=dress.built;model.localScale=dress.builtScale;
                foreach(var piece in dress.added)if(piece!=null){bool was=piece.activeSelf;piece.SetActive(false);undo.Add(()=>piece.SetActive(was));}
                undo.Add(()=>{body.sharedMaterial=material;model.localScale=scale;});
            }
            return undo;
        }
        [UnityTest] public IEnumerator NoTwoTownsfolkInOneFrameWearOneLook([ValueSource(nameof(PeopledZones))] string zone)
        {
            yield return Zone(zone);
            var gates=Gates();
            List<Seen> Folk()=>Residents().Where(r=>!NeverShown(r,gates)).Select(r=>Read(r,gates)).Where(s=>s!=null).ToList();
            var folk=Folk();
            if(folk.Count==0){TestContext.WriteLine($"TOWNSFOLK_LOOKS {zone} residents=0");yield break;}
            var rig=Object.FindFirstObjectByType<CameraRig>();Assert.IsNotNull(rig!=null?rig.profile:null,"fixture: the zone's camera has a profile");
            // Where the routes walk, and where a player who walks up to anyone stands: a town is walked all over, and a fight roams off its route's points (a route's
            // walk to a fight or a person has no point of its own, so the foundry's smiths, twenty metres off the colossus route's last point, were in no frame of it).
            var routes=Stands(zone);var stands=routes.Concat(folk.Select(s=>s.at[0])).Concat(All<Npc>().Select(n=>n.transform.position)).ToList();
            string where=$"{routes.Count} stands along the routes and one at each resident and each named person";
            var frames=stands.Select(p=>new Frame(p,rig.profile)).ToArray();
            // The check can fail: a resident a frame holds, stood beside a copy of itself a metre and a half nearer that frame's middle, is caught.
            var first=folk.FirstOrDefault(s=>!s.look.uniform&&Together(s,s)&&frames.Any(f=>f.Holds(s.at[0],s.tall)));
            if(first!=null)
            {
                var frame=frames.First(f=>f.Holds(first.at[0],first.tall));var toward=frame.hero-first.at[0];toward.y=0;
                var at=first.at[0]+(toward.sqrMagnitude>.01f?toward.normalized:first.who.transform.right)*1.5f;
                var twin=Object.Instantiate(first.who,at,first.who.transform.rotation,first.who.transform.parent);twin.name=first.who.name+" (control)";
                var copy=Read(twin,gates);foreach(var flag in first.needs)copy.needs.Add(flag);foreach(var flag in first.never)copy.never.Add(flag);
                var control=Measure(new[]{first,copy},new[]{frame});
                bool copies=TownsfolkLook.Copies(first.look,copy.look,out string differ);
                Object.Destroy(twin);
                Assert.IsTrue(copies,$"control: {first.Name} and a copy of it are not copies ({differ} differ)");
                Assert.AreEqual(1,control.pairs.Count,$"control: {first.Name} beside a copy of itself in one frame passes (in frame: {frame.Holds(copy.at[0],copy.tall)})");
            }
            // The town as built, for the count before the pass: the same frames, everyone in what their builder gave them.
            var undo=AsBuilt();int dressed=All<TownsfolkDress>().Length;var (builtPairs,builtWorst)=Measure(Folk(),frames);foreach(var u in undo)u();
            var (pairs,worst)=Measure(folk,frames);
            var byBody=folk.GroupBy(s=>s.look.template).OrderByDescending(g=>g.Count()).Select(g=>$"{g.Key} x{g.Count()} in {g.Select(s=>s.look.dye).Distinct().Count()} paintings{(g.Any(s=>s.look.uniform)?" (uniforms)":"")}");
            TestContext.WriteLine($"TOWNSFOLK_LOOKS {zone} residents={folk.Count} frames={frames.Length} copies={pairs.Count} worst={worst} as-built={builtPairs.Count} worst-as-built={builtWorst} dressed={dressed} ({where}); bodies: {string.Join(", ",byBody)}");
            Assert.IsEmpty(pairs,$"{zone}: {pairs.Count} pairs of townsfolk read as copies in one frame of the play camera (up to {worst} people in one frame have a copy beside them; {folk.Count} residents, {where}):\n"+
                string.Join("\n",pairs.Values.Take(25)));
        }
    }
}
