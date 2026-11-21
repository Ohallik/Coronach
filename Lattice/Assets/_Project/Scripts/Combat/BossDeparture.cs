using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// A boss that is beaten and not killed leaves (BossController.Ending.Leaves): it reels for a beat, turns for its way out (<see cref="BossExit"/>, or straight away from the
    /// party) and goes, sinking out of sight at the end, with no body left on the floor. The fight was over the moment its integrity ran out: Health and EnemyBrain have already
    /// paid it out and opened the door, and this is only what the player watches. Added by the boss's own Kit.start, and run by DefeatPresentation in place of a corpse.
    /// </summary>
    public sealed class BossDeparture:MonoBehaviour
    {
        /// <summary>The beat it reels before it turns, and how long the whole leaving takes.</summary>
        public float reel=.7f,Duration=3.6f;
        public float speed=8;
        /// <summary>What it sounds like as it goes (water down a drain, a body through brush); empty for none.</summary>
        public string sound="water_rush";
        Transform visual;Vector3 from,heading,rest,scale;Quaternion facing;float distance,depth;bool sinks,began,settled;
        public bool Leaving=>began;
        public void Begin(Transform visual,DamagePacket packet)
        {
            this.visual=visual;began=true;from=transform.position;facing=transform.rotation;rest=visual.localPosition;scale=transform.localScale;
            var exit=BossExit.For(GetComponent<Health>().id);float run=speed*(Duration-reel);
            if(exit!=null){heading=exit.transform.position-from;heading.y=0;distance=Mathf.Min(heading.magnitude,run);sinks=exit.sinks;}
            else
            {
                var party=PartyController.Current;heading=party!=null&&party.Active!=null?from-party.Active.transform.position:-transform.forward;heading.y=0;distance=run;
            }
            heading=heading.sqrMagnitude>.01f?heading.normalized:-transform.forward;
            depth=TryGetComponent<CharacterController>(out var capsule)?capsule.height+1:4;
            if(!string.IsNullOrEmpty(sound))AudioManager.PlayAt(sound,null,from,.3f,AudioBus.SFX,50,.2f);
            Debug.Log("BOSS_LEAVES "+GetComponent<Health>().id);
        }
        public void Advance(float elapsed)
        {
            if(!began||visual==null)return;
            // Its rest is read here, not as it fell: the boss's own death hands its body back from whatever pose its last move left it in, after this was begun.
            if(!settled){settled=true;rest=visual.localPosition;}
            float k=Mathf.Clamp01((elapsed-reel)/Mathf.Max(.1f,Duration-reel)),gone=Mathf.SmoothStep(0,1,Mathf.InverseLerp(sinks?.55f:.8f,1,k));
            // Reeling: it sags where it stands. Then it turns in under half a second and goes, faster as it gets clear.
            float sag=elapsed<reel?Mathf.Sin(Mathf.Clamp01(elapsed/reel)*Mathf.PI)*.25f:0;
            var at=from+heading*(distance*k*k*(3-2*k));
            var zone=ZoneController.Current;if(zone==null||!zone.Flight){var ground=CombatCover.Ground(at+Vector3.up*3);if(Mathf.Abs(ground.y-at.y)<2.5f)at.y=ground.y;}
            transform.SetPositionAndRotation(at,Quaternion.Slerp(facing,Quaternion.LookRotation(heading),Mathf.Clamp01((elapsed-reel*.5f)/.45f)));
            visual.localPosition=rest+Vector3.down*(sag+depth*gone);
            // What walks out of sight is small when it gets there; what goes down a drain is simply under.
            if(!sinks)transform.localScale=scale*(1-.85f*gone);
        }
    }
}
