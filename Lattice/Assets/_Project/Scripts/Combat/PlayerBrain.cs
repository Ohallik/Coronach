using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    [RequireComponent(typeof(CombatActor))]
    public sealed class PlayerBrain:MonoBehaviour
    {
        CombatActor actor;
        float lungeRequestedUntil=-1;
        float attackRequestedUntil=-1;
        public bool AutoPilot;
        void Awake(){actor=GetComponent<CombatActor>();}
        void Update()
        {
            if(AutoPilot||GameServices.Current==null||actor.motor==null)return;
            var input=GameServices.Current.Input;if(input.Blocked||!actor.Health.Alive){lungeRequestedUntil=attackRequestedUntil=-1;return;}
            if(GameTime.Paused)return;
            if(actor.State==Lattice.Data.ActorState.Stagger){lungeRequestedUntil=attackRequestedUntil=-1;actor.motor.Move(Vector2.zero,false,true);return;}
            var zone=ZoneController.Current;bool combat=zone!=null&&zone.Combat;
            if(!combat||actor.target==null||!actor.target.Alive||(actor.target.transform.position-transform.position).sqrMagnitude>900)
                actor.TargetLocked=false;
            PromptService.FindInteraction(transform.position);
            if(input.Pressed("Interact")&&PromptService.TryInteract()){attackRequestedUntil=-1;return;}
            Vector2 move=input.Move;
            var rotated=Quaternion.Euler(0,zone.definition.cameraProfile.yaw,0)*new Vector3(move.x,0,move.y);
            move=new Vector2(rotated.x,rotated.z);
            bool boost=input.Held(actor.flight?"Boost":"Sprint")&&actor.thrust>1;
            if(boost)actor.thrust=Mathf.Max(0,actor.thrust-Time.unscaledDeltaTime*34);
            actor.motor.Move(move,boost,actor.flight&&input.Held("Brake"));
            if(input.Pressed("Swap")&&!input.SkillMod)PartyController.Current.Swap();
            if(!combat)return;
            if(input.Pressed("LockOn"))
            {
                if(actor.TargetLocked)actor.TargetLocked=false;
                else{actor.target=FindTarget();actor.TargetLocked=actor.target!=null;}
            }
            int cycle=input.Cycle("CycleTarget");if(cycle!=0)CycleTarget(cycle);
            int itemCycle=input.Cycle("CycleItem");if(itemCycle!=0)
            {
                var state=GameServices.Current.State;var items=new System.Collections.Generic.List<string>(state.consumables.Keys);
                if(items.Count>0)state.quickItem=items[(items.IndexOf(state.quickItem)+itemCycle+items.Count)%items.Count];
            }
            if(input.SkillMod)
            {lungeRequestedUntil=attackRequestedUntil=-1;for(int i=0;i<4;i++)if(input.Pressed("Skill"+(i+1)))actor.Skill(i);return;}
            for(int i=0;i<4;i++)if(input.Pressed("Skill"+(i+1)))actor.Skill(i);
            // A short lunge tap during emitter recovery should fire as soon as recovery ends.
            // Give it priority over held fire so continuous shooting cannot starve the request.
            if(actor.flight&&input.Pressed("Lunge"))lungeRequestedUntil=GameTime.Now+.2f;
            bool lungePending=actor.flight&&GameTime.Now<=lungeRequestedUntil;
            if(lungePending&&actor.Lunge())lungeRequestedUntil=-1;
            if(actor.flight&&!lungePending&&!PromptService.AConsumed&&input.Held("Fire"))actor.Attack();
            if(!actor.flight&&!PromptService.AConsumed&&input.Pressed("Attack"))attackRequestedUntil=GameTime.Now+.24f;
            if(!actor.flight&&GameTime.Now<=attackRequestedUntil&&actor.Attack())attackRequestedUntil=-1;
            if(input.Pressed(actor.flight?"Roll":"Dodge")){attackRequestedUntil=-1;actor.Dodge(new Vector3(move.x,0,move.y));}
            if(!actor.flight&&input.Held("Guard"))attackRequestedUntil=-1;
            if(!actor.flight)actor.Guard(input.Held("Guard"));
            if(!actor.flight&&input.Pressed("QuickItem"))UseItem();
            if(actor.target==null||!actor.target.Alive)actor.target=FindTarget();
        }
        void OnDisable(){lungeRequestedUntil=attackRequestedUntil=-1;if(actor!=null)actor.TargetLocked=false;}
        public Health FindTarget()
        {
            Health nearest=null;float best=30*30;
            foreach(var h in Health.All)
            {
                if(h==null||h.friendly||!h.Alive)continue;var offset=h.transform.position-transform.position;
                float score=offset.sqrMagnitude;if(Vector3.Angle(actor.motor.Facing,offset)>120)score*=2;
                if(score<best){nearest=h;best=score;}
            }
            return nearest;
        }
        public void CycleTarget(int direction)
        {
            var targets=new System.Collections.Generic.List<Health>();
            foreach(var h in Health.All)if(h!=null&&h.Alive&&!h.friendly&&(h.transform.position-transform.position).sqrMagnitude<900)targets.Add(h);
            targets.Sort((a,b)=>Vector3.SignedAngle(transform.forward,a.transform.position-transform.position,Vector3.up).CompareTo(Vector3.SignedAngle(transform.forward,b.transform.position-transform.position,Vector3.up)));
            if(targets.Count>0){actor.target=targets[(targets.IndexOf(actor.target)+direction+targets.Count)%targets.Count];actor.TargetLocked=true;}
        }
        public void UseItem(string id=null)
        {
            var state=GameServices.Current.State;
            id??=state.quickItem;var def=GameCatalog.Find<Lattice.Data.ConsumableDef>(id);if(def==null)return;
            if(state.consumables.TryGetValue(id,out var n)&&n>0&&(def.heal>0&&actor.Health.integrity<actor.Health.maximum||def.charge>0&&actor.charge<100))
            {state.consumables[id]=n-1;actor.Health.Heal(def.heal);actor.charge=Mathf.Min(100,actor.charge+def.charge);}
        }
    }
}
