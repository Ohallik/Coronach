using System;
using System.IO;
using Lattice.Combat;
using Lattice.Core;
using Lattice.Data;
using Lattice.UI;
using Lattice.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
namespace Lattice.EditorTools
{
    public static class ArenaBuilder
    {
        const string Root="Assets/_Project/Resources/Definitions/";
        public static void Build()=>BatchTools.Run(()=>
        {
            PipelineConverter.ConvertTo3DInternal();Definitions();Materials();ConfigureHd2d();BuildScene(false);BuildScene(true);
            AssetDatabase.SaveAssets();Debug.Log("ARENAS_BUILD_OK");
        });
        public static T Asset<T>(string id,Action<T> configure)where T:ScriptableObject
        {
            string dir=Root+typeof(T).Name;Directory.CreateDirectory(dir);string path=dir+"/"+id+".asset";
            var data=AssetDatabase.LoadAssetAtPath<T>(path);if(data==null){data=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(data,path);}
            configure(data);EditorUtility.SetDirty(data);return data;
        }
        public static void Definitions()
        {
            var ground=Asset<CameraProfile>("Ground",p=>{p.pitch=40;p.yaw=20;p.distance=19.5f;p.deadZone=.4f;});
            var flight=Asset<CameraProfile>("Flight",p=>{p.pitch=48;p.yaw=0;p.distance=31;p.deadZone=.8f;p.lookAhead=.3f;});
            var warm=Asset<Hd2dProfile>("Warm",p=>{p.tint=new Color(1,.94f,.84f);p.tiltStart=.18f;p.tiltStrength=6;p.bloom=.45f;});
            var cold=Asset<Hd2dProfile>("Cold",p=>{p.tint=new Color(.82f,.94f,1);p.tiltStart=.18f;p.tiltStrength=7;p.bloom=.7f;});
            var deck=Asset<Hd2dProfile>("DeckSoft",p=>{p.tint=new Color(.96f,.98f,1);p.tiltStart=.23f;p.tiltStrength=4;p.bloom=.3f;});
            var halo=Asset<Hd2dProfile>("Halo",p=>{p.tint=new Color(1,.97f,.93f);p.tiltStart=.22f;p.tiltStrength=4.5f;p.bloom=.4f;});
            var tallow=Asset<Hd2dProfile>("Tallow",p=>{p.tint=new Color(.95f,1,.96f);p.tiltStart=.23f;p.tiltStrength=4;p.bloom=.35f;});
            foreach(var name in new[]{"Arena_Ground","Arena_Flight","Hub_CinderHalo","Hub_Decks","Sorrel_Ridges","Gullet_Tunnel","TallowApproach","TallowDrift"})
            {
                bool isFlight=name=="Arena_Flight"||name=="Hub_CinderHalo"||name=="Gullet_Tunnel"||name=="TallowApproach";
                bool safe=name.StartsWith("Hub")||name.StartsWith("Tallow");
                Asset<ZoneDef>(name,z=>{z.id=z.scene=name;z.kind=isFlight?(safe?ZoneKind.SpaceSafe:ZoneKind.SpaceCombat):(safe?ZoneKind.GroundSafe:ZoneKind.GroundCombat);z.cameraProfile=isFlight?flight:ground;z.hd2dProfile=name=="Hub_Decks"?deck:name=="Hub_CinderHalo"?halo:name.StartsWith("Tallow")?tallow:isFlight?cold:warm;});
            }
            foreach(string name in new[]{"Taren","Sela"})Asset<CharacterDef>(name,c=>{c.id=c.displayName=c.speakerId=name;c.female=name=="Sela";c.baseStats=new Stats{output=50,plating=10,response=10,resonance=10,fortune=5};c.growthPerLevel=new Stats{output=5,plating=2,response=1,resonance=2,fortune=1};c.syncHue=name=="Taren"?new Color(1,.65f,.12f):Color.cyan;});
            string[][] skillNames={new[]{"Arc Cleave","Ember Step","Pulse Burst","Overdrive"},new[]{"Thread Lance","Scatter Bloom","Static Net","Refract"}};
            for(int hero=0;hero<2;hero++)
            {
                string character=hero==0?"Taren":"Sela";var skills=new SkillDef[4];
                for(int i=0;i<4;i++)
                {
                    int slot=i;string title=skillNames[hero][i];
                    skills[i]=Asset<SkillDef>(character+"_"+(i+1),s=>{s.id=character+"_"+(slot+1);s.displayName=title;s.chargeCost=20;s.cooldown=slot==3?10:4;s.damageType=slot==1?DamageType.Plasma:slot==2?DamageType.Pulse:character=="Taren"?DamageType.Kinetic:DamageType.Beam;s.ground=s.flight=true;});
                }
                Asset<CharacterDef>(character,c=>c.skills=skills);
            }
            var basic=Asset<AttackDef>("Bite",a=>{a.damage=18;a.range=2.8f;a.telegraph=.65f;a.cooldown=1.7f;a.type=DamageType.Kinetic;});
            var ranged=Asset<AttackDef>("Volley",a=>{a.damage=14;a.range=15;a.telegraph=.8f;a.cooldown=1.6f;a.projectile=true;a.type=DamageType.Beam;});
            string[] names={"Ridgehound","Scrapmite","SentinelHusk","Burrower","ChoristerDart","ChoristerDrifter","Shellmine","Cantor"};
            EnemyArchetype[] ai={EnemyArchetype.PackHunter,EnemyArchetype.Swarm,EnemyArchetype.Sentinel,EnemyArchetype.Spitter,EnemyArchetype.Swarm,EnemyArchetype.Spitter,EnemyArchetype.Mine,EnemyArchetype.Serpent};
            DamageType[] weak={DamageType.Plasma,DamageType.Pulse,DamageType.Kinetic,DamageType.Beam,DamageType.Beam,DamageType.Pulse,DamageType.Kinetic,DamageType.Plasma};
            DamageType[] resist={DamageType.Kinetic,DamageType.Beam,DamageType.Plasma,DamageType.Pulse,DamageType.Pulse,DamageType.Beam,DamageType.Plasma,DamageType.Kinetic};
            for(int i=0;i<names.Length;i++){int n=i;Asset<EnemyDef>(names[i],e=>{e.id=names[n];e.archetype=ai[n];e.weakness=weak[n];e.resistance=resist[n];e.boss=n==3||n==7;e.integrity=e.boss?(n==3?18000:22000):n==2?240:n==1||n==4?40:100;e.breakThreshold=e.boss?(n==3?600:900):80;e.xp=e.boss?240:24;e.speed=n==2?2:3.2f;e.attacks=new[]{n==3||n==5||n==7?ranged:basic};});}
            foreach(string mat in new[]{"ScrapAlloy","LatticeFilament","RidgeCrystal","HuskCore","ChoirResin","CantorPearl"})Asset<MaterialDef>(mat,m=>m.id=m.displayName=mat);
            foreach(string name in names)
            {
                var loot=Asset<LootTable>(name,l=>l.entries=name=="Cantor"?new[]{new LootEntry{id="CantorPearl",count=1,chance=1},new LootEntry{id="ChoirResin",count=6,chance=1}}:
                    name=="SentinelHusk"||name=="Burrower"?new[]{new LootEntry{id="HuskCore",count=name=="Burrower"?2:1,chance=1}}:
                    name.StartsWith("Chorister")?new[]{new LootEntry{id="ChoirResin",count=1,chance=.35f}}:new[]{new LootEntry{id="LatticeFilament",count=1,chance=.25f}});
                Asset<EnemyDef>(name,e=>e.lootTable=loot);
            }
            Asset<ConsumableDef>("RepairGel",d=>{d.id="RepairGel";d.displayName="Repair gel";d.heal=60;});
            var affix=Asset<AffixDef>("Resonant",a=>{a.id=a.displayName="Resonant";a.bonus=new Stats{resonance=4};});
            foreach(Discipline discipline in Enum.GetValues(typeof(Discipline)))
            {
                GearSlot slot=discipline switch{Discipline.Emitters=>GearSlot.Emitter,Discipline.Edges=>GearSlot.Edge,Discipline.Frames=>GearSlot.Frame,Discipline.Drives=>GearSlot.Drive,_=>GearSlot.Module};
                for(int tier=1;tier<=2;tier++)
                {
                    int t=tier;string id=discipline+"T"+t;
                    var part=Asset<TechPartDef>(id,p=>{p.id=id;p.displayName=(t==1?"Scrap ":"Refined ")+slot;p.tier=t;p.slot=slot;p.baseStats=slot==GearSlot.Frame?new Stats{plating=t*8}:slot==GearSlot.Drive?new Stats{response=t*6}:slot==GearSlot.Module?new Stats{resonance=t*5,fortune=t*3}:new Stats{output=t==2?(slot==GearSlot.Edge?30:25):8};p.damageType=slot==GearSlot.Emitter?DamageType.Beam:DamageType.Kinetic;p.affixPool=new[]{affix};p.salvage=new[]{new Ingredient{id="ScrapAlloy",count=t*2}};});
                    Asset<RecipeDef>(id,r=>{r.id=id;r.displayName=part.displayName;r.discipline=discipline;r.tier=t;r.unlockLevel=t;r.output=part;r.inputs=t==1?new[]{new Ingredient{id="ScrapAlloy",count=3}}:new[]{new Ingredient{id="ScrapAlloy",count=6},new Ingredient{id="RidgeCrystal",count=2}};});
                }
            }
            Asset<RecipeDef>("RepairGel",r=>{r.id="RepairGel";r.displayName="Repair gel";r.consumableOutput="RepairGel";r.inputs=new[]{new Ingredient{id="ScrapAlloy",count=2}};});
            Asset<QuestDef>("Sorrel",q=>{q.id="Sorrel";q.title="The silent outpost";q.steps=new[]{new Objective{kind=ObjectiveKind.Reach,target="Sorrel_Ridges",count=1},new Objective{kind=ObjectiveKind.TalkTo,target="Survivor",count=1},new Objective{kind=ObjectiveKind.Kill,target="Burrower",count=1},new Objective{kind=ObjectiveKind.Collect,target="WarpKey",count=1}};q.rewardXp=160;q.rewardScrip=200;q.flagsOnComplete=new[]{"warpkey"};});
            Asset<QuestDef>("MiraCrystals",q=>{q.id="MiraCrystals";q.title="Six bright answers";q.steps=new[]{new Objective{kind=ObjectiveKind.Collect,target="RidgeCrystal",count=6}};q.rewardXp=80;q.rewardScrip=100;});
        }
        public static void Materials()
        {
            string root="Assets/_Project/Resources/Blockout/";Directory.CreateDirectory(root);
            foreach(var pair in new[]{("Ground",new Color(.34f,.25f,.19f)),("Rock",new Color(.47f,.36f,.27f)),("Taren",new Color(1,.6f,.12f)),("Sela",new Color(.16f,.85f,.94f)),("Enemy",new Color(.67f,.19f,.27f)),("Emission",new Color(.2f,1,1)),("Threat",new Color(1,.18f,.07f))})
            {
                string path=root+pair.Item1+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Lattice/Toon"));AssetDatabase.CreateAsset(mat,path);}
                mat.SetColor("_BaseColor",pair.Item2);if(pair.Item1=="Emission"||pair.Item1=="Threat")mat.SetColor("_EmissionColor",pair.Item2*4);EditorUtility.SetDirty(mat);
            }
        }
        static void ConfigureHd2d()
        {
            string path="Assets/_Project/Settings/Hd2d.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Lattice/HD2D"));AssetDatabase.CreateAsset(mat,path);}
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/_Project/Settings/URP-3D-Renderer.asset");
            Hd2dFeature feature=null;foreach(var f in renderer.rendererFeatures)if(f is Hd2dFeature h)feature=h;
            if(feature==null){feature=ScriptableObject.CreateInstance<Hd2dFeature>();feature.name="Lattice HD2D";AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);}
            feature.material=mat;feature.Create();EditorUtility.SetDirty(feature);renderer.SetDirty();
        }
        public static GameObject Block(string name,Vector3 position,Vector3 scale,string material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.position=position;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Blockout/"+material);go.isStatic=true;return go;
        }
        public static void BuildGeneratedArenas(){BuildScene(false);BuildScene(true);}
        static void BuildScene(bool flight)
        {
            string name=flight?"Arena_Flight":"Arena_Ground";
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("ZoneRoot",typeof(ZoneController),typeof(ArenaRuntime));root.GetComponent<ZoneController>().definition=GameCatalog.Find<ZoneDef>(name);
            new GameObject("Arrival",typeof(SpawnPoint)).transform.position=new Vector3(0,flight?1:0,-4);
            var floor=Block("Training terrain",new Vector3(0,-.7f,0),new Vector3(40,1,40),"Ground");WorldArt.Dress(floor,flight?"GulletMembrane":"MoonGroundA",new(40,1,40));
            for(int i=0;i<8;i++)WorldBuilder.Piece("RidgeRock"+(i%3+1),new Vector3((i%2==0?-1:1)*(11+i%3),1,i*5-18),new Vector3(4,2+i%3,3));
            if(flight)for(int i=0;i<5;i++)for(int sign=-1;sign<=1;sign+=2)WorldBuilder.Piece("GulletWallA",new Vector3(sign*17,3,i*8-16),new Vector3(1,8,8));
            WorldBuilder.Piece("CrystalClusterB",new Vector3(-4,.7f,3),Vector3.one*1.2f,"Emission");
            var light=new GameObject("Sun",typeof(Light));light.transform.rotation=Quaternion.Euler(48,-30,0);light.GetComponent<Light>().type=LightType.Directional;light.GetComponent<Light>().intensity=1.5f;light.GetComponent<Light>().shadows=LightShadows.Soft;
            RenderSettings.ambientLight=new Color(.35f,.39f,.5f);
            var volume=new GameObject("Look",typeof(Volume)).GetComponent<Volume>();volume.isGlobal=true;
            string vp="Assets/_Project/Settings/"+name+"Volume.asset";
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(vp);
            if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,vp);}
            profile.components.RemoveAll(c=>c==null);
            if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}
            if(!profile.TryGet<Vignette>(out var vignette)){vignette=profile.Add<Vignette>();AssetDatabase.AddObjectToAsset(vignette,profile);}
            bloom.intensity.Override(.45f);bloom.threshold.Override(1.1f);vignette.intensity.Override(.17f);
            EditorUtility.SetDirty(profile);EditorUtility.SetDirty(bloom);EditorUtility.SetDirty(vignette);volume.sharedProfile=profile;
            var guide=WorldBuilder.Npc("Hal",new Vector3(7,0,-4),"ArenaGuide");guide.repeatNode="ArenaGuide";guide.postNode="ArenaGuide";
            for(int i=0;i<3;i++){var spawn=new GameObject("EnemySpawn_"+i,typeof(Spawner));spawn.transform.position=new Vector3((i-1)*5,flight?1:0,5);var s=spawn.GetComponent<Spawner>();s.definition=GameCatalog.Find<EnemyDef>(flight?"ChoristerDart":"Ridgehound");s.count=1;s.radius=0;}
            EditorSceneManager.SaveScene(scene,"Assets/_Project/Scenes/"+name+".unity");
        }
    }
}
