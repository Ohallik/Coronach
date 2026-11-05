using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Boss fights (D198). Each boss has a readable move set that grows with its phases. Every special marks the exact
    /// ground it will strike (<see cref="Telegraph"/>) and lands only inside that marking; the heaviest leave an opening
    /// to punish (a charge driven into rock, an eruption, a collapsed breath). Crossing a phase threshold mid-fight plays
    /// a short shift: the boss braces, cannot be hurt for a moment, shoves the party back and brings in what the next
    /// phase adds. <see cref="Telegraphing"/> and <see cref="TelegraphRemaining"/> describe the next hit that threatens
    /// the active hero, which the companion and the replay driver read as a visible tell. Per-boss move sets live in the
    /// partial files beside this one.
    /// </summary>
    public sealed partial class BossController:MonoBehaviour
    {
        public static readonly List<BossController> All=new();
        public static event System.Action<BossController,string> MoveStarted;
        public int Phase{get;private set;}=1;
        /// <summary>The phase from integrity alone; collar cuts can raise <see cref="Phase"/> without a shift.</summary>
        public int HealthPhase{get;private set;}=1;
        Health health;
        EnemyBrain brain;
        CantorCollar collar;int cantorSpecials;
        float nextSpecial,strikeAt,engaged=-1;bool busy;
        Transform inflated,body;Vector3 inflatedRest,bodyRest;int breaths;
        Transform visual;Vector3 visualRest;Quaternion visualRestRotation=Quaternion.identity;
        bool shiftPending,shifting;string lastMove;int moveCount;
        readonly List<GameObject> temporary=new();
        readonly List<EnemyBrain> summoned=new();
        public bool Busy=>busy;
        public bool Telegraphing=>busy&&Time.time<strikeAt;
        public float TelegraphRemaining=>Mathf.Max(0,strikeAt-Time.time);
        public bool Shifting=>shifting;
        public string CurrentMove{get;private set;}="";
        public int MovesStarted=>moveCount;
        public Health Health=>health;
        public bool Engaged=>engaged>=0;
        /// <summary>Alive, awake and in the fight: the boss bar shows while the party is near.</summary>
        public bool Fighting=>isActiveAndEnabled&&health!=null&&health.Alive&&brain!=null&&brain.enabled&&!brain.Passive;
        public string DisplayName=>health==null?"":health.id switch{"Burrower"=>"THE BURROWER","BellowsBelow"=>"THE BELLOWS BELOW","Cantor"=>"THE CANTOR","HessaKorl"=>"HESSA KORL",var id=>id.ToUpperInvariant()};
        public string Epithet=>health==null?"":health.id switch
        {
            "Burrower"=>"Sorrel's drill rig, chasing an evacuation signal",
            "BellowsBelow"=>"The breath that guards Hushwell's nursery",
            "Cantor"=>"A collared route-singer of the Gullet",
            "HessaKorl"=>"Muster scavenger captain, stripping the relay's glass",
            _=>""
        };
        /// <summary>Integrity fractions at which the next phase begins (boss bar notches).</summary>
        public float[] Thresholds=>health==null?System.Array.Empty<float>():health.id switch
        {
            "Burrower"=>new[]{.6f,.3f},"BellowsBelow"=>new[]{.5f,.3f},"Cantor"=>new[]{.66f,.33f},"HessaKorl"=>new[]{.66f,.33f},_=>new[]{.5f}
        };

        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable(){All.Remove(this);}
        /// <summary>A boss fight is under way around the party: an awake, engaged boss within 45 m of the active hero. Travel
        /// waits for it; a party that has retreated out of the fight can still leave.</summary>
        public static bool AnyFightUnderway
        {
            get
            {
                var party=PartyController.Current;if(party==null||party.Active==null)return false;
                foreach(var boss in All)
                {
                    if(boss==null||!boss.Fighting||!boss.Engaged)continue;
                    var offset=boss.transform.position-party.Active.transform.position;offset.y=0;
                    if(offset.sqrMagnitude<45*45)return true;
                }
                return false;
            }
        }
        /// <summary>The fighting boss nearest a point, within range.</summary>
        public static BossController Focus(Vector3 from,float range=34)
        {
            BossController best=null;float score=range*range;
            foreach(var boss in All)
            {
                if(boss==null||!boss.Fighting)continue;
                var offset=boss.transform.position-from;offset.y=0;
                if(offset.sqrMagnitude<score){score=offset.sqrMagnitude;best=boss;}
            }
            return best;
        }

        void Start()
        {
            health=GetComponent<Health>();brain=GetComponent<EnemyBrain>();
            collar=GetComponent<CantorCollar>();
            // Dormant previews delay Start until activation. The combat cue
            // then lasts until resolution or removal.
            MusicDirector.Encounter(this,"Alien Boss Battle",true);
            nextSpecial=Time.time+5;health.Damaged+=OnDamaged;
            var presentation=GetComponent<DefeatPresentation>();
            visual=presentation!=null?presentation.visual:null;if(visual!=null){visualRest=visual.localPosition;visualRestRotation=visual.localRotation;}
            // The Bellows has no rig: its breathing is its animation.
            if(health.id=="BellowsBelow"){body=visual;if(body!=null)bodyRest=body.localScale;}
            for(int i=0;i<(collar!=null?0:3);i++)
            {
                var weak=new GameObject("WeakPoint_"+i,typeof(SphereCollider),typeof(Hurtbox));weak.transform.SetParent(transform,false);
                weak.transform.localPosition=new Vector3((i-1)*1.2f,1.4f,0);weak.GetComponent<SphereCollider>().radius=.5f;weak.GetComponent<SphereCollider>().isTrigger=true;
                weak.GetComponent<Hurtbox>().owner=health;weak.GetComponent<Hurtbox>().multiplier=2;
            }
            health.Died+=(_,__)=>
            {
                StopAllCoroutines();Interrupt();busy=false;enabled=false;MusicDirector.Encounter(this,null,false);
                DismissSummons();
                GameServices.Current.Flags.SetBool("bossdown."+health.id,true);
                if(health.id=="BellowsBelow")PressureOrgan.SettleAll();
                BarkService.Play(PartyController.Current.Active.character,health.id=="Cantor"?"release":"boss",true);
                Debug.Log("BOSS_DOWN "+health.id);Debug.Log($"BOSS_DURATION {health.id} seconds={Time.realtimeSinceStartup-engaged:0.0}");
            };
        }
        void OnDamaged(Health _,DamagePacket packet,float amount)
        {
            if(engaged<0&&amount>0)engaged=Time.realtimeSinceStartup;
            if(!health.Broken)return;
            StopAllCoroutines();Interrupt();busy=false;strikeAt=0;
            nextSpecial=Mathf.Max(nextSpecial,health.BrokenUntil+.5f);
        }
        void OnDestroy(){if(health!=null)health.Damaged-=OnDamaged;MusicDirector.Encounter(this,null,false);Telegraph.CancelAll(this);ClearTemporary();}
        public void RestoreAfterRecovery()
        {
            StopAllCoroutines();Interrupt();busy=false;strikeAt=0;engaged=-1;
            Phase=1;HealthPhase=1;cantorSpecials=0;moveCount=0;lastMove=null;shiftPending=false;brain.DamageScale=1;nextSpecial=Time.time+5;enabled=true;
            DismissSummons();
            MusicDirector.Encounter(this,"Alien Boss Battle",true);
        }
        /// <summary>Everything an interrupted special left behind: markings, thrown things, a sunken or swollen body.</summary>
        void Interrupt()
        {
            Collide(true);Telegraph.CancelAll(this);ClearTemporary();Deflate();RestoreBody();shifting=false;CurrentMove="";
        }
        void RestoreBody()
        {
            if(visual!=null&&health!=null&&health.id!="BellowsBelow"){visual.localPosition=visualRest;visual.localRotation=visualRestRotation;}
            if(health!=null)health.Damageable=true;
            var controller=GetComponent<CharacterController>();
            if(controller!=null&&health!=null&&health.Alive&&burrowed){controller.enabled=true;}
            burrowed=false;
        }
        readonly List<CharacterController> passing=new();
        /// <summary>A charging body passes through the party (its contact still strikes) instead of stalling against a hero.</summary>
        void Collide(bool solid)
        {
            var controller=GetComponent<CharacterController>();if(controller==null)return;
            if(!solid&&PartyController.Current!=null)
                foreach(var member in PartyController.Current.members)
                {var body=member.GetComponent<CharacterController>();if(body!=null&&!passing.Contains(body)){Physics.IgnoreCollision(controller,body,true);passing.Add(body);}}
            if(solid){foreach(var body in passing)if(body!=null)Physics.IgnoreCollision(controller,body,false);passing.Clear();}
        }
        void ClearTemporary(){foreach(var item in temporary)if(item!=null)Destroy(item);temporary.Clear();}
        void DismissSummons()
        {
            // Released adds leave quietly with the fight: no death blasts beside heroes who just won.
            foreach(var add in summoned)if(add!=null){CombatVfx.Burst(add.transform.position+Vector3.up*.5f,new Color(.6f,.6f,.6f),"failure");Destroy(add.gameObject);}
            summoned.Clear();
        }

        void Update()
        {
            if(GameTime.Paused||health==null||!health.Alive)return;
            float ratio=health.integrity/health.maximum;int healthPhase=1;var thresholds=Thresholds;
            for(int i=0;i<thresholds.Length;i++)if(ratio<thresholds[i])healthPhase=i+2;
            if(healthPhase>HealthPhase&&engaged>=0&&!brain.Passive)shiftPending=true;
            HealthPhase=healthPhase;
            int phase=healthPhase;
            if(collar!=null)phase=Mathf.Max(phase,Mathf.Min(3,4-collar.Remaining));
            if(phase!=Phase){Phase=phase;Debug.Log($"BOSS_PHASE {health.id} {Phase}");}
            brain.DamageScale=1+(Phase-1)*.18f;
            // The Bellows braces against the cave while any pressure organ pumps.
            if(health.id=="BellowsBelow"&&PressureOrgan.AnyPumping)health.ShieldUntil=Time.time+.25f;
            if(body!=null&&inflated==null)body.localScale=bodyRest*(1+.035f*Mathf.Sin(Time.time*2.1f));
            if(brain.Passive||busy||health.Broken||GameServices.Current.Input.Blocked||PartyController.Current==null)return;
            var hero=PartyController.Current.Active;
            if((hero.transform.position-transform.position).sqrMagnitude>900)return;
            if(shiftPending){shiftPending=false;Begin("PhaseShift",PhaseShift());return;}
            if(Time.time<nextSpecial)return;
            var (move,routine)=health.id switch
            {
                "Burrower"=>ChooseBurrower(hero),
                "BellowsBelow"=>ChooseBellows(hero),
                "Cantor"=>ChooseCantor(hero),
                "HessaKorl"=>ChooseHessa(hero),
                _=>("DrillCharge",DrillCharge(1))
            };
            if(routine!=null)Begin(move,routine);
        }
        /// <summary>Start a named special now, if the boss is free to (tests and review captures; the fight never calls it).</summary>
        public bool Perform(string move)
        {
            if(busy||health==null||!health.Alive||health.Broken)return false;
            IEnumerator routine=move switch
            {
                "DrillCharge"=>DrillCharge(Phase>=2?2:1),"SpoilToss"=>SpoilToss(),"TailSweep"=>TailSweep(),"Burrow"=>Burrow(),"SeismicRings"=>SeismicRings(),
                "Breath"=>BellowsBreath(),"RibStar"=>RibStar(),"Bubbles"=>Bubbles(),"Crossing"=>BellowsCrossing(),
                "Choir"=>ChoirAttack(),"SongSpiral"=>SongSpiral(),"CoilLash"=>CoilLash(),"PhaseShift"=>PhaseShift(),
                "HookLance"=>HookLance(Phase>=3?4:3),"GrappleChain"=>GrappleChain(),"Sidearm"=>Sidearm(),"SweepingCut"=>SweepingCut(),"Lampbreaker"=>Lampbreaker(),
                _=>null
            };
            if(routine==null)return false;
            Begin(move,routine);return true;
        }
        void Begin(string move,IEnumerator routine)
        {
            busy=true;strikeAt=0;CurrentMove=move;lastMove=move;moveCount++;
            Debug.Log($"BOSS_MOVE {health.id} {move} phase={Phase}");MoveStarted?.Invoke(this,move);
            StartCoroutine(routine);
        }
        void Finish(float cooldown){busy=false;strikeAt=0;CurrentMove="";nextSpecial=Time.time+cooldown;}

        // ---------- shared pieces ----------
        Telegraph Mark(Telegraph marking){marking.owner=this;return marking;}
        DamagePacket Packet(float amount,DamageType type)=>new DamagePacket{source=health,amount=amount*brain.DamageScale,type=type};
        static Vector3 Flat(Vector3 v){v.y=0;return v;}
        Vector3 Toward(Vector3 point){var d=Flat(point-transform.position);return d.sqrMagnitude>.0001f?d.normalized:transform.forward;}
        bool Interrupted=>!health.Alive||health.Broken;
        /// <summary>The marking lands: everyone inside it is struck once (a dodge still evades), outside it nobody.</summary>
        void Strike(Telegraph marking,float amount,DamageType type,float stagger=0,float shove=0)
        {
            marking.Release();
            foreach(var actor in PartyController.Current.members)
            {
                if(!actor.Health.Alive||!marking.Contains(actor.transform.position))continue;
                float dealt=actor.Health.Receive(Packet(amount,type));
                if(dealt<=0)continue;
                if(stagger>0)actor.Stagger(stagger);
                if(shove>0)StartCoroutine(Shove(actor,Flat(actor.transform.position-marking.Origin),shove));
            }
        }
        /// <summary>A moving body striking whoever it touches, once each.</summary>
        void Contact(HashSet<Health> hit,Vector3 centre,float radius,float amount,float stagger)
        {
            foreach(var actor in PartyController.Current.members)
            {
                if(!actor.Health.Alive||hit.Contains(actor.Health)||Flat(actor.transform.position-centre).sqrMagnitude>=radius*radius)continue;
                hit.Add(actor.Health);float dealt=actor.Health.Receive(Packet(amount,DamageType.Kinetic));
                if(dealt>0&&stagger>0)actor.Stagger(stagger);
            }
        }
        IEnumerator Shove(CombatActor actor,Vector3 direction,float distance)
        {
            if(direction.sqrMagnitude<.0001f)direction=-actor.transform.forward;direction.y=0;direction.Normalize();
            var controller=actor.GetComponent<CharacterController>();
            for(float t=0;t<.2f;)
            {
                if(GameTime.Paused){yield return null;continue;}
                float dt=Time.deltaTime;t+=dt;
                if(controller!=null&&controller.enabled&&actor.Health.Alive)controller.Move(direction*(distance/.2f)*dt);
                yield return null;
            }
        }
        /// <summary>A rigid generated body still anticipates and follows through (D198): rear back or rise through a tell,
        /// tremble as it peaks, lunge nose-down into a charge. Pitch is degrees about the body's own right axis.</summary>
        void Pose(float pitch,float lift=0,float tremble=0)
        {
            if(visual==null||health.id=="BellowsBelow"||burrowed)return;
            var shake=tremble>0?new Vector3(Random.Range(-1f,1f),0,Random.Range(-1f,1f))*tremble:Vector3.zero;
            visual.localRotation=visualRestRotation*Quaternion.Euler(pitch,0,0);visual.localPosition=visualRest+Vector3.up*lift+shake;
        }
        IEnumerator Windup(float seconds,float pitch,float lift=0,float tremble=0)
        {
            for(float t=0;t<seconds;)
            {
                if(!GameTime.Paused)t+=Time.deltaTime;if(Interrupted)yield break;
                float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(t/seconds));Pose(pitch*k,lift*k,tremble*k*k);yield return null;
            }
        }
        /// <summary>Time until the earliest of these markings strikes the active hero, or none.</summary>
        void ThreatFrom(List<Telegraph> markings)
        {
            float soonest=float.PositiveInfinity;var hero=PartyController.Current.Active.transform.position;
            foreach(var marking in markings)if(marking!=null&&!marking.Released&&marking.Contains(hero))soonest=Mathf.Min(soonest,marking.Remaining);
            strikeAt=float.IsPositiveInfinity(soonest)?0:Time.time+soonest;
        }
        string Pick(params (string move,float weight)[] options)
        {
            float total=0;
            foreach(var option in options)total+=Weight(option);
            float roll=Random.value*total;
            foreach(var option in options){roll-=Weight(option);if(roll<=0&&Weight(option)>0)return option.move;}
            foreach(var option in options)if(option.weight>0)return option.move;
            return options[0].move;
            float Weight((string move,float weight) option)=>option.weight<=0?0:option.move==lastMove?option.weight*.25f:option.weight;
        }
        static CameraRig rig;
        static void Tremor(float seconds,float amplitude)
        {
            if(rig==null)rig=FindFirstObjectByType<CameraRig>();
            if(rig!=null)rig.Tremor(seconds,amplitude);
        }

        IEnumerator PhaseShift()
        {
            shifting=true;
            // Braced and untouchable for a moment, the boss announces the change, shoves the party clear and brings in
            // what the next phase adds. The ring is a marking like any other and does no damage.
            health.InvulnerableUntil=GameTime.Now+1.4f;
            Tremor(.9f,.09f);
            AudioManager.PlayAt("explosionCrunch_000",transform,transform.position+Vector3.up,.2f,AudioBus.SFX,60);
            CombatVfx.Burst(transform.position+Vector3.up*1.5f,new Color(.75f,.45f,1f),"kill");
            BarkService.Play(PartyController.Current.Active.character,"bossphase",true);
            var wave=Mark(Telegraph.Circle(transform.position,collar!=null?9:7,.7f,Telegraph.Stagger));
            strikeAt=Time.time+.7f;
            yield return new WaitForSeconds(.7f);
            wave.Release();strikeAt=0;
            foreach(var actor in PartyController.Current.members)
                if(actor.Health.Alive&&wave.Contains(actor.transform.position))StartCoroutine(Shove(actor,Flat(actor.transform.position-transform.position),3.2f));
            if(health.id=="Burrower")SummonMites(HealthPhase>=3?2:2);
            if(health.id=="HessaKorl")CallCrew(2);
            Debug.Log($"BOSS_SHIFT {health.id} {HealthPhase}");
            yield return new WaitForSeconds(.7f);
            // Untouchable for the shift and not a moment after it.
            health.InvulnerableUntil=Mathf.Min(health.InvulnerableUntil,GameTime.Now);
            shifting=false;Finish(1.2f);
        }
    }
}
