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
        /// <summary>When each slot last refused a press for Charge or cooldown, for the HUD's flash.</summary>
        public readonly float[] SkillRefusedAt=new float[4];
        /// <summary>Where the player is steering, for skills that take a direction (Waypoint Blink).</summary>
        public Vector3 MoveIntent;
        /// <summary>Remaining cooldowns of skills out of their slots, as when each comes ready.</summary>
        readonly System.Collections.Generic.Dictionary<string,float> benchedReady=new();
        float nextDodge;
        /// <summary>A beat between dodges: back-to-back rolls were near-total immunity at no cost.</summary>
        public const float DodgeGap=.1f;
        /// <summary>When the last perfect guard showed itself.</summary>
        public float FlashGuardShownAt{get;private set;}
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
        void Awake(){Health=GetComponent<Health>();Health.Died+=OnDown;Health.Revived+=OnRevive;SkillBook.LoadoutChanged+=OnLoadout;}
        void OnDestroy(){Health.Died-=OnDown;Health.Revived-=OnRevive;SkillBook.LoadoutChanged-=OnLoadout;}
        /// <summary>A cooldown belongs to the skill, not its slot: a trip to the menu used to reset a skill's timer and hand the
        /// other skill one it never earned.</summary>
        void OnLoadout(string who,System.Collections.Generic.IReadOnlyList<string> before,System.Collections.Generic.IReadOnlyList<string> after)
        {
            if(who!=character)return;
            for(int i=0;i<before.Count&&i<4;i++)if(before[i]!=null)benchedReady[before[i]]=GameTime.Now+cooldowns[i];
            for(int i=0;i<after.Count&&i<4;i++)cooldowns[i]=after[i]!=null&&benchedReady.TryGetValue(after[i],out float ready)?Mathf.Max(0,ready-GameTime.Now):0;
        }
        void CancelAction()
        {AttackSequence++;VisualAttackUntil=0;guarding=false;refractUntil=0;critical=false;combo=0;}
        internal void BeginFormChange(){CancelAction();Halt();if(Health.Alive&&!Recovering)State=ActorState.Idle;}
        void Halt()
        {if(TryGetComponent<GroundMotor>(out var ground))ground.Halt();if(TryGetComponent<FlightMotor>(out var ship))ship.Halt();}
        void OnDown(Health _,DamagePacket __)
        {CancelAction();State=ActorState.Down;TargetLocked=false;target=null;recoverUntil=0;Halt();CombatAudio.HeroDown(this);}
        void OnRevive(Health _)
        {
            CombatAudio.HeroRevive(this);
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
            if(GameTime.Paused||!Health.Alive||Recovering||ChangingForm||State==ActorState.Dodge||State==ActorState.Down||State==ActorState.Stagger||GameTime.Now<nextDodge)return false;
            refractUntil=0;State=ActorState.Dodge;dodgeAt=GameTime.Now;flashConsumed=false;nextDodge=GameTime.Now+(flight?.3f:.25f)+DodgeGap;
            BeginVisual("Dodge",flight?.3f:.25f);
            CombatAudio.Dodge(this);
            actionUntil=GameTime.Now+(flight?.3f:.25f);motor.Dash(direction.sqrMagnitude>.01f?direction:motor.Facing,flight?4:3.3f);
            return true;
        }
        public bool Deflect(DamagePacket packet)
        {
            if(GameTime.Now<refractUntil)
            {
                refractUntil=0;critical=true;CombatAudio.Deflect(this);
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
                    // Slowing the world is the player's reward: the AI partner dodges by design, and it used to put the
                    // player's own hero in slow motion, unearned, in every marked fight.
                    bool controlled=PartyController.Current==null||PartyController.Current.Active==this;
                    if(controlled){StartCoroutine(FlashTime());AudioManager.Play("forceField_000",.3f);}
                    Debug.Log("FLASH_MOVE_OK");BarkService.Play(character,"flash");
                    if(flight){critical=true;charge=Mathf.Min(100,charge+25);}
                }
                return true;
            }
            if(guarding)
            {
                if(GameTime.Now-guardAt<=.2f)
                {
                    FlashGuards++;critical=true;charge=Mathf.Min(100,charge+25);guardAt=-99;Debug.Log("FLASH_GUARD_OK");
                    // A perfect guard has to show itself: it used to be silent and invisible but for the Charge bar.
                    FlashGuardShownAt=GameTime.Now;CombatAudio.Deflect(this);
                    CombatVfx.Burst(transform.position+Vector3.up*1.2f+motor.Facing*.5f,new Color(1.5f,1.2f,.5f),"hit");
                    BarkService.Play(character,"flash");
                    return true;
                }
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
        /// <summary>Single-target burst skills (Arc Cleave, Ember Dash, Lance) in multiples of the actor's output. At 1.7-1.8
        /// a skill cost three landed hits of charge and a cooldown for less than two hits of damage, so skills barely
        /// mattered; an Ys-style burst should be worth spending charge on (November 4, D194).</summary>
        public const float SkillBurst=2.6f;
        public bool Skill(int slot)
        {
            if(slot<0||slot>3)return false;
            var skill=SkillBook.Equipped(character,slot);if(skill==null)return false;
            int mastery=SkillBook.Mastery(character,skill.id);
            float cost=SkillBook.Cost(skill,mastery);
            if(!CanAct)return false;
            if(cooldowns[slot]>0||charge<cost){SkillRefusedAt[slot]=GameTime.Now;return false;}
            charge-=cost;cooldowns[slot]=skill.cooldown;State=ActorState.Skill;
            float power=(1+Mathf.Max(0,resonance-10)*.025f)*SkillBook.Power(mastery);Vector3 direction=Aim();
            var kind=skill.kind;
            if(flight)
            {
                if(Learned(kind))GetComponent<HeroSkills>().Flight(kind,power,direction);
                else
                {
                    BeginVisual(kind==SkillKind.ArcCleave?"Cleave":kind==SkillKind.ThreadLance?"Shoot":Slot(kind)==1?"Dash":Slot(kind)==2?"Pulse":"Buff",Slot(kind)==0?.5f:Slot(kind)==1?.4f:.55f);
                    actionUntil=GameTime.Now+(Slot(kind)==2?.3f:Slot(kind)==3?.2f:kind==SkillKind.ArcCleave?.24f:.16f);ApplySkill(kind,power,direction);
                }
            }
            else if(Learned(kind))GetComponent<HeroSkills>().Ground(kind,power,direction);
            else
            {
                var move=kind switch{SkillKind.ArcCleave=>GroundMove.Cleave,SkillKind.EmberStep=>GroundMove.EmberDash,SkillKind.PulseBurst=>GroundMove.Pulse,
                    SkillKind.Overdrive=>GroundMove.Overdrive,SkillKind.ThreadLance=>GroundMove.Lance,SkillKind.ScatterBloom=>GroundMove.Scatter,
                    SkillKind.StaticNet=>GroundMove.StaticNet,_=>GroundMove.Refract};
                BeginVisual(move.clip,move.duration);actionUntil=GameTime.Now+move.recoveryEnd;
                if(kind==SkillKind.ArcCleave)
                    GetComponent<MeleeContact>().Begin(move,Packet(damage*SkillBurst*power,DamageType.Kinetic,35));
                else if(kind==SkillKind.ThreadLance||kind==SkillKind.ScatterBloom)
                    GetComponent<RangedContact>().Begin(move,direction,
                        Packet(rangedDamage*(kind==SkillKind.ThreadLance?SkillBurst:.65f)*power,kind==SkillKind.ThreadLance?DamageType.Beam:DamageType.Plasma,kind==SkillKind.ThreadLance?40:18),
                        kind==SkillKind.ThreadLance?RangedContact.Pattern.Lance:RangedContact.Pattern.Fan);
                else
                {
                    var release=kind==SkillKind.Overdrive?GroundSkillContact.Release.Overdrive:kind==SkillKind.Refract?GroundSkillContact.Release.Refract:
                        kind==SkillKind.StaticNet?GroundSkillContact.Release.Net:kind==SkillKind.EmberStep?GroundSkillContact.Release.Dash:GroundSkillContact.Release.Pulse;
                    var packet=kind==SkillKind.Overdrive||kind==SkillKind.Refract?default:
                        kind==SkillKind.EmberStep?Packet(damage*SkillBurst*power,DamageType.Plasma,40):
                        kind==SkillKind.PulseBurst?Packet(damage*1.1f*power,DamageType.Pulse,85):
                        Packet(rangedDamage*.45f*power,DamageType.Pulse,30);
                    GetComponent<GroundSkillContact>().Begin(move,release,direction,packet);
                    if(kind==SkillKind.EmberStep)GetComponent<MeleeContact>().Begin(move,packet);
                }
            }
            SkillBook.RecordUse(character,skill);
            Debug.Log($"SKILL_OK {character} slot={slot+1}");Debug.Log($"SKILL_KIND {character} {skill.id} {kind} mastery={mastery}");return true;
        }
        /// <summary>Skills learned with level (D201) run on <see cref="HeroSkills"/>; the starting eight keep their contacts.</summary>
        public static bool Learned(SkillKind kind)=>kind>=SkillKind.AnchorSlam;
        /// <summary>The starting skill's original slot, which still decides its flight pose and recovery.</summary>
        static int Slot(SkillKind kind)=>kind switch{SkillKind.ArcCleave or SkillKind.ThreadLance=>0,SkillKind.EmberStep or SkillKind.ScatterBloom=>1,SkillKind.PulseBurst or SkillKind.StaticNet=>2,_=>3};
        /// <summary>Used by <see cref="HeroSkills"/> for its own poses.</summary>
        internal void Perform(string clip,float duration,float recovery){BeginVisual(clip,duration);actionUntil=GameTime.Now+recovery;}
        void ApplySkill(SkillKind kind,float power,Vector3 direction)
        {
            if(kind==SkillKind.ArcCleave)GetComponent<FlightSkillContact>().Begin(FlightSkillContact.Release.Cleave,direction,Packet(damage*SkillBurst*power,DamageType.Kinetic,35));
            else if(kind==SkillKind.EmberStep)
            {
                motor.Dash(direction,flight?10:6);var packet=Packet(damage*SkillBurst*power,DamageType.Plasma,40);
                if(flight)GetComponent<FlightDashContact>().Begin(packet);else Pierce(direction,packet,6);
            }
            else if(kind==SkillKind.PulseBurst)GetComponent<FlightSkillContact>().Begin(FlightSkillContact.Release.Pulse,direction,Packet(damage*1.1f*power,DamageType.Pulse,85));
            else if(kind==SkillKind.Overdrive)GetComponent<FlightSkillContact>().Begin(FlightSkillContact.Release.Overdrive,direction,default);
            else if(kind==SkillKind.ThreadLance)GetComponent<FlightEmitter>().Queue(direction,Packet(rangedDamage*SkillBurst*power,DamageType.Beam,40),FlightEmitter.Pattern.Beam);
            else if(kind==SkillKind.ScatterBloom)
            {
                var pellets=new DamagePacket[5];for(int i=0;i<pellets.Length;i++)pellets[i]=Packet(rangedDamage*.65f*power,DamageType.Plasma,18);
                GetComponent<FlightEmitter>().Queue(direction,pellets[0],FlightEmitter.Pattern.Fan,pellets);
            }
            else if(kind==SkillKind.StaticNet)GetComponent<FlightSkillContact>().Begin(FlightSkillContact.Release.Net,direction,Packet(rangedDamage*.45f*power,DamageType.Pulse,30));
            else GetComponent<FlightSkillContact>().Begin(FlightSkillContact.Release.Refract,direction,default);
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
        /// <summary>Charge for a landed hit, by how much of a full hit it was: a flat award let a drone's needles and a field's
        /// ticks repay their skill several times over, so Charge never limited anything.</summary>
        public void AwardHit(float weight=1){charge=Mathf.Min(100,charge+7*Mathf.Clamp01(weight)*(1+Mathf.Max(0,resonance-10)*.025f));}
        // The defeated body now voices its own death at its position.
        public void AwardKill(bool remark=true){Kills++;if(remark)BarkService.Play(character,"kill");}
    }
}
