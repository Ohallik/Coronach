using System;
using System.Collections.Generic;

namespace Lattice.Core
{
    [Serializable]
    public sealed class GameState
    {
        public int version=1;
        public string savedAtUtc;
        public string zone="Hub_CinderHalo";
        public string spawn="Arrival";
        public int activeMember;
        public List<MemberState> party=new(){new MemberState{ id="Taren" }};
        public Dictionary<string,bool> flags=new();
        public Dictionary<string,int> materials=new();
        public Dictionary<string,int> consumables=new(){{"RepairGel",3}};
        public Dictionary<string,int> craftingXp=new();
        public Dictionary<string,int> questSteps=new();
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
    }
}
