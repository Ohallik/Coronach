using UnityEngine;
namespace Lattice.Data
{
    [CreateAssetMenu(menuName="Lattice/SkillDef")]
    public sealed class SkillDef:ScriptableObject
    {
        public string id,displayName; public float chargeCost=20,cooldown=4,damage=40,breakPower=35,range=5,radius=2; public DamageType damageType; public bool ground=true,flight=true; public GameObject groundVariant,flightVariant; public Sprite icon;
    }
}
