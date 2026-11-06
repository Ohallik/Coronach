using System;
using Lattice.Core;

namespace Lattice.World
{
    /// <summary>
    /// Side stories (D199): who says what when a side quest is at a given point. A speaker's ordinary tiers (first, repeat,
    /// post, final) stay as authored; when one of these beats applies, its conversation plays instead. Steps advance on
    /// flags the side conversations set themselves (<c>ObjectiveKind.Flag</c>), never on "talked to X", so the main
    /// story's own conversation with the same person can never tick a side step by accident. A beat never preempts a
    /// first meeting: each one requires its speaker to have been met already.
    /// </summary>
    public static class SideStories
    {
        readonly struct Beat
        {
            public readonly string speaker,node;public readonly Func<GameState,bool> when;
            public Beat(string speaker,string node,Func<GameState,bool> when){this.speaker=speaker;this.node=node;this.when=when;}
        }
        static int Step(GameState state,string quest)=>state.questSteps.TryGetValue(quest,out var step)?step:-1;
        static bool Flag(GameState state,string flag)=>state.flags.TryGetValue(flag,out var value)&&value;
        static bool Met(GameState state,string speaker)=>Flag(state,"met."+speaker);

        static readonly Beat[] Beats=
        {
            // An overdue parcel (NeveParcel): Neve has held one of Sorrel's missed deliveries; the Survivor answers it.
            new("Survivor","SurvivorParcel",s=>Step(s,"NeveParcel")==0&&Met(s,"Survivor")),
            new("Neve","NeveParcelReturn",s=>Step(s,"NeveParcel")==1),
            new("Neve","NeveParcelOffer",s=>Step(s,"NeveParcel")<0&&Met(s,"Neve")),
            new("Neve","NeveAfterParcel",s=>Flag(s,"quest.NeveParcel.complete")&&Flag(s,"bossdown.Burrower")&&!Flag(s,"neve.afterParcel")),
            // Quiet ridges (SorrelBeacons): the survey crew's evacuation beacons still call machinery toward the outpost.
            new("Survivor","SurvivorBeaconsDone",s=>Step(s,"SorrelBeacons")==1),
            new("Survivor","SurvivorBeaconsOffer",s=>Step(s,"SorrelBeacons")<0&&Flag(s,"bossdown.Burrower")&&Met(s,"Survivor")&&(!Flag(s,"hushwell.nursery")||Flag(s,"sorrel.nurseryHeard"))),
            new("Survivor","SurvivorAfterParcel",s=>Flag(s,"quest.NeveParcel.complete")&&Flag(s,"bossdown.Burrower")&&!Flag(s,"sorrel.afterParcel")&&(!Flag(s,"hushwell.nursery")||Flag(s,"sorrel.nurseryHeard"))),
            // A berth for two (OdaBerth): Oda's family filed for Nacre; their berth token waits at Orrin's office.
            new("Orrin","OrrinBerth",s=>Step(s,"OdaBerth")==0&&Met(s,"Orrin")),
            new("Oda","OdaBerthReturn",s=>Step(s,"OdaBerth")==1),
            new("Oda","OdaBerthOffer",s=>Step(s,"OdaBerth")<0&&Met(s,"Oda")&&Met(s,"Orrin")),
            new("Oda","OdaAfterBerth",s=>Flag(s,"quest.OdaBerth.complete")&&!Flag(s,"oda.afterBerth")),
            // The chapter's last beat: once Tallow knows it is moving, the Compact calls on the beacon band (Act II's hook).
            new("Keeper","CompactHail",s=>Flag(s,"tallow.driftDiscussed")&&!Flag(s,"compact.hailHeard")),
            // The Lantern Road (chapter 2, D202): Abri remembers the canyon town whose lanterns guided moving stations.
            new("Keeper","AbriLanternHome",s=>Flag(s,"lanternwick.chartGiven")&&!Flag(s,"chapter2.homecoming")),
            new("Keeper","AbriLanternTale",s=>Flag(s,"compact.hailHeard")&&Step(s,"LanternRoad")<0&&!Flag(s,"lanternwick.chartGiven")),
            new("Keeper","AbriLanternRepeat",s=>Step(s,"LanternRoad")>=0&&Step(s,"LanternRoad")<2),
            new("Keeper","AbriLanternWaiting",s=>Step(s,"LanternRoad")>=2&&!Flag(s,"lanternwick.chartGiven")),
            new("Keeper","AbriLanternAfter",s=>Flag(s,"chapter2.homecoming")),
            // Pell's lamp (PellsLamp): the apprentice lost her mother's lamp to a grazer in the gorge.
            new("Pell","PellLampReturn",s=>Step(s,"PellsLamp")==1),
            new("Pell","PellLampOffer",s=>Step(s,"PellsLamp")<0&&Met(s,"Pell")&&Step(s,"LanternRoad")>=3),
            new("Pell","PellLampAfter",s=>Flag(s,"pell.lampReturned")&&!Flag(s,"lanternwick.chartGiven")),
            // Ishen's record (IshensRecord): three record plates from the Sunken Relay.
            new("Ishen","IshenRecordReturn",s=>Step(s,"IshensRecord")==1),
            // Unsworn rations (UnswornRations): Corra feeds a Muster deserter hiding in the orchard shed.
            new("Brekk","BrekkRations",s=>Step(s,"UnswornRations")==1),
            // The captain's surrender: heard once, wherever she is standing (D202).
            new("Hessa","HessaYields",s=>Flag(s,"bossdown.HessaKorl")&&!Flag(s,"relay.hessaYielded")),
            new("Corra","CorraRationsOffer",s=>Step(s,"UnswornRations")<0&&Met(s,"Corra")&&Step(s,"LanternRoad")>=3),
        };

        /// <summary>The side-story conversation for this speaker now, or null for their ordinary tiers.</summary>
        public static string NodeFor(string speaker,GameState state)
        {
            if(state==null||string.IsNullOrEmpty(speaker))return null;
            foreach(var beat in Beats)if(beat.speaker==speaker&&beat.when(state))return beat.node;
            return null;
        }
    }
}
