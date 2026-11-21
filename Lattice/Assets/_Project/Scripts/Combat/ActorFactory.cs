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
        static readonly System.Collections.Generic.Dictionary<Color,Material> energy=new();
        /// <summary>The material of a body made of energy, shared by everything of one colour.</summary>
        public static Material Energy(Color rim)
        {
            if(energy.TryGetValue(rim,out var material)&&material!=null)return material;
            material=new Material(Resources.Load<Shader>("Effects/EnergyBubble"));material.SetColor("_BaseColor",rim);energy[rim]=material;return material;
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
            root.AddComponent<HeroCollision>();
            root.AddComponent<GroundMotor>();root.AddComponent<FlightMotor>();
            var form=root.AddComponent<FormController>();var definition=GameCatalog.Find<CharacterDef>(id);
            string material=id=="Taren"?"Taren":"Sela";
            form.natural=definition!=null&&definition.natural!=null?Object.Instantiate(definition.natural,root.transform):Visual("Natural",PrimitiveType.Capsule,root.transform,new Vector3(.7f,.95f,.7f),Vector3.up*.95f,material);
            form.shaped=definition!=null&&definition.shaped!=null?Object.Instantiate(definition.shaped,root.transform):Visual("Shaped",PrimitiveType.Capsule,root.transform,new Vector3(.85f,.95f,.8f),Vector3.up*.95f,material);
            form.flight=definition!=null&&definition.flight!=null?Object.Instantiate(definition.flight,root.transform):Visual("Flight",PrimitiveType.Capsule,root.transform,new Vector3(.7f,.9f,.7f),Vector3.up*.75f,material);
            form.shaped.SetActive(false);form.flight.SetActive(false);
            root.AddComponent<HitFlash>();
            root.AddComponent<HeroWeaponVfx>();root.AddComponent<HeroBuffPresentation>();root.AddComponent<MeleeContact>();root.AddComponent<RangedContact>();root.AddComponent<GroundSkillContact>();root.AddComponent<FlightDashContact>();root.AddComponent<FlightEmitter>();root.AddComponent<FlightSkillContact>();root.AddComponent<HeroSkills>();root.AddComponent<FlightThrusters>();root.AddComponent<PlayerBrain>();root.AddComponent<PartnerBrain>();root.AddComponent<DefeatPresentation>();return actor;
        }
        public static EnemyBrain Enemy(EnemyDef definition,Vector3 position)
        {
            // A boss no BossController file registers: in the editor it stops here, before anything is built; a player logs it and fights a body with no specials.
            if(definition.boss)BossController.Require(definition.id);
            var root=new GameObject(definition.id);root.transform.position=position;
            // The capsule is the definition's to say; unsaid, an ordinary body's, or a boss's at the width its own file registers (a person's .8, a rig's 1.4).
            var cc=root.AddComponent<CharacterController>();cc.height=definition.capsuleHeight>0?definition.capsuleHeight:definition.boss?3:1.5f;cc.radius=definition.capsuleRadius>0?definition.capsuleRadius:definition.boss?BossController.CapsuleRadius(definition.id):.55f;cc.center=Vector3.up*cc.height*.5f;cc.minMoveDistance=0;
            // The difficulty's share of its integrity is taken here, as the body is made, and nowhere after: the definition's own number on Standard, and a bar that never jumps under a change.
            var health=root.AddComponent<Health>();health.id=definition.id;health.maximum=health.integrity=Difficulty.EnemyIntegrity(definition.integrity);health.weakness=definition.weakness;health.resistance=definition.resistance;health.breakThreshold=definition.breakThreshold;
            AddBodyHurtbox(root,health,cc);
            // A swarm cloud with no model is its motes: that is its body, not a stand-in for one. So is a rider with none (chapter 7's Fibre Wisp, a mote that rides the fibres).
            var visual=definition.prefab!=null?Object.Instantiate(definition.prefab,root.transform):definition.behaviour==EnemyBehaviourKind.SwarmCloud||definition.behaviour==EnemyBehaviourKind.Rider?SwarmMotes.Body(root.transform,definition,cc):Visual("ART_PENDING_"+definition.id,definition.archetype==EnemyArchetype.Mine?PrimitiveType.Sphere:PrimitiveType.Capsule,root.transform,new Vector3(cc.radius*2,cc.height*.5f,cc.radius*2),cc.center,"Enemy");
            // Only what's drawn: these two were too small to find on a dark sky, and their hit shapes are the capsule above.
            // The definition says how much (EnemyDef.drawnScale); these two were drawn larger before it could.
            float drawn=definition.drawnScale>0&&definition.drawnScale!=1?definition.drawnScale:definition.id=="ChoristerDart"?1.5f:definition.id=="Shellmine"?1.4f:1;
            if(drawn!=1)visual.transform.localScale*=drawn;
            if(definition.id=="Cantor")SerpentSegments.CenterVisual(visual,SerpentSegments.CenterHeight);
            // A body with no rig hovers or skitters as its definition says (EnemyDef.motion); the first four moved before it could.
            var idle=definition.motion!=EnemyMotion.None?definition.motion:definition.id=="Scrapmite"?EnemyMotion.Skitter:definition.id.StartsWith("Chorister")||definition.id=="MusterSkiff"?EnemyMotion.Hover:EnemyMotion.None;
            if(idle!=EnemyMotion.None){var motion=visual.AddComponent<ProceduralMotion>();motion.motion=idle==EnemyMotion.Skitter?ProceduralMotion.Motion.Skitter:ProceduralMotion.Motion.Hover;}
            var enemy=root.AddComponent<EnemyBrain>();enemy.definition=definition;
            if(definition.archetype==EnemyArchetype.Sentinel)root.AddComponent<EnemyArmorPresentation>();
            // Before DefeatPresentation: a death hands the body back unposed before that takes its measure.
            root.AddComponent<EnemyTellPose>();
            // The same: a blow's squash and recoil come off the body before a death measures it.
            root.AddComponent<HitFlash>();
            root.AddComponent<LootDrop>().definition=definition;
            if(definition.boss)root.AddComponent<BossController>();if(definition.id=="Cantor")root.AddComponent<SerpentSegments>();
            // Mother of Doors' coil is laid as her body is made, so it is there while she lies on show before her fight (a held boss's own Start waits for the fight).
            if(definition.id=="MotherOfDoors")root.AddComponent<MotherCoil>();
            root.AddComponent<DefeatPresentation>().visual=visual.transform;
            if(definition.id=="Cantor")root.AddComponent<CantorCollar>().Initialize(definition,root.GetComponent<SerpentSegments>());
            if(definition.id=="Burrower")root.AddComponent<BurrowerShutdown>();
            if(BossController.Occludes(definition.id))root.AddComponent<BossOcclusion>();
            return enemy;
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
