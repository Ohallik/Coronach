using Lattice.Core;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// A boss that is beaten and not killed leaves (BossController.Ending.Leaves): it reels for a beat, turns for its way out (<see cref="BossExit"/>, or straight away from the
    /// party) and goes, sinking out of sight at the end, with no body left on the floor. The fight was over the moment its integrity ran out: Health and EnemyBrain have already
    /// paid it out and opened the door, and this is only what the player watches. Added by the boss's own Kit.start, and run by DefeatPresentation in place of a corpse.
    /// </summary>
    /// <para>D363: it goes beaten. The Hulljack stood whole in its yellow and black beside the ship under the quest card, and the Floodcrown and the Seedthane walked off in full
    /// colour (nov22b-whole s29/043 to 049, s21/288 to 295, s20/336 to 346): from the moment it is beaten its lights go down and its paint drains to three fifths (BodyGoesDark),
    /// it breaks off in under half a second, and a body not on its own feet stays sunk and bowed as it goes instead of bobbing back up.</para>
    public sealed class BossDeparture:MonoBehaviour
    {
        /// <summary>The beat it reels before it turns, and how long the whole leaving takes.</summary>
        public float reel=.4f,Duration=3.6f;
        public float speed=8;
        /// <summary>What it sounds like as it goes (water down a drain, a body through brush); empty for none.</summary>
        public string sound="water_rush";
        /// <summary>Round eleven: what is heard as it goes out of sight (a door it walks out through), played once where it is then; empty for none. Vessa Korl walked
        /// out with no sound at all.</summary>
        public string outSound="";
        bool outPlayed;
        /// <summary>D419: a person who walks out on her own feet (Vessa Korl) keeps her colours, as one who yields does (D412): beaten, not powered down. Gone dark she walked out a
        /// black figure (nov25-r5-chain s48/227 to 244) where her sister, yielding, stood in her own colours (s15/158). Set by her own fight.</summary>
        public bool keepsColours;
        Transform visual;Vector3 from,heading,rest,scale;Quaternion facing,restTurn;float distance,depth;bool sinks,began,settled,upright;
        public bool Leaving=>began;
        public void Begin(Transform visual,DamagePacket packet)
        {
            this.visual=visual;began=true;from=transform.position;facing=transform.rotation;rest=visual.localPosition;restTurn=visual.localRotation;scale=transform.localScale;
            if(!keepsColours)(GetComponent<BodyGoesDark>()??gameObject.AddComponent<BodyGoesDark>()).BeginBeaten(transform);
            var exit=BossExit.For(GetComponent<Health>().id);float run=speed*(Duration-reel);
            if(exit!=null){heading=exit.transform.position-from;heading.y=0;distance=Mathf.Min(heading.magnitude,run);sinks=exit.sinks;}
            else
            {
                var party=PartyController.Current;heading=party!=null&&party.Active!=null?from-party.Active.transform.position:-transform.forward;heading.y=0;distance=run;
            }
            heading=heading.sqrMagnitude>.01f?heading.normalized:-transform.forward;
            depth=TryGetComponent<CharacterController>(out var capsule)?capsule.height+1:4;
            // Someone on their own feet (a humanoid rig) walks out bowed, feet on the floor: no sag through it and no sinking under it, unless the way out is a drain.
            var rig=GetComponentInChildren<Animator>();upright=rig!=null&&rig.isHuman;
            if(!string.IsNullOrEmpty(sound))AudioManager.PlayAt(sound,null,from,.3f,AudioBus.SFX,50,.2f);
            Debug.Log("BOSS_LEAVES "+GetComponent<Health>().id);
        }
        public void Advance(float elapsed)
        {
            if(!began||visual==null)return;
            // Its rest is read here, not as it fell: the boss's own death hands its body back from whatever pose its last move left it in, after this was begun.
            if(!settled){settled=true;rest=visual.localPosition;restTurn=visual.localRotation;}
            float k=Mathf.Clamp01((elapsed-reel)/Mathf.Max(.1f,Duration-reel)),gone=Mathf.SmoothStep(0,1,Mathf.InverseLerp(sinks?.55f:.8f,1,k));
            // Reeling: it sags where it stands and bows, and stays so. Then it turns in under half a second and goes, faster as it gets clear. It used to sag and rise again in
            // the reel, and walked off as tall as it fought.
            float bowed=upright?0:Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/Mathf.Max(.05f,reel))),sag=bowed*.25f;
            visual.localRotation=restTurn*Quaternion.Euler(7*bowed,0,0);
            var at=from+heading*(distance*k*k*(3-2*k));
            var zone=ZoneController.Current;if(zone==null||!zone.Flight){var ground=CombatCover.Ground(at+Vector3.up*3);if(Mathf.Abs(ground.y-at.y)<2.5f)at.y=ground.y;}
            transform.SetPositionAndRotation(at,Quaternion.Slerp(facing,Quaternion.LookRotation(heading),Mathf.Clamp01((elapsed-reel*.5f)/.45f)));
            visual.localPosition=rest+Vector3.down*(sag+(upright&&!sinks?0:depth*gone));
            if(gone>0&&!outPlayed&&!string.IsNullOrEmpty(outSound)&&!CueLevels.CuesAsRoundTen){outPlayed=true;AudioManager.PlayAt(outSound,null,at,.3f,AudioBus.SFX,50,.2f);Debug.Log("BOSS_GOES_OUT "+outSound);}
            // What walks out of sight is small when it gets there; what goes down a drain is simply under.
            if(!sinks)transform.localScale=scale*(1-.85f*gone);
        }
    }
}
