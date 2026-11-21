using Lattice.Core;
using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The tender (chapter 7's Clamp Tender: a little maintenance construct of the court's, half a person tall): it keeps what it was made to keep. While anything it tends is wanted
    /// (<see cref="Tended"/>: a Bridle clamp that can be worked, a stay gone slack) within <c>tune.wake</c>, it walks to its seat and works it, and pays no heed to anyone while it does:
    /// a clamp it is at loses what a hero has taken of its weight and never eases, so it has to be stopped first, and it is fragile. With nothing to keep it fights with its welding
    /// torch, a short marked fan before it, and keeps a step off from whoever it burns.
    /// Reads: attacks[0] (telegraph, range, damage, cooldown: the torch); tune.arc (the torch's fan, 70), wake (how far it looks for its work, 24), bark (said as it sets to work on
    /// something a hero has his hands on).
    /// </summary>
    public sealed class EnemyTender:EnemyBehaviour
    {
        Tended work;bool barked;float nextSpark;
        float Fan=>Mathf.Clamp(Or(tune.arc,70),20,170);float Looks=>Or(tune.wake,24);
        /// <summary>What it is keeping now, if anything.</summary>
        public Tended Work=>work;
        public bool Working=>work!=null&&work.Tending&&work.By==brain;
        public override bool Roams=>false;
        public override void Tick(CombatActor victim)
        {
            if(brain.Telegraphing)
            {
                Posing=true;Pitch=-10*brain.TellProgress;
                if(!brain.TellRunOut)return;
                var fan=brain.EndTell(.3f);
                if(fan!=null){StrikeIn(fan,brain.Blow(attack,HitWeight.Medium),Chest);fan.Release();CombatVfx.Burst(fan.Origin+fan.Forward*(fan.Radius*.5f)+Vector3.up*.6f,Torch,"kill");}
                Sound("laserSmall_000",.18f,.7f);brain.Rest(Cooldown);Posing=false;Pitch=0;
                return;
            }
            if(work==null||!work.wanted||!work.isActiveAndEnabled||work.Tending&&work.By!=brain){work=Tended.Nearest(body.position,Looks,brain);barked=false;}
            if(work!=null){Keep();return;}
            Posing=false;brain.Acting=false;
            var to=Flat(victim.transform.position-body.position);float distance=to.magnitude;
            Face(to,240);
            if(distance<Range&&brain.Rested&&brain.Sees(victim))
            {
                Mark(Telegraph.Cone(body.position,body.forward,Range,Fan,Tell,Telegraph.Threat,true),Tell,body.forward,victim,"forceField_000",1.4f);return;
            }
            brain.Spread(to,distance,Range);
            if(distance>Range*.85f)brain.Walk(to,def.speed);
            else if(distance<Range*.45f)brain.Walk(-to,def.speed*.8f);
        }
        static readonly Color Torch=new(1.4f,.9f,.45f,.9f);
        /// <summary>To its seat (a step to one side of it, where a hero's hands aren't), and at work there.</summary>
        void Keep()
        {
            // Beside the seat, on whichever flank of it is nearer: the seat itself is where a hero's hands go.
            var seat=work.Seat;var across=Flat(work.seat!=null?work.seat.right:Vector3.right).normalized;
            var stand=seat+across*(Vector3.Dot(Flat(body.position-seat),across)>=0?1.1f:-1.1f);var way=Flat(stand-body.position);
            if(way.magnitude>.45f&&Flat(seat-body.position).magnitude>1.6f){brain.Walk(way,def.speed);Face(way,300);Posing=false;brain.Acting=false;return;}
            Face(Flat(seat-body.position),300);
            work.Tend(brain,Time.deltaTime);brain.Acting=true;
            Posing=true;Pitch=8+3*Mathf.Sin(Time.time*14);
            if(Time.time>=nextSpark){nextSpark=Time.time+.35f;CombatVfx.Burst(Vector3.Lerp(body.position,seat,.6f)+Vector3.up*.9f,Torch,"failure");}
            if(!barked&&work.contested)
            {
                barked=true;Debug.Log("TENDER_CLOSING "+def.id);
                if(!string.IsNullOrEmpty(tune.bark)&&PartyController.Current!=null)BarkService.Play(PartyController.Current.Active.character,tune.bark,true);
            }
        }
        public override void Cancel(){brain.Acting=false;Posing=false;Pitch=0;}
        public override void Fell(DamagePacket packet){brain.Acting=false;if(work!=null&&work.By==brain)work.Clear();}
    }
}
