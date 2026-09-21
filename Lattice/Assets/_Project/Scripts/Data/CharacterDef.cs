using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/CharacterDef")]
    public sealed class CharacterDef:ScriptableObject
    {
        public string id,displayName,speakerId; public bool female; public Stats baseStats,growthPerLevel; public GameObject natural,shaped,flight; public PortraitSet portraitNatural,portraitShaped; public SkillDef[] skills=new SkillDef[4]; public TechPartDef[] startingGear; public Color syncHue=Color.cyan;
    }
}
