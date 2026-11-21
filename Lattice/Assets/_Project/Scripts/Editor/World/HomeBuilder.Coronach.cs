using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lattice.Data;
using Lattice.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace Lattice.EditorTools
{
    /// <summary>The Coronach walk and the launch (brief, story beats 8 and 9; manifest section 1, "The Coronach"; design and contract: docs/maps/DecksLeaving.md, "The Coronach" and "The launch"),
    /// staged into DecksLeaving by the Decks' own build (<see cref="Decks"/>.Build calls <see cref="Coronach.Stage"/> before its bake). The Coronach engineer's file. Rebuild with
    /// <c>-home-zones DecksLeaving</c>. Its people, lanterns and points are <see cref="CoronachDoor"/>s under one root, the walk as a whole a <see cref="CoronachWalk"/>, the launch a
    /// <see cref="CoronachLaunch"/>. It reads where everything is from the Decks' published places and never repeats them; a place the Decks hasn't published stops the build by name.</summary>
    public static partial class HomeBuilder
    {
        internal static class Coronach
        {
            /// <summary>The walk's music and the launch's (docs/music/NEW_TRACKS.md, cues 1 and 2). Stand-ins until the composer's pieces are in docs/music/tracks.json: <b>the swap is this line</b>.
            /// Old Village is the sparest track the game has (docs/maps/DecksLeaving.md, "Music"); Title Theme is the game's own, for the moment it is named for.</summary>
            internal const string WalkMusic="Old Village",LaunchMusic="Title Theme";
            internal const string Root="The Coronach",LaunchRoot="The launch";
            internal const string Called="home.coronachCalled",Walked="home.coronachWalked",Launched="home.launched",DoorsHeard="coronach.doorsHeard",LastHeard="coronach.lastHeard";
            /// <summary>The seven needed doors (Dace's is not one).</summary>
            internal static readonly string[] Needed={"coronach.mira","coronach.orrin","coronach.hal","coronach.neve","coronach.oda","coronach.jun","coronach.ilo"};
            internal static readonly string[] Last={"coronach.taren","coronach.sela","coronach.idra"};
            const string Listen="Stop and listen",Yours="Say yours",Close="Stand with Orrin";
            /// <summary>How far a body stands off a declared way's middle, and from anyone else placed.</summary>
            const float WayClear=1.7f,Apart=1.25f;
            /// <summary>The walk's own ways: to the doors the Decks' ways don't reach, each from a point on one of them to where a player stands to hear the door (the stands of
            /// docs/maps/DecksLeaving.md, "For route writers"). Nobody of the walk stands on them, as on the Decks' own (<see cref="Clear"/>, <see cref="OffTheWays"/>), and the routes
            /// walk them (tools/home_routes.py reads them here). Without them the dock office's front was nobody's way: the doors' prompts pushed Jask 4.4 m off his stair onto it, and
            /// the first chain to play the walk stopped against him there, on its way to Oda and for good on its way back to Orrin's board (route 60, November 14).</summary>
            internal static readonly Vector2[][] DoorWays=
            {
                new[]{new Vector2(-28,1.6f),new Vector2(-32.7f,1.6f),new Vector2(-34.4f,3)},   // off gate six's way across the dock office's front, south of its crated benches, to Oda at the stamping window
                new[]{new Vector2(-30.6f,6.8f),new Vector2(-30.6f,8.3f),new Vector2(-31,9)},   // off the meeting's front up the office's east side to Orrin at his board
                new[]{new Vector2(-28,-13.8f),new Vector2(-29.9f,-14.9f)},                     // off gate six's way to Sela's last word at the gate
                new[]{new Vector2(0,-11),new Vector2(-1.7f,-12.4f)},                           // off lock nine's way to Neve at the lock
            };
            /// <summary>Every way a body keeps off: the Decks' and the walk's own.</summary>
            static IEnumerable<Vector2[]> AllWays=>ways.Concat(DoorWays);
            static string Done(string quest)=>"quest."+quest+".complete";

            // ---------------------------------------------------------------- the Decks' places (docs/maps/DecksLeaving.md, "For the Coronach")
            /// <summary>A door: the thing a neighbour looks at, and where they stand to look at it.</summary>
            internal readonly struct Spot{public readonly Vector2 at,stand;public Spot(Vector2 at,Vector2 stand){this.at=at;this.stand=stand;}}
            static FieldInfo Field(string name)=>typeof(Decks).GetField(name,BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
            static object Value(string name)=>(Field(name)??throw new InvalidOperationException("CORONACH_PLACE_UNPUBLISHED "+name+": the Decks hasn't published it (docs/maps/DecksLeaving.md, \"For the Coronach\")")).GetValue(null);
            static Vector2 Flat(object value,string name)=>value switch
            {
                Vector2 v=>v,Vector3 v=>new Vector2(v.x,v.z),ValueTuple<Vector2,Vector2> pair=>pair.Item1,ValueTuple<Vector2,float> wide=>wide.Item1,
                _=>throw new InvalidOperationException("CORONACH_PLACE_SHAPE "+name+" is "+value?.GetType().Name+": a point is a Vector2 on the plan"),
            };
            static Vector2 Point(string name)=>Flat(Value(name),name);
            static Vector2? Maybe(string name)=>Field(name)!=null?Point(name):null;
            static Spot Door(string name)
            {
                var value=Value(name);
                if(value is ValueTuple<Vector2,Vector2> pair)return new Spot(pair.Item1,pair.Item2);
                // A thing with a length (the dock rail: its two ends) and where its person stands at it.
                if(value is ValueTuple<Vector2,Vector2,Vector2> run)return new Spot((run.Item1+run.Item2)*.5f,run.Item3);
                return new Spot(Flat(value,name),Point(name+"Stand"));
            }
            static float Width(string name,float otherwise)=>Field(name)?.GetValue(null) switch{ValueTuple<Vector2,float> wide=>wide.Item2,float w=>w,_=>otherwise};
            static (Vector2 a,Vector2 b) Line(string name)
            {
                var value=Field(name)!=null?Value(name):null;
                if(value is ValueTuple<Vector2,Vector2> pair)return (pair.Item1,pair.Item2);
                return (Point(name+"A"),Point(name+"B"));
            }
            static List<Vector2[]> Lines(string name)
            {
                var found=new List<Vector2[]>();var value=Field(name)?.GetValue(null);if(value==null)return found;
                foreach(var line in (IEnumerable)value)
                    found.Add(line switch{ValueTuple<Vector2,Vector2> pair=>new[]{pair.Item1,pair.Item2},IEnumerable points=>points.Cast<object>().Select(p=>Flat(p,name)).ToArray(),_=>Array.Empty<Vector2>()});
                return found.Where(l=>l.Length>1).ToList();
            }

            // ---------------------------------------------------------------- staging
            static List<Vector2[]> ways;static readonly List<Vector2> taken=new();
            /// <summary>Where the ten prompts a player has to press (the seven doors, the three last words) and the close are offered: everyone else of the walk stands, and is spoken to,
            /// at least <see cref="PromptApart"/> from each, while <see cref="apart"/> is set. A guest's prompt stood on the door they looked at (Abri's a metre from Ilo's), and from
            /// where a player stood to press one, the other was offered.</summary>
            static readonly List<Vector2> held=new(),heldBy=new();static float apart;
            /// <summary>Where the person of a door, a last word or the close stands (Orrin, who walks, at his post): a player walks up beside them, so a ring round them is where they press
            /// too.</summary>
            static Vector2? Person(CoronachDoor door)=>door.body!=null?Flat(door.body.transform.position):door.post!=null?Flat(door.post.position):null;
            /// <summary>The ring a player stands on to press one of them (CoronachTests: 1 and 1.75 m off), and how far a guest or someone there for their story is spoken to from: a
            /// person is walked up to. From anywhere on the ring anything else that can be pressed is out of its reach or a metre further off than the walk's own: a door's reach is 3, so
            /// another door 4.6 m off (twice 1.75 and a metre, and a little); a guest's 2.2, so a guest 4 m off (1.75 and 2.2, and a little).</summary>
            const float RingOut=1.75f,BodyRing=1.2f,GuestReach=2.2f,DoorApart=4.6f,PromptApart=4f;
            static float Needs(float reach)=>Mathf.Min(2*RingOut+1,RingOut+reach);
            /// <summary>The town's floor signs, where they lie and how far their words run either side: nobody of the walk stands on one (Abri stood on NOTICE BOARD, its words across her).</summary>
            static readonly List<(Vector2 at,float half)> signs=new();
            static Material brass,glass,crystal,timber;static Mesh frame,pane;
            /// <summary>The yellow line, and the way across it to the side that stays (the walkway hatch's side: the office's).</summary>
            static Vector2 lineA,lineB,across,along,mid;
            internal static void Stage(ZoneKit kit)
            {
                taken.Clear();held.Clear();apart=0;ways=Lines("Ways");signs.Clear();
                // The walk's people never stand with the meeting's room (it is gone at the vote), so they are placed with it away; it is put back as it was at the end.
                meetingRoom=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.parent==null&&t.name==Decks.MeetingRoot)?.gameObject;
                bool roomWas=meetingRoom!=null&&meetingRoom.activeSelf;if(meetingRoom!=null)meetingRoom.SetActive(false);
                try{StageWalk(kit);}finally{if(meetingRoom!=null)meetingRoom.SetActive(roomWas);}
            }
            static void StageWalk(ZoneKit kit)
            {
                foreach(var sign in Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    if(sign.name.StartsWith("Sign_")&&!string.IsNullOrEmpty(sign.text)&&sign.GetComponentInParent<Npc>(true)==null&&sign.transform.position.y<1.5f)
                        signs.Add((new Vector2(sign.transform.position.x,sign.transform.position.z),sign.text.Length*.14f));
                (lineA,lineB)=Line("YellowLine");mid=(lineA+lineB)*.5f;along=(lineB-lineA).normalized;across=new Vector2(-along.y,along.x);
                if(Vector2.Dot(Point("WalkwayHatch")-lineA,across)<0)across=-across;
                ArenaBuilder.Asset<ZoneDef>(DecksZone,z=>z.cueMusicIds=new[]{WalkMusic,LaunchMusic});
                Materials();Lantern(kit);
                var root=new GameObject(Root);
                var walk=new GameObject("The Coronach walk",typeof(CoronachWalk)).GetComponent<CoronachWalk>();walk.transform.SetParent(root.transform,false);
                walk.music=WalkMusic;walk.doors=Needed;
                // Nothing that leaves the walk or belongs to a story already over is pressable during it: the Halo's three docks (gate six's was nearer than Sela's last word and a press
                // launched for the Halo mid-walk) and the still things Tam is shown (Tam is aboard).
                // Hal's folding bench and his crated console go with him at the call, and only out of sight: switched off at once, they are never pressed beside him at his bolt holes.
                walk.shut=Object.FindObjectsByType<DockingPad>(FindObjectsInactive.Include,FindObjectsSortMode.None).Cast<Behaviour>()
                    .Concat(Object.FindObjectsByType<Bench>(FindObjectsInactive.Include,FindObjectsSortMode.None)).Concat(Object.FindObjectsByType<RepairBay>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    .Concat(Object.FindObjectsByType<DiscoveryPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(p=>p.transform.parent!=null&&p.transform.parent.name=="Three still things")).ToArray();
                var town=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="The Decks carrying things");
                walk.town=town!=null?town.Cast<Transform>().Select(t=>t.gameObject).ToArray():Array.Empty<GameObject>();
                if(town==null)Debug.LogWarning("CORONACH_TOWN_UNNAMED: no 'The Decks carrying things' in the scene, so the town doesn't thin as the doors are heard (docs/maps/DecksLeaving.md, \"For the Coronach\")");
                All(root,"The seven doors are heard",DoorsHeard,Needed);
                All(root,"The three have said theirs",LastHeard,Last);
                Orrin(kit,root);
                Doors(kit,root);
                // The heroes' places before the guests': Taren's is the walkway joint of the first scene, and a guest placed first took the room beside it (he stood past the line).
                Heroes(kit,root);
                held.Clear();heldBy.Clear();
                foreach(var door in root.GetComponentsInChildren<CoronachDoor>(true))if(door.point!=null&&(Needed.Contains(door.point.flag)||Last.Contains(door.point.flag)||door.point.flag==Walked))
                {held.Add(Flat(door.point.transform.position));if(Person(door) is Vector2 by)heldBy.Add(by);}
                apart=PromptApart;
                Guests(kit,root);
                Stories(kit,root);
                apart=0;
                PromptsApart(root,walk.shut);
                DoorWaysLead(root);
                Launch(kit,root);
                OffTheWays(root);
                // Nobody of the walk is in the day's paths: hidden until the call (and for good, someone whose story wasn't done), each carves their own while they stand, as the
                // meeting does. Baked, the two placed on the walkway bridge left holes in its floor all day, edges a walker could step off (QaPlanWorldTests.NoWalkableEdgeDropsIntoNothing).
                foreach(var door in root.GetComponentsInChildren<CoronachDoor>(true))
                {
                    if(door.body!=null)Carves(door.body);
                    foreach(var extra in door.extras)if(extra.shows!=null)foreach(var thing in extra.shows)if(thing!=null)Carves(thing);
                }
                // Saved switched off, so an editor still shows the day as the Decks built it (the walk switches them on as play begins); the town's ordinary crates stay drawn.
                walk.drawn=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.GetComponentInParent<CoronachDoor>(true)!=null||r.enabled&&r.GetComponentInParent<Whereabouts>(true)!=null).ToArray();
                walk.lit=root.GetComponentsInChildren<Light>(true).Where(l=>l.enabled).ToArray();
                foreach(var r in walk.drawn)r.enabled=false;foreach(var l in walk.lit)l.enabled=false;
                Debug.Log($"CORONACH_STAGED places={root.GetComponentsInChildren<CoronachDoor>(true).Length} town={walk.town.Length}");
            }
            static void All(GameObject root,string name,string result,string[] flags)
            {
                var all=new GameObject(name,typeof(FlagAll)).GetComponent<FlagAll>();all.transform.SetParent(root.transform,false);all.flags=flags;all.result=result;
            }
            /// <summary>A holder that shows its targets from a flag, decided at load (nobody blinks in) or live; it stays active itself, as a FlagGate's holder does.</summary>
            static GameObject From(GameObject parent,string name,string flag,bool live,params GameObject[] targets)
            {
                var holder=new GameObject(name,typeof(Whereabouts));holder.transform.SetParent(parent.transform,false);
                var where=holder.GetComponent<Whereabouts>();where.from=flag;where.live=live;where.targets=targets;return holder;
            }

            // ---------------------------------------------------------------- the call, the board, the line: Orrin
            /// <summary>Orrin, from the moment the Saint is let go: waiting on the promenade outside Hal's workshop, giving Hal room while he takes his machine apart (there when the party comes up
            /// from the Works, decided at load, so nobody sees him appear). He calls the Coronach to whoever comes out of the workshop, walks to his board, and once his own door is said, to the
            /// walkway joint to wait for the last of it. One body, a <see cref="Guide"/>, and three places: the call, his board and his close, each offered only while he stands at it.</summary>
            static void Orrin(ZoneKit kit,GameObject root)
            {
                var hall=Point("HalsDoor");var board=Door("Board");var hulls=(float[])Value("Hulls");
                var calls=Free(kit,"Orrin waiting outside Hal's",hall,Maybe("OrrinWaits")is Vector2 w?w-hall:new Vector2(4,-1),5);
                var waits=Free(kit,"Orrin at the last joint",Back(Point("WalkwayHatch"),3.2f),Vector2.zero,3);
                var body=Person(kit,"Orrin",calls,hall,"Orrin, the Coronach's");
                foreach(var c in body.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);foreach(var t in body.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=false;
                var guide=body.AddComponent<Guide>();guide.speed=1.9f;guide.patience=4;
                guide.legs=new[]
                {
                    new Guide.Leg{flag="",path=new[]{kit.On(calls.x,calls.y)}},
                    new Guide.Leg{flag=Called,path=Route(calls,board.stand).Select(p=>On(kit,p)).ToArray()},
                    new Guide.Leg{flag="coronach.orrin",path=Route(board.stand,waits).Select(p=>On(kit,p)).ToArray()},
                };
                guide.faces=new[]{On(kit,hall),On(kit,hall),On(kit,board.at),On(kit,mid)};
                var holder=From(root,"Orrin, from the Saint's release","home.saintReleased",false,body);body.transform.SetParent(holder.transform,true);body.SetActive(false);

                // The call: a crossing the whole width of the workshop's open front, so whoever comes out of Hal's hears it. The point stands on Orrin, so the hero turns to him.
                float front=Mathf.Max(Width("HalsDoor",8),23);
                var call=Place(kit,root,"Orrin calls the Coronach","Talk to Orrin",Called,"OrrinCallsCoronach","Orrin",calls,calls,null,lamp:false);
                call.calledFlag="home.halChose";call.stays=true;call.arrives=body.transform;call.post=Post(kit,call,"Orrin's place outside Hal's",calls);
                call.point.startsOnCrossing=true;call.point.range=2.5f;
                var mouth=call.point.gameObject.AddComponent<BoxCollider>();mouth.isTrigger=true;var toFront=new Vector2(hulls[2],hall.y)-calls;mouth.center=new Vector3(toFront.x,1.5f,toFront.y);mouth.size=new Vector3(front,3,2.2f);

                var atBoard=Place(kit,root,"Orrin, at his board",Listen,"coronach.orrin","OrrinBoardDoor","Orrin",board.at,board.stand,null);
                atBoard.stays=true;atBoard.arrives=body.transform;atBoard.post=Post(kit,atBoard,"Orrin's place at the board",board.stand);
                var close=Place(kit,root,"Orrin, at the last joint",Close,Walked,"OrrinGiveItRoom","Orrin",mid,waits,null);
                close.stays=true;close.opensWith=LastHeard;close.arrives=body.transform;close.post=Post(kit,close,"Orrin's place at the line",waits);close.point.range=2.4f;
                // The point stands between Orrin and the line on the side that stays, so whoever stands with him is already back from it.
                var stand=Vector2.Lerp(waits,mid,.3f);close.point.transform.position=On(kit,stand);
            }
            static Transform Post(ZoneKit kit,CoronachDoor door,string name,Vector2 at){var post=new GameObject(name).transform;post.position=On(kit,at);post.SetParent(door.transform,true);return post;}
            static Vector3 On(ZoneKit kit,Vector2 at)=>kit.On(at.x,at.y);
            /// <summary>A walk along the Decks' declared ways from one place to another: onto the nearest way, along the ways (crossing from one to another where they meet), and off to the
            /// goal. A Guide walks straight lines and only a body stops it, so a straight line from the workshop to the board walked Orrin through three bulkheads.</summary>
            static Vector2[] Route(Vector2 from,Vector2 to)
            {
                var legs=ways.SelectMany(w=>Enumerable.Range(1,w.Length-1).Select(i=>(a:w[i-1],b:w[i]))).ToList();
                var stops=legs.Select(l=>new List<Vector2>{l.a,l.b}).ToArray();
                for(int i=0;i<legs.Count;i++)for(int j=i+1;j<legs.Count;j++)if(Cross(legs[i],legs[j],out var x)){stops[i].Add(x);stops[j].Add(x);}
                static Vector2 Near(Vector2 p,(Vector2 a,Vector2 b) l){var d=l.b-l.a;float t=Mathf.Clamp01(Vector2.Dot(p-l.a,d)/Mathf.Max(1e-6f,d.sqrMagnitude));return l.a+d*t;}
                Vector2 Onto(Vector2 p){int best=0;float d=float.MaxValue;for(int i=0;i<legs.Count;i++){float g=Vector2.Distance(p,Near(p,legs[i]));if(g<d){d=g;best=i;}}var at=Near(p,legs[best]);stops[best].Add(at);return at;}
                var onFrom=Onto(from);var onTo=Onto(to);
                var points=new List<Vector2>();var links=new List<(int a,int b)>();
                int Node(Vector2 p){for(int i=0;i<points.Count;i++)if((points[i]-p).sqrMagnitude<.0025f)return i;points.Add(p);return points.Count-1;}
                for(int i=0;i<legs.Count;i++){var a=legs[i].a;var sorted=stops[i].OrderBy(p=>Vector2.Distance(a,p)).ToList();for(int k=1;k<sorted.Count;k++)links.Add((Node(sorted[k-1]),Node(sorted[k])));}
                int start=Node(onFrom),goal=Node(onTo);
                var dist=Enumerable.Repeat(float.MaxValue,points.Count).ToArray();var prev=Enumerable.Repeat(-1,points.Count).ToArray();var done=new bool[points.Count];dist[start]=0;
                while(true)
                {
                    int u=-1;float least=float.MaxValue;for(int i=0;i<points.Count;i++)if(!done[i]&&dist[i]<least){least=dist[i];u=i;}
                    if(u<0||u==goal)break;done[u]=true;
                    foreach(var (a,b) in links){int v=a==u?b:b==u?a:-1;if(v<0)continue;float through=dist[u]+Vector2.Distance(points[u],points[v]);if(through<dist[v]){dist[v]=through;prev[v]=u;}}
                }
                if(dist[goal]==float.MaxValue)throw new InvalidOperationException("CORONACH_NO_ROUTE from "+from+" to "+to+" along the Decks' ways");
                var route=new List<Vector2>{from};var back=new List<Vector2>();for(int v=goal;v>=0;v=prev[v])back.Add(points[v]);back.Reverse();route.AddRange(back);route.Add(to);
                var kept=new List<Vector2>();foreach(var p in route)if(kept.Count==0||Vector2.Distance(p,kept[^1])>.05f)kept.Add(p);
                return kept.ToArray();
            }
            static bool Cross((Vector2 a,Vector2 b) p,(Vector2 a,Vector2 b) q,out Vector2 at)
            {
                at=default;var r=p.b-p.a;var s=q.b-q.a;float d=r.x*s.y-r.y*s.x;if(Mathf.Abs(d)<1e-6f)return false;
                var qp=q.a-p.a;float t=(qp.x*s.y-qp.y*s.x)/d,u=(qp.x*r.y-qp.y*r.x)/d;
                if(t<-.001f||t>1.001f||u<-.001f||u>1.001f)return false;at=p.a+r*t;return true;
            }

            // ---------------------------------------------------------------- the doors
            static void Doors(ZoneKit kit,GameObject root)
            {
                var stall=Door("PitchNine");var pears=Point("PearCrate");
                // Mira's stall: her lantern has six Ridge Crystals in it; the Arrens carry their table top past in two halves as her stop begins; she keeps the awning.
                var mira=Place(kit,root,"Mira, at pitch nine",Listen,"coronach.mira","MiraStallDoor","Mira",stall.at,stall.stand,"Mira");
                var crown=Crystals(kit,mira.lamp.transform);
                var halves=TableHalves(kit,root,stall);
                mira.extras=new[]
                {
                    new CoronachDoor.Extra{node="MiraDoorCrystals",needs=Done("MiraCrystals"),shows=new[]{crown}},
                    new CoronachDoor.Extra{node="MiraDoorTable",needs=Done("SecondInventory"),carried=halves.carriers,path=halves.path,pace=.75f},
                };
                mira.takes=Named("Mira's awning");

                var bolts=Door("BoltHoles");
                var hal=Place(kit,root,"Hal, at the bolt holes",Listen,"coronach.hal","HalBenchDoor","Hal",bolts.at,bolts.stand,"Hal");
                hal.extras=new[]{new CoronachDoor.Extra{node="HalDoorBenches",needs=Done("PortableBenches")}};hal.shuts=Named("Hal's shutter");
                // Hal is in his workshop as Orrin calls the walk outside it: he takes over from the day's own Hal where that Hal stands, and turns from the party to his bolt holes.
                var dayHal=Object.FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(n=>n.speaker=="Hal").OrderBy(n=>Vector2.Distance(Flat(n.transform.position),bolts.stand)).FirstOrDefault();
                hal.handsOver=true;if(dayHal!=null&&Vector2.Distance(Flat(dayHal.transform.position),bolts.stand)<1)hal.turnsFrom=dayHal.transform.eulerAngles.y;

                var lock9=Door("LockNine");
                var neve=Place(kit,root,"Neve, at lock nine",Listen,"coronach.neve","NeveLockDoor","Neve",lock9.at,lock9.stand,"Neve");
                neve.extras=new[]{new CoronachDoor.Extra{node="NeveDoorLetter",needs=Done("ReturnAddress")}};

                var window=Door("StampingWindow");
                var oda=Place(kit,root,"Oda, at the stamping window",Listen,"coronach.oda","OdaOfficeDoor","Oda",window.at,window.stand,"OdaBody");
                oda.extras=new[]{new CoronachDoor.Extra{node="OdaDoorStamp",needs=Done("OdaBerth")}};oda.shuts=Named("The stamping window's shutter");

                // Jun stands on the market side of the line, looking down at it, her pears one crate past it: chapter 1's first scene, answered.
                // At its south end, by the promenade's wall, and spoken to where she stands: at the stall end her door and Mira's were 0.7 m apart, and a player at one was given the other.
                held.Add(Flat(mira.point.transform.position));apart=DoorApart;
                var lineAt=lineA+along*.5f;var junAt=Free(kit,"Jun at the line",lineAt-across*.9f,Vector2.zero,2.5f);apart=0;
                var jun=OnThem(Place(kit,root,"Jun, at the yellow line",Listen,"coronach.jun","JunLineDoor","Jun",lineAt,junAt,"JunBody"));
                jun.point.range=3;jun.takes=Named("Jun's pears");

                var notices=Door("NoticeBoard");
                var ilo=Place(kit,root,"Ilo, at the notice board",Listen,"coronach.ilo","IloNoticeDoor","Ilo",notices.at,notices.stand,"IloBody");
                ilo.extras=new[]{new CoronachDoor.Extra{node="IloDoorLedger",needs=Done("ClaimsLedger")}};

                var rail=Door("DockRail");
                var dace=Place(kit,root,"Dace, at the dock rail",Listen,"coronach.dace","SurvivorDockDoor","Survivor",rail.at,rail.stand,"SurvivorNatural");
                dace.extras=new[]{new CoronachDoor.Extra{node="SurvivorDoorSocks",needs=Done("NeveParcel")},new CoronachDoor.Extra{node="SurvivorDoorBeacons",needs=Done("SorrelBeacons")}};
            }
            static GameObject[] Named(string name)=>Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(t=>t.name==name).Select(t=>t.gameObject).ToArray();

            // ---------------------------------------------------------------- the guests, who have no door and no lantern
            static void Guests(ZoneKit kit,GameObject root)
            {
                var office=Point("DockOfficeDoor");var pears=Point("PearCrate");var board=Door("Board");var hall=Point("HalsDoor");var notices=Door("NoticeBoard");
                // Pell under the lamp she has lit over the dock office (her mother's, if it came home: the same lamp, so nothing is missing without it), the Oriel flame set down beside her.
                var pell=Guest(kit,root,"Pell, under her lamp","coronach.pell","PellEdge","Pell",Free(kit,"Pell",office,new Vector2(1.4f,-1.6f),4),office,"PellBody");
                OverTheDoor(kit,root,office);
                var flame=Lantern(kit,"The Oriel flame",Beside(pell.body,.9f),new Color(.86f,.94f,1),1.2f).transform.parent.gameObject;flame.transform.SetParent(pell.transform,true);
                pell.extras=new[]{new CoronachDoor.Extra{node="PellDoorMothersLamp",needs=Done("PellsLamp")},new CoronachDoor.Extra{node="PellDoorOriel",needs=Done("LumesLantern"),shows=new[]{flame}}};
                // Ruva by the fruit stall; the Skyfields' gardener beside her with its light low, if the gardens were filled.
                var ruvaAt=Free(kit,"Ruva",pears,new Vector2(-1.8f,-1.6f),4);
                var ruva=Guest(kit,root,"Ruva, by the fruit stall","coronach.ruva","RuvaEdge","Ruva",ruvaAt,pears,"RuvaBody");
                var gardener=Person(kit,"Gardener",Free(kit,"the gardener",ruvaAt,new Vector2(1.6f,.4f),3),pears,"The gardener, its light low");gardener.transform.SetParent(ruva.transform,true);
                ruva.extras=new[]{new CoronachDoor.Extra{node="RuvaDoorSenn",needs=Done("MooringPost")},
                    new CoronachDoor.Extra{node="RuvaDoorGardener",needs=Done("EmptyGardens"),shows=new[]{gardener},dim=gardener.GetComponentsInChildren<Renderer>(),dimTo=.18f}};
                // Maru at the walkway's end, on the side that stays. Her lamps count two short and one long at each end of the walkway from the call, if the beacon was kept, and stay lit
                // after she has gone aboard: they are for the halves, not for her.
                var maru=Guest(kit,root,"Maru, at the walkway's end","coronach.maru","MaruEdge","Maru",Free(kit,"Maru",Back(Point("WalkwayHatch"),5.5f),new Vector2(0,2.2f),4),mid,"MaruBody");
                maru.extras=new[]{new CoronachDoor.Extra{node="MaruDoorLamps",needs=Done("BeaconKeeper")}};
                WalkwayLamps(kit,root);
                // Hask across from the board, Tovra by Hal's workshop door, Dorrow by a chalked joint with his goggles up, Abri by the notice board, writing.
                var hask=Guest(kit,root,"Hask, across from the board","coronach.hask","HaskEdge","Hask",Free(kit,"Hask",board.stand+(board.stand-board.at).normalized*4.5f,Vector2.zero,4),board.at,"HaskBody");
                hask.extras=new[]{new CoronachDoor.Extra{node="HaskDoorStrays",needs=Done("StrayBeasts")}};
                var tovraAt=Free(kit,"Tovra",hall,new Vector2(-2.6f,-2),4);
                var tovra=Guest(kit,root,"Tovra, by Hal's door","coronach.tovra","TovraEdge","Tovra",tovraAt,hall,"TovraBody");
                tovra.extras=new[]{new CoronachDoor.Extra{node="TovraDoorPikes",needs=Done("QuietHands"),shows=Smiths(kit,tovra,tovraAt,hall)}};
                // Dorrow where the day had him, bent over the hatch by the walkway with his ear to the station; on the walk, standing, goggles up, still listening to it.
                var dayDorrow=Object.FindObjectsByType<Npc>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(n=>n.speaker=="Dorrow");
                var listens=dayDorrow!=null?Flat(dayDorrow.transform.position+dayDorrow.transform.forward*1.4f):(Lines("ChalkedJoints").Select(j=>(j[0]+j[^1])*.5f).OrderBy(j=>Vector2.Distance(j,mid)).Skip(1).FirstOrDefault());
                var dorrowAt=dayDorrow!=null?Free(kit,"Dorrow",Flat(dayDorrow.transform.position),Vector2.zero,3):Free(kit,"Dorrow",listens,new Vector2(1.2f,-1.2f),4);
                Guest(kit,root,"Dorrow, listening to the station","coronach.dorrow","DorrowEdge","Dorrow",dorrowAt,listens,"DorrowBody");
                var abri=Guest(kit,root,"Keeper Abri, by the notice board","coronach.abri","AbriEdge","Keeper",Free(kit,"Abri",notices.stand,new Vector2(1.8f,-.8f),4),notices.at,"KeeperNatural");
                abri.extras=new[]{new CoronachDoor.Extra{node="AbriDoorLanterns",needs=Done("RelayGlass")}};
            }

            // ---------------------------------------------------------------- people here only for their own stories
            static void Stories(ZoneKit kit,GameObject root)
            {
                var stall=Door("PitchNine");var board=Door("Board");var rail=Door("DockRail");var office=Point("DockOfficeDoor");var hall=Point("HalsDoor");
                var promenade=Maybe("Promenade")??(stall.stand+board.stand)*.5f;
                var ishenAt=Free(kit,"Ishen",promenade,new Vector2(0,-2.4f),5);
                var ishenCrate=Aside(kit,"Ishen's crate",ishenAt,new Vector2(.7f,.35f));Thing(kit,root,"A crate, written on","DeckCrate",ishenCrate,.8f);
                Story(kit,root,"Ishen, taking it down","coronach.ishen","Ishen",ishenAt,ishenCrate,"IshenBody",("IshenDoorRecord","IshensRecord"),("IshenDoorQuill","FourthPump"),("IshenDoorMoons","MoonNames"));
                var tovra=Of(root,"Tovra");
                var brekk=Story(kit,root,"Brekk, with Tovra","coronach.brekk","Brekk",Free(kit,"Brekk",Flat(tovra.body.transform.position),new Vector2(-1.3f,.2f),3),hall,"BrekkBody",("BrekkDoorWalked","UnswornRations"),("BrekkDoorKettle","OdrunsKitchen"));
                brekk.extras[1].shows=new[]{Kettle(kit,brekk.body.transform,"Mother Odrun's kettle")};
                var binnaAt=Free(kit,"Binna",stall.stand,new Vector2(-2.6f,-1.4f),4);
                var binnaCrate=Aside(kit,"Binna's crate",binnaAt,new Vector2(.8f,.2f));var crate=Thing(kit,root,"A crate by the stall","DeckCrate",binnaCrate,.75f);
                var binna=Story(kit,root,"Binna, by the stall","coronach.binna","Binna",binnaAt,binnaCrate,"BinnaBody",("BinnaDoorBaskets","ThreeBaskets"));
                binna.extras[0].shows=new[]{Pot(kit,crate.transform,"Binna's pot")};
                // The Decks has one stair, the dock office's two steps (the worn one Tam is shown), and Jask stands by it, looking at it ("Can I thank the stairs?"): past its top end and
                // the booth's north-east corner, on the nearest floor to it four metres off Oda's window and Orrin's board, and a metre back from the booth, which cut him off at the
                // waist from the camera at .4 m (he is 1.25 m tall). Tried first on its foot, he was pushed 4.4 m off it by those two doors' prompts, onto the dock office's front, the
                // walk's own way to Oda, and the first chain to play the walk stopped against him there (route 60, November 14).
                var stair=Maybe("PromenadeStair")??Maybe("WornStep")??Vector2.Lerp(promenade,rail.stand,.5f);
                Story(kit,root,"Jask, by the stair","coronach.jask","Jask",Free(kit,"Jask",stair,new Vector2(-1.1f,2.6f),2.4f),stair,"JaskBody",("JaskDoorQuestion","Questions"),("JaskDoorFare","StowawayFare"));
                Story(kit,root,"Teo, at the dock rail, counting","coronach.teo","Teo",Free(kit,"Teo",rail.stand,new Vector2(3.2f,0),4),rail.at+new Vector2(3.2f,0),"TeoBody",("TeoDoorTags","ClaimTags"));
                var dovAt=Free(kit,"Dov",Vector2.Lerp(promenade,stall.stand,.6f),new Vector2(0,-3),5);
                var dovCrate=Aside(kit,"Dov's crate",dovAt,new Vector2(.75f,.3f));var tray=Thing(kit,root,"A crate with a tray on it","DeckCrate",dovCrate,.75f);
                var dov=Story(kit,root,"Dov, with a tray nobody has touched","coronach.dov","Dov",dovAt,dovCrate,"DovBody",("DovDoorEel","SmokehouseOrder"),("DovDoorBell","SchoolBell"));
                dov.extras[0].shows=new[]{Tray(kit,tray.transform,"Blue eel on a tray")};dov.extras[1].sound="school_bell";dov.extras[1].volume=.34f;dov.extras[1].soundReach=7;
                var corraAt=Free(kit,"Corra",Vector2.Lerp(promenade,rail.stand,.3f),new Vector2(-3,0),5);
                var corra=Story(kit,root,"Corra, with a handcart of kettles","coronach.corra","Corra",corraAt,corraAt+new Vector2(1,0),"CorraBody",("CorraDoorKettles","KettleTrade"));
                var cart=Handcart(kit,Free(kit,"Corra's handcart",corraAt,new Vector2(1.3f,0),3),"Corra's handcart of kettles");cart.transform.SetParent(corra.transform,true);
                corra.extras[0].shows=new[]{cart};
                var pell=Of(root,"Pell");
                var iru=Story(kit,root,"Iru, with a lamp of her own","coronach.iru","Iru",Free(kit,"Iru",Flat(pell.body.transform.position),new Vector2(-1.5f,-.4f),3),office,"IruBody",("IruDoorLamp","ShrineLamps"));
                var own=Lantern(kit,"Iru's lamp",Beside(iru.body,.9f),new Color(1,.8f,.5f),1).transform.parent.gameObject;own.transform.SetParent(iru.transform,true);iru.extras[0].shows=new[]{own};
                var drain=Maybe("Drain")??Free(kit,"the drain",Vector2.Lerp(board.stand,rail.stand,.5f),new Vector2(2,0),5);
                Story(kit,root,"Pip, at attention by the drain","coronach.pip","Pip",Free(kit,"Pip",drain,new Vector2(.9f,.4f),3),drain+new Vector2(0,4),"PipBody",("PipDoorFile","PipsOath"));
                var dorrow=Of(root,"Dorrow");
                var tam=Story(kit,root,"Tam, with Dorrow","coronach.tam","Tam",Free(kit,"Tam",Flat(dorrow.body.transform.position),new Vector2(1.2f,.3f),3),Flat(dorrow.point.transform.position),"TamBody",("TamDoorEdge","StillGround"));
                // Tam's stop is at Tam: at what Tam looks at it was Dorrow's to the centimetre, two prompts on one spot.
                tam.point.transform.position=tam.body.transform.position;
            }
            /// <summary>A carving obstacle on each solid box of someone the walk shows and hides (Decks.Bake leaves carving obstacles out of its bake).</summary>
            static void Carves(GameObject who)
            {
                foreach(var box in who.GetComponentsInChildren<BoxCollider>(true))
                {
                    if(box.isTrigger||box.GetComponent<UnityEngine.AI.NavMeshObstacle>()!=null)continue;
                    var cut=box.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();cut.shape=UnityEngine.AI.NavMeshObstacleShape.Box;cut.center=box.center;cut.size=box.size;cut.carving=true;cut.carveOnlyStationary=true;
                }
            }
            /// <summary>The build's own reading of what the ring test asks (CoronachTests.EachDoorLastWordAndTheCloseIsThePromptOfferedAllRoundIt): from each door, last word and the close,
            /// nothing else that can be offered at the same time is within <see cref="PromptApart"/>: the walk's other people, the day's own prompts left on through the walk (not its
            /// people, all aboard at the call, nor the meeting's room, nor what the walk shuts), and the others of its own kind. Throws CORONACH_PROMPTS_CROWDED.</summary>
            static void PromptsApart(GameObject root,Behaviour[] shut)
            {
                var places=root.GetComponentsInChildren<CoronachDoor>(true).Where(d=>d.point!=null).ToArray();
                var groups=new[]{Needed,Last,new[]{Walked}};var ours=groups.SelectMany(g=>g).Append(Called).ToHashSet();
                var others=places.Where(d=>!ours.Contains(d.point.flag)).Select(d=>(name:d.name,at:Flat(d.point.transform.position),reach:d.point.range)).ToList();
                foreach(var p in Object.FindObjectsByType<Lattice.Core.InteractionPrompt>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    if(p.range>0&&p is not Npc&&p.GetComponentInParent<CoronachDoor>(true)==null&&!shut.Contains(p)&&!p.GetComponentsInParent<Transform>(true).Any(t=>t.name==Decks.MeetingRoot))
                        others.Add((p.name+" (\""+p.prompt+"\")",Flat(p.transform.position),p.range));
                var faults=new List<string>();
                foreach(var group in groups)foreach(string flag in group)
                {
                    var place=places.Single(d=>d.point.flag==flag);var at=Flat(place.point.transform.position);
                    var near=others.Concat(places.Where(d=>d!=place&&group.Contains(d.point.flag)).Select(d=>(name:d.name,at:Flat(d.point.transform.position),reach:d.point.range)));
                    foreach(var (name,there,reach) in near)if(Vector2.Distance(at,there)<Needs(reach))faults.Add($"{place.name} at {at}: {name} is {Vector2.Distance(at,there):0.00} m off (needs {Needs(reach):0.00})");
                    // And from beside the person, out of the other's reach.
                    if(Person(place) is Vector2 by)foreach(var (name,there,reach) in near)if(Vector2.Distance(by,there)<BodyRing+reach+.2f)
                        faults.Add($"{place.name}, beside them at {by}: {name} is {Vector2.Distance(by,there):0.00} m off (needs {BodyRing+reach+.2f:0.00})");
                }
                if(faults.Count>0)throw new InvalidOperationException("CORONACH_PROMPTS_CROWDED\n"+string.Join("\n",faults));
                Debug.Log("CORONACH_PROMPTS_APART "+groups.Sum(g=>g.Length));
            }
            /// <summary>Each of the walk's own ways leaves one of the Decks' and ends where one of the ten doors is heard, well inside its reach, on floor nothing solid stands on: a way
            /// that has drifted off its door, or a door moved off its way, stops the build (CORONACH_DOOR_WAY).</summary>
            static void DoorWaysLead(GameObject root)
            {
                var doors=root.GetComponentsInChildren<CoronachDoor>(true).Where(d=>d.point!=null&&(Needed.Contains(d.point.flag)||Last.Contains(d.point.flag))).ToArray();
                var faults=new List<string>();Physics.SyncTransforms();
                foreach(var way in DoorWays)
                {
                    if(ways.Min(w=>ZoneKit.FromPath(way[0],w))>.05f)faults.Add($"the way {way[0]} > {way[^1]} doesn't leave one of the Decks' ways");
                    var door=doors.OrderBy(d=>Vector2.Distance(Flat(d.point.transform.position),way[^1])).First();
                    if(Vector2.Distance(Flat(door.point.transform.position),way[^1])>door.point.range-.6f)faults.Add($"the way {way[0]} > {way[^1]} ends {Vector2.Distance(Flat(door.point.transform.position),way[^1]):0.00} m from {door.name}, which reaches {door.point.range}");
                    for(int i=1;i<way.Length;i++)
                        if(Physics.CheckCapsule(new Vector3(way[i].x,.4f,way[i].y),new Vector3(way[i].x,1.55f,way[i].y),.35f,~0,QueryTriggerInteraction.Ignore))faults.Add($"something solid stands at {way[i]} on the way to {door.name}");
                }
                if(faults.Count>0)throw new InvalidOperationException("CORONACH_DOOR_WAY\n"+string.Join("\n",faults));
            }
            static CoronachDoor Of(GameObject root,string speaker)=>root.GetComponentsInChildren<CoronachDoor>(true).First(d=>d.speaker==speaker&&d.body!=null);
            /// <summary>Someone whose only lines are extras: there only if one of their stories was finished, saying the first of them and then the rest.</summary>
            static CoronachDoor Story(ZoneKit kit,GameObject root,string name,string flag,string speaker,Vector2 at,Vector2 looks,string body,params (string node,string quest)[] lines)
            {
                var place=Place(kit,root,name,Listen,flag,"",speaker,looks,at,body,lamp:false);
                place.extras=lines.Select(l=>new CoronachDoor.Extra{node=l.node,needs=Done(l.quest)}).ToArray();
                return OnThem(place);
            }
            static CoronachDoor Guest(ZoneKit kit,GameObject root,string name,string flag,string node,string speaker,Vector2 at,Vector2 looks,string body)=>OnThem(Place(kit,root,name,Listen,flag,node,speaker,looks,at,body,lamp:false));
            /// <summary>Someone without a door is spoken to where they stand, not at what they look at (a guest's prompt stood on the door they looked at).</summary>
            static CoronachDoor OnThem(CoronachDoor place){if(place.body!=null)place.point.transform.position=place.body.transform.position;place.point.range=GuestReach;return place;}

            // ---------------------------------------------------------------- the heroes' last words, and Idra's crew
            static void Heroes(ZoneKit kit,GameObject root)
            {
                var hatch=Point("WalkwayHatch");var gate=Door("GateSix");var stall=Door("PitchNine");var board=Door("Board");
                var tarenAt=Free(kit,"Taren at the walkway",hatch,new Vector2(-.9f,-1.1f),3);
                var taren=Place(kit,root,"Taren, at the walkway joint",Yours,"coronach.taren","TarenWalkway","Taren",hatch,tarenAt,"TarenNatural");
                taren.hero="Taren";taren.opensWith=DoorsHeard;taren.stays=true;
                var sela=Place(kit,root,"Sela, at gate six",Yours,"coronach.sela","SelaGateSix","Sela",gate.at,gate.stand,"SelaNatural");
                sela.hero="Sela";sela.opensWith=DoorsHeard;sela.stays=true;
                // Idra has no door: she stands in the open street, where she can see who speaks, and her lantern is set down there.
                // On the promenade's far side by the office, looking across the street, so her word is said from the street: in the dock office behind the crated chairs, the way to it
                // was round them and a route's straight walk was cut (HomeRouteTests, November 14).
                var street=Maybe("IdraStands")??Free(kit,"Idra in the street",new Vector2(board.stand.x-1.4f,-5.6f),Vector2.zero,4);
                var idra=Place(kit,root,"Idra, in the street",Yours,"coronach.idra","IdraNoDoor","Idra",street+new Vector2(0,2.4f),street,"IdraNatural");
                idra.hero="Idra";idra.opensWith=DoorsHeard;idra.stays=true;
                var crew=new[]{Person(kit,"SurvivorNatural",Free(kit,"Kott",street,new Vector2(3.4f,1.6f),4),street,"Kott",new Color(.78f,.82f,.9f)),
                    Person(kit,"OkhariGrower",Free(kit,"Sibb",street,new Vector2(4.6f,.2f),4),street,"Sibb",new Color(.86f,.8f,.74f))};
                foreach(var one in crew)one.transform.SetParent(idra.transform,true);
                idra.extras=new[]{new CoronachDoor.Extra{node="IdraDoorCrew",needs=Done("CutterDown"),shows=crew}};
            }

            // ---------------------------------------------------------------- one place
            /// <summary>A place in the walk: its point at what is looked at (or a step short of it, from the floor), its person standing at <paramref name="stands"/> and facing it, its lantern
            /// set down beside their feet on the camera's side, where the fixed camera sees it past their legs.</summary>
            static CoronachDoor Place(ZoneKit kit,GameObject root,string name,string prompt,string flag,string node,string speaker,Vector2 looks,Vector2 stands,string body,bool lamp=true)
            {
                var holder=new GameObject(name);holder.transform.SetParent(root.transform,false);holder.transform.position=On(kit,stands);
                var door=holder.AddComponent<CoronachDoor>();door.speaker=speaker;door.node=node;door.calledFlag=Called;
                var point=kit.Discovery(name+" (the point)",new Vector3(looks.x,0,looks.y),prompt,flag,"",node??"",speaker,null,0,3);
                point.transform.SetParent(holder.transform,true);point.enabled=false;door.point=point;
                if(Vector2.Distance(looks,stands)>2.2f){var near=stands+(looks-stands).normalized*1.6f;point.transform.position=On(kit,near);}
                if(body!=null){var person=Person(kit,body,stands,looks,name);person.transform.SetParent(holder.transform,true);door.body=person;if(!taken.Contains(stands))taken.Add(stands);}
                if(lamp)
                {
                    var light=Lantern(kit,name+"'s lantern",Feet(stands,looks),new Color(1,.74f,.42f),1);light.transform.parent.SetParent(holder.transform,true);
                    door.lamp=light.transform.parent.gameObject;door.lampLight=light;door.glass=door.lamp.GetComponentsInChildren<Renderer>(true).Where(r=>r.name=="Glass").ToArray();
                }
                return door;
            }
            /// <summary>Beside their feet, on the side the fixed camera looks from, a little toward what they face.</summary>
            static Vector2 Feet(Vector2 stands,Vector2 looks)
            {
                var toward=(looks-stands).normalized;if(toward.sqrMagnitude<.01f)toward=Vector2.up;
                var side=new Vector2(toward.y,-toward.x);if(Vector2.Dot(side,ToCamera)<0)side=-side;
                return stands+side*.48f+toward*.22f+ToCamera*.12f;
            }
            /// <summary>From the floor toward the fixed camera (yaw 20, looking north-north-east).</summary>
            static readonly Vector2 ToCamera=new(-Mathf.Sin(20*Mathf.Deg2Rad),-Mathf.Cos(20*Mathf.Deg2Rad));
            static Vector2 Beside(GameObject body,float by){var p=body.transform.position+body.transform.right*by;return new Vector2(p.x,p.z);}
            static Vector2 Flat(Vector3 p)=>new(p.x,p.z);
            /// <summary>The point of the yellow line level with this one, then this far back from it on the side that stays.</summary>
            static Vector2 Back(Vector2 near,float back)=>lineA+along*Vector2.Dot(near-lineA,along)+across*back;

            /// <summary>Someone standing still: a named person in their own colours (no tint: they are themselves), or one of the town in a tint of their own.</summary>
            static GameObject Person(ZoneKit kit,string body,Vector2 at,Vector2 looks,string name,Color? tint=null)
            {
                var yaw=Mathf.Atan2(looks.x-at.x,looks.y-at.y)*Mathf.Rad2Deg;
                var one=kit.Townsperson(body,new Vector3(at.x,0,at.y),yaw,tint??Color.white,"Neve");one.name=name;
                if(tint==null)Object.DestroyImmediate(one.GetComponent<PropTint>());
                return one;
            }
            /// <summary>A spot near <paramref name="near"/> (first tried at <paramref name="first"/> off it, then in widening rings, in a fixed order) where a person can stand: floor under it, nothing
            /// solid there, a step off every declared way and off everyone placed so far. Stops the build by name if there is none within <paramref name="reach"/>.</summary>
            static Vector2 Free(ZoneKit kit,string who,Vector2 near,Vector2 first,float reach)
            {
                Physics.SyncTransforms();
                var tried=new List<Vector2>{near+first};
                for(float r=.8f;r<=reach;r+=.8f)for(int k=0;k<12;k++){float a=k*Mathf.PI/6+r;tried.Add(near+first+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r);}
                // In the same room as what they stand by: nothing tall between (Tam, pushed out of a crowded corner, stood behind the promenade's wall in the vestibule).
                bool InView(Vector2 p)=>!Physics.Linecast(new Vector3(near.x,kit.Y(near.x,near.y)+1.7f,near.y),new Vector3(p.x,kit.Y(p.x,p.y)+1.7f,p.y),~0,QueryTriggerInteraction.Ignore);
                foreach(var p in tried)if(Clear(kit,p)&&InView(p)){taken.Add(p);return p;}
                // A crowded corner (the dock office's, Pell and Iru by its lamp): a little further out before giving up, said in the log.
                for(float r=reach+.8f;r<=reach*2.2f;r+=.8f)for(int k=0;k<16;k++){float a=k*Mathf.PI/8+r;var p=near+first+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;if(Clear(kit,p)&&InView(p)){taken.Add(p);Debug.Log("CORONACH_FAR "+who+" at "+p);return p;}}
                throw new InvalidOperationException("CORONACH_NO_ROOM "+who+" near "+near+": no floor free of ways, props and people within "+reach*2.2f+" m");
            }
            /// <summary>Where one of the party stands to watch the launch: floor, nothing solid, nobody else, on the side that stays and two and a half to four and a half metres back from the
            /// parting. Nobody walks while the party is held, so a way is no reason to stand elsewhere.</summary>
            static Vector2 Watch(ZoneKit kit,string who,Vector2 near)
            {
                Physics.SyncTransforms();
                for(float r=0;r<=3;r+=.4f)for(int k=0;k<(r==0?1:12);k++)
                {
                    float a=k*Mathf.PI/6;var p=near+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;float back=Vector2.Dot(p-lineA,across);
                    if(back<2.5f||back>4.5f||taken.Any(o=>Vector2.Distance(o,p)<Apart))continue;
                    float floor=kit.Y(p.x,p.y);
                    if(!Physics.Raycast(new Vector3(p.x,floor+3,p.y),Vector3.down,out var hit,6,~0,QueryTriggerInteraction.Ignore)||Mathf.Abs(hit.point.y-floor)>.25f)continue;
                    if(Physics.CheckBox(new Vector3(p.x,floor+1.05f,p.y),new Vector3(.38f,.85f,.38f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))continue;
                    taken.Add(p);return p;
                }
                throw new InvalidOperationException("CORONACH_NO_ROOM "+who+" near "+near+": no clear floor two and a half to four and a half metres back from the parting");
            }
            static bool Clear(ZoneKit kit,Vector2 p)
            {
                float floor=kit.Y(p.x,p.y);
                if(!Physics.Raycast(new Vector3(p.x,floor+3,p.y),Vector3.down,out var hit,6,~0,QueryTriggerInteraction.Ignore)||Mathf.Abs(hit.point.y-floor)>.25f)return false;
                if(Physics.CheckBox(new Vector3(p.x,floor+1.05f,p.y),new Vector3(.38f,.85f,.38f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))return false;
                foreach(var way in AllWays)if(ZoneKit.FromPath(p,way)<WayClear)return false;
                foreach(var other in taken)if(Vector2.Distance(p,other)<Apart)return false;
                if(apart>0)foreach(var prompt in held)if(Vector2.Distance(p,prompt)<apart)return false;
                if(apart>0)foreach(var by in heldBy)if(Vector2.Distance(p,by)<BodyRing+GuestReach+.2f)return false;
                foreach(var (at,half) in signs)if(Mathf.Abs(p.x-at.x)<half+.7f&&Mathf.Abs(p.y-at.y)<1.2f)return false;
                return true;
            }
            /// <summary>A spot beside someone for a thing they have with them (a crate): floor under it, nothing solid there, nobody else's place, and a step further off the ways than a person.
            /// Placed by offset alone, Binna's crate by the stall stood round the day's Jun behind her pears (QaPlanWorldTests.NobodyStandsInsideAProp).</summary>
            static Vector2 Aside(ZoneKit kit,string what,Vector2 person,Vector2 first)
            {
                // A thing stands whenever the meeting's room does, so it keeps clear of the room's benches and people (the walk's people needn't: they and the room are never there at once).
                bool room=meetingRoom!=null&&!meetingRoom.activeSelf;if(room)meetingRoom.SetActive(true);
                try{return AsideOf(kit,what,person,first);}finally{if(room)meetingRoom.SetActive(false);}
            }
            static GameObject meetingRoom;
            static Vector2 AsideOf(ZoneKit kit,string what,Vector2 person,Vector2 first)
            {
                Physics.SyncTransforms();float r0=first.magnitude,a0=Mathf.Atan2(first.y,first.x);
                for(int ring=0;ring<10;ring++)for(int k=0;k<12;k++)
                {
                    float a=a0+(k%2==0?1:-1)*((k+1)/2)*Mathf.PI/6,r=r0+ring*.3f;var p=person+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;
                    float floor=kit.Y(p.x,p.y);
                    if(!Physics.Raycast(new Vector3(p.x,floor+3,p.y),Vector3.down,out var hit,6,~0,QueryTriggerInteraction.Ignore)||Mathf.Abs(hit.point.y-floor)>.25f)continue;
                    if(Physics.CheckBox(new Vector3(p.x,floor+.6f,p.y),new Vector3(.45f,.5f,.45f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))continue;
                    if(AllWays.Any(w=>ZoneKit.FromPath(p,w)<1.1f)||taken.Any(o=>o!=person&&Vector2.Distance(o,p)<.8f))continue;
                    return p;
                }
                throw new InvalidOperationException("CORONACH_NO_ROOM "+what+" beside "+person);
            }
            static GameObject Thing(ZoneKit kit,GameObject root,string name,string key,Vector2 at,float size)
            {
                var thing=kit.Prop(ZoneKit.Kit(key,"DeckCrate"),new Vector3(at.x,0,at.y),size,(at.x*37+at.y*11)%360);thing.name=name;thing.transform.SetParent(root.transform,true);taken.Add(at);return thing;
            }

            // ---------------------------------------------------------------- built things: the lantern, crystals, a pot, a tray, a kettle, a handcart, the table halves, the smiths
            static void Materials()
            {
                var metal=ZoneKit.Texture("Coronach/coronach-metal",128,(u,v)=>{float n=ZoneKit.Cloud(u,v,8,61)*.35f+.75f;return new Color(n,n,n);});
                brass=ZoneKit.Tinted(ZoneKit.Toon("coronach-lantern-brass",metal,m=>{m.SetColor("_BaseColor",Color.white);m.SetFloat("_RampSteps",4);}));
                glass=ZoneKit.Toon("coronach-lantern-glass",null,m=>{m.SetColor("_BaseColor",new Color(1,.86f,.62f));m.SetColor("_EmissionColor",new Color(2.6f,1.62f,.66f));});
                crystal=ZoneKit.Tinted(ZoneKit.Toon("coronach-crystal",null,m=>{m.SetColor("_BaseColor",Color.white);m.SetColor("_EmissionColor",new Color(.5f,.5f,.5f));}));
                timber=ZoneKit.Tinted(ZoneKit.Toon("coronach-timber",metal,m=>m.SetColor("_BaseColor",Color.white)));
            }
            /// <summary>The Coronach's lantern, built once at the origin and shared: a hand lantern of the Wick kind (the atlas makes Lanternwick the custom's origin), a brass foot and cage round
            /// a warm glass, a cap and a ring to carry it by, about 0.45 m.</summary>
            static void Lantern(ZoneKit kit)
            {
                Color b=new(.66f,.48f,.24f),d=ZoneKit.Shape.Dim(b,.62f);
                var cage=new ZoneKit.Shape();
                cage.Tube(new[]{new Vector3(0,0,0),new Vector3(0,.045f,0)},new[]{.13f,.115f},12,d,b,1,.25f);
                for(int k=0;k<4;k++){float a=k*Mathf.PI/2+Mathf.PI/4;cage.Post(new Vector3(Mathf.Cos(a)*.092f,.04f,Mathf.Sin(a)*.092f),.25f,.011f,.011f,5,d,b);}
                cage.Tube(new[]{new Vector3(0,.285f,0),new Vector3(0,.31f,0)},new[]{.115f,.1f},12,b,b,1,.25f);
                cage.Dome(new Vector3(0,.31f,0),.1f,.075f,4,12,b,d);
                cage.Tube(new[]{new Vector3(0,.37f,0),new Vector3(0,.4f,0)},new[]{.03f,.026f},8,d,b,1,.25f);
                var ring=new List<Vector3>();for(int k=0;k<=8;k++){float a=k*Mathf.PI/8;ring.Add(new Vector3(Mathf.Cos(a)*.07f,.4f+Mathf.Sin(a)*.08f,0));}
                cage.Tube(ring,ring.Select(_=>.009f).ToList(),5,d,d,1,.25f,false);
                var lit=new ZoneKit.Shape().Tube(new[]{new Vector3(0,.05f,0),new Vector3(0,.15f,0),new Vector3(0,.28f,0)},new[]{.072f,.08f,.07f},12,Color.white,Color.white,1,.25f);
                var made=kit.Built("Coronach lantern frame",cage,brass);frame=made.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(made);
                made=kit.Built("Coronach lantern glass",lit,glass);pane=made.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(made);
            }
            /// <summary>One lantern set down on the deck: its frame, its glass (named Glass, for the door to dim), and its light, a warm pool about three metres across with no shadows.
            /// Returns the light; its parent is the lantern.</summary>
            static Light Lantern(ZoneKit kit,string name,Vector2 at,Color colour,float strength)=>Lantern(kit,name,kit.On(at.x,at.y),colour,strength);
            static Light Lantern(ZoneKit kit,string name,Vector3 at,Color colour,float strength)
            {
                var lantern=new GameObject(name);lantern.transform.SetPositionAndRotation(at,Quaternion.Euler(0,Mathf.Repeat(at.x*53+at.z*29,360),0));
                Part(lantern,"Frame",frame,brass);var glow=Part(lantern,"Glass",pane,glass);
                if(colour!=new Color(1,.74f,.42f))
                {
                    var tinted=ZoneKit.Toon("coronach-lantern-glass-"+ColorUtility.ToHtmlStringRGB(colour).ToLowerInvariant(),null,m=>{m.SetColor("_BaseColor",colour);m.SetColor("_EmissionColor",colour*2.4f);});
                    glow.GetComponent<MeshRenderer>().sharedMaterial=tinted;
                }
                var go=new GameObject("Lantern light",typeof(Light));go.transform.SetParent(lantern.transform,false);go.transform.localPosition=new Vector3(0,.2f,0);
                var l=go.GetComponent<Light>();l.type=LightType.Point;l.color=colour;l.range=3.2f*strength;l.intensity=1.8f*strength;l.shadows=LightShadows.None;
                return l;
            }
            static GameObject Part(GameObject parent,string name,Mesh mesh,Material material)
            {
                var part=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));part.transform.SetParent(parent.transform,false);
                part.GetComponent<MeshFilter>().sharedMesh=mesh;var r=part.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;return part;
            }
            /// <summary>Six Ridge Crystals set round the lantern's foot, every colour, including "not stolen".</summary>
            static GameObject Crystals(ZoneKit kit,Transform lamp)
            {
                var shape=new ZoneKit.Shape();var hues=new[]{new Color(.9f,.4f,.4f),new Color(.95f,.75f,.3f),new Color(.5f,.9f,.5f),new Color(.4f,.7f,.95f),new Color(.75f,.5f,.95f),new Color(.95f,.95f,.9f)};
                for(int k=0;k<6;k++){float a=k*Mathf.PI/3;var c=lamp.position+new Vector3(Mathf.Cos(a)*.17f,.05f,Mathf.Sin(a)*.17f);shape.Lump(c,new Vector3(.025f,.07f,.025f),k+3,.02f,hues[k],ZoneKit.Shape.Dim(hues[k],.6f),5,3,.25f,Quaternion.Euler(Mathf.Sin(a)*18,0,Mathf.Cos(a)*18));}
                var made=kit.Built("Mira's six Ridge Crystals",shape,crystal);made.transform.SetParent(lamp,true);made.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;return made;
            }
            static GameObject Pot(ZoneKit kit,Transform on,string name)
            {
                var top=TopOf(on);var shape=new ZoneKit.Shape().Tube(new[]{top,top+Vector3.up*.18f},new[]{.15f,.17f},12,new Color(.36f,.3f,.26f),new Color(.5f,.42f,.34f),1,.25f).Disc(top+Vector3.up*.18f,Vector3.up,.15f,12,new Color(.62f,.4f,.24f));
                var made=kit.Built(name,shape,timber);made.transform.SetParent(on,true);return made;
            }
            static GameObject Tray(ZoneKit kit,Transform on,string name)
            {
                var top=TopOf(on);var shape=new ZoneKit.Shape().Box(top+Vector3.up*.02f,new Vector3(.62f,.04f,.42f),Quaternion.identity,new Color(.56f,.44f,.32f));
                for(int k=0;k<4;k++)shape.Lump(top+new Vector3(-.2f+k*.13f,.07f,0),new Vector3(.05f,.03f,.16f),k,.01f,new Color(.36f,.5f,.72f),new Color(.24f,.34f,.5f),6,3);
                var made=kit.Built(name,shape,timber);made.transform.SetParent(on,true);return made;
            }
            static GameObject Kettle(ZoneKit kit,Transform holder,string name)
            {
                var at=holder.position+holder.forward*.32f+holder.right*.18f+Vector3.up*.82f;
                var shape=new ZoneKit.Shape().Tube(new[]{at,at+Vector3.up*.16f},new[]{.1f,.075f},10,new Color(.5f,.52f,.55f),new Color(.66f,.68f,.7f),1,.25f).Dome(at+Vector3.up*.16f,.075f,.04f,3,10,new Color(.66f,.68f,.7f),new Color(.5f,.52f,.55f));
                var made=kit.Built(name,shape,timber);made.transform.SetParent(holder,true);return made;
            }
            static GameObject Handcart(ZoneKit kit,Vector2 at,string name)
            {
                var c=kit.On(at.x,at.y);var wood=new Color(.52f,.4f,.28f);var shape=new ZoneKit.Shape().Box(c+Vector3.up*.55f,new Vector3(1.1f,.12f,.7f),Quaternion.identity,wood);
                foreach(float side in new[]{-.38f,.38f})shape.Disc(c+new Vector3(.2f,.25f,side),Vector3.forward*Mathf.Sign(side),.25f,10,new Color(.3f,.28f,.26f));
                shape.Beam(c+new Vector3(-.5f,.6f,-.3f),c+new Vector3(-1.1f,.85f,-.3f),.05f,.05f,ZoneKit.Shape.Dim(wood,.85f)).Beam(c+new Vector3(-.5f,.6f,.3f),c+new Vector3(-1.1f,.85f,.3f),.05f,.05f,ZoneKit.Shape.Dim(wood,.85f));
                for(int k=0;k<6;k++){var p=c+new Vector3(-.35f+(k%3)*.33f,.62f,-.17f+(k/3)*.34f);shape.Tube(new[]{p,p+Vector3.up*.17f},new[]{.1f,.075f},10,new Color(.5f,.52f,.56f),new Color(.7f,.7f,.72f),1,.25f);}
                return kit.Built(name,shape,timber);
            }
            static Vector3 TopOf(Transform thing){var box=ZoneKit.Drawn(thing.gameObject);return new Vector3(box.center.x,box.max.y,box.center.z);}
            /// <summary>The lamp Pell has lit over the dock office's door: hung on its bracket above the door from the call (hers either way: her mother's if it came home).</summary>
            static void OverTheDoor(ZoneKit kit,GameObject root,Vector2 door)
            {
                var light=Lantern(kit,"The lamp over the dock office",kit.On(door.x,door.y,2.6f)+new Vector3(ToCamera.x,0,ToCamera.y)*.3f,new Color(1,.74f,.42f),1.1f);
                var shown=light.transform.parent.gameObject;shown.SetActive(false);
                From(root,"The lamp over the dock office, from the call",Called,true,shown);
            }
            /// <summary>Maru's lamps, one at each end of the walkway, counting two short and one long, so the halves can tell each other they're afloat: from the call, if the beacon was kept, and
            /// lit to the end whoever has gone aboard (two holders: the call, then the story, as Many Hands seats its dinner).</summary>
            static void WalkwayLamps(ZoneKit kit,GameObject root)
            {
                var posts=new List<GameObject>();int k=0;
                foreach(var end in new[]{Back(mid,5.2f),Back(mid,-3.4f)})
                {
                    var spot=Free(kit,"Maru's lamp",end,new Vector2(along.x,along.y)*3.2f,4);
                    var post=kit.Prop(ZoneKit.Kit("LampPost","DressLightBollard"),new Vector3(spot.x,0,spot.y),1.6f,0,false);post.name="Maru's lamp";
                    var glow=new GameObject("Maru's lamp light",typeof(Light));glow.transform.SetParent(post.transform,false);glow.transform.position=kit.On(spot.x,spot.y,1.5f);
                    var l=glow.GetComponent<Light>();l.type=LightType.Point;l.range=4;l.shadows=LightShadows.None;
                    var count=post.AddComponent<RhythmLamp>();count.count="SSL";count.glow=l;count.lamps=post.GetComponentsInChildren<Renderer>();count.offset=k++*1.7f;
                    posts.Add(post);
                }
                var story=new GameObject("Maru's lamps, if the beacon was kept",typeof(Whereabouts));var where=story.GetComponent<Whereabouts>();where.from=Done("BeaconKeeper");where.targets=posts.ToArray();
                foreach(var post in posts){post.transform.SetParent(story.transform,true);post.SetActive(false);}
                var call=From(root,"Maru's lamps, from the call",Called,true,story);story.transform.SetParent(call.transform,true);story.SetActive(false);
            }
            /// <summary>Three of Tovra's smiths, each with the young pikeguard who came to read under a kettle.</summary>
            static GameObject[] Smiths(ZoneKit kit,CoronachDoor tovra,Vector2 at,Vector2 looks)
            {
                var made=new List<GameObject>();var bodies=new[]{("BrekkBody",new Color(.84f,.78f,.72f)),("OdrunBody",new Color(.78f,.82f,.86f)),("HaddaBody",new Color(.9f,.82f,.74f))};
                for(int k=0;k<3;k++)
                {
                    var smith=Free(kit,"a smith",at,new Vector2(-1.6f-k*1.5f,-2.2f+k*.4f),4);
                    made.Add(Person(kit,bodies[k].Item1,smith,looks,"One of Tovra's smiths",bodies[k].Item2));
                    var young=Person(kit,"PipBody",Free(kit,"a young pikeguard",smith,new Vector2(.7f,-.5f),2.4f),looks,"A young pikeguard",new Color(.8f+k*.05f,.84f,.9f-k*.05f));young.transform.localScale=Vector3.one*.86f;made.Add(young);
                }
                foreach(var one in made)one.transform.SetParent(tovra.transform,true);
                return made.ToArray();
            }
            /// <summary>The Arrens, with the wedding table's top in two halves: out of a doorway as Mira's stop begins, past the back of the stall, toward the market's airlock and gone.</summary>
            static (Transform[] carriers,Vector3[] path) TableHalves(ZoneKit kit,GameObject root,Spot stall)
            {
                var from=Maybe("TableComesOut")??stall.stand+new Vector2(-7,1.5f);
                var to=Maybe("MarketLock")??stall.stand+new Vector2(8,-2.5f);
                var past=stall.stand+(stall.stand-stall.at).normalized*2.4f;
                var path=new[]{On(kit,from),On(kit,past),On(kit,to)};
                var carriers=new List<Transform>();
                foreach(var (body,tint,k) in new[]{("SennBody",new Color(.86f,.82f,.74f),0),("OkhariBody",new Color(.8f,.86f,.84f),1)})
                {
                    var one=kit.Townsperson(body,path[0],90,tint,"Neve");one.name=k==0?"One of the Arrens, with half the table":"The other Arren, with the other half";
                    foreach(var c in one.GetComponentsInChildren<Collider>())Object.DestroyImmediate(c);foreach(var t in one.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=false;
                    var top=new ZoneKit.Shape().Box(new Vector3(0,1.02f,.42f),new Vector3(1.05f,.07f,.78f),Quaternion.Euler(0,0,k==0?-4:4),new Color(.6f,.42f,.26f),new Color(.7f,.5f,.32f));
                    var half=kit.Built("Half of the Arrens' table top "+(k+1),top,timber);half.transform.SetParent(one.transform,false);half.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                    one.transform.SetParent(root.transform,true);one.SetActive(false);carriers.Add(one.transform);
                }
                return (carriers.ToArray(),path);
            }

            // ---------------------------------------------------------------- the launch
            static void Launch(ZoneKit kit,GameObject root)
            {
                // The Decks builds the market as one root, its floor cut along Taren's yellow line, so the promenade parts exactly under the line, half of it to each side. Without that root
                // the market would be taken by its footprint and part at its own collar, the chalked joint a step short of the line.
                var named=Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.name=="Market district"&&t.parent==null);
                var collar=named!=null?new[]{lineA,lineB}:Lines("ChalkedJoints").Where(j=>Vector2.Dot((j[0]+j[^1])*.5f-mid,across)>0).OrderBy(j=>Vector2.Distance((j[0]+j[^1])*.5f,mid)).FirstOrDefault()??new[]{lineA,lineB};
                var a=On(kit,collar[0]);var b=On(kit,collar[^1]);var parting=(collar[0]+collar[^1])*.5f;
                var launch=new GameObject(LaunchRoot,typeof(CoronachLaunch)).GetComponent<CoronachLaunch>();launch.transform.SetParent(root.transform,false);
                launch.music=LaunchMusic;launch.ring=RingZone;launch.ringSpawn="Arrival";launch.lineA=a;launch.lineB=b;launch.stays=On(kit,parting+across*4);
                var watchA=Watch(kit,"the lead watching",parting+across*3.2f-along*.9f);var watchB=Watch(kit,"the partner watching",parting+across*3.4f+along*1.3f);
                launch.watch=new[]{On(kit,watchA),On(kit,watchB)};launch.lookAt=On(kit,parting-across*7);
                launch.shows=new Bounds(kit.On(parting.x,parting.y,3)-new Vector3(across.x,0,across.y)*4.5f,new Vector3(19,8,17));
                launch.vents=Enumerable.Range(0,7).Select(i=>Vector3.Lerp(a,b,i/6f)).ToArray();
                var centre=(a+b)*.5f;
                launch.chalk=Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(r=>r.name.StartsWith("Chalked joint")&&Vector3.Distance(r.bounds.center,centre)<26)
                    .OrderByDescending(r=>Vector3.Distance(r.bounds.center,centre)).ToArray();
                // Its far collar is the near one's mirror about the market's middle (the Decks' hull centres): chapter 1 built the hulls alike.
                float middle=((float[])Value("Hulls")).OrderBy(x=>Mathf.Abs(x-mid.x)).First();
                var west=Lines("ChalkedJoints").Select(j=>(j[0].x+j[^1].x)*.5f).Where(x=>Vector2.Dot(new Vector2(x,mid.y)-mid,across)>0).OrderBy(x=>Mathf.Abs(x-mid.x)).DefaultIfEmpty(parting.x).First();
                float east=2*middle-west;if(west>east)(west,east)=(east,west);
                var (parts,torn)=named!=null?(new[]{named},Array.Empty<GameObject>()):Market(west,east);
                if(parts.Length==0)throw new InvalidOperationException("CORONACH_NO_DISTRICT: nothing stands in the market's footprint between x "+west+" and "+east);
                // The walk's own people and things on the market's side (Mira, Jun, Ruva, Binna, whoever is still standing there) go up with it: they are aboard. The Decks made its root
                // before the walk was staged, so they are found here, each by where it stands: inside the market's walls, and past the line on the promenade.
                bool Aboard(Vector2 p)=>p.x>west+.05f&&p.x<east-.05f&&(Vector2.Dot(p-lineA,across)<-.05f||p.y<Mathf.Min(lineA.y,lineB.y)-1.1f||p.y>Mathf.Max(lineA.y,lineB.y)+.4f);
                var ours=new List<Transform>();
                void Ours(Transform t)
                {
                    if(t.GetComponent<CoronachWalk>()!=null||t.GetComponent<CoronachLaunch>()!=null||Extent(t) is not Bounds e)return;
                    bool min=Aboard(new Vector2(e.min.x,e.min.z))&&Aboard(new Vector2(e.max.x,e.max.z))&&Aboard(new Vector2(e.min.x,e.max.z))&&Aboard(new Vector2(e.max.x,e.min.z));
                    if(min){ours.Add(t);return;}
                    foreach(Transform child in t)Ours(child);
                }
                foreach(Transform child in root.transform)Ours(child);
                parts=parts.Concat(ours).ToArray();
                // It moves once, at the launch: still in the bake (its other static flags stay), but never batched, or what moved would be a copy while the batch stayed put.
                foreach(var part in parts)foreach(var t in part.GetComponentsInChildren<Transform>(true))
                {
                    var flags=GameObjectUtility.GetStaticEditorFlags(t.gameObject);
                    if((flags&StaticEditorFlags.BatchingStatic)!=0)GameObjectUtility.SetStaticEditorFlags(t.gameObject,flags&~StaticEditorFlags.BatchingStatic);
                }
                var under=Underside(kit,root,parts,a);
                launch.district=parts.Append(under.transform).ToArray();launch.torn=torn;launch.released=new[]{under};
                launch.lift=new Vector3(along.x*4-across.x*12,30,along.y*4-across.y*12);
                Debug.Log($"CORONACH_DISTRICT parts={parts.Length} torn={torn.Length} between x {west:0.00} and {east:0.00}");
            }
            /// <summary>Everything that lifts with the market: the topmost things whose whole extent stands inside its footprint, between its two collars (its hull's own pieces, its keel and
            /// casing under the floor, its dressing, its lamps, whoever stands there), and nothing that reaches past either collar: the walkway bridges and their casings stay with the hulls
            /// they join. What is laid flat across the parting itself (a runner, a strip of studs) can't be split, so it is torn: hidden as the joint lets go.</summary>
            static (Transform[] parts,GameObject[] torn) Market(float west,float east)
            {
                var parts=new List<Transform>();var torn=new List<GameObject>();
                bool Inside(Bounds b)=>b.min.x>=west+.05f&&b.max.x<=east-.05f&&b.min.z>=-30&&b.max.z<=30;
                bool Across(Bounds b)=>b.min.x<west&&b.max.x>west+.05f&&b.size.y<.3f&&b.max.y<.45f&&b.min.z>-30&&b.max.z<30;
                void Look(Transform t)
                {
                    if(t.TryGetComponent<Light>(out var sun)&&sun.type==LightType.Directional)return;
                    var box=Extent(t);if(box==null)return;
                    if(Inside(box.Value)){parts.Add(t);return;}
                    if(t.childCount==0&&t.GetComponent<Renderer>()!=null&&Across(box.Value)){torn.Add(t.gameObject);return;}
                    foreach(Transform child in t)Look(child);
                }
                foreach(var top in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())Look(top.transform);
                return (parts.ToArray(),torn.ToArray());
            }
            /// <summary>Where a thing and everything under it stands, drawn or solid, from its meshes' own bounds (a renderer switched off reports none), with its lamps as points.</summary>
            static Bounds? Extent(Transform t)
            {
                Bounds? all=null;void Add(Vector3 p){if(all is Bounds a){a.Encapsulate(p);all=a;}else all=new Bounds(p,Vector3.zero);}
                foreach(var r in t.GetComponentsInChildren<Renderer>(true))
                {
                    if(r is ParticleSystemRenderer||r is LineRenderer||r is TrailRenderer)continue;
                    var local=r.localBounds;var m=r.transform.localToWorldMatrix;var c=local.center;var e=local.extents;
                    for(int k=0;k<8;k++)Add(m.MultiplyPoint3x4(c+Vector3.Scale(e,new Vector3((k&1)==0?-1:1,(k&2)==0?-1:1,(k&4)==0?-1:1))));
                }
                foreach(var box in t.GetComponentsInChildren<BoxCollider>(true))if(!box.isTrigger)Add(box.transform.TransformPoint(box.center));
                foreach(var l in t.GetComponentsInChildren<Light>(true))if(l.type!=LightType.Directional)Add(l.transform.position);
                return all;
            }
            /// <summary>What the market turns out to be once it lifts: the station's own keel and casing under its floor go with it (they are in its footprint), so its cut edge is real plating.
            /// This adds what the casing doesn't have: a row of lit ports along its outer face on the side that stays, its lit windows rising past, and three Crawl-rigged engines burning under
            /// the keel. Drawn only, hidden until the joint lets go, and lifted with the rest.</summary>
            static GameObject Underside(ZoneKit kit,GameObject root,Transform[] parts,Vector3 a)
            {
                var holder=new GameObject("The market's ports and engines");holder.transform.SetParent(root.transform,true);
                Bounds? all=null;foreach(var part in parts)if(Extent(part) is Bounds e){if(all is Bounds x){x.Encapsulate(e);all=x;}else all=e;}
                var box=all.Value;var toward=new Vector3(across.x,0,across.y);var side=Vector3.Cross(toward,Vector3.up).normalized;
                // The casing's outer face on the side that stays: the market's extent's edge toward it.
                float face=Vector3.Dot(box.center,toward)+Vector3.Dot(box.extents,new Vector3(Mathf.Abs(toward.x),0,Mathf.Abs(toward.z)));
                float run=Vector3.Dot(box.size,new Vector3(Mathf.Abs(side.x),0,Mathf.Abs(side.z)))-2;
                var lit=new ZoneKit.Shape();var start=box.center-side*run*.5f;start+=toward*(face-Vector3.Dot(start,toward)+.03f);
                int ports=Mathf.Max(4,Mathf.RoundToInt(run/1.6f));
                for(int k=0;k<ports;k++)
                {
                    var p=start+side*(run*(k+.5f)/ports);p.y=a.y-1.1f;
                    lit.Quad(p+Vector3.up*.2f-side*.26f,p+Vector3.up*.2f+side*.26f,p-Vector3.up*.2f+side*.26f,p-Vector3.up*.2f-side*.26f,toward,Color.white,.5f);
                }
                var flame=ZoneKit.Toon("coronach-engine",null,m=>{m.SetColor("_BaseColor",new Color(.7f,.85f,1));m.SetColor("_EmissionColor",new Color(1.6f,2.4f,3.2f));});
                var engines=new ZoneKit.Shape();float floor=box.min.y-.05f;
                for(int k=0;k<3;k++)
                {
                    var c=new Vector3(box.center.x+(k-1)*box.size.x*.28f,floor,box.center.z+(k%2==0?-1:1)*box.size.z*.18f);
                    engines.Disc(c,Vector3.down,1.2f,16,Color.white);
                    var glow=new GameObject("Engine light",typeof(Light));glow.transform.SetParent(holder.transform,false);glow.transform.position=c+Vector3.down*1.6f;
                    var l=glow.GetComponent<Light>();l.type=LightType.Point;l.color=new Color(.6f,.78f,1);l.range=10;l.intensity=2.4f;l.shadows=LightShadows.None;
                }
                foreach(var made in new[]{kit.Built("The market's lit ports",lit,glass),kit.Built("The market's engines",engines,flame)})made.transform.SetParent(holder.transform,true);
                foreach(var r in holder.GetComponentsInChildren<Renderer>())r.shadowCastingMode=ShadowCastingMode.Off;
                foreach(var t in holder.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=false;
                holder.SetActive(false);return holder;
            }

            // ---------------------------------------------------------------- nobody of the walk on a way
            /// <summary>The Decks' own rule for its people, kept for the walk's (CoronachTests' NobodyOfTheWalkStandsOnAWayOrCutsTheTownInTwo says the same): nobody it placed stands within
            /// reach of a way people walk, except the way they are the goal of (Ilo at the notice board, at the end of the way to it and past it, not beside it); within reach of another
            /// way's end, a door at a corner, .6 m.</summary>
            static void OffTheWays(GameObject root)
            {
                var faults=new List<string>();
                foreach(var door in root.GetComponentsInChildren<CoronachDoor>(true))
                {
                    var bodies=new List<GameObject>();if(door.body!=null)bodies.Add(door.body);foreach(var extra in door.extras)if(extra.shows!=null)bodies.AddRange(extra.shows.Where(s=>s!=null&&s.GetComponent<Collider>()!=null));
                    foreach(var body in bodies)
                    {
                        var at=Flat(body.transform.position);var others=AllWays.Where(w=>!GoalOf(at,w)).ToList();if(others.Count==0)continue;
                        float gap=others.Min(w=>ZoneKit.FromPath(at,w));bool atAnEnd=others.SelectMany(w=>new[]{w[0],w[^1]}).Any(e=>Vector2.Distance(e,at)<2.6f);
                        if(gap<(atAnEnd?.6f:WayClear-.05f))faults.Add($"{door.name}: {body.name} at ({at.x:0.0}, {at.y:0.0}) is {gap:0.00} m from a way");
                    }
                }
                if(faults.Count>0)throw new InvalidOperationException("CORONACH_ON_A_WAY\n"+string.Join("\n",faults));
                Debug.Log("CORONACH_WAYS_CLEAR");
            }
            /// <summary>A way that ends at this person: within 1.2 m of an end and past it, the end the nearest point of the way to them.</summary>
            static bool GoalOf(Vector2 p,Vector2[] w)
            {
                foreach(var (end,before) in new[]{(w[^1],w[^2]),(w[0],w[1])})
                {
                    if(Vector2.Distance(p,end)>1.2f)continue;
                    var d=end-before;if(Vector2.Dot(p-end,d)>=-.05f*d.magnitude)return true;
                }
                return false;
            }
        }
    }
}
