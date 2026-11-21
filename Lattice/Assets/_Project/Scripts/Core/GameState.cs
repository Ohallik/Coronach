using System;
using System.Collections.Generic;
using Lattice.Data;

namespace Lattice.Core
{
    [Serializable]
    public sealed class GameState
    {
        public int version=ChapterProgress.SaveVersion;
        public string savedAtUtc;
        public string zone="Hub_CinderHalo";
        public string spawn="Arrival";
        public int activeMember;
        public string quickItem="RepairGel";
        public List<MemberState> party=new(){new MemberState{ id="Taren" }};
        public Dictionary<string,bool> flags=new();
        public Dictionary<string,int> materials=new();
        public Dictionary<string,int> collectedMaterials=new();
        public Dictionary<string,int> consumables=new(){{"RepairGel",3}};
        public Dictionary<string,int> craftingXp=new();
        public Dictionary<string,int> questSteps=new();
        public Dictionary<string,int> questCounts=new();
        public List<PartInstance> parts=new();
        public int scrip=120;
        public float playtime;
        /// <summary>The draught and the meal the party is under (consumable ids) and the seconds each has left. One of each at a time; they ride through a zone change and a save.</summary>
        public string draught,meal;
        public float draughtLeft,mealLeft;
        /// <summary>How hard enemies hit and how much they take (<see cref="Difficulty"/>). A save written before the setting has no value and reads Standard, which is how it was played;
        /// so does a new game and every fixture's state, since nothing outside the save remembers a choice.</summary>
        public DifficultySetting difficulty;
        /// <summary>The ids of the two heroes out on the field once the party is three (<see cref="Deployment"/>, which is the only thing that reads or writes it). A save from
        /// before the third hero has no value, and a party of two is never asked: both are out, as they always were.</summary>
        public List<string> deployed;
    }
    [Serializable]
    public sealed class MemberState
    {
        public string id;
        public int level=1;
        public int xp;
        public float integrity=120;
        public float charge;
        public List<string> gear=new();
        public Dictionary<GearSlot,string> equipped=new();
        public string module2;
        /// <summary>Equipped skill ids for RB+A/B/X/Y; null or short means the character's starting four (D201).</summary>
        public List<string> skillLoadout;
        /// <summary>Releases per skill id: mastery rises with use, Ys style.</summary>
        public Dictionary<string,int> skillUses;
        /// <summary>The EXTRA gauge, 0 to 100 (ExtraGauge): it never runs down, so it's saved with the hero.</summary>
        public float extra;
    }
}
