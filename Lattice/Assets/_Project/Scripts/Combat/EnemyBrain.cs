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
            if(party!=null)foreach(var hero in party.members)if(hero.Health.Alive&&mark.Contains(hero.transform.position))hero.Health.Receive(packet);
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
        public bool Passive;
        public int ImpactSequence{get;private set;}
        public float ImpactStarted{get;private set;}=float.NegativeInfinity;
        public bool ArmoredImpact{get;private set;}
        public const float ImpactDuration=.36f;
        CharacterController controller;
        Vector3 home;
        Vector3 aim;
        float nextAttack,strikeAt,attackAnimationUntil;
        bool lunging;
        public float DamageScale=1;
        float wardedUntil;Vector3 wardFrom;
        /// <summary>Touched by a lit relay's light. It keeps clear for a moment after, so a pack paces the ward's edge instead of
        /// shuddering on it.</summary>
        public void Ward(Vector3 from){wardedUntil=Time.time+1;wardFrom=from;}
        public bool Warded=>Time.time<wardedUntil;
        BossController boss;
        CantorCollar collar;
        void Awake(){Health=GetComponent<Health>();controller=GetComponent<CharacterController>();home=transform.position;}
        void Start()
        {
            Health.Damaged+=OnDamaged;
            boss=GetComponent<BossController>();
            collar=GetComponent<CantorCollar>();
            Health.Died+=OnDeath;
            nextAttack=Time.time+.6f;
        }
        void OnDamaged(Health _,DamagePacket packet,float amount)
        {
            if(packet.source!=null&&packet.source.TryGetComponent<CombatActor>(out var actor))actor.AwardHit(amount/Mathf.Max(1,actor.character=="Sela"?actor.rangedDamage:actor.damage));
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
            else if(Health.Alive&&definition.archetype==EnemyArchetype.Sentinel&&packet.type==definition.resistance)
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
        const float MineBlast=3,LungeLength=4.5f+1.15f,LungeWidth=2.3f;
        /// <summary>Every ordinary attack marks the ground it will strike, in its true shape: a lunge its lane, a mine its blast, a
        /// bite its reach, a shot its line. A 3 m disc at the feet understated all of them.</summary>
        Telegraph MarkAttack(float tell,bool dive,bool ranged,float range)
        {
            var at=transform.position;Telegraph mark;
            if(Mine)mark=Telegraph.Circle(at,MineBlast,tell,Telegraph.Heavy,true);
            else if(dive)mark=Telegraph.Line(at,aim,LungeLength,LungeWidth,tell,Telegraph.Threat,true);
            else if(definition.id=="ChoristerDrifter"||collar!=null&&collar.VolleyAttached)mark=Telegraph.Cone(at,aim,12,32,tell,Telegraph.Threat,true);
            else if(ranged)mark=Telegraph.Line(at,aim,13,1.1f,tell,Telegraph.Threat,true);
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
        /// <summary>Moves the body through its controller, so walls and floors still hold it.</summary>
        public void Displace(Vector3 delta){if(!CanBeMoved)return;delta.y=0;controller.Move(delta);}
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
            else CombatAudio.Death(transform,definition.archetype==EnemyArchetype.Sentinel||definition.archetype==EnemyArchetype.Mine||definition.id=="MusterSkiff");
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
                    {packet.hullVolleyHits=new HashSet<Health>();packet=ProjectileProvenance.Volley(packet);CombatAudio.HostileShot(transform,transform.position+Vector3.up);for(int i=-1;i<=1;i++)Projectile.Fire(transform.position+Vector3.up,Quaternion.Euler(0,i*10,0)*aim,packet,11);}
                    else if(ranged)
                    {
                        packet=ProjectileProvenance.Volley(packet);
                        CombatAudio.HostileShot(transform,transform.position+Vector3.up);
                        int fan=collar!=null&&collar.VolleyAttached?1:0;
                        if(collar!=null)packet.amount*=.55f;
                        for(int i=-fan;i<=fan;i++)Projectile.Fire(transform.position+Vector3.up,Quaternion.Euler(0,i*12,0)*aim,packet,11);
                    }
                    else if(Mine)
                    {
                        // The blast and the body's end, in that order, and the blast outlives the body. Going off pays nobody.
                        CombatActor.Strike(transform.position+Vector3.up*.7f,MineBlast,packet);
                        CombatVfx.Burst(transform.position+Vector3.up,new Color(1.4f,.5f,.2f),"kill");
                        Health.Receive(new DamagePacket{amount=Health.maximum*3,type=DamageType.Pulse,tag=SelfDetonation});
                    }
                    else CombatActor.Strike(transform.position+aim*1.1f+Vector3.up*.7f,Mathf.Max(1,range*.7f),packet);
                    nextAttack=Time.time+(attack!=null?attack.cooldown:1.5f);
                }
                return;
            }
            if(distance<range&&Time.time>=nextAttack)
            {
                float tell=attack!=null?attack.telegraph:.65f;Telegraphing=true;strikeAt=Time.time+tell;
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
                if(distance>range*.85f)Move(d,definition.speed);
                // A boss's rest is the time to close: a heavy brawler doesn't back-pedal from melee.
                else if(distance<range*.4f&&boss==null)Move(-d,definition.speed);
            }
            else if(distance>range*.7f)
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
        IEnumerator LungeAttack(DamagePacket packet)
        {
            lunging=true;var victims=new HashSet<Health>();Vector3 direction=aim;
            for(float elapsed=0;elapsed<.28f&&Health.Alive&&!Health.Broken&&!Warded;)
            {
                if(GameTime.Paused||GameServices.Current.Input.Blocked){yield return null;continue;}
                elapsed+=Time.deltaTime;
                if(controller.enabled)controller.Move(direction*16*Time.deltaTime);
                var hit=CombatActor.Strike(transform.position+Vector3.up*.7f+direction*.5f,1.15f,packet);hit.hit=victims;
                yield return null;
            }
            lunging=false;
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
