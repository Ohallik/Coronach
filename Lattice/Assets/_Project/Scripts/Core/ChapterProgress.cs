namespace Lattice.Core
{
    /// <summary>The nursery supplies the living route missing from the survey key.
    /// Older saves keep access already earned before this chapter connection.</summary>
    public static class ChapterProgress
    {
        public const int SaveVersion=2;
        public const string LegacyAccess="legacy.gulletAccess";
        static bool Has(GameState state,string flag)=>state.flags!=null&&state.flags.TryGetValue(flag,out bool value)&&value;
        public static bool NeedsNursery(GameState state)=>!Has(state,"hushwell.nursery")&&!Has(state,LegacyAccess);
        public static bool CanEnterGullet(GameState state)=>Has(state,"warpkey")&&!NeedsNursery(state);

        public static bool Migrate(GameState state)
        {
            if(state==null||state.party==null||state.party.Count==0||state.flags==null)return false;
            if(state.version==SaveVersion)return true;
            if(state.version!=1)return false;
            if(Has(state,"warpkey"))state.flags[LegacyAccess]=true;
            else state.flags.Remove(LegacyAccess);
            state.version=SaveVersion;
            return true;
        }
    }
}
