using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
using UnityEngine.AI;
namespace Lattice.Combat
{
    /// <summary>
    /// One of the Foundry Saint's six arms (chapter 9, <c>BossController.FoundrySaint.cs</c>): an empty object at the arm's **collar**, the shoulder where it meets the press, its forward along the
    /// arm's bearing and level. It draws its own arm (the Saint's definition's <c>bodySegment</c>, the generated <c>SaintArm</c>) from the moment the scene loads, raised while the override holds it, so the
    /// room has its arms at the first look. Nothing here can be struck until the Saint wakes and claims it (<see cref="Claim"/>): break targets are locked before the fight.
    /// <list type="bullet">
    /// <item><b>Raised</b> (the override holds it): pitched up like a crane's boom, its collar a cyan knot. A blow on the collar is resisted (its own <see cref="Health"/>'s plate).</item>
    /// <item><b>Planted</b>: brought down along the floor by a slam, or with the head's lift, its collar <see cref="Unlocked"/> for a few seconds (the knot burns amber): a blow lands.</item>
    /// <item><b>Released</b>: its collar struck through while unlocked, or let go as the press settles. The ring turns, the cyan goes out, the arm comes down and lies still along the floor, and its
    /// <b>road</b> turns solid: a box 1.8 m wide on top from the floor at its foot to 2.3 m at the collar, the climb to the rig on the press's back. Anyone where it comes down is moved clear first.</item>
    /// </list>
    /// A save with <c>bossdown.&lt;settledBy&gt;</c> loads every arm down and still with its road solid. The room's contract is docs/maps/SeparationWorks.md, "Boss arena contract".
    /// Logs <c>SAINT_ARM_DOWN &lt;index&gt;</c> as a released arm comes to rest.
    /// </summary>
    public sealed class SaintArm:MonoBehaviour
    {
        public static readonly List<SaintArm> All=new();
        /// <summary>1 to 6, west to east along the press's back.</summary>
        public int index;
        /// <summary>The boss whose arm this is, by EnemyDef id: who may claim it, and whose fall leaves it down.</summary>
        public string settledBy="FoundrySaint";
        /// <summary>A released arm has come to rest and its road is solid: the boss, and anyone else, may listen.</summary>
        public event System.Action<SaintArm> Released;
        /// <summary>The arm is let go, this frame (before it has come down): its collar struck through (false), or the press settling or its bar gone past its line (true).</summary>
        public event System.Action<SaintArm,bool> LetGo;

        /// <summary>The arm's numbers. The collar's height and the road's slope are the room's contract; the drawn arm is posed to them whatever its model's pivot.</summary>
        public static class Tune
        {
            /// <summary>The collar's height over the floor, the arm's length, raised and lying (degrees: up is negative about its right axis), and its thickness.</summary>
            public const float CollarHeight=2,Length=7,RaisedPitch=-50,LiftedPitch=-64,DownPitch=12.8f,Thickness=1.2f;
            /// <summary>The road a released arm makes: its width on top, where its top meets the collar (out, and over the floor), and where its foot meets the floor.</summary>
            public const float RoadWidth=1.8f,RoadTopOut=.3f,RoadTopHeight=2.3f,RoadFootOut=7.1f;
            /// <summary>The collar's target: how far proud of the press it stands and its size; the ring of light drawn there (round the arm's shoulder, the arm 1.2 thick).</summary>
            public const float CollarOut=.15f,CollarRadius=.8f,RingRadius=.72f,RingTube=.1f;
            /// <summary>The end of the arm model's long axis its shoulder flange is at, when its mesh can't be read to tell (the generated model can't).</summary>
            public const float RingEnd=-1;
            /// <summary>How long a released arm takes to come down, and how far the ring turns as it lets go.</summary>
            public const float Falls=1.05f,RingTurns=38;
            /// <summary>How fast a raised arm swings to a new pitch when nothing hurries it (degrees a second).</summary>
            public const float Swing=55;
            /// <summary>How far, and in what steps, the floor is searched for somewhere clear to step a body the road (or the settling press) would hold (route 59, November 14).</summary>
            public const float ClearReach=8,ClearStep=.25f;
        }
        static readonly Color Held=new(.45f,1.45f,1.6f,.95f),Open=new(1.7f,.95f,.35f,.95f),Dark=new(.2f,.2f,.22f,.6f),Slack=new(.25f,.32f,.34f,.5f);
        static readonly Color WarmLamp=new(1.25f,.8f,.48f,1);

