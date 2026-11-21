using System.Collections.Generic;
using System.Linq;
using Lattice.World;
using UnityEngine;

namespace Lattice.EditorTools
{
    public static partial class TownLife
    {
        // ---------------------------------------------------------------- the Decks
        /// <summary>Breakfast is the first morning's (until the survey key): a queue at the galley's counter, someone eating standing up, a child weaving between the tables,
        /// the stallholders setting out, two neighbours still at home behind the cabins' low walls. From the survey key on it's a working day: the queue has become shoppers
        /// at the stalls and someone waiting to see Orrin, the child is at school, one neighbour is at the market and the other at Hal's bench. From the Cantor's fall (Tallow
        /// found adrift, the Nacre transfer opening) cabin 01 stands open while its household packs, and from the homecoming they wait in the market's vestibule with their packs
        /// and the hatch is shut on an empty cabin (STORY_CAMPAIGN: "Compact evacuation notices and Nacre transfer departures are already present in the first town").
        /// People here are Vael: chapter 1 and 2's own bodies, none of the four who stand on these decks with names, each in its own dye.</summary>
        internal static readonly Folk[] DecksFolk=
        {
            // The market, its two stalls against the galley wall, each with its seller at its open end and, later, a shopper at its counter.
            new("A stallholder at the west stall","IloBody",-2.8f,9f,200,new Color(.9f,.78f,.6f)),
            new("A stallholder at the east stall","PellBody",2.8f,9f,160,new Color(.72f,.8f,.9f)),
            new("A shopper at the west stall","SurvivorNatural",-5f,7.4f,0,new Color(.8f,.84f,.66f),from:"warpkey"),
            new("A shopper at the east stall","OdaBody",4.4f,7.3f,350,new Color(.86f,.74f,.84f),from:"warpkey"),
            // The galley at breakfast.
            new("At the front of the breakfast queue","BrekkBody",8.6f,15.2f,0,new Color(.76f,.7f,.86f),goneAt:"warpkey"),
            new("In the breakfast queue","SurvivorNatural",8.9f,13.6f,10,new Color(.8f,.84f,.66f),goneAt:"warpkey"),
            new("Eating in the galley","Neve",-7.2f,13.4f,90,new Color(.66f,.8f,.78f),goneAt:"warpkey"),
            // The office: someone waiting to see Orrin, and from the second day the one who was first in the queue.
            new("Visiting the dock office","IloBody",-31.5f,8f,250,new Color(.7f,.76f,.7f)),
            new("Waiting to see Orrin","BrekkBody",-25.3f,5.3f,80,new Color(.76f,.7f,.86f),from:"warpkey"),
            // Home: two cabins lit and lived in on the first morning, seen over their low walls.
            new("At home in cabin 02","OdaBody",-34.4f,15.6f,250,new Color(.86f,.74f,.84f),goneAt:"warpkey"),
            new("At the bench in cabin 03","CorraBody",-20f,12.4f,0,new Color(.7f,.86f,.72f),goneAt:"warpkey"),
            // Arrivals, the workshop and receiving.
            new("Checking the arrivals freight","PellBody",-24f,-13.6f,120,new Color(.86f,.8f,.64f)),
            new("Hal's apprentice","CorraBody",37.5f,6f,90,new Color(.7f,.86f,.72f),from:"warpkey"),
            new("Signing for receiving","BrekkBody",36.4f,-14f,90,new Color(.86f,.72f,.62f)),
            // Cabin 01's household: packing in its open doorway from the Cantor's fall, waiting for the transfer with their packs from the homecoming.
            new(CabinPacker,"IshenBody",-34.15f,12.85f,90,new Color(.78f,.72f,.66f),from:"bossdown.Cantor",goneAt:"chapter2.homecoming"){door=true},
            new("Bound for Nacre","IshenBody",6.6f,-13f,200,new Color(.78f,.72f,.66f),carries:"pack",from:"chapter2.homecoming"),
            new("Bound for Nacre with a pack","CorraBody",8.4f,-12.6f,170,new Color(.72f,.74f,.88f),carries:"pack",from:"chapter2.homecoming"),
            new("A child bound for Nacre","IloBody",7.6f,-11.6f,190,new Color(.9f,.8f,.7f),.72f,from:"chapter2.homecoming"),
        };
        const string CabinPacker="Packing up cabin 01";
        /// <summary>Errands: a shopper between the two stalls (from the first morning: it's the floor a new player crosses to Mira), freight carried in at arrivals, parts run from the stores to Hal's side, a hauler at receiving, and on the
        /// first morning a child between the galley's tables.</summary>
        internal static readonly Errand[] DecksErrands=
        {
            new("Between the two stalls","Neve",new[]{new Vector2(-3.4f,6.9f),new Vector2(3.1f,6.9f)},3,new Color(.88f,.8f,.62f),true),
            new("Carrying freight in","SurvivorNatural",new[]{new Vector2(-22.6f,-13.4f),new Vector2(-18f,-13.4f)},2.4f,new Color(.7f,.78f,.88f),true,carries:"crate"),
            new("Running parts from the stores","IshenBody",new[]{new Vector2(33.4f,12),new Vector2(33.4f,16)},2,new Color(.86f,.82f,.66f),true,carries:"parcel"),
            new("Hauling at receiving","Neve",new[]{new Vector2(36.3f,-11.6f),new Vector2(32.6f,-11.6f)},2.6f,new Color(.84f,.7f,.66f),true,carries:"crate"),
            new("A child between the tables","PellBody",new[]{new Vector2(-2.9f,12.2f),new Vector2(-2.9f,17.8f)},1.2f,new Color(.9f,.86f,.72f),true,.72f,goneAt:"warpkey"),
        };
        /// <summary>The story's walks on the first morning (MarketOpeningStaging), and the lines from each hull's door to what is in it, which nobody stands on.</summary>
        static IEnumerable<Way> DecksWays()
        {
            Vector2 Plan(Vector3 p)=>new(p.x,p.z);
            yield return new Way("Sela's approach",false,MarketOpeningStaging.SelaApproach.Select(Plan).ToArray());
            yield return new Way("Sela's exit",false,MarketOpeningStaging.SelaExit.Select(Plan).ToArray());
            yield return new Way("Mira's walk home",false,new Vector2(MarketOpeningStaging.MiraAtStall.x,MarketOpeningStaging.MiraAtStall.z),new(-6,-2.6f),new(-2,.6f),new(0,4));
            yield return new Way("the market to the galley",true,new(0,2),new(0,14.5f));
            yield return new Way("the office to the cabins",true,new(-28,3),new(-28,14.5f));
            yield return new Way("the workshop to the stores",true,new(28,2),new(28,15));
            yield return new Way("to Hal's bench",true,new(28,2),new(31,4.2f));
            yield return new Way("to the repair cradle",true,new(28,2),new(25,4.3f));
        }
        static (Folk[],Errand[],IEnumerable<Way>) Decks(ZoneKit kit,Transform root)
        {
            foreach(var f in DecksFolk)Stand(kit,root,f);
            foreach(var e in DecksErrands)Walk(kit,root,e);
            CabinOneOpens(root);
            return (DecksFolk,DecksErrands,DecksWays());
        }
        /// <summary>Cabin 01's hatch, open while its household packs: the closed panel is put away for that stretch of the story, and their packed crate stands on the deck
        /// inside the threshold beside the one packing, so the doorway is a home being emptied and not a way in (the gaps either side are under half a metre).</summary>
        static void CabinOneOpens(Transform root)
        {
            var hatch=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name=="Closed private pressure hatch"&&Vector2.Distance(new Vector2(t.position.x,t.position.z),new Vector2(-33,12.25f))<.8f).ToArray();
            if(hatch.Length!=1)throw new System.InvalidOperationException("Cabin 01's hatch: expected one closed panel at (-33, 12.25), found "+hatch.Length);
            var shut=new GameObject("Cabin 01 is shut (when)",typeof(Whereabouts)).GetComponent<Whereabouts>();shut.transform.SetParent(root,false);
            shut.from=shut.goneAt="";shut.awayFrom="bossdown.Cantor";shut.backAt="chapter2.homecoming";shut.live=false;shut.targets=new[]{hatch[0].gameObject};
            var crate=WorldBuilder.Piece("DeckCrate",new Vector3(-34.25f,.325f,11.6f),new Vector3(.65f,.65f,.65f),"Rock");crate.name="Cabin 01's packed crate";
            crate.transform.SetParent(root,true);foreach(var t in crate.GetComponentsInChildren<Transform>(true))t.gameObject.isStatic=true;
            var packer=root.GetComponentsInChildren<Whereabouts>(true).Single(w=>w.name==CabinPacker+" (when)");packer.targets=packer.targets.Append(crate).ToArray();crate.SetActive(false);
        }

