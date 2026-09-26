namespace Lattice.EditorTools
{
    public static class DecksBuilder
    {
        public static void Build()=>BatchTools.Run(()=>{WorldBuilder.Prepare();BuildZone();UnityEngine.Debug.Log("DECKS_OK");});
        // Keep the regular rebuild entry point on the reviewed dimensional plan.
        public static void BuildZone()=>StationRedesign.BuildDecks();
    }
}
