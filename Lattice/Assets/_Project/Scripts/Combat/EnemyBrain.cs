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
        public float TelegraphRemaining=>Mathf.Max(0,strikeAt-Time.time);
        public bool Passive;
        CharacterController controller;
        GameObject warning;
        Vector3 home;
        Vector3 aim;
        float nextAttack,strikeAt;
        public float DamageScale=1;
        BossController boss;
        void Awake(){Health=GetComponent<Health>();controller=GetComponent<CharacterController>();home=transform.position;}
        void Start()
        {
            Health.Damaged+=OnDamaged;
            boss=GetComponent<BossController>();
            Health.Died+=OnDeath;
            warning=ActorFactory.Visual("Telegraph",PrimitiveType.Cylinder,transform,new Vector3(3,.015f,3),Vector3.up*.03f,"Threat");warning.SetActive(false);
            nextAttack=Time.time+.6f;
        }
        void OnDamaged(Health _,DamagePacket packet,float amount)
        {
            if(packet.source!=null&&packet.source.TryGetComponent<CombatActor>(out var actor))actor.AwardHit();
            if(definition.archetype==EnemyArchetype.Sentinel&&packet.type==definition.resistance){Health.ShieldUntil=Time.time+2;CombatVfx.Burst(transform.position+Vector3.up,Color.cyan,"shape");}
        }
        void OnDeath(Health _,DamagePacket packet)
        {
            if(packet.source!=null&&packet.source.TryGetComponent<CombatActor>(out var actor))actor.AwardKill();
            if(packet.tag=="lunge")Debug.Log("LUNGE_KILL");
            Debug.Log("COMBAT_KILL "+definition.id);
            CombatVfx.Burst(transform.position+Vector3.up,Color.cyan,"kill");
            if(definition.id=="Scrapmite")CombatActor.Strike(transform.position+Vector3.up*.6f,2.5f,new DamagePacket{source=Health,amount=12,type=DamageType.Pulse});
            if(warning!=null)warning.SetActive(false);controller.enabled=false;Destroy(gameObject,.25f);
        }
        void Update()
        {
            if(Passive||!Health.Alive||Health.Broken||PartyController.Current==null||GameServices.Current.Input.Blocked)return;
            if(boss!=null&&boss.Busy){if(warning!=null)warning.SetActive(false);Telegraphing=false;return;}
            var victim=PartyController.Current.Active;if(!victim.Health.Alive)return;
            Vector3 d=victim.transform.position-transform.position;d.y=0;float distance=d.magnitude;
            var attack=definition.attacks?.Length>0?definition.attacks[0]:null;
            float range=attack!=null?attack.range:2.6f;
            bool ranged=definition.archetype==EnemyArchetype.Spitter||definition.archetype==EnemyArchetype.Serpent;
            if(Telegraphing)
            {
                if(Time.time>=strikeAt)
                {
                    Telegraphing=false;warning.SetActive(false);
                    var packet=new DamagePacket{source=Health,amount=(attack!=null?attack.damage:18)*DamageScale,type=attack!=null?attack.type:DamageType.Kinetic,breakPower=10};
                    if(ranged)Projectile.Fire(transform.position+Vector3.up,aim,packet,11);
                    else CombatActor.Strike(transform.position+aim*1.1f+Vector3.up*.7f,definition.archetype==EnemyArchetype.Mine?3:range*.7f,packet);
                    if(definition.archetype==EnemyArchetype.Mine)Health.Receive(new DamagePacket{amount=Health.maximum*3,type=DamageType.Pulse,source=victim.Health});
                    nextAttack=Time.time+(attack!=null?attack.cooldown:1.5f);
                }
                return;
            }
            if(distance<range&&Time.time>=nextAttack){Telegraphing=true;strikeAt=Time.time+(attack!=null?attack.telegraph:.65f);aim=d.normalized;warning.SetActive(true);return;}
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
        void Move(Vector3 direction,float speed){direction.y=0;if(controller.enabled)controller.Move(direction.normalized*speed*(Time.time<Health.SlowUntil?.4f:1)*Time.deltaTime+(ZoneController.Current.Flight?Vector3.zero:Vector3.down*6*Time.deltaTime));}
    }
}
