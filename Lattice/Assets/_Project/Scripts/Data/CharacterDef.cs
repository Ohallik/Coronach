using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/CharacterDef")]
    public sealed class CharacterDef:ScriptableObject
    {
        public string id,displayName,speakerId; public bool female; public Stats baseStats,growthPerLevel; public GameObject natural,shaped,flight; public PortraitSet portraitNatural,portraitShaped; public SkillDef[] skills=new SkillDef[4]; /// <summary>Skills learned with Sync level beyond the starting four (D201).</summary>
        public SkillDef[] learnable=new SkillDef[0]; public TechPartDef[] startingGear; public Color syncHue=Color.cyan;
        /// <summary>The hero's EXTRA move (S3): its name, words and icon. Never in a loadout slot; released from a full EXTRA gauge.</summary>
        public SkillDef extra;
        /// <summary>Where this hero's guard, dodge and plain attack differ from the first two heroes' (S6a). Left alone, it's theirs.</summary>
        public HeroStyle style=new();
    }
}
