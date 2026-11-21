using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Lattice.Core;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace Lattice.EditorTools
{
    /// <summary>
    /// Every staging step in the project, in one run and in the one order they need. Scenes are build output, and chapters reach into each other's scenes afterwards
    /// (a warp on the Tallow approach, a chart talk and a warp on Many Hands, chapter 1's whole staging order): a rebuild of the earlier scene drops every later
    /// chapter's piece until someone remembers to stage it again. So each staging entry point carries a <see cref="StepAttribute"/> in its own file, naming the scenes
    /// it changes, the steps it runs after and what it leaves in each scene, and this runs them all:
    /// <c>exec-when-green.ps1 -Method Lattice.EditorTools.Restage.All -Marker RESTAGE_OK -Graphics -TimeoutSec 3000 [-ExtraArgs @('-restage-scenes','ManyHands,TallowApproach')]</c>.
    /// <para>With <c>-restage-scenes a,b</c> only the steps that change those scenes run, still in order. A step that can be held to some of its scenes (its
    /// <see cref="StepAttribute.ScenesArg"/>) is given only those; one that can't changes all of its scenes, and they join the run, so every later step for them runs
    /// too (ChapterOneStaging leans Sorrel's rig and QaNov10Staging leans it further: the first alone would undo the second).</para>
    /// <para>It states each scene's fingerprint (objects, a hash of its hierarchy, its navigation) before and after, what a step declares and the scene lacked before,
    /// and refuses, with no marker, when a step fails, a declared piece is still missing afterwards, or a step saved a scene it doesn't declare.
    /// RestageTests loads each built scene in the player's way and fails by name on anything a step declares for it that it lacks.</para>
    /// </summary>
    public static class Restage
    {
        /// <summary>On a staging entry point (static, no parameters; it may be a <c>BatchTools.Run</c> one, which runs here without quitting Unity). One per step:
        /// a method run twice in the order (chapter 1's staging, before and after its dressing) carries two.
        /// <code>[Restage.Step("chapter9.tallow-warp","TallowApproach",After="chapter5.tallow-dock",Chapter=9,Leaves=new[]{"TallowApproach: Warp — The Ninth <WarpBeacon> #1"})]</code></summary>
        [AttributeUsage(AttributeTargets.Method,AllowMultiple=true)]
        public sealed class StepAttribute:Attribute
        {
            public StepAttribute(string id,string scenes){Id=id;Scenes=scenes;}
            /// <summary>A name for the step that other steps' <see cref="After"/> use: "chapter6.many-hands".</summary>
            public string Id{get;}
            /// <summary>Every scene it opens and saves, comma-separated.</summary>
            public string Scenes{get;}
            /// <summary>The ids of the steps it must follow, comma-separated: what it reads or replaces has to be in place first.</summary>
            public string After{get;set;}="";
            /// <summary>What it leaves, each <c>"Scene: object name"</c>, then optionally <c>" &lt;Component&gt;"</c> (the object has a component of that type)
            /// and <c>" #n"</c> (exactly n such objects, for a piece that must not pile up on a second run). Searched at any depth, active or not.</summary>
            public string[] Leaves{get;set;}=Array.Empty<string>();
            /// <summary>A static <c>string[] (Scene)</c> method that names what is wrong with a scene it changed (a check names cannot express): on the step's own
            /// class, or on Restage.</summary>
            public string Check{get;set;}="";
            /// <summary>The argument that holds the step to some of its scenes (<c>-qa-zones</c>): given the asked scenes when they are fewer than its own.</summary>
            public string ScenesArg{get;set;}="";
            /// <summary>Arguments it always takes, as on its command line: <c>"-dress-final"</c>.</summary>
            public string Args{get;set;}="";
            /// <summary>Runs only on a run where something it leaves is missing: for a step that isn't safe to run twice.</summary>
            public bool OnlyWhereMissing{get;set;}
            /// <summary>A file or folder (from the project folder) it needs; without it the step is passed over and says so.</summary>
            public string Needs{get;set;}="";
            /// <summary>Only orders steps no <see cref="After"/> orders, so the run reads chapter by chapter.</summary>
            public int Chapter{get;set;}
            /// <summary>The scenes this step builds its own scenes from, whole (comma-separated): a scene made from another, such as DecksLeaving from Hub_Decks. When one
            /// of them is restaged, this step runs, after every step that changes it, and its scenes are made again and join the run, so every step that stages into
            /// them runs after it too. Give it the builder's entry point, with <see cref="ScenesArg"/> set if that entry point builds other zones as well.</summary>
            public string From{get;set;}="";
        }

        // ---------------------------------------------------------------- what is registered
        sealed class Piece{public string scene,name,type,text;public int count=-1;}
        sealed class Step
        {
            public StepAttribute a;public MethodInfo method,check;public string id;public string[] scenes,after,from;public Piece[] leaves;
            public string Name=>method.DeclaringType.Name+"."+method.Name;
        }
        static List<Step> gathered;
        static string[] List(string text)=>(text??"").Split(',').Select(s=>s.Trim()).Where(s=>s.Length>0).ToArray();
        static string ScenePath(string scene)=>"Assets/_Project/Scenes/"+scene+".unity";

        static Piece Parse(Step step,string entry)
        {
            int colon=entry.IndexOf(": ",StringComparison.Ordinal);
            if(colon<=0)throw new InvalidOperationException($"{step.Name} [{step.id}] leaves '{entry}': write it as \"Scene: object name\"");
            var piece=new Piece{scene=entry.Substring(0,colon).Trim(),text=entry.Substring(colon+2).Trim()};
            string rest=piece.text;
            int hash=rest.LastIndexOf(" #",StringComparison.Ordinal);
            if(hash>0&&int.TryParse(rest.Substring(hash+2),NumberStyles.None,CultureInfo.InvariantCulture,out int count)){piece.count=count;rest=rest.Substring(0,hash).TrimEnd();}
            int open=rest.LastIndexOf(" <",StringComparison.Ordinal);
            if(rest.EndsWith(">")&&open>0){piece.type=rest.Substring(open+2,rest.Length-open-3);rest=rest.Substring(0,open).TrimEnd();}
            piece.name=rest;
            if(piece.name.Length==0)throw new InvalidOperationException($"{step.Name} [{step.id}] leaves '{entry}' with no object name");
            if(!step.scenes.Contains(piece.scene))throw new InvalidOperationException($"{step.Name} [{step.id}] leaves '{entry}' in {piece.scene}, a scene it doesn't name in its scenes ({step.a.Scenes})");
            return piece;
        }

        /// <summary>Every registered step, in run order: each after what it names in <see cref="StepAttribute.After"/>, and otherwise by chapter, then id.</summary>
        static List<Step> Gather()
        {
            if(gathered!=null)return gathered;
            var steps=new List<Step>();
            foreach(var method in TypeCache.GetMethodsWithAttribute<StepAttribute>())
            {
                if(!method.IsStatic||method.GetParameters().Length!=0)throw new InvalidOperationException(method.DeclaringType.Name+"."+method.Name+" is marked [Restage.Step] and must be static with no parameters");
                foreach(var a in method.GetCustomAttributes<StepAttribute>())
                {
                    var step=new Step{a=a,method=method,id=a.Id,scenes=List(a.Scenes),after=List(a.After),from=List(a.From)};
                    if(string.IsNullOrWhiteSpace(step.id))throw new InvalidOperationException(step.Name+" has a [Restage.Step] with no id");
                    if(step.scenes.Length==0)throw new InvalidOperationException($"{step.Name} [{step.id}] names no scenes");
                    step.leaves=(a.Leaves??Array.Empty<string>()).Select(e=>Parse(step,e)).ToArray();
                    if(!string.IsNullOrEmpty(a.Check))
                    {
                        const BindingFlags any=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
                        step.check=method.DeclaringType.GetMethod(a.Check,any,null,new[]{typeof(Scene)},null)??typeof(Restage).GetMethod(a.Check,any,null,new[]{typeof(Scene)},null);
                        if(step.check==null||step.check.ReturnType!=typeof(string[]))throw new InvalidOperationException($"{step.Name} [{step.id}] names a check {a.Check}, which isn't a static string[] {a.Check}(Scene) on {method.DeclaringType.Name} or Restage");
                    }
                    steps.Add(step);
                }
            }
            foreach(var twice in steps.GroupBy(s=>s.id).Where(g=>g.Count()>1))throw new InvalidOperationException("Two steps are both called "+twice.Key+": "+string.Join(", ",twice.Select(s=>s.Name)));
            var byId=steps.ToDictionary(s=>s.id);
            foreach(var step in steps)foreach(var first in step.after)if(!byId.ContainsKey(first))throw new InvalidOperationException($"{step.Name} [{step.id}] runs after {first}, which no step is called");
            // A scene made from another is made after every step that changes its source, and before every other step that stages into it.
            var before=steps.ToDictionary(s=>s.id,s=>new HashSet<string>(s.after));
            foreach(var made in steps.Where(s=>s.from.Length>0))
            {
                foreach(var other in steps.Where(o=>o.from.Length>0&&o!=made&&o.scenes.Intersect(made.scenes).Any()))throw new InvalidOperationException($"{made.id} and {other.id} both make {string.Join(",",made.scenes.Intersect(other.scenes))} from another scene");
                foreach(var other in steps.Where(o=>o!=made))
                {
                    if(other.scenes.Intersect(made.from).Any())before[made.id].Add(other.id);
                    else if(other.scenes.Intersect(made.scenes).Any())before[other.id].Add(made.id);
                }
            }
            // Kahn's order, taking the earliest chapter (then id) among the steps whose predecessors have all run.
            var order=new List<Step>();var done=new HashSet<string>();var waiting=steps.ToList();
            while(waiting.Count>0)
            {
                var next=waiting.Where(s=>before[s.id].All(done.Contains)).OrderBy(s=>s.a.Chapter).ThenBy(s=>s.id,StringComparer.Ordinal).FirstOrDefault();
                if(next==null)throw new InvalidOperationException("The steps' order makes a loop: "+string.Join(", ",waiting.Select(s=>s.id+" after "+string.Join("+",before[s.id]))));
                order.Add(next);done.Add(next.id);waiting.Remove(next);
            }
            return gathered=order;
        }

        // ---------------------------------------------------------------- what a scene should carry (also read by RestageTests, in play mode)
        /// <summary>Every scene some step leaves something in, or checks: RestageTests' cases.</summary>
        public static string[] StagedScenes()=>Gather().SelectMany(s=>s.leaves.Select(p=>p.scene).Concat(s.check!=null?s.scenes:Array.Empty<string>())).Distinct().OrderBy(s=>s,StringComparer.Ordinal).ToArray();
        /// <summary>The steps that leave something in a scene, in run order, for a message.</summary>
        public static string[] StepsFor(string scene)=>Gather().Where(s=>s.leaves.Any(p=>p.scene==scene)||s.check!=null&&s.scenes.Contains(scene)).Select(s=>s.id+" ("+s.Name+")").ToArray();
        /// <summary>What a loaded scene lacks of what every step declares for it, one line per piece, naming the step: empty when it carries everything.</summary>
        public static string[] Missing(Scene scene)
        {
            var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Select(t=>t.gameObject).ToArray();
            var problems=new List<string>();
            foreach(var step in Gather())
            {
                foreach(var piece in step.leaves.Where(p=>p.scene==scene.name))
                {
                    var named=all.Where(g=>g.name==piece.name).ToArray();
                    var fitting=piece.type==null?named:named.Where(g=>g.GetComponents<Component>().Any(c=>c!=null&&c.GetType().Name==piece.type)).ToArray();
                    bool whole=piece.count<0?fitting.Length>0:fitting.Length==piece.count;
                    if(whole)continue;
                    string found=fitting.Length==0?"none":fitting.Length+(piece.count>=0?" (one run too many?)":"");
                    if(piece.type!=null&&named.Length>fitting.Length)found+=$", and {named.Length-fitting.Length} so named with no {piece.type}";
                    problems.Add($"{scene.name}: {piece.text} (found {found}), left by {step.id} ({step.Name})");
                }
                if(step.check!=null&&step.scenes.Contains(scene.name))
                    foreach(var problem in (string[])step.check.Invoke(null,new object[]{scene})??Array.Empty<string>())problems.Add($"{scene.name}: {problem}, checked for {step.id} ({step.Name})");
            }
            return problems.ToArray();
        }

        /// <summary>LocalBiomeBake's check: on a machine with the licensed packs, every biome patch has its local art, baked from the sprouts it has now (one source
        /// blade mesh per sprout, so every grass patch's mesh has the same number of vertices per sprout). A patch re-sown without a bake has stale or no art.</summary>
        public static string[] LocalBiomesBaked(Scene scene)
        {
            if(!Directory.Exists("Packages/xyz.staggart-creations.stylized-grass"))return Array.Empty<string>();
            var problems=new List<string>();var perSprout=new Dictionary<int,string>();
            foreach(var patch in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<BiomePatch>(true)))
            {
                var art=Resources.Load<BiomeVisualAsset>("LocalBiomes/"+patch.resourceKey);
                if(art==null||art.mesh==null||art.material==null){problems.Add("biome patch "+patch.resourceKey+" has no local art baked");continue;}
                if(patch.water||patch.sprouts==null||patch.sprouts.Length==0)continue;
                if(art.mesh.vertexCount%patch.sprouts.Length!=0){problems.Add($"biome patch {patch.resourceKey}'s local art has {art.mesh.vertexCount} vertices for {patch.sprouts.Length} sprouts: baked before it was re-sown");continue;}
                perSprout[art.mesh.vertexCount/patch.sprouts.Length]=patch.resourceKey;
            }
            if(perSprout.Count>1)problems.Add("biome patches' local art was baked from different sprout counts ("+string.Join(", ",perSprout.Select(p=>p.Value+": "+p.Key+" per sprout"))+"): some were re-sown after the bake");
            return problems.ToArray();
        }

        /// <summary>TownLife's check: its own audit (TownLife.Audit), read off the built scene with the residents and errands it stages there and every route as it now
        /// stands, with everyone standing whatever beat they belong to, as Apply audits them. A route laid since through a resident's spot fails here, and by name.</summary>
        public static string[] TownLifeHolds(Scene scene)
        {
            var (folk,errands,ways)=scene.name switch{"Hub_Decks"=>(TownLife.DecksFolk,TownLife.DecksErrands,"DecksWays"),"TallowDrift"=>(TownLife.TallowFolk,TownLife.TallowErrands,"TallowWays"),_=>(null,null,null)};
            var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name==TownLife.RootName);
            // No town here, or none built yet (the step's leaves say so).
            if(folk==null||root==null)return Array.Empty<string>();
            const BindingFlags any=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
            var routes=(IEnumerable<TownLife.Way>)typeof(TownLife).GetMethod(ways,any,null,Type.EmptyTypes,null).Invoke(null,null);
            var probes=((Vector2 a,Vector2 b)[])typeof(TownLife).GetMethod("Probes",any,null,new[]{typeof(string)},null).Invoke(null,new object[]{scene.name});
            var hidden=root.GetComponentsInChildren<Transform>(true).Where(t=>t.parent==root.transform&&!t.gameObject.activeSelf).Select(t=>t.gameObject).ToArray();
            foreach(var h in hidden)h.SetActive(true);
            try
            {
                return TownLife.Audit(scene.name,folk.Select(f=>(f.name,f.body,new Vector3(f.at.x,0,f.at.y),f.build,f.from,f.goneAt,f.door)).ToArray(),
                    errands.Select(e=>(e.name,e.body,e.loop,e.back,e.build,e.carries!=null,e.from,e.goneAt)).ToArray(),routes,probes).Distinct().Select(f=>"town life: "+f).ToArray();
            }
            finally{foreach(var h in hidden)if(h!=null)h.SetActive(false);}
        }

        // ---------------------------------------------------------------- a scene's fingerprint
        sealed class Print{public int objects;public string hash,nav;public List<string> lines;public string[] missing;}
        static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
        static string F(float v)=>v.ToString("F3",Inv);
        static string F(Vector3 v)=>F(v.x)+","+F(v.y)+","+F(v.z);
        static string F(Quaternion q){if(q.w<0)q=new Quaternion(-q.x,-q.y,-q.z,-q.w);return q.x.ToString("F4",Inv)+","+q.y.ToString("F4",Inv)+","+q.z.ToString("F4",Inv)+","+q.w.ToString("F4",Inv);}
        static string F(Color c)=>F(c.r)+","+F(c.g)+","+F(c.b)+","+F(c.a);
        static string Hash(IEnumerable<string> lines)
        {
            using var sha=SHA256.Create();var bytes=sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n",lines)));
            return string.Concat(bytes.Take(6).Select(b=>b.ToString("x2")));
        }
        static string PathOf(Transform t){var names=new List<string>();for(;t!=null;t=t.parent)names.Add(t.name);names.Reverse();return string.Join("/",names);}
        /// <summary>One line per object: where it is in the hierarchy, whether it's on, its local placement, and each component with what most often moves (a mesh's
        /// size, a renderer's materials, a sign's words, a light's colour and reach). Sorted, so a root destroyed and made again in the same place reads the same.</summary>
        static string Line(Transform t)
        {
            var sb=new StringBuilder(PathOf(t)).Append('|').Append(t.gameObject.activeSelf?'1':'0').Append('|').Append(F(t.localPosition)).Append(' ').Append(F(t.localRotation)).Append(' ').Append(F(t.localScale));
            foreach(var c in t.GetComponents<Component>())
            {
                if(c==null){sb.Append("|(missing script)");continue;}
                if(c is Transform)continue;
                sb.Append('|').Append(c.GetType().Name);
                switch(c)
                {
                    case MeshFilter filter:var mesh=filter.sharedMesh;sb.Append(mesh==null?":none":":"+mesh.name+"/"+mesh.vertexCount+"/"+F(mesh.bounds.center)+"/"+F(mesh.bounds.size));break;
                    case Renderer view:sb.Append(view.enabled?":":":off:").Append(string.Join(",",view.sharedMaterials.Select(m=>m==null?"none":m.name)));break;
                    case TMPro.TMP_Text text:sb.Append(':').Append(text.text);break;
                    case Light light:sb.Append(':').Append(light.type).Append('/').Append(F(light.color)).Append('/').Append(F(light.intensity)).Append('/').Append(F(light.range));break;
                    case Behaviour behaviour when !behaviour.enabled:sb.Append(":off");break;
                    case Collider collider when !collider.enabled:sb.Append(":off");break;
                }
            }
            return sb.ToString();
        }
        /// <summary>The walkable surface its GroundNavigation carries, triangulated with every carving obstacle off (carving is applied as the data is added, a
        /// frame later or not, so it would make the same data read differently).</summary>
        static string Navigation(Scene scene)
        {
            var navs=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Lattice.Combat.GroundNavigation>(true)).Where(n=>n.data!=null).ToArray();
            if(navs.Length==0)return "none";
            // Nothing else is running between steps: whatever a step left loaded would be counted as this scene's.
            NavMesh.RemoveAllNavMeshData();
            foreach(var obstacle in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<NavMeshObstacle>(true)))obstacle.enabled=false;
            var parts=new List<string>();
            foreach(var nav in navs)
            {
                var instance=NavMesh.AddNavMeshData(nav.data);
                try
                {
                    var tri=NavMesh.CalculateTriangulation();
                    parts.Add(tri.vertices.Length+"v/"+tri.indices.Length/3+"t/"+Hash(tri.vertices.Select(v=>v.x.ToString("F2",Inv)+","+v.y.ToString("F2",Inv)+","+v.z.ToString("F2",Inv)).Concat(tri.indices.Select(i=>i.ToString(Inv)))));
                }
                finally{instance.Remove();}
            }
            return string.Join("+",parts);
        }
        /// <summary>Opens the scene, reads it, and leaves it unsaved: reading turns carving off.</summary>
        static Print Fingerprint(string name)
        {
            var scene=EditorSceneManager.OpenScene(ScenePath(name),OpenSceneMode.Single);
            var lines=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Select(Line).ToList();
            lines.Sort(StringComparer.Ordinal);
            var missing=Missing(scene);
            return new Print{objects=lines.Count,lines=lines,hash=Hash(lines),missing=missing,nav=Navigation(scene)};
        }
        static string Short(string line)=>line.Length<=240?line:line.Substring(0,240)+"...";
        /// <summary>What a log line may say: exec.ps1 stops a run at the first "FAILED:" or "error CS0" it reads, in any case (PowerShell's -match), and a step's own
        /// words may contain one. A run of November 12 was stopped half-way by "which failed: it runs".</summary>
        static string Quiet(string text)=>System.Text.RegularExpressions.Regex.Replace(System.Text.RegularExpressions.Regex.Replace(text??"","(?i)failed:","failed -"),"(?i)error cs(\\d)","error C-S$1").Replace("\r","").Replace("\n"," / ");

        // ---------------------------------------------------------------- the run
        /// <summary>Runs every step, or those for <c>-restage-scenes a,b</c>, in order; marker <c>RESTAGE_OK</c>. See the class.</summary>
        public static void All()=>BatchTools.Run(()=>
        {
            gathered=null;var steps=Gather();
            var known=steps.SelectMany(s=>s.scenes.Concat(s.from)).Distinct().ToList();
            var derived=steps.Where(s=>s.from.Length>0).SelectMany(s=>s.scenes).ToHashSet();
            string asked=DevArgs.Value("-restage-scenes");
            var start=new HashSet<string>(string.IsNullOrEmpty(asked)?known:List(asked));
            foreach(var scene in start)if(!File.Exists(ScenePath(scene))&&!derived.Contains(scene))
            {
                if(!string.IsNullOrEmpty(asked))throw new InvalidOperationException("-restage-scenes names "+scene+", which has no built scene");
            }
            foreach(var scene in start.Where(s=>!known.Contains(s)))Debug.Log("RESTAGE_NOTHING_REACHES "+scene+": no step stages into it, so a rebuild of it needs nothing more");
            Debug.Log("RESTAGE_ORDER "+string.Join(" > ",steps.Select(s=>s.id)));

            // Every scene the run will change, worked out before anything runs and read before anything changes: a step that can't be held to some of its scenes
            // brings all of them in, and so does a scene made from one in the run, until nothing more joins. Taken as the steps run, a scene brought in by a later
            // step missed the earlier ones: restaging Harrowgate brought Many Hands in by chapter 9's step, after chapters 6 and 7 had been passed (November 13).
            var reach=new HashSet<string>(start);
            for(bool grew=true;grew;)
            {
                grew=false;
                foreach(var step in steps)
                    if(step.from.Any(reach.Contains)||step.scenes.Any(reach.Contains)&&string.IsNullOrEmpty(step.a.ScenesArg))
                        foreach(var scene in step.scenes)grew|=reach.Add(scene);
            }
            if(reach.Count>start.Count)Debug.Log("RESTAGE_JOINED "+string.Join(",",reach.Except(start).OrderBy(s=>s,StringComparer.Ordinal))+": a step that restages them all reaches them");
            var before=new Dictionary<string,Print>();
            foreach(var scene in reach.Where(s=>known.Contains(s)).OrderBy(s=>s,StringComparer.Ordinal))
            {
                if(!File.Exists(ScenePath(scene))){Debug.LogWarning("RESTAGE_UNBUILT "+scene+": a step stages into it and it isn't built yet");continue;}
                var print=before[scene]=Fingerprint(scene);
                Debug.Log($"RESTAGE_BEFORE {scene} objects={print.objects} hash={print.hash} nav={print.nav} missing={print.missing.Length}");
                foreach(var line in print.missing)Debug.Log("RESTAGE_MISSING_BEFORE "+Quiet(line));
            }

            var set=new HashSet<string>(reach);var problems=new List<string>();var failed=new HashSet<string>();var ran=new List<string>();
            foreach(var step in steps)
            {
                // A scene made from another is made again whenever its source is in the run.
                bool remade=step.from.Any(set.Contains);
                var chosen=remade?step.scenes:step.scenes.Where(set.Contains).ToArray();
                if(chosen.Length==0)continue;
                // A scene made from another needs its source built, not itself: making it is the step.
                var sourceless=step.from.Where(s=>!File.Exists(ScenePath(s))).ToArray();
                if(sourceless.Length>0){Debug.LogWarning($"RESTAGE_STEP_PASSED {step.id}: it is made from {string.Join(",",sourceless)}, which isn't built yet");continue;}
                // Its first build is its own builder's (asked for by name, a restage makes it too); after that the restage keeps it in step with its source.
                bool askedFor(string s)=>!string.IsNullOrEmpty(asked)&&start.Contains(s);
                var built=chosen.Where(s=>File.Exists(ScenePath(s))||step.from.Length>0&&askedFor(s)).ToArray();
                if(built.Length==0){Debug.Log($"RESTAGE_STEP_PASSED {step.id}: none of {string.Join(",",chosen)} is built yet");continue;}
                // A step that fails leaves each of its scenes as it was or as it meant to (every step saves a scene only once its work there is done), so what
                // follows it still runs: on a scene it left as it was, that is the same restage that scene had before.
                var waitsOn=step.after.Where(failed.Contains).ToArray();
                if(waitsOn.Length>0)Debug.LogWarning(Quiet("RESTAGE_STEP_AFTER_PROBLEM "+step.id+" follows "+string.Join(",",waitsOn)+", which had a problem; it runs on the scenes as they were left"));
                if(!string.IsNullOrEmpty(step.a.Needs)&&!File.Exists(step.a.Needs)&&!Directory.Exists(step.a.Needs)){Debug.LogWarning($"RESTAGE_STEP_PASSED {step.id}: it needs {step.a.Needs}, which isn't on this machine");continue;}
                bool held=!string.IsNullOrEmpty(step.a.ScenesArg);
                if(step.a.OnlyWhereMissing)
                {
                    // Read now, not from before the run: an earlier step may have rebuilt what this one dresses.
                    var lacking=built.Where(s=>Fingerprint(s).missing.Any(m=>m.Contains(", left by "+step.id+" ("))).ToArray();
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                    if(lacking.Length==0){Debug.Log($"RESTAGE_STEP_KEPT {step.id}: what it leaves is in {string.Join(",",built)} already, and it is a step that isn't run twice");continue;}
                    // Held to the scenes that lack it where it can be; otherwise it runs on all of its scenes, and says so.
                    if(held)built=lacking;
                    Debug.LogWarning($"RESTAGE_STEP_ONCE {step.id}: {string.Join(",",lacking)} lacks what it leaves, so it runs, on {(held?string.Join(",",lacking):"all of "+step.a.Scenes)}");
                }
                var given=new Dictionary<string,string>();
                var args=List(step.a.Args.Replace(' ',','));
                for(int i=0;i<args.Length;i++){bool valued=i+1<args.Length&&!args[i+1].StartsWith("-");given[args[i]]=valued?args[++i]:"";}
                // Always given, so an entry point that builds several zones (a region's BuildZones) builds only the step's.
                if(held)given[step.a.ScenesArg]=string.Join(",",built);
                if(!held||step.from.Length>0)set.UnionWith(held?built:step.scenes);
                var saved=new List<string>();void Saved(Scene s)=>saved.Add(s.name);
                var clock=System.Diagnostics.Stopwatch.StartNew();
                Debug.Log($"RESTAGE_STEP_START {step.id} {step.Name} scenes={(held?string.Join(",",built):step.a.Scenes)} args={string.Join(" ",given.Select(g=>g.Key+(g.Value.Length>0?" "+g.Value:"")))}");
                EditorSceneManager.sceneSaved+=Saved;DevArgs.Given=given;BatchTools.Nested=true;
                try{step.method.Invoke(null,null);ran.Add(step.id);}
                catch(Exception e)
                {
                    var inner=e is TargetInvocationException t&&t.InnerException!=null?t.InnerException:e;
                    failed.Add(step.id);problems.Add($"{step.id} ({step.Name}) threw {inner.GetType().Name}: {Quiet(inner.Message)}");
                    Debug.LogWarning("RESTAGE_STEP_PROBLEM "+step.id+": "+Quiet(inner.ToString()));
                }
                finally{BatchTools.Nested=false;DevArgs.Given=null;EditorSceneManager.sceneSaved-=Saved;}
                var undeclared=saved.Distinct().Where(s=>!step.scenes.Contains(s)).ToArray();
                if(undeclared.Length>0)problems.Add($"{step.id} ({step.Name}) saved {string.Join(", ",undeclared)}, which its [Restage.Step] doesn't name: add {(undeclared.Length==1?"it":"them")} to its scenes");
                Debug.Log($"RESTAGE_STEP {step.id} {(failed.Contains(step.id)?"failed":"ran")} in {clock.Elapsed.TotalSeconds:F0}s saved={string.Join(",",saved.Distinct())}");
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            }
            AssetDatabase.SaveAssets();

            // Every scene the run changed, read again.
            foreach(var scene in set.Where(s=>known.Contains(s)&&File.Exists(ScenePath(s))).OrderBy(s=>s,StringComparer.Ordinal))
            {
                var after=Fingerprint(scene);
                before.TryGetValue(scene,out var was);
                var gone=new List<string>();var come=new List<string>();
                if(was!=null)
                {
                    var counts=new Dictionary<string,int>(StringComparer.Ordinal);
                    foreach(var line in was.lines)counts[line]=counts.TryGetValue(line,out int n)?n+1:1;
                    foreach(var line in after.lines){if(counts.TryGetValue(line,out int n)&&n>0)counts[line]=n-1;else come.Add(line);}
                    gone=counts.Where(c=>c.Value>0).SelectMany(c=>Enumerable.Repeat(c.Key,c.Value)).ToList();
                }
                string same=was==null?"new":was.hash==after.hash&&was.nav==after.nav?"unchanged":"changed";
                Debug.Log($"RESTAGE_SCENE {scene} {same} objects {was?.objects.ToString()??"?"}>{after.objects} hash {was?.hash??"?"}>{after.hash} nav {was?.nav??"?"}>{after.nav} lines +{come.Count} -{gone.Count}");
                foreach(var line in gone.Take(30))Debug.Log($"RESTAGE_GONE {scene} {Quiet(Short(line))}");
                foreach(var line in come.Take(30))Debug.Log($"RESTAGE_CAME {scene} {Quiet(Short(line))}");
                if(was!=null)foreach(var line in was.missing.Where(m=>!after.missing.Contains(m)))Debug.Log("RESTAGE_PUT_BACK "+Quiet(line));
                foreach(var line in after.missing){problems.Add("still missing: "+line);Debug.LogWarning("RESTAGE_STILL_MISSING "+Quiet(line));}
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            if(problems.Count>0)throw new InvalidOperationException($"RESTAGE_INCOMPLETE {problems.Count}: "+string.Join(" | ",problems));
            Debug.Log($"RESTAGE_OK steps={ran.Count} scenes={set.Count(s=>known.Contains(s))}");
        });
    }
}
