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

        public static float MaxIntegrity(int level)=>120+18*(UnityEngine.Mathf.Clamp(level,1,40)-1);
        static readonly System.Text.RegularExpressions.Regex PartId=new("^(Edges|Emitters|Frames|Drives|Modules)T[0-9]$");
        /// <summary>Puts right what older builds wrote: gels, cells and parts stranded in materials where they can't be used,
        /// and a party saved with nobody standing, which would load into an endless defeat.</summary>
        public static void Repair(GameState state)
        {
            // A damaged file can hold null where a list belongs. Read as it stood it threw where the title reads its saves, outside Read's guard, and the title never came up.
            state.materials??=new();state.collectedMaterials??=new();state.consumables??=new();state.craftingXp??=new();state.questSteps??=new();state.questCounts??=new();state.parts??=new();
            foreach(var member in state.party){member.equipped??=new();member.gear??=new();}
            foreach(string id in new[]{"RepairGel","ChargeCell"})
                if(state.materials.TryGetValue(id,out int count))
                {state.consumables.TryGetValue(id,out int held);state.consumables[id]=held+count;state.materials.Remove(id);}
            foreach(string id in new System.Collections.Generic.List<string>(state.materials.Keys))
            {
                if(!PartId.IsMatch(id))continue;
                for(int i=0;i<state.materials[id];i++)state.parts.Add(new Lattice.Data.PartInstance{definitionId=id});
                state.materials.Remove(id);
            }
            if(state.party.Count>0&&state.party.TrueForAll(m=>m.integrity<=0))foreach(var member in state.party)member.integrity=MaxIntegrity(member.level);
        }
        public static bool Migrate(GameState state)
        {
            if(state==null||state.party==null||state.party.Count==0||state.party.Contains(null)||state.flags==null)return false;
            if(state.version==SaveVersion)return true;
            if(state.version!=1)return false;
            if(Has(state,"warpkey"))state.flags[LegacyAccess]=true;
            else state.flags.Remove(LegacyAccess);
            state.version=SaveVersion;
            return true;
        }
    }
}
