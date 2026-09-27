using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using UnityEngine;

namespace Lattice.Combat
{
    // Resolve only after the generated rig and visible wrist edge have posed.
    [DefaultExecutionOrder(700)]
    public sealed class MeleeContact : MonoBehaviour
    {
        CombatActor actor;Animator rig;HeroWeaponVfx weapon;
        GroundMove move;DamagePacket packet;
        int sequence;bool pending,previousReady,sounded;
        float previousPhase;
        Vector3 previousHand,previousTip;
        readonly HashSet<Health> victims=new();
        readonly Collider[] overlaps=new Collider[128];
        void Awake(){actor=GetComponent<CombatActor>();weapon=GetComponent<HeroWeaponVfx>();}
        public void Begin(GroundMove definition,DamagePacket damage)
        {
            move=definition;packet=damage;sequence=actor.AttackSequence;
            rig=GetComponentInChildren<Animator>();pending=true;previousReady=sounded=false;victims.Clear();
        }
        void LateUpdate()
        {
            if(!pending)return;
            if(!actor.Health.Alive||actor.Recovering||sequence!=actor.AttackSequence||
                actor.State!=ActorState.Attack&&actor.State!=ActorState.Skill)
            {pending=false;return;}
            if(GameTime.Paused||rig==null||!rig.isHuman)return;
            var state=rig.GetCurrentAnimatorStateInfo(0);
            if(rig.IsInTransition(0)&&rig.GetNextAnimatorStateInfo(0).IsName(move.clip))state=rig.GetNextAnimatorStateInfo(0);
            if(!state.IsName(move.clip))return;
            float phase=state.normalizedTime;
            if(weapon==null||!weapon.TryGetBlade(out var hand,out var tip))return;
            if(!previousReady){previousHand=hand;previousTip=tip;previousPhase=phase;previousReady=true;}
            if(phase>=move.contactStart&&previousPhase<=move.contactEnd)
            {
                float span=phase-previousPhase;
                float from=span>0?Mathf.Clamp01((move.contactStart-previousPhase)/span):0;
                float to=span>0?Mathf.Clamp01((move.contactEnd-previousPhase)/span):1;
                var firstHand=Vector3.Lerp(previousHand,hand,from);var firstTip=Vector3.Lerp(previousTip,tip,from);
                var lastHand=Vector3.Lerp(previousHand,hand,to);var lastTip=Vector3.Lerp(previousTip,tip,to);
                int slices=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(Vector3.Distance(firstHand,lastHand),Vector3.Distance(firstTip,lastTip))/.1f),1,32);
                if(!sounded){sounded=true;AudioManager.Play("impactMetal_000",.12f);}
                for(int slice=0;slice<=slices;slice++)
                {
                    float t=slice/(float)slices;
                    var a=Vector3.Lerp(firstHand,lastHand,t);var b=Vector3.Lerp(firstTip,lastTip,t);
                    int count=Physics.OverlapCapsuleNonAlloc(a,b,.085f,overlaps,~0,QueryTriggerInteraction.Collide);
                    for(int i=0;i<count;i++)
                    {
                        var collider=overlaps[i];
                        if(!collider.TryGetComponent<Hurtbox>(out var box)||box.owner==null||!box.owner.Alive||
                            box.owner==actor.Health||box.owner.friendly==actor.Health.friendly||victims.Contains(box.owner))continue;
                        var target=collider.ClosestPoint((a+b)*.5f);
                        if(Physics.Linecast(a,target,out var obstruction,~0,QueryTriggerInteraction.Ignore)&&
                            obstruction.collider.gameObject.isStatic&&!obstruction.collider.TryGetComponent<Hurtbox>(out _))continue;
                        victims.Add(box.owner);box.Hit(packet);
                    }
                }
            }
            previousHand=hand;previousTip=tip;previousPhase=phase;
            if(phase>move.contactEnd)pending=false;
        }
    }
}
