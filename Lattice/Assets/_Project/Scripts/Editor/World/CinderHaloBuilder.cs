namespace Lattice.EditorTools
{
    public static class CinderHaloBuilder
    {
        public static void Build()=>BatchTools.Run(()=>{WorldBuilder.Prepare();BuildZone();UnityEngine.Debug.Log("CINDER_HALO_OK");});
        // Keep the regular rebuild entry point on the reviewed dimensional plan.
        public static void BuildZone()=>StationRedesign.BuildExterior();
    }
}
