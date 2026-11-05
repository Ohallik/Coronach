using UnityEngine;
namespace Lattice.Data
{
    /// <summary>What a skill does when released. The first eight are the original kit; the rest are learned with Sync
    /// level (D201). Append only: assets serialize the ordinal.</summary>
    public enum SkillKind
    {
        ArcCleave,EmberStep,PulseBurst,Overdrive,ThreadLance,ScatterBloom,StaticNet,Refract,
        AnchorSlam,TetherHook,ForgeSpiral,Faultline,SurveyDrone,WaypointBlink,GravityFold,Starfall,
    }

    [CreateAssetMenu(menuName="Lattice/SkillDef")]
    public sealed class SkillDef:ScriptableObject
    {
        public string id,displayName; public float chargeCost=20,cooldown=4,damage=40,breakPower=35,range=5,radius=2; public DamageType damageType; public bool ground=true,flight=true; public GameObject groundVariant,flightVariant; public Sprite icon;
        public SkillKind kind;
        /// <summary>The HUD's short label ("Cleave"); the menus use <see cref="displayName"/>.</summary>
        public string shortName;
        [TextArea] public string description;
        /// <summary>Sync level at which the skill is learned. The starting four are level 1.</summary>
        public int unlockLevel=1;
        /// <summary>Generated icon sheet under Resources/UI/Generated and its cell (0-3 top row, 4-7 bottom row).</summary>
        public string iconSheet="skill-icons"; public int iconCell;
    }
}
