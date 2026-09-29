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
        float nextSpecial,strikeAt,engaged=-1;bool busy;
        public bool Busy=>busy;
        public bool Telegraphing=>busy&&Time.time<strikeAt;
        public float TelegraphRemaining=>Mathf.Max(0,strikeAt-Time.time);
        void Start()
        {
            health=GetComponent<Health>();brain=GetComponent<EnemyBrain>();
            // A boss exists only once its encounter begins; its cue lasts until defeat or removal.
            MusicDirector.Encounter(this,"Alien Boss Battle",true);
            nextSpecial=Time.time+5;health.Damaged+=OnDamaged;
            for(int i=0;i<3;i++)
            {
                var weak=new GameObject("WeakPoint_"+i,typeof(SphereCollider),typeof(Hurtbox));weak.transform.SetParent(transform,false);
                weak.transform.localPosition=new Vector3((i-1)*1.2f,1.4f,0);weak.GetComponent<SphereCollider>().radius=.5f;weak.GetComponent<SphereCollider>().isTrigger=true;
                weak.GetComponent<Hurtbox>().owner=health;weak.GetComponent<Hurtbox>().multiplier=2;
            }
            health.Died+=(_,__)=>{StopAllCoroutines();busy=false;enabled=false;MusicDirector.Encounter(this,null,false);GameServices.Current.Flags.SetBool("bossdown."+health.id,true);BarkService.Play(PartyController.Current.Active.character,"boss",true);Debug.Log("BOSS_DOWN "+health.id);Debug.Log($"BOSS_DURATION {health.id} seconds={Time.realtimeSinceStartup-engaged:0.0}");};
        }
        void OnDamaged(Health _,DamagePacket packet,float amount)
        {
            if(engaged<0&&amount>0)engaged=Time.realtimeSinceStartup;
            if(!health.Broken)return;
            StopAllCoroutines();busy=false;strikeAt=0;
            nextSpecial=Mathf.Max(nextSpecial,health.BrokenUntil+.5f);
        }
        void OnDestroy(){if(health!=null)health.Damaged-=OnDamaged;MusicDirector.Encounter(this,null,false);}
        void Update()
        {
            if(GameTime.Paused||health==null||!health.Alive)return;
            float ratio=health.integrity/health.maximum;int phase=health.id=="Cantor"?(ratio<.33f?3:ratio<.66f?2:1):(ratio<.5f?2:1);
            if(phase!=Phase){Phase=phase;Debug.Log($"BOSS_PHASE {health.id} {Phase}");}
            brain.DamageScale=1+(Phase-1)*.18f;
            if(brain.Passive||busy||health.Broken||GameServices.Current.Input.Blocked||PartyController.Current==null||Time.time<nextSpecial)return;
            if((PartyController.Current.Active.transform.position-transform.position).sqrMagnitude>900)return;
            StartCoroutine(health.id=="Cantor"?ChoirAttack():BurrowCharge());
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
        IEnumerator ChoirAttack()
        {
            busy=true;bool song=Phase>=2;float radius=song?14:8;strikeAt=Time.time+(song?1.1f:.85f);
            for(int i=0;i<16;i++){float a=i*Mathf.PI/8;CombatVfx.Burst(transform.position+new Vector3(Mathf.Cos(a)*radius,.4f,Mathf.Sin(a)*radius),song?new Color(.8f,.2f,1):new Color(1,.3f,.1f),"shape");}
            yield return new WaitForSeconds(song?1.1f:.85f);
            if(health.Alive&&!health.Broken)
                foreach(var actor in PartyController.Current.members)
                    if((actor.transform.position-transform.position).sqrMagnitude<radius*radius)
                    {
                        float damage=actor.Health.Receive(new DamagePacket{source=health,amount=(song?12:24)*brain.DamageScale,type=song?DamageType.Pulse:DamageType.Kinetic});
                        if(song&&damage>0)actor.Stagger(.65f);
                    }
            busy=false;nextSpecial=Time.time+(Phase==3?4:6);
        }
    }
}
