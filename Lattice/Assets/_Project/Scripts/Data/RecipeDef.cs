using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/RecipeDef")]
    public sealed class RecipeDef:ScriptableObject
    {
        public string id,displayName; public Discipline discipline; public int tier=1,unlockLevel=1; public Ingredient[] inputs; public TechPartDef output; public string consumableOutput; public int craftXp=10;
    }
}
