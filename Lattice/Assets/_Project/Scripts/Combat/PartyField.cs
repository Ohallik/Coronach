using System.Collections.Generic;
using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>The bodies on the field, kept to the pair the save names (<see cref="Deployment"/>). A hero who is resting has no body, in any zone: so
    /// <see cref="PartyController.members"/> goes on meaning "the heroes out", and everything that walks it (a boss's marks, the defeat screen's "is anyone standing",
    /// the camera's framing, a party item, the partner's line on the HUD) is right about a party of three without knowing there is one. The third is in the story by
    /// their portrait and their lines, as anyone who speaks is; a third body walking behind in a town is not built.</summary>
    public static class PartyField
    {
        /// <summary>What a refused press on the Party page says, anywhere the pair can't be changed.</summary>
        public const string Refusal="The party can only be changed somewhere safe or at a bench.";
        /// <summary>A hero who goes to rest while down comes back as a revive would have left them: nobody is carried into the next fight at nothing.</summary>
        const float CarriedOff=.35f;
        /// <summary>Whether who is out may be changed here: in a safe zone, or a sheltered pocket of a wild one (nothing fights in either), or at a bench, which only
        /// answers with nothing awake near it. Never from the pause menu in the open: a fresh hero for a spent one mid-dungeon would be a third life.</summary>
        public static bool MayChange(bool atBench)=>atBench||ZoneController.Current!=null&&!ZoneController.Current.Combat;
        /// <summary>The Party page's press on a hero's row. One who is resting comes out in the partner's place (the lead stays the lead); one who is out goes to rest
        /// and the hero resting takes their place, lead and all. True with what happened, false with why not; refused, nothing has changed.</summary>
        public static bool Choose(PartyController party,string id,bool atBench,out string said)
        {
            var state=GameServices.Current.State;
            if(!Deployment.Choosing(state)||Deployment.PartyIndex(state,id)<0){said="";return false;}
            if(!MayChange(atBench)){said=Refusal;return false;}
            string incoming,outgoing;
            if(Deployment.Out(state,id)){var resting=Deployment.Resting(state);if(resting.Count==0){said="";return false;}incoming=resting[0].id;outgoing=id;}
            else
            {
                // The lead is the hero the pad is steering: the one who makes room is whoever stands beside them.
                var field=Deployment.Of(state);int lead=Deployment.FieldIndex(state);
                incoming=id;outgoing=field[field.Count>1?(lead+1)%field.Count:0].id;
            }
            if(!Deployment.Trade(state,incoming,outgoing)){said="";return false;}
            if(party!=null)Reform(party);
            said=$"{DisplayNames.Speaker(incoming)} is out. {DisplayNames.Speaker(outgoing)} rests.";
            Debug.Log("PARTY_DEPLOYED "+string.Join(",",state.deployed));
            return true;
        }
        /// <summary>A third hero who joins comes out at once, in the partner's place, so they can be played where they joined (Idra, in the dig, with the couplings
        /// still to break). Who rests after that is the Party page's.</summary>
        public static void Join(PartyController party,string id)
        {
            var state=GameServices.Current.State;var partner=party.Partner;
            if(Deployment.Choosing(state)&&!Deployment.Out(state,id)&&partner!=null)Deployment.Trade(state,id,partner.character);
            var member=state.party.Find(m=>m.id==id);
            // The conversation's own cards (a quest it moved on) go first: this is what the talk brought, not the answer to a press (Notices.Pay).
            if(member!=null&&FirstFour(member))Notices.Pay(DisplayNames.Speaker(id).ToUpperInvariant()+" JOINS",Welcome(member));
            Reform(party);
        }
        /// <summary>The four a hero who joins late brings into their slots (D255: she joins at the party's Sync on the same ladder, so at Sync 16 Idra knows nine). Two that
        /// hit, a line and a break; the dome that shields both; and the flare that mends or raises the partner, the rescue officer's own. Tow Line and the rest wait on
        /// the Skills page. A hero with nobody here keeps their definition's first four, as the first two always have.</summary>
        static readonly (string hero,SkillKind[] four)[] JoinsWith={("Idra",new[]{SkillKind.Breach,SkillKind.RescueFlare,SkillKind.Shelter,SkillKind.SlagHammer})};
        /// <summary>Puts a joining hero's four in their slots, once, before anyone has chosen any. True when it did.</summary>
        public static bool FirstFour(MemberState member)
        {
            if(member==null||member.skillLoadout!=null&&member.skillLoadout.Count>0)return false;
            foreach(var (hero,four) in JoinsWith)
            {
                if(hero!=member.id)continue;
                var all=SkillBook.All(hero);bool any=false;
                for(int slot=0;slot<four.Length&&slot<SkillBook.Slots;slot++)
                {
                    var skill=System.Array.Find(all,s=>s.kind==four[slot]);
                    if(skill!=null&&SkillBook.Knows(member,skill))any|=SkillBook.Equip(member,slot,skill);
                }
                return any;
            }
            return false;
        }
        /// <summary>One card for the join, not one for each of the skills she arrives knowing: her Sync, her four, and how many more wait on the Skills page.</summary>
        static string Welcome(MemberState member)
        {
            var four=new List<string>();for(int slot=0;slot<SkillBook.Slots;slot++){var skill=SkillBook.Equipped(member.id,slot);if(skill!=null)four.Add(skill.displayName);}
            int more=0;foreach(var skill in SkillBook.All(member.id))if(SkillBook.Knows(member,skill)&&!four.Contains(skill.displayName))more++;
            string set=four.Count>1?string.Join(", ",four.GetRange(0,four.Count-1))+" and "+four[four.Count-1]:string.Join("",four);
            return $"Sync {member.level}. Skills set: {set}."+(more>0?$" {more} more are on the Skills page.":"");
        }
        /// <summary>Makes the field the pair the save names. Whoever stays keeps their body and everything about it; whoever comes out stands where the one going to
        /// rest stood; the lead is the save's. Called with the game held (a menu, the end of a conversation), never in the middle of a blow.</summary>
        public static void Reform(PartyController party)
        {
            var state=GameServices.Current.State;var field=Deployment.Of(state);
            var before=party.members??new CombatActor[0];var next=new CombatActor[field.Count];var leaving=new List<CombatActor>();
            foreach(var body in before)
            {
                if(body==null)continue;
                int place=field.FindIndex(m=>m.id==body.character);
                if(place>=0&&next[place]==null)next[place]=body;else leaving.Add(body);
            }
            var beside=party.members!=null&&party.members.Length>0?party.Active:null;int taken=0;
            for(int i=0;i<next.Length;i++)
            {
                if(next[i]!=null)continue;
                var from=taken<leaving.Count?leaving[taken++]:null;
                float spacing=beside!=null&&beside.flight?HeroCollision.Apart(beside.character,field[i].id):2;
                var at=from!=null?from.transform.position:beside!=null?beside.transform.position+Vector3.right*spacing:party.transform.position;
                next[i]=ActorFactory.Hero(field[i].id,at);next[i].transform.SetParent(party.transform);
                if(from!=null)next[i].transform.rotation=from.transform.rotation;
            }
            foreach(var body in leaving)
            {
                // What they had as they left the field is what they have when they come back.
                var member=state.party.Find(m=>m.id==body.character);
                if(member!=null){member.integrity=body.Health.Alive?body.Health.integrity:Levels.MaxIntegrity(member.level)*CarriedOff;member.charge=body.charge;}
                body.gameObject.SetActive(false);Object.Destroy(body.gameObject);
            }
            party.members=next;party.index=Mathf.Clamp(Deployment.FieldIndex(state),0,Mathf.Max(0,next.Length-1));
            party.Apply();
        }
    }
}