        Transform swing,drawn,knotNode;Renderer knot;Renderer[] armRenderers;MaterialPropertyBlock block;
        GameObject road,footprint;BoxCollider roadBox;LineRenderer cable;Vector3 cableFrom;bool cabled;
        /// <summary>The press this arm hangs from, once it has claimed it (BossController sets it): its back faces the camera, which is where anyone the road would hold is stepped out to.</summary>
        public Transform press;
        /// <summary>The way off the press's back, level: toward the camera in the Works. Before a press has claimed it, the arm's own bearing.</summary>
        public Vector3 Back{get{var back=press!=null?-press.forward:Outward;back.y=0;return back.sqrMagnitude>.0001f?back.normalized:Vector3.back;}}
        Health collar;SphereCollider collarShape;
        float pitch=Tune.RaisedPitch,goal=Tune.RaisedPitch,rate=Tune.Swing,yaw,yawGoal,yawRate=90,unlockedUntil=-1,turned;
        bool released,resting,claimed;float plantedUntil=-1;
        public Health Collar=>collar;
        /// <summary>Its collar answers blows now: planted by a slam or under the head's lift, and not yet released.</summary>
        public bool Unlocked=>claimed&&!released&&Time.time<unlockedUntil;
        public bool IsReleased=>released;
        /// <summary>Released and come to rest: its road is solid.</summary>
        public bool Resting=>resting;
        public bool Claimed=>claimed;
        public float Pitch=>pitch;
        /// <summary>Where the arm meets the press, on the floor under it, and the floor's height there.</summary>
        public Vector3 CollarFloor=>transform.position+Vector3.down*Tune.CollarHeight;
        public float Floor=>transform.position.y-Tune.CollarHeight;
        public Vector3 Outward{get{var f=transform.forward;f.y=0;return f.sqrMagnitude>.0001f?f.normalized:Vector3.forward;}}
        /// <summary>Its bearing, degrees clockwise from north.</summary>
        public float Bearing=>Mathf.Repeat(Mathf.Atan2(Outward.x,Outward.z)*Mathf.Rad2Deg,360);
        /// <summary>The floor under its raised hand: where a weld it drags begins.</summary>
        public Vector3 HandFloor=>CollarFloor+Outward*(Tune.Length*Mathf.Cos(Tune.RaisedPitch*Mathf.Deg2Rad));
        /// <summary>A point on the road's top: <paramref name="t"/> 0 at its foot on the floor, 1 where it meets the collar.</summary>
        public Vector3 RoadPoint(float t){float out_=Mathf.Lerp(Tune.RoadFootOut,Tune.RoadTopOut,t);return CollarFloor+Outward*out_+Vector3.up*(Tune.RoadTopHeight*t);}
        /// <summary>How far up the road a point on its top is (0 to 1), from where it is on the plan.</summary>
        public float RoadAt(Vector3 point){float along=Vector3.Dot(Flat(point-CollarFloor),Outward);return Mathf.Clamp01((Tune.RoadFootOut-along)/(Tune.RoadFootOut-Tune.RoadTopOut));}
        /// <summary>Whether a point is on the road (on the plan, within its width and length).</summary>
        public bool OnRoad(Vector3 point)
        {
            var off=Flat(point-CollarFloor);float along=Vector3.Dot(off,Outward),across=Mathf.Abs(Vector3.Dot(off,Vector3.Cross(Vector3.up,Outward)));
            return along>=Tune.RoadTopOut-.2f&&along<=Tune.RoadFootOut+.2f&&across<=Tune.RoadWidth*.5f+.2f;
        }
        static Vector3 Flat(Vector3 v){v.y=0;return v;}

        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable()=>All.Remove(this);
        void Awake()
        {
            block=new MaterialPropertyBlock();
            swing=new GameObject("Swing").transform;swing.SetParent(transform,false);
            var definition=GameCatalog.Find<EnemyDef>(settledBy);
            var prefab=definition!=null?definition.bodySegment:null;
            if(prefab!=null)drawn=Instantiate(prefab,swing,false).transform;
            else{var stand=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Destroy(stand.GetComponent<Collider>());stand.name="Arm stand-in";drawn=stand.transform;drawn.SetParent(swing,false);drawn.localScale=new Vector3(1.1f,Tune.Length*.5f,1.1f);drawn.localRotation=Quaternion.Euler(90,0,0);drawn.localPosition=Vector3.forward*Tune.Length*.5f;}
            drawn.name="Arm";if(prefab!=null)Align(drawn);
            alignedRotation=drawn.localRotation;alignedPosition=drawn.localPosition;
            foreach(var collider in drawn.GetComponentsInChildren<Collider>())Destroy(collider);
            armRenderers=drawn.GetComponentsInChildren<Renderer>();
            // The collar's own target: its Health, a trigger and a hurtbox, and a knot of light where the ring is. Shut and untargetable until the boss claims it.
            knotNode=new GameObject("Collar").transform;knotNode.SetParent(transform,false);knotNode.localPosition=Vector3.forward*Tune.CollarOut;
            collar=knotNode.gameObject.AddComponent<Health>();collar.id="Shoulder collar";collar.maximum=collar.integrity=1;collar.breakThreshold=1e9f;collar.Damageable=false;
            collar.Plate=_=>Unlocked?1:0;
            collarShape=knotNode.gameObject.AddComponent<SphereCollider>();collarShape.isTrigger=true;collarShape.radius=Tune.CollarRadius;collarShape.enabled=false;
            knotNode.gameObject.AddComponent<Hurtbox>().owner=collar;
            // A ring of light round the arm's shoulder, its axis along the arm: the collar a player is told to strike. It was a bare sphere primitive, a placeholder by the zone rule
            // (QaPlanWorldTests.NoPlaceholderIsDrawnInAnyZone, six times in the Works).
            var drawnKnot=new GameObject("Collar ring",typeof(MeshFilter),typeof(MeshRenderer));drawnKnot.transform.SetParent(knotNode,false);
            drawnKnot.GetComponent<MeshFilter>().sharedMesh=RingMesh;knot=drawnKnot.GetComponent<Renderer>();knot.sharedMaterial=ActorFactory.Energy(Held);
            knot.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;knot.receiveShadows=false;
            collar.Died+=(_,packet)=>Release(false);
            // Any hero's blow on it, landed or shut out, means the fight is on and the press knows it: the boss listens to this.
            collar.Damaged+=(_,packet,amount)=>{if(amount>0)CombatActor.PayHit(packet,amount);};
            road=MakeRoad();road.SetActive(false);
            Apply(pitch);
        }
        void Start()
        {
            // Its boss already beaten: down and still, road solid, nothing to strike. No fall is played.
            if(GameServices.Current!=null&&GameServices.Current.Flags.GetBool("bossdown."+settledBy))Settle(true);
        }
        /// <summary>The generated arm lies along its own longest side with its foot at the origin. Laid along the swing's forward with its collar end (the broad gold ring) at the swing's origin and its
        /// middle on the axis. Which end is the ring is read from the mesh where it can be: the broader end.</summary>
        void Align(Transform arm)
        {
            arm.localRotation=Quaternion.identity;arm.localPosition=Vector3.zero;arm.localScale=Vector3.one;
            var filters=arm.GetComponentsInChildren<MeshFilter>();if(filters.Length==0)return;
            var bounds=new Bounds();bool first=true;
            foreach(var filter in filters){if(filter.sharedMesh==null)continue;var b=filter.sharedMesh.bounds;foreach(var corner in Corners(b)){var p=arm.InverseTransformPoint(filter.transform.TransformPoint(corner));if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}}
            bool alongX=bounds.size.x>bounds.size.z;float half=(alongX?bounds.size.x:bounds.size.z)*.5f;
            // Which end of the long axis the ring is at. The broader end is the ring where the mesh can be read; the generated SaintArm is imported unreadable, and guessed at +1 its six
            // flanges stood at the raised hands, 7 m up, with the hands at the press (the hall's stills, ch9-wr-01): its ring is at the negative end.
            float ringEnd=Tune.RingEnd;
            foreach(var filter in filters)
            {
                var mesh=filter.sharedMesh;if(mesh==null||!mesh.isReadable)continue;
                float broadPlus=0,broadMinus=0;
                foreach(var v in mesh.vertices)
                {
                    var p=arm.InverseTransformPoint(filter.transform.TransformPoint(v));float along=(alongX?p.x:p.z)-(alongX?bounds.center.x:bounds.center.z);
                    float spread=Mathf.Abs(p.y-bounds.center.y)+Mathf.Abs((alongX?p.z-bounds.center.z:p.x-bounds.center.x));
                    if(along>half*.78f)broadPlus=Mathf.Max(broadPlus,spread);else if(along<-half*.78f)broadMinus=Mathf.Max(broadMinus,spread);
                }
                ringEnd=broadPlus>=broadMinus?1:-1;break;
            }
            // Turn the long axis onto +z with the ring end at the origin and the rest of the arm out along +z (the ring's end goes to -z), then hang its middle on the axis.
            // Yaw 90 takes +x to -z, -90 takes -x to -z; 180 takes +z to -z.
            var turn=alongX?Quaternion.Euler(0,ringEnd>0?90:-90,0):Quaternion.Euler(0,ringEnd>0?180:0,0);
            arm.localRotation=turn;
            var ring=alongX?new Vector3(bounds.center.x+ringEnd*half,bounds.center.y,bounds.center.z):new Vector3(bounds.center.x,bounds.center.y,bounds.center.z+ringEnd*half);
            arm.localPosition=-(turn*ring);
            // Scaled to the room's length whatever it was taken in at.
            float scale=Tune.Length/Mathf.Max(.1f,half*2);if(Mathf.Abs(scale-1)>.02f){arm.localScale=Vector3.one*scale;arm.localPosition*=scale;}
        }
        static IEnumerable<Vector3> Corners(Bounds b){for(int i=0;i<8;i++)yield return new Vector3((i&1)==0?b.min.x:b.max.x,(i&2)==0?b.min.y:b.max.y,(i&4)==0?b.min.z:b.max.z);}
        /// <summary>The road, made once and kept off until the arm lies still: a solid box under the arm's top, from the floor at its foot to the collar, a box deep, with no Health over it so a
        /// danger marking follows its surface and the cover rule sees it.</summary>
        GameObject MakeRoad()
        {
            var go=new GameObject("Road");go.transform.SetParent(transform,false);
            float rise=Tune.RoadTopHeight,run=Tune.RoadFootOut-Tune.RoadTopOut,slope=Mathf.Atan2(rise,run)*Mathf.Rad2Deg,length=Mathf.Sqrt(rise*rise+run*run);
            var topMiddle=new Vector3(0,-Tune.CollarHeight+rise*.5f,(Tune.RoadTopOut+Tune.RoadFootOut)*.5f);
            var rotation=Quaternion.Euler(slope,0,0);
            go.transform.localRotation=rotation;go.transform.localPosition=topMiddle-rotation*Vector3.up*(Tune.Thickness*.5f);
            var box=go.AddComponent<BoxCollider>();box.size=new Vector3(Tune.RoadWidth,Tune.Thickness,length+.2f);roadBox=box;
            // Its plan, cut out of the paths once it lies there (route 59, November 14): the partner, and anything else that walks by the bake, goes round a fallen arm's high end, which it can neither pass
            // under (a metre clear of the floor, and a body is 1.9 m) nor step onto, and round its foot. Level, on the arm's root (which never turns), from behind the collar to past the foot.
            footprint=new GameObject("Road footprint (off the paths)");footprint.transform.SetParent(transform,false);
            float behind=-.2f,past=Tune.RoadFootOut+.1f;footprint.transform.localPosition=new Vector3(0,-Tune.CollarHeight+1.2f,(behind+past)*.5f);
            var hole=footprint.AddComponent<NavMeshObstacle>();hole.shape=NavMeshObstacleShape.Box;hole.size=new Vector3(Tune.RoadWidth,2.4f,past-behind);hole.center=Vector3.zero;hole.carving=true;hole.carveOnlyStationary=false;
            footprint.SetActive(false);
            return go;
        }
        void Apply(float at)
        {
            pitch=at;swing.localRotation=Quaternion.Euler(0,yaw,0)*Quaternion.Euler(at,0,0);
            // The ring turning as it lets go: the arm about its own long axis, which lies on the swing's forward through the collar.
            if(drawn!=null){var spin=Quaternion.AngleAxis(turned,Vector3.forward);drawn.localRotation=spin*alignedRotation;drawn.localPosition=spin*alignedPosition;}
        }
        Quaternion alignedRotation=Quaternion.identity;Vector3 alignedPosition;

