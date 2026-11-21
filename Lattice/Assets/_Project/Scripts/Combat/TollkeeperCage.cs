using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The Tollkeeper's cage (chapter 10, Last Mooring; docs/maps/LastMooring.md, "What the fight needs of the room"): the machine its fight works, stood at the gate by the zone
    /// (<c>LastMooringBuilder.Tollkeeper.StandCage</c>), by a test in a yard, or by the fight itself if the zone has none. Two outriggers (the <c>TollkeeperOutrigger</c> body, its drum on
    /// the hinge) lie open south from the gate's two hinges, their bars under the plane so ships fly over them; on each three <b>braces</b> stand up to a coupling at the plane (a break
    /// target, struck only from flight); along each four lamps light in turn as the cage's clock runs to its next contraction (the clock a player reads). The <b>arm</b> (the zone's, a
    /// berth) is run out of the gate and drawn in by its root, only while its berth may move (nobody on it, no change, no conversation: wiring 6.1 rule 5, D275). On its seat the
    /// <b>gate motor</b> (a break target, struck only on foot, kept by a Clamp Tender while one works it). Nothing of it is solid: what it does to a ship is its fight's marked strikes
    /// (D289: nothing it makes can wall a hero in). It keeps what the save says (braces broken, the cage hanging open after the fall) as it wakes. Every number is
    /// <see cref="BossController.TollTune"/>'s.
    /// </summary>
    public sealed class TollkeeperCage:MonoBehaviour
    {
        public static TollkeeperCage Current{get;private set;}
        /// <summary>The gate's two hinges at the plane (west, east), the carrier's drawn middle, where the lanes the guns fire down end (its distance south of the hinges), the dividing
        /// chains' and outer chains' offsets across the gate from its middle.</summary>
        public Vector3 hingeWest=new(-40,1,468),hingeEast=new(40,1,468),carrier=new(0,-29.6f,492);
        public float lanesRun=82;
        public float[] dividers={-18,18};
        public float outer=58;
        /// <summary>The arm's root (moved whole), where it stands run out and drawn in, the motor's seat on it, its berth's id.</summary>
        public Transform arm,seat;
        public Vector3 armOut=new(0,-2,450),armIn=new(0,-8,486);
        public string berthId="Arm";
        /// <summary>The outrigger's body (the Tollkeeper's EnemyDef carries it as <see cref="EnemyDef.bodySegment"/>; a builder may set it).</summary>
        public GameObject outriggerPrefab;
        public const int Braces=3;
        /// <summary>What it is painted in: the Compact's grey and slate where it is shut or still, the amber of a live weld where it can be struck or is running (D217 keeps orange off
        /// the floor: these are on the machine). Solid, lit by the scene, so a coupling reads on the white bars it stands on; what glows, glows.</summary>
        static readonly Color CouplingShut=new(.32f,.5f,.56f),CouplingOpen=new(1f,.56f,.16f),LampOff=new(.14f,.16f,.19f),LampOn=new(1f,.5f,.14f),
            MotorLive=new(1f,.45f,.12f),MotorDead=new(.16f,.17f,.19f),Strut=new(.55f,.58f,.62f),MotorIron=new(.3f,.31f,.34f);
        static readonly Dictionary<(Color,bool),Material> paints=new();
        /// <summary>A solid colour (the blockout's toon body), or a glowing one (its emission), made once a colour.</summary>
        static Material Paint(Color colour,bool glow)
        {
            if(paints.TryGetValue((colour,glow),out var made)&&made!=null)return made;
            var source=Resources.Load<Material>(glow?"Blockout/Emission":"Blockout/Enemy");
            made=source!=null?new Material(source):new Material(Shader.Find("Universal Render Pipeline/Lit"));made.name="Tollkeeper cage "+(glow?"glow ":"")+ColorUtility.ToHtmlStringRGB(colour);
            made.SetColor("_BaseColor",colour);if(glow&&made.HasProperty("_EmissionColor"))made.SetColor("_EmissionColor",colour*3.2f);
            if(made.HasProperty("_BaseMap"))made.SetTexture("_BaseMap",Plate);
            paints[(colour,glow)]=made;return made;
        }
        static Texture2D plate;
        /// <summary>The Compact's plate in courses, its seams a shade darker, a little wear across it: what every part of the cage is made of, so a coupling or a strut reads as worked
        /// metal beside the generated bars, not a bare shape (a primitive with no surface is a placeholder: QaPlanWorldTests).</summary>
        static Texture2D Plate
        {
            get
            {
                if(plate!=null)return plate;
                const int Size=64;plate=new Texture2D(Size,Size,TextureFormat.RGBA32,true){name="Tollkeeper cage plate",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};
                var pixels=new Color[Size*Size];var random=new System.Random(47);
                for(int y=0;y<Size;y++)for(int x=0;x<Size;x++)
                {
                    bool seam=x%16==0||(y+(x/16%2)*8)%16==0;float wear=.94f+.06f*(float)random.NextDouble();float v=(seam?.72f:1)*wear;
                    pixels[y*Size+x]=new Color(v,v,v,1);
                }
                plate.SetPixels(pixels);plate.Apply(true,true);return plate;
            }
        }

        /// <summary>One outrigger: its pivot at its hinge (forward along the bar), its braces and lamps, its angle (0 across the gate at the other hinge, 90 lying straight down the lanes; at rest <see cref="BossController.TollTune.CageLies"/>) and pitch.</summary>
        public sealed class Rigger
        {
            public Transform pivot;public Vector3 hinge;public bool west;public float angle=BossController.TollTune.CageLies,pitch,drop;
            public readonly List<Brace> braces=new();public readonly List<Renderer> lamps=new();
        }
        public sealed class Brace{public int index;public float along;public Health health;public Transform node;public Renderer knot;public SphereCollider shape;public bool broken,open;}
        public readonly Rigger[] riggers={new(),new()};
        public readonly List<Brace> braces=new();
        /// <summary>The motor on its seat: its target, its look, what a tender keeps of it.</summary>
        public Health Motor{get;private set;}
        public Tended Kept{get;private set;}
        Renderer motorCore;Transform motorBody;CapsuleCollider motorShape;
        float armK=1;bool built,hanging;
        public bool Hanging=>hanging;
        /// <summary>The middle of the gate at the plane, the way across it (west to east), the way its lanes run (away from the gate), half its opening.</summary>
        public Vector3 Middle=>(hingeWest+hingeEast)*.5f;
        public Vector3 Across{get{var a=hingeEast-hingeWest;a.y=0;return a.sqrMagnitude>.01f?a.normalized:Vector3.right;}}
        public Vector3 South=>Vector3.Cross(Vector3.up,Across);
        public float Half=>Vector3.Distance(hingeWest,hingeEast)*.5f;
        public float Plane=>Middle.y;
        /// <summary>How far run out the arm is (0 in, 1 out).</summary>
        public float ArmOut=>armK;
        public IBerth Berth=>ZoneController.FindBerth(berthId);

        void OnEnable(){Current=this;}
        void OnDisable(){if(Current==this)Current=null;}
        void Start(){Build();}
        /// <summary>Everything it draws and every target, once; the save read for what is already broken.</summary>
        public void Build()
        {
            if(built)return;built=true;
            if(arm!=null&&seat==null){var found=arm.Find("Gate motor seat");if(found!=null)seat=found;}
            for(int r=0;r<2;r++)
            {
                var rigger=riggers[r];rigger.west=r==0;rigger.hinge=r==0?hingeWest:hingeEast;
                var pivot=new GameObject(rigger.west?"West outrigger":"East outrigger").transform;pivot.SetParent(transform,false);rigger.pivot=pivot;
                if(outriggerPrefab!=null)
                {
                    var body=Instantiate(outriggerPrefab,pivot,false);body.name="Outrigger";
                    foreach(var c in body.GetComponentsInChildren<Collider>(true))Destroy(c);
                    // The model's length lies along its own x with its drum at its -x end: laid along the pivot's forward, the drum on the hinge, the bar's middle under the plane.
                    body.transform.localRotation=Quaternion.Euler(0,-90,0);
                    var drawn=LocalBounds(body,pivot);
                    body.transform.localPosition+=new Vector3(-drawn.center.x,BossController.TollTune.BarBelow-drawn.center.y,-drawn.min.z-Mathf.Min(drawn.size.x,drawn.size.y)*.5f);
                }
                for(int b=0;b<Braces;b++)rigger.braces.Add(MakeBrace(rigger,r*Braces+b,BossController.TollTune.BraceAt[b]));
                for(int l=0;l<BossController.TollTune.Lamps;l++)
                {
                    var lamp=GameObject.CreatePrimitive(PrimitiveType.Sphere);lamp.name="Cage clock lamp";Destroy(lamp.GetComponent<Collider>());lamp.transform.SetParent(pivot,false);
                    lamp.transform.localPosition=new Vector3(0,BossController.TollTune.BarBelow+1.5f,6+l*(BossController.TollTune.Length-8)/(BossController.TollTune.Lamps-1));lamp.transform.localScale=Vector3.one*1.4f;
                    var lit=lamp.GetComponent<Renderer>();lit.sharedMaterial=Paint(LampOff,false);lit.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;rigger.lamps.Add(lit);
                }
                braces.AddRange(rigger.braces);
            }
            MakeMotor();
            bool down=Restore();if(!down)Pose();
            Clock(0);Debug.Log($"TOLL_CAGE stands braces={braces.Count} broken={Broken} hanging={down} arm={(arm!=null)} seat={(seat!=null)}");
        }
        /// <summary>What the save says, again: every brace whose flag is set broken, and after the fall the cage hanging open with its motor dark. Its fight asks as it wakes too: a cage
        /// stood before the save's flags were read (a yard's, a fixture's) stood whole. True after the fall.</summary>
        public bool Restore()
        {
            var flags=GameServices.Current!=null?GameServices.Current.Flags:null;if(flags==null)return false;
            foreach(var brace in braces)if(!brace.broken&&flags.GetBool(BossController.TollTune.BraceFlag(brace.index)))Break(brace,true);
            bool down=flags.GetBool("bossdown.Tollkeeper");if(down){HangOpen(1);ShowMotor(false);}
            return down;
        }
        static Bounds LocalBounds(GameObject body,Transform space)
        {
            bool any=false;var b=new Bounds();
            foreach(var r in body.GetComponentsInChildren<Renderer>())
            {
                var w=r.bounds;
                for(int i=0;i<8;i++){var corner=space.InverseTransformPoint(w.center+Vector3.Scale(w.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(!any){b=new Bounds(corner,Vector3.zero);any=true;}else b.Encapsulate(corner);}
            }
            return b;
        }
        Brace MakeBrace(Rigger rigger,int index,float along)
        {
            var node=new GameObject("Cage brace "+(index+1));node.transform.SetParent(rigger.pivot,false);node.transform.localPosition=new Vector3(0,0,along);
            // A strut from the bar up to the coupling: what holds the cage's tension, and what breaks.
            var strut=GameObject.CreatePrimitive(PrimitiveType.Cylinder);strut.name="Strut";Destroy(strut.GetComponent<Collider>());strut.transform.SetParent(node.transform,false);
            float low=BossController.TollTune.BarBelow+1.2f;strut.transform.localPosition=new Vector3(0,low*.5f,0);strut.transform.localScale=new Vector3(.7f,Mathf.Abs(low)*.5f,.7f);
            strut.GetComponent<Renderer>().sharedMaterial=Paint(Strut,false);
            var drawn=GameObject.CreatePrimitive(PrimitiveType.Sphere);drawn.name="Coupling";Destroy(drawn.GetComponent<Collider>());drawn.transform.SetParent(node.transform,false);drawn.transform.localScale=Vector3.one*BossController.TollTune.BraceDrawn;
            var knot=drawn.GetComponent<Renderer>();knot.sharedMaterial=Paint(CouplingShut,false);knot.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            Dress(node.transform,BossController.TollTune.BraceDrawn*.5f);
            var part=node.AddComponent<Health>();part.id="Cage brace";part.maximum=part.integrity=BossController.TollTune.BraceIntegrity;part.weakness=DamageType.Pulse;part.resistance=DamageType.Kinetic;part.breakThreshold=1e9f;part.Damageable=false;
            var shape=node.AddComponent<SphereCollider>();shape.isTrigger=true;shape.radius=BossController.TollTune.BraceTarget;shape.enabled=false;
            node.AddComponent<Hurtbox>().owner=part;
            var brace=new Brace{index=index,along=along,health=part,node=node.transform,knot=knot,shape=shape};
            part.Died+=(_,__)=>Break(brace,false);
            return brace;
        }
        /// <summary>The coupling as a machine part, not a ball (the look pass of November 14: the couplings a player breaks were plain spheres): a socket it sits in on the strut's head, a
        /// flange round it with its ring of bolts, and two jaws gripping it from either side across the bar. Drawn only and under the brace's node, so they go when it breaks
        /// (<see cref="Break"/> turns off every renderer under it); the knot itself, its colours, its target and its hit shape are as they were, and its top stays open to the camera.</summary>
        static void Dress(Transform node,float r)
        {
            Part(PrimitiveType.Cylinder,"Coupling socket",node,new Vector3(0,-r*.68f,0),new Vector3(r*1.9f,r*.34f,r*1.9f),MotorIron);
            // The collar round its middle, wide enough to read as a ring from the flight camera over it, with its bolts on top; a bolted cap on its crown.
            Part(PrimitiveType.Cylinder,"Coupling collar",node,new Vector3(0,-r*.05f,0),new Vector3(r*2.56f,r*.14f,r*2.56f),Strut);
            Part(PrimitiveType.Cylinder,"Coupling collar band",node,new Vector3(0,r*.06f,0),new Vector3(r*2.2f,r*.06f,r*2.2f),MotorIron);
            for(int k=0;k<10;k++){float a=k*36*Mathf.Deg2Rad;Part(PrimitiveType.Cylinder,"Coupling bolt",node,new Vector3(Mathf.Sin(a)*r*1.18f,r*.1f,Mathf.Cos(a)*r*1.18f),new Vector3(r*.16f,r*.08f,r*.16f),MotorIron);}
            Part(PrimitiveType.Cylinder,"Coupling cap",node,new Vector3(0,r*.96f,0),new Vector3(r*.72f,r*.08f,r*.72f),Strut);
            Part(PrimitiveType.Cylinder,"Coupling cap boss",node,new Vector3(0,r*1.04f,0),new Vector3(r*.3f,r*.07f,r*.3f),MotorIron);
            foreach(int side in new[]{-1,1})
            {
                Part(PrimitiveType.Cube,"Coupling jaw",node,new Vector3(side*r*.98f,-r*.2f,0),new Vector3(r*.3f,r*1.1f,r*.8f),Strut);
                Part(PrimitiveType.Cube,"Coupling jaw pin",node,new Vector3(side*r*1.16f,-r*.2f,0),new Vector3(r*.1f,r*.3f,r*.96f),MotorIron);
            }
        }
        /// <summary>A painted primitive, drawn only (its collider gone at once, not at the frame's end), under a parent.</summary>
        static void Part(PrimitiveType kind,string name,Transform parent,Vector3 at,Vector3 size,Color colour)
        {
            var part=GameObject.CreatePrimitive(kind);part.name=name;DestroyImmediate(part.GetComponent<Collider>());part.transform.SetParent(parent,false);
            part.transform.localPosition=at;part.transform.localScale=size;
            var r=part.GetComponent<Renderer>();r.sharedMaterial=Paint(colour,false);r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        void MakeMotor()
        {
            var under=seat!=null?seat:transform;
            var body=new GameObject("Gate motor");body.transform.SetParent(under,false);motorBody=body.transform;
            var drum=GameObject.CreatePrimitive(PrimitiveType.Cylinder);drum.name="Motor drum";Destroy(drum.GetComponent<Collider>());drum.transform.SetParent(body.transform,false);
            drum.transform.localPosition=new Vector3(0,1,0);drum.transform.localScale=new Vector3(1.7f,1,1.7f);drum.GetComponent<Renderer>().sharedMaterial=Paint(MotorIron,false);
            var cap=GameObject.CreatePrimitive(PrimitiveType.Cylinder);cap.name="Motor cap";Destroy(cap.GetComponent<Collider>());cap.transform.SetParent(body.transform,false);
            cap.transform.localPosition=new Vector3(0,2.05f,0);cap.transform.localScale=new Vector3(1.2f,.1f,1.2f);cap.GetComponent<Renderer>().sharedMaterial=Paint(Strut,false);
            var core=GameObject.CreatePrimitive(PrimitiveType.Cylinder);core.name="Motor core";Destroy(core.GetComponent<Collider>());core.transform.SetParent(body.transform,false);
            core.transform.localPosition=new Vector3(0,1.15f,0);core.transform.localScale=new Vector3(1.82f,.22f,1.82f);motorCore=core.GetComponent<Renderer>();motorCore.sharedMaterial=Paint(MotorLive,true);
            var part=body.AddComponent<Health>();part.id="Gate motor";part.maximum=part.integrity=BossController.TollTune.MotorIntegrity;part.weakness=DamageType.Kinetic;part.resistance=DamageType.Beam;part.breakThreshold=1e9f;part.Damageable=false;
            motorShape=body.AddComponent<CapsuleCollider>();motorShape.isTrigger=true;motorShape.radius=1;motorShape.height=2.4f;motorShape.center=Vector3.up*1.2f;motorShape.enabled=false;
            body.AddComponent<Hurtbox>().owner=part;
            // A Clamp Tender that steps out on the deck keeps it: while one works it, a blow takes a quarter (BossController.TollTune.Tended).
            Kept=body.AddComponent<Tended>();Kept.seat=under;
            part.Plate=_=>Kept.Tending?BossController.TollTune.Tended:1;
            Motor=part;ShowMotor(false);
        }
        /// <summary>The motor on its seat shown (the arm out with a stage still running) or not; struck only while <paramref name="open"/>.</summary>
        public void ShowMotor(bool shown,bool open=false)
        {
            if(motorBody!=null&&motorBody.gameObject.activeSelf!=shown)motorBody.gameObject.SetActive(shown);
            if(Motor!=null&&Motor.Damageable!=(shown&&open)){Motor.Damageable=shown&&open;if(motorShape!=null)motorShape.enabled=shown&&open;}
            bool live=Motor!=null&&Motor.Alive&&releasedAt<0;if(motorCore!=null&&live!=motorLit){motorLit=live;motorCore.sharedMaterial=live?Paint(MotorLive,true):Paint(MotorDead,false);}
        }
        bool motorLit=true;
        /// <summary>The motor is struck now (shown, on the arm, with the party on it).</summary>
        public bool MotorOpen=>Motor!=null&&Motor.Damageable;
        /// <summary>A brace struck only from flight, and only while the fight lets it be (<paramref name="open"/>).</summary>
        public void OpenBraces(bool open)
        {
            foreach(var brace in braces)
            {
                bool on=open&&!brace.broken;if(brace.open==on)continue;brace.open=on;
                brace.health.Damageable=on;if(brace.shape!=null)brace.shape.enabled=on;if(brace.knot!=null)brace.knot.sharedMaterial=on?Paint(CouplingOpen,true):Paint(CouplingShut,false);
            }
        }
        public int Broken{get{int n=0;foreach(var b in braces)if(b.broken)n++;return n;}}
        public event System.Action<Brace> BraceBroken;
        void Break(Brace brace,bool loaded)
        {
            if(brace.broken)return;brace.broken=true;brace.open=false;
            if(brace.health!=null){brace.health.Damageable=false;if(loaded)brace.health.integrity=0;}
            if(brace.shape!=null)brace.shape.enabled=false;
            if(brace.node!=null)foreach(var r in brace.node.GetComponentsInChildren<Renderer>())r.enabled=false;
            if(!loaded){CombatVfx.Burst(brace.node.position,new Color(1.5f,.8f,.35f),"kill");AudioManager.PlayAt("explosionCrunch_000",null,brace.node.position,.32f,AudioBus.SFX,50,.05f,.8f);BraceBroken?.Invoke(brace);}
            Pose();
        }
        /// <summary>How far an outrigger sweeps from its hinge: its bar's length, or short of its nearest broken brace (the bar beyond it hangs: the gap).</summary>
        public float Reach(Rigger rigger)
        {
            float reach=BossController.TollTune.Length;
            foreach(var b in rigger.braces)if(b.broken)reach=Mathf.Min(reach,b.along-BossController.TollTune.BraceGap);
            return Mathf.Max(0,reach);
        }
        /// <summary>The way an outrigger points at this angle (0 across the gate at the other hinge, 90 away from it down the lanes).</summary>
        public Vector3 Along(Rigger rigger,float angle)
        {
            var across=rigger.west?Across:-Across;float a=angle*Mathf.Deg2Rad;
            return across*Mathf.Cos(a)+South*Mathf.Sin(a);
        }
        /// <summary>Both outriggers to an angle (and the fall's pitch and drop), drawn.</summary>
        public void Pose(float angle=-1)
        {
            foreach(var rigger in riggers)
            {
                if(rigger.pivot==null)continue;
                if(angle>=0)rigger.angle=angle;
                var along=Along(rigger,rigger.angle);
                rigger.pivot.SetPositionAndRotation(rigger.hinge+Vector3.down*rigger.drop,Quaternion.LookRotation(along)*Quaternion.Euler(rigger.pitch,0,0));
            }
        }
        int litLamps=-1;
        /// <summary>The cage's clock: <paramref name="share"/> of the way to its next contraction, lit in turn along both bars (the last as its tell begins); 0 dark.</summary>
        public void Clock(float share)
        {
            int lit=Mathf.Clamp(Mathf.FloorToInt(share*BossController.TollTune.Lamps+.0001f),0,BossController.TollTune.Lamps);if(lit==litLamps)return;litLamps=lit;
            foreach(var rigger in riggers)for(int i=0;i<rigger.lamps.Count;i++)if(rigger.lamps[i]!=null)rigger.lamps[i].sharedMaterial=i<lit?Paint(LampOn,true):Paint(LampOff,false);
        }
        public int LitLamps=>Mathf.Max(0,litLamps);

        // ---------------------------------------------------------------- the gate opens
        float releasedAt=-1;int beat;CameraRig rig;
        public bool Releasing=>releasedAt>=0&&!hanging;
        /// <summary>The motor is dead: the cage swings out and hangs, the arm draws in once nobody is on it, and the view holds the gate and the party while it goes (the moment the convoy
        /// is let through: the zone's hulls move from <c>bossdown.Tollkeeper</c>). Its own staging: only a pause holds it.</summary>
        public void Release()
        {
            if(releasedAt>=0)return;releasedAt=Time.time;beat=0;hangFrom=riggers[0].angle;ShowMotor(false);OpenBraces(false);Clock(0);
            AudioManager.PlayAt("joint_release",null,Middle,.5f,AudioBus.SFX,16,.2f,.7f);
            CombatVfx.Burst(Middle+Vector3.up*2,new Color(1.2f,1.1f,.8f),"kill");
            if(rig==null)rig=FindFirstObjectByType<CameraRig>();if(rig!=null)rig.Tremor(1.2f,.12f);
            Debug.Log("TOLL_CAGE_OPENS");
        }
        /// <summary>Lays the carrier's body (the boss, on show before its fight or fighting) as the zone draws it: its length across the way as taken in, its drawn middle on
        /// <see cref="carrier"/>. Made at its spawner, a body whose pivot is its keel stood 15 m high with its top over the plane. True once laid.</summary>
        public static bool LayCarrier(Transform body,Vector3 at)
        {
            if(body==null)return false;
            body.rotation=Quaternion.identity;
            bool any=false;var drawn=new Bounds();foreach(var r in body.GetComponentsInChildren<Renderer>()){if(!any){drawn=r.bounds;any=true;}else drawn.Encapsulate(r.bounds);}
            if(any)body.position+=at-drawn.center;
            return any;
        }
        float layUntil=-1;bool laid;GameObject standIn;Renderer[] bodyDrawn;
        void LayOnShow()
        {
            if(laid||Time.time>layUntil)return;
            foreach(var brain in FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
            {
                if(brain==null||brain.definition==null||brain.definition.id!="Tollkeeper")continue;
                var shown=brain.GetComponent<DefeatPresentation>();if(shown==null||shown.visual==null)continue;
                laid=LayCarrier(shown.visual,carrier);bodyDrawn=shown.visual.GetComponentsInChildren<Renderer>();
                if(laid)Debug.Log("TOLL_CARRIER_LAID "+carrier);
            }
        }
        void Update()
        {
            if(layUntil<0){layUntil=Time.time+5;foreach(var t in FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(t.name=="The Tollkeeper, as it stays (drawn after its fall)"){standIn=t.gameObject;break;}}
            LayOnShow();
            // After its fall the zone's own carrier stands where it lies: the fight's body gives way to it, or the two draw through each other.
            if(standIn!=null&&standIn.activeInHierarchy&&bodyDrawn!=null){foreach(var r in bodyDrawn)if(r!=null)r.enabled=false;bodyDrawn=null;}
            if(releasedAt<0||GameTime.Paused)return;
            float age=Time.time-releasedAt;
            if(!hanging){HangOpen(Mathf.SmoothStep(0,1,Mathf.Clamp01(age/BossController.TollTune.HangSeconds)));if(hanging)Debug.Log("TOLL_CAGE_HANGS");}
            if(beat==0&&age>=.5f){beat++;AudioManager.PlayAt("district_lift",null,Middle,.46f,AudioBus.SFX,16,.2f,.6f);}
            if(beat==1&&age>=BossController.TollTune.HangSeconds){beat++;AudioManager.PlayAt("doorOpen_000",null,Middle,.42f,AudioBus.SFX,16,.2f,.55f);if(rig!=null)rig.Tremor(.6f,.06f);}
            // The arm draws in under the carrier once the party is off it.
            if(armK>0)DriveArm(0,Time.deltaTime/BossController.TollTune.ArmSeconds);
            // The view holds the open gate and the party while it swings and the first hulls set off.
            var party=PartyController.Current;
            if(age<BossController.TollTune.FrameSeconds&&party!=null&&party.Active!=null&&party.Active.flight)
            {
                if(rig==null)rig=FindFirstObjectByType<CameraRig>();
                var view=new Bounds(Middle,new Vector3(Half*1.4f,6,8));view.Encapsulate(party.Active.transform.position);
                if(rig!=null)rig.FrameSceneInterest(this,view,new Rect(.08f,.16f,.84f,.72f));
            }
        }
        /// <summary>The arm toward run out (1) or drawn in (0) by up to <paramref name="step"/>, only while its berth may move. False while it is held.</summary>
        public bool DriveArm(float toward,float step)
        {
            if(arm==null){armK=Mathf.MoveTowards(armK,toward,step);return true;}
            var berth=Berth;if(berth!=null&&!berth.MayMove)return false;
            armK=Mathf.MoveTowards(armK,toward,step);float k=Mathf.SmoothStep(0,1,armK);
            arm.position=Vector3.Lerp(armIn,armOut,k);Physics.SyncTransforms();return true;
        }
        /// <summary>Set where it stands now (a load, the fight waking with the arm in): nothing between.</summary>
        public void SetArm(float k){armK=Mathf.Clamp01(k);if(arm!=null){var berth=Berth;if(berth==null||berth.MayMove){arm.position=Vector3.Lerp(armIn,armOut,Mathf.SmoothStep(0,1,armK));Physics.SyncTransforms();}}}
        /// <summary>The fall: both outriggers swing out past lying open and hang, pitched down under the plane, the motor dark. <paramref name="k"/> 0 to 1 of the way.</summary>
        public void HangOpen(float k)
        {
            k=Mathf.Clamp01(k);hanging=k>=1;OpenBraces(false);Clock(0);
            foreach(var rigger in riggers){rigger.angle=Mathf.Lerp(hangFrom,BossController.TollTune.HangAngle,k);rigger.pitch=BossController.TollTune.HangPitch*k;rigger.drop=BossController.TollTune.HangDrop*k;}
            Pose();
        }
        float hangFrom=BossController.TollTune.CageLies;
        /// <summary>Where the guns' lanes run across the gate (offsets from its middle along <see cref="Across"/>): outer chain, dividers, outer chain.</summary>
        public (float from,float to) LaneAt(float offset)
        {
            float from=-outer,to=outer;
            foreach(var d in dividers){if(d<=offset)from=Mathf.Max(from,d);else to=Mathf.Min(to,d);}
            return (from,to);
        }
        /// <summary>A point's offset across the gate from its middle, and how far down the lanes from the gate line.</summary>
        public float OffsetOf(Vector3 point)=>Vector3.Dot(point-Middle,Across);
        public float DownOf(Vector3 point)=>Vector3.Dot(point-Middle,South);
    }
}
