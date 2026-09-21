"""P2 ScriptableObject schema creation, one type per file."""
from pathlib import Path
R=Path(__file__).resolve().parents[1]/'Lattice/Assets/_Project/Scripts/Data'
defs={
'CameraProfile':'public float pitch=40,yaw=0,fov=30,distance=23,deadZone=0.7f,lookAhead=0.18f,speedZoom=0.12f;',
'Hd2dProfile':'public int internalWidth=960,internalHeight=540; public float tiltStart=0.28f,tiltStrength=1.4f,bloom=0.45f,bloomThreshold=1.1f,vignette=0.16f; public Color tint=Color.white;',
'ZoneDef':'public string id,scene; public ZoneKind kind; public CameraProfile cameraProfile; public Hd2dProfile hd2dProfile; public AudioClip ambientAudio; public string musicId; public Sprite mapIcon; public float flightPlane=1; public string[] safePockets;',
'CharacterDef':'public string id,displayName,speakerId; public bool female; public Stats baseStats,growthPerLevel; public GameObject natural,shaped,flight; public PortraitSet portraitNatural,portraitShaped; public SkillDef[] skills=new SkillDef[4]; public TechPartDef[] startingGear; public Color syncHue=Color.cyan;',
'AttackDef':'public float telegraph=0.6f,range=2.5f,radius=1,damage=18,breakPower=15,cooldown=1.6f; public DamageType type; public bool projectile;',
'EnemyDef':'public string id; public GameObject prefab; public Stats stats; public float integrity=100,breakThreshold=80,speed=3.2f; public AttackDef[] attacks; public DamageType weakness,resistance; public EnemyArchetype archetype; public LootTable lootTable; public int xp=24; public bool boss;',
'TechPartDef':'public string id,displayName; public GearSlot slot; public DamageType damageType; public int tier=1; public Stats baseStats; public AffixDef[] affixPool; public GameObject model; public Sprite icon; public Ingredient[] salvage;',
'MaterialDef':'public string id,displayName; public Sprite icon; public int stack=999;',
'ConsumableDef':'public string id,displayName; public Sprite icon; public int stack=99; public float heal=60,charge;',
'RecipeDef':'public string id,displayName; public Discipline discipline; public int tier=1,unlockLevel=1; public Ingredient[] inputs; public TechPartDef output; public string consumableOutput; public int craftXp=10;',
'SkillDef':'public string id,displayName; public float chargeCost=20,cooldown=4,damage=40,breakPower=35,range=5,radius=2; public DamageType damageType; public bool ground=true,flight=true; public GameObject groundVariant,flightVariant; public Sprite icon;',
'QuestDef':'public string id,title,description; public Objective[] steps; public int rewardXp,rewardScrip; public Ingredient[] rewards; public string[] flagsOnComplete;',
'LootTable':'public LootEntry[] entries;',
'ShopInventory':'public TechPartDef[] parts; public ConsumableDef[] consumables; public MaterialDef[] materials; public int[] prices;',
'AffixDef':'public string id,displayName; public Stats bonus;'
}
for name,body in defs.items():
    (R/(name+'.cs')).write_text('using UnityEngine;\nnamespace Lattice.Data\n{\n    [CreateAssetMenu(menuName="Lattice/'+name+'")]\n    public sealed class '+name+':ScriptableObject\n    {\n        '+body+'\n    }\n}\n',encoding='utf-8')
print('DATA_SCHEMA_OK count='+str(len(defs)))
