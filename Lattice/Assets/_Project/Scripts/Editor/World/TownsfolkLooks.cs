using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Core;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace Lattice.EditorTools
{
    /// <summary>
    /// The townsfolk rule no builder can get wrong (November 21, after the whole-game capture's clones: s49/140, s50/131, s57/136, s24/022). Every scene under
    /// <c>Assets/_Project/Scenes</c> is dressed as it is saved, by whichever builder or staging step saves it: its ordinary townspeople (<see cref="TownsfolkLook.Resident"/>)
    /// are given looks so that no two who can stand in one frame of the play camera read as one figure twice (<see cref="TownsfolkLook.Copies"/>: of one template, at
    /// least three of clothes, head, what they carry and build differ). A builder still chooses who lives where and in whose body: the pass keeps that body's own painting
    /// where it can and reaches, in this order of cost, for one of the body's dyes (tools/townsfolk_dyes.py: its clothes and hair redyed on its own UVs), another painting of
    /// the same template the place already has, something on the head (a cap, a band, a scarf), something carried (a pack, a satchel, a bedroll, a coil, a parcel) and a
    /// build (eight percent taller or shorter, twelve broader or slighter). Nobody moves, and nothing solid is added: the routes walk the same floor.
    /// <para>What the builder gave is kept on each person (<see cref="TownsfolkDress"/>), so a scene saved again is dressed from it and two saves give the same people. Two
    /// people the story never has there together (one gone at the flag the other comes with) may share a look. Someone on an errand made taller or shorter walks at that height's pace (TownLife.Pace), so their soles still hold. A body in a force's colours is a uniform and is left as it is. A scene whose engineer is rebuilding it is <see cref="Held"/> until the lead lifts it.</para>
    /// <para>Run once after new dyes: <c>exec-when-green.ps1 -Method Lattice.EditorTools.TownsfolkLooks.Make -Marker TOWNSFOLK_LOOKS_MADE_OK</c>. Dress scenes without
    /// building them: <c>-Method Lattice.EditorTools.TownsfolkLooks.Dress -Marker TOWNSFOLK_LOOKS_OK -ExtraArgs @('-looks-zones','Harrowgate,Driftreed')</c>
    /// (<c>-looks-dry</c> reads and reports without saving; <c>-looks-held</c> dresses a held scene).</para>
    /// </summary>
    [InitializeOnLoad]
    public static class TownsfolkLooks
    {
        const string Folder="Assets/_Project/Art/Generated/Townsfolk",Worn=Folder+"/Worn",GoodsPath="Assets/_Project/Resources/WorldMaterials/townsfolk-goods.mat";
        /// <summary>Scenes another engineer is rebuilding (on November 21 the Sunken Relay, being relit): left as their builders leave them until the lead says.</summary>
        public static readonly HashSet<string> Held=new(){"SunkenRelay"};
        static readonly HashSet<string> lifted=new();
        static bool busy;
        static TownsfolkLooks(){EditorSceneManager.sceneSaving-=Saving;EditorSceneManager.sceneSaving+=Saving;}

        static void Saving(Scene scene,string path)
        {
            if(busy||string.IsNullOrEmpty(path))return;path=path.Replace('\\','/');if(!path.StartsWith("Assets/_Project/Scenes/"))return;
            string zone=Path.GetFileNameWithoutExtension(path);
            try
            {
                // Nothing until the dyes and pieces are made (Make): a scene saved before that keeps its builder's people.
                busy=true;if(AssetDatabase.LoadAssetAtPath<Material>(GoodsPath)==null)return;
                var residents=Residents(scene);
                if(Held.Contains(zone)&&!lifted.Contains(zone)){if(residents.Count>0)Debug.Log($"TOWNSFOLK_LOOKS_HELD {zone}: {residents.Count} residents left as built while its engineer rebuilds it");return;}
                if(residents.Count>0)Debug.Log(DressScene(zone,residents));
                // Then everyone seated, on a seat of their own height (Seating), fitted to the build they were just given.
                var seats=Seating.Fit(scene,zone);if(seats!=null)Debug.Log(seats);
            }
            catch(Exception e){Debug.LogWarning("TOWNSFOLK_LOOKS_FAILED "+zone+": "+e);}
            finally{busy=false;}
        }
        static List<GameObject> Residents(Scene scene)=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<PropTint>(true)).Select(t=>t.gameObject).Where(TownsfolkLook.Resident).Distinct().ToList();

        // ---------------------------------------------------------------- the choices
        static readonly string[] Heads={"","cap","band","scarf"},Carries={"","pack","satchel","roll","coil","parcel"};
        static readonly (float tall,float broad)[] Builds={(1,1),(1.08f,1),(.925f,1),(1,1.12f),(1,.89f),(1.08f,1.12f),(.925f,.89f),(1.08f,.89f),(.925f,1.12f)};
        static readonly string[] Tones={"moss","rust","slate","oxblood","sand","indigo"};
        static readonly Color[] ToneColours={new(.32f,.38f,.2f),new(.59f,.27f,.17f),new(.35f,.38f,.42f),new(.41f,.14f,.16f),new(.77f,.7f,.55f),new(.19f,.22f,.41f)};

        sealed class Person
        {
            public GameObject who;public TownsfolkDress record;public SkinnedMeshRenderer body;public Transform model;public string template,ownCarried,ownWorn;public Material own;
            public float tall,broad,hash;public bool walks,uniform;public Vector3[] plan;public readonly HashSet<string> needs=new(),never=new();
            public readonly List<Person> near=new();
            public int mat=-1,head,carry,build;public bool chosen;
        }
        sealed class Template
        {
            public string key;public readonly List<Material> mats=new();public readonly List<int> family=new();public readonly List<bool> worn=new();public bool[,] clothes,head;public bool cap;
        }
        static Material Dye(string body,int k)=>AssetDatabase.LoadAssetAtPath<Material>($"{Folder}/{body}/{body}_dye{k}.mat");
        static string BodyOf(Texture texture){if(texture==null)return null;string n=texture.name;int i=n.IndexOf("_base_color",StringComparison.Ordinal);if(i>0)return n.Substring(0,i);i=n.IndexOf("_dye",StringComparison.Ordinal);return i>0?n.Substring(0,i):null;}

        /// <summary>Dresses the open scene's residents. Returns the line that says what it did.</summary>
        static string DressScene(string zone,List<GameObject> residents)
        {
            var people=new List<Person>();
            foreach(var who in residents)
            {
                var model=who.transform.Find(TownsfolkLook.Model);var body=TownsfolkLook.Body(who);if(model==null||body==null)continue;
                var record=who.GetComponent<TownsfolkDress>();
                var errand=who.GetComponent<WalkLoop>();
                if(record==null){record=who.AddComponent<TownsfolkDress>();record.built=body.sharedMaterial;record.builtScale=model.localScale;record.builtSpeed=errand!=null?errand.speed:-1;}
                else
                {
                    foreach(var piece in record.added)if(piece!=null)Object.DestroyImmediate(piece);
                    record.added=Array.Empty<GameObject>();
                    if(record.built!=null&&body.sharedMaterial!=record.built){body.sharedMaterial=record.built;PrefabUtility.RecordPrefabInstancePropertyModifications(body);}
                    if(model.localScale!=record.builtScale){model.localScale=record.builtScale;PrefabUtility.RecordPrefabInstancePropertyModifications(model);}
                    // A record made before paces were kept: the errand's pace is still the builder's.
                    if(errand!=null&&record.builtSpeed<=0)record.builtSpeed=errand.speed;
                    if(errand!=null)errand.speed=record.builtSpeed;
                }
                var look=TownsfolkLook.Read(who);
                var p=new Person{who=who,record=record,body=body,model=model,template=look.template,own=body.sharedMaterial,ownCarried=look.carried,ownWorn=look.worn,tall=look.tall,broad=look.broad,
                    walks=who.GetComponent<WalkLoop>()!=null,uniform=look.uniform};
                var walk=who.GetComponent<WalkLoop>();
                if(p.walks&&walk.path!=null&&walk.path.Length>1)
                {
                    var pts=new List<Vector3>();int legs=walk.path.Length-(walk.thereAndBack?1:0);
                    for(int k=0;k<legs;k++){Vector3 a=walk.path[k],b=walk.path[(k+1)%walk.path.Length];int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)));for(int s=0;s<steps;s++)pts.Add(Vector3.Lerp(a,b,s/(float)steps));}
                    p.plan=pts.ToArray();
                }
                else p.plan=new[]{who.transform.position};
                p.hash=Mathf.Repeat(Mathf.Sin(p.plan[0].x*12.9898f+p.plan[0].z*78.233f)*43758.5453f,1);
                people.Add(p);
            }
            Gates(people);
            var profile=Profile(zone);var turn=Quaternion.Euler(0,-profile.yaw,0);
            foreach(var p in people)p.plan=p.plan.Select(q=>turn*q).ToArray();
            // Who can stand in one frame with whom: of one template, both in the scene at once, and some hero's frame holds both, the hero standing at either's height (a
            // terrace below is further up the screen). A uniform is never dressed, but whoever stands beside one in the same body is told apart from it.
            var dressed=people.Where(p=>!p.uniform).ToList();
            foreach(var p in people.Where(p=>p.uniform))p.chosen=true;
            // The heights a hero can stand at here: everyone's floor, and everyone named's (a deck below a gallery is framed from the gallery).
            var floors=people.SelectMany(p=>p.plan).Select(q=>q.y).Concat(Object.FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(n=>n.transform.position.y))
                .Select(y=>Mathf.Round(y)).Distinct().ToArray();
            foreach(var group in people.GroupBy(p=>p.template))
            {
                var list=group.ToList();
                for(int i=0;i<list.Count;i++)for(int j=i+1;j<list.Count;j++)
                {
                    var a=list[i];var b=list[j];if(a.uniform&&b.uniform||a.needs.Overlaps(b.never)||b.needs.Overlaps(a.never))continue;
                    if(a.plan.Any(x=>b.plan.Any(y=>Fits(profile,x,y,floors)))){a.near.Add(b);b.near.Add(a);}
                }
            }
            // Each template's choices of painting: the builder's own, their dyes, then the other paintings the place already has of the template and theirs. A uniform's
            // painting is there to be compared with, never to be put on.
            var templates=new Dictionary<string,Template>();
            foreach(var group in people.GroupBy(p=>p.template))
            {
                var t=new Template{key=group.Key};var sample=group.First();
                foreach(var own in group.Select(p=>p.own).Where(m=>m!=null).Distinct().OrderBy(m=>m.name))
                {
                    int family=t.family.Count==0?0:t.family.Max()+1;var ownTexture=TownsfolkLook.TextureOf(own);bool uniform=ownTexture!=null&&System.Array.IndexOf(TownsfolkLook.Uniforms,ownTexture.name)>=0;
                    t.mats.Add(own);t.family.Add(family);t.worn.Add(uniform);
                    string body=BodyOf(ownTexture);
                    if(body!=null&&!uniform)for(int k=1;k<=4;k++){var dye=Dye(body,k);if(dye!=null&&!t.mats.Contains(dye)){t.mats.Add(dye);t.family.Add(family);t.worn.Add(false);}}
                }
                int n=t.mats.Count;t.clothes=new bool[n,n];t.head=new bool[n,n];
                for(int i=0;i<n;i++)for(int j=0;j<n;j++)
                {
                    if(i==j)continue;var (c,h)=TownsfolkLook.Changed(sample.body,TownsfolkLook.TextureOf(t.mats[i]),TownsfolkLook.TextureOf(t.mats[j]));
                    // Five points past the measure's line, as with the build: a painting a hair under it on another read (.246 against .25 in the Neighbourhood) is not counted on.
                    t.clothes[i,j]=c>=TownsfolkLook.ClothesChanged+.05f;t.head[i,j]=h>=TownsfolkLook.HeadChanged+.05f;
                }
                var (_,_,headSize)=Measure(sample);t.cap=headSize.y<headSize.x*1.35f;
                templates[t.key]=t;
            }
            foreach(var p in people.Where(p=>p.uniform))p.mat=templates[p.template].mats.IndexOf(p.own);
            // Most hemmed in first; each takes the cheapest look that differs from everyone already dressed beside it in three of the four. Then whoever is still short
            // looks again against everyone round them, those dressed after them too, and so do those they are short against, until nobody is short or 24 rounds have gone.
            void Choose(Person p)
            {
                var t=templates[p.template];int own=t.mats.IndexOf(p.own);float best=float.MaxValue;(int m,int h,int c,int b) pick=(own,0,0,0);int bestShort=int.MaxValue;
                var builds=Enumerable.Range(0,Builds.Length).ToArray();var carries=p.ownCarried.Length>0?new[]{0}:Enumerable.Range(0,Carries.Length).ToArray();
                var heads=Enumerable.Range(0,Heads.Length).Where(h=>h!=1||t.cap).ToArray();
                var beside=p.near.Where(q=>q.chosen).ToArray();
                for(int m=0;m<t.mats.Count;m++)
                {
                    if(t.worn[m])continue;
                    foreach(int h in heads)foreach(int c in carries)foreach(int b in builds)
                    {
                        int shortHere=0;
                        foreach(var q in beside)if(Apart(p,m,h,c,b,q,t)<TownsfolkLook.Apart&&++shortHere>bestShort)break;
                        if(shortHere>bestShort)continue;
                        float cost=(m==own?0:t.family[m]==t.family[own]?.5f:1.2f)+(h>0?.9f:0)+(c>0?.8f:0)+(Builds[b].tall!=1?.3f:0)+(Builds[b].broad!=1?.3f:0)
                            +Mathf.Repeat(p.hash*(7+m*3+h*5+c*11+b*13),1)*.25f;
                        if(shortHere<bestShort||cost<best){bestShort=shortHere;best=cost;pick=(m,h,c,b);}
                    }
                }
                (p.mat,p.head,p.carry,p.build)=pick;p.chosen=true;
            }
            int Short(Person p)=>p.near.Count(q=>q.chosen&&Apart(p,p.mat,p.head,p.carry,p.build,q,templates[p.template])<TownsfolkLook.Apart);
            foreach(var p in dressed.OrderByDescending(p=>p.near.Count).ThenBy(p=>p.hash))Choose(p);
            for(int round=0;round<24;round++)
            {
                // The short, and from the second round on the dressed beside them they are short against (one of the two can often give way), alternately.
                var shortNow=dressed.Where(p=>Short(p)>0).ToList();if(shortNow.Count==0)break;
                var still=round%2==0?shortNow:shortNow.SelectMany(p=>p.near.Where(q=>!q.uniform&&Apart(p,p.mat,p.head,p.carry,p.build,q,templates[p.template])<TownsfolkLook.Apart)).Distinct().ToList();
                foreach(var p in still.OrderByDescending(Short).ThenBy(p=>p.hash))Choose(p);
            }
            // -looks-why "A|B": what the rule made of two people by name (a pair the test reads as copies): whether they are neighbours, and what differs.
            string why=DevArgs.Value("-looks-why");
            if(!string.IsNullOrEmpty(why))
            {
                var names=why.Split('|');var a=people.FirstOrDefault(x=>x.who.name.StartsWith(names[0]));var b=names.Length>1?people.FirstOrDefault(x=>x.who.name.StartsWith(names[1])):null;
                if(a!=null&&b!=null)
                {
                    var t=templates[a.template];
                    Debug.Log($"TOWNSFOLK_WHY {a.who.name} [{a.template}] / {b.who.name} [{b.template}] near={a.near.Contains(b)} together={!(a.needs.Overlaps(b.never)||b.needs.Overlaps(a.never))} "+
                        $"fits={a.plan.Any(x=>b.plan.Any(y=>Fits(profile,x,y,floors)))} a.plan={a.plan.Length} b.plan={b.plan.Length} apart={(a.template==b.template?Apart(a,a.mat,a.head,a.carry,a.build,b,t):-1)} "+
                        $"mats={(a.mat>=0?t.mats[a.mat].name:"?")}/{(b.mat>=0&&a.template==b.template?t.mats[b.mat].name:"?")} changed={(a.template==b.template&&a.mat>=0&&b.mat>=0?TownsfolkLook.Changed(a.body,TownsfolkLook.TextureOf(t.mats[a.mat]),TownsfolkLook.TextureOf(t.mats[b.mat])).ToString():"")}");
                }
                else Debug.Log("TOWNSFOLK_WHY nobody by those names: "+why);
            }
            var shortList=dressed.Where(p=>Short(p)>0).Select(p=>$"{p.who.name} at ({p.who.transform.position.x:0.0}, {p.who.transform.position.z:0.0})").ToList();int shortOf=shortList.Count;
            // On them.
            var goods=AssetDatabase.LoadAssetAtPath<Material>(GoodsPath);int dyed=0,hats=0,carried=0,built=0;
            foreach(var p in dressed)
            {
                var t=templates[p.template];var mat=t.mats[p.mat];
                if(mat!=p.body.sharedMaterial){p.body.sharedMaterial=mat;PrefabUtility.RecordPrefabInstancePropertyModifications(p.body);}
                if(mat!=p.own)dyed++;
                var (tall,broad)=Builds[p.build];
                if(tall!=1||broad!=1){p.model.localScale=Vector3.Scale(p.record.builtScale,new Vector3(broad,tall,broad));PrefabUtility.RecordPrefabInstancePropertyModifications(p.model);built++;}
                // An errand is walked at a pace measured for its walker's height (TownLife.Pace, CivilianStride): taller strides further at the same rate.
                var walk=p.who.GetComponent<WalkLoop>();if(walk!=null&&p.record.builtSpeed>0){walk.speed=p.record.builtSpeed*tall;EditorUtility.SetDirty(walk);}
                var added=new List<GameObject>();int tone=Mathf.FloorToInt(p.hash*Tones.Length)%Tones.Length;
                if(p.head>0&&goods!=null){var piece=Wear(p,Heads[p.head],tone,goods);if(piece!=null){added.Add(piece);hats++;}}
                if(p.carry>0&&goods!=null){var piece=Carry(p,Carries[p.carry],(tone+2)%Tones.Length,goods);if(piece!=null){added.Add(piece);carried++;}}
                p.record.added=added.ToArray();
                p.record.look=$"{mat.name}; head {(p.head>0?Heads[p.head]:"bare")}; carries {(p.carry>0?Carries[p.carry]:p.ownCarried.Length>0?p.ownCarried:"nothing")}; build {tall:0.###}x{broad:0.###}";
                EditorUtility.SetDirty(p.record);
            }
            string line=$"TOWNSFOLK_LOOKS {zone} residents={people.Count} uniforms={people.Count(p=>p.uniform)} dyed={dyed} worn={hats} carried={carried} built={built} short={shortOf}";
            if(shortOf>0)line+=" (no look left for: "+string.Join("; ",shortList.Take(8))+")";
            return line;
        }
        /// <summary>How many of the four differ between someone wearing this choice and someone already dressed, as TownsfolkLook will read them.</summary>
        static int Apart(Person p,int m,int h,int c,int b,Person q,Template t)
        {
            int n=0;
            if(m!=q.mat&&t.clothes[m,q.mat])n++;
            if(m!=q.mat&&t.head[m,q.mat]||h!=q.head||p.ownWorn!=q.ownWorn)n++;
            if((p.ownCarried.Length>0?p.ownCarried:Carries[c])!=(q.ownCarried.Length>0?q.ownCarried:Carries[q.carry]))n++;
            float tp=p.tall*Builds[b].tall,bp=p.broad*Builds[b].broad,tq=q.tall*Builds[q.build].tall,bq=q.broad*Builds[q.build].broad;
            // A little past the measure's line, so a scale that lands on it is never read the other way.
            if(Mathf.Abs(tp/tq-1)>=TownsfolkLook.Taller+.004f||Mathf.Abs(bp/bq-1)>=TownsfolkLook.Broader+.004f)n++;
            return n;
        }

        // ---------------------------------------------------------------- the story, the camera
        /// <summary>The flags each needs set and is gone at (Whereabouts and FlagGate on them or above them), as TownsfolkLookTests reads them.</summary>
        static void Gates(List<Person> people)
        {
            var says=new Dictionary<GameObject,List<(string,bool)>>();
            void Say(GameObject target,string flag,bool set){if(target==null||string.IsNullOrEmpty(flag))return;if(!says.TryGetValue(target,out var list))says[target]=list=new();list.Add((flag,set));}
            foreach(var w in Object.FindObjectsByType<Whereabouts>(FindObjectsInactive.Include,FindObjectsSortMode.None))foreach(var t in w.targets??new GameObject[0]){Say(t,w.from,true);Say(t,w.goneAt,false);}
            foreach(var g in Object.FindObjectsByType<FlagGate>(FindObjectsInactive.Include,FindObjectsSortMode.None))foreach(var t in g.targets??new GameObject[0])Say(t,g.flag,g.showWhenSet);
            foreach(var p in people)for(var t=p.who.transform;t!=null;t=t.parent)if(says.TryGetValue(t.gameObject,out var list))foreach(var (flag,set) in list)(set?p.needs:p.never).Add(flag);
        }
        static CameraProfile Profile(string zone)
        {
            var def=Resources.LoadAll<ZoneDef>("Definitions/ZoneDef").FirstOrDefault(z=>z.scene==zone||z.name==zone);
            if(def!=null&&def.cameraProfile!=null)return def.cameraProfile;
            return Resources.Load<CameraProfile>("Definitions/CameraProfile/Ground")??ScriptableObject.CreateInstance<CameraProfile>();
        }
        /// <summary>What one frame of the play camera holds, on the plan turned to the camera (x across the screen, z away from it), around a hero at the middle: the
        /// view's four corners met at a body's chest, <paramref name="chest"/> over the hero's feet, as CameraRig poses it at its profile's distance, 16:9.</summary>
        static Vector2[] Footprint(CameraProfile profile,float chest)
        {
            var rotation=Quaternion.Euler(profile.pitch,0,0);var eye=-(rotation*Vector3.forward)*profile.distance;float tan=Mathf.Tan(profile.fov*.5f*Mathf.Deg2Rad),aspect=16f/9;
            chest=Mathf.Min(chest,eye.y-1);var corners=new List<Vector2>();
            foreach(var (x,y) in new[]{(-1f,-1f),(1f,-1f),(1f,1f),(-1f,1f)})
            {
                // Four percent wider and taller than the frame, so a body at its very edge (or a taller one, its chest higher) is never missed.
                var ray=rotation*new Vector3(x*tan*aspect*1.04f,y*tan*1.04f,1);if(ray.y>-.02f)ray.y=-.02f;float along=(chest-eye.y)/ray.y;var at=eye+ray*along;corners.Add(new Vector2(at.x,at.z));
            }
            return corners.ToArray();
        }
        static readonly Dictionary<(CameraProfile,int),Vector2[]> footprints=new();
        static Vector2[] FootprintAt(CameraProfile profile,float chest)
        {
            int key=Mathf.RoundToInt(chest*2);if(!footprints.TryGetValue((profile,key),out var f))footprints[(profile,key)]=f=Footprint(profile,key*.5f);return f;
        }
        /// <summary>Whether bodies standing at these two places (turned to the camera) fit in one frame for a hero standing at any of the place's floors: a hero at height h
        /// sees a's chest on one plane and b's on another, and some place of the hero's holds both when b's footprint and a's, moved by the gap between them, overlap.</summary>
        static bool Fits(CameraProfile profile,Vector3 a,Vector3 b,float[] floors)
        {
            var gap=new Vector2(b.x-a.x,b.z-a.z);
            foreach(float h in floors.Append(a.y).Append(b.y))if(Overlap(FootprintAt(profile,1.1f+b.y-h),FootprintAt(profile,1.1f+a.y-h),gap))return true;
            return false;
        }
        /// <summary>Two convex quads, the second moved by a gap, overlap (by their edges).</summary>
        static bool Overlap(Vector2[] f,Vector2[] other,Vector2 gap)
        {
            var g=other.Select(p=>p+gap).ToArray();
            foreach(var poly in new[]{f,g})for(int i=0;i<poly.Length;i++)
            {
                var e=poly[(i+1)%poly.Length]-poly[i];var axis=new Vector2(-e.y,e.x);
                float a0=float.MaxValue,a1=float.MinValue,b0=float.MaxValue,b1=float.MinValue;
                foreach(var q in f){float d=Vector2.Dot(q,axis);a0=Mathf.Min(a0,d);a1=Mathf.Max(a1,d);}
                foreach(var q in g){float d=Vector2.Dot(q,axis);b0=Mathf.Min(b0,d);b1=Mathf.Max(b1,d);}
                if(a1<b0||b1<a0)return false;
            }
            return true;
        }

        // ---------------------------------------------------------------- what is worn and carried
        /// <summary>The body as it stands, on the person's own axes (x right, y up from the feet, z ahead): its head's box, its chest's back and side, and its shoulder.</summary>
        static (Bounds head,Bounds chest,Vector3 headSize) Measure(Person p)
        {
            var verts=SeatGeometry.World(p.body);var weights=p.body.sharedMesh.boneWeights;var bones=p.body.bones;
            var root=p.who.transform;var head=new List<Vector3>();var chest=new List<Vector3>();
            for(int i=0;i<verts.Count&&i<weights.Length;i++)
            {
                var b=weights[i].boneIndex0;if(b<0||b>=bones.Length||bones[b]==null)continue;string name=bones[b].name.ToLowerInvariant();
                var at=root.InverseTransformPoint(verts[i]);at=Vector3.Scale(at,root.lossyScale);
                if(name.Contains("head"))head.Add(at);else if(name.Contains("spine")||name.Contains("chest")||name.Contains("shoulder"))chest.Add(at);
            }
            Bounds Robust(List<Vector3> pts)
            {
                if(pts.Count==0)return new Bounds(Vector3.up*1.6f,Vector3.one*.25f);
                float P(Func<Vector3,float> f,float q){var s=pts.Select(f).OrderBy(v=>v).ToArray();return s[Mathf.Clamp((int)(q*(s.Length-1)),0,s.Length-1)];}
                var lo=new Vector3(P(v=>v.x,.05f),P(v=>v.y,.03f),P(v=>v.z,.05f));var hi=new Vector3(P(v=>v.x,.95f),P(v=>v.y,.97f),P(v=>v.z,.95f));
                var bounds=new Bounds();bounds.SetMinMax(lo,hi);return bounds;
            }
            var hb=Robust(head);return (hb,Robust(chest),hb.size);
        }
        static Transform Bone(Person p,HumanBodyBones bone,params string[] names)
        {
            var rig=p.model.GetComponentInChildren<Animator>();var found=rig!=null&&rig.isHuman?rig.GetBoneTransform(bone):null;if(found!=null)return found;
            foreach(var n in names){var t=p.model.GetComponentsInChildren<Transform>(true).FirstOrDefault(x=>x.name==n);if(t!=null)return t;}
            return p.who.transform;
        }
        static GameObject Piece(Person p,string name,string kind,int tone,Material goods,Vector3 at,Vector3 scale,Transform bone)
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>($"{Worn}/{kind}-{Tones[tone]}.asset");if(mesh==null)return null;
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=goods;r.shadowCastingMode=ShadowCastingMode.Off;
            go.transform.SetParent(p.who.transform,false);go.transform.localPosition=Vector3.Scale(at,Inverse(p.who.transform.lossyScale));go.transform.localRotation=Quaternion.identity;
            go.transform.localScale=Vector3.Scale(scale,Inverse(p.who.transform.lossyScale));go.transform.SetParent(bone,true);go.isStatic=false;
            return go;
        }
        static Vector3 Inverse(Vector3 s)=>new(1/Mathf.Max(.0001f,s.x),1/Mathf.Max(.0001f,s.y),1/Mathf.Max(.0001f,s.z));
        /// <summary>Something on the head, fitted to this head as it stands: a cap over its crown, a band round its brow, a scarf round its neck. Named "Worn …".</summary>
        static GameObject Wear(Person p,string kind,int tone,Material goods)
        {
            var (head,_,_)=Measure(p);var c=head.center;var s=head.size;var bone=Bone(p,HumanBodyBones.Head,"Head");
            switch(kind)
            {
                case "cap":return Piece(p,$"Worn cap ({Tones[tone]})","cap",tone,goods,new Vector3(c.x,head.max.y-s.y*.42f,c.z),new Vector3(s.x*.56f,s.y*.5f,s.z*.58f),bone);
                case "band":return Piece(p,$"Worn band ({Tones[tone]})","band",tone,goods,new Vector3(c.x,head.max.y-s.y*.34f,c.z),new Vector3(s.x*.54f,s.x*.54f,s.z*.56f),bone);
                case "scarf":return Piece(p,$"Worn scarf ({Tones[tone]})","scarf",tone,goods,new Vector3(c.x,head.min.y+s.y*.06f,c.z-s.z*.04f),new Vector3(s.x*.5f,s.x*.42f,s.z*.55f),Bone(p,HumanBodyBones.Neck,"neck","Neck")??bone);
            }
            return null;
        }
        /// <summary>Something carried, on the bone that carries it so it goes with the walk: a pack or a bedroll on the back, a satchel at the hip, a coil over a shoulder, a parcel
        /// under an arm. Sized to the body (made for a 1.9 m one). Named "Carried …".</summary>
        static GameObject Carry(Person p,string kind,int tone,Material goods)
        {
            var (head,chest,_)=Measure(p);float size=Mathf.Clamp(head.max.y/1.9f,.6f,1.3f);float shoulder=chest.max.y,back=chest.min.z,side=chest.max.x;var scale=Vector3.one*size;
            var spine=Bone(p,HumanBodyBones.Chest,"Spine02","Spine01","Spine");
            switch(kind)
            {
                case "pack":return Piece(p,$"Carried pack ({Tones[tone]})","pack",tone,goods,new Vector3(0,shoulder-.3f*size,back-.1f*size),scale,spine);
                case "roll":return Piece(p,$"Carried roll ({Tones[tone]})","roll",tone,goods,new Vector3(0,shoulder-.04f*size,back-.07f*size),new Vector3(Mathf.Max(.7f,(side*2+.1f)/.5f)*size*.5f,size,size),spine);
                case "satchel":return Piece(p,$"Carried satchel ({Tones[tone]})","satchel",tone,goods,new Vector3(side+.04f*size,shoulder-.62f*size,0),scale,Bone(p,HumanBodyBones.Hips,"Hips"));
                case "coil":return Piece(p,$"Carried coil ({Tones[tone]})","coil",tone,goods,new Vector3(side*.55f,shoulder-.1f*size,0),scale,spine);
                case "parcel":return Piece(p,$"Carried parcel ({Tones[tone]})","parcel",tone,goods,new Vector3(side+.06f*size,shoulder-.4f*size,.03f*size),scale,spine);
            }
            return null;
        }

        // ---------------------------------------------------------------- entry points
        /// <summary>A material for every dye tools/townsfolk_dyes.py wrote (a copy of its body's own, the dye for its colour), the pieces worn and carried in every tone, and
        /// the one material they share.</summary>
        public static void Make()=>BatchTools.Run(()=>
        {
            int made=0;
            foreach(var dir in Directory.GetDirectories(Folder).Select(d=>d.Replace('\\','/')))
            {
                string body=Path.GetFileName(dir);if(body=="Worn")continue;
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Characters/{body}.prefab")??AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Enemies/{body}.prefab");
                var source=prefab!=null?prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(r=>r.sharedMesh!=null?r.sharedMesh.vertexCount:0).Select(r=>r.sharedMaterial).FirstOrDefault():null;
                if(source==null){Debug.LogWarning("TOWNSFOLK_LOOKS_MAKE no body material for "+body);continue;}
                for(int k=1;k<=4;k++)
                {
                    string texPath=$"{dir}/{body}_dye{k}.jpg";if(!File.Exists(texPath))continue;
                    if(AssetImporter.GetAtPath(texPath) is TextureImporter importer&&(importer.maxTextureSize!=512||!importer.sRGBTexture||importer.textureType!=TextureImporterType.Default))
                    {importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.maxTextureSize=512;importer.mipmapEnabled=true;importer.SaveAndReimport();}
                    var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);string matPath=$"{dir}/{body}_dye{k}.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
                    if(mat==null){mat=new Material(source){name=$"{body}_dye{k}"};AssetDatabase.CreateAsset(mat,matPath);}else mat.CopyPropertiesFromMaterial(source);
                    mat.SetTexture("_BaseMap",texture);EditorUtility.SetDirty(mat);made++;
                }
            }
            // Every body's map of head, hands, feet and clothes, for TownsfolkLookTests (a body's mesh can't be read in play).
            int maps=0;
            foreach(var folder in new[]{"Assets/_Project/Prefabs/Characters","Assets/_Project/Prefabs/Enemies"})
                foreach(var guid in AssetDatabase.FindAssets("t:GameObject",new[]{folder}))
                {
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                    var body=prefab!=null?prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(r=>r.sharedMesh!=null).OrderByDescending(r=>r.sharedMesh.vertexCount).FirstOrDefault():null;
                    if(body!=null&&body.sharedMesh.boneWeights.Length>0){TownsfolkLook.SaveRegions(body);maps++;}
                }
            var goods=ZoneKit.Toon("townsfolk-goods",null,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_ShadowTint",new Color(.55f,.55f,.64f));});
            if(!AssetDatabase.IsValidFolder(Worn))AssetDatabase.CreateFolder(Folder,"Worn");
            for(int i=0;i<Tones.Length;i++)
            {
                Color tone=ToneColours[i],under=ZoneKit.Shape.Dim(tone,.68f),strap=ZoneKit.Shape.Dim(tone,.55f);
                Save("cap",i,new ZoneKit.Shape().Dome(Vector3.zero,1,1,5,14,tone,under).Disc(new Vector3(0,.02f,.18f),Vector3.up,.95f,14,under,.82f));
                Save("band",i,Ring(1,.2f,tone,under));
                Save("scarf",i,Ring(1,.34f,tone,under).Lump(new Vector3(.35f,-.55f,.86f),new Vector3(.22f,.5f,.12f),3,.15f,tone,under,8,5));
                Save("pack",i,new ZoneKit.Shape().Lump(new Vector3(0,0,-.05f),new Vector3(.22f,.3f,.13f),31,.12f,tone,under,9,5).Box(new Vector3(0,.28f,-.02f),new Vector3(.3f,.05f,.2f),Quaternion.identity,strap));
                Save("satchel",i,new ZoneKit.Shape().Lump(Vector3.zero,new Vector3(.07f,.13f,.15f),17,.1f,tone,under,8,5).Box(new Vector3(.05f,.06f,0),new Vector3(.03f,.1f,.27f),Quaternion.identity,strap));
                var roll=new Vector3[7];var thick=new float[7];for(int k=0;k<7;k++){roll[k]=new Vector3(-.25f+k*.5f/6,0,0);thick[k]=.075f;}
                Save("roll",i,new ZoneKit.Shape().Tube(roll,thick,9,tone,under,1,1,true));
                var ring=new Vector3[13];var turns=new float[13];for(int k=0;k<13;k++){float a=k*Mathf.PI*2/12;ring[k]=new Vector3(0,Mathf.Sin(a)*.24f,Mathf.Cos(a)*.24f);turns[k]=.035f;}
                Save("coil",i,new ZoneKit.Shape().Tube(ring,turns,5,ZoneKit.Shape.Dim(tone,.6f),ZoneKit.Shape.Dim(tone,.75f),1,1,false));
                // A packing crate to sit on: boards round a box, its slats a shade darker (unit size, its foot at nought: Seating scales it to the sitter).
                {
                    var wood=new Color(.58f,.47f,.34f);var dark=ZoneKit.Shape.Dim(wood,.62f);var crate=new ZoneKit.Shape().Box(new Vector3(0,.5f,0),new Vector3(.96f,1,.96f),Quaternion.identity,wood,ZoneKit.Shape.Dim(wood,1.1f));
                    foreach(float y in new[]{.08f,.5f,.92f})crate.Box(new Vector3(0,y,0),new Vector3(1,.07f,1),Quaternion.identity,dark,dark);
                    foreach(float x in new[]{-.47f,.47f})foreach(float z in new[]{-.47f,.47f})crate.Box(new Vector3(x,.5f,z),new Vector3(.07f,1,.07f),Quaternion.identity,dark,dark);
                    crate.Box(new Vector3(0,.97f,0),new Vector3(.5f,.04f,.06f),Quaternion.identity,ToneColours[i],ToneColours[i]);
                    Save("crate",i,crate);
                }
                Save("cushion",i,new ZoneKit.Shape().Lump(new Vector3(0,.5f,0),new Vector3(.5f,.5f,.5f),41,.1f,tone,under,10,5));
                Save("blanket",i,new ZoneKit.Shape().Lump(new Vector3(0,.27f,0),new Vector3(.5f,.27f,.5f),43,.08f,under,ZoneKit.Shape.Dim(tone,.55f),10,4).Lump(new Vector3(.02f,.74f,-.01f),new Vector3(.47f,.26f,.46f),47,.08f,tone,under,10,4));
                Save("parcel",i,new ZoneKit.Shape().Box(Vector3.zero,new Vector3(.12f,.26f,.34f),Quaternion.identity,new Color(.72f,.62f,.46f),new Color(.8f,.7f,.52f)).Box(new Vector3(.065f,0,0),new Vector3(.01f,.27f,.05f),Quaternion.identity,tone));
            }
            AssetDatabase.SaveAssets();TownsfolkLook.Forget();
            Debug.Log($"TOWNSFOLK_LOOKS_MADE dye materials={made} region maps={maps} goods={goods.name}");
            Debug.Log("TOWNSFOLK_LOOKS_MADE_OK");
        });
        static ZoneKit.Shape Ring(float radius,float thick,Color tone,Color under)
        {
            var path=new Vector3[17];var radii=new float[17];for(int k=0;k<17;k++){float a=k*Mathf.PI*2/16;path[k]=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);radii[k]=thick;}
            return new ZoneKit.Shape().Tube(path,radii,7,tone,under,1,1,false);
        }
        static void Save(string kind,int tone,ZoneKit.Shape shape)=>ZoneKit.SaveMesh(shape.Mesh($"{kind}-{Tones[tone]}"),$"{Worn}/{kind}-{Tones[tone]}.asset");

        /// <summary>Stills of townsfolk as the play camera frames them, for before and after: <c>-looks-run name -looks-views "Zone@x,y,z@label;..."</c>, each with the ground
        /// camera's profile at its distance looking at the point (a hero's feet), everyone in their idle as in play (LookDev's way), written to Builds/quality/look/name.
        /// Changes nothing.</summary>
        public static void Stills()=>BatchTools.Run(()=>
        {
            string run=DevArgs.Value("-looks-run")??throw new ArgumentException("-looks-run <name> is required");
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality/look",run));Directory.CreateDirectory(folder);
            var ground=AssetDatabase.LoadAssetAtPath<CameraProfile>("Assets/_Project/Resources/Definitions/CameraProfile/Ground.asset");
            var views=(DevArgs.Value("-looks-views")??"").Split(';').Where(v=>v.Contains('@')).Select(v=>v.Split('@')).ToArray();
            // A view's fourth part says how its scene stands: "all" (everyone the story brings in at any beat), "built" (as the builder left them), or both.
            string Flags(string[] v)=>v.Length>3?v[3]:"";
            foreach(var group in views.GroupBy(v=>(v[0],Flags(v))))
            {
                var (zone,flags)=group.Key;
                EditorSceneManager.OpenScene($"Assets/_Project/Scenes/{zone}.unity");
                foreach(var text in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))text.gameObject.SetActive(false);
                // -looks-all: everyone the story brings in at some beat stands too (the foundry's smiths come with the colossus's fight).
                if(DevArgs.Has("-looks-all")||flags.Contains("all"))foreach(var who in Residents(UnityEngine.SceneManagement.SceneManager.GetActiveScene()))for(var t=who.transform;t!=null;t=t.parent)t.gameObject.SetActive(true);
                // -looks-built: everyone as their builder left them, before this pass (a before still of a scene dressed already). In memory only.
                if(DevArgs.Has("-looks-built")||flags.Contains("built"))foreach(var dress in Object.FindObjectsByType<TownsfolkDress>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    var body=TownsfolkLook.Body(dress.gameObject);var model=dress.transform.Find(TownsfolkLook.Model);if(body==null||model==null)continue;
                    if(dress.built!=null)body.sharedMaterial=dress.built;model.localScale=dress.builtScale;foreach(var piece in dress.added)if(piece!=null)piece.SetActive(false);
                }
                if(DevArgs.Has("-looks-built")||flags.Contains("built"))foreach(var fit in Object.FindObjectsByType<SeatFit>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                {
                    var model=fit.transform.Find(TownsfolkLook.Model);if(model!=null)model.localPosition=fit.builtLocal;if(fit.seat!=null)fit.seat.SetActive(false);
                }
                foreach(var animator in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
                {
                    if(animator.GetComponentInParent<Posed>()!=null)continue;
                    var clips=animator.runtimeAnimatorController!=null?animator.runtimeAnimatorController.animationClips:new AnimationClip[0];
                    var idle=clips.FirstOrDefault(c=>c!=null&&c.name.ToLowerInvariant().Contains("idle"));if(idle!=null)idle.SampleAnimation(animator.gameObject,.3f);
                }
                foreach(var posed in Object.FindObjectsByType<Posed>(FindObjectsSortMode.None))posed.Hold();
                var def=Resources.LoadAll<ZoneDef>("Definitions/ZoneDef").FirstOrDefault(d=>d.scene==zone||d.id==zone);if(def!=null)RenderSettings.skybox=def.skybox;
                var profile=def!=null&&def.cameraProfile!=null?def.cameraProfile:ground;
                var camera=new GameObject("Look camera",typeof(Camera),typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData)).GetComponent<Camera>();
                camera.clearFlags=RenderSettings.skybox!=null?CameraClearFlags.Skybox:CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.03f,.06f);camera.nearClipPlane=.3f;camera.farClipPlane=1500;
                var data=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.antialiasing=UnityEngine.Rendering.Universal.AntialiasingMode.None;
                foreach(var v in group)
                {
                    var xyz=v[1].Split(',').Select(n=>float.Parse(n,System.Globalization.CultureInfo.InvariantCulture)).ToArray();var point=new Vector3(xyz[0],xyz[1],xyz[2]);
                    // A height of -9999 is the floor under the point: the lowest walkable thing a ray from above meets.
                    if(point.y<-999){Physics.SyncTransforms();var hits=Physics.RaycastAll(new Vector3(point.x,300,point.z),Vector3.down,600,~0,QueryTriggerInteraction.Ignore).Where(h=>h.normal.y>.7f&&h.collider.GetComponentInParent<PropTint>()==null).OrderBy(h=>h.point.y).ToArray();point.y=hits.Length>0?hits[0].point.y:0;}
                    var rotation=Quaternion.Euler(profile.pitch,profile.yaw,0);camera.fieldOfView=profile.fov;
                    // "close" in the flags: a third of the play distance, to see a body's feet and seat.
                    camera.transform.SetPositionAndRotation(point-rotation*Vector3.forward*profile.distance*(flags.Contains("close")?.33f:1),rotation);
                    var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=4};camera.targetTexture=rt;camera.Render();camera.Render();
                    var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
                    File.WriteAllBytes(Path.Combine(folder,$"{zone}-{(v.Length>2?v[2]:v[1])}{(flags.Length>0?"-"+flags:"")}.png"),image.EncodeToPNG());
                    RenderTexture.active=prior;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
                }
                Object.DestroyImmediate(camera.gameObject);
            }
            Debug.Log("TOWNSFOLK_STILLS_OK "+folder);
        });
        /// <summary>Opens each scene named by <c>-looks-zones</c> and saves it, which dresses it; nothing else in it changes. <c>-looks-dry</c> dresses in memory, says what it
        /// would do and saves nothing; <c>-looks-held</c> lifts the hold on the named scenes for this run.</summary>
        public static void Dress()=>BatchTools.Run(()=>
        {
            var zones=(DevArgs.Value("-looks-zones")??"").Split(',').Select(z=>z.Trim()).Where(z=>z.Length>0).ToArray();
            if(zones.Length==0)throw new ArgumentException("-looks-zones names the scenes to dress");
            bool dry=DevArgs.Has("-looks-dry"),held=DevArgs.Has("-looks-held");
            foreach(var zone in zones)
            {
                var scene=EditorSceneManager.OpenScene($"Assets/_Project/Scenes/{zone}.unity");
                if(dry){var residents=Residents(scene);Debug.Log(residents.Count==0?$"TOWNSFOLK_LOOKS {zone} residents=0":DressScene(zone,residents)+" (dry: not saved)");continue;}
                if(held)lifted.Add(zone);
                try{EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}finally{lifted.Remove(zone);}
                Debug.Log("TOWNSFOLK_LOOKS_SAVED "+zone);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("TOWNSFOLK_LOOKS_OK");
        });
    }
}
