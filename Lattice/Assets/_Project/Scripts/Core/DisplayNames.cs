using System.Collections.Generic;
using System.Text.RegularExpressions;
using Lattice.Data;

namespace Lattice.Core
{
    /// <summary>One place that turns stable ids (save keys, Yarn speakers, definition ids) into the words a player reads
    /// (D200). Screens never print a raw id such as "ScrapAlloy" or "Hub_CinderHalo".</summary>
    public static class DisplayNames
    {
        static readonly Dictionary<string,string> items=new(),speakers=new();
        static readonly Dictionary<string,string> zones=new()
        {
            {"Hub_CinderHalo","Cinder Halo"},{"Hub_Decks","The Decks"},{"Sorrel_Ridges","Sorrel Ridges"},{"Hushwell","Hushwell"},
            {"Gullet_Tunnel","The Gullet"},{"TallowApproach","Tallow Drift Approach"},{"TallowDrift","Tallow Drift"},
            {"Arena_Ground","Proving Yard"},{"Arena_Flight","Proving Berth"},
            {"WickReach","The Wick Reach"},{"Lanternwick","Lanternwick"},{"LanternGorge","Lantern Gorge"},{"SunkenRelay","The Sunken Relay"},
        };
        static readonly Dictionary<string,string> special=new()
        {
            {"WarpKey","Survey Key"},{"FramesT2","Refined Frame"},
        };

        /// <summary>Splits "RidgeCrystal" into "Ridge Crystal" for ids no definition names.</summary>
        public static string Readable(string id)=>Regex.Replace(id??"","(?<=[a-z0-9])(?=[A-Z])"," ").Replace('_',' ');

        public static string Item(string id)
        {
            if(string.IsNullOrEmpty(id))return "";
            if(items.TryGetValue(id,out var name))return name;
            if(!special.TryGetValue(id,out name))
            {
                name=GameCatalog.Find<MaterialDef>(id)?.displayName;
                if(string.IsNullOrWhiteSpace(name))name=GameCatalog.Find<ConsumableDef>(id)?.displayName;
                if(string.IsNullOrWhiteSpace(name))name=GameCatalog.Find<TechPartDef>(id)?.displayName;
                if(string.IsNullOrWhiteSpace(name))name=Readable(id);
            }
            items[id]=name;return name;
        }

        public static string Zone(string id)=>string.IsNullOrEmpty(id)?"":zones.TryGetValue(id,out var name)?name:Readable(id);

        /// <summary>A Yarn speaker id as the character's own name ("Survivor" is Dace).</summary>
        public static string Speaker(string id)
        {
            if(string.IsNullOrEmpty(id))return id;
            if(speakers.TryGetValue(id,out var name))return name;
            var definition=GameCatalog.Find<CharacterDef>(id);
            name=definition!=null&&!string.IsNullOrWhiteSpace(definition.displayName)?definition.displayName:id;
            speakers[id]=name;return name;
        }

        /// <summary>A combat target's id ("SentinelHusk", "Cantor - sweep link") as a label.</summary>
        public static string Target(string id)=>Readable((id??"").Replace(" - "," — "));
    }
}
