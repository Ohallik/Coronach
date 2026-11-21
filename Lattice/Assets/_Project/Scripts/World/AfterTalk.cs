using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// What the party says about something that has just happened, once it is safe to say it: a talk that follows an event, where a discovery waits at a point before one. An anchor root's lurch
    /// settling, a creature driven off, a boss gone out with the water. It plays when <see cref="flag"/> is set and nothing is fighting, a beat after the event (or as soon as the zone has settled,
    /// for a save made between the event and the talk, so a compulsory line can't be missed by quitting on the win), and once it has been heard through, <see cref="heardFlag"/> keeps it from
    /// playing again. The script's own last line may set that flag (a story beat); if it doesn't, this does, and tells the quests. An interrupted talk has not been heard and comes round again.
    /// </summary>
    public sealed class AfterTalk:MonoBehaviour
    {
        public string flag,node,speaker="Sela",heardFlag;
        /// <summary>Seconds from the event to the first word.</summary>
        public float delay=2;
        /// <summary>Only while the leading hero is this near this object (zero: anywhere in the zone).</summary>
        public float range;
        /// <summary>Plays in the middle of a fight, without waiting for it to end (chapter 8: Vessa Korl writes off her squad in front of them, as she falls back). A conversation stops every
        /// enemy's clock and nothing lands on a hero through it, so the fight picks up where it was.</summary>
        public bool duringFight;
        float at=-1;bool unsetSeen,pending;
        void Update()
        {
            var services=GameServices.Current;if(services==null||pending||string.IsNullOrEmpty(flag)||string.IsNullOrEmpty(node)||string.IsNullOrEmpty(heardFlag))return;
            if(services.Flags.GetBool(heardFlag)){enabled=false;return;}
            if(!services.Flags.GetBool(flag)){unsetSeen=true;at=-1;return;}
            if(at<0)at=Time.time+(unsetSeen?delay:.6f);
            if(Time.time<at||services.Input.Blocked||!duringFight&&BossController.AnyFightUnderway)return;
            var party=PartyController.Current;var lead=party!=null?party.Active:null;if(lead==null||!lead.Health.Alive)return;
            if(!duringFight&&EnemyBrain.AwakeNear(lead.transform.position,EnemyBrain.InteractionCalm))return;
            if(range>0){var offset=lead.transform.position-transform.position;offset.y=0;if(offset.sqrMagnitude>range*range)return;}
            var asked=services.State;pending=true;
            if(!Npc.Say(node,speaker,completed=>
            {
                if(this==null)return;pending=false;at=Time.time+2;
                var now=GameServices.Current;if(!completed||now==null||!ReferenceEquals(asked,now.State))return;
                if(!now.Flags.GetBool(heardFlag)){now.Flags.SetBool(heardFlag,true);RpgServices.Quests.Report(ObjectiveKind.Flag,heardFlag);}
                now.Saves.Save("autosave",now.State);Debug.Log("AFTER_TALK "+node);
            }))pending=false;
        }
        void OnDisable()=>pending=false;
    }
}
