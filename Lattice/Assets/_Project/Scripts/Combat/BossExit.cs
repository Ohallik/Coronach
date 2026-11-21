using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// Where a boss that is driven off goes (BossController.Ending.Leaves): an empty object a builder stands at the way out, with the boss's id on it. The Seedthane's is upriver
    /// of Seedfall, the Floodcrown's is the basin's drain mouth. With none in the scene the boss still leaves: straight away from the party.
    /// </summary>
    public sealed class BossExit:MonoBehaviour
    {
        public static readonly List<BossExit> All=new();
        /// <summary>The EnemyDef id of the boss that leaves this way.</summary>
        public string boss;
        /// <summary>It goes under as it reaches here (a drain, deep water) rather than walking on out of sight.</summary>
        public bool sinks;
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable()=>All.Remove(this);
        public static BossExit For(string id){foreach(var exit in All)if(exit!=null&&exit.boss==id)return exit;return null;}
    }
}
