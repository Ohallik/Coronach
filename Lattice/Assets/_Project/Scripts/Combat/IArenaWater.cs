using UnityEngine;
namespace Lattice.Combat
{
    /// <summary>
    /// The water of a fighting floor, as a boss needs it: how full it stands, a way to send it to another level, and whether a place is under it. The water itself is the world's
    /// (Lattice.World.WaterVolume, which also does the wading and the slowing); combat can't see that assembly, so whatever owns an arena's water implements this and stands itself in
    /// <see cref="ArenaWater.Current"/> while it is enabled. One arena, one water: the last to register is the one a boss reads. A boss fights without one too (a test floor, a zone not built
    /// yet): then nothing is under water and nothing drains.
    /// </summary>
    public interface IArenaWater
    {
        /// <summary>The share of the flood standing, or on its way to standing: 1 full, 0 drained.</summary>
        float Level{get;}
        /// <summary>Takes the water to a share of full over a time (zero: at once). Asking for the level it already has does nothing.</summary>
        void SetLevel(float fraction,float seconds);
        /// <summary>Is the ground at this point under water now? The point's own height is what's asked about: pass a point on the floor, not a body's middle.</summary>
        bool Submerged(Vector3 point);
    }
    /// <summary>The arena water a boss reads: registered by its owner in OnEnable, withdrawn in OnDisable.</summary>
    public static class ArenaWater
    {
        static IArenaWater current;
        public static IArenaWater Current{get{if(current is Object owner&&owner==null)current=null;return current;}}
        public static void Register(IArenaWater water)=>current=water;
        public static void Unregister(IArenaWater water){if(ReferenceEquals(current,water))current=null;}
        public static float Level=>Current!=null?Current.Level:0;
        public static bool Submerged(Vector3 point)=>Current!=null&&Current.Submerged(point);
        public static void SetLevel(float fraction,float seconds)=>Current?.SetLevel(fraction,seconds);
    }
}
