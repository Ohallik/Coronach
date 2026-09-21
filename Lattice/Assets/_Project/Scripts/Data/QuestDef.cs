using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/QuestDef")]
    public sealed class QuestDef:ScriptableObject
    {
        public string id,title,description; public Objective[] steps; public int rewardXp,rewardScrip; public Ingredient[] rewards; public string[] flagsOnComplete;
    }
}
