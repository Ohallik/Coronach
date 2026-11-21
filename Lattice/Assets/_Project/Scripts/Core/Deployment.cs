using System.Collections.Generic;
namespace Lattice.Core
{
    /// <summary>Three in the party, two out (chapter 6 on, when Idra joins). The pair on the field is chosen on the Party page, in a safe zone or at a bench; the third
    /// rests: no body anywhere, their Integrity and Charge kept as they left the field, their Sync rising with the others' (a kill pays the whole party).
    /// The choice is <see cref="GameState.deployed"/>. A party the field holds whole has no choice to make: its save has no value there, any value it has is passed
    /// over, and everything reads as it did when the party was the field. Who is out is always given in the party's own order, so a hero keeps the same side of the
    /// other whoever chose whom first, and <see cref="GameState.activeMember"/> stays what it always was: a place in the party.</summary>
    public static class Deployment
    {
        /// <summary>How many heroes the field holds.</summary>
        public const int Size=2;
        /// <summary>More heroes than the field holds: somebody rests, and the Party page says who.</summary>
        public static bool Choosing(GameState state)=>state!=null&&state.party!=null&&state.party.Count>Size;
        /// <summary>The heroes out on the field, in the party's order. With no choice made (a save from before the third hero, or the day she joins unseen) it's the first two.</summary>
        public static List<MemberState> Of(GameState state)
        {
            var field=new List<MemberState>();if(state==null||state.party==null)return field;
            if(!Choosing(state)){field.AddRange(state.party);return field;}
            var chosen=new List<MemberState>();
            if(state.deployed!=null)foreach(string id in state.deployed){var member=Find(state,id);if(member!=null&&!chosen.Contains(member)&&chosen.Count<Size)chosen.Add(member);}
            // A name the party no longer has, or one hero named twice, leaves a place: whoever comes first in the party takes it, so the field is never short.
            foreach(var member in state.party)if(chosen.Count<Size&&member!=null&&!chosen.Contains(member))chosen.Add(member);
            foreach(var member in state.party)if(chosen.Contains(member))field.Add(member);
            return field;
        }
        /// <summary>The heroes resting, in the party's order.</summary>
        public static List<MemberState> Resting(GameState state)
        {
            var resting=new List<MemberState>();if(!Choosing(state))return resting;
            var field=Of(state);foreach(var member in state.party)if(member!=null&&!field.Contains(member))resting.Add(member);
            return resting;
        }
        public static bool Out(GameState state,string id){var member=Find(state,id);return member!=null&&Of(state).Contains(member);}
        /// <summary>Where the save's lead stands among the heroes out. With the whole party out it's the save's own number, as it was read before there was a choice
        /// (whoever asks clamps it); a lead who is resting, which only a save edited by hand can hold, gives the first hero out.</summary>
        public static int FieldIndex(GameState state)
        {
            if(!Choosing(state))return state!=null?state.activeMember:0;
            int lead=state.activeMember>=0&&state.activeMember<state.party.Count?Of(state).IndexOf(state.party[state.activeMember]):-1;
            return lead<0?0:lead;
        }
        /// <summary>The hero's place in the party, which is what the save's lead is counted in; -1 for nobody of that name.</summary>
        public static int PartyIndex(GameState state,string id)
        {
            if(state!=null&&state.party!=null)for(int i=0;i<state.party.Count;i++)if(state.party[i]!=null&&state.party[i].id==id)return i;
            return -1;
        }
        /// <summary>One hero comes out and one goes to rest. If the one leaving led, the one arriving leads: the player is never left steering nobody.
        /// False, and nothing changed, unless the first is resting and the second is out.</summary>
        public static bool Trade(GameState state,string incoming,string outgoing)
        {
            if(!Choosing(state)||incoming==outgoing||Out(state,incoming)||!Out(state,outgoing)||Find(state,incoming)==null)return false;
            bool led=state.activeMember==PartyIndex(state,outgoing);
            var pair=new List<string>();foreach(var member in Of(state))pair.Add(member.id==outgoing?incoming:member.id);
            state.deployed=pair;if(led)state.activeMember=PartyIndex(state,incoming);
            return true;
        }
        /// <summary>Names the pair outright: a fixture's way (DevLoadout, a replay route's loadout), not a player's. False, and nothing changed, unless it names
        /// exactly as many different heroes of this party as the field holds. A party with no choice to make is asked nothing and keeps no value.</summary>
        public static bool Set(GameState state,IList<string> ids)
        {
            if(state==null||state.party==null||ids==null)return false;
            int room=state.party.Count<Size?state.party.Count:Size;if(ids.Count!=room)return false;
            for(int i=0;i<ids.Count;i++){if(Find(state,ids[i])==null)return false;for(int j=0;j<i;j++)if(ids[j]==ids[i])return false;}
            if(!Choosing(state))return true;
            state.deployed=new List<string>(ids);Settle(state);
            return true;
        }
        /// <summary>The save's lead is somebody out. A lead left pointing at a resting hero would have the menus equip one hero while the pad steered another.</summary>
        public static void Settle(GameState state)
        {
            if(!Choosing(state))return;
            var field=Of(state);if(field.Count==0)return;
            if(state.activeMember<0||state.activeMember>=state.party.Count||!field.Contains(state.party[state.activeMember]))state.activeMember=state.party.IndexOf(field[0]);
        }
        static MemberState Find(GameState state,string id)
        {
            if(state!=null&&state.party!=null&&id!=null)foreach(var member in state.party)if(member!=null&&member.id==id)return member;
            return null;
        }
    }
}
