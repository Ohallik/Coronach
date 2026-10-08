using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    public sealed class EnemyBrain:MonoBehaviour
    {
        public EnemyDef definition;
        public Health Health{get;private set;}
        public bool Telegraphing{get;private set;}
        public bool Attacking=>lunging||Time.time<attackAnimationUntil;
        public float TelegraphRemaining=>Mathf.Max(0,strikeAt-Time.time);
        public bool Passive;
        public int ImpactSequence{get;private set;}
        public float ImpactStarted{get;private set;}=float.NegativeInfinity;
        public bool ArmoredImpact{get;private set;}
        public const float ImpactDuration=.36f;
        CharacterController controller;
        GameObject warning;
        Vector3 home;
        Vector3 aim;
        float nextAttack,strikeAt,attackAnimationUntil;
        bool lunging;
        public float DamageScale=1;
        BossController boss;
        CantorCollar collar;
        void Awake(){Health=GetComponent<Health>();controller=GetComponent<CharacterController>();home=transform.position;}
        void Start()
        {
            Health.Damaged+=OnDamaged;
            boss=GetComponent<BossController>();
            collar=GetComponent<CantorCollar>();
            Health.Died+=OnDeath;
            warning=ActorFactory.Visual("Telegraph",PrimitiveType.Quad,transform,new Vector3(3,3,1),Vector3.up*.04f,"Threat");
            warning.transform.localRotation=Quaternion.Euler(90,0,0);
            var renderer=warning.GetComponent<Renderer>();renderer.sharedMaterial=Resources.Load<Material>("Effects/circle_02");
            var tint=new MaterialPropertyBlock();tint.SetColor("_BaseColor",new Color(1,.32f,.09f,.9f));renderer.SetPropertyBlock(tint);
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;warning.SetActive(false);
            nextAttack=Time.time+.6f;
        }
        void OnDamaged(Health _,DamagePacket packet,float amount)
        {
            if(packet.source!=null&&packet.source.TryGetComponent<CombatActor>(out var actor))actor.AwardHit();
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
            if(warning!=null)warning.SetActive(false);
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
            if(packet.source!=null&&packet.source.TryGetComponent<CombatActor>(out var actor))actor.AwardKill();
            if(packet.tag=="lunge")Debug.Log("LUNGE_KILL");
            Debug.Log("COMBAT_KILL "+definition.id);
            bool disabledRig=definition.id=="Burrower",releasedAnimal=definition.id=="Cantor";
            CombatVfx.Burst(transform.position+Vector3.up,Color.cyan,disabledRig||releasedAnimal?"failure":"kill");
            // Resolution can disable equipment or release an animal. Neither
            // outcome borrows the generic boss explosion.
            if(releasedAnimal)AudioManager.PlayAt("doorOpen_000",transform,transform.position+Vector3.up,.24f,AudioBus.SFX,70);
            else if(disabledRig)CombatAudio.Death(transform,true);
            else if(definition.boss)AudioManager.PlayAt("explosionCrunch_000",transform,transform.position+Vector3.up,.3f,AudioBus.SFX,40);
            else CombatAudio.Death(transform,definition.archetype==EnemyArchetype.Sentinel||definition.archetype==EnemyArchetype.Mine);
            CancelAttack();
            if(definition.id=="Scrapmite")CombatActor.Strike(transform.position+Vector3.up*.6f,2.5f,new DamagePacket{source=Health,amount=12,type=DamageType.Pulse,deathAttack=true});
            if(warning!=null)warning.SetActive(false);controller.enabled=false;enabled=false;
        }
        void Update()
        {
            // Charges can step onto geometry or another controller. Settling
            // cannot depend on the AI choosing to chase again afterward.
            if(!GameTime.Paused&&Health.Alive&&controller.enabled&&ZoneController.Current!=null&&!ZoneController.Current.Flight&&!GameServices.Current.Input.Blocked)
                controller.Move(Vector3.down*6*Time.deltaTime);
            if(GameTime.Paused||Passive||lunging||!Health.Alive||Health.Broken||PartyController.Current==null||GameServices.Current.Input.Blocked)return;
            if(boss!=null&&boss.Busy){if(warning!=null)warning.SetActive(false);Telegraphing=false;return;}
            var victim=PartyController.Current.Active;if(!victim.Health.Alive)return;
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
                    Telegraphing=false;warning.SetActive(false);attackAnimationUntil=Time.time+.55f;
                    var packet=new DamagePacket{source=Health,amount=(attack!=null?attack.damage:18)*DamageScale,type=attack!=null?attack.type:DamageType.Kinetic,breakPower=10};
                    if(dive)StartCoroutine(LungeAttack(packet));
                    else if(definition.id=="ChoristerDrifter")
                    {CombatAudio.HostileShot(transform,transform.position+Vector3.up);for(int i=-1;i<=1;i++)Projectile.Fire(transform.position+Vector3.up,Quaternion.Euler(0,i*10,0)*aim,packet,11);}
                    else if(ranged)
                    {
                        CombatAudio.HostileShot(transform,transform.position+Vector3.up);
                        int fan=collar!=null&&collar.VolleyAttached?1:0;
                        if(collar!=null)packet.amount*=.55f;
                        for(int i=-fan;i<=fan;i++)Projectile.Fire(transform.position+Vector3.up,Quaternion.Euler(0,i*12,0)*aim,packet,11);
                    }
                    else CombatActor.Strike(transform.position+aim*1.1f+Vector3.up*.7f,definition.archetype==EnemyArchetype.Mine?3:range*.7f,packet);
                    if(definition.archetype==EnemyArchetype.Mine)Health.Receive(new DamagePacket{amount=Health.maximum*3,type=DamageType.Pulse,source=victim.Health});
                    nextAttack=Time.time+(attack!=null?attack.cooldown:1.5f);
                }
                return;
            }
            if(distance<range&&Time.time>=nextAttack)
            {
                Telegraphing=true;strikeAt=Time.time+(attack!=null?attack.telegraph:.65f);
                aim=d.sqrMagnitude>.001f?d.normalized:transform.forward;transform.rotation=Quaternion.LookRotation(aim);
                warning.SetActive(true);AudioManager.Play("computerNoise_000",.08f);return;
            }
            if(definition.archetype==EnemyArchetype.Mine)return;
            if(distance>35){Move(home-transform.position,definition.speed);return;}
            if(ranged)
            {if(distance>range*.85f)Move(d,definition.speed);else if(distance<range*.4f)Move(-d,definition.speed);}
            else if(distance>range*.7f)
            {
                Vector3 approach=d.normalized;
                if(definition.archetype==EnemyArchetype.PackHunter)approach+=Vector3.Cross(Vector3.up,approach)*Mathf.Sin(Time.time+GetInstanceID())*.35f;
                Move(approach,definition.speed);
            }
            if(d.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(d);
        }
        IEnumerator LungeAttack(DamagePacket packet)
        {
            lunging=true;var victims=new HashSet<Health>();Vector3 direction=aim;
            for(float elapsed=0;elapsed<.28f&&Health.Alive&&!Health.Broken;)
            {
                if(GameTime.Paused||GameServices.Current.Input.Blocked){yield return null;continue;}
                elapsed+=Time.deltaTime;
                if(controller.enabled)controller.Move(direction*16*Time.deltaTime);
                var hit=CombatActor.Strike(transform.position+Vector3.up*.7f+direction*.5f,1.15f,packet);hit.hit=victims;
                yield return null;
            }
            lunging=false;
        }
        void Move(Vector3 direction,float speed){direction.y=0;if(controller.enabled)controller.Move(direction.normalized*speed*(Time.time<Health.SlowUntil?.4f:1)*Time.deltaTime);}
    }
}
