using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lattice.Combat;
using Lattice.Core;
using Lattice.UI;
using Lattice.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace Lattice.EditorTools
{
    /// <summary>
    /// People with somewhere to be in the first two chapters' settlements, as the towns built since have them (guide 0.1 and 7.4). The Decks and Tallow Drift are staged
    /// here, isolated as the rest of chapter 1's staging is: each scene is opened, its "Town life" root replaced, its station navigation baked again with the residents
    /// standing in it, and saved; nothing else in the scene is rebuilt. Lanternwick's people are its own builder's (LanternRoadBuilder) and are held to the same
    /// <see cref="Audit"/>. A resident is refused, and the scene left unsaved, if they stand within two metres of any replay route or story walk, in the fixed camera's
    /// line to anyone named or to a prompt, on a floor sign, in a doorway, inside a prop or beside a neighbour in the same body; an errand is refused if anything solid
    /// stands on its loop; and the whole staging is refused if the bake changes the path of any route or the reach of any prompt.
    /// Order: after QaNov10Staging (and MarketOpeningStaging.Refresh after a Decks rebuild), before GroundStaging.
    /// Run: exec-when-green.ps1 -Method Lattice.EditorTools.TownLife.Apply -Marker TOWN_LIFE_OK -Graphics [-ExtraArgs @('-town-zones','Hub_Decks')]
    /// </summary>
    public static partial class TownLife
    {
        public const string RootName="Town life";
        /// <summary>Someone who lives here, standing: the body they wear, where (plan), facing, dye, build, what they carry, and the story's say on whether they are here
        /// (from a flag, gone at another; decided as the scene loads, so nobody blinks out in view).</summary>
        internal sealed class Folk
        {
            public string name,body,carries,from,goneAt;public Vector2 at;public float yaw,build=1;public Color tint=Color.white;
            /// <summary>Stands in their own doorway on purpose (a household packing): the rule that keeps people out of doorways doesn't hold them.</summary>
            public bool door;
            public Folk(string name,string body,float x,float z,float yaw,Color tint,float build=1,string carries=null,string from=null,string goneAt=null)
            {this.name=name;this.body=body;at=new Vector2(x,z);this.yaw=yaw;this.tint=tint;this.build=build;this.carries=carries;this.from=from;this.goneAt=goneAt;}
        }
        /// <summary>Someone on an errand (<see cref="WalkLoop"/>): a loop on the plan, walked there and back or round, a wait at each stop. Its pace is the body's own:
        /// GeneratedAnimator plays an unprofiled walk at .7 of its native rate below 1.47 m/s, so the loop is walked at .7 of the body's measured walk and its soles hold.</summary>
        internal sealed class Errand
        {
            public string name,body,carries,from,goneAt;public Vector2[] loop;public float pause,build=1;public Color tint=Color.white;public bool back;
            public Errand(string name,string body,Vector2[] loop,float pause,Color tint,bool back,float build=1,string carries=null,string from=null,string goneAt=null)
            {this.name=name;this.body=body;this.loop=loop;this.pause=pause;this.tint=tint;this.back=back;this.build=build;this.carries=carries;this.from=from;this.goneAt=goneAt;}
        }
        /// <summary>A way people take that no route names: a story walk (no one stands or walks on it), or the natural line from a door to a counter (no one stands on it;
        /// an errand may cross it, and stops for whoever is on it).</summary>
        internal readonly struct Way
        {
            public readonly string name;public readonly Vector2[] path;public readonly bool crossable;
            public Way(string name,bool crossable,params Vector2[] path){this.name=name;this.path=path;this.crossable=crossable;}
        }
        /// <summary>How near a way someone may stand (the towns built since keep two metres; the tests ask more than 1.6), and how near an errand's line may run.</summary>
        internal const float StandClear=2,WalkClear=1.8f;
        static readonly int GoodsSeed=31;

        [Restage.Step("chapter1.town-life","Hub_Decks,TallowDrift",After="chapter1.nov10,chapter1.market",Chapter=1,ScenesArg="-town-zones",Check="TownLifeHolds",Leaves=new[]{"Hub_Decks: Town life #1","TallowDrift: Town life #1"})]
        public static void Apply()=>BatchTools.Run(()=>
        {
            var refused=new List<string>();
            foreach(var zone in Zones())
            {
                if(zone=="Lanternwick"){Debug.Log("TOWN_LIFE Lanternwick is its own builder's (LanternRoadBuilder.BuildZones -lantern-zones Lanternwick)");continue;}
                var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/"+zone+".unity");
                foreach(var old in scene.GetRootGameObjects().Where(g=>g.name==RootName).ToArray())Object.DestroyImmediate(old);
                Physics.SyncTransforms();
                var routes=RouteLegs(zone);
                var before=WithNavigation(()=>(Paths(routes),Reachable()));
                var root=new GameObject(RootName).transform;
                var kit=ZoneKit.Ground(zone,p=>0,3.2f);
                var (folk,errands,ways)=zone=="Hub_Decks"?Decks(kit,root):zone=="TallowDrift"?Tallow(kit,root):throw new ArgumentException("No town life for "+zone);
                // Everyone stands for the audit and the bake, whichever beat they belong to: an errand ran into a shopper who comes only on the second day, and a body that
                // comes with the story isn't walked into by a path laid before it came.
                var hidden=root.GetComponentsInChildren<Transform>(true).Where(t=>t.parent==root&&!t.gameObject.activeSelf).Select(t=>t.gameObject).ToArray();
                foreach(var h in hidden)h.SetActive(true);
                var faults=Audit(zone,folk.Select(f=>(f.name,f.body,new Vector3(f.at.x,0,f.at.y),f.build,f.from,f.goneAt,f.door)).ToArray(),
                    errands.Select(e=>(e.name,e.body,e.loop,e.back,e.build,e.carries!=null,e.from,e.goneAt)).ToArray(),ways,Probes(zone));
                foreach(var nav in Object.FindObjectsByType<GroundNavigation>(FindObjectsInactive.Include,FindObjectsSortMode.None))Object.DestroyImmediate(nav.gameObject);
                StationNavigationBake.Bake(zone);
                foreach(var h in hidden)h.SetActive(false);
                var after=WithNavigation(()=>(Paths(routes),Reachable()));
                faults.AddRange(NavigationKept(before,after));
                if(faults.Count>0){foreach(var f in faults.Distinct())Debug.LogWarning("TOWN_LIFE_PROBLEM "+zone+": "+f);refused.Add(zone);continue;}
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                Debug.Log($"TOWN_LIFE {zone} standing={folk.Length} errands={errands.Length} routes={routes.Count}");
            }
            AssetDatabase.SaveAssets();
            if(refused.Count>0)throw new InvalidOperationException("Town life refused, scenes left unsaved: "+string.Join(", ",refused));
            Debug.Log("TOWN_LIFE_OK");
        });

        // ---------------------------------------------------------------- making people
        /// <summary>The body if it has been taken in, else the plainest one: a town is staged before every body lands.</summary>
        internal static string Body(string id)=>ZoneKit.Prefab("Characters",id)!=null?id:"Neve";
        static readonly Dictionary<string,float> paces=new();
        static float convention=float.NaN;
        /// <summary>The pace at which this body's unprofiled walk holds its soles, at this build.</summary>
        internal static float Pace(string body,float build)
        {
            if(!paces.TryGetValue(body,out float walk))
            {
                if(float.IsNaN(convention))convention=CivilianStride.Convention();
                paces[body]=walk=CivilianStride.WalkSpeed(ZoneKit.Prefab("Characters",body))*convention;
            }
            return walk*.7f*build;
        }
        internal static GameObject Stand(ZoneKit kit,Transform root,Folk f)
        {
            var person=kit.Townsperson(Body(f.body),new Vector3(f.at.x,0,f.at.y),f.yaw,f.tint,"Neve");person.transform.localScale=Vector3.one*f.build;person.name=f.name;
            Carry(kit,person,f.carries);person.transform.SetParent(root,true);
            When(root,f.name,person,f.from,f.goneAt);
            return person;
        }
        internal static WalkLoop Walk(ZoneKit kit,Transform root,Errand e)
        {
            string body=Body(e.body);
            var walker=kit.Walker(body,e.loop,Pace(body,e.build),e.pause,e.tint,"Neve");walker.thereAndBack=e.back;walker.name=e.name;walker.transform.localScale=Vector3.one*e.build;
            Carry(kit,walker.gameObject,e.carries);walker.transform.SetParent(root,true);
            When(root,e.name,walker.gameObject,e.from,e.goneAt);
            return walker;
        }
        /// <summary>Here only from a beat, or only until one, decided as the scene loads. Saved hidden when the first visit doesn't have them, so a still of the scene shows
        /// the town as it's first walked.</summary>
        internal static void When(Transform root,string name,GameObject who,string from,string goneAt)
        {
            if(string.IsNullOrEmpty(from)&&string.IsNullOrEmpty(goneAt))return;
            var where=new GameObject(name+" (when)",typeof(Whereabouts)).GetComponent<Whereabouts>();where.transform.SetParent(root,false);
            where.from=from??"";where.awayFrom=where.backAt="";where.goneAt=goneAt??"";where.live=false;where.targets=new[]{who};
            if(!string.IsNullOrEmpty(from))who.SetActive(false);
        }
        static Material goods;
        /// <summary>One tinted material for everything people carry that is made here: cloth, cable, a bag's strap.</summary>
        static Material Goods()=>goods!=null?goods:goods=ZoneKit.Toon("town-goods",null,m=>{ZoneKit.Tinted(m);ZoneKit.TwoSided(m);m.SetColor("_ShadowTint",new Color(.55f,.55f,.64f));});
        /// <summary>What someone carries, on the bone that carries it so it goes with the walk: a crate on the shoulder (the station's own crate, small), a pack on the back,
        /// a coil of line over a shoulder, a parcel under an arm. Drawn only, no shadow of its own to speak of, and in the body's dye.</summary>
        internal static void Carry(ZoneKit kit,GameObject person,string what)
        {
            if(string.IsNullOrEmpty(what))return;
            var model=person.transform.GetChild(0);var rig=model.GetComponentInChildren<Animator>();
            Transform chest=rig!=null&&rig.isHuman?rig.GetBoneTransform(HumanBodyBones.Chest)??rig.GetBoneTransform(HumanBodyBones.Spine):null;
            var arm=rig!=null&&rig.isHuman?rig.GetBoneTransform(HumanBodyBones.RightUpperArm):null;
            float feet=person.transform.position.y,shoulder=arm!=null?arm.position.y-feet:1.45f*person.transform.localScale.y;
            GameObject thing;Vector3 local;
            switch(what)
            {
                case "crate":
                    thing=(GameObject)PrefabUtility.InstantiatePrefab(ZoneKit.Prefab("Environment","DeckCrate"));var size=ModelGeometry.BoundsOf(thing).size;thing.transform.localScale*=.42f/Mathf.Max(.01f,size.y);
                    local=new Vector3(.2f,shoulder+.17f,.02f);break;
                case "pack":
                    thing=kit.Built("Pack",new ZoneKit.Shape().Lump(new Vector3(0,0,-.05f),new Vector3(.22f,.3f,.13f),GoodsSeed,.12f,new Color(.62f,.56f,.48f),new Color(.4f,.36f,.32f),9,5)
                        .Box(new Vector3(0,.28f,-.02f),new Vector3(.3f,.05f,.2f),Quaternion.identity,new Color(.36f,.32f,.3f)),Goods());
                    local=new Vector3(0,shoulder-.32f,-.24f);break;
                case "coil":
                    var ring=new Vector3[13];var thick=new float[13];for(int k=0;k<13;k++){float a=k*Mathf.PI*2/12;ring[k]=new Vector3(0,Mathf.Sin(a)*.24f,Mathf.Cos(a)*.24f);thick[k]=.035f;}
                    thing=kit.Built("Cable coil",new ZoneKit.Shape().Tube(ring,thick,5,new Color(.2f,.22f,.25f),new Color(.28f,.3f,.33f),1,1,false),Goods());
                    local=new Vector3(.17f,shoulder-.12f,0);break;
                case "parcel":
                    thing=kit.Built("Parcel",new ZoneKit.Shape().Box(Vector3.zero,new Vector3(.12f,.26f,.34f),Quaternion.identity,new Color(.72f,.62f,.46f),new Color(.8f,.7f,.52f))
                        .Box(new Vector3(.065f,0,0),new Vector3(.01f,.27f,.05f),Quaternion.identity,new Color(.3f,.28f,.3f)),Goods());
                    local=new Vector3(.27f,shoulder-.42f,.02f);break;
                default:throw new ArgumentException("Nothing to carry called "+what);
            }
            thing.name="Carried "+what;
            foreach(var c in thing.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
            foreach(var t in thing.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=false;
            foreach(var r in thing.GetComponentsInChildren<Renderer>(true))r.shadowCastingMode=ShadowCastingMode.Off;
            thing.transform.SetPositionAndRotation(person.transform.TransformPoint(local),person.transform.rotation);thing.transform.SetParent(chest!=null?chest:person.transform,true);
        }

        // ---------------------------------------------------------------- what may not be crossed
        /// <summary>Every leg a replay route walks in this zone, from the nearest spawn to its first point (as RoomDressing reads them): the chain's sixteen and every older one.</summary>
        internal static List<(Vector2 a,Vector2 b,string route)> RouteLegs(string zone)
        {
            var legs=new List<(Vector2,Vector2,string)>();
            string dir=Path.GetFullPath(Path.Combine(Application.dataPath,"../../docs/quality/routes"));
            var spawns=Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Select(s=>new Vector2(s.transform.position.x,s.transform.position.z)).ToArray();
            foreach(var file in Directory.GetFiles(dir,"*.json").OrderBy(f=>f))
            {
                QualityRoute route;try{route=JsonUtility.FromJson<QualityRoute>(File.ReadAllText(file));}catch{continue;}
                if(route?.steps==null)continue;
                string current=route.scene,name=Path.GetFileNameWithoutExtension(file);Vector2? last=null;
                foreach(var step in route.steps)
                {
                    string scene=string.IsNullOrEmpty(step.expectedScene)?current:step.expectedScene;
                    if(scene!=current){current=scene;last=null;}
                    if(scene!=zone){last=null;continue;}
                    if(!step.navigate||step.approachTarget||step.approachPartner)continue;
                    var p=new Vector2(step.point.x,step.point.z);
                    legs.Add((last??(spawns.Length>0?spawns.OrderBy(s=>Vector2.Distance(s,p)).First():p),p,name));last=p;
                }
            }
            return legs;
        }
        /// <summary>The probe lines RoomDressingTests runs through the gaps beside the Decks' four cabins (its control needs them open with the walls away).</summary>
        static (Vector2 a,Vector2 b)[] Probes(string zone)=>zone!="Hub_Decks"?new (Vector2,Vector2)[0]:new[]{(new Vector2(-38.5f,8.8f),new Vector2(-38.5f,10.9f)),(new Vector2(-37.85f,20.2f),new Vector2(-37.85f,18.3f)),
            (new Vector2(-18.15f,8.8f),new Vector2(-18.15f,10.9f)),(new Vector2(-18.15f,20.2f),new Vector2(-18.15f,18.3f))};
        /// <summary>Whether a body this wide, standing here, crosses the fixed camera's line to a point: the box QaWorldSecondTests and XylosWorldTests hold townsfolk to.</summary>
        internal static bool InTheLine(Vector3 foot,float build,float wide,Vector3 target)
        {
            var box=new Bounds(foot+Vector3.up*.95f*build,new Vector3(wide*build,1.9f*build,wide*build));var eye=target+SightlineAudit.ToCamera*SightlineAudit.Distance;
            return box.IntersectRay(new Ray(eye,target-eye),out float along)&&along<SightlineAudit.Distance-SightlineAudit.NearIgnore;
        }
        static bool Together(string fromA,string goneA,string fromB,string goneB)=>!(!string.IsNullOrEmpty(goneA)&&goneA==fromB||!string.IsNullOrEmpty(goneB)&&goneB==fromA);
        /// <summary>Every rule a resident is held to, read off the open scene. Empty when all of them stand where they may.</summary>
        internal static List<string> Audit(string zone,(string name,string body,Vector3 at,float build,string from,string goneAt,bool door)[] standing,
            (string name,string body,Vector2[] loop,bool back,float build,bool carrying,string from,string goneAt)[] errands,IEnumerable<Way> ways,(Vector2 a,Vector2 b)[] probes,Func<Vector2,float> ground=null)
        {
            ground??=p=>0;Physics.SyncTransforms();var faults=new List<string>();
            var legs=RouteLegs(zone).Select(l=>(l.a,l.b,l.route,false)).ToList();
            foreach(var way in ways)for(int i=1;i<way.path.Length;i++)legs.Add((way.path[i-1],way.path[i],way.name,way.crossable));
            var named=Object.FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var prompts=Object.FindObjectsByType<InteractionPrompt>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(p=>p is not Npc&&p.range>0).ToArray();
            var signs=Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Sign_")&&t.GetComponentInParent<Npc>(true)==null&&!named.Any(n=>DisplayNames.Speaker(n.speaker)==t.text)).ToArray();
            var doors=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Pressure frame collision").Select(t=>new Vector2(t.position.x,t.position.z)).ToArray();
            var spawns=Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).Select(s=>new Vector2(s.transform.position.x,s.transform.position.z)).ToArray();
            bool Ours(Component c)=>c.GetComponentInParent<PropTint>()!=null&&(c.transform.root.name==RootName||c.GetComponentInParent<WalkLoop>()!=null||c.transform.root.name=="Townsperson");
            var drawn=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&r.GetComponent<TMP_Text>()==null&&!Ours(r)&&!r.name.StartsWith("Floor detail")&&r.shadowCastingMode!=ShadowCastingMode.ShadowsOnly).ToArray();
            void Point(string who,Vector2 p,float y,float build,float wide,bool stand,bool door=false)
            {
                foreach(var (a,b,route,crossable) in legs)
                {
                    if(!stand&&crossable)continue;float gap=ZoneKit.Segment(p,a,b);
                    if(gap<(stand?StandClear:WalkClear))faults.Add($"{who} at {p} is {gap:0.00} m from {route} {a}->{b}");
                }
                var foot=new Vector3(p.x,y,p.y);
                foreach(var npc in named)
                {
                    var at=npc.transform.position;if(Vector2.Distance(p,new Vector2(at.x,at.z))<2)faults.Add($"{who} at {p} stands at {npc.speaker}'s elbow");
                    foreach(float h in new[]{.4f,.9f,1.5f})if(InTheLine(foot,build,wide,at+Vector3.up*h)){faults.Add($"{who} at {p} stands between the camera and {npc.speaker}");break;}
                }
                foreach(var prompt in prompts)
                {
                    var at=prompt.transform.position;float floor=at.y-(prompt is Bench||prompt is RepairBay?.8f:0);
                    if(Vector2.Distance(p,new Vector2(at.x,at.z))<(stand?StandClear:WalkClear))faults.Add($"{who} at {p} crowds \"{prompt.prompt}\"");
                    foreach(float h in new[]{.3f,.9f})if(InTheLine(foot,build,wide,new Vector3(at.x,floor+h,at.z))){faults.Add($"{who} at {p} hides \"{prompt.prompt}\" from the camera");break;}
                }
                foreach(var sign in signs)
                {
                    if(!sign.gameObject.activeInHierarchy||sign.transform.position.y>y+1)continue;
                    sign.ForceMeshUpdate();float half=sign.textBounds.extents.x*sign.transform.lossyScale.x;var at=sign.transform.position;
                    if(Mathf.Abs(p.x-at.x)<half+1.2f&&p.y>at.z-3.2f&&p.y<at.z+1.6f)faults.Add($"{who} at {p} stands on the floor sign {sign.text}");
                }
                if(!door)foreach(var frame in doors)if(Vector2.Distance(p,frame)<(stand?1.8f:1.3f))faults.Add($"{who} at {p} stands in the doorway at {frame}");
                foreach(var spawn in spawns)if(Vector2.Distance(p,spawn)<2)faults.Add($"{who} at {p} stands where the party arrives ({spawn})");
                foreach(var (a,b) in probes)if(ZoneKit.Segment(p,a,b)<.8f)faults.Add($"{who} at {p} stands in a cabin's side gap ({a}->{b})");
            }
            bool InsideDrawn(Vector2 p,float y,float grow,out string what)
            {
                what=null;
                foreach(var r in drawn)
                {
                    var b=r.bounds;if(b.max.y<y+.3f||b.min.y>y+1.8f||b.size.y<.25f||Mathf.Max(b.size.x,b.size.z)>9)continue;
                    if(Mathf.Abs(p.x-b.center.x)<b.extents.x*grow+.3f&&Mathf.Abs(p.y-b.center.z)<b.extents.z*grow+.3f){what=r.transform.root.name+"/"+r.name;return true;}
                }
                return false;
            }
            foreach(var (name,body,at,build,_,_,door) in standing)
            {
                var p=new Vector2(at.x,at.z);float y=ground(p);Point(name,p,y,build,.9f,true,door);
                foreach(var hit in Physics.OverlapBox(new Vector3(at.x,y+1.05f*build,at.z),new Vector3(.42f,.75f,.42f)*build,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                {
                    // Itself, and anyone walking (who stops for whoever is there), aren't in the way; another resident standing is.
                    bool self=hit.GetComponentInParent<PropTint>()!=null&&Vector2.Distance(new Vector2(hit.transform.position.x,hit.transform.position.z),p)<.05f;
                    if(!self&&hit.GetComponentInParent<WalkLoop>()==null&&hit.GetComponentInParent<CombatActor>()==null)faults.Add($"{name} at {p} stands in {hit.transform.root.name}/{hit.name}");
                }
                if(InsideDrawn(p,y,.75f,out var what))faults.Add($"{name} at {p} stands inside {what}");
            }
            foreach(var (name,body,loop,back,build,carrying,_,_) in errands)
            {
                int count=loop.Length-(back?1:0);
                for(int k=0;k<count;k++)
                {
                    Vector2 a=loop[k],b=loop[(k+1)%loop.Length];int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.25f));
                    for(int s=0;s<=steps;s++){var p=Vector2.Lerp(a,b,s/(float)steps);float y=ground(p);Point(name,p,y,build,carrying?1.1f:.9f,false);if(InsideDrawn(p,y,1,out var what)){faults.Add($"{name}'s errand runs through {what} at {p}");break;}}
                    // A walker stands still for good at anything solid in its way (Driftreed's check).
                    var from=new Vector3(a.x,ground(a)+.95f,a.y);var to=new Vector3(b.x,ground(b)+.95f,b.y);var run=to-from;run.y=0;
                    foreach(var hit in Physics.SphereCastAll(from,.42f,run.normalized,run.magnitude,~0,QueryTriggerInteraction.Ignore))
                        if(hit.collider.GetComponentInParent<WalkLoop>()==null)faults.Add($"{name}'s errand runs into {hit.collider.transform.root.name}/{hit.collider.name} at ({hit.point.x:0.0}, {hit.point.z:0.0})");
                }
            }
            // No two neighbours alike: nobody within 3.2 m of someone in the same body, unless the story never has them there together.
            var everyone=standing.Select(s=>(s.name,s.body,p:new Vector2(s.at.x,s.at.z),s.from,s.goneAt)).Concat(errands.Select(e=>(e.name,e.body,p:e.loop[0],e.from,e.goneAt))).ToArray();
            for(int i=0;i<everyone.Length;i++)for(int j=i+1;j<everyone.Length;j++)
            {
                var (an,ab,ap,af,ag)=everyone[i];var (bn,bb,bp,bf,bg)=everyone[j];
                if(ab==bb&&Vector2.Distance(ap,bp)<3.2f&&Together(af,ag,bf,bg))faults.Add($"{an} and {bn} stand side by side in the same body ({ab})");
            }
            return faults;
        }

        // ---------------------------------------------------------------- the navigation the routes walk
        static T WithNavigation<T>(Func<T> body)
        {
            var nav=Object.FindFirstObjectByType<GroundNavigation>();if(nav==null||nav.data==null)throw new InvalidOperationException("No baked navigation in the open scene");
            var instance=NavMesh.AddNavMeshData(nav.data);
            try{return body();}finally{instance.Remove();}
        }
        static float[] Paths(List<(Vector2 a,Vector2 b,string route)> legs,Func<Vector2,float> ground=null)
        {
            ground??=p=>0;var lengths=new float[legs.Count];var path=new NavMeshPath();
            for(int i=0;i<legs.Count;i++)
            {
                var (a,b,_)=legs[i];
                if(!NavMesh.SamplePosition(new Vector3(a.x,ground(a),a.y),out var from,1.5f,NavMesh.AllAreas)||!NavMesh.SamplePosition(new Vector3(b.x,ground(b),b.y),out var to,1.5f,NavMesh.AllAreas)){lengths[i]=-1;continue;}
                if(!NavMesh.CalculatePath(from.position,to.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete){lengths[i]=-2;continue;}
                float length=0;var corners=path.corners;for(int k=1;k<corners.Length;k++)length+=Vector3.Distance(corners[k-1],corners[k]);lengths[i]=length;
            }
            return lengths;
        }
        static HashSet<string> Reachable()
        {
            var result=new HashSet<string>();var start=Object.FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None).First(s=>s.id=="Arrival").transform.position;
            if(!NavMesh.SamplePosition(start,out var from,2,NavMesh.AllAreas))return result;var path=new NavMeshPath();
            foreach(var prompt in Object.FindObjectsByType<InteractionPrompt>(FindObjectsSortMode.None))
            {
                var at=prompt.transform.position;bool ok=false;
                for(float r=.6f;r<=prompt.range&&!ok;r+=.4f)for(int i=0;i<16&&!ok;i++)
                {
                    float a=i*Mathf.PI/8;var probe=at+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);
                    if(!NavMesh.SamplePosition(probe,out var hit,2f,NavMesh.AllAreas))continue;
                    var flat=hit.position-at;flat.y=0;var drift=hit.position-probe;drift.y=0;if(flat.magnitude>prompt.range||drift.magnitude>.4f)continue;
                    ok=NavMesh.CalculatePath(from.position,hit.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
                }
                if(ok)result.Add(prompt.GetType().Name+" "+prompt.name+" "+at);
            }
            return result;
        }
        static IEnumerable<string> NavigationKept((float[] paths,HashSet<string> reach) before,(float[] paths,HashSet<string> reach) after)
        {
            for(int i=0;i<before.paths.Length;i++)
                if(Mathf.Abs(before.paths[i]-after.paths[i])>.05f)yield return $"route leg {i} walked {before.paths[i]:0.00} m before and {after.paths[i]:0.00} m with the town's people standing";
            foreach(var prompt in before.reach)if(!after.reach.Contains(prompt))yield return prompt+" can no longer be reached";
        }
    }
}
