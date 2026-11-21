using Lattice.Combat;
using UnityEngine;
namespace Lattice.World
{
    /// <summary>
    /// The water of an arena that stands behind gates: the Walking Canopy's basin. It is the arena's water as a boss sees it (<see cref="IArenaWater"/>, registered while enabled), over a
    /// <see cref="WaterVolume"/> that does the wading: a share of full, 1 at <see cref="fullLevel"/> and 0 at <see cref="emptyLevel"/>, moved over a time.
    /// Left alone it follows the gates (<see cref="FloodGate.Held"/>): three gates drain it by thirds, and whatever stands above the falling level is dry floor from then on. A boss
    /// that wants the water for a move of its own asks for a level through the same interface; the next gate to break takes it back to what the gates hold.
    /// A won arena loads drained, because its gates load down.
    /// </summary>
    public sealed class FloodBasin:MonoBehaviour,IArenaWater
    {
        public WaterVolume water;
        public float fullLevel,emptyLevel;
        /// <summary>How long a third of the basin takes to go when a gate does.</summary>
        public float seconds=2.4f;
        /// <summary>Off, only whoever calls <see cref="SetLevel"/> moves the water.</summary>
        public bool followsGates=true;
        /// <summary>The share of full the water is at or on its way to.</summary>
        public float Level{get;private set;}=-1;
        /// <summary>The height that share stands at.</summary>
        public float Target=>Mathf.Lerp(emptyLevel,fullLevel,Mathf.Clamp01(Level));
        float held=-1;
        void OnEnable()=>ArenaWater.Register(this);
        void OnDisable()=>ArenaWater.Unregister(this);
        public void SetLevel(float fraction,float time)
        {
            fraction=Mathf.Clamp01(fraction);if(water==null||Mathf.Approximately(fraction,Level))return;
            bool first=Level<0;Level=fraction;water.MoveTo(Target,first?0:Mathf.Max(0,time));
            if(!first)Debug.Log("BASIN_LEVEL "+fraction.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture));
        }
        /// <summary>Ground under water: a point on the floor, not a body's middle. A film over it doesn't count.</summary>
        public bool Submerged(Vector3 point)=>water!=null&&water.Depth(point)>.03f;
        void Update()
        {
            if(water==null)return;
            // The first reading is the state the zone loaded in, taken at once and with no ceremony; after that, each change in what the gates hold.
            float now=FloodGate.Held;if(Level>=0&&(!followsGates||Mathf.Approximately(now,held)))return;
            held=now;SetLevel(Level<0&&!followsGates?1:now,seconds);
        }
    }
}