        /// <summary>The boss has woken: its collar can be struck (resisted until unlocked), at this integrity, weakness and resistance.</summary>
        public void Claim(float integrity,DamageType weakness,DamageType resistance)
        {
            if(released)return;
            claimed=true;collar.maximum=collar.integrity=Mathf.Max(1,integrity);collar.weakness=weakness;collar.resistance=resistance;collar.Damageable=true;collarShape.enabled=true;
        }
        /// <summary>The collar answers blows for this long (the arm planted, or the head lifting).</summary>
        public void Unlock(float seconds){if(claimed&&!released)unlockedUntil=Mathf.Max(unlockedUntil,Time.time+seconds);}
        public void Lock(){unlockedUntil=-1;}
        /// <summary>No more answering: the fight has moved past its collars (the rig is bare), or its boss is gone.</summary>
        public void Shut(){unlockedUntil=-1;if(!released){collar.Damageable=false;collarShape.enabled=false;}}
        /// <summary>Swing to a pitch over a time (a raised arm's tell, a slam's fall); and its sweep's turn about the collar.</summary>
        public void SwingTo(float to,float seconds){if(released)return;goal=to;rate=Mathf.Abs(to-pitch)/Mathf.Max(.02f,seconds);}
        public void TurnTo(float degrees,float seconds){if(released)return;yawGoal=degrees;yawRate=Mathf.Abs(degrees-yaw)/Mathf.Max(.02f,seconds);}
        /// <summary>Down on the floor for a while (a slam's weight on it, its collar unlocked with it), then back up by itself, so an interrupted move never leaves an arm lying that is still the Saint's.</summary>
        public void Plant(float seconds){if(released)return;SwingTo(Tune.DownPitch,.12f);plantedUntil=Time.time+seconds;Unlock(seconds);}
        public bool Planted=>!released&&Time.time<plantedUntil;
        /// <summary>The override's cable, from the rig to this collar: taut cyan while it holds, slack and dark once released.</summary>
        public void Cable(Vector3 from)
        {
            cableFrom=from;
            if(cable==null)
            {
                var go=new GameObject("Override cable");go.transform.SetParent(transform,false);cable=go.AddComponent<LineRenderer>();cable.useWorldSpace=true;cable.positionCount=8;
                cable.startWidth=.11f;cable.endWidth=.09f;cable.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;cable.receiveShadows=false;
            }
            cabled=true;DrawCable();
        }
        void DrawCable()
        {
            if(cable==null)return;cable.sharedMaterial=ActorFactory.Energy(released?Slack:Held);
            var to=knotNode.position;float sag=released?1.4f:.15f;
            for(int i=0;i<8;i++){float k=i/7f;cable.SetPosition(i,Vector3.Lerp(cableFrom,to,k)+Vector3.down*(Mathf.Sin(k*Mathf.PI)*sag));}
        }
        /// <summary>Lets the arm go: the ring turns, the cyan goes out, it comes down and lies still, and its road turns solid. <paramref name="settling"/>: the press settling (its last
        /// three, after the rig), the same fall. A blow that kills the collar calls it; so does the boss when its bar has gone past this arm's line with the arm still on.</summary>
        public void Release(bool settling)
        {
            if(released)return;
            released=true;unlockedUntil=-1;plantedUntil=-1;collar.Damageable=false;collarShape.enabled=false;
            goal=Tune.DownPitch;rate=Mathf.Max(5,Mathf.Abs(Tune.DownPitch-pitch)/Tune.Falls);yawGoal=0;yawRate=120;
            AudioManager.PlayAt("doorOpen_000",transform,transform.position,.3f,AudioBus.SFX,50,.05f,.55f);
            CombatVfx.Burst(transform.position,new Color(1.2f,.8f,.45f),"crack");
            if(cabled)DrawCable();
            Debug.Log($"SAINT_ARM_LET_GO {index} {(settling?"settle":"collar")}");
            LetGo?.Invoke(this,settling);
        }
        /// <summary>At load with its boss down: down and still with nothing played.</summary>
        public void Settle(bool atOnce)
        {
            if(!released)Release(true);
            if(atOnce){Apply(Tune.DownPitch);goal=pitch;yaw=yawGoal=0;turned=Tune.RingTurns;Apply(pitch);Rest();PowerDown();}
        }
        bool poweredDown;
        /// <summary>Its press has settled (D385): the arm is powered down with it, its paint dark as the press's (BodyGoesDark, shut down: half, its lamps out). Once.</summary>
        public void PowerDown()
        {
            if(poweredDown)return;poweredDown=true;
            var dark=GetComponent<BodyGoesDark>();if(dark==null)dark=gameObject.AddComponent<BodyGoesDark>();
            dark.Begin(transform,false);
        }
        public bool PoweredDown=>poweredDown;
        void Rest()
        {
            if(resting)return;resting=true;
            // Anyone it would hold is put on the floor clear of it before it turns solid (D258, and the Saint's fall in route 59, November 14): standing where it lies, under its high end (which clears the floor by a metre
            // and stands a hand off the press's back), or with any of the body where the road will be (up on the next road's top beside it). Only whoever stood on its plan a stride out
            // from the collar used to be moved: a hero against the press's back under the collar stayed, and the road came down on his shoulders. He goes to the nearest floor where he
            // stands clear of everything, on the camera's side of the collars.
            if(PartyController.Current!=null)foreach(var member in PartyController.Current.members)
            {
                if(member==null)continue;var body=member.GetComponent<CharacterController>();var at=member.transform.position;
                bool under=OnRoad(at)&&at.y<=Floor+.6f;
                if(!under&&(body==null||!Holds(roadBox,body,at,body.skinWidth)))continue;
                if(body!=null&&StandClear(member,Floor,CollarFloor,Back,(b,p)=>Holds(roadBox,b,p,b.skinWidth),"arm "+index))continue;
                // Nowhere clear in reach (never, in the hall): beside it, as it always was.
                var side=Vector3.Cross(Vector3.up,Outward);float across=Vector3.Dot(Flat(at-CollarFloor),side);
                var to=at+side*((across>=0?1:-1)*(Tune.RoadWidth*.5f+.65f)-across);
                if(body!=null)body.enabled=false;member.transform.position=to;if(body!=null)body.enabled=true;
            }
            road.SetActive(true);if(footprint!=null)footprint.SetActive(true);Physics.SyncTransforms();
            AudioManager.PlayAt("impactMetal_000",transform,RoadPoint(.5f),.32f,AudioBus.SFX,50,.05f,.55f);
            for(int i=0;i<4;i++)CombatVfx.Burst(RoadPoint(i/3f)+Vector3.up*.2f,new Color(.55f,.52f,.48f),"kill");
            Debug.Log("SAINT_ARM_DOWN "+index);
            Released?.Invoke(this);
        }
        void Update()
        {
            if(made&&maker==null){Destroy(gameObject);return;}
            if(GameTime.Paused)return;
            float dt=Time.deltaTime;
            // A planted arm the Saint has stopped holding down rises again by itself.
            if(!released&&plantedUntil>=0&&Time.time>=plantedUntil){plantedUntil=-1;SwingTo(Tune.RaisedPitch,.8f);}
            float next=Mathf.MoveTowards(pitch,goal,rate*dt);yaw=Mathf.MoveTowards(yaw,yawGoal,yawRate*dt);
            if(released&&turned<Tune.RingTurns)turned=Mathf.MoveTowards(turned,Tune.RingTurns,Tune.RingTurns/Tune.Falls*dt);
            Apply(next);
            if(released&&!resting&&Mathf.Abs(pitch-Tune.DownPitch)<.01f&&Mathf.Abs(yaw)<.01f)Rest();
            Looks();
            if(cabled&&!released)DrawCable();
        }
        void Looks()
        {
            if(knot==null)return;
            // Cyan while the override holds the collar, amber and breathing while it answers a blow, gone once let go.
            knot.enabled=!released;
            if(released)
            {
                // A released arm's own lamps go warm: home's again. Once its press has settled the whole machine is powered down (PowerDown) and they are out.
                if(!poweredDown&&armRenderers!=null)foreach(var renderer in armRenderers){if(renderer==null)continue;renderer.GetPropertyBlock(block);block.SetColor("_EmissionColor",WarmLamp*.35f);renderer.SetPropertyBlock(block);}
                return;
            }
            bool open=Unlocked;knot.sharedMaterial=ActorFactory.Energy(open?Open:Held);
            float k=open?1+.12f*Mathf.Sin(Time.time*9):1;knot.transform.localScale=Vector3.one*k;
        }
        static Mesh ringMesh;
        /// <summary>The collar's ring: a torus <see cref="Tune.RingRadius"/> round, <see cref="Tune.RingTube"/> thick, its axis along local +z (the arm's bearing).</summary>
        static Mesh RingMesh
        {
            get
            {
                if(ringMesh!=null)return ringMesh;
                const int round=28,tube=8;var vertices=new Vector3[(round+1)*(tube+1)];var normals=new Vector3[vertices.Length];var triangles=new int[round*tube*6];
                for(int i=0;i<=round;i++)for(int j=0;j<=tube;j++)
                {
                    float a=i*Mathf.PI*2/round,b=j*Mathf.PI*2/tube;var centre=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0)*Tune.RingRadius;
                    var normal=new Vector3(Mathf.Cos(a)*Mathf.Cos(b),Mathf.Sin(a)*Mathf.Cos(b),Mathf.Sin(b));
                    vertices[i*(tube+1)+j]=centre+normal*Tune.RingTube;normals[i*(tube+1)+j]=normal;
                }
                int n=0;for(int i=0;i<round;i++)for(int j=0;j<tube;j++)
                {
                    int a=i*(tube+1)+j,b=a+tube+1;
                    triangles[n++]=a;triangles[n++]=b;triangles[n++]=a+1;triangles[n++]=a+1;triangles[n++]=b;triangles[n++]=b+1;
                }
                ringMesh=new Mesh{name="Saint collar ring",vertices=vertices,normals=normals,triangles=triangles};ringMesh.RecalculateBounds();
                return ringMesh;
            }
        }

