using System;
using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// Lava that crusts over and breaks on a cycle (chapter 8, the Scoria Flows; rule: the crust tells you). Each <see cref="Plate"/> is a convex piece of a flow's floor with a clock of its
    /// own: dark crust for <see cref="Plate.dark"/> seconds (safe), then its seams light for <see cref="Plate.seam"/> (deep red going to hot yellow, the slabs trembling at the end, a groan:
    /// about to go), then its slabs sink and the melt is open for <see cref="Plate.bright"/> (it burns), then the crust rises again, dark, in <see cref="reform"/> seconds with a hiss.
    /// A hero who stands on open melt longer than <see cref="grace"/> loses <see cref="damageShare"/> of integrity (never the last of it; no source, so the place's damage) and the party is
    /// set down on the last ground the lead stood on that doesn't cycle; a partner caught alone is moved and never burnt, since it doesn't choose where it stands. An enemy left standing on
    /// open melt loses <see cref="enemyShare"/> of its integrity a second (hidden bodies and bosses are left alone). Over an occupied ambush hide on a plate the crust is heaved into a blister
    /// that breathes and puffs ash (<see cref="heaves"/>): what just moved under it.
    /// The plates draw before every danger marking (opaque, under it) and the melt is a pale yellow-white, never a marking's orange or red, so a marking laid on open melt still reads.
    /// Nothing is saved: every load starts the clocks at nought. Nothing cycles while the game is paused or a conversation holds the party. Registers <see cref="GroundHeat"/> for combat.
    /// Logs <c>CRUST_THROWN &lt;hero&gt; &lt;plate&gt;</c>, <c>CRUST_BURNS &lt;enemy&gt; &lt;plate&gt;</c>, <c>CRUST_SEAM_UNDER &lt;plate&gt;</c> (the lead's plate begins to go). Event <see cref="Thrown"/>.
    /// </summary>
    [ExecuteAlways]
    public sealed class CrustField:MonoBehaviour
    {
        public enum Phase{Dark,Seam,Bright}
        [Serializable] public sealed class Plate
        {
            public string name;
            /// <summary>Convex, on the plan, in order.</summary>
            public Vector2[] corners=Array.Empty<Vector2>();
            /// <summary>Its floor's height, and the head start of its clock.</summary>
            public float floor,offset;
            public float dark=6,seam=2.5f,bright=3.5f;
            /// <summary>The open melt under it, and its slabs (drawn as one, moved as one: they sink as it opens).</summary>
            public Renderer melt,slabs;
            /// <summary>The seeds of its slabs' cracks: a point belongs, to the eye, to the plate whose seed is nearest. Empty: the plate is drawn as its quad.</summary>
            public Vector2[] seeds=Array.Empty<Vector2>();
            [NonSerialized] public Vector3 rest;[NonSerialized] public Vector2 centre;[NonSerialized] public int was=-1;[NonSerialized] public Vector4 tiling;
            public float Period=>dark+seam+bright;
        }
        public static event Action<CrustField,CombatActor,Plate> Thrown;
        public static CrustField Current{get;private set;}
        public Plate[] plates=Array.Empty<Plate>();
        /// <summary>Ground that stands out of a flow and never cycles (x, z, radius): an island in a plate is not the plate. Without them a hero on the delta's islands was burnt.</summary>
        public Vector3[] islands=Array.Empty<Vector3>();
        /// <summary>The blisters over ambush hides, each standing where its hide is.</summary>
        public Transform[] heaves=Array.Empty<Transform>();
        public float damageShare=.18f,enemyShare=.1f,grace=.2f;
        /// <summary>How far the slabs go under as a plate opens, how long that takes, how long the crust takes to rise again (still hot until it has), and the tremble at the end of a seam.</summary>
        public float sink=.16f,sinkSeconds=.3f,reform=.5f,tremble=.014f;
        /// <summary>The seam's light as it starts and as it is about to go; the melt open; the crust's glow as it rises again. The melt is pale, never a marking's colour.</summary>
        [ColorUsage(false,true)] public Color seamStart=new(.9f,.08f,.05f),seamEnd=new(1.6f,.9f,.36f),open=new(1.35f,.75f,.3f),risen=new(.8f,.18f,.05f),ember=new(.2f,.04f,.01f);
        /// <summary>How fast the open melt moves (its texture), metres a second, along the plan.</summary>
        public Vector2 flow=new(-.18f,-.1f);
        /// <summary>The clock an editor still is taken at, so a still shows plates in every state.</summary>
        public float stillClock=7.4f;
        public string groan="root_groan",openSound="thrusterFire_000",throwSound="explosionCrunch_000";
        /// <summary>The field's clock, seconds since load, stopped while play is held. A test may set it, and hold it.</summary>
        public float Clock{get;set;}
        public bool Held{get;set;}
        static readonly int EmissionId=Shader.PropertyToID("_EmissionColor"),TilingId=Shader.PropertyToID("_BaseMap_ST");
        MaterialPropertyBlock block;
        readonly Dictionary<CombatActor,Vector3> safe=new();readonly Dictionary<CombatActor,float> onMelt=new();readonly Dictionary<Health,float> burnAt=new();
        float nextBark,nextPuff;bool started;

        // ---------------------------------------------------------------- what a plate is doing
        /// <summary>Seconds into its own cycle at a clock.</summary>
        public static float Into(Plate plate,float clock){float p=Mathf.Max(.01f,plate.Period);return Mathf.Repeat(clock+plate.offset,p);}
        public static Phase PhaseOf(Plate plate,float clock){float t=Into(plate,clock);return t<plate.dark?Phase.Dark:t<plate.dark+plate.seam?Phase.Seam:Phase.Bright;}
        /// <summary>Open, or its crust still rising: it burns.</summary>
        public bool Burns(Plate plate,float clock){float t=Into(plate,clock);return t>=plate.dark+plate.seam||t<reform;}
        public bool Burns(Plate plate)=>Burns(plate,Clock);
        public Phase PhaseOf(Plate plate)=>PhaseOf(plate,Clock);
        /// <summary>The plate under a point (within a body's height of its floor), or null.</summary>
        public Plate At(Vector3 point)
        {
            var p=new Vector2(point.x,point.z);
            foreach(var island in islands)if((p-new Vector2(island.x,island.y)).sqrMagnitude<island.z*island.z)return null;
            foreach(var plate in plates)if(plate!=null&&Mathf.Abs(point.y-plate.floor)<1.6f&&Inside(plate.corners,p))return plate;
            return null;
        }
        public static bool Inside(Vector2[] polygon,Vector2 p)
        {
            if(polygon==null||polygon.Length<3)return false;
            float sign=0;
            for(int i=0;i<polygon.Length;i++)
            {
                var a=polygon[i];var b=polygon[(i+1)%polygon.Length];float cross=(b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x);
                if(Mathf.Abs(cross)<1e-6f)continue;if(sign==0)sign=Mathf.Sign(cross);else if(Mathf.Sign(cross)!=sign)return false;
            }
            return true;
        }
        /// <summary>The plate a point belongs to as it is drawn: the one whose crack seed is nearest, among the plates round the one under it. A plate's drawn outline follows its cracks
        /// into its neighbour's quad and back, so near a shared edge this can differ from <see cref="At"/>.</summary>
        public Plate Drawn(Vector3 point)
        {
            var under=At(point);if(under==null)return null;var p=new Vector2(point.x,point.z);Plate best=under;float least=float.PositiveInfinity;
            foreach(var plate in plates)
            {
                if(plate==null||plate.seeds==null||plate.seeds.Length==0||(Centre(plate)-p).sqrMagnitude>144)continue;
                foreach(var s in plate.seeds){float d=(s-p).sqrMagnitude;if(d<least){least=d;best=plate;}}
            }
            return best;
        }
        /// <summary>Whether a point burns now: the plate under it by its footprint and the plate it belongs to as drawn must both be burning. Where the two disagree the burn is shrunk
        /// to the inside: nobody is burnt on crust that looks dark.</summary>
        public bool BurnsAt(Vector3 point){var under=At(point);if(under==null||!Burns(under))return false;var drawn=Drawn(point);return drawn==null||drawn==under||Burns(drawn);}
        /// <summary>0 ground that never cycles or dark crust, .5 a lit seam, 1 open melt (or crust still rising), as drawn and as footprint agree.</summary>
        public float Heat(Vector3 point)
        {
            var under=At(point);if(under==null)return 0;if(BurnsAt(point))return 1;var drawn=Drawn(point)??under;
            return PhaseOf(under)!=Phase.Dark&&PhaseOf(drawn)!=Phase.Dark?.5f:0;
        }
        /// <summary>Seconds before the ground at a point lights or burns: nought if it is lit or burning now, a long time if it never cycles.</summary>
        public float SafeFor(Vector3 point)
        {
            var under=At(point);if(under==null)return float.PositiveInfinity;float least=float.PositiveInfinity;
            foreach(var plate in new[]{under,Drawn(point)??under})
            {
                if(Burns(plate)||PhaseOf(plate)!=Phase.Dark)return 0;least=Mathf.Min(least,plate.dark-Into(plate,Clock));
            }
            return least;
        }
        /// <summary>The first plate whose clock puts it in this phase now, nearest a point: for a test.</summary>
        public Plate Nearest(Vector3 point){Plate best=null;float least=float.PositiveInfinity;foreach(var plate in plates){if(plate==null)continue;float d=(Centre(plate)-new Vector2(point.x,point.z)).sqrMagnitude;if(d<least){least=d;best=plate;}}return best;}
        public static Vector2 Centre(Plate plate){var c=Vector2.zero;foreach(var p in plate.corners)c+=p;return plate.corners.Length>0?c/plate.corners.Length:c;}
        /// <summary>A clock at which this plate is that far into its cycle.</summary>
        public static float ClockFor(Plate plate,float into)=>Mathf.Repeat(into-plate.offset,Mathf.Max(.01f,plate.Period));

        // ---------------------------------------------------------------- life
        void OnEnable()
        {
            block??=new MaterialPropertyBlock();
            if(!Application.isPlaying){Pose(stillClock,true);return;}
            Current=this;GroundHeat.Register(this,Heat);
        }
        void OnDisable(){if(Current==this)Current=null;GroundHeat.Unregister(this);}
        void Start()
        {
            if(!Application.isPlaying)return;
            foreach(var plate in plates)
            {
                if(plate==null)continue;plate.centre=Centre(plate);plate.was=-1;
                if(plate.slabs!=null)plate.rest=plate.slabs.transform.position;
                if(plate.melt!=null&&plate.melt.sharedMaterial!=null){var m=plate.melt.sharedMaterial;plate.tiling=m.HasProperty(TilingId)?m.GetVector(TilingId):new Vector4(1,1,0,0);}
            }
            started=true;Pose(Clock,false);
        }
        void Update()
        {
            if(!Application.isPlaying||!started)return;
            if(!Held&&!GameTime.CombatHeld)Clock+=Time.deltaTime;
            Pose(Clock,false);
            if(GameTime.CombatHeld)return;
            Heroes();Enemies();Heaves();
        }

        // ---------------------------------------------------------------- how it looks
        /// <summary>Lays every plate as it is at a clock: slabs up, trembling or sunk, the seams' and the melt's light. In an editor (a still) the slabs are hidden instead of moved, so
        /// nothing a still does is saved with the scene.</summary>
        public void Pose(float clock,bool still)
        {
            block??=new MaterialPropertyBlock();
            foreach(var plate in plates)
            {
                if(plate==null)continue;
                float t=Into(plate,clock),opens=plate.dark+plate.seam;
                Color glow=Color.black,melt=ember;float drop=0,shake=0;
                if(t<plate.dark)
                {
                    // Risen again: hot until it is up, its glow going out over a second and a half.
                    float up=Mathf.Clamp01(t/reform);drop=sink*(1-Mathf.SmoothStep(0,1,up));
                    glow=Color.Lerp(risen,ember*1.4f,Mathf.Clamp01(t/1.5f));melt=Color.Lerp(open,ember,Mathf.Clamp01(t/.6f));
                }
                else if(t<opens)
                {
                    float k=Mathf.Clamp01((t-plate.dark)/Mathf.Max(.01f,plate.seam));
                    // From the dark crust's own faint ember through cherry red to white heat, brightening all the way: a blackbody's way, which never dwells on a marking's saturated orange.
                    glow=Color.Lerp(seamStart,seamEnd,Mathf.Pow(k,1.5f))*(.25f+.75f*k);if(k<.15f)glow=Color.Lerp(ember*1.4f,glow,k/.15f);
                    // The last third flickers, so a lit seam that is about to go doesn't read as one that has just lit.
                    if(k>.66f)glow*=.8f+.2f*Mathf.Sin(t*38);
                    melt=Color.Lerp(ember,seamStart,k);shake=k>.6f?tremble*Mathf.Sin(t*57)*(k-.6f)/.4f:0;
                }
                else{float k=Mathf.Clamp01((t-opens)/Mathf.Max(.01f,sinkSeconds));drop=sink*Mathf.SmoothStep(0,1,k);glow=seamEnd*(1-k);melt=open;}
                if(plate.slabs!=null)
                {
                    if(still)plate.slabs.forceRenderingOff=drop>sink*.5f;
                    else plate.slabs.transform.position=plate.rest+Vector3.up*(shake-drop);
                    plate.slabs.GetPropertyBlock(block);block.SetColor(EmissionId,glow);plate.slabs.SetPropertyBlock(block);
                }
                if(plate.melt!=null)
                {
                    plate.melt.GetPropertyBlock(block);block.SetColor(EmissionId,melt);
                    if(!still&&plate.tiling.x!=0){var tile=plate.tiling;tile.z=Mathf.Repeat(tile.z+flow.x*clock*tile.x,1);tile.w=Mathf.Repeat(tile.w+flow.y*clock*tile.y,1);block.SetVector(TilingId,tile);}
                    plate.melt.SetPropertyBlock(block);
                }
                if(still)continue;
                int phase=(int)PhaseOf(plate,clock);
                if(phase!=plate.was)
                {
                    if(plate.was>=0)Sounded(plate,phase);
                    plate.was=phase;
                }
            }
            if(still)foreach(var heave in heaves)if(heave!=null)foreach(var r in heave.GetComponentsInChildren<Renderer>())r.forceRenderingOff=false;
        }
        void Sounded(Plate plate,int phase)
        {
            var party=PartyController.Current;if(party==null||party.Active==null)return;
            var at=new Vector3(plate.centre.x,plate.floor,plate.centre.y);float far=(at-party.Active.transform.position).magnitude;if(far>26)return;
            if(phase==(int)Phase.Seam)AudioManager.PlayAt(groan,null,at,.16f,AudioBus.SFX,140,.4f,1.35f);
            else if(phase==(int)Phase.Bright){AudioManager.PlayAt(openSound,null,at,.12f,AudioBus.SFX,140,.4f,.6f);CombatVfx.Burst(at+Vector3.up*.3f,new Color(1,.7f,.35f,.8f),"spark");}
            else CombatVfx.Burst(at+Vector3.up*.2f,new Color(.7f,.68f,.66f,.5f),"steam");
        }

        // ---------------------------------------------------------------- the rule
        void Heroes()
        {
            var party=PartyController.Current;if(party==null||party.Active==null)return;var lead=party.Active;
            foreach(var hero in party.members)
            {
                if(hero==null||hero.flight||!hero.Health.Alive)continue;
                var at=hero.transform.position;var plate=At(at);
                if(plate==null){safe[hero]=at;onMelt[hero]=0;continue;}
                if(hero==lead&&plate.was==(int)Phase.Seam&&Into(plate,Clock)-plate.dark<.25f&&Time.time>=nextBark)
                {
                    nextBark=Time.time+12;BarkService.Play(lead.character,"crust");Debug.Log("CRUST_SEAM_UNDER "+plate.name);
                }
                if(!BurnsAt(at)){onMelt[hero]=0;continue;}
                onMelt.TryGetValue(hero,out float held);held+=Time.deltaTime;onMelt[hero]=held;
                if(held>=grace){onMelt[hero]=0;Throw(party,hero,hero==lead,plate);}
            }
        }
        void Throw(PartyController party,CombatActor hero,bool lead,Plate plate)
        {
            var at=hero.transform.position;
            if(lead)
            {
                // Never the last of it: the flow sets the party back, it doesn't end the run.
                float amount=Mathf.Min(hero.Health.maximum*damageShare,hero.Health.integrity-1);
                if(amount>0)hero.Health.Receive(new DamagePacket{amount=amount,type=DamageType.Plasma});
                BarkService.Play(hero.character,"flow",true);
            }
            var to=safe.TryGetValue(lead?hero:party.Active,out var place)?place:FarSide(at);
            CombatVfx.Burst(at+Vector3.up,new Color(1,.8f,.5f,.8f),"crack");CombatVfx.Burst(at+Vector3.up*.6f,new Color(.75f,.72f,.7f,.6f),"steam");
            AudioManager.PlayAt(throwSound,null,at,.32f,AudioBus.SFX,60);
            if(lead)
            {
                int side=0;foreach(var member in party.members)if(member!=null&&member.Health.Alive)Put(member,member==hero?to:Beside(to,++side));
            }
            // Beside the lead when the lead stands on ground that holds: set back to its own last firm ground, a partner following over a delta was thrown further behind at every
            // plate and ended a walk 39 m back (QaPlanWorldTests.TheCompanionKeepsUpOnFoot).
            else if(party.Active!=null&&party.Active!=hero&&(At(party.Active.transform.position) is not Plate under||!Burns(under)))Put(hero,Beside(party.Active.transform.position,1));
            else Put(hero,safe.TryGetValue(party.Active,out var leads)?Beside(leads,1):safe.TryGetValue(hero,out var own)?own:Beside(to,1));
            Debug.Log("CRUST_THROWN "+hero.character+" "+plate.name);
            Thrown?.Invoke(this,hero,plate);
        }
        static void Put(CombatActor member,Vector3 to)
        {
            var body=member.GetComponent<CharacterController>();bool was=body!=null&&body.enabled;
            if(body!=null)body.enabled=false;member.transform.position=to;if(body!=null)body.enabled=was;
            if(member.TryGetComponent<GroundMotor>(out var motor))motor.Halt();
            member.Health.InvulnerableUntil=Mathf.Max(member.Health.InvulnerableUntil,GameTime.Now+1);
        }
        /// <summary>A place for another body beside one set down: off every plate, on ground, the first of right, left, behind and ahead of it that is.</summary>
        Vector3 Beside(Vector3 at,int n)
        {
            // Off every plate if there is room; else on crust that isn't burning (the lead out on a dark plate); never on top of the lead.
            for(int pass=0;pass<2;pass++)foreach(var way in new[]{Vector3.right,Vector3.left,Vector3.back,Vector3.forward})
            {
                var p=at+way*1.6f*n;var plate=At(p);if(plate!=null&&(pass==0||Burns(plate)))continue;
                if(Physics.Raycast(p+Vector3.up*2,Vector3.down,out var hit,4,~0,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<CombatActor>()==null)return hit.point+Vector3.up*.05f;
            }
            return at+Vector3.right*1.6f*n;
        }
        /// <summary>Nowhere remembered (a load onto a plate): the nearest ground off every plate, round about.</summary>
        Vector3 FarSide(Vector3 at)
        {
            for(float r=1;r<14;r+=1)for(int a=0;a<16;a++)
            {
                var p=at+Quaternion.Euler(0,a*22.5f,0)*Vector3.forward*r;
                if(At(p)==null&&Physics.Raycast(p+Vector3.up*4,Vector3.down,out var hit,8,~0,QueryTriggerInteraction.Ignore))return hit.point+Vector3.up*.05f;
            }
            return at;
        }
        void Enemies()
        {
            foreach(var health in Health.All)
            {
                if(health==null||health.friendly||!health.Targetable||!health.TryGetComponent<EnemyBrain>(out _)||health.GetComponent<BossController>()!=null)continue;
                var plate=At(health.transform.position);if(plate==null||!BurnsAt(health.transform.position)){burnAt.Remove(health);continue;}
                if(!burnAt.TryGetValue(health,out float next)){burnAt[health]=Time.time+.5f;continue;}
                if(Time.time<next)continue;burnAt[health]=Time.time+1;
                health.Receive(new DamagePacket{amount=health.maximum*enemyShare,type=DamageType.Plasma,weight=HitWeight.Tick});
                CombatVfx.Burst(health.transform.position+Vector3.up*.5f,new Color(1,.8f,.5f,.7f),"spark");
                Debug.Log("CRUST_BURNS "+health.id+" "+plate.name);
            }
        }
        /// <summary>A blister over each hide something has gone under; flat, and not drawn, over an empty one.</summary>
        void Heaves()
        {
            bool puff=Time.time>=nextPuff;if(puff)nextPuff=Time.time+1.7f;
            foreach(var heave in heaves)
            {
                if(heave==null)continue;
                AmbushSpot under=null;foreach(var spot in AmbushSpot.All){if(spot==null)continue;var d=spot.transform.position-heave.position;d.y=0;if(d.sqrMagnitude<1.2f){under=spot;break;}}
                bool live=under!=null&&under.Occupant!=null&&under.Occupant.Health.Alive&&under.Shown;
                float breathe=.55f+.45f*Mathf.Sin(Time.time*2.3f+heave.position.x);
                foreach(var r in heave.GetComponentsInChildren<Renderer>())
                {
                    r.forceRenderingOff=!live;if(!live)continue;
                    r.GetPropertyBlock(block);block.SetColor(EmissionId,risen*breathe);r.SetPropertyBlock(block);
                }
                if(live&&puff)CombatVfx.Burst(heave.position+Vector3.up*.6f,new Color(.62f,.6f,.58f,.55f),"steam");
            }
        }
        /// <summary>Whether a blister is up over a hide: for a test.</summary>
        public static bool Heaved(Transform heave){foreach(var r in heave.GetComponentsInChildren<Renderer>())if(!r.forceRenderingOff)return true;return false;}
    }
}
