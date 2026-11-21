using Lattice.Combat;
using Lattice.Core;
namespace Lattice.UI
{
    /// <summary>The Party page's hero rows once the party is three: each is a control that sends its hero to rest or brings them out (PartyField.Choose decides and
    /// refuses; this is the rows' names and words). With two in the party none of this is asked and the page is the one it always was: two rows that say and do nothing.
    /// A row goes by its hero (<c>Hero_Idra</c>), never by a number, so a replay route finds it whoever joined first and whoever is resting.</summary>
    public static class PartyPairUi
    {
        public const string RowPrefix="Hero_",RestingMark="Resting  ·  ";
        public static string RowName(string id)=>RowPrefix+id;
        /// <summary>The hero's line as the page always gave it, led by the one thing that differs: they aren't out.</summary>
        public static string Line(GameState state,MemberState member,string line)=>Deployment.Out(state,member.id)?line:RestingMark+line;
        /// <summary>The same without the XP count, for a row the mark has made too long: Sync, Integrity and the two stats decide who goes out, and the count doesn't.</summary>
        public static string Short(string line)
        {
            int from=line.IndexOf("   XP ",System.StringComparison.Ordinal),to=from<0?-1:line.IndexOf("   ",from+3,System.StringComparison.Ordinal);
            return to>from?line.Remove(from,to-from):line;
        }
        /// <summary>What a press on the row would do, for the note under the list while the cursor is on it: said before the press, as a price is.</summary>
        public static string Says(GameState state,MemberState member,bool atBench)
        {
            if(!PartyField.MayChange(atBench))return PartyField.Refusal;
            string who=DisplayNames.Speaker(member.id);
            if(Deployment.Out(state,member.id)){var resting=Deployment.Resting(state);return resting.Count>0?$"Select to rest {who}: {DisplayNames.Speaker(resting[0].id)} comes out in their place.":"";}
            var field=Deployment.Of(state);int lead=Deployment.FieldIndex(state);
            return field.Count>1?$"Select to bring {who} out: {DisplayNames.Speaker(field[(lead+1)%field.Count].id)} rests.":"";
        }
        /// <summary>The press. True with what happened, false with why it didn't.</summary>
        public static bool Press(MemberState member,bool atBench,out string said)=>PartyField.Choose(PartyController.Current,member.id,atBench,out said);

        // ---------- the HUD: who is waiting ----------
        /// <summary>The HUD's line under the partner's, with three in the party: who is resting, so a player in a fight knows who isn't coming. With two there is no such
        /// line at all (GameHud keeps it hidden), and the vitals are the panel they always were.</summary>
        public static string RestingLine(GameState state)
        {
            var resting=Deployment.Resting(state);if(resting.Count==0)return "";
            var names=new System.Text.StringBuilder(RestingHud);
            for(int i=0;i<resting.Count;i++)names.Append(i==0?"  ":"  ·  ").Append(DisplayNames.Speaker(resting[i].id).ToUpperInvariant());
            return names.ToString();
        }
        public const string RestingHud="RESTING";

        // ---------- the Gear page with three ----------
        /// <summary>With three in the party the Gear page's Swap looks at the next hero, as the Skills page's does, and the field stays as it is: a real swap only ever
        /// reaches the two out, and the one resting has parts to put on too (they were changed only by bringing the hero out and swapping to them). With two it is the
        /// real swap it always was, and its header says "switches member" as it always did.</summary>
        public static bool GearByLook(GameState state)=>Deployment.Choosing(state);
        /// <summary>Whose parts the Gear page puts on: the lead, as ever; with three, whoever the page's Swap has come round to (the page opens on the lead).</summary>
        public static int GearOf(GameState state,int shown)=>GearByLook(state)&&shown>=0&&shown<state.party.Count?shown:state.activeMember;
        /// <summary>The Gear page's header after the hero's name. With two, the clause it always had; with three, whether the hero is resting and whose gear Swap shows next.</summary>
        public static string GearClause(GameState state,int shown,string clause)
        {
            if(!GearByLook(state))return clause;
            int at=GearOf(state,shown);var next=state.party[(at+1)%state.party.Count];
            return (Deployment.Out(state,state.party[at].id)?"":"  ·  resting")+$"  ·  {PromptService.Tag("Swap")} shows {DisplayNames.Speaker(next.id)}'s gear";
        }
    }
}