        /// <summary>For a room with no arms of its own (the proving yard): an arm made where the boss stands it, as a builder would place it. It goes when <paramref name="maker"/> does, so a
        /// yard that sees several presses never hands one's arms (or roads) to the next.</summary>
        public static SaintArm Make(string boss,int index,Vector3 collar,float bearing,Object maker=null)
        {
            var go=new GameObject("Saint arm "+index);go.SetActive(false);go.transform.SetPositionAndRotation(collar,Quaternion.Euler(0,bearing,0));
            var arm=go.AddComponent<SaintArm>();arm.index=index;arm.settledBy=boss;arm.maker=maker;arm.made=maker!=null;go.SetActive(true);return arm;
        }
        Object maker;bool made;

        // ---------------------------------------------------------------- nobody held by what comes down (route 59, November 14)
        /// <summary>Whether a box, live or not yet (a road still in the air: its pose is already set), would hold a body standing at <paramref name="at"/>: some point of the body's axis is
        /// nearer the box than its radius and <paramref name="slack"/> (its skin, for something about to appear; less than nothing, for only a body already inside).</summary>
        public static bool Holds(BoxCollider box,CharacterController body,Vector3 at,float slack)
        {
            if(box==null||body==null)return false;var t=box.transform;var s=t.lossyScale;
            return Holds(t.TransformPoint(box.center),t.rotation,Vector3.Scale(box.size,new Vector3(Mathf.Abs(s.x),Mathf.Abs(s.y),Mathf.Abs(s.z)))*.5f,body,at,slack);
        }
        public static bool Holds(Vector3 centre,Quaternion rotation,Vector3 half,CharacterController body,Vector3 at,float slack)
        {
            float reach=Mathf.Max(.01f,body.radius+slack);var inverse=Quaternion.Inverse(rotation);
            var bottom=at+body.center+Vector3.down*(body.height*.5f-body.radius);var top=at+body.center+Vector3.up*(body.height*.5f-body.radius);
            for(int i=0;i<=8;i++)
            {
                var local=inverse*(Vector3.Lerp(bottom,top,i/8f)-centre);
                var outside=new Vector3(Mathf.Max(0,Mathf.Abs(local.x)-half.x),Mathf.Max(0,Mathf.Abs(local.y)-half.y),Mathf.Max(0,Mathf.Abs(local.z)-half.z));
                if(outside.sqrMagnitude<reach*reach)return true;
            }
            return false;
        }
        static readonly Collider[] touching=new Collider[16];
        /// <summary>Steps a body onto the nearest floor where it stands clear of everything solid and of <paramref name="coming"/> (what is about to turn solid), on the camera's side of a line
        /// across the press's back (<paramref name="line"/>, facing <paramref name="back"/>) by a body's width: searched in rings out to <see cref="Tune.ClearReach"/>, each from the way off the
        /// press's back round either side. So the press's frame, its legs and the roads' high ends never keep anyone, and nobody comes down inside another hero. Logs <c>SAINT_STOOD_CLEAR</c>;
        /// false (and the body left where it was) with no such floor in reach.</summary>
        public static bool StandClear(CombatActor member,float floor,Vector3 line,Vector3 back,System.Func<CharacterController,Vector3,bool> coming,string why)
        {
            var body=member!=null?member.GetComponent<CharacterController>():null;if(body==null)return false;
            back.y=0;back=back.sqrMagnitude>.0001f?back.normalized:Vector3.back;
            var from=member.transform.position;float room=body.radius+body.skinWidth;
            for(float d=0;d<=Tune.ClearReach+.001f;d+=Tune.ClearStep)
            {
                int around=d<.01f?1:Mathf.Max(8,Mathf.CeilToInt(2*Mathf.PI*d/Tune.ClearStep));
                for(int k=0;k<around;k++)
                {
                    int turn=(k+1)/2*(k%2==1?1:-1);
                    var at=new Vector3(from.x,floor,from.z)+Quaternion.Euler(0,turn*360f/around,0)*back*d;
                    if(Vector3.Dot(at-line,back)<room||!FloorUnder(ref at,floor)||coming!=null&&coming(body,at)||Crowded(member,body,at))continue;
                    body.enabled=false;member.transform.position=at+Vector3.up*.02f;body.enabled=true;if(member.motor is GroundMotor ground)ground.Halt();Physics.SyncTransforms();
                    Debug.Log($"SAINT_STOOD_CLEAR {member.character} {why} from {from:F2} to {at:F2}");
                    return true;
                }
            }
            Debug.LogWarning($"SAINT_NO_CLEAR_FLOOR {member.character} {why} at {from:F2}");
            return false;
        }
        /// <summary>The hall's own floor under a plan point (within a hand of its height, not a road, a crate or a body), and its height there.</summary>
        static bool FloorUnder(ref Vector3 at,float floor)
        {
            if(!Physics.Raycast(new Vector3(at.x,floor+1.2f,at.z),Vector3.down,out var hit,1.8f,~0,QueryTriggerInteraction.Ignore))return false;
            if(Mathf.Abs(hit.point.y-floor)>.25f||hit.collider is CharacterController||hit.collider.GetComponentInParent<Health>()!=null)return false;
            at.y=hit.point.y;return true;
        }
        /// <summary>Anything solid (another hero, the press, a road, a wall) where a body standing here would be, with its skin's room, from a little over the floor to its head.</summary>
        static bool Crowded(CombatActor member,CharacterController body,Vector3 at)
        {
            float room=body.radius+body.skinWidth;
            var bottom=at+body.center+Vector3.down*(body.height*.5f-body.radius)+Vector3.up*(body.skinWidth+.15f);var top=at+body.center+Vector3.up*(body.height*.5f-body.radius);
            int n=Physics.OverlapCapsuleNonAlloc(bottom,top,room,touching,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<n;i++)if(touching[i]!=null&&!touching[i].transform.IsChildOf(member.transform))return true;
            return false;
        }
        /// <summary>The arms that answer to a boss, by index.</summary>
        public static List<SaintArm> For(string boss)
        {
            var list=new List<SaintArm>();foreach(var arm in All)if(arm!=null&&(string.IsNullOrEmpty(arm.settledBy)||arm.settledBy==boss))list.Add(arm);
            list.Sort((a,b)=>a.index.CompareTo(b.index));return list;
        }
    }
}
