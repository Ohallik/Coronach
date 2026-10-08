using System.Collections;
using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    public sealed class BossController:MonoBehaviour
    {
        public int Phase{get;private set;}=1;
        Health health;
        EnemyBrain brain;
        CantorCollar collar;int cantorSpecials;
        float nextSpecial,strikeAt,engaged=-1;bool busy;
        Transform inflated,body;Vector3 inflatedRest,bodyRest;int breaths;
        public bool Busy=>busy;
        public bool Telegraphing=>busy&&Time.time<strikeAt;
        public float TelegraphRemaining=>Mathf.Max(0,strikeAt-Time.time);
        void Start()
        {
            health=GetComponent<Health>();brain=GetComponent<EnemyBrain>();
            collar=GetComponent<CantorCollar>();
            // Dormant previews delay Start until activation. The combat cue
            // then lasts until resolution or removal.
            MusicDirector.Encounter(this,"Alien Boss Battle",true);
            nextSpecial=Time.time+5;health.Damaged+=OnDamaged;
            // The Bellows has no rig: its breathing is its animation.
            if(health.id=="BellowsBelow"){var presentation=GetComponent<DefeatPresentation>();body=presentation!=null?presentation.visual:null;if(body!=null)bodyRest=body.localScale;}
            for(int i=0;i<(collar!=null?0:3);i++)
            {
                var weak=new GameObject("WeakPoint_"+i,typeof(SphereCollider),typeof(Hurtbox));weak.transform.SetParent(transform,false);
                weak.transform.localPosition=new Vector3((i-1)*1.2f,1.4f,0);weak.GetComponent<SphereCollider>().radius=.5f;weak.GetComponent<SphereCollider>().isTrigger=true;
                weak.GetComponent<Hurtbox>().owner=health;weak.GetComponent<Hurtbox>().multiplier=2;
            }
            health.Died+=(_,__)=>
            {
                StopAllCoroutines();Deflate();busy=false;enabled=false;MusicDirector.Encounter(this,null,false);
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
            StopAllCoroutines();Deflate();busy=false;strikeAt=0;
            nextSpecial=Mathf.Max(nextSpecial,health.BrokenUntil+.5f);
        }
        void OnDestroy(){if(health!=null)health.Damaged-=OnDamaged;MusicDirector.Encounter(this,null,false);}
        public void RestoreAfterRecovery()
        {
            StopAllCoroutines();Deflate();busy=false;strikeAt=0;engaged=-1;
            Phase=1;cantorSpecials=0;brain.DamageScale=1;nextSpecial=Time.time+5;enabled=true;
            MusicDirector.Encounter(this,"Alien Boss Battle",true);
        }
        void Update()
        {
            if(GameTime.Paused||health==null||!health.Alive)return;
            float ratio=health.integrity/health.maximum;int phase=health.id=="Cantor"?(ratio<.33f?3:ratio<.66f?2:1):health.id=="BellowsBelow"?(ratio<.3f?3:ratio<.5f?2:1):(ratio<.5f?2:1);
            if(collar!=null)phase=Mathf.Max(phase,Mathf.Min(3,4-collar.Remaining));
            if(phase!=Phase){Phase=phase;Debug.Log($"BOSS_PHASE {health.id} {Phase}");}
            brain.DamageScale=1+(Phase-1)*.18f;
            // The Bellows braces against the cave while any pressure organ pumps.
            if(health.id=="BellowsBelow"&&PressureOrgan.AnyPumping)health.ShieldUntil=Time.time+.25f;
            if(body!=null&&inflated==null)body.localScale=bodyRest*(1+.035f*Mathf.Sin(Time.time*2.1f));
            if(brain.Passive||busy||health.Broken||GameServices.Current.Input.Blocked||PartyController.Current==null||Time.time<nextSpecial)return;
            if((PartyController.Current.Active.transform.position-transform.position).sqrMagnitude>900)return;
            StartCoroutine(health.id=="Cantor"?ChoirAttack():health.id=="BellowsBelow"?(Phase==3?BellowsCrossing():BellowsBreath()):BurrowCharge());
        }
        IEnumerator BurrowCharge()
        {
            busy=true;strikeAt=Time.time+.85f;var direction=PartyController.Current.Active.transform.position-transform.position;direction.y=0;direction.Normalize();transform.rotation=Quaternion.LookRotation(direction);
            for(int i=1;i<=6;i++)CombatVfx.Burst(transform.position+direction*i*2+Vector3.up*.2f,new Color(1,.3f,.1f),"shape");
            yield return new WaitForSeconds(.85f);
            var hit=new HashSet<Health>();var controller=GetComponent<CharacterController>();
            for(float t=0;t<.85f&&health.Alive&&!health.Broken;t+=Time.deltaTime)
            {
                if(GameTime.Paused){yield return null;continue;}
                if(controller.enabled)controller.Move(direction*(Phase==2?19:15)*Time.deltaTime);
                foreach(var actor in PartyController.Current.members)if(actor.Health.Alive&&!hit.Contains(actor.Health)&&(actor.transform.position-transform.position).sqrMagnitude<9)
                {hit.Add(actor.Health);actor.Health.Receive(new DamagePacket{source=health,amount=26*brain.DamageScale,type=DamageType.Kinetic});}
                yield return null;
            }
            busy=false;nextSpecial=Time.time+(Phase==2?4:6);
        }
        void Deflate()
        {
            if(inflated!=null)inflated.localScale=inflatedRest;inflated=null;if(body!=null)body.localScale=bodyRest;
            foreach(var organ in PressureOrgan.All)organ.Breathe(0);
        }
        IEnumerator BellowsBreath()
        {
            // Inhale: the body and its organs swell inside a ring that marks the
            // blast, and one pumping organ, in turn, marks its half of the chamber.
            // Exhale: everything inside the ring or that half is struck and staggered.
            busy=true;float radius=Phase>=2?13:9.5f,inhale=Phase>=2?1.25f:1.05f;strikeAt=Time.time+inhale;
            PressureOrgan blast=null;int pumping=0;foreach(var organ in PressureOrgan.All)if(organ.Pumping)pumping++;
            if(pumping>0){int turn=breaths++%pumping;foreach(var organ in PressureOrgan.All)if(organ.Pumping&&turn--==0){blast=organ;break;}}
            if(blast!=null)
            {
                var mid=PressureOrgan.Middle;var side=blast.transform.position-mid;side.y=0;side.Normalize();
                for(int r=1;r<=3;r++)for(int a=-2;a<=2;a++)CombatVfx.Burst(mid+Quaternion.Euler(0,a*32,0)*side*(r*6)+Vector3.up*.3f,new Color(.7f,.45f,1),"shape");
            }
            inflated=body;inflatedRest=bodyRest;
            for(int i=0;i<16;i++){float a=i*Mathf.PI/8;CombatVfx.Burst(transform.position+new Vector3(Mathf.Cos(a)*radius,.4f,Mathf.Sin(a)*radius),new Color(.85f,.72f,.55f),"shape");}
            for(float t=0;t<inhale;)
            {
                if(GameTime.Paused){yield return null;continue;}
                t+=Time.deltaTime;float k=Mathf.Clamp01(t/inhale);
                if(inflated!=null)inflated.localScale=inflatedRest*(1+.35f*k);
                foreach(var organ in PressureOrgan.All)organ.Breathe(k);
                yield return null;
            }
            Deflate();
            if(health.Alive&&!health.Broken)
            {
                CombatVfx.Burst(transform.position+Vector3.up,new Color(.85f,.72f,.55f),"shape");
                foreach(var actor in PartyController.Current.members)
                    if(actor.Health.Alive&&((actor.transform.position-transform.position).sqrMagnitude<radius*radius||blast!=null&&blast.Pumping&&blast.Covers(actor.transform.position)))
                    {
                        float damage=actor.Health.Receive(new DamagePacket{source=health,amount=22*brain.DamageScale,type=DamageType.Kinetic});
                        if(damage>0)actor.Stagger(.5f);
                    }
            }
            busy=false;nextSpecial=Time.time+(Phase>=2?4.5f:6);
        }
        IEnumerator BellowsCrossing()
        {
            // Last phase: the dome collapses onto its ribs. It marks a path to the
            // rib nearest the party, lunges along it striking whatever stands in
            // the way, then breathes from the new rib.
            busy=true;var rib=BellowsRib.Choose(transform.position,PartyController.Current.Active.transform.position);
            if(rib==null){busy=false;yield return BellowsBreath();yield break;}
            var goal=rib.Stance;var direction=goal-transform.position;direction.y=0;float distance=direction.magnitude;direction/=Mathf.Max(.01f,distance);
            transform.rotation=Quaternion.LookRotation(direction);strikeAt=Time.time+.9f;
            for(float d=2;d<distance;d+=2.5f)CombatVfx.Burst(transform.position+direction*d+Vector3.up*.2f,new Color(.85f,.72f,.55f),"shape");
            CombatVfx.Burst(goal+Vector3.up*.4f,new Color(.7f,.45f,1),"shape");
            yield return new WaitForSeconds(.9f);
            var hit=new HashSet<Health>();var controller=GetComponent<CharacterController>();
            for(float t=0;t<1.8f&&health.Alive&&!health.Broken;)
            {
                if(GameTime.Paused){yield return null;continue;}
                t+=Time.deltaTime;var left=goal-transform.position;left.y=0;if(left.magnitude<.6f)break;
                if(controller.enabled)controller.Move(left.normalized*Mathf.Min(16*Time.deltaTime,left.magnitude));
                foreach(var actor in PartyController.Current.members)
                    if(actor.Health.Alive&&!hit.Contains(actor.Health)&&(actor.transform.position-transform.position).sqrMagnitude<9)
                    {hit.Add(actor.Health);float damage=actor.Health.Receive(new DamagePacket{source=health,amount=24*brain.DamageScale,type=DamageType.Kinetic});if(damage>0)actor.Stagger(.5f);}
                yield return null;
            }
            busy=false;
            if(health.Alive&&!health.Broken)yield return BellowsBreath();
        }
        IEnumerator ChoirAttack()
        {
            busy=true;bool song=Phase>=2&&(cantorSpecials++%2)==1;
            bool sweep=collar==null||collar.SweepAttached,chorus=collar==null||collar.ChorusAttached;
            float radius=song?(chorus?14:10):(sweep?8:5);strikeAt=Time.time+(song?1.1f:.85f);
            Debug.Log($"CANTOR_TELL song={song} radius={radius} stagger={song&&chorus}");
            for(int i=0;i<16;i++){float a=i*Mathf.PI/8;CombatVfx.Burst(transform.position+new Vector3(Mathf.Cos(a)*radius,.4f,Mathf.Sin(a)*radius),song?new Color(.8f,.2f,1):new Color(1,.3f,.1f),"shape");}
            yield return new WaitForSeconds(song?1.1f:.85f);
            if(health.Alive&&!health.Broken)
                foreach(var actor in PartyController.Current.members)
                    if((actor.transform.position-transform.position).sqrMagnitude<radius*radius)
                    {
                        float damage=actor.Health.Receive(new DamagePacket{source=health,amount=(song?(chorus?12:8):24)*brain.DamageScale,type=song?DamageType.Pulse:DamageType.Kinetic});
                        if(song&&chorus&&damage>0)actor.Stagger(.65f);
                    }
            busy=false;nextSpecial=Time.time+(Phase==3?4:6);
        }
    }
}
