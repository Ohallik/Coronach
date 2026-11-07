using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
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
        public bool Attacking=>lunging||Time.time<attackAnimationUntil;
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
        const float KeepApartSpeed=2.4f,HeroBody=.45f,HeroCapsule=.35f,MostBody=2;
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
        void OnEnable(){if(!crowd.Contains(this))crowd.Add(this);}
        void OnDisable(){crowd.Remove(this);}
        void Awake(){Health=GetComponent<Health>();controller=GetComponent<CharacterController>();home=transform.position;}
        void Start()
        {
            // Its drawn body, not its capsule: a Scrapmite's legs reach .75 m on a .55 m capsule, so five of them standing shoulder to shoulder were one heap.
            bodyRadius=controller!=null?controller.radius:.5f;
            foreach(var body in GetComponentsInChildren<Renderer>())
                if(body is MeshRenderer||body is SkinnedMeshRenderer)bodyRadius=Mathf.Max(bodyRadius,Mathf.Min(MostBody,Mathf.Min(body.bounds.extents.x,body.bounds.extents.z)));
            Health.Damaged+=OnDamaged;
            boss=GetComponent<BossController>();
            collar=GetComponent<CantorCollar>();
            Health.Died+=OnDeath;
            nextAttack=Time.time+.6f;
        }
        void OnDamaged(Health _,DamagePacket packet,float amount)
        {
            CombatActor.PayHit(packet,amount);
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
        /// <summary>Whether a skill may drag this body (Tether Hook, Gravity Fold): bosses, serpents and anchored mines
        /// hold their ground (D201).</summary>
        public bool CanBeMoved=>Health.Alive&&controller!=null&&controller.enabled&&GetComponent<BossController>()==null&&
            definition!=null&&!definition.boss&&definition.archetype!=EnemyArchetype.Serpent&&definition.archetype!=EnemyArchetype.Mine;
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
        public void RestoreAfterRecovery()
        {
            CancelAttack();ImpactStarted=float.NegativeInfinity;ImpactSequence=0;
            nextAttack=Time.time+.6f;enabled=true;
        }
        void OnDestroy()
        {
            if(Health==null)return;
            Health.Damaged-=OnDamaged;Health.Died-=OnDeath;
        }
        void OnDeath(Health _,DamagePacket packet)
        {
            if(packet.source!=null&&packet.source.TryGetComponent<CombatActor>(out var actor))actor.AwardKill(!definition.boss);// A boss gets its own line, never "One less."
            if(packet.tag=="lunge")Debug.Log("LUNGE_KILL");
            Debug.Log("COMBAT_KILL "+definition.id);
            bool disabledRig=definition.id=="Burrower",releasedAnimal=definition.id=="Cantor",yielded=definition.id=="HessaKorl";
            CombatVfx.Burst(transform.position+Vector3.up,Color.cyan,disabledRig||releasedAnimal||yielded?"failure":"kill");
            // Resolution can disable equipment or release an animal. Neither
            // outcome borrows the generic boss explosion.
            if(releasedAnimal)AudioManager.PlayAt("doorOpen_000",transform,transform.position+Vector3.up,.24f,AudioBus.SFX,70);
            // A captain who yields drops her lance; she is arrested, not killed (D202).
            else if(yielded)AudioManager.PlayAt("impactMetal_000",transform,transform.position+Vector3.up,.3f,AudioBus.SFX,40,.06f,.7f);
            else if(disabledRig)CombatAudio.Death(transform,true);
            else if(definition.boss)AudioManager.PlayAt("explosionCrunch_000",transform,transform.position+Vector3.up,.3f,AudioBus.SFX,40);
            else CombatAudio.Death(transform,definition.id=="SentinelHusk"||definition.archetype==EnemyArchetype.Mine||definition.id=="MusterSkiff");
            if(definition.boss)Telegraph.ClearAll(this);
            CancelAttack();
            if(definition.id=="Scrapmite")DeathBlast.Arm(transform.position,new DamagePacket{source=Health,amount=12,type=DamageType.Pulse,deathAttack=true});
            controller.enabled=false;enabled=false;
        }
        void Update()
        {
            // Charges can step onto geometry or another controller. Settling
            // cannot depend on the AI choosing to chase again afterward.
            if(!GameTime.Paused&&Health.Alive&&controller.enabled&&ZoneController.Current!=null&&!ZoneController.Current.Flight&&!GameServices.Current.Input.Blocked)
                controller.Move(Vector3.down*6*Time.deltaTime);
            // A conversation or load stops the clock on what it interrupted: no strike the instant it ends.
            if(GameServices.Current.Input.Blocked&&!GameTime.Paused){strikeAt+=Time.deltaTime;nextAttack+=Time.deltaTime;}
            // A stun takes the enemy out of its attack: it used to hold its tell and strike the moment it recovered.
            if(Health.Broken&&Telegraphing){CancelAttack();nextAttack=Mathf.Max(nextAttack,Health.BrokenUntil);}
            if(GameTime.Paused||Passive||lunging||!Health.Alive||Health.Broken||PartyController.Current==null||GameServices.Current.Input.Blocked)return;
            if(boss!=null&&boss.Busy){if(Telegraphing)CancelAttack();return;}
            // Light-shy: inside a ward it drops its lunge and backs out toward the dark, at the pace the light allows.
            if(Warded){if(Telegraphing)CancelAttack();Move(transform.position-wardFrom,definition.speed);return;}
            var victim=PartyController.Current.Active;if(!victim.Health.Alive)return;
            // A hero inside a safe pocket can't fight back, so nothing follows them in: they go home and wait.
            if(ZoneController.Current!=null&&ZoneController.Current.InSafePocket)
            {
                if(Telegraphing)CancelAttack();
                if(!Mine&&(home-transform.position).sqrMagnitude>1)Move(home-transform.position,definition.speed);
                return;
            }
            Vector3 d=victim.transform.position-transform.position;d.y=0;float distance=d.magnitude;
            var attack=definition.attacks?.Length>0?definition.attacks[0]:null;
            float range=attack!=null?attack.range:2.6f;
            bool dive=definition.archetype==EnemyArchetype.PackHunter||definition.id=="ChoristerDart";
            if(dive)range=6;
            bool ranged=definition.archetype==EnemyArchetype.Spitter||definition.archetype==EnemyArchetype.Serpent;
            if(Telegraphing)
            {
                if(Time.time>=strikeAt)
                {
                    Telegraphing=false;if(AttackMark!=null){AttackMark.Release();AttackMark=null;}attackAnimationUntil=Time.time+.55f;
                    var packet=new DamagePacket{source=Health,amount=(attack!=null?attack.damage:18)*DamageScale,type=attack!=null?attack.type:DamageType.Kinetic,breakPower=10,deathAttack=Mine};
                    if(dive)StartCoroutine(LungeAttack(packet));
                    else if(definition.id=="ChoristerDrifter")
                    {packet.hullVolleyHits=new HashSet<Health>();packet=ProjectileProvenance.Volley(packet);CombatAudio.HostileShot(transform,transform.position+Vector3.up);for(int i=-1;i<=1;i++)Projectile.Fire(transform.position+Vector3.up,Quaternion.Euler(0,i*10,0)*aim,packet,11,ShotReach(range));}
                    else if(ranged)
                    {
                        packet=ProjectileProvenance.Volley(packet);
                        CombatAudio.HostileShot(transform,transform.position+Vector3.up);
                        int fan=collar!=null&&collar.VolleyAttached?1:0;
                        if(collar!=null)packet.amount*=.55f;
                        for(int i=-fan;i<=fan;i++)Projectile.Fire(transform.position+Vector3.up,Quaternion.Euler(0,i*12,0)*aim,packet,11,ShotReach(range));
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
                    nextAttack=Time.time+(attack!=null?attack.cooldown:1.5f);
                }
                return;
            }
            KeepApart(d,distance,range);
            if(distance<range&&Time.time>=nextAttack&&Time.time>=draggedAt+DragSettles&&InSight(victim))
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
            else if(distance>range*(crowded?1:.7f)||Time.time<nextSight)
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
            if(boss!=null||Mine||definition.archetype==EnemyArchetype.Serpent||!controller.enabled||ZoneController.Current==null||ZoneController.Current.Flight)return;
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
            // Exactly its travel, however long a frame runs: the lane is painted to the end of it.
            for(float travelled=0;travelled<LungeTravel&&Health.Alive&&!Health.Broken&&!Warded;)
            {
                if(GameTime.Paused||GameServices.Current.Input.Blocked){yield return null;continue;}
                float step=Mathf.Min(LungeSpeed*Time.deltaTime,LungeTravel-travelled);travelled+=step;
                if(controller.enabled)controller.Move(direction*step);
                if(SlidOffItsLane(direction))break;
                var hit=CombatActor.Strike(transform.position+Vector3.up*StrikeHeight+direction*LungeAhead,LungeRadius,packet);hit.hit=victims;hit.ThroughWalls=false;
                yield return null;
            }
            lunging=false;
        }
        /// <summary>A controller slides along a wall it meets at an angle, and the strike slid with it, off the lane it painted: there it stops. Only a wall does that: round a hero or one of its own in
        /// the way it goes on as it always has. It casts only once it is off its line, so an ordinary lunge never does.</summary>
        bool SlidOffItsLane(Vector3 direction)
        {
            if(Mathf.Abs(Vector3.Dot(transform.position-markedFrom,Vector3.Cross(Vector3.up,direction)))<=Telegraph.BodyAllowance)return false;
            var chest=transform.position+Vector3.up*StrikeHeight;
            return !CombatCover.Clear(chest,chest+direction,controller.radius);
        }
        void Move(Vector3 direction,float speed)
        {
            direction.y=0;if(!controller.enabled||direction.sqrMagnitude<.0001f)return;
            float travel=speed*(Time.time<Health.SlowUntil?.4f:1)*Time.deltaTime;
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
