using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/QuestDef")]
    public sealed class QuestDef:ScriptableObject
    {
        public string id,title,description; public Objective[] steps; public int rewardXp,rewardScrip; public Ingredient[] rewards; public string[] flagsOnComplete;
        /// <summary>Player-facing text for each step (D199): the quest log and task cards show it.</summary>
        public string[] stepText;
    }
}
