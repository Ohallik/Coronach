using System.Collections.Generic;
using Lattice.Combat;
using Lattice.Core;
using Lattice.World;
using UnityEngine;

namespace Lattice.UI
{
    // The driver's hands for Mother of Doors in her room (route 63, tools/mooring_routes.py; MooringBossReplayTests). Ordinary pad presses and what a player reads off the screen, as
    // everywhere in the driver: the game still does to the party whatever it would do to a person who misjudged it.
    public sealed partial class ContinuousReview
    {
        /// <summary>The driver as it was before these hands, for a test's broken state (MooringBossReplayTests sets it by reflection and puts it back): no route sets it.</summary>
        static bool motherHandsOff;

        // ---------- where a hero stands to take one of her clamps ----------
        /// <summary>How round a hero's body is, with a margin, and how far from the seat's middle the driver will go looking for floor it fits on (inside the seat's 1.6 m and the hold's
        /// 2 m, so the prompt is offered there and the hands stay on).</summary>
        const float MotherBody=.45f,MotherSeatRoom=1.2f;
        static readonly Dictionary<BridleClamp,Vector3> motherStands=new();
        static readonly Collider[] motherHits=new Collider[16];
        /// <summary>Where the lead stands to take one of her clamps' weight: the seat, or, where the seat is too near something solid for a body (on Sorrel's shelves the works stand
        /// five centimetres in front of it), the nearest floor behind it, away from her, that a body fits on. Chapter 7's spot (1.38 m aside of the Warden's fibre, reached to within .2 m)
        /// stood inside the works' face: in the player's run the lead pushed into the works for five rounds, .42 m short, with "Take the clamp's weight" offered the whole time, and
        /// never pressed (nov14-recon32 s63). Her clamps have no fibre to stand clear of; behind the works is where her wave doesn't reach. Null for any other clamp.</summary>
        static Vector3? MotherStand(BridleClamp clamp)
        {
            if(motherHandsOff||clamp==null||clamp.settledBy!="MotherOfDoors")return null;
            if(motherStands.TryGetValue(clamp,out var kept)&&(kept-clamp.transform.position).sqrMagnitude<MotherSeatRoom*MotherSeatRoom*1.5f)return kept;
            var seat=clamp.transform.position;var back=-clamp.transform.forward;back.y=0;back=back.sqrMagnitude>.01f?back.normalized:Vector3.back;
            var stand=seat;
            for(float out_=0;out_<=MotherSeatRoom;out_+=.05f)
            {
                var at=seat+back*out_;
                if(!MotherSolidAt(at)){stand=at;break;}
            }
            motherStands[clamp]=stand;
            Debug.Log("QUALITY_MOTHER_STAND clamp "+clamp.index+" at "+stand.ToString("F2")+" ("+Vector3.Distance(stand,seat).ToString("0.00")+" m behind the seat)");
            return stand;
        }
        /// <summary>Her clamps are walked to straight across the deck they stand on: the fourth shelf's floor is baked where it was built, and docked where her strain had carried it
        /// (four metres round her ring) the floor's way to its seat led off the deck, and the lead stood against the berth's edge for seventy seconds (MooringBossReplayTests' third run).</summary>
        static bool MotherWalksStraight(BridleClamp clamp)=>!motherHandsOff&&clamp!=null&&clamp.settledBy=="MotherOfDoors";
        /// <summary>The game offers the lead her clamp's prompt now ("Take the clamp's weight", the one the interact button answers): a person presses then, wherever in the seat's ring
        /// he stands, and the driver does.</summary>
        static bool MotherOffered(BridleClamp clamp)=>MotherWalksStraight(clamp)&&PromptService.Interaction==clamp&&clamp.Available;
        /// <summary>Something solid (not a body, not a trigger) where a hero standing on this floor point would be.</summary>
        static bool MotherSolidAt(Vector3 floor)
        {
            int n=Physics.OverlapCapsuleNonAlloc(floor+Vector3.up*(MotherBody+.3f),floor+Vector3.up*1.7f,MotherBody,motherHits,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<n;i++)
            {
                var hit=motherHits[i];if(hit==null)continue;
                if(hit.GetComponentInParent<CombatActor>()!=null||hit.GetComponentInParent<EnemyBrain>()!=null||hit.GetComponentInParent<Health>()!=null)continue;
                return true;
            }
            return false;
        }

