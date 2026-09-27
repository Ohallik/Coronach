using Lattice.Core;
using Lattice.Data;
using Lattice.Rpg;
using UnityEngine;
namespace Lattice.Combat
{
    public static class ActorFactory
    {
        public static GameObject Visual(string name,PrimitiveType type,Transform parent,Vector3 scale,Vector3 position,string material)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localScale=scale;go.transform.localPosition=position;
            Object.Destroy(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Blockout/"+material);return go;
        }
        public static CombatActor Hero(string id,Vector3 position)
        {
            var root=new GameObject(id);root.transform.position=position;
            var cc=root.AddComponent<CharacterController>();cc.height=1.9f;cc.radius=.35f;cc.center=Vector3.up*.95f;cc.minMoveDistance=0;cc.stepOffset=.35f;
            var health=root.AddComponent<Health>();health.id=id;health.friendly=true;
            var state=GameServices.Current.State.party.Find(m=>m.id==id);
            health.maximum=Levels.MaxIntegrity(state?.level??1);health.integrity=state?.integrity??health.maximum;
            AddBodyHurtbox(root,health,cc);
            var actor=root.AddComponent<CombatActor>();actor.character=id;actor.damage=50+5*((state?.level??1)-1);actor.charge=state?.charge??0;
            root.AddComponent<GroundMotor>();root.AddComponent<FlightMotor>();
            var form=root.AddComponent<FormController>();var definition=GameCatalog.Find<CharacterDef>(id);
            string material=id=="Taren"?"Taren":"Sela";
            form.natural=definition!=null&&definition.natural!=null?Object.Instantiate(definition.natural,root.transform):Visual("Natural",PrimitiveType.Capsule,root.transform,new Vector3(.7f,.95f,.7f),Vector3.up*.95f,material);
            form.shaped=definition!=null&&definition.shaped!=null?Object.Instantiate(definition.shaped,root.transform):Visual("Shaped",PrimitiveType.Capsule,root.transform,new Vector3(.85f,.95f,.8f),Vector3.up*.95f,material);
            form.flight=definition!=null&&definition.flight!=null?Object.Instantiate(definition.flight,root.transform):Visual("Flight",PrimitiveType.Capsule,root.transform,new Vector3(.7f,.9f,.7f),Vector3.up*.75f,material);
            form.shaped.SetActive(false);form.flight.SetActive(false);
            root.AddComponent<HeroWeaponVfx>();root.AddComponent<MeleeContact>();root.AddComponent<RangedContact>();root.AddComponent<PlayerBrain>();root.AddComponent<PartnerBrain>();root.AddComponent<DefeatPresentation>();return actor;
        }
        public static EnemyBrain Enemy(EnemyDef definition,Vector3 position)
        {
            var root=new GameObject(definition.id);root.transform.position=position;
            var cc=root.AddComponent<CharacterController>();cc.height=definition.boss?3:1.5f;cc.radius=definition.boss?1.4f:.55f;cc.center=Vector3.up*cc.height*.5f;cc.minMoveDistance=0;
            var health=root.AddComponent<Health>();health.id=definition.id;health.maximum=health.integrity=definition.integrity;health.weakness=definition.weakness;health.resistance=definition.resistance;health.breakThreshold=definition.breakThreshold;
            AddBodyHurtbox(root,health,cc);
            var visual=definition.prefab!=null?Object.Instantiate(definition.prefab,root.transform):Visual("ART_PENDING_"+definition.id,definition.archetype==EnemyArchetype.Mine?PrimitiveType.Sphere:PrimitiveType.Capsule,root.transform,new Vector3(cc.radius*2,cc.height*.5f,cc.radius*2),cc.center,"Enemy");
            if(definition.id=="Cantor")SerpentSegments.CenterVisual(visual,SerpentSegments.CenterHeight);
            if(definition.id=="Scrapmite"||definition.id.StartsWith("Chorister")){var motion=visual.AddComponent<ProceduralMotion>();motion.motion=definition.id=="Scrapmite"?ProceduralMotion.Motion.Skitter:ProceduralMotion.Motion.Hover;}
            var enemy=root.AddComponent<EnemyBrain>();enemy.definition=definition;
            if(definition.archetype==EnemyArchetype.Sentinel)root.AddComponent<EnemyArmorPresentation>();
            root.AddComponent<LootDrop>().definition=definition;
            if(definition.boss)root.AddComponent<BossController>();if(definition.id=="Cantor")root.AddComponent<SerpentSegments>();
            root.AddComponent<DefeatPresentation>().visual=visual.transform;return enemy;
        }
        static void AddBodyHurtbox(GameObject root,Health health,CharacterController movement)
        {
            root.AddComponent<Hurtbox>().owner=health;
            // CharacterController's skin/query shape is a movement detail, not
            // the damage surface. A trigger retains the declared body envelope
            // without adding a second movement obstacle.
            var body=root.AddComponent<CapsuleCollider>();body.isTrigger=true;
            body.center=movement.center;body.height=movement.height;body.radius=movement.radius;
        }
    }
}
