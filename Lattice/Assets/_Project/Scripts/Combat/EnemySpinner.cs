using Lattice.Data;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The spinner (chapter 7's Pruner: a garden automaton set to prune, with shears longer than a man): it walks in, opens its shears and turns once on the spot, and the blades sweep a
    /// marked ring round it. The ring has a hole in its middle: inside the handles, against its body, the blades pass over. Then it is giddy, open for <c>tune.recover</c>. So the answer
    /// is to step in under the blades, or out past them, and strike it while it reels. It does not chase a hero who stays out of its reach for long: it keeps to where it was set
    /// (its garden), <c>tune.wake</c> metres round it.
    /// Reads: attacks[0] (telegraph, range: how close a hero must be for it to open its shears, radius: the blades' reach, damage, cooldown); tune.width (the hole the handles leave, 1.6),
    /// recover (giddy, 1.5), wake (its garden, 12; negative for none).
    /// </summary>
    public sealed class EnemySpinner:EnemyBehaviour
    {
        float Blades=>attack!=null&&attack.radius>0?attack.radius:4.2f;float Handles=>Mathf.Min(Or(tune.width,1.6f),Blades-1);float Giddy=>Or(tune.recover,1.5f);
        float Garden=>tune.wake<0?float.PositiveInfinity:Or(tune.wake,12);
        float spun=-1,turnedFrom;
        public override bool Roams=>false;
        /// <summary>Turning through its cut: what a check reads.</summary>
        public bool Spinning=>spun>=0;
        public override void Tick(CombatActor victim)
        {
            var to=Flat(victim.transform.position-body.position);float distance=to.magnitude;
            if(brain.Telegraphing)
            {
                // The shears open as the ring fills: it leans back, arms wide.
                Posing=true;Pitch=-8*brain.TellProgress;Swell=.12f*brain.TellProgress;
                if(!brain.TellRunOut)return;
                var ring=brain.EndTell(.4f);
                if(ring!=null){StrikeIn(ring,brain.Blow(attack,HitWeight.Heavy),Chest);ring.Release();}
                Sound("swing_2",.26f,.6f);spun=0;turnedFrom=body.eulerAngles.y;brain.Acting=true;
                CombatVfx.Burst(body.position+Vector3.up*1.2f,new Color(1f,.55f,.4f),"kill");
                return;
            }
            if(spun>=0)return;
            Posing=false;
            var home=Flat(brain.Home-body.position);
            if(home.magnitude>Garden){brain.Walk(home,def.speed);Face(home,240);return;}
            if(distance<Range&&brain.Rested&&brain.Sees(victim))
            {
                Mark(Telegraph.Ring(body.position,Handles,Blades,Tell,Telegraph.Threat),Tell,to,victim,"swing_2",.55f);
                Debug.Log("SPINNER_OPENS "+def.id);return;
            }
            brain.Spread(to,distance,Range);Face(to,300);
            if(distance>Range*.6f&&Flat(victim.transform.position-brain.Home).magnitude<Garden+Range)brain.Walk(to,def.speed);
        }
        /// <summary>The turn itself, late every frame, and the giddy beat after it: the brain ticks nothing open.</summary>
        public override void Present()
        {
            if(spun<0||Lattice.Core.GameTime.Paused)return;
            float before=spun;spun+=Time.deltaTime;
            if(spun<SpinSeconds){body.rotation=Quaternion.Euler(0,turnedFrom+360*Mathf.SmoothStep(0,1,spun/SpinSeconds),0);return;}
            if(before<SpinSeconds){body.rotation=Quaternion.Euler(0,turnedFrom,0);brain.Acting=false;Posing=true;Pitch=10;Roll=8;Open(Giddy);brain.Rest(Giddy+Cooldown*.5f);Debug.Log("SPINNER_GIDDY "+def.id);}
            if(spun>=SpinSeconds+Giddy){spun=-1;Posing=false;Pitch=Roll=0;}
        }
        /// <summary>How long one turn of the shears takes.</summary>
        public const float SpinSeconds=.4f;
        public override void Cancel(){if(spun>=0&&spun<SpinSeconds){body.rotation=Quaternion.Euler(0,turnedFrom,0);brain.Acting=false;spun=-1;}Posing=false;Pitch=Roll=Swell=0;}
    }
}