        // ---------- her markings, read as they come ----------
        /// <summary>Whether a marking of a boss nobody strikes lands on the lead within <paramref name="soon"/>, and the way out of it (ContinuousReview.Mooring.cs asks, for each one): her wave
        /// as it runs, then the Tollkeeper's own (ContinuousReview.Tollkeeper.cs), and otherwise as it always was (its tell nearly out, and over the lead).</summary>
        bool UnstruckLands(Telegraph marking,BossController boss,CombatActor lead,float soon,out Vector3 way)
        {
            if(MotherLands(marking,boss,lead,soon,out way) is bool hers)return hers;
            if(TollLands(marking,boss,lead,soon,out way) is bool its)return its;
            var here=lead.transform.position;way=default;
            if(marking.Remaining>soon||!marking.Contains(here))return false;
            way=Aside(marking,here);return true;
        }
        /// <summary>The roll into a running band starts this long before it reaches the lead: a ship rolling at her through a 4 m band running at 12 m/s is through it in under a fifth of a
        /// second, and a roll is untouchable for all of its .3 s (on foot .25).</summary>
        const float MotherBandLead=.1f;
        /// <summary>Her wave (<c>MOTHER_WAVE</c>): a band filling at her coil's edge, then running out at 12 m/s across every lane. Read as a person sees it: behind cover (a rock, the clamp's
        /// works in front of its seat) it never reaches him and nothing is done; otherwise it is met, a roll toward her through it as it arrives (away from it, it follows faster than a
        /// roll and is on him as the roll ends). The player's run read it only once it was over the ship, the frame it struck: every wave of the fight struck both ships (nov14-recon32 s63,
        /// a ninth of a bar each, seven in the two minutes before each defeat). Null for every other marking.</summary>
        bool? MotherLands(Telegraph marking,BossController boss,CombatActor lead,float soon,out Vector3 way)
        {
            way=default;
            if(motherHandsOff||!boss.MotherRoom||marking.Spreads<=0||marking.Shape!=Telegraph.Kind.Ring)return null;
            var here=lead.transform.position;
            var cover=marking.Origin+Vector3.up*3;
            if(!CombatCover.Clear(cover,here+Vector3.up*.8f,.2f))return false;
            // Inside the band as it fills, as it goes; outside it, as it arrives: either way the roll is still going while the band passes over him.
            float lands=marking.Reaches(here);
            if(float.IsPositiveInfinity(lands)||lands>MotherBandLead*Time.timeScale)return false;
            way=marking.Origin-here;way.y=0;
            if(way.sqrMagnitude<.01f)way=Aside(marking,here);
            return true;
        }

        // ---------- a dock step presses only at its own pad ----------
        /// <summary>A step that ends docked at a berth presses A as it flies (in flight A is also Fire), and A answers whatever pad is offered: a roll out of her lash carried the lead within
        /// reach of the third shelf's pad on his way to the fourth's, the press docked him on a shelf whose clamp was already open, and the step walked at the fourth's pad on foot for the
        /// rest of the fight (MooringBossReplayTests' fifth run). A person reads which shelf the prompt names: with another berth's pad offered, A isn't pressed.</summary>
        bool WrongPad()
        {
            if(mooringHandsOff||step==null)return false;
            string condition=Berthing(step.until)?step.until:Berthing(step.stopWhen)?step.stopWhen:null;
            if(condition==null||!condition.StartsWith("docked:"))return false;
            return PromptService.Interaction is BerthPrompt offered&&!offered.wayOut&&offered.berth!=null&&offered.berth.id!=condition.Substring(7);
        }

        // ---------- a hand on her clamp, and her marking about to land on it ----------
        /// <summary>The lead's hands are on one of her clamps and a marking of hers is about to land on him (every other sweep puts a wedge over the seat, the lash and the heave reach it;
        /// her wave doesn't, behind the works): a person lets go and rolls, and takes the weight again after. Chapter 7's hand kept the pad still whatever came, which in her room is a
        /// sweep's blow and a stagger, and the hold lost all the same.</summary>
        static bool MotherLetsGo(PartyController party,bool evading)
        {
            if(motherHandsOff||!evading||party==null||party.Active==null)return false;
            var held=BridleClamp.Held;return held!=null&&held.Holder==party.Active&&held.settledBy=="MotherOfDoors";
        }
    }
}
