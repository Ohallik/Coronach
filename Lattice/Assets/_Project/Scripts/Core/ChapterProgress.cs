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
        /// a party saved with nobody standing, which would load into an endless defeat, and what a later flag cost a save for good.
        /// It runs on every read and writes nothing itself, so each repair sets the flag that stops it: the next save holds both, and it's paid once.</summary>
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
            // Until November 6 a later flag played a speaker's later words as the first talk and still marked them met, and a first node never plays twice.
            // Her cache has only this flag to wait on, and pays once by itself (SalvageField), so it's safe whatever she has or hasn't said.
            if(Has(state,"met.Neve")&&!Has(state,"neve.cache"))Restore(state,"neve.cache");
            // His gift and his flag are one unbroken run of commands in every revision of HalFirst, and nothing else sets the flag, so met without it proves the gift was never given.
            // Ilo's first meeting sets nothing and gives nothing: it can't be proved lost, and there's nothing to give back.
            if(Has(state,"met.Hal")&&!Has(state,"hal.lesson")){Gift(state,"ScrapAlloy",12);Gift(state,"LatticeFilament",3);Restore(state,"hal.lesson");}
            // Reaching Tallow with the chart is what takes Pell out of Lanternwick (a FlagAll in TallowDrift), and the homecoming is only heard there. A save from before that flag
            // has heard it and still has her on the square, packing, until the next dock at Tallow.
            if(Has(state,"chapter2.homecoming")&&!Has(state,"pell.aboard"))Restore(state,"pell.aboard");
        }
        static void Restore(GameState state,string flag){state.flags[flag]=true;UnityEngine.Debug.Log("SAVE_REPAIRED "+flag);}
        /// <summary>What Inventory.Give does to a material, which Core can't call. No quest gathers these two, so there's nothing to report.</summary>
        static void Gift(GameState state,string id,int amount)
        {
            state.materials.TryGetValue(id,out int held);state.materials[id]=held+amount;
            state.collectedMaterials.TryGetValue(id,out int total);state.collectedMaterials[id]=total+amount;
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
