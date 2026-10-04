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
    }
}
