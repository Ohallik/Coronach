using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// A guardian that <b>gives ground</b> (<c>tune.givesGround</c>: chapter 9's Compact Warden, Meret's enforcement crews in sealed frames, professionals with a shield and a stun-baton who
    /// are doing a job and are not there to die for it). <b>Its guard broken</b> (its Break meter filled), it doesn't stand and take it: it backs off <see cref="GivesGroundBy"/> metres from
    /// whoever is nearest in half a second, shield down, and stands open for the rest of the break. <b>At nought it stands aside</b>: it lowers its baton, turns from the party and walks out of
    /// the fight (<see cref="BossDeparture"/>, by a <see cref="BossExit"/> of its id if the room has one), and is gone. No body falls and nothing bursts; nobody calls it a kill, and the lead says
    /// the definition's <c>tune.bark</c> as it goes. Nobody of Meret's is killed on screen. Logs <c>WARDEN_GIVES_GROUND &lt;id&gt;</c>, <c>WARDEN_STANDS_ASIDE &lt;id&gt;</c>.
    /// </summary>
    public sealed partial class EnemyGuardian
    {
        /// <summary>How far it backs off when its guard breaks, and in how long; how fast it walks out of the fight at nought, and how long its leaving takes.</summary>
        public const float GivesGroundBy=3,GivesGroundIn=.5f,StandAsideSpeed=3.2f,StandAsideSeconds=3.4f;
        bool wasBroken,givingGround;float groundUntil;Vector3 groundWay;
        /// <summary>Backing off from a broken guard now; and how many times it has, since it woke.</summary>
        public bool GivingGround=>givingGround;
        public int GaveGround{get;private set;}
        void GiveGround()
        {
            if(!tune.givesGround||health==null||!health.Alive){givingGround=false;return;}
            bool broken=health.Broken;
            if(broken&&!wasBroken&&PartyController.Current!=null)
            {
                // From whoever is nearest: that is who broke it.
                CombatActor nearest=null;float least=float.PositiveInfinity;
                foreach(var hero in PartyController.Current.members){if(!hero.Health.Alive)continue;float d=Flat(hero.transform.position-body.position).sqrMagnitude;if(d<least){least=d;nearest=hero;}}
                var away=nearest!=null?Flat(body.position-nearest.transform.position):-Flat(body.forward);
                groundWay=away.sqrMagnitude>.01f?away.normalized:-body.forward;givingGround=true;groundUntil=Time.time+GivesGroundIn;GaveGround++;
                Debug.Log("WARDEN_GIVES_GROUND "+def.id);
            }
            wasBroken=broken;
            if(!givingGround)return;
            if(!broken||Time.time>=groundUntil||GameTime.Paused){givingGround=Time.time<groundUntil&&broken;return;}
            if(controller!=null&&controller.enabled)controller.Move(groundWay*(GivesGroundBy/GivesGroundIn*Time.deltaTime));
            Face(-groundWay,360);
        }
        /// <summary>At nought: the baton lowered, the walk out. The leaving itself is DefeatPresentation's (BossDeparture, added as it woke); here only what the body does with its legs and
        /// what the party says.</summary>
        void StandAside()
        {
            if(!tune.givesGround)return;
            // Its own animation driver would put it down on the floor: it walks instead.
            foreach(var rig in body.GetComponentsInChildren<GeneratedAnimator>())rig.enabled=false;
            var animator=body.GetComponentInChildren<Animator>();
            if(animator!=null&&animator.runtimeAnimatorController!=null){animator.speed=1;if(animator.HasState(0,Animator.StringToHash("Walk")))animator.CrossFadeInFixedTime("Walk",.3f,0);}
            if(!string.IsNullOrEmpty(tune.bark)&&PartyController.Current!=null&&PartyController.Current.Active!=null)BarkService.Play(PartyController.Current.Active.character,tune.bark);
            Debug.Log("WARDEN_STANDS_ASIDE "+def.id);
        }
    }
}
