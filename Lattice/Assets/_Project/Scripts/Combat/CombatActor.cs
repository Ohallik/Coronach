using System.Collections;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    [RequireComponent(typeof(Health))]
    public sealed class CombatActor:MonoBehaviour
    {
        public Health Health{get;private set;}
        public ActorState State{get;private set;}
        public float charge,thrust=100,damage=50,rangedDamage=50,damageScale=1,plating,resonance=10,response=10,fortune=5;
        public DamageType edgeType=DamageType.Kinetic;
        public DamageType emitterType=DamageType.Beam;
        public string character="Taren";
        public bool flight;
        public Health target;
        public IMotor motor;
        public int combo;
        public int Kills{get;private set;}
        public int FlashMoves{get;private set;}
        public int FlashGuards{get;private set;}
        public readonly float[] cooldowns=new float[4];
        float nextAttack,actionUntil,dodgeAt=-99,guardAt=-99,overdriveUntil,refractUntil;
        bool guarding,critical,flashConsumed;
        public float GuardDamageMultiplier=>guarding?.35f:1;
        public bool CanAct=>!GameTime.Paused&&Health.Alive&&State!=ActorState.Stagger&&GameTime.Now>=actionUntil;
        public float MotorDelta=>GameTime.Paused?0:GameTime.Now<Health.InvulnerableUntil?Time.unscaledDeltaTime:Time.deltaTime;
        void Awake(){Health=GetComponent<Health>();Health.Died+=(_,__)=>State=ActorState.Down;}
        void Update()
        {
            if(!Health.Alive)return;
            if(GameTime.Now>=actionUntil&&State!=ActorState.Guard)State=motor!=null&&motor.Velocity.sqrMagnitude>.1f?ActorState.Move:ActorState.Idle;
            charge=Mathf.Max(0,charge-Time.deltaTime*.4f);thrust=Mathf.Min(100,thrust+Time.deltaTime*17);
            for(int i=0;i<4;i++)cooldowns[i]=Mathf.Max(0,cooldowns[i]-Time.deltaTime);
        }
        public void Guard(bool held)
        {
            if(held&&!guarding)guardAt=GameTime.Now;
            guarding=held&&!flight; if(guarding)State=ActorState.Guard;else if(State==ActorState.Guard)State=ActorState.Idle;
        }
        public void Stagger(float seconds){if(!Health.Alive)return;State=ActorState.Stagger;actionUntil=GameTime.Now+seconds;guarding=false;}
        public bool Dodge(Vector3 direction)
        {
            if(GameTime.Paused||!Health.Alive||State==ActorState.Dodge||State==ActorState.Down||State==ActorState.Stagger)return false;
            State=ActorState.Dodge;dodgeAt=GameTime.Now;flashConsumed=false;
            AudioManager.Play("thrusterFire_000",.16f);
            actionUntil=GameTime.Now+(flight?.3f:.25f);motor.Dash(direction.sqrMagnitude>.01f?direction:motor.Facing,flight?4:3.3f);
            return true;
        }
        public bool Deflect(DamagePacket packet)
        {
            if(GameTime.Now<refractUntil)
            {
                refractUntil=0;critical=true;
                Pierce(Aim(),Packet(rangedDamage*2,DamageType.Beam,60),flight?16:11);
                return true;
            }
            if(State==ActorState.Dodge&&GameTime.Now<actionUntil)
            {
                if(!flashConsumed&&GameTime.Now-dodgeAt<=Mathf.Clamp(.2f+Mathf.Max(0,response-10)*.001f,.2f,.27f))
                {
                    flashConsumed=true;FlashMoves++;Health.InvulnerableUntil=GameTime.Now+1.5f;
                    StartCoroutine(FlashTime());Debug.Log("FLASH_MOVE_OK");
                    AudioManager.Play("forceField_000",.3f);BarkService.Play(character,"flash",true);
                    if(flight){critical=true;charge=Mathf.Min(100,charge+25);}
                }
                return true;
            }
            if(guarding)
            {
                if(GameTime.Now-guardAt<=.2f){FlashGuards++;critical=true;charge=Mathf.Min(100,charge+25);guardAt=-99;Debug.Log("FLASH_GUARD_OK");return true;}
            }
            return false;
        }
        public static void ResetTime()=>GameTime.Reset();
        IEnumerator FlashTime()
        {
            GameTime.BeginFlash();
            float remaining=1.5f;
            while(remaining>0){if(!GameTime.Paused)remaining-=Time.unscaledDeltaTime;yield return null;}
            GameTime.EndFlash();
        }
        public DamagePacket Packet(float amount,DamageType type,float breakPower,string tag=null)
        {
            bool crit=critical||UnityEngine.Random.value<Mathf.Clamp(.04f+Mathf.Max(0,response-10)*.003f,.04f,.25f);
            var p=new DamagePacket{source=Health,amount=amount*damageScale*(GameTime.Now<overdriveUntil?1.2f:1)*(crit?1+Mathf.Max(0,fortune)*.01f:1),type=type,breakPower=breakPower,isCrit=crit,tag=tag};
            critical=false;return p;
        }
        Vector3 Aim()
        {
            if(target!=null&&target.Alive){var d=target.transform.position-transform.position;d.y=0;if(Vector3.Angle(motor.Facing,d)<(flight?30:120))return d.normalized;}
            return motor.Facing;
        }
        public bool Attack()
        {
            if(!CanAct||GameTime.Now<nextAttack)return false;
            State=ActorState.Attack;float delay=flight?.17f:.29f;nextAttack=GameTime.Now+delay;actionUntil=GameTime.Now+.08f;
            combo=(combo+1)%3;Vector3 aim=Aim();
            AudioManager.Play(flight||character=="Sela"?"laserSmall_000":"impactMetal_000",.18f);
            if(flight||character=="Sela")Projectile.Fire(transform.position+Vector3.up*.9f+aim*.6f,aim,Packet(rangedDamage*(flight?.38f:.7f),emitterType,12));
            else Strike(transform.position+Vector3.up*.8f+aim*1.2f,1.65f,Packet(damage*(combo==0?1.25f:1),edgeType,20));
            return true;
        }
        public bool Lunge()
        {
            if(!CanAct||GameTime.Now<nextAttack)return false;
            State=ActorState.Attack;nextAttack=GameTime.Now+.7f;actionUntil=GameTime.Now+.2f;
            var direction=target!=null&&target.Alive?(target.transform.position-transform.position).normalized:motor.Facing;
            direction.y=0;StartCoroutine(LungePath(direction));return true;
        }
        IEnumerator LungePath(Vector3 direction)
        {
            Vector3 start=transform.position;motor.Dash(direction,8);
            var victims=new System.Collections.Generic.HashSet<Health>();var packet=Packet(damage,edgeType,28,"lunge");
            for(int i=0;i<5;i++){var at=start+direction*(i*1.7f)+Vector3.up*.8f;var hit=Strike(at,1.05f,packet);hit.hit=victims;CombatVfx.Burst(at,character=="Taren"?new Color(1,.65f,.2f):Color.cyan,"lunge");yield return null;}
        }
        public bool Skill(int slot)
        {
            if(!CanAct||slot<0||slot>3||cooldowns[slot]>0)return false;
            var definition=GameCatalog.Find<CharacterDef>(character);var skill=definition!=null&&definition.skills.Length>slot?definition.skills[slot]:null;
            float cost=skill!=null?skill.chargeCost:20;if(charge<cost)return false;
            charge-=cost;cooldowns[slot]=skill!=null?skill.cooldown:slot==3?10:4;State=ActorState.Skill;actionUntil=GameTime.Now+.16f;
            float power=1+Mathf.Max(0,resonance-10)*.025f;Vector3 direction=Aim();
            if(character=="Taren")
            {
                if(slot==0)Strike(transform.position+direction*2+Vector3.up*.8f,3.4f,Packet(damage*1.7f*power,DamageType.Kinetic,35));
                else if(slot==1){motor.Dash(direction,flight?10:6);Pierce(direction,Packet(damage*1.8f*power,DamageType.Plasma,40),flight?10:6);}
                else if(slot==2)Strike(transform.position+Vector3.up,5,Packet(damage*1.1f*power,DamageType.Pulse,85));
                else overdriveUntil=GameTime.Now+10;
            }
            else
            {
                if(slot==0)Pierce(direction,Packet(rangedDamage*1.7f*power,DamageType.Beam,40),flight?18:13);
                else if(slot==1)for(int i=-2;i<=2;i++)Projectile.Fire(transform.position+Vector3.up*.9f+direction*.6f,Quaternion.Euler(0,i*12,0)*direction,Packet(rangedDamage*.65f*power,DamageType.Plasma,18));
                else if(slot==2)
                {
                    var field=new GameObject("StaticNet",typeof(PulseField)).GetComponent<PulseField>();field.transform.position=target!=null?target.transform.position:transform.position+direction*4;
                    field.packet=Packet(rangedDamage*.45f*power,DamageType.Pulse,30);
                }
                else refractUntil=GameTime.Now+1.4f;
            }
            Debug.Log($"SKILL_OK {character} slot={slot+1}");return true;
        }
        void Pierce(Vector3 direction,DamagePacket packet,float length)
        {
            var victims=new System.Collections.Generic.HashSet<Health>();
            for(float distance=1;distance<=length;distance+=1.5f)
            {var hit=Strike(transform.position+Vector3.up*.9f+direction*distance,.9f,packet);hit.hit=victims;}
        }
        public static HitVolume Strike(Vector3 position,float radius,DamagePacket packet)
        {
            var go=new GameObject("HitVolume",typeof(HitVolume));go.transform.position=position;
            var v=go.GetComponent<HitVolume>();v.radius=radius;v.packet=packet;
            return v;
        }
        public void AwardHit(){charge=Mathf.Min(100,charge+7*(1+Mathf.Max(0,resonance-10)*.025f));}
        public void AwardKill(){Kills++;BarkService.Play(character,"kill");AudioManager.Play("explosionCrunch_000",.22f);}
    }
}
