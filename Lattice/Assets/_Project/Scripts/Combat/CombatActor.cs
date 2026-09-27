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
        // Assisted attack selection and deliberate target-facing locomotion
        // are separate: a nearby enemy alone must never turn free running.
        public bool TargetLocked;
        public IMotor motor;
        public int combo;
        public int Kills{get;private set;}
        public int FlashMoves{get;private set;}
        public int FlashGuards{get;private set;}
        public int AttackSequence{get;private set;}
        public float VisualAttackUntil{get;private set;}
        public string VisualAction{get;private set;}="Idle";
        public float VisualDuration{get;private set;}
        public readonly float[] cooldowns=new float[4];
        float nextAttack,actionUntil,dodgeAt=-99,guardAt=-99,overdriveUntil,refractUntil,lastComboAt=-99;
        bool guarding,critical,flashConsumed;
        float recoverUntil;
        public const float DownDuration=1.05f,ReviveDuration=1.15f;
        public bool Overdriving=>Health.Alive&&GameTime.Now<overdriveUntil;
        public bool Refracting=>Health.Alive&&GameTime.Now<refractUntil;
        internal void ArmOverdrive()=>overdriveUntil=GameTime.Now+10;
        internal void ArmRefract()=>refractUntil=GameTime.Now+1.4f;
        public bool Recovering=>Health.Alive&&GameTime.Now<recoverUntil;
        public float GuardDamageMultiplier=>guarding?.35f:1;
        public bool ChangingForm=>TryGetComponent<FormController>(out var form)&&form.Shaping;
        public bool CanAct=>!GameTime.Paused&&Health.Alive&&!Recovering&&!ChangingForm&&State!=ActorState.Stagger&&GameTime.Now>=actionUntil;
        public float MotorDelta=>GameTime.Paused?0:GameTime.Now<Health.InvulnerableUntil?Time.unscaledDeltaTime:Time.deltaTime;
        void Awake(){Health=GetComponent<Health>();Health.Died+=OnDown;Health.Revived+=OnRevive;}
        void OnDestroy(){Health.Died-=OnDown;Health.Revived-=OnRevive;}
        void CancelAction()
        {AttackSequence++;VisualAttackUntil=0;guarding=false;refractUntil=0;critical=false;combo=0;}
        internal void BeginFormChange(){CancelAction();Halt();if(Health.Alive&&!Recovering)State=ActorState.Idle;}
        void Halt()
        {if(TryGetComponent<GroundMotor>(out var ground))ground.Halt();if(TryGetComponent<FlightMotor>(out var ship))ship.Halt();}
        void OnDown(Health _,DamagePacket __)
        {CancelAction();State=ActorState.Down;TargetLocked=false;target=null;recoverUntil=0;Halt();}
        void OnRevive(Health _)
        {
            CancelAction();recoverUntil=GameTime.Now+ReviveDuration;actionUntil=nextAttack=recoverUntil;
            Health.InvulnerableUntil=Mathf.Max(Health.InvulnerableUntil,recoverUntil+.35f);State=ActorState.Down;Halt();
        }
        void Update()
        {
            if(!Health.Alive||Recovering)return;
            if(GameTime.Now>=actionUntil&&State!=ActorState.Guard)State=motor!=null&&motor.Velocity.sqrMagnitude>.1f?ActorState.Move:ActorState.Idle;
            charge=Mathf.Max(0,charge-Time.deltaTime*.4f);thrust=Mathf.Min(100,thrust+Time.deltaTime*17);
            for(int i=0;i<4;i++)cooldowns[i]=Mathf.Max(0,cooldowns[i]-Time.deltaTime);
        }
        public void Guard(bool held)
        {
            if(!Health.Alive||Recovering||ChangingForm){guarding=false;return;}
            if(held&&!guarding){refractUntil=0;guardAt=GameTime.Now;VisualAttackUntil=0;AttackSequence++;}
            guarding=held&&!flight; if(guarding)State=ActorState.Guard;else if(State==ActorState.Guard)State=ActorState.Idle;
        }
        public void Stagger(float seconds){if(!Health.Alive||Recovering)return;CancelAction();State=ActorState.Stagger;actionUntil=GameTime.Now+seconds;}
        public bool Dodge(Vector3 direction)
        {
            if(GameTime.Paused||!Health.Alive||Recovering||ChangingForm||State==ActorState.Dodge||State==ActorState.Down||State==ActorState.Stagger)return false;
            refractUntil=0;State=ActorState.Dodge;dodgeAt=GameTime.Now;flashConsumed=false;
            BeginVisual("Dodge",flight?.3f:.25f);
            AudioManager.Play("thrusterFire_000",.16f);
            actionUntil=GameTime.Now+(flight?.3f:.25f);motor.Dash(direction.sqrMagnitude>.01f?direction:motor.Facing,flight?4:3.3f);
            return true;
        }
        public bool Deflect(DamagePacket packet)
        {
            if(GameTime.Now<refractUntil)
            {
                refractUntil=0;critical=true;
                var direction=Aim();var counter=Packet(rangedDamage*2,DamageType.Beam,60);
                if(flight)
                {
                    State=ActorState.Attack;BeginVisual("Shoot",.22f);actionUntil=GameTime.Now+.08f;
                    GetComponent<FlightEmitter>().Queue(direction,counter,FlightEmitter.Pattern.Counter);
                }
                else
                {
                    var move=GroundMove.Counter;State=ActorState.Skill;guarding=false;
                    BeginVisual(move.clip,move.duration);actionUntil=GameTime.Now+move.recoveryEnd;
                    GetComponent<RangedContact>().Begin(move,direction,counter,RangedContact.Pattern.Counter);
                    GetComponent<HeroBuffPresentation>().Intercept();
                }
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
            if(GameTime.Now-lastComboAt>.9f)combo=0;int stage=combo;combo=(combo+1)%3;lastComboAt=GameTime.Now;
            var cut=GroundMove.Cut(stage);
            State=ActorState.Attack;float duration=flight?.17f:character=="Sela"?GroundMove.Needle.duration:cut.duration;
            nextAttack=GameTime.Now+duration;actionUntil=GameTime.Now+(flight?.08f:character=="Taren"?cut.recoveryEnd:GroundMove.Needle.recoveryEnd);
            Vector3 aim=Aim();BeginVisual(flight||character=="Sela"?"Shoot":"Attack"+(stage+1),duration);
            if(flight)
            {
                GetComponent<FlightEmitter>().Queue(aim,Packet(rangedDamage*.38f,emitterType,12));
            }
            else if(character=="Taren")GetComponent<MeleeContact>().Begin(cut,Packet(damage*(stage==2?1.5f:1),edgeType,stage==2?30:20));
            else GetComponent<RangedContact>().Begin(GroundMove.Needle,aim,Packet(rangedDamage*.7f,emitterType,12));
            return true;
        }
        void BeginVisual(string action,float duration)
        {
            AttackSequence++;VisualAction=action;VisualDuration=duration;VisualAttackUntil=GameTime.Now+duration;
        }
        public bool Lunge()
        {
            if(!CanAct||GameTime.Now<nextAttack)return false;
            State=ActorState.Attack;nextAttack=GameTime.Now+.7f;actionUntil=GameTime.Now+.2f;
            BeginVisual("Dash",.42f);
            var direction=target!=null&&target.Alive?(target.transform.position-transform.position).normalized:motor.Facing;
            direction.y=0;motor.Dash(direction,8);
            GetComponent<FlightDashContact>().Begin(Packet(damage,edgeType,28,"lunge"));return true;
        }
        public bool Skill(int slot)
        {
            if(!CanAct||slot<0||slot>3||cooldowns[slot]>0)return false;
            var definition=GameCatalog.Find<CharacterDef>(character);var skill=definition!=null&&definition.skills.Length>slot?definition.skills[slot]:null;
            float cost=skill!=null?skill.chargeCost:20;if(charge<cost)return false;
            charge-=cost;cooldowns[slot]=skill!=null?skill.cooldown:slot==3?10:4;State=ActorState.Skill;
            float power=1+Mathf.Max(0,resonance-10)*.025f;Vector3 direction=Aim();
            if(flight)
            {
                BeginVisual(slot==0?(character=="Taren"?"Cleave":"Shoot"):slot==1?"Dash":slot==2?"Pulse":"Buff",slot==0?.5f:slot==1?.4f:.55f);
                actionUntil=GameTime.Now+(slot==2?.3f:slot==3?.2f:character=="Taren"&&slot==0?.24f:.16f);ApplySkill(slot,power,direction);
            }
            else
            {
                var move=character=="Taren"?slot==0?GroundMove.Cleave:slot==1?GroundMove.EmberDash:slot==2?GroundMove.Pulse:GroundMove.Overdrive:
                    slot==0?GroundMove.Lance:slot==1?GroundMove.Scatter:slot==2?GroundMove.StaticNet:GroundMove.Refract;
                BeginVisual(move.clip,move.duration);actionUntil=GameTime.Now+move.recoveryEnd;
                if(character=="Taren"&&slot==0)
                    GetComponent<MeleeContact>().Begin(move,Packet(damage*1.7f*power,DamageType.Kinetic,35));
                else if(character=="Sela"&&slot<2)
                    GetComponent<RangedContact>().Begin(move,direction,
                        Packet(rangedDamage*(slot==0?1.7f:.65f)*power,slot==0?DamageType.Beam:DamageType.Plasma,slot==0?40:18),
                        slot==0?RangedContact.Pattern.Lance:RangedContact.Pattern.Fan);
                else
                {
                    var release=slot==3?(character=="Taren"?GroundSkillContact.Release.Overdrive:GroundSkillContact.Release.Refract):
                        character=="Sela"?GroundSkillContact.Release.Net:slot==1?GroundSkillContact.Release.Dash:GroundSkillContact.Release.Pulse;
                    var packet=slot==3?default:character=="Taren"?Packet(damage*(slot==1?1.8f:1.1f)*power,slot==1?DamageType.Plasma:DamageType.Pulse,slot==1?40:85):
                        Packet(rangedDamage*.45f*power,DamageType.Pulse,30);
                    GetComponent<GroundSkillContact>().Begin(move,release,direction,packet);
                    if(character=="Taren"&&slot==1)GetComponent<MeleeContact>().Begin(move,packet);
                }
            }
            Debug.Log($"SKILL_OK {character} slot={slot+1}");return true;
        }
        void ApplySkill(int slot,float power,Vector3 direction)
        {
            if(character=="Taren")
            {
                if(slot==0)GetComponent<FlightSkillContact>().Begin(FlightSkillContact.Release.Cleave,direction,Packet(damage*1.7f*power,DamageType.Kinetic,35));
                else if(slot==1)
                {
                    motor.Dash(direction,flight?10:6);var packet=Packet(damage*1.8f*power,DamageType.Plasma,40);
                    if(flight)GetComponent<FlightDashContact>().Begin(packet);else Pierce(direction,packet,6);
                }
                else if(slot==2)GetComponent<FlightSkillContact>().Begin(FlightSkillContact.Release.Pulse,direction,Packet(damage*1.1f*power,DamageType.Pulse,85));
                else GetComponent<FlightSkillContact>().Begin(FlightSkillContact.Release.Overdrive,direction,default);
            }
            else
            {
                if(slot==0)GetComponent<FlightEmitter>().Queue(direction,Packet(rangedDamage*1.7f*power,DamageType.Beam,40),FlightEmitter.Pattern.Beam);
                else if(slot==1)
                {
                    var pellets=new DamagePacket[5];for(int i=0;i<pellets.Length;i++)pellets[i]=Packet(rangedDamage*.65f*power,DamageType.Plasma,18);
                    GetComponent<FlightEmitter>().Queue(direction,pellets[0],FlightEmitter.Pattern.Fan,pellets);
                }
                else if(slot==2)
                {
                    GetComponent<FlightSkillContact>().Begin(FlightSkillContact.Release.Net,direction,Packet(rangedDamage*.45f*power,DamageType.Pulse,30));
                }
                else GetComponent<FlightSkillContact>().Begin(FlightSkillContact.Release.Refract,direction,default);
            }
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
            var v=go.GetComponent<HitVolume>();v.radius=radius;v.packet=packet;v.BindOwner();
            return v;
        }
        public void AwardHit(){charge=Mathf.Min(100,charge+7*(1+Mathf.Max(0,resonance-10)*.025f));}
        public void AwardKill(){Kills++;BarkService.Play(character,"kill");AudioManager.Play("explosionCrunch_000",.22f);}
    }
}
