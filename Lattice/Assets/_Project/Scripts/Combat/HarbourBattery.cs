using System.Collections.Generic;
using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// A gun battery of Harrowgate's war harbour (chapter 8), on the flak mole or the shore: an empty object at its guns, numbered as the Warmaster calls them ("Battery two. Battery four.").
    /// When he calls the guns, each battery that still answers throws a shell onto a marked circle on the water, and the shell is drawn from here. Fewer answer each call, as the Unsworn take
    /// them. With none in the room (the flight yard) the shells come from beyond the frame. The harbour: docs/maps/Harrowgate.md, "Pursuit contract".
    /// </summary>
    public sealed class HarbourBattery:MonoBehaviour
    {
        public static readonly List<HarbourBattery> All=new();
        /// <summary>Its number, 1 to 4, as he calls it.</summary>
        public int index;
        void OnEnable(){if(!All.Contains(this))All.Add(this);}
        void OnDisable()=>All.Remove(this);
        public static HarbourBattery Numbered(int index){foreach(var battery in All)if(battery!=null&&battery.index==index)return battery;return null;}
    }
}
