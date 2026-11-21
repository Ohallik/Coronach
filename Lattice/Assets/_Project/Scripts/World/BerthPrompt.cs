using Lattice.Core;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// One of a <see cref="Berth"/>'s two prompts (ch10-wiring 6.1): its pad, the way in, offered in the zone's own form while nobody is berthed and the pad would take the party;
    /// or its launch, the way out, offered on the berth while the party stands on it and no hand is on a hold. The launch answers with a boss bar up, always; the pad when its berth is
    /// the fight's (<see cref="Berth.inFight"/>). A pad that refuses (an enemy awake near the lead, a boss fight it isn't part of) isn't offered, and says why in the log once.
    /// </summary>
    public sealed class BerthPrompt:InteractionPrompt
    {
        public Berth berth;
        /// <summary>The launch, not the pad.</summary>
        public bool wayOut;
        string refused;
        public override bool InFight=>wayOut||berth!=null&&berth.inFight;
        public override bool Available
        {
            get
            {
                if(berth==null||!isActiveAndEnabled)return false;
                if(wayOut)return berth.CanLaunch;
                bool open=berth.CanDock(out var why);
                if(!open&&why!=null&&why!=refused)Debug.Log("BERTH_REFUSED "+berth.id+" "+why);
                refused=open?null:why;
                // A shelf out of its fight's turn is offered all the same, saying which shelf comes first (or next), as a locked warp says what opens it; pressed, it takes nobody.
                if(!open&&why==Berth.Turn&&berth.OutOfTurn(out var words)){prompt=words;return true;}
                if(open&&prompt!=berth.PadWords)prompt=berth.PadWords;
                return open;
            }
        }
        public override void Interact(){if(berth==null)return;if(wayOut)berth.LaunchPressed();else berth.DockPressed();}
    }
}
