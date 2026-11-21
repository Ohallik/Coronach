using Lattice.Core;
using UnityEngine;

namespace Lattice.World
{
    /// <summary>
    /// The lead's word the first time something is seen (chapter 9: "first sight of each" in the manifest's barks, a wreck with its family's colours, the way into the inner spiral).
    /// Once <see cref="encounter"/> has begun, or once the leading hero first comes within <see cref="range"/> of this object (with no encounter), the lead says <see cref="situation"/>,
    /// once a visit; with <see cref="onceFlag"/> set, once ever (the flag is set as it is said, and a save that has it says nothing). Nothing is said with a conversation open, in a boss
    /// fight, or with a line already on the panel (a bark not given priority waits its turn in <see cref="BarkService"/>), and it never needs anything to answer it.
    /// Logs <c>FIRST_SIGHT situation name</c>.
    /// </summary>
    public sealed class FirstSight:MonoBehaviour
    {
        public string situation="";
        /// <summary>Said as this fight begins (its first wake); null: said on coming within <see cref="range"/>.</summary>
        public EncounterVolume encounter;
        public float range=18;
        /// <summary>Said once ever when set; empty: once each time the zone is entered.</summary>
        public string onceFlag="";
        /// <summary>Not said until this flag is set (empty: always).</summary>
        public string requiredFlag="";
        bool said;
        public bool Said=>said;
        void Update()
        {
            var services=GameServices.Current;var party=Lattice.Combat.PartyController.Current;
            if(said||services==null||party==null||party.Active==null||string.IsNullOrEmpty(situation)||services.Input.Blocked)return;
            if(!string.IsNullOrEmpty(onceFlag)&&services.Flags.GetBool(onceFlag)){said=true;return;}
            if(!string.IsNullOrEmpty(requiredFlag)&&!services.Flags.GetBool(requiredFlag))return;
            if(encounter!=null){if(!encounter.Started)return;}
            else{var gap=party.Active.transform.position-transform.position;gap.y=0;if(gap.sqrMagnitude>range*range)return;}
            if(Lattice.Combat.BossController.AnyFightUnderway)return;
            said=true;if(!string.IsNullOrEmpty(onceFlag))services.Flags.SetBool(onceFlag,true);
            BarkService.Play(party.Active.character,situation,true);Debug.Log("FIRST_SIGHT "+situation+" "+name);
        }
    }
}
