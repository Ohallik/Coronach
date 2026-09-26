namespace Lattice.EditorTools
{
    public static class TallowDriftBuilder
    {
        public static void Build()=>BatchTools.Run(()=>{WorldBuilder.Prepare();BuildZone();UnityEngine.Debug.Log("TALLOW_DRIFT_OK");});
        // Keep the regular rebuild entry point on the reviewed dimensional plan.
        public static void BuildZone()=>TallowRedesign.Build();
    }
}
