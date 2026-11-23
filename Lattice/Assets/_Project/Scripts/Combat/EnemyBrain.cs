using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
using UnityEngine.AI;
namespace Lattice.Combat
{
    /// <summary>A dying Scrapmite's burst: marked on the ground, with a short fuse, and striking only inside its marking. It
    /// used to go off on the next frame with no tell, so a partner's kill blew up on the player.</summary>
    public sealed class DeathBlast:MonoBehaviour
    {
        public const float Fuse=.45f,Radius=2.5f;
        Telegraph mark;DamagePacket packet;Vector3 at;
        /// <summary>The fight it belonged to is over: it doesn't go off on the heroes who just won.</summary>
        public void Defuse(){if(mark!=null)mark.Clear();Destroy(gameObject);}
        public static void Arm(Vector3 at,DamagePacket packet)
        {
            var blast=new GameObject("Death blast").AddComponent<DeathBlast>();blast.at=at;blast.packet=packet;
            blast.mark=Telegraph.Circle(at,Radius,Fuse,Telegraph.Threat,true);blast.mark.owner=blast;
        }
        void Update()
        {
            if(GameTime.CombatHeld||mark==null)return;if(!mark.Charged)return;
            mark.Release();CombatVfx.Burst(at+Vector3.up*.6f,new Color(1.3f,.55f,.2f),"kill");
            var party=PartyController.Current;
            if(party!=null)foreach(var hero in party.members)if(hero.Health.Alive&&mark.Contains(hero.transform.position)&&CombatCover.Clear(at+Vector3.up*.6f,hero.transform.position+Vector3.up*.9f))hero.Health.Receive(packet);
            Destroy(gameObject);
        }
    }
    public sealed class EnemyBrain:MonoBehaviour
    {
        public EnemyDef definition;
        public Health Health{get;private set;}
        public bool Telegraphing{get;private set;}
        /// <summary>The ground marking of the attack being told, if any.</summary>
        public Telegraph AttackMark{get;private set;}
        public bool Attacking=>lunging||acting||Time.time<attackAnimationUntil;
        public float TelegraphRemaining=>Mathf.Max(0,strikeAt-Time.time);
        public float TellDuration{get;private set;}=1;
        /// <summary>How far through its tell the attack is, 0 when there is none: what a body's wind-up follows.</summary>
        public float TellProgress=>Telegraphing?Mathf.Clamp01(1-TelegraphRemaining/Mathf.Max(.01f,TellDuration)):0;
        /// <summary>The first moment of the strike, for a body's follow-through.</summary>
        public bool Striking=>Time.time<attackAnimationUntil-.38f;
        /// <summary>How much of its Attack clip a rigged body has played when the strike lands. Measured: Fox-Attack rears up to 17% and pounces
        /// by 30%, Sword_Attack winds up to 20% and swings by 33%. Any later and the body has already struck before the hit lands.</summary>
        public const float TellShare=.2f;
        public bool Passive;
        public int ImpactSequence{get;private set;}
        public float ImpactStarted{get;private set;}=float.NegativeInfinity;
        public bool ArmoredImpact{get;private set;}
        public const float ImpactDuration=.36f;
        CharacterController controller;
        Vector3 home;
        Vector3 aim;
        float nextAttack,strikeAt,attackAnimationUntil;
        /// <summary>Where the body stood when it marked this attack and how far a drag has carried it since, and when it was last dragged.</summary>
        Vector3 markedFrom;float dragged,draggedAt=float.NegativeInfinity;
        bool lunging;
        // ---------- behaviours (EnemyBehaviour.cs): a body past the six archetypes fights through the same tell everything else reads ----------
        EnemyBehaviour behaviour;bool acting;
        /// <summary>How this body fights when its definition names a behaviour (EnemyDef.behaviour); null for one that fights by archetype alone.</summary>
        public EnemyBehaviour Behaviour=>behaviour;
        internal CharacterController Controller=>controller;
        internal Vector3 Home=>home;
        /// <summary>In the middle of a move of its own (a charge, a pass): it counts as attacking, for its clip, its sounds and a shove.</summary>
        internal bool Acting{get=>acting;set=>acting=value;}
        /// <summary>Starts a tell as every ordinary attack does: its marking, the clock the partner, the driver and the body's wind-up read, and its sound, in that order.</summary>
        internal void BeginTell(Telegraph mark,float tell,Vector3 toward,CombatActor victim,string sound,float pitch,bool shooter=false)
        {
            Telegraphing=true;strikeAt=Time.time+tell;TellDuration=tell;if(shooter)shotAt=Time.time;
            markedFrom=transform.position;dragged=0;toward.y=0;aim=toward.sqrMagnitude>.001f?toward.normalized:transform.forward;
            if(mark!=null)mark.owner=this;AttackMark=mark;
            if(victim!=null&&boss==null){var gap=victim.transform.position-transform.position;gap.y=0;FlashLessons.MarkedAttack(victim,gap.magnitude);}
            AudioManager.PlayAt(sound,transform,transform.position+Vector3.up,.09f,AudioBus.SFX,128,.1f,pitch);
        }
        internal bool TellRunOut=>Telegraphing&&Time.time>=strikeAt;
        internal Vector3 Aim=>aim;
        /// <summary>The tell is over and the strike is the caller's: the marking comes back still live, to be released as it lands or carried by a shot.</summary>
        internal Telegraph EndTell(float followThrough=.55f){Telegraphing=false;var told=AttackMark;AttackMark=null;attackAnimationUntil=Time.time+followThrough;return told;}
        internal void Rest(float seconds)=>nextAttack=Time.time+seconds;
        internal bool Rested=>Time.time>=nextAttack&&Time.time>=draggedAt+DragSettles;
        internal void Walk(Vector3 direction,float speed)=>Move(direction,speed);
        internal bool Sees(CombatActor victim)=>InSight(victim);
        internal void Spread(Vector3 toVictim,float distance,float reach)=>KeepApart(toVictim,distance,reach);
        internal DamagePacket Blow(AttackDef attack,HitWeight weight)=>new DamagePacket{source=Health,amount=(attack!=null?attack.damage:18)*DamageScale,type=attack!=null?attack.type:DamageType.Kinetic,breakPower=10,weight=weight};
        void LateUpdate(){if(behaviour!=null)behaviour.Present();}
        public float DamageScale=1;
        float wardedUntil;Vector3 wardFrom;
        /// <summary>Touched by a lit relay's light. It keeps clear for a moment after, so a pack paces the ward's edge instead of
        /// shuddering on it.</summary>
        public void Ward(Vector3 from){wardedUntil=Time.time+1;wardFrom=from;}
        public bool Warded=>Time.time<wardedUntil;
        BossController boss;
        CantorCollar collar;
        /// <summary>Awake bodies on the ground, so they can keep out of each other without asking the scene.</summary>
        static readonly List<EnemyBrain> crowd=new();
        const float KeepApartSpeed=2.4f,HeroBody=.45f,HeroCapsule=.35f,MostBody=2,MostFront=3;
        /// <summary>How long after its last tell a shooter stays framed, past the cooldown between shots; and how far from the hero one is worth drawing back for.</summary>
        const float ShooterFramedFor=3,ShooterReach=24;
        float shotAt=float.NegativeInfinity;
        /// <summary>The ground shooters firing at a point, as one box, with nothing allocated: the few bodies awake are all it walks.</summary>
        public static bool ShootingAt(Vector3 at,ref Bounds bounds)
        {
            bool any=false;
            foreach(var enemy in crowd)
            {
                if(enemy.Passive||enemy.boss!=null||Time.time-enemy.shotAt>ShooterFramedFor||!enemy.Health.Alive)continue;
                var offset=enemy.transform.position-at;offset.y=0;if(offset.sqrMagnitude>ShooterReach*ShooterReach)continue;
                var body=new Bounds(enemy.transform.position+Vector3.up*1.2f,new Vector3(1.4f,2.4f,1.4f));
                if(any)bounds.Encapsulate(body);else{bounds=body;any=true;}
            }
            return any;
        }
        /// <summary>Is anything awake within reach of a fight here: the few bodies awake are all it walks, with nothing allocated.</summary>
        public static bool FightNear(Vector3 at)
        {
            foreach(var enemy in crowd)
            {
                if(enemy.Passive||!enemy.Health.Alive)continue;
                var offset=enemy.transform.position-at;offset.y=0;if(offset.sqrMagnitude<InteractionCalm*InteractionCalm)return true;
            }
            return false;
        }
        float bodyRadius=.5f;bool crowded;
        /// <summary>How far its drawn body reaches round its middle on the plan (never less than its capsule): what a hero keeps off.</summary>
        public float BodyRadius=>bodyRadius;
        /// <summary>How far its drawn body reaches ahead of its middle, the way it faces (never less than its capsule): where its head is. A hound's or a grazer's capsule is round its
        /// middle and its head a body's length ahead of that, so run in until the capsules met, its head and shoulders stood in the hero (Lantern Gorge, nov20-whole s14/017;
        /// BodiesKeepApartTests). It walks in, and lunges, until that front meets his capsule, and no further.</summary>
        public float FrontReach=>front;
        float front=.5f;
        static float FrontOf(Transform root,Renderer drawn)
        {
            Bounds box;Matrix4x4 into;var toRoot=root.worldToLocalMatrix;
            if(drawn is SkinnedMeshRenderer skin){box=skin.localBounds;into=toRoot*(skin.rootBone!=null?skin.rootBone:skin.transform).localToWorldMatrix;}
            else if(drawn is MeshRenderer&&drawn.TryGetComponent<MeshFilter>(out var filter)&&filter.sharedMesh!=null){box=filter.sharedMesh.bounds;into=toRoot*drawn.transform.localToWorldMatrix;}
            else return 0;
            if(drawn.name=="Telegraph"||drawn.shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)return 0;
            float most=0;for(int i=0;i<8;i++)most=Mathf.Max(most,into.MultiplyPoint3x4(box.center+Vector3.Scale(box.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))).z);
            return most;
        }
        /// <summary>How far from a hero's middle its own middle stops: its front at his capsule.</summary>
        float Contact(CombatActor hero)=>front+(hero.TryGetComponent<CharacterController>(out var his)?his.radius:HeroCapsule)+.05f;
        void OnEnable(){if(!crowd.Contains(this))crowd.Add(this);}
        void OnDisable(){crowd.Remove(this);}
        void Awake(){Health=GetComponent<Health>();controller=GetComponent<CharacterController>();home=transform.position;}
        void Start()
        {
            // Its drawn body, not its capsule: a Scrapmite's legs reach .75 m on a .55 m capsule, so five of them standing shoulder to shoulder were one heap.
            bodyRadius=controller!=null?controller.radius:.5f;
            foreach(var body in GetComponentsInChildren<Renderer>())
                if(body is MeshRenderer||body is SkinnedMeshRenderer)bodyRadius=Mathf.Max(bodyRadius,Mathf.Min(MostBody,Mathf.Min(body.bounds.extents.x,body.bounds.extents.z)));
            front=controller!=null?controller.radius:.5f;
            foreach(var body in GetComponentsInChildren<Renderer>())front=Mathf.Max(front,Mathf.Min(MostFront,FrontOf(transform,body)));
            Health.Damaged+=OnDamaged;
            boss=GetComponent<BossController>();
            collar=GetComponent<CantorCollar>();
            Health.Died+=OnDeath;Health.Revived+=OnRevived;
            nextAttack=Time.time+.6f;
            behaviour=EnemyBehaviour.For(this);
        }
        void OnDamaged(Health _,DamagePacket packet,float amount)
        {
            CombatActor.PayHit(packet,amount);
            if(behaviour!=null&&Health.Alive)behaviour.Struck(packet,amount);
            if(Health.Alive&&!Health.Broken&&amount>0)
            {
                ImpactStarted=Time.time;ImpactSequence++;
                ArmoredImpact=packet.type==definition.resistance||Time.time<Health.ShieldUntil;
            }
            if(Health.Broken)
            {
                CancelAttack();Health.ShieldUntil=0;
                // Recovery can begin a new tell; it must never release the
                // expired strike that was interrupted three seconds earlier.
                nextAttack=Mathf.Max(nextAttack,Health.BrokenUntil);
            }
            // The Husk's own. Keyed on its archetype, a Sergeant or Hessa took it too: a Sergeant resists Taren's every swing, so each one landed at a quarter and halved Sela's needles.
            else if(Health.Alive&&definition.id=="SentinelHusk"&&packet.type==definition.resistance)
            {
                // The attached ward carries the protective state. Re-emitting
                // forty large rings for every pellet hid the hit silhouette.
                Health.ShieldUntil=Time.time+2;
            }
        }
        void CancelAttack()
        {
            StopAllCoroutines();lunging=false;Telegraphing=false;strikeAt=attackAnimationUntil=0;
            if(AttackMark!=null){AttackMark.Cancel();AttackMark=null;}
            if(behaviour!=null){acting=false;behaviour.Cancel();}
        }
        /// <summary>Something took this enemy out of its attack (a hook's reel): it starts over, no expired strike waiting.</summary>
        public void Interrupt(){CancelAttack();nextAttack=Mathf.Max(nextAttack,Time.time+.6f);}
        public bool Mine=>definition!=null&&definition.archetype==EnemyArchetype.Mine;
        /// <summary>Too big to be reeled in: a hook pulls the hero to it instead.</summary>
        public bool Heavy=>definition!=null&&definition.archetype==EnemyArchetype.Sentinel;
        public const string SelfDetonation="selfDetonation";
        const float MineBlast=3,LungeTravel=4.5f,LungeSpeed=16,LungeAhead=.5f,LungeRadius=1.15f,LungeLength=LungeTravel+LungeAhead+LungeRadius,LungeWidth=LungeRadius*2;
        /// <summary>Where a shot's marking ends and the shot with it: what is painted is what can be hit.</summary>
        static float ShotReach(float range)=>Mathf.Max(13,range);
        /// <summary>Every ordinary attack marks the ground it will strike, in its true shape: a lunge its lane, a mine its blast, a
        /// bite its reach, a shot its line. A 3 m disc at the feet understated all of them.</summary>
        Telegraph MarkAttack(float tell,bool dive,bool ranged,float range)
        {
            var at=transform.position;Telegraph mark;
            if(Mine)mark=Telegraph.Circle(at,MineBlast,tell,Telegraph.Heavy,true);
            else if(dive)mark=Telegraph.Line(at,aim,LungeLength,LungeWidth,tell,Telegraph.Threat,true);
            else if(definition.id=="ChoristerDrifter"||collar!=null&&collar.VolleyAttached)mark=Telegraph.Cone(at,aim,ShotReach(range),32,tell,Telegraph.Threat,true);
            else if(ranged)mark=Telegraph.Line(at,aim,ShotReach(range),1.1f,tell,Telegraph.Threat,true);
            else mark=Telegraph.Circle(at+aim*1.1f,Mathf.Max(1,range*.7f),tell,Telegraph.Threat,true);
            mark.owner=this;return mark;
        }
        /// <summary>How close an awake enemy must be before quiet interactions (repair, bench, mining) wait for the fight.</summary>
        public const float InteractionCalm=15;
        /// <summary>The same for a way out of the zone (dock, warp): only what is on the party holds it, so they can still run for it.</summary>
        public const float ExitCalm=6;
        public static bool AwakeNear(Vector3 point,float radius)
        {
            foreach(var health in Health.All)
            {
                if(health==null||health.friendly||!health.Alive)continue;
                var offset=health.transform.position-point;offset.y=0;if(offset.sqrMagnitude>radius*radius)continue;
                if(health.TryGetComponent<EnemyBrain>(out var brain)&&brain.enabled&&!brain.Passive)return true;
            }
            return false;
        }
        // ---------- forms apart (chapter 10's berth, ch10-wiring 6.1) ----------
        bool formRead,flier;
        /// <summary>This body fights in flight: read once, from where it first stood (a flier woke at the plane; a Clamp Tender on a shelf in a flight zone did not). In a zone
        /// with a berth the zone's form changes under it while the party is docked; its own doesn't, and a flier read as on foot fell off the plane.</summary>
        public bool Flier
        {
            get
            {
                if(!formRead){var zone=ZoneController.Current;if(zone==null)return false;formRead=true;flier=zone.FlightAt(transform.position);}
                return flier;
            }
        }
        /// <summary>Flying, for its own settling and spacing: its own form in a zone with a berth, the zone's anywhere else (exactly as it always was).</summary>
        bool Flying=>ZoneController.HasBerth?Flier:ZoneController.Current!=null&&ZoneController.Current.Flight;
        /// <summary>A hero and a body of different forms (a hero on a shelf and a flier over it): neither is the other's fight. Only where a zone has a berth; everywhere else never.</summary>
        public static bool OtherForm(CombatActor hero,Health body)
        {
            if(!ZoneController.HasBerth||hero==null||body==null)return false;
            var owner=body.reactionOwner!=null?body.reactionOwner:body;
            return owner.TryGetComponent<EnemyBrain>(out var brain)&&brain.Flier!=hero.flight;
        }
        /// <summary>The same for a hit about to land on a hero (Health.Receive): what a body of the other form dealt passes him by.</summary>
        public static bool OtherForm(Health hero,Health source)=>ZoneController.HasBerth&&hero!=null&&source!=null&&hero.TryGetComponent<CombatActor>(out var actor)&&OtherForm(actor,source);
        /// <summary>Whether a skill may drag this body (Tether Hook, Gravity Fold): bosses, serpents and anchored mines
        /// hold their ground (D201).</summary>
        public bool CanBeMoved=>Health.Alive&&controller!=null&&controller.enabled&&GetComponent<BossController>()==null&&
            definition!=null&&!definition.boss&&definition.archetype!=EnemyArchetype.Serpent&&definition.archetype!=EnemyArchetype.Mine&&(behaviour==null||behaviour.Movable);
        /// <summary>How far a drag can carry a body through its attack: under this a shot or a lane is still within a hero's width of its paint. How long after a drag it can start a tell:
        /// one started on the way would be lost an eighth of a second later, a flicker of paint and a lesson for nothing. And how fast a pull has to carry it for that: at the well's centre it only
        /// takes back what the body just walked or its own kind just shoved it, which is carrying it nowhere, and from there it can attack as it always could.</summary>
        const float MostDragged=.5f,DragSettles=.1f,DragPace=GravityWell.PullSpeed*.75f;
        /// <summary>Moves the body through its controller, so walls and floors still hold it. What it marked is where it strikes from, so dragged off that it loses the attack, as a hook's reel does:
        /// a Fold carried a bite up to 2.6 m off its painted circle, onto a hero who had stepped clear of it. Held by a wall it hasn't moved, and keeps it.</summary>
        public void Displace(Vector3 delta)
        {
            if(!CanBeMoved)return;delta.y=0;var from=transform.position;controller.Move(delta);
            var moved=transform.position-from;moved.y=0;if(moved.sqrMagnitude<.000001f)return;
            float travel=moved.magnitude;if(travel>=DragPace*Time.deltaTime)draggedAt=Time.time;
            if(!Telegraphing&&!lunging)return;
            dragged+=travel;if(dragged>MostDragged)Interrupt();
        }
        /// <summary>Carried by the world this far (chapter 6's currents: World's AuroraCurrent carries a body in one while it fights the party), through its controller, so a wall still
        /// holds it. What it marked and what its behaviour is running down go with it; its home stays where it was set, so left behind it goes back there. Nothing else calls it.</summary>
        public void Carry(Vector3 delta)
        {
            if(controller==null||!controller.enabled||!Health.Alive)return;delta.y=0;
            var from=transform.position;controller.Move(delta);var moved=transform.position-from;moved.y=0;
            markedFrom+=moved;behaviour?.Carried(moved);
        }
        /// <summary>A blow can carry this body back: a pack hunter or one of a swarm, awake, and neither telling an attack nor making one. What it marked is where it strikes
        /// from, and a shove is not a hook: it may never take an attack away, or a three-hit combo would cancel everything a hound began. A body asleep or held for a scene keeps its place.</summary>
        public bool Shovable=>enabled&&!Passive&&definition!=null&&(definition.archetype==EnemyArchetype.PackHunter||definition.archetype==EnemyArchetype.Swarm)&&
            !Telegraphing&&!Attacking&&CanBeMoved;
        /// <summary>One step of a blow's shove (Impact), through the controller as a drag goes. False once it may not be shoved or a wall holds it.</summary>
        public bool Shoved(Vector3 delta)
        {
            if(!Shovable)return false;var from=transform.position;Displace(delta);
            var moved=transform.position-from;moved.y=0;return moved.sqrMagnitude>.000001f;
        }
        public void RestoreAfterRecovery()
        {
            OpenItsParts();CancelAttack();ImpactStarted=float.NegativeInfinity;ImpactSequence=0;
            nextAttack=Time.time+.6f;enabled=true;
        }
        void OnDestroy()
        {
            if(behaviour!=null)behaviour.Dispose();
            if(Health==null)return;
            Health.Damaged-=OnDamaged;Health.Died-=OnDeath;Health.Revived-=OnRevived;
        }
        void OnDeath(Health _,DamagePacket packet)
        {
            // A boss gets its own line, never "One less."; nor does someone who stands aside (EnemyBehaviour.Yields), who isn't killed.
            bool standsAside=behaviour!=null&&behaviour.Yields;
            if(packet.source!=null&&packet.source.TryGetComponent<CombatActor>(out var actor))actor.AwardKill(!definition.boss&&!standsAside);
            if(packet.tag=="lunge")Debug.Log("LUNGE_KILL");
            Debug.Log("COMBAT_KILL "+definition.id);
            // How a boss ends is its own file's to say (BossController.Kit.ending); anything else is destroyed.
            var ending=BossController.EndingOf(definition.id);
            bool disabledRig=ending==BossController.Ending.ShutDown,releasedAnimal=ending==BossController.Ending.Released,yielded=ending==BossController.Ending.Yields,leaves=ending==BossController.Ending.Leaves;
            CombatVfx.Burst(transform.position+Vector3.up,Color.cyan,disabledRig||releasedAnimal||yielded||leaves||standsAside?"failure":"kill");
            // Resolution can disable equipment or release an animal. Neither
            // outcome borrows the generic boss explosion.
            if(releasedAnimal)AudioManager.PlayAt("doorOpen_000",transform,transform.position+Vector3.up,.24f,AudioBus.SFX,70);
            // A captain who yields drops her lance; she is arrested, not killed (D202).
            else if(yielded)AudioManager.PlayAt("impactMetal_000",transform,transform.position+Vector3.up,.3f,AudioBus.SFX,40,.06f,.7f);
            // Driven off, it has had enough: a groan as it turns away, and no explosion.
            else if(leaves)AudioManager.PlayAt("root_groan",transform,transform.position+Vector3.up,.34f,AudioBus.SFX,40,.06f,.7f);
            else if(disabledRig)CombatAudio.Death(transform,true);
            else if(definition.boss)AudioManager.PlayAt("explosionCrunch_000",transform,transform.position+Vector3.up,.3f,AudioBus.SFX,40);
            // Standing aside, it lowers its baton: metal set down, not a body falling.
            else if(standsAside)AudioManager.PlayAt("impactMetal_000",transform,transform.position+Vector3.up,.2f,AudioBus.SFX,40,.06f,.85f);
            else CombatAudio.Death(transform,definition.heavyDeath||definition.id=="SentinelHusk"||definition.archetype==EnemyArchetype.Mine||definition.id=="MusterSkiff");
            if(definition.boss)Telegraph.ClearAll(this);
            CancelAttack();
            if(behaviour!=null)behaviour.Fell(packet);
            if(definition.id=="Scrapmite")DeathBlast.Arm(transform.position,new DamagePacket{source=Health,amount=12,type=DamageType.Pulse,deathAttack=true,weight=HitWeight.Medium});
            if(definition.boss)ShutItsParts();
            controller.enabled=false;enabled=false;
        }
        /// <summary>A beaten boss takes nothing more (D355): its parts (BossParts' targets, a collar's links), which share its break but not its death, are shut with it, their knots dark,
        /// so no blow lands on what is left of it and no number rises from it.</summary>
        void ShutItsParts()
        {
            foreach(var set in GetComponentsInChildren<BossParts>(true)){if(set.Exposed)shutSets.Add(set);set.Expose(false);}
            for(int i=0;i<Health.All.Count;i++)
            {
                var part=Health.All[i];if(part==null||part==Health||part.reactionOwner!=Health)continue;
                if(part.Damageable){part.Damageable=false;shutParts.Add(part);}
                foreach(var shape in part.GetComponents<Collider>())if(shape.enabled){shape.enabled=false;shutShapes.Add(shape);}
            }
        }
        /// <summary>A beaten boss brought back (the Cantor's delayed recovery) has its parts back as they stood: what this shut, and only that, opens again (CantorCollarTests).</summary>
        void OpenItsParts()
        {
            foreach(var part in shutParts)if(part!=null)part.Damageable=true;
            foreach(var shape in shutShapes)if(shape!=null)shape.enabled=true;
            foreach(var set in shutSets)if(set!=null)set.Expose(true);
            shutParts.Clear();shutShapes.Clear();shutSets.Clear();
        }
        readonly List<Health> shutParts=new();readonly List<Collider> shutShapes=new();readonly List<BossParts> shutSets=new();
        void OnRevived(Health _)=>OpenItsParts();
        void Update()
        {
            // Charges can step onto geometry or another controller. Settling
            // cannot depend on the AI choosing to chase again afterward.
            if(!GameTime.Paused&&Health.Alive&&controller.enabled&&ZoneController.Current!=null&&!Flying&&!GameServices.Current.Input.Blocked)
            {controller.Move(Vector3.down*6*Time.deltaTime);KeepToTheFloor();}
            // A conversation or load stops the clock on what it interrupted: no strike the instant it ends.
            if(GameServices.Current.Input.Blocked&&!GameTime.Paused){strikeAt+=Time.deltaTime;nextAttack+=Time.deltaTime;}
            // A stun takes the enemy out of its attack: it used to hold its tell and strike the moment it recovered.
            if(Health.Broken&&Telegraphing){CancelAttack();nextAttack=Mathf.Max(nextAttack,Health.BrokenUntil);}
            if(GameTime.Paused||Passive||lunging||!Health.Alive||Health.Broken||PartyController.Current==null||GameServices.Current.Input.Blocked)return;
            if(boss!=null&&boss.Busy){if(Telegraphing)CancelAttack();return;}
            // Light-shy: inside a ward it drops its lunge and backs out toward the dark, at the pace the light allows.
            if(Warded){if(Telegraphing)CancelAttack();Move(transform.position-wardFrom,definition.speed);return;}
            // Whoever the player is playing, unless a hero near it is holding the line (S6a, BraceKit): an ordinary body turns to her for those seconds; a boss never does.
            var victim=BraceKit.Holder(this)??PartyController.Current.Active;if(!victim.Health.Alive)return;
            // Chapter 10's berth: a body fights heroes of its own form. A flier doesn't mark or fire at a hero on a shelf, nor a body on the shelf at a ship flying past: it goes home and waits.
            if(OtherForm(victim,Health))
            {
                if(Telegraphing)CancelAttack();
                if(!Mine&&(home-transform.position).sqrMagnitude>1)Move(home-transform.position,definition.speed);
                return;
            }
            // A hero inside a safe pocket can't fight back, so nothing follows them in: they go home and wait.
            if(ZoneController.Current!=null&&ZoneController.Current.InSafePocket)
            {
                if(Telegraphing)CancelAttack();
                if(!Mine&&(home-transform.position).sqrMagnitude>1)Move(home-transform.position,definition.speed);
                return;
            }
            // A body with a behaviour of its own fights by it from here: what's above (a break, a ward, a safe pocket, a boss's special) holds for every body alike.
            if(behaviour!=null)
            {
                // Left behind by the party it goes home, as every body does, unless its behaviour keeps a place of its own or is in the middle of something.
                var gap=victim.transform.position-transform.position;gap.y=0;
                if(gap.sqrMagnitude>35*35&&!Telegraphing&&!acting&&behaviour.Roams){if((home-transform.position).sqrMagnitude>1)Move(home-transform.position,definition.speed);return;}
                behaviour.Tick(victim);return;
            }
            Vector3 d=victim.transform.position-transform.position;d.y=0;float distance=d.magnitude;
            var attack=definition.attacks?.Length>0?definition.attacks[0]:null;
            float range=attack!=null?attack.range:2.6f;
            bool dive=definition.archetype==EnemyArchetype.PackHunter||definition.id=="ChoristerDart";
            if(dive)range=6;
            // A lunge is judged from its head: a hound drawn a body's length ahead of its capsule marks and commits from as far off, head to hero, as a short body does middle to middle,
            // so it covers the same ground before its front meets him (CorePlayTests' pack lunge, more than 2.2 m) and stops there rather than in him.
            float near=dive?distance-Mathf.Max(0,front-(controller!=null?controller.radius:.5f)):distance;
            bool ranged=definition.archetype==EnemyArchetype.Spitter||definition.archetype==EnemyArchetype.Serpent;
            if(Telegraphing)
            {
                if(Time.time>=strikeAt)
                {
                    // A shot's marking goes with it until it lands (Telegraph.Carry); anything else strikes now, and its marking flashes.
                    Telegraphing=false;var told=AttackMark;AttackMark=null;Projectile[] shots=null;attackAnimationUntil=Time.time+.55f;
                    var packet=new DamagePacket{source=Health,amount=(attack!=null?attack.damage:18)*DamageScale,type=attack!=null?attack.type:DamageType.Kinetic,breakPower=10,deathAttack=Mine,weight=Mine?HitWeight.Heavy:dive?HitWeight.Medium:HitWeight.Light};
                    if(dive)StartCoroutine(LungeAttack(packet));
                    else if(definition.id=="ChoristerDrifter")
                    {packet.hullVolleyHits=new HashSet<Health>();packet=ProjectileProvenance.Volley(packet);CombatAudio.HostileShot(transform,transform.position+Vector3.up);shots=new Projectile[3];for(int i=-1;i<=1;i++)shots[i+1]=Projectile.Fire(transform.position+Vector3.up,Quaternion.Euler(0,i*10,0)*aim,packet,11,ShotReach(range));}
                    else if(ranged)
                    {
                        packet=ProjectileProvenance.Volley(packet);
                        CombatAudio.HostileShot(transform,transform.position+Vector3.up);
                        int fan=collar!=null&&collar.VolleyAttached?1:0;
                        if(collar!=null)packet.amount*=.55f;
                        shots=new Projectile[fan*2+1];
                        for(int i=-fan;i<=fan;i++)shots[i+fan]=Projectile.Fire(transform.position+Vector3.up,Quaternion.Euler(0,i*12,0)*aim,packet,11,ShotReach(range));
                    }
                    else if(Mine)
                    {
                        // The blast and the body's end, in that order, and the blast outlives the body. Going off pays nobody.
                        CombatActor.Strike(transform.position+Vector3.up*.7f,MineBlast,packet).ThroughWalls=false;
                        CombatVfx.Burst(transform.position+Vector3.up,new Color(1.4f,.5f,.2f),"kill");
                        Health.Receive(new DamagePacket{amount=Health.maximum*3,type=DamageType.Pulse,tag=SelfDetonation});
                    }
                    else
                    {
                        // From where it marked, whatever a drag under the limit has done since. Its centre stands 1.1 m ahead of that, which can be past a thin wall the tell began beside: it stays on this side.
                        var mouth=new Vector3(markedFrom.x,transform.position.y+StrikeHeight,markedFrom.z);float ahead=CombatCover.Sweep(mouth,mouth+aim*1.1f,.05f,out var wall)?Mathf.Max(0,wall.distance-.05f):1.1f;
                        CombatActor.Strike(mouth+aim*ahead,Mathf.Max(1,range*.7f),packet).ThroughWalls=false;
                    }
                    if(told!=null){if(shots!=null)told.Carry(shots);else told.Release();}
                    nextAttack=Time.time+(attack!=null?attack.cooldown:1.5f);
                }
                return;
            }
            KeepApart(d,distance,range);
            if(near<range&&Time.time>=nextAttack&&Time.time>=draggedAt+DragSettles&&InSight(victim))
            {
                float tell=attack!=null?attack.telegraph:.65f;Telegraphing=true;strikeAt=Time.time+tell;TellDuration=tell;if(ranged)shotAt=Time.time;
                markedFrom=transform.position;dragged=0;
                aim=d.sqrMagnitude>.001f?d.normalized:transform.forward;
                if(definition.archetype!=EnemyArchetype.Serpent&&boss==null)transform.rotation=Quaternion.LookRotation(aim);
                AttackMark=MarkAttack(tell,dive,ranged,range);
                if(boss==null)FlashLessons.MarkedAttack(victim,distance);
                AudioManager.PlayAt("forceField_000",transform,transform.position+Vector3.up,.07f,AudioBus.SFX,128,.1f,1.7f);return;
            }
            if(definition.archetype==EnemyArchetype.Mine)return;
            if(distance>35){Move(home-transform.position,definition.speed);return;}
            if(ranged)
            {
                // A shooter with a wall in the way closes until it has a line, rather than standing in range of nothing.
                if(distance>range*.85f||Time.time<nextSight)Move(d,definition.speed);
                // A boss's rest is the time to close: a heavy brawler doesn't back-pedal from melee. A serpent's Move swims forward through a
                // broad turn, so this is how it keeps moving with the party on it: without it the Cantor stood still at close range.
                else if(distance<range*.4f&&(boss==null||definition.archetype==EnemyArchetype.Serpent))Move(-d,definition.speed);
            }
            // Pressed in among its own, a body in reach stops there instead of shoving for a place at the front.
            // But with no line it walks on: stood 4 m from a hero round a pillar, a hound would never get one.
            else if((near>range*(crowded?1:.7f)||Time.time<nextSight)&&distance>Contact(victim))
            {
                Vector3 approach=d.normalized;
                if(definition.archetype==EnemyArchetype.PackHunter)approach+=Vector3.Cross(Vector3.up,approach)*Mathf.Sin(Time.time+GetInstanceID())*.35f;
                Move(approach,definition.speed);
            }
            // A boss turns at a heavy body's pace, so "behind it" is a place a player can get to.
            if(d.sqrMagnitude>.01f&&definition.archetype!=EnemyArchetype.Serpent)
                transform.rotation=boss!=null?Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(d),BossTurnRate*Time.deltaTime):Quaternion.LookRotation(d);
        }
        const float BossTurnRate=120;
        /// <summary>Where a bite or a lunge is measured from, the same height as the look before its tell and a shot's muzzle. Knee-high rubble that a blade swings over stops neither.</summary>
        const float StrikeHeight=1;
        float nextSight;
        /// <summary>A tell starts only at a hero it can see: pressed against a wall with him behind it, it keeps walking, and round a corner it can see past it still attacks.
        /// Asked only when a tell would start, and a blocked look waits a beat before the next, so a body at a wall doesn't ask every frame.</summary>
        bool InSight(CombatActor victim)
        {
            if(Time.time<nextSight)return false;
            if(CombatCover.Clear(transform.position+Vector3.up*StrikeHeight,victim.transform.position+Vector3.up*.9f))return true;
            nextSight=Time.time+.25f;return false;
        }
        /// <summary>A light shove apart for bodies that aren't busy, under walking pace so nothing is ever held off a hero or pinned in a corridor. Never mid-tell:
        /// what a body marked is where it strikes from. It only ever moves itself, through its controller, so walls and floors still hold it.</summary>
        void KeepApart(Vector3 toVictim,float distance,float reach)
        {
            crowded=false;
            if(boss!=null||Mine||definition.archetype==EnemyArchetype.Serpent||!controller.enabled||ZoneController.Current==null||Flying)return;
            var at=transform.position;var push=Vector3.zero;
            foreach(var other in crowd)
            {
                if(other==this||other.Passive||!other.Health.Alive)continue;
                if(Shove(at,other.transform.position,bodyRadius+other.bodyRadius,controller.radius+other.controller.radius,ref push))crowded=true;
            }
            // In reach, its own can't press it further in: they fan out round the hero, or the one in front ends up on top of him.
            if(distance<reach&&distance>.01f){float inward=Vector3.Dot(push,toVictim)/distance;if(inward>0)push-=toVictim*(inward/distance);}
            foreach(var hero in PartyController.Current.members)if(hero.Health.Alive)Shove(at,hero.transform.position,bodyRadius+HeroBody,controller.radius+HeroCapsule,ref push);
            if(push.sqrMagnitude>.0001f)controller.Move(Vector3.ClampMagnitude(push,1)*(KeepApartSpeed*Time.deltaTime));
        }
        /// <summary>Full strength where the capsules touch, which is as close as they can get. Scaled on the whole gap instead, a body at the capsules' limit shoved at a quarter strength
        /// and a pack still walking in never came clear of itself.</summary>
        bool Shove(Vector3 at,Vector3 from,float gap,float touching,ref Vector3 push)
        {
            var away=at-from;away.y=0;float distance=away.magnitude;if(distance>=gap)return false;
            push+=(distance>.01f?away/distance:transform.right)*Mathf.Min(1,(gap-distance)/Mathf.Max(.05f,gap-touching));return true;
        }
        IEnumerator LungeAttack(DamagePacket packet)
        {
            lunging=true;var victims=new HashSet<Health>();Vector3 direction=aim;
            // The bite is at its mouth: for a body drawn long ahead of its middle, further ahead than the half metre a short one bites at, so stopping at the hero's capsule still reaches him.
            float bite=Mathf.Max(LungeAhead,front-.5f);
            // Exactly its travel, however long a frame runs: the lane is painted to the end of it. Or to where its front meets a hero in the lane: it stops there, it doesn't run on into him.
            for(float travelled=0;travelled<LungeTravel&&Health.Alive&&!Health.Broken&&!Warded;)
            {
                if(GameTime.Paused||GameServices.Current.Input.Blocked){yield return null;continue;}
                float step=Mathf.Min(LungeSpeed*Time.deltaTime,LungeTravel-travelled),room=RoomAhead(direction);bool met=room<=step;step=Mathf.Min(step,room);travelled+=step;
                if(controller.enabled&&step>0)controller.Move(direction*step);
                if(SlidOffItsLane(direction))break;
                var hit=CombatActor.Strike(transform.position+Vector3.up*StrikeHeight+direction*bite,LungeRadius,packet);hit.hit=victims;hit.ThroughWalls=false;
                if(met)break;
                yield return null;
            }
            lunging=false;
        }
        /// <summary>How far it can lunge on before its drawn front meets the capsule of a hero in its way (a hero its body would pass over), on the plan.</summary>
        float RoomAhead(Vector3 direction)
        {
            float room=float.PositiveInfinity;if(PartyController.Current==null)return room;
            var at=transform.position;direction.y=0;direction.Normalize();
            foreach(var hero in PartyController.Current.members)
            {
                if(hero==null||!hero.Health.Alive||hero.flight!=Flying)continue;
                float his=hero.TryGetComponent<CharacterController>(out var capsule)?capsule.radius:HeroCapsule;
                var off=hero.transform.position-at;off.y=0;float along=Vector3.Dot(off,direction);if(along<=0)continue;
                if((off-direction*along).magnitude>=his+bodyRadius)continue;
                room=Mathf.Min(room,Mathf.Max(0,along-front-his-.02f));
            }
            return room;
        }
        /// <summary>A controller slides along a wall it meets at an angle, and the strike slid with it, off the lane it painted: there it stops. Only a wall does that: round a hero or one of its own in
        /// the way it goes on as it always has. It casts only once it is off its line, so an ordinary lunge never does.</summary>
        bool SlidOffItsLane(Vector3 direction)
        {
            if(Mathf.Abs(Vector3.Dot(transform.position-markedFrom,Vector3.Cross(Vector3.up,direction)))<=Telegraph.BodyAllowance)return false;
            var chest=transform.position+Vector3.up*StrikeHeight;
            return !CombatCover.Clear(chest,chest+direction,controller.radius);
        }
        /// <summary>How often a body on the ground asks whether it still stands on its own floor; how far above or below its feet that floor may be and still be under it (a slope, the
        /// bake's own rounding); how far to one side (the bake keeps half a body clear of every wall and drop, and a body at a ledge's lip stands past its edge by as much again); and
        /// how far round it it looks for that floor when it doesn't.</summary>
        public const float FloorCheckEvery=.25f,OnFloorWithin=.5f,BesideFloor=1.25f,DownWithin=5,SunkWithin=2;
        float floorCheckAt;bool floorKnown;Vector3 ownFloor,floorSeenAt=new(float.NaN,0,0);
        static NavMeshPath floorPath;
        /// <summary>Nothing that fights on foot may come to rest where a hero on the floor can't strike it. On the 17:17 build both of the Frozen Host's third-rank Bridle-hounds ended in
        /// the gap between Rig three and the north wall, one standing on the other 1.3 m up, while the lead swung at them from the rig's corner for two minutes, and the fight never
        /// cleared (nov14-recon24 s35). The bake has no floor in a gap narrower than a body, nor on whatever it never saw (another body's back, ice that melts, a frozen rank), and it
        /// makes a solid prop's flat top an island of floor of its own. So a body on the ground keeps the floor it first stood on (the baked floor at its feet, and everything joined to
        /// it) and the floor the lead stands on as its own, and one that finds itself anywhere else, with either beside or below it within <see cref="DownWithin"/>, is put on it at
        /// once, at the nearest place with room for it. Never up: a body that went over a drop to a floor below is left where it is, as is one in a zone or a fixture with no bake.</summary>
        void KeepToTheFloor()
        {
            if(Time.time<floorCheckAt)return;floorCheckAt=Time.time+FloorCheckEvery;
            if(Passive||boss!=null||Mine||lunging||acting||definition==null||definition.archetype==EnemyArchetype.Serpent||behaviour!=null&&!behaviour.Movable)return;
            var feet=transform.position;
            if(!float.IsNaN(floorSeenAt.x)&&(feet-floorSeenAt).sqrMagnitude<.25f)return;
            bool sampled=NavMesh.SamplePosition(feet,out var on,2,NavMesh.AllAreas),inside=InScenery(feet);
            bool baked=sampled&&!inside&&Mathf.Abs(on.position.y-feet.y)<=OnFloorWithin&&Beside(on.position,feet)<=BesideFloor;
            if(!floorKnown){if(baked){ownFloor=on.position;floorKnown=true;floorSeenAt=feet;}return;}
            // The floor the lead stands on counts as its own too: a pen's floor is an island in the bake until its ice melts, and what comes out of it fights on the lead's.
            var lead=PartyController.Current!=null?PartyController.Current.Active:null;
            leadOnFloor=false;
            if(lead!=null&&!lead.flight&&NavMesh.SamplePosition(lead.transform.position,out var under,2,NavMesh.AllAreas)&&Mathf.Abs(under.position.y-lead.transform.position.y)<=OnFloorWithin)
            {leadOnFloor=true;leadFloor=under.position;}
            if(baked&&Joined(on.position)){floorSeenAt=feet;return;}
            // Off its floor: the nearest place on it, ring by ring outward. Below it only, for a body on floor of another piece (a prop's top, or a floor the bake doesn't join to
            // its own, as through a door that opens); beside it too, for one on none (a gap, another body's back, inside something solid).
            // And a little above, for one inside something solid: a controller set down inside a wall sinks through the floor under it before anything sees it.
            float highest=baked?feet.y-OnFloorWithin:inside?feet.y+SunkWithin:feet.y+OnFloorWithin;
            for(float r=0;r<=DownWithin;r+=.75f)
                for(int i=0;i<(r==0?1:12);i++)
                    foreach(float depth in Depths)
                    {
                        var probe=feet+Quaternion.Euler(0,i*30,0)*Vector3.forward*r+Vector3.down*depth;
                        if(!NavMesh.SamplePosition(probe,out var to,2,NavMesh.AllAreas)||to.position.y>highest||!Room(to.position)||!Joined(to.position))continue;
                        Bring(feet,to.position);return;
                    }
            // Nothing of its own floor near it: left be until it moves.
            floorSeenAt=feet;
            if(!saidOff){saidOff=true;Debug.Log($"ENEMY_OFF_FLOOR {definition.id} at {feet}: none of its floor or the lead's near it");}
        }
        bool saidOff;
        static readonly float[] Depths={0,1.5f,3.5f};
        static float Beside(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
        bool leadOnFloor;Vector3 leadFloor;
        static readonly Collider[] Crowded=new Collider[8];
        /// <summary>Room for its body standing there: nothing solid in its capsule, a hero's or another body's included (set down inside another controller, it would be pushed out
        /// anywhere, up onto it as like as not).</summary>
        bool Room(Vector3 at)
        {
            float r=controller.radius*.9f,low=controller.radius+.25f;
            int count=Physics.OverlapCapsuleNonAlloc(at+Vector3.up*low,at+Vector3.up*Mathf.Max(low,controller.height-controller.radius),r,Crowded,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(Crowded[i]!=controller&&!Crowded[i].transform.IsChildOf(transform))return false;
            return true;
        }
        /// <summary>Its body is inside something solid of the scene's (put there by a wall that came up round it, or a fixture): a body or a hero it touches doesn't count.</summary>
        bool InScenery(Vector3 at)
        {
            // Clear of the floor under it by half a metre, so a ramp or a step it stands on is no wall.
            float r=controller.radius*.8f,low=controller.radius+.4f;
            int count=Physics.OverlapCapsuleNonAlloc(at+Vector3.up*low,at+Vector3.up*Mathf.Max(low,controller.height-controller.radius),r,Crowded,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var c=Crowded[i];
                if(c==controller||c.transform.IsChildOf(transform)||c is CharacterController||c.attachedRigidbody!=null||c.GetComponentInParent<Health>()!=null)continue;
                return true;
            }
            return false;
        }
        bool Joined(Vector3 at)=>JoinedTo(at,ownFloor)||leadOnFloor&&JoinedTo(at,leadFloor);
        static bool JoinedTo(Vector3 at,Vector3 floor)
        {
            if(!NavMesh.Raycast(at,floor,out _,NavMesh.AllAreas))return true;
            floorPath??=new NavMeshPath();
            return NavMesh.CalculatePath(at,floor,NavMesh.AllAreas,floorPath)&&floorPath.status==NavMeshPathStatus.PathComplete;
        }
        void Bring(Vector3 from,Vector3 to)
        {
            if(Telegraphing)CancelAttack();
            controller.enabled=false;transform.position=to;controller.enabled=true;Physics.SyncTransforms();
            var moved=to-from;moved.y=0;markedFrom+=moved;behaviour?.Carried(moved);floorSeenAt=to;
            Debug.Log($"ENEMY_PUT_ON_FLOOR {definition.id} from {from} to {to}");
        }
        /// <summary>Planted: it turns and strikes where it stands and does not walk (a boss rooted for a phase). Its own moves still carry it.</summary>
        internal bool Rooted;
        void Move(Vector3 direction,float speed)
        {
            direction.y=0;if(Rooted||!controller.enabled||direction.sqrMagnitude<.0001f)return;
            float travel=speed*(Time.time<Health.SlowUntil?.4f:1)*Health.Pace*Time.deltaTime;
            if(definition.archetype!=EnemyArchetype.Serpent){controller.Move(direction.normalized*travel);return;}
            // A long animal swims forward through a broad turn. Ranged retreat
            // must not back the head through its body, and firing aim must not
            // reverse the neck independently of that travelled curve.
            const float radius=6;
            var rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(direction),travel/radius*Mathf.Rad2Deg);
            var before=transform.position;controller.Move(rotation*Vector3.forward*travel);
            var moved=transform.position-before;moved.y=0;
            // A wall cannot make the carrier spin in place into its own tail.
            transform.rotation=Quaternion.RotateTowards(transform.rotation,rotation,moved.magnitude/radius*Mathf.Rad2Deg);
        }
    }
}
