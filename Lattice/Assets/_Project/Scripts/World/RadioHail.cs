using Lattice.Combat;
using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>A voice on the radio the first time the party reaches a place ready to hear it: a few lines from barks.json, one every few
    /// seconds, over the flying and not instead of it, so nothing stops and no route has to read it. Once per save.</summary>
    public sealed class RadioHail:MonoBehaviour
    {
        /// <summary>Long enough for a bark to be read and gone (BarkPanel holds one 3.5 s), and for the arrival's own line to pass.</summary>
        const float Gap=4.2f,Settle=1.6f;
        public string speaker,heardFlag,silencedBy;public string[] situations=System.Array.Empty<string>(),needs=System.Array.Empty<string>();public float range=16;
        float next=-1;int said;
        void Update()
        {
            var services=GameServices.Current;var party=PartyController.Current;
            if(services==null||party==null||party.Active==null||said>=situations.Length)return;
            // The gap waits with the line it follows, which BarkPanel holds through a pause: run on behind the menu, the next line came straight over it.
            if(services.Input.Blocked){if(next>=0)next+=Time.unscaledDeltaTime;return;}
            if(said==0)
            {
                if(services.Flags.GetBool(heardFlag)||!string.IsNullOrEmpty(silencedBy)&&services.Flags.GetBool(silencedBy))return;
                foreach(var need in needs)if(!services.Flags.GetBool(need))return;
                if(Vector3.Distance(party.Active.transform.position,transform.position)>range)return;
                if(next<0){next=Time.unscaledTime+Settle;return;}
            }
            if(Time.unscaledTime<next)return;
            // Heard from the first line on: a warp taken mid-hail doesn't play it again.
            if(said==0)services.Flags.SetBool(heardFlag,true);
            BarkService.Play(speaker,situations[said],true);said++;next=Time.unscaledTime+Gap;
        }
    }
}