        // ---------------------------------------------------------------- Tallow Drift
        /// <summary>A working refuge (STORY_CAMPAIGN: "a small, messy town with a frightened population"): someone running cable in the west life-support room and someone
        /// mending the lift's air in the east one, both seen over the low walls from the arrival; a quartermaster at the stores; someone eating at the mess table; someone resting in the west rest cabin and a
        /// parent and child in the east one. Once the party has found the docking line leads nowhere, the one who was resting is at the refuge's console with a neighbour,
        /// worried: its residents have not been consulted. Everything here keeps off the routes that loop the refuge floor, the old station tours' among them.</summary>
        internal static readonly Folk[] TallowFolk=
        {
            new("Counting the stores","Neve",11.6f,3f,290,new Color(.86f,.76f,.68f)),
            // At the mess table's south side, the one spot on the refuge floor the routes' loops leave two metres clear of.
            new("Eating at the mess table","OdaBody",-4.3f,-3.95f,0,new Color(.76f,.84f,.72f)),
            new("Resting","CorraBody",-7.4f,10.8f,180,new Color(.7f,.74f,.84f),goneAt:"tallow.lineObserved"),
            new("Worried at the console","CorraBody",-6.5f,6.4f,150,new Color(.7f,.74f,.84f),from:"tallow.lineObserved"),
            new("Asking what it means","BrekkBody",-5.4f,5.2f,120,new Color(.8f,.7f,.6f),from:"tallow.lineObserved"),
            new("A parent in the east cabin","OdaBody",7.8f,11f,200,new Color(.84f,.8f,.7f)),
            new("A child in the east cabin","Neve",4.6f,11f,150,new Color(.9f,.72f,.72f),.72f),
        };
        internal static readonly Errand[] TallowErrands=
        {
            new("Running cable in life support","IloBody",new[]{new Vector2(-9.2f,-8.3f),new Vector2(-5.4f,-8.3f)},2.2f,new Color(.8f,.82f,.7f),true,carries:"coil"),
            new("Mending the lift's air","IshenBody",new[]{new Vector2(6f,-8.4f),new Vector2(9.8f,-8.4f)},3,new Color(.78f,.8f,.68f),true),
        };
        static IEnumerable<Way> TallowWays()
        {
            yield return new Way("to Pell",false,new(0,1),new(-4,-1.2f));
            yield return new Way("to the repair cradle",false,new(0,2),new(6,2));
        }
        static (Folk[],Errand[],IEnumerable<Way>) Tallow(ZoneKit kit,Transform root)
        {
            foreach(var f in TallowFolk)Stand(kit,root,f);
            foreach(var e in TallowErrands)Walk(kit,root,e);
            return (TallowFolk,TallowErrands,TallowWays());
        }
    }
}
